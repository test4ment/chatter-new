using ConsoleGUI.Controls;
using ConsoleGUI.Input;

namespace chatter_new_console;

internal class CtrlBspace(TextBox tbox): IInputListener {
    public void OnInput(InputEvent inputEvent) {
        if (inputEvent.Key.Key == ConsoleKey.Backspace && inputEvent.Key.Modifiers.HasFlag(ConsoleModifiers.Control)) {
            var rpos = tbox.CaretEnd;
            var lpos = tbox.Text[..rpos].LastIndexOf(' ');
            if (lpos == -1) { lpos = 0; }

            tbox.Text = tbox.Text[..lpos] + tbox.Text[rpos..];
            tbox.Caret = rpos;
            inputEvent.Handled = true;
        }
    }
}