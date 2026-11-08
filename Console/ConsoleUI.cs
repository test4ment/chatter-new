using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using ConsoleGUI.UserDefined;

namespace chatter_new_console;

public static class ConsoleUI {
    static ConsoleUI() {
        ConsoleManager.Setup();
    }
    
    internal static (IControl root, TextInputBox, TextDisplay) BasicIOLayout() {
        var tinput = new TextInputBox();
        var textDisplay = new TextDisplay();

        var textPanel = new DockPanel() {
            Placement = DockPanel.DockedControlPlacement.Bottom,
            DockedControl = tinput,
            FillingControl = textDisplay
        };
        
        var root = ChatterHeader(textPanel);
        
        return (root, tinput, textDisplay);
    }

    internal static IControl ChatterHeader(IControl content, IControl[]? additional = null) {
        return new DockPanel() {
            Placement = DockPanel.DockedControlPlacement.Top,
            DockedControl = new VerticalStackPanel() {
                Children = [
                    new TextBlock() { Text = "chatter-new", Color = ConsoleColor.DarkCyan },
                    .. additional ?? []
                ]
            },
            FillingControl = new Margin() {
                Offset = new Offset(0, 1, 1, 0),
                Content = content
            }
        };
    }
    
    internal static (IControl root, TextInputBox, ChatHistoryWindow, TextBlock bottomInfo) ChattingUI() {
        var textin = new TextInputBox();
        var chatHistoryWindow = new ChatHistoryWindow();
        var bottomInfo = new TextBlock(){Text = "current time should be here"};

        var root = ChatterHeader(new DockPanel() {
                Placement = DockPanel.DockedControlPlacement.Bottom,
                DockedControl = new VerticalStackPanel() {
                    Children = new IControl[] {
                        textin,
                        bottomInfo,
                    }
                },
                FillingControl = chatHistoryWindow
            }, [new TextBlock() { Text = "Downloads (todo)" }]
        );
        
        return (root, textin, chatHistoryWindow, bottomInfo);
    }
}
