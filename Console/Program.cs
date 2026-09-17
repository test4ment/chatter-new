using System.Diagnostics;
using chatter_new_console;
using System.Net;
using System.Text;
using System.Text.Json;
using chatter_crypto;
using chatter_new.Messaging;
using chatter_new.Messaging.Connection;
using chatter_new.Messaging.Messages;

// TODO: LLM security review

Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;

const int defaultPort = 50001;

var (baseUI, @base) = ConsoleUI.BasicIOLayout();
var (chatUI, chat) = ConsoleUI.ChattingUI();

var nav = new ScreenNavigator();
var menuScreen = new Screen(baseUI, @base.Tick);
var chatScreen = new Screen(chatUI, chat.Tick);

var appCts = new CancellationTokenSource();
var appCtx = appCts.Token;

var username = RandomUsername.Generate();
Action<string>? chatOnEnter = null;

var appState = new State(Menu);

nav.Show(menuScreen);
ShowMenu();

@base.OnEnter += appState.Handle;

while (true) {
    nav.Tick();
    try {
        await Task.Delay(16, appCtx);
    }
    catch (OperationCanceledException) {
        break;
    }
}

void ShowMenu() {
    nav.Show(menuScreen);
    @base.SetText(
        // todo: move into tests
        $"Chatter\nHello {username}!\n\n" +
        $"1. Connect to localhost:{defaultPort}\n" +
        "2. Connect to localhost:16777\n" +
        $"3. Listen on localhost:{defaultPort}\n" +
        "4. Connect to address\n" +
        "5. Settings\n");
}

void Menu(string input, State state) {
    if (string.IsNullOrWhiteSpace(input.Trim())) {
        @base.BlinkUserInput();
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
                    SocketConnection.ListenAndAwaitClient(new IPEndPoint(IPAddress.Loopback, defaultPort), CancellationToken),
                appCtx);
            break;
        case "4":
            state.ChangeState(ConnectAddress);
            @base.SetInputText("");
            ConnectAddress("", state);
            break;
        case "5":
            state.ChangeState(Settings);
            Settings("", state);
            break;
        default:
            @base.BlinkUserInput();
            break;
    }
}

void ConnectAddress(string input, State state) {
    
    @base.SetText(
        "Connect to address\n\n" +
        $"Enter IP or IP:port (default port: {defaultPort}).\n" +
        "0. Cancel");

    if (string.IsNullOrWhiteSpace(input.Trim())) return;

    var line = input.Trim();
    if (line == "0") {
        state.ChangeState(Menu);
        ShowMenu();
        return;
    }

    if (!line.Contains(':'))
        line += $":{defaultPort}";

    if (!IPEndPoint.TryParse(line, out var endpoint)) {
        @base.AddMsg("Invalid address. Expected IP:port");
        @base.BlinkUserInput();
        return;
    }

    state.ChangeState(Menu);
    @base.SetInputText("");
    _ = RunSessionAsync(ct => SocketConnection.ConnectTo(endpoint, ct), appCtx);
}

void Settings(string input, State state) {
    @base.SetText(
        "Settings\n\n" +
        $"Username: {username}\n\n" +
        "1. Regenerate username\n" +
        "0. Back");

    if (string.IsNullOrWhiteSpace(input.Trim())) return;

    switch (input.Trim()) {
        case "1":
            username = RandomUsername.Generate();
            @base.AddMsg("New username: " + username);
            Settings("", state);
            break;
        case "0":
            state.ChangeState(Menu);
            ShowMenu();
            break;
        default:
            @base.BlinkUserInput();
            break;
    }
}

