using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Draws the Thunderstore icon (256x256): the top-right corner of a minimap, a house centred in the icon and
// an arrow pointing out of the corner, spaced evenly between the frame and the roof.
public static class IconDraw
{
    static readonly Color Bg = Color.FromArgb(30, 37, 48);
    static readonly Color Frame = Color.FromArgb(216, 207, 181);
    static readonly Color White = Color.FromArgb(245, 245, 240);

    // Layout. Frame bars are 14 px thick and sit 24 px from the top and right edges. The house's visual
    // centre is ~0.085 * size above its anchor, so (128, 133) centres it in the icon.
    const float FrameX = 220, FrameY = 26, Bar = 14;
    const float HouseX = 128, HouseY = 133, HouseSize = 62;
    const float ArrowSize = 27;

    public static void Render(string path)
    {
        using (var bmp = new Bitmap(256, 256))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Bg);

            using (var frame = new SolidBrush(Frame))
            {
                g.FillPolygon(frame, RightBar());
                g.FillPolygon(frame, TopBar());
            }
            using (var white = new SolidBrush(White))
            {
                g.FillPolygon(white, Roof());
                g.FillPolygon(white, Body());
                g.FillPolygon(white, ArrowPoints(BalanceArrow(), ArrowSize));
            }
            using (var door = new SolidBrush(Bg))
                g.FillRectangle(door, HouseX - 0.17f * HouseSize, HouseY + 0.3f * HouseSize, 0.34f * HouseSize, 0.53f * HouseSize);

            bmp.Save(path, ImageFormat.Png);
        }
    }

    static PointF[] RightBar() { return Rect(FrameX - 2, FrameY - 2, Bar, 256 - FrameY + 2); }
    static PointF[] TopBar() { return Rect(0, FrameY - 2, FrameX + Bar - 2, Bar); }

    static PointF[] Roof()
    {
        float s = HouseSize;
        return new[] { new PointF(HouseX - s, HouseY - 0.08f * s), new PointF(HouseX, HouseY - s), new PointF(HouseX + s, HouseY - 0.08f * s) };
    }

    static PointF[] Body()
    {
        float s = HouseSize;
        return Rect(HouseX - 0.72f * s, HouseY - 0.12f * s, 1.44f * s, 0.95f * s);
    }

    static PointF[] Rect(float x, float y, float w, float h)
    {
        return new[] { new PointF(x, y), new PointF(x + w, y), new PointF(x + w, y + h), new PointF(x, y + h) };
    }

    // Triangle pointing up-right (45 degrees), centred on `c`.
    static PointF[] ArrowPoints(PointF c, float s)
    {
        float k = (float)Math.Sqrt(0.5);
        PointF[] local = { new PointF(s, 0), new PointF(-0.6f * s, 0.75f * s), new PointF(-0.6f * s, -0.75f * s) };
        var pts = new PointF[3];
        for (int i = 0; i < 3; i++)
            pts[i] = new PointF(c.X + (local[i].X - local[i].Y) * k, c.Y - (local[i].X + local[i].Y) * k);
        return pts;
    }

    // Slides the arrow along the diagonal out of the frame's inner corner until its gap to the frame
    // equals its gap to the house.
    static PointF BalanceArrow()
    {
        float cornerX = FrameX - 2, cornerY = FrameY + Bar - 2;
        PointF best = new PointF(cornerX, cornerY);
        double bestDiff = double.MaxValue;
        for (float t = 0; t < 120; t += 0.25f)
        {
            var c = new PointF(cornerX - t, cornerY + t);
            PointF[] arrow = ArrowPoints(c, ArrowSize);
            double toFrame = Math.Min(PolyDistance(arrow, RightBar()), PolyDistance(arrow, TopBar()));
            double toHouse = Math.Min(PolyDistance(arrow, Roof()), PolyDistance(arrow, Body()));
            double diff = Math.Abs(toFrame - toHouse);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = c;
            }
        }
        return best;
    }

    // Shortest distance between two non-overlapping convex polygons.
    static double PolyDistance(PointF[] a, PointF[] b)
    {
        double best = double.MaxValue;
        for (int i = 0; i < a.Length; i++)
            for (int j = 0; j < b.Length; j++)
            {
                best = Math.Min(best, PointSegment(a[i], b[j], b[(j + 1) % b.Length]));
                best = Math.Min(best, PointSegment(b[j], a[i], a[(i + 1) % a.Length]));
            }
        return best;
    }

    static double PointSegment(PointF p, PointF s0, PointF s1)
    {
        double dx = s1.X - s0.X, dy = s1.Y - s0.Y;
        double t = Math.Max(0, Math.Min(1, ((p.X - s0.X) * dx + (p.Y - s0.Y) * dy) / (dx * dx + dy * dy)));
        double ex = s0.X + t * dx - p.X, ey = s0.Y + t * dy - p.Y;
        return Math.Sqrt(ex * ex + ey * ey);
    }
}
