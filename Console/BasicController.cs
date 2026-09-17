using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

namespace chatter_new_console;

internal class BasicController {
    private const int BlinkTime = 16; 
        
    private readonly EnterInput enter = new();
    private readonly IReadOnlyCollection<IInputListener> inputListeners;
    private readonly VerticalStackPanel textPan;
    private readonly TextBoxFacade userInput;
    
    private int blinkFor = 0;
    private Color? knownColor;
    
    public event Action<string>? OnEnter;

    private BasicController(IReadOnlyCollection<IInputListener> inputListeners,
        VerticalStackPanel textPan,
        TextBoxFacade userInput) {
        this.inputListeners = inputListeners;
        this.textPan = textPan;
        this.userInput = userInput;
        knownColor = userInput.Style.Foreground;
    }

    public static BasicController Create(IReadOnlyCollection<IInputListener> inputListeners,
        VerticalStackPanel textPan,
        TextBoxFacade userInput) {
        var ctrlBspace = new CtrlBspace(userInput.Input);
        var i = new BasicController([ctrlBspace, ..inputListeners], textPan, userInput);
        i.enter.OnEnter += i.ReadLine;
        
        return i;
    }

    public void Tick() {
        ConsoleManager.AdjustBufferSize();
        ConsoleManager.ReadInput([enter, ..inputListeners]);
        
        if (blinkFor > 0) {
            blinkFor--;
            userInput.Style.Foreground = LerpColor(new(255, 0, 0), Color.White, 1 - (blinkFor / (float)BlinkTime));
        }
        else {
            userInput.Style.Foreground = knownColor;
        }
    }

    public void SetText(string text) {
        var en = text.Split("\n").Select(Control (s) => 
            new WrapPanel() {
                Content = new TextBlock() { Text = s }
            });
        
        textPan.Children = en;
    }

    public void SetInputText(string text) {
        userInput.Text = text;
    }
    
    public void BlinkUserInput() {
        if (blinkFor == 0) {
            knownColor = userInput.Style.Foreground;
        }

        userInput.Style.Foreground = ConsoleColor.Red;
        blinkFor = BlinkTime;
    }

    public void AddMsg(string msg) {
        textPan.Add(new Margin() {
            Offset = new Offset(0, 1, 0, 0),
            Content = new WrapPanel() {
                Content = new TextBlock() {
                    Text = msg, Color = new Color(0xFF, 0xEE, 0x8C)
                }
            },
        });
    }
    
    private void ReadLine() {
        OnEnter?.Invoke(userInput.Text);
    }

    public static Color LerpColor(Color from, Color to, float t) {
        var r = from.Red + (to.Red - from.Red) * t;
        var g = from.Green + (to.Green - from.Green) * t;
        var b = from.Blue + (to.Blue - from.Blue) * t;
        return new Color((byte)r, (byte)g, (byte)b);
    }
}