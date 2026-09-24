using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MouseHaven.Core;

namespace MouseHaven.Windows;

internal enum SpriteFrameId
{
    HomeIdle, HomeWalkA, HomeWalkB, HomeWorkA, HomeWorkB,
    OutdoorWalkA, OutdoorWalkB, OutdoorRunA, OutdoorRunB,
    OutdoorNibbleA, OutdoorNibbleB, OutdoorStartled
}

internal readonly record struct FrameSpec(
    SpriteFrameId Id, string Resource, Size SheetSize, Rectangle SourceRect,
    PointF SourceRoot, float Scale, bool Backpack, PointF? MouthSource = null);

internal readonly record struct AnimationClip(SpriteFrameId[] Frames, double FrameSeconds, bool Loop = true)
{
    public SpriteFrameId At(double seconds)
    {
        var index = (int)Math.Max(0, Math.Floor(seconds / FrameSeconds));
        return Frames[Loop ? index % Frames.Length : Math.Min(index, Frames.Length - 1)];
    }
}

// One source-root convention: the mouse's feet are at (24, 40) on every cached canvas.
// Explicit source rectangles exclude the original run-tail pixels that crossed x=1330.
internal sealed class SpriteFrames : IDisposable
{
    public static readonly Size CanvasSize = new(48, 44);
    public static readonly Point RootInCanvas = new(24, 40);

    internal static readonly FrameSpec[] Specs =
    [
        new(SpriteFrameId.HomeIdle, "MouseHaven.SpritePreview", new(1774, 887), new(0, 0, 444, 444), new(230, 420), 0.081f, false),
        new(SpriteFrameId.HomeWalkA, "MouseHaven.SpritePreview", new(1774, 887), new(444, 0, 443, 444), new(674, 420), 0.081f, false),
        new(SpriteFrameId.HomeWalkB, "MouseHaven.SpritePreview", new(1774, 887), new(887, 0, 443, 444), new(1117, 420), 0.081f, false),
        new(SpriteFrameId.HomeWorkA, "MouseHaven.HomeWork", new(2172, 724), new(0, 0, 1086, 724), new(525, 617), 0.060f, false),
        new(SpriteFrameId.HomeWorkB, "MouseHaven.HomeWork", new(2172, 724), new(1086, 0, 1086, 724), new(1611, 624), 0.060f, false),
        new(SpriteFrameId.OutdoorWalkA, "MouseHaven.OutdoorMotion", new(1536, 1024), new(0, 0, 512, 512), new(280, 492), 0.078f, true),
        new(SpriteFrameId.OutdoorWalkB, "MouseHaven.OutdoorMotion", new(1536, 1024), new(512, 0, 512, 512), new(792, 491), 0.078f, true),
        new(SpriteFrameId.OutdoorRunA, "MouseHaven.OutdoorMotion", new(1536, 1024), new(1024, 0, 512, 512), new(1314, 448), 0.078f, true),
        new(SpriteFrameId.OutdoorRunB, "MouseHaven.OutdoorMotion", new(1536, 1024), new(0, 512, 512, 512), new(290, 943), 0.078f, true),
        new(SpriteFrameId.OutdoorNibbleA, "MouseHaven.OutdoorMotion", new(1536, 1024), new(512, 512, 512, 512), new(792, 947), 0.078f, true, new PointF(932, 770)),
        new(SpriteFrameId.OutdoorNibbleB, "MouseHaven.OutdoorMotion", new(1536, 1024), new(1024, 512, 512, 512), new(1304, 947), 0.078f, true, new PointF(1445, 770)),
        new(SpriteFrameId.OutdoorStartled, "MouseHaven.SpritePreview", new(1774, 887), new(887, 444, 403, 443), new(1117, 818), 0.081f, true)
    ];

    private static readonly AnimationClip HomeWalk = new([SpriteFrameId.HomeWalkA, SpriteFrameId.HomeWalkB], 0.16);
    private static readonly AnimationClip HomeWork = new([SpriteFrameId.HomeWorkA, SpriteFrameId.HomeWorkB], 0.30);
    private static readonly AnimationClip OutdoorWalk = new([SpriteFrameId.OutdoorWalkA, SpriteFrameId.OutdoorWalkB], 0.16);
    private static readonly AnimationClip OutdoorRun = new([SpriteFrameId.OutdoorRunA, SpriteFrameId.OutdoorRunB], 0.11);
    private static readonly AnimationClip OutdoorNibble = new([SpriteFrameId.OutdoorNibbleA, SpriteFrameId.OutdoorNibbleB], 0.25);

    private readonly Dictionary<SpriteFrameId, Bitmap> _right = [];
    private readonly Dictionary<SpriteFrameId, Bitmap> _left = [];

