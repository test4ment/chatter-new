using System.Diagnostics;
using chatter_new_console;
using System.Net;
using System.Text;
using chatter_crypto;
using chatter_messages;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using ConsoleGUI;
using ConsoleGUI.Input;
using DHHandshake = chatter_new.Messaging.DHHandshake;

// TODO: LLM security review

Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;

const int defaultPort = 50001;

var (root, tinput, tout) = ConsoleUI.BasicIOLayout();
var ioEnter = new EnterInput();

var (chatRoot, chatInput, chatMsg, chatBottomInfo) = ConsoleUI.ChattingUI();
var chatEnter = new EnterInput();

ConsoleManager.Content = root;


var appCts = new CancellationTokenSource();
var appCtx = appCts.Token;

var username = RandomUsername.Generate();
Action? chatOnEnterHandler = null;

ShowMenu();

IReadOnlyCollection<IInputListener>? currentInput = null;
var currentState = Menu;

ioEnter.OnEnter += () => currentState(tinput.Text);
IOScreen();

while (true) {
    ConsoleManager.ReadInput(currentInput);
    ConsoleManager.AdjustBufferSize();
    try {
        await Task.Delay(16, appCtx);
    }
    catch (OperationCanceledException) {
        break;
    }
}

return;

void IOScreen() {
    ConsoleManager.Content = root;
    currentInput = [ioEnter, tinput];
}

void ChatScreen() {
    ConsoleManager.Content = chatRoot;
    currentInput = [chatEnter, chatMsg, chatInput];
}

void ShowMenu() {
    tout.SetText(
        // todo: move into tests
        $"Hello {username}!\n \n" +
        $"1. Connect to localhost:{defaultPort}\n" +
        "2. Connect to localhost:16777\n" +
        $"3. Listen on any:{defaultPort}\n" +
        "4. Connect to address\n" +
        "5. Settings\n");
}

void Menu(string input) {
    if (string.IsNullOrWhiteSpace(input.Trim())) {
        // @base.BlinkUserInput();
        return;
    }

    switch (input.Trim()) {
        case "1":
            _ = RunSessionAsync(
                CancellationToken =>
                    SocketConnection.ConnectTo(new IPEndPoint(IPAddress.Loopback, defaultPort), CancellationToken), appCtx);
            break;
        case "2":
            _ = RunSessionAsync(
                CancellationToken =>
                    SocketConnection.ConnectTo(new IPEndPoint(IPAddress.Loopback, 16777), CancellationToken), appCtx);
            break;
        case "3":
            _ = RunSessionAsync(
                CancellationToken =>
                    SocketConnection.ListenAndAwaitClient(new IPEndPoint(IPAddress.Any, defaultPort), CancellationToken),
                appCtx);
            break;
        case "4":
            currentState = ConnectAddress;
            chatInput.Text = "";
            ConnectAddress("");
            break;
        case "5":
            currentState = Settings;
            Settings("");
            break;
        default:
            // @base.BlinkUserInput();
            break;
    }
    tinput.Text = "";
}

void ConnectAddress(string input) {
    tout.SetText(
        "Connect to address\n\n" +
        $"Enter IP or IP:port (default port: {defaultPort}).\n" +
        "0. Cancel");

    if (string.IsNullOrWhiteSpace(input.Trim())) return;

    var line = input.Trim();
    if (line == "0") {
        currentState = Menu;
        ShowMenu();
        return;
    }

    if (!line.Contains(':'))
        line += $":{defaultPort}";

    if (!IPEndPoint.TryParse(line, out var endpoint)) {
        tout.AddNotification("Invalid address. Expected IP:port");
        // @base.BlinkUserInput();
        return;
    }

    currentState = Menu;
    chatInput.Text = "";
    _ = RunSessionAsync(ct => SocketConnection.ConnectTo(endpoint, ct), appCtx);
}

void Settings(string input) {
    tout.SetText(
        "Settings\n\n" +
        $"Username: {username}\n\n" +
        "1. Regenerate username\n" +
        "0. Back");

    if (string.IsNullOrWhiteSpace(input.Trim())) return;

    switch (input.Trim()) {
        case "1":
            username = RandomUsername.Generate();
            Settings("");
            tout.AddNotification("New username: " + username);
            break;
        case "0":
            currentState = Menu;
            ShowMenu();
            break;
        default:
            // @base.BlinkUserInput();
            break;
    }
}

