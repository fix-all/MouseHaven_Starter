using MouseHaven.Core;

namespace MouseHaven.Windows;

internal static class PlacementMath
{
    // All transitions use the same foot/root coordinate, then place the small actor surface around it.
    public static Point ActorTopLeft(Point2 screenRoot, Point rootInCanvas, float dpiScale) => new(
        (int)Math.Round(screenRoot.X - rootInCanvas.X * dpiScale),
        (int)Math.Round(screenRoot.Y - rootInCanvas.Y * dpiScale));

    // The nibbling mouse stops with its mouth at the near edge of one persistent folder prop.
    public static Point2 NibbleRoot(Point2 folderCenter, Facing facing, Point2 mouthOffset,
        float actorDpiScale, float folderDpiScale)
    {
        var edgeX = (facing == Facing.Right ? -19 : 19) * folderDpiScale;
        return new Point2(folderCenter.X + edgeX - mouthOffset.X * actorDpiScale,
            folderCenter.Y - mouthOffset.Y * actorDpiScale);
    }
}
