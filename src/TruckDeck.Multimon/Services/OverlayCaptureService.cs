using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TruckDeck.Multimon.Services
{
    /// <summary>
    /// Captures pixels from the ETS2/ATS game window (DX borderless) for overlay panes.
    /// </summary>
    public static class OverlayCaptureService
    {
        const uint PwRenderFullContent = 0x00000002;

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out NativeRect rect);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint point);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h,
            IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        const int SrcCopy = 0x00CC0020;

        [StructLayout(LayoutKind.Sequential)]
        struct NativeRect
        {
            public int Left, Top, Right, Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct NativePoint
        {
            public int X, Y;
        }

        public static bool TryGetClientScreenBounds(IntPtr hwnd, out Rectangle screenBounds)
        {
            screenBounds = Rectangle.Empty;
            if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out var client))
                return false;

            var topLeft = new NativePoint { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref topLeft))
                return false;

            screenBounds = new Rectangle(topLeft.X, topLeft.Y, client.Width, client.Height);
            return screenBounds.Width > 0 && screenBounds.Height > 0;
        }

        /// <summary>
        /// Captures the full game client area. Prefer PrintWindow (DX-friendly); fall back to BitBlt.
        /// Caller owns the returned bitmap.
        /// </summary>
        public static Bitmap CaptureClient(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out var client) ||
                client.Width < 64 || client.Height < 64)
                return null;

            var bmp = new Bitmap(client.Width, client.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                var hdc = g.GetHdc();
                try
                {
                    if (!PrintWindow(hwnd, hdc, PwRenderFullContent))
                    {
                        // Fallback GDI blit (often black for DX — keep for windowed edge cases)
                        var windowDc = GetWindowDC(hwnd);
                        if (windowDc != IntPtr.Zero)
                        {
                            BitBlt(hdc, 0, 0, client.Width, client.Height, windowDc, 0, 0, SrcCopy);
                            ReleaseDC(hwnd, windowDc);
                        }
                    }
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }

            return bmp;
        }

        public static Bitmap Crop(Bitmap source, Rectangle clientRelative)
        {
            if (source == null || clientRelative.Width < 2 || clientRelative.Height < 2)
                return null;

            var bounds = Rectangle.Intersect(
                new Rectangle(0, 0, source.Width, source.Height),
                clientRelative);
            if (bounds.Width < 2 || bounds.Height < 2)
                return null;

            var crop = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(crop))
                g.DrawImage(source, new Rectangle(0, 0, crop.Width, crop.Height), bounds, GraphicsUnit.Pixel);
            return crop;
        }

        /// <summary>
        /// Maps an absolute desktop/pixel region to game-client-relative coords.
        /// </summary>
        public static Rectangle ToClientRelative(Rectangle absolutePixelBounds, Rectangle gameClientScreenBounds)
        {
            return new Rectangle(
                absolutePixelBounds.X - gameClientScreenBounds.X,
                absolutePixelBounds.Y - gameClientScreenBounds.Y,
                absolutePixelBounds.Width,
                absolutePixelBounds.Height);
        }
    }
}
