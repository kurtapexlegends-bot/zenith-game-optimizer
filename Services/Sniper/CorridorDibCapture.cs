using System;
using System.Runtime.InteropServices;

namespace ZenithOptimizer.Services.Sniper
{
    /// <summary>
    /// Persistent 32-bit top-down DIB section capture engine for optical corridor scanning.
    /// Replaces hundreds of per-pixel GDI calls with a single 1-shot BitBlt (<0.05ms) per scan tick.
    /// </summary>
    public class CorridorDibCapture : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        private const int DIB_RGB_COLORS = 0;
        private const uint SRCCOPY = 0x00CC0020;

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        private IntPtr _memDc = IntPtr.Zero;
        private IntPtr _hBitmap = IntPtr.Zero;
        private IntPtr _oldBitmap = IntPtr.Zero;
        private IntPtr _pBits = IntPtr.Zero;
        private int _width;
        private int _height;
        private int[] _rawBuffer;
        private uint[] _buffer;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public uint[] Buffer { get { return _buffer; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public IntPtr BitsPointer { get { return _pBits; } }
        public IntPtr MemoryDc { get { return _memDc; } }
        public uint this[int x, int y] { get { return GetPixel(x, y); } }

        public CorridorDibCapture(int width = 260, int height = 32)
        {
            AllocateBuffers(Math.Max(16, width), Math.Max(4, height));
        }

        private void AllocateBuffers(int width, int height)
        {
            _width = width;
            _height = height;
            _rawBuffer = new int[_width * _height];
            _buffer = new uint[_width * _height];

            _memDc = CreateCompatibleDC(IntPtr.Zero);
            if (_memDc == IntPtr.Zero) return;

            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = _width;
            bmi.bmiHeader.biHeight = -_height; // Top-down
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0; // BI_RGB

            _hBitmap = CreateDIBSection(_memDc, ref bmi, DIB_RGB_COLORS, out _pBits, IntPtr.Zero, 0);
            if (_hBitmap != IntPtr.Zero)
            {
                _oldBitmap = SelectObject(_memDc, _hBitmap);
            }
        }

        public void Reallocate(int width, int height)
        {
            if (width == _width && height == _height) return;
            lock (_lock)
            {
                FreeHandles();
                AllocateBuffers(Math.Max(16, width), Math.Max(4, height));
            }
        }

        public bool Capture(IntPtr hwnd, int screenCenterX, int screenCenterY)
        {
            if (_disposed) return false;
            lock (_lock)
            {
                if (_memDc == IntPtr.Zero || _pBits == IntPtr.Zero) return false;

                IntPtr srcDc = IntPtr.Zero;
                bool isTargetHwnd = false;
                try
                {
                    int srcX = screenCenterX - _width / 2;
                    int srcY = screenCenterY - _height / 2;

                    if (hwnd != IntPtr.Zero)
                    {
                        POINT pt = new POINT { X = srcX, Y = srcY };
                        if (ScreenToClient(hwnd, ref pt))
                        {
                            srcX = pt.X;
                            srcY = pt.Y;
                        }
                        srcDc = GetDC(hwnd);
                        if (srcDc != IntPtr.Zero) isTargetHwnd = true;
                    }

                    if (srcDc == IntPtr.Zero)
                    {
                        srcDc = GetDC(IntPtr.Zero);
                        srcX = screenCenterX - _width / 2;
                        srcY = screenCenterY - _height / 2;
                    }

                    if (srcDc == IntPtr.Zero) return false;

                    BitBlt(_memDc, 0, 0, _width, _height, srcDc, srcX, srcY, SRCCOPY);
                }
                finally
                {
                    if (srcDc != IntPtr.Zero)
                    {
                        ReleaseDC(isTargetHwnd ? hwnd : IntPtr.Zero, srcDc);
                    }
                }

                // Copy native DIB memory to managed buffer and normalize BGRA -> COLORREF (0x00BBGGRR)
                int len = _width * _height;
                Marshal.Copy(_pBits, _rawBuffer, 0, len);
                for (int i = 0; i < len; i++)
                {
                    uint c = (uint)_rawBuffer[i];
                    _buffer[i] = ((c & 0x000000FF) << 16) | (c & 0x0000FF00) | ((c & 0x00FF0000) >> 16) | (c & 0xFF000000);
                }

                return true;
            }
        }

        public uint GetPixel(int relX, int relY)
        {
            if (relX < 0 || relX >= _width || relY < 0 || relY >= _height) return 0xFFFFFFFF;
            return _buffer[relY * _width + relX];
        }

        private void FreeHandles()
        {
            if (_memDc != IntPtr.Zero)
            {
                if (_oldBitmap != IntPtr.Zero)
                {
                    SelectObject(_memDc, _oldBitmap);
                    _oldBitmap = IntPtr.Zero;
                }
                DeleteDC(_memDc);
                _memDc = IntPtr.Zero;
            }

            if (_hBitmap != IntPtr.Zero)
            {
                DeleteObject(_hBitmap);
                _hBitmap = IntPtr.Zero;
            }

            _pBits = IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            lock (_lock)
            {
                FreeHandles();
            }
        }

        ~CorridorDibCapture()
        {
            Dispose(false);
        }
    }
}