async Task RunSessionAsync(Func<CancellationToken, Task<SocketConnection>> open, CancellationToken appCtx) {
    Protocol? sess = null;
    UniversalEncryption? enc = null;
    try {
        using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(appCtx);
        var sessionCtx = sessionCts.Token;

        nav.Show(chatScreen);
        chat.AddSysMessage($"Connecting...");

        var conn = await open(sessionCtx);
        sess = new Protocol(conn);
        chat.AddSysMessage($"Connected");

        chat.AddSysMessage($"Handshaking...");
        enc = await new DHHandshake(sess).Perform(sessionCtx);
        await sess.Send(PrepareMsg(new UserInfoMessage(username), enc), sessionCtx);

        chat.AddSysMessage("Waiting for remote username...");
        var remoteName = await ReadRemoteNameAsync(sess, enc, sessionCtx);
        if (remoteName is null) {
            chat.AddSysMessage("Connection closed before the remote announced a username.");
            return;
        }

        await RunChatAsync(sess, enc, remoteName, sessionCtx);
    }
    catch (OperationCanceledException) {
    }
    catch (Exception e) {
        if (!appCtx.IsCancellationRequested) {
            chat.AddSysMessage("Session error: " + e.Message);
            try {
                await Task.Delay(1500, appCtx);
            }
            catch (OperationCanceledException) {
            }
        }
    }
    finally {
        if (sess != null && enc != null) {
            await SendLeaveAsync(sess, enc);
            await sess.DisposeAsync();
            sess = null;
            enc = null;
        }
        if (!appCtx.IsCancellationRequested) {
            DetachChatInput();
            ShowMenu();
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

    chatOnEnter = async void (text) => {
        if (string.IsNullOrWhiteSpace(text)) return;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("/img", StringComparison.Ordinal)) {
            chat.AddSysMessage("File sending is not implemented yet.");
            chat.Text = "";
            return;
        }

        try {
            await sess.Send(PrepareMsg(new TextMessage(text), enc), ct);
            chat.AddMessage(username, text);
            chat.Text = "";
            chat.ScrollToBotton();
        }
        catch (Exception e) {
            chat.AddSysMessage("Failed to send: " + e.Message);
        }
    };
    chat.OnEnter += chatOnEnter;

    Console.CancelKeyPress += async (_, e) => {
        if (appCts.IsCancellationRequested) return;
        await SendLeaveAsync(sess, enc);
        appCts.Cancel();
        e.Cancel = true;
    };

    nav.Show(chatScreen);
    chat.AddSysMessage($"Connected to {remoteName}. Ctrl+C to leave");
    
    await foreach (var frame in sess.ReadFramesAsync(ct)) {
        var msg = ProcessMsg(frame, enc);
        if (msg is SystemMessage { Type: SystemMessage.SysMsgType.Left }) {
            chat.AddSysMessage($"{remoteName} has left.");
            await SendLeaveAsync(sess, enc);
            await Task.Delay(1500, appCtx);
            break;
        }
        HandleMessage(msg, remoteName);
    }

    chat.AddSysMessage($"{remoteName} closed the connection.");
}

void HandleMessage(BaseMessage msg, string sender) {
    switch (msg) {
        case TextMessage tmsg:
            chat.AddMessage(sender, tmsg.Text);
            chat.ScrollToBotton();
            break;
        case SystemMessage { Type: SystemMessage.SysMsgType.Left }:
            throw new UnreachableException();
        case UserInfoMessage userInfo:
            chat.AddSysMessage($"{userInfo.Name} joined the chat.");
            break;
        case BLOBMessage blob:
            chat.AddSysMessage($"Received file '{blob.Filename}' ({blob.Data.Length} bytes): saving not implemented.");
            break;
        case RetransmittedMessage rmsg:
            HandleMessage(rmsg.Msg, rmsg.OriginalSender);
            break;
        default:
            chat.AddSysMessage("Received an unknown message.");
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
    if (chatOnEnter is not null) {
        chat.OnEnter -= chatOnEnter;
        chatOnEnter = null;
    }
}

byte[] PrepareMsg(BaseMessage msg, UniversalEncryption enc) {
    return enc.Encrypt(msg.Serialize().Encode());
}

BaseMessage ProcessMsg(byte[] data, UniversalEncryption enc) {
    return JsonSerializer.Deserialize<BaseMessage>(enc.Decrypt(data).Decode())!;
}