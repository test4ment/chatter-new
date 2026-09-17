using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Controls;
using ConsoleGUI.Data;
using ConsoleProgressbar;
using System.Diagnostics;
using ConsoleGUI.Space;

ConsoleManager.Setup();
    
var labels = new[] { "download", "upload", "render", "syncing" };
var phases = new[] { 3.6, 1.1, 0.8, 0.45 };
var colors = new[]
{
    new Color(0, 200, 90),
    new Color(60, 120, 255),
    new Color(255, 165, 0),
    new Color(230, 60, 90)
};

var bars = new ProgressBar[labels.Length];
var rows = new IControl[labels.Length];

for (var i = 0; i < labels.Length; i++)
{
    bars[i] = new ProgressBar
    {
        BarWidth = i + 1,
        FillColor = colors[i],
        EmptyColor = colors[i].Mix(Color.Black, 0.4f),
        LabelColor = new Color(235, 235, 235),
        ShowLabel = true,
    };

    rows[i] = new Boundary() {
        Content = new HorizontalStackPanel {
            Children = [
                new Box() {
                    Content = new Style {
                        Foreground = new Color(150, 180, 220),
                        Content = new TextBlock { Text = $"{labels[i],10} " }
                    },
                    VerticalContentPlacement = Box.VerticalPlacement.Center
                },
                new Box() {
                    Content = new Margin() {
                        Content = (i & 1) == 0 ? bars[i] : new Border() {
                        Content = bars[i]
                    },
                        Offset = new Offset(0, 0, 2, 0)
                    },
                    VerticalContentPlacement = Box.VerticalPlacement.Center,
                    HorizontalContentPlacement = Box.HorizontalPlacement.Stretch
                },
            ]
        },
        MaxHeight = i + 3 + (i & 1) * 2
    };
}

ConsoleManager.Content = new VerticalStackPanel {
    Children = new IControl[] {
        new Box {
            HorizontalContentPlacement = Box.HorizontalPlacement.Center,
            Content = new TextBlock {
                Text = "ConsoleGUI progress bars",
                Color = new Color(220, 220, 220)
            }
        },
        new HorizontalSeparator(),
        new VerticalStackPanel { Children = rows },
        new HorizontalSeparator(),
        new TextBlock {
            Text = "Press Ctrl+C to exit",
            Color = new Color(120, 120, 120)
        }
    }
};

var stopwatch = Stopwatch.StartNew();

while (true)
{
    Thread.Sleep(16);

    var time = stopwatch.Elapsed.TotalSeconds;
    for (var i = 0; i < bars.Length; i++)
    {
        var value = (Math.Sin(time * phases[i]) + 1) / 2;
        bars[i].Progress = value;
    }

    ConsoleManager.AdjustBufferSize();
}