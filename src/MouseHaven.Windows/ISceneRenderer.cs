using MouseHaven.Core;

namespace MouseHaven.Windows;

// Keeps scene drawing replaceable without changing window ownership or world behavior.
internal interface ISceneRenderer : IDisposable
{
    bool UsePixelArt { get; set; }
    int ExpandedZoom { get; set; }
    Size DesktopSurfaceSize { get; }
    Point DesktopRoot { get; }
    Point2 NibbleMouthOffset(Facing facing);
    Point2 EntryInHome(WorldState state, int width);
    void Home(Graphics graphics, WorldState state, int width, int height);
    void DesktopMouse(Graphics graphics, WorldState state);
    void Folder(Graphics graphics, WorldState state);
}
