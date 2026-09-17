using ConsoleGUI;

namespace chatter_new_console;

internal sealed class Screen(IControl content, Action tick) {
    public IControl Content { get; } = content;
    public Action Tick { get; } = tick;
}

internal sealed class ScreenNavigator {
    private Screen? current;

    public void Show(Screen screen) {
        current = screen;
        ConsoleManager.Content = screen.Content;
        ConsoleManager.AdjustBufferSize();
    }

    public void Tick() {
        current?.Tick();
    }
}
