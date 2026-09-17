using ConsoleGUI.Common;
using ConsoleGUI.Data;
using ConsoleGUI.Space;

namespace ConsoleProgressbar;

public sealed class ProgressBar : Control {
    private const char FillSymbol = '█';
    private const char EmptySymbol = '░';
    // TODO: custom symbols
    // TODO: spinner

    private int barWidth = 1;
    private double progress;
    private bool showLabel = true;
    private Color? fillColor = new Color(230, 230, 230);
    private Color? emptyColor = new Color(90, 90, 90);
    private Color? labelColor = new Color(230, 230, 230);

    public double Progress {
        get => progress;
        set {
            progress = Math.Clamp(value, 0.0, 1.0);
            Redraw();
        }
    }

    public int BarWidth {
        get => barWidth;
        set {
            barWidth = Math.Max(1, value);
            Initialize();
            Redraw();
        }
    }

    public bool ShowLabel {
        get => showLabel;
        set {
            showLabel = value;
            Redraw();
        }
    }

    public Color? FillColor {
        get => fillColor;
        set {
            fillColor = value;
            Redraw();
        }
    }

    public Color? EmptyColor {
        get => emptyColor;
        set {
            emptyColor = value;
            Redraw();
        }
    }

    public Color? LabelColor {
        get => labelColor;
        set {
            labelColor = value;
            Redraw();
        }
    }

    protected override void Initialize() {
        var width = Math.Max(MinSize.Width, Size.MaxLength);

        Resize(new Size(width, barWidth));
    }

    public override Cell this[Position position] {
        get {
            if (Size.Width <= 0 || Size.Height <= 0 || position.Y >= barWidth || Size.Width <= 0)
                return Character.Empty;

            if (showLabel && position.Y == barWidth / 2) {
                var text = $"{progress * 100:0}%"; 
                // TODO: custom format
                // TODO: text location
                // TODO: custom description
                // TODO: ETA
                var startX = Math.Max(0, Size.Width - text.Length - 1);
                var index = position.X - startX;
                if (index >= 0 && index < text.Length)
                    return new Character(text[index], labelColor, null);
            }

            var filled = (int)Math.Round(progress * Size.Width);
            var symbol = position.X < filled ? FillSymbol : EmptySymbol;
            var color = position.X < filled ? fillColor : emptyColor;
            return new Character(symbol, color, null);
        }
    }
}