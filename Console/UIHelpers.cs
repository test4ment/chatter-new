using ConsoleGUI.Data;

namespace chatter_new_console;

public static class UIHelpers {
    public static Color LerpColor(this Color from, Color to, float t) {
        var r = from.Red + (to.Red - from.Red) * t;
        var g = from.Green + (to.Green - from.Green) * t;
        var b = from.Blue + (to.Blue - from.Blue) * t;
        return new Color((byte)r, (byte)g, (byte)b);
    }
}
