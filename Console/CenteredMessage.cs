using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

namespace chatter_new_console;

internal sealed class CenteredMessage(string text, Color? foreground = null) : Control {
    private List<string> lines = new();

    public override Cell this[Position position] {
        get {
            if (position.Y >= lines.Count) return Character.Empty;

            var line = lines[position.Y];
            var offset = Math.Max(0, (Size.Width - line.Length) / 2);
            if (position.X < offset || position.X - offset >= line.Length) return Character.Empty;

            return new Character(line[position.X - offset], foreground: foreground);
        }
    }

    protected override void Initialize() {
        using (Freeze()) {
            lines = ComputeLines();

            var width = MaxSize.Width > 0 ? MaxSize.Width : Size.Width;
            Resize(new Size(width, Math.Max(1, lines.Count)));
        }
    }

    private List<string> ComputeLines() {
        var result = new List<string>();
        var width = MaxSize.Width > 0 ? MaxSize.Width : Size.Width;

        foreach (var rawLine in text.Split('\n')) {
            if (rawLine.Length <= width) {
                result.Add(rawLine);
                continue;
            }

            var line = rawLine;
            while (line.Length > width) {
                var splitAt = line.LastIndexOf(' ', width);
                if (splitAt < 0) splitAt = width - 1;
                if (splitAt < 0) break;

                var part = line[..splitAt].TrimEnd();
                line = line[(splitAt + 1)..].TrimStart();

                if (part.Length > 0) result.Add(part);
            }

            result.Add(line);
        }
        
        return result;
    }
}