async Task RunSessionAsync(Func<CancellationToken, Task<SocketConnection>> open, CancellationToken appCtx) {
    Protocol? sess = null;
    UniversalEncryption? enc = null;
    try {
        using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(appCtx);
        var sessionCtx = sessionCts.Token;

        ChatScreen();

        chatMsg.AddSysMessage($"Connecting...");

        var conn = await open(sessionCtx);
        sess = new Protocol(conn);
        chatMsg.AddSysMessage($"Connected");

        chatMsg.AddSysMessage($"Handshaking...");
        enc = await new DHHandshake(sess).Perform(sessionCtx);
        await sess.Send(PrepareMsg(new UserInfoMessage(username), enc), sessionCtx);

        chatMsg.AddSysMessage("Waiting for remote username...");
        var remoteName = await ReadRemoteNameAsync(sess, enc, sessionCtx);
        if (remoteName is null) {
            chatMsg.AddSysMessage("Connection closed before the remote announced a username.");
            return;
        }

        await RunChatAsync(sess, enc, remoteName, sessionCtx);
    }
    catch (OperationCanceledException) {
    }
    catch (Exception e) {
        if (!appCtx.IsCancellationRequested) {
            chatMsg.AddSysMessage("Session error: " + e.Message);
            try {
                await Task.Delay(1500, appCtx);
            }
            catch (OperationCanceledException) {
            }
        }
    }
    finally {
        if (sess != null) {
            await sess.DisposeAsync();
            sess = null;
            enc = null;
        }
        if (!appCtx.IsCancellationRequested) {
            DetachChatInput();
            ShowMenu();
            IOScreen();
        }
    }
}

async Task<string?> ReadRemoteNameAsync(Protocol sess, UniversalEncryption enc, CancellationToken ct) {
    while (true) {
        var frame = await sess.ReadNextFrameAsync(ct);
        if (frame is null) return null;

        var msg = ProcessMsg(frame, enc);
        if (msg is UserInfoMessage userInfo) return userInfo.Name;
    }
}

async Task RunChatAsync(Protocol sess, UniversalEncryption enc, string remoteName, CancellationToken ct) {
    DetachChatInput();

    var exitToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
    chatOnEnterHandler = async void () => {
        var text = chatInput.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("/img", StringComparison.Ordinal)) {
            chatMsg.AddSysMessage("File sending is not implemented yet.");
            chatInput.Text = "";
            return;
        }

        if (trimmed == "/exit") {
            chatMsg.AddSysMessage("Exiting...");
            chatInput.Text = "";
            exitToken.Cancel();
            await SendLeaveAsync(sess, enc);
            // await sess.DisposeAsync();
            return;
        }

        try {
            await sess.Send(PrepareMsg(new TextMessage(text), enc), ct);
            chatMsg.AddMessage(username, text);
            chatInput.Text = "";
            chatMsg.ScrollToBotton();
        }
        catch (Exception e) {
            chatMsg.AddSysMessage("Failed to send: " + e.Message);
        }
    };
    chatEnter.OnEnter += chatOnEnterHandler;
    
    Console.CancelKeyPress += (__, e) => {
        e.Cancel = true;
        if (appCts.IsCancellationRequested) return;
        _ = ExitSessionAsync();
    };

    chatMsg.AddSysMessage($"Connected to {remoteName}. Ctrl+C or /exit to leave");
    
    await foreach (var frame in sess.ReadFramesAsync(exitToken.Token)) {
        var msg = ProcessMsg(frame, enc);
        if (msg is SystemMessage { Type: SystemMessage.SysMsgType.Left }) {
            chatMsg.AddSysMessage($"{remoteName} has left.");
            await Task.Delay(1500, appCtx);
            break;
        }
        HandleMessage(msg, remoteName);
    }

    return;

    async Task ExitSessionAsync() {
        await SendLeaveAsync(sess, enc);
        await sess.DisposeAsync();
        appCts.Cancel();
    }
}

void HandleMessage(BaseMessage msg, string sender) {
    switch (msg) {
        case TextMessage tmsg:
            chatMsg.AddMessage(sender, tmsg.Text);
            chatMsg.ScrollToBotton();
            break;
        case SystemMessage { Type: SystemMessage.SysMsgType.Left }:
            throw new UnreachableException();
        case UserInfoMessage userInfo:
            chatMsg.AddSysMessage($"{userInfo.Name} joined the chat.");
            break;
        case BLOBMessage blob:
            chatMsg.AddSysMessage($"Received file '{blob.Filename}' ({blob.Data.Length} bytes): saving not implemented.");
            break;
        case RetransmittedMessage rmsg:
            HandleMessage(rmsg.Msg, rmsg.OriginalSender);
            break;
        default:
            chatMsg.AddSysMessage("Received an unknown message.");
            break;
    }
}

async Task SendLeaveAsync(Protocol sess, UniversalEncryption enc) {
    try {
        await sess.Send(PrepareMsg(new SystemMessage(SystemMessage.SysMsgType.Left), enc), CancellationToken.None);
    }
    catch { // ignored
    }
}

void DetachChatInput() {
    if (chatOnEnterHandler is not null) {
        chatEnter.OnEnter -= chatOnEnterHandler;
        chatOnEnterHandler = null;
    }
}

byte[] PrepareMsg(BaseMessage msg, UniversalEncryption enc) {
    return enc.Encrypt(msg.Serialize());
}

BaseMessage ProcessMsg(byte[] data, UniversalEncryption enc) {
    return BaseMessage.Deserialize(enc.Decrypt(data));
}
