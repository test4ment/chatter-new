using chatter_new_console;
using System.Collections.Concurrent;
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

var (baseUI, @base) = ConsoleUI.BasicIOLayout();
var (chatUI, chat) = ConsoleUI.ChattingUI();
var nav = new ScreenNavigator();
var menuScreen = new Screen(baseUI, @base.Tick);
var chatScreen = new Screen(chatUI, chat.Tick);

var appState = new State(Start);
var username = RandomUsername.Generate();

var tok = new CancellationTokenSource();

nav.Show(menuScreen);
Start("", appState);

@base.OnEnter += appState.Handle;
chat.OnEnter += appState.Handle;

while (true) {
    await Task.Delay(16, tok.Token);
    nav.Tick();
}

void Options(string input, State state) {
    @base.SetText("0. Back\n"+
                  "1. Change username");

    switch (input.Trim()) {
        case "0":
            state.ChangeState(Start);
            Start("", state);
            break;
        case "1":
            username = RandomUsername.Generate();
            @base.AddMsg("New username: " + username);
            break;
        default:
            @base.BlinkUserInput();
            break;
    }
}

void Start(string input, State state) {
    @base.SetText(
        // todo: move into tests
        "1. Client connect to localhost:50001\n" +
        "2. Client connect to localhost:16777\n" +
        "3. Server listen at localhost:50001\n" +
        "4. Connect\n" +
        "5. Options\n");

    if (string.IsNullOrWhiteSpace(input)) return;
    switch (input.Trim()) {
        case "1":
            nav.Show(chatScreen);
            var p = ConnectTo(50001).Result;
            
            break;
        case "2":
            nav.Show(chatScreen);
            break;
        case "3":
            nav.Show(chatScreen);
            var p2 = Listen(50001).Result;
            
            break;
        case "4":
            nav.Show(chatScreen);
            break;
        case "5":
            state.ChangeState(Options);
            Options(input, state);
            @base.BlinkUserInput();
            break;
        default:
            @base.BlinkUserInput();
            break;
    }
}

async Task<Protocol> ConnectTo(int port) {
    var ip = new IPEndPoint(IPAddress.Loopback, port);
    chat.AddSysMessage($"Connecting...");
    var sess = new Protocol(await SocketConnection.ConnectTo(ip));
    chat.AddSysMessage($"Connected to {ip}");
    return sess;
}

async Task<Protocol> Listen(int port) {
    var ip = new IPEndPoint(IPAddress.Loopback, port);
    chat.AddSysMessage($"Waiting for connections...");
    var sess = new Protocol(await SocketConnection.ListenAndAwaitClient(ip));
    chat.AddSysMessage($"Client connected");
    return sess;
}


byte[] PrepareMsg(BaseMessage msg, UniversalEncryption enc) {
    return enc.Encrypt(msg.Serialize().Encode());
}

BaseMessage ProcessMsg(byte[] msg, UniversalEncryption enc) {
    return JsonSerializer.Deserialize<BaseMessage>(enc.Decrypt(msg).Decode())!;
}

