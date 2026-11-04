using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;
using ConsoleGUI.UserDefined;

namespace chatter_new_console;

internal class ChatHistoryWindow : SimpleControl, IInputListener {
    private readonly VerticalStackPanel msgCollection;
    private readonly VerticalScrollPanel scrollPanel;
    
    public ChatHistoryWindow() {
        this.msgCollection = new VerticalStackPanel() {
            Children = Array.Empty<Control>()
        };
        this.scrollPanel = new VerticalScrollPanel() {
            ScrollBarForeground = new Character('▀', ConsoleColor.Gray),
            Content = new Margin() {
                Offset = new Offset(0, 0, 1, 0), 
                Content = new DockPanel() {
                    Placement = DockPanel.DockedControlPlacement.Bottom,
                    DockedControl = new VerticalStackPanel() {
                        Children = new Control[] {msgCollection}
                    }
                }
            }
        };

        this.Content = scrollPanel;
    }
    
    public void ScrollToBotton() {
        scrollPanel.Top = int.MaxValue;
    }

    public void AddMessage(string name, string text) {
        msgCollection.Add(CreateMessage(name, text));
    }
    
    public void AddSysMessage(string text) {
        msgCollection.Add(CreateSysMessage(text));
    }

    internal static Control CreateMessage(string name, string text) {
        var r = new BreakPanel() {
            Content = new HorizontalStackPanel {
                Children = new Control[] {
                    new TextBlock { Text = $"{DateTime.Now:HH:mm}", Color = ConsoleColor.Gray },
                    new VerticalSeparator(){Character = new Character(' ')},
                    new TextBlock { Text = name, Color = ConsoleColor.Yellow },
                    new TextBlock { Text = ":" },
                    new VerticalSeparator(){Character = new Character(' ')},
                    new TextBlock { Text = text },
                },
            },
        };
        return r;
    }

    internal static Control CreateSysMessage(string text) {
        return new CenteredMessage(text, ConsoleColor.Gray);
    }

    public void OnInput(InputEvent inputEvent) {
        ((IInputListener)scrollPanel).OnInput(inputEvent);
    }
}