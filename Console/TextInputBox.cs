using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.UserDefined;

namespace chatter_new_console;


public class TextInputBox: SimpleControl, IInputListener {
    private const int BlinkTime = 16;
    private int blinkFor = 0;
    private Color? knownColor;
    
    private readonly CtrlBspace ctrlBspace;
    private readonly TextBox tbox = new();
    private readonly Style styler;
    
    public string Text {get => tbox.Text; set => tbox.Text = value; }
    
    // public Action<string>? OnTextboxUpdate; // TODO: commands window
    
    public TextInputBox() {
        this.styler = new Style() { Content = this.tbox };
        this.ctrlBspace = new CtrlBspace(tbox);
        this.Content = new WrapPanel() {
            Content = new HorizontalStackPanel() {
                Children = new Control[] {
                    new TextBlock() { Text = "> ", Color = ConsoleColor.Gray },
                    styler
                }
            }
        };
    }
    
    public void BlinkUserInput() {
        if (blinkFor == 0) {
            knownColor = styler.Foreground;
        }

        styler.Foreground = ConsoleColor.Red;
        blinkFor = BlinkTime;
    }
    
    public void Tick() {
        ConsoleManager.AdjustBufferSize();
        
        if (blinkFor > 0) {
            blinkFor--;
            styler.Foreground = UIHelpers.LerpColor(new(255, 0, 0), Color.White, 1 - (blinkFor / (float)BlinkTime));
        }
        else {
            styler.Foreground = knownColor;
        }
    }

    public void OnInput(InputEvent inputEvent) {
        ctrlBspace.OnInput(inputEvent); // TODO: move ctrlbspace out of here
        if(inputEvent.Handled) return;
        ((IInputListener)tbox).OnInput(inputEvent);
    }
}
