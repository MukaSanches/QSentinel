using System.Drawing.Drawing2D;

namespace QSentinel;

public sealed class PerformanceChart : Control
{
    private readonly Queue<double> points = new();

    public string ChartTitle { get; set; } = "";
    public string Suffix { get; set; } = "";
    public double FixedMaximum { get; set; }

    public Color Accent { get; set; } =
        Color.FromArgb(115, 103, 255);

    public PerformanceChart()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(18, 24, 37);
        ForeColor = Color.FromArgb(240, 243, 249);
        Dock = DockStyle.Fill;
        Margin = new Padding(6);
    }

    public void AddPoint(double value)
    {
        points.Enqueue(Math.Max(0, value));

        while (points.Count > 60)
            points.Dequeue();

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode =
            SmoothingMode.AntiAlias;

        using var border =
            new Pen(Color.FromArgb(38, 48, 68));

        e.Graphics.DrawRectangle(
            border,
            0,
            0,
            Width - 1,
            Height - 1);

        using var titleFont =
            new Font("Segoe UI Semibold", 9);

        using var valueFont =
            new Font("Segoe UI Semibold", 18);

        using var muted =
            new SolidBrush(Color.FromArgb(133, 146, 170));

        using var primary =
            new SolidBrush(ForeColor);

        e.Graphics.DrawString(
            ChartTitle.ToUpperInvariant(),
            titleFont,
            muted,
            16,
            13);

        double current =
            points.Count == 0
                ? 0
                : points.Last();

        e.Graphics.DrawString(
            $"{current:N1}{Suffix}",
            valueFont,
            primary,
            15,
            36);

        var chart = new RectangleF(
            16,
            79,
            Math.Max(10, Width - 32),
            Math.Max(10, Height - 95));

        using var grid =
            new Pen(Color.FromArgb(29, 38, 55));

        for (int i = 0; i <= 3; i++)
        {
            float y =
                chart.Top +
                chart.Height *
                i /
                3f;

            e.Graphics.DrawLine(
                grid,
                chart.Left,
                y,
                chart.Right,
                y);
        }

        if (points.Count < 2)
            return;

        double max =
            FixedMaximum > 0
                ? FixedMaximum
                : Math.Max(
                    10,
                    points.Max() * 1.15);

        var values = points.ToArray();
        var graph = new PointF[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            float x =
                chart.Left +
                chart.Width *
                i /
                Math.Max(1, values.Length - 1);

            float y =
                chart.Bottom -
                (float)(
                    Math.Min(values[i], max) /
                    max *
                    chart.Height);

            graph[i] = new PointF(x, y);
        }

        using var line = new Pen(Accent, 2.4f)
        {
            LineJoin = LineJoin.Round
        };

        e.Graphics.DrawLines(line, graph);
    }
}
