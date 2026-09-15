using ConsoleGUI.Input;

namespace chatter_new_console;

public class EnterInput: IInputListener {
    public event Action? OnEnter;
    
    public void OnInput(InputEvent inputEvent) {
        if (inputEvent.Key is { Key: ConsoleKey.Enter, Modifiers: ConsoleModifiers.None }) {
            inputEvent.Handled = true;
            OnEnter?.Invoke();
        }
    }
}
