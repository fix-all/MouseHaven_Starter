using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MouseHaven.Windows;

// A small, per-pixel-alpha, non-activating surface. No full-screen overlay exists.
internal sealed class LayeredSurface : Form
{
    private const int WsExLayered = 0x00080000;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTransparent = 0x00000020;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;

    private readonly bool _clickThrough;
    private Bitmap? _buffer;
    public int LogicalWidth { get; private set; }
    public int LogicalHeight { get; private set; }
    public float DpiScale => DeviceDpi / 96f;
    public long DrawCount { get; private set; }

    public LayeredSurface(bool clickThrough = false)
    {
        _clickThrough = clickThrough;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var p = base.CreateParams;
            p.ExStyle |= WsExLayered | WsExNoActivate | WsExToolWindow;
            if (_clickThrough) p.ExStyle |= WsExTransparent;
            return p;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmMouseActivate)
        {
            m.Result = MaNoActivate;
            return;
        }
        base.WndProc(ref m);
    }

    public void SetLogicalSize(int width, int height)
    {
        LogicalWidth = width;
        LogicalHeight = height;
        ClientSize = new Size(Math.Max(1, (int)Math.Round(width * DpiScale)),
                              Math.Max(1, (int)Math.Round(height * DpiScale)));
        _buffer?.Dispose();
        _buffer = null;
    }

    public void Render(Action<Graphics> draw)
    {
        if (!IsHandleCreated || !Visible || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        if (_buffer is null || _buffer.Width != ClientSize.Width || _buffer.Height != ClientSize.Height)
        {
            _buffer?.Dispose();
            _buffer = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppPArgb);
        }
        using (var graphics = Graphics.FromImage(_buffer))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.ScaleTransform(DpiScale, DpiScale);
            draw(graphics);
        }
        Present(_buffer);
        DrawCount++;
    }

    private void Present(Bitmap bitmap)
    {
        var screen = Native.GetDC(nint.Zero);
        if (screen == nint.Zero) throw new System.ComponentModel.Win32Exception();
        nint memory = nint.Zero, hBitmap = nint.Zero, old = nint.Zero;
        try
        {
            memory = Native.CreateCompatibleDC(screen);
            if (memory == nint.Zero) throw new System.ComponentModel.Win32Exception();
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            old = Native.SelectObject(memory, hBitmap);
            var destination = new Native.POINT(Left, Top);
            var size = new Native.SIZE(bitmap.Width, bitmap.Height);
            var source = new Native.POINT(0, 0);
            var blend = new Native.BLENDFUNCTION(0, 0, 255, 1);
            if (!Native.UpdateLayeredWindow(Handle, screen, ref destination, ref size,
                    memory, ref source, 0, ref blend, 2))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (old != nint.Zero && memory != nint.Zero) Native.SelectObject(memory, old);
            if (hBitmap != nint.Zero) Native.DeleteObject(hBitmap);
            if (memory != nint.Zero) Native.DeleteDC(memory);
            Native.ReleaseDC(nint.Zero, screen);
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (LogicalWidth > 0) SetLogicalSize(LogicalWidth, LogicalHeight);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _buffer?.Dispose();
        base.Dispose(disposing);
    }
}

internal static partial class Native
{
    [StructLayout(LayoutKind.Sequential)] internal record struct POINT(int X, int Y);
    [StructLayout(LayoutKind.Sequential)] internal record struct SIZE(int Width, int Height);
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct BLENDFUNCTION(byte BlendOp, byte BlendFlags, byte SourceConstantAlpha, byte AlphaFormat);

    [LibraryImport("user32.dll")] internal static partial nint GetDC(nint hWnd);
    [LibraryImport("user32.dll")] internal static partial int ReleaseDC(nint hWnd, nint hDC);
    [LibraryImport("gdi32.dll")] internal static partial nint CreateCompatibleDC(nint hDC);
    [LibraryImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static partial bool DeleteDC(nint hDC);
    [LibraryImport("gdi32.dll")] internal static partial nint SelectObject(nint hDC, nint hObject);
    [LibraryImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static partial bool DeleteObject(nint hObject);
    [LibraryImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UpdateLayeredWindow(nint hwnd, nint hdcDst, ref POINT pptDst,
        ref SIZE psize, nint hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int flags);
}