// Console.WriteLine("Sending handshake");
// enc = await new DHHandshake(sess).Perform();
// Console.WriteLine("Got handshake, sending username");
// await sess.Send(PrepareMsg(new UserInfoMessage(username)));
// Console.WriteLine("Sent username");
//
// var msgQueue = new ConcurrentQueue<byte[]>();
// _ = Task.Run(async () =>
// {
//     try
//     {
//         await foreach (var frame in sess.ReadFramesAsync(tok.Token))
//             msgQueue.Enqueue(frame);
//     }
//     catch (OperationCanceledException) { }
// }, tok.Token);
//
// bool running = true;
// Console.CancelKeyPress += (_, __) =>
// {
//     Console.WriteLine("Exiting...");
//     sess.Send(PrepareMsg(new SystemMessage(SystemMessage.SysMsgType.Left))).Wait();
//     running = false;
//     tok.Cancel();
// };
//
// string nick = "";
//
// byte[]? result;
// Console.WriteLine("Waiting name");
// while(!msgQueue.TryDequeue(out result) );
// var r = ProcessMsg(result);
//
// if (r is UserInfoMessage userInfo) {
//     nick = userInfo.Name;
// }
// else { 
//     Console.WriteLine("Unknown payload");
//     sess.Send(PrepareMsg(new SystemMessage(SystemMessage.SysMsgType.Left))).Wait();
//     return;
// }
//
//
// // sess.OnReceive += (sender, msg) =>
// // {
// //     
//     // if (msg is BLOBMessage blob)
//     // {
//     //     Console.WriteLine($"Got {blob.Filename} ({blob.Data.Length} bytes)");
//     //     var path = Path.GetFullPath(blob.Filename);
//     //     while (File.Exists(path))
//     //     {
//     //         var i = 1;
//     //         path = Path.GetFullPath(Path.GetFileNameWithoutExtension(blob.Filename) + $"-{i}" + Path.GetExtension(blob.Filename));
//     //         ++i;
//     //     }
//     //     Console.WriteLine($"Saving to {path}");
//     //     try
//     //     {
//     //         File.WriteAllBytes(path, blob.Data);
//     //         Console.WriteLine($"Saved successfully");
//     //     }
//     //     catch (Exception e)
//     //     {
//     //         Console.WriteLine($"Error: {e.Message}");
//     //     }
//     //     
//     // }
// // };
//
// // int downloaded = 0;
// // var started = DateTime.Now;
// // sess.OnMsgProgress += (sender, progress) =>
// // {
// //     if(downloaded == 0)
// //         started = DateTime.Now;
// //     downloaded = progress.Current;
// //     Console.Write("\r" + '\t'*5 + "\r");
// //     if (progress.Current < progress.Total)
// //     {
// //         Console.Write(
// //             $"Downloading blob {progress.Current / (float)progress.Total:P} ({downloaded / 1024f / ((DateTime.Now - started).Seconds + 1)} KiB/s)" + ' ' * 10);
// //     }
// //     else
// //     {
// //         downloaded = 0;
// //         Console.WriteLine("Finished!");
// //     }
// // };
//
// Console.WriteLine("Started main loop");
// while (running)
// {
//     if (Console.KeyAvailable)
//     {
//         var inp = Console.ReadLine();
//         if(inp?.StartsWith("/img") ?? false)
//         {
//             Console.WriteLine("File transfer currently unavailable");
//             // try
//             // {
//             //     var path = inp.Split()[1];
//             //     var fname = Path.GetFileName(path);
//             //     var bytes = File.ReadAllBytes(path);
//             //     _ = sess.Send(PrepareMsg(new BLOBMessage(bytes, fname)));
//             // }
//             // catch (Exception e)
//             // {
//             //     Console.WriteLine($"Error: {e.Message}");
//             // }
//         }
//         else if(!string.IsNullOrEmpty(inp))
//             _ = sess.Send(PrepareMsg(new TextMessage(inp)));
//     }
//
//     if (!msgQueue.IsEmpty) {
//         if (!msgQueue.TryDequeue(out var fmsg)) continue;
//         
//         var msg = ProcessMsg(fmsg);
//
//         HandleMessage(msg);
//     }
//
//     await Task.Delay(16);
// }

// void HandleMessage(BaseMessage msg) {
//     switch (msg) {
//         case RetransmittedMessage rmsg:
//             if (rmsg.Msg is TextMessage tmsgi) 
//                 Console.WriteLine(rmsg.OriginalSender + ": " + tmsgi.Text);
//             break;
//         case TextMessage tmsg:
//             Console.WriteLine(nick + ": " + tmsg.Text);
//             break;
//         case SystemMessage { Type: SystemMessage.SysMsgType.Left }:
//             Console.WriteLine($"{nick} has left. Exiting...");
//             running = false;
//             break;
//     }
// }