using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MouseHaven.Windows;

// Scene props are separate from the mouse and are resampled only once at startup.
internal sealed class RasterAssets : IDisposable
{
    public Bitmap House { get; } = Load("MouseHaven.SceneHouse", new Rectangle(118, 107, 1205, 892), 128, 96);
    public Bitmap GardenBare { get; } = Load("MouseHaven.SceneGardenBare", new Rectangle(96, 330, 1765, 197), 110, 13);
    public Bitmap GardenCarrots { get; } = Load("MouseHaven.SceneGardenCarrots", new Rectangle(96, 267, 1765, 262), 110, 17);
    public Bitmap Flowers { get; } = Load("MouseHaven.SceneFlowers", new Rectangle(190, 260, 1359, 574), 78, 33);
    public Bitmap FolderNormal { get; } = Load("MouseHaven.PropIcons", new Rectangle(73, 479, 203, 175), 38, 33);

    private static Bitmap Load(string resourceName, Rectangle sourceRect, int width, int height)
    {
        using var stream = typeof(RasterAssets).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded art: {resourceName}");
        using var source = new Bitmap(stream);
        if (sourceRect.Left < 0 || sourceRect.Top < 0 ||
            sourceRect.Right > source.Width || sourceRect.Bottom > source.Height)
            throw new InvalidOperationException($"Invalid source rectangle: {resourceName}");
        var result = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(result))
        {
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(source, new Rectangle(0, 0, width, height), sourceRect, GraphicsUnit.Pixel);
        }
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            if (result.GetPixel(x, y).A < 20) result.SetPixel(x, y, Color.Transparent);
        return result;
    }

    public void Dispose()
    {
        House.Dispose();
        GardenBare.Dispose();
        GardenCarrots.Dispose();
        Flowers.Dispose();
        FolderNormal.Dispose();
    }
}
