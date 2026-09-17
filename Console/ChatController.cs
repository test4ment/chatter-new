using ConsoleGUI;
using ConsoleGUI.Controls;
using ConsoleGUI.Input;

namespace chatter_new_console;

internal class ChatController {
    private readonly IReadOnlyCollection<IInputListener> inputListeners;
    private readonly VerticalStackPanel msgCollection;
    private readonly TextBoxFacade userInput;
    private readonly VerticalScrollPanel scrollPanel;
    private readonly Action? tick;
    
    public Action<string>? OnEnter;
    public string Text {
        get => userInput.Text;
        set => userInput.Text = value;
    }
    // public Action<string>? OnTextboxUpdate; // TODO: commands window
    
    private ChatController(IReadOnlyCollection<IInputListener> inputListeners, VerticalStackPanel msgCollection,
        TextBoxFacade userInput, VerticalScrollPanel scrollPanel, Action? tick = null) {
        this.inputListeners = inputListeners;
        this.msgCollection = msgCollection;
        this.userInput = userInput;
        this.scrollPanel = scrollPanel;
        this.tick = tick;
    }

    public static ChatController Create(IReadOnlyCollection<IInputListener> inputListeners, 
        VerticalStackPanel msgCollection, TextBoxFacade userInput, VerticalScrollPanel scrollPanel, Action? tick = null) {
        var enterHandler = new EnterInput();
        var ctrlBspace = new CtrlBspace(userInput.Input);
        var chatController =
            new ChatController([enterHandler, ctrlBspace, ..inputListeners], msgCollection, userInput, 
                scrollPanel, tick);
        enterHandler.OnEnter += () => chatController.OnEnter?.Invoke(chatController.Text);

        return chatController;
    }

    public void Tick() {
        ConsoleManager.AdjustBufferSize();
        ConsoleManager.ReadInput(inputListeners);
        
        tick?.Invoke();
    }

    public void ScrollToBotton() {
        scrollPanel.Top = int.MaxValue;
    }

    public void AddMessage(string name, string text) {
        msgCollection.Add(ConsoleUI.CreateMessage(name, text));
    }
    
    public void AddSysMessage(string text) {
        msgCollection.Add(ConsoleUI.CreateSysMessage(text));
    }
}