    public SpriteFrames()
    {
        var sheets = new Dictionary<string, Bitmap>();
        try
        {
            foreach (var spec in Specs)
            {
                if (!sheets.TryGetValue(spec.Resource, out var sheet))
                {
                    sheet = LoadSheet(spec.Resource, spec.SheetSize);
                    sheets.Add(spec.Resource, sheet);
                }
                ValidateSource(spec, sheet);
                var right = Rasterize(spec, sheet);
                _right.Add(spec.Id, right);
                var left = (Bitmap)right.Clone();
                left.RotateFlip(RotateFlipType.RotateNoneFlipX);
                _left.Add(spec.Id, left);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            foreach (var sheet in sheets.Values) sheet.Dispose();
        }
    }

    private static Bitmap LoadSheet(string resource, Size expected)
    {
        using var stream = typeof(SpriteFrames).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded character art: {resource}");
        using var original = new Bitmap(stream);
        if (original.Size != expected) throw new InvalidOperationException($"Unexpected image size: {resource}");
        return new Bitmap(original);
    }

    private static void ValidateSource(FrameSpec spec, Bitmap sheet)
    {
        var r = spec.SourceRect;
        if (r.Left < 0 || r.Top < 0 || r.Right > sheet.Width || r.Bottom > sheet.Height ||
            !r.Contains(Point.Round(spec.SourceRoot)))
            throw new InvalidOperationException($"Invalid source frame: {spec.Id}");
        // A visible edge means a neighboring pose or part of this pose was cut off.
        for (var x = r.Left; x < r.Right; x++)
            if (sheet.GetPixel(x, r.Top).A >= 20 || sheet.GetPixel(x, r.Bottom - 1).A >= 20)
                throw new InvalidOperationException($"Frame touches horizontal boundary: {spec.Id}");
        for (var y = r.Top; y < r.Bottom; y++)
            if (sheet.GetPixel(r.Left, y).A >= 20 || sheet.GetPixel(r.Right - 1, y).A >= 20)
                throw new InvalidOperationException($"Frame touches vertical boundary: {spec.Id}");
    }

    private static Bitmap Rasterize(FrameSpec spec, Bitmap sheet)
    {
        var frame = new Bitmap(CanvasSize.Width, CanvasSize.Height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(frame))
        {
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            var r = spec.SourceRect;
            var dest = new RectangleF(
                RootInCanvas.X - (spec.SourceRoot.X - r.Left) * spec.Scale,
                RootInCanvas.Y - (spec.SourceRoot.Y - r.Top) * spec.Scale,
                r.Width * spec.Scale, r.Height * spec.Scale);
            g.DrawImage(sheet, dest, r, GraphicsUnit.Pixel);
        }
        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
            if (frame.GetPixel(x, y).A < 20) frame.SetPixel(x, y, Color.Transparent);
        for (var x = 0; x < frame.Width; x++)
            if (frame.GetPixel(x, 0).A >= 20 || frame.GetPixel(x, frame.Height - 1).A >= 20)
                throw new InvalidOperationException($"Cached frame clipped vertically: {spec.Id}");
        for (var y = 0; y < frame.Height; y++)
            if (frame.GetPixel(0, y).A >= 20 || frame.GetPixel(frame.Width - 1, y).A >= 20)
                throw new InvalidOperationException($"Cached frame clipped horizontally: {spec.Id}");
        return frame;
    }

    internal static SpriteFrameId HomeFrame(MouseAction action, double seconds) => action switch
    {
        MouseAction.Work => HomeWork.At(seconds),
        MouseAction.Walk or MouseAction.ExitHome => HomeWalk.At(seconds),
        _ => SpriteFrameId.HomeIdle
    };

    internal static SpriteFrameId DesktopFrame(MouseAction action, double seconds) => action switch
    {
        MouseAction.Nibble => OutdoorNibble.At(seconds),
        MouseAction.Startled => SpriteFrameId.OutdoorStartled,
        MouseAction.ReturnHome => OutdoorRun.At(seconds),
        _ => OutdoorWalk.At(seconds)
    };

    public PointF NibbleMouthOffset
    {
        get
        {
            var spec = Array.Find(Specs, s => s.Id == SpriteFrameId.OutdoorNibbleA);
            var mouth = spec.MouthSource!.Value;
            return new PointF((mouth.X - spec.SourceRoot.X) * spec.Scale,
                (mouth.Y - spec.SourceRoot.Y) * spec.Scale);
        }
    }

    public void DrawHome(Graphics graphics, PointF root, MouseAction action, double seconds, Facing facing) =>
        Draw(graphics, HomeFrame(action, seconds), root, facing);

    public void DrawDesktop(Graphics graphics, PointF root, MouseAction action, double seconds, Facing facing) =>
        Draw(graphics, DesktopFrame(action, seconds), root, facing);

    private void Draw(Graphics graphics, SpriteFrameId id, PointF root, Facing facing)
    {
        var bitmap = facing == Facing.Right ? _right[id] : _left[id];
        var old = graphics.InterpolationMode;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.DrawImageUnscaled(bitmap, (int)Math.Round(root.X - RootInCanvas.X),
            (int)Math.Round(root.Y - RootInCanvas.Y));
        graphics.InterpolationMode = old;
    }

    internal Bitmap GetCachedFrame(SpriteFrameId id, Facing facing) =>
        facing == Facing.Right ? _right[id] : _left[id];

    public void Dispose()
    {
        foreach (var bitmap in _right.Values) bitmap.Dispose();
        foreach (var bitmap in _left.Values) bitmap.Dispose();
        _right.Clear();
        _left.Clear();
    }
}
