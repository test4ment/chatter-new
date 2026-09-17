namespace chatter_new_console;

public class State(Action<string, State> initial) {
    private Action<string, State> current = initial;
    
    public void Handle(string input) {
        current(input, this);
    }
    
    public void ChangeState(Action<string, State> @new) {
        current = @new;
    }
}
