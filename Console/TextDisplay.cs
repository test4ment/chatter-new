using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Space;
using ConsoleGUI.UserDefined;

namespace chatter_new_console;

internal class TextDisplay: SimpleControl {
    private readonly VerticalStackPanel textPan = new();

    public TextDisplay() => this.Content = this.textPan;

    public void SetText(string text) {
        var en = text.Split("\n").Select(Control (s) => 
            new WrapPanel() {
                Content = new TextBlock() { Text = s }
            });
        
        textPan.Children = en;
    }

    public void AddNotification(string msg) {
        textPan.Add(new Margin() {
            Offset = new Offset(0, 1, 0, 0),
            Content = new WrapPanel() {
                Content = new TextBlock() {
                    Text = msg, Color = new Color(0xFF, 0xEE, 0x8C)
                }
            },
        });
    }
}
