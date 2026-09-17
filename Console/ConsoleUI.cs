using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

namespace chatter_new_console;

public static class ConsoleUI {
    static ConsoleUI() {
        ConsoleManager.Setup();
    }
    
    internal static (IControl, BasicController) BasicIOLayout() {
        var (control, textBoxFacade) = CreateTextbox();

        var textPan = new VerticalStackPanel() { };
        
        var root = new DockPanel() {
            Placement = DockPanel.DockedControlPlacement.Top,
            DockedControl = new VerticalStackPanel() {
                Children = new Control[] {
                    new TextBlock(){Text = "chatter-new", Color = ConsoleColor.DarkCyan},
                    // new TextBlock(){Text = "Downloads (todo)"},
                }
            },
            FillingControl = new Margin() {
                Offset = new Offset(0, 1, 1, 0),
                Content = new DockPanel() {
                    Placement = DockPanel.DockedControlPlacement.Bottom,
                    DockedControl = control,
                    FillingControl = textPan
                }
            } 
        };
        
        IInputListener[] inputListeners = [textBoxFacade.Input];
        
        return (root, BasicController.Create(inputListeners, textPan, textBoxFacade));
    }
    
    internal static (IControl, ChatController) ChattingUI() {
        var msgs = new VerticalStackPanel() {
            Children = new Control[] { }
        };
    
        var scrollPanel = new VerticalScrollPanel() {
            ScrollBarForeground = new Character('▀', ConsoleColor.Gray),
            Content = new Margin() {
                Offset = new Offset(0, 0, 1, 0), 
                Content = new VerticalStackPanel() {
                    Children = new Control[] {msgs}
                }
            }
        };
        var marg = new Margin { Content = scrollPanel, };
        var msgList = new DockPanel() {
            Placement = DockPanel.DockedControlPlacement.Bottom,
            DockedControl = marg
        };
    
        marg.Offset = new Offset(0, 
            Math.Max(msgList.Size.Height - msgs.Size.Height - 1, 0), 
            0, 0);

        var tbox = CreateTextbox();
        var bottomInfo = new TextBlock(){Text = ""};
    
        var root = new DockPanel() {
            Placement = DockPanel.DockedControlPlacement.Top,
            DockedControl = new VerticalStackPanel() {
                Children = new Control[] {
                    new TextBlock(){Text = "chatter-new", Color = ConsoleColor.DarkCyan},
                    new TextBlock(){Text = "Downloads (todo)"},
                }
            },
            FillingControl = new DockPanel() {
                Placement = DockPanel.DockedControlPlacement.Bottom,
                DockedControl = new VerticalStackPanel() {
                    Children = new Control[] {
                        tbox.Control,
                        bottomInfo,
                    }
                },
                FillingControl = msgList
            },
        };
        
        var inputs = new List<IInputListener> { scrollPanel, tbox.textBoxFacade.Input };

        return (root, ChatController.Create(inputs, msgs, tbox.textBoxFacade, scrollPanel, () => {
            marg.Offset = new Offset(0,
                Math.Max(msgList.Size.Height - msgs.Size.Height - 1, 0),
                0, 0);

            bottomInfo.Text = $"{DateTime.Now:T} \t Type /help to list commands";
        }));
    }

    internal static (Control Control, TextBoxFacade textBoxFacade) CreateTextbox() {
        var tbox = new TextBox() { Text = "" };
        var styler = new Style() {
            Content = tbox
        };
        return (new WrapPanel() {
            Content = new HorizontalStackPanel() {
                Children = new Control[] {
                    new TextBlock() { Text = "> ", Color = ConsoleColor.Gray },
                    styler
                }
            }
        }, new TextBoxFacade(tbox, styler));
    }

    internal static Control CreateMessage(string name, string text) {
        var r = new BreakPanel() {
            Content = new HorizontalStackPanel {
                Children = new Control[] {
                    new TextBlock { Text = $"{DateTime.Now:hh:mm}", Color = ConsoleColor.Gray },
                    new VerticalSeparator(){Character = new Character(' ')},
                    new TextBlock { Text = name, Color = ConsoleColor.Yellow },
                    new TextBlock { Text = ":" },
                    new VerticalSeparator(){Character = new Character(' ')},
                    new TextBlock { Text = text },
                },
            }
        };
        return r;
    }

    internal static Control CreateSysMessage(string text) {
        var r = new Box {
                Content = new TextBlock { Text = text, Color = ConsoleColor.Gray },
                HorizontalContentPlacement = Box.HorizontalPlacement.Center,
            };
        return r;
    }
}

internal class TextBoxFacade(TextBox input, Style styler) {
    public TextBox Input { get; } = input;
    public Style Style { get; } = styler;
    
    public string Text {get => Input.Text; set => Input.Text = value; }
}
