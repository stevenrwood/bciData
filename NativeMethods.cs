using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace bciData
{
    /// <summary> Represents an RGB color. </summary>
    public class Color
    {
        /// <summary> Creates a new Color from rgb. </summary>
        public Color(int r, int g, int b)
        {
            R = (uint)r;
            G = (uint)g;
            B = (uint)b;
        }

        /// <summary> Red component. </summary>
        public uint R { get; set; }

        /// <summary> Green component. </summary>
        public uint G { get; set; }

        /// <summary> Blue component. </summary>
        public uint B { get; set; }
    }

    public class NativeMethods
    {
        #region Signatures

        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out POINT vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy,
            int wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DrawMenuBar(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In][Out] ref Rect rect,
            [MarshalAs(UnmanagedType.U4)] int cPoints);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_ERROR_HANDLE = -12;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern void SetStdHandle(int nStdHandle, IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        public const uint GENERIC_WRITE = 0x40000000;
        public const uint GENERIC_READ = 0x80000000;
        public const int FILE_SHARE_READ = 0x00000001;
        public const int FILE_SHARE_WRITE = 0x00000002;
        public const int OPEN_EXISTING = 3;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] int creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateConsoleScreenBuffer(
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr screenBufferDataf
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ReadConsoleOutput(
            SafeFileHandle hConsoleOutput,
            IntPtr lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WriteConsoleOutput(
            SafeFileHandle hConsoleOutput,
            IntPtr lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff; // default value if not specifing a process ID

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferInfoEx(IntPtr ConsoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ScrollConsoleScreenBuffer(IntPtr hConsoleOutput, ref SmallRect lpScrollRectangle,
            ref SmallRect lpClipRectangle, Coord dwDestinationOrigin, ref CharInfo lpFill);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Coord GetLargestConsoleWindowSize(IntPtr ConsoleOutput);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetCurrentConsoleFontEx(
            IntPtr ConsoleOutput,
            bool MaximumWindow,
            ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

        public const int TMPF_TRUETYPE = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCurrentConsoleFontEx(
            IntPtr ConsoleOutput,
            bool MaximumWindow,
            ref CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushConsoleInputBuffer(
            IntPtr hConsoleInput);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(
            IntPtr handle,
            uint timeout);

        [DllImport("Kernel32.DLL", EntryPoint = "ReadConsoleInputW", CallingConvention = CallingConvention.StdCall)]
        public static extern bool ReadConsoleInput(
            IntPtr hConsoleInput,
            [Out] InputRecord[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsRead);

        [Flags]
        public enum ConsoleMode : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,

            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        #endregion

        #region Structs

        // Basic
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct CharInfo
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColorRef
        {
            internal uint ColorDWORD;

            internal ColorRef(Color color)
            {
                ColorDWORD = color.R + (color.G << 8) + (color.B << 16);
            }

            internal ColorRef(uint r, uint g, uint b)
            {
                ColorDWORD = r + (g << 8) + (b << 16);
            }

            internal Color GetColor()
            {
                return new Color((int)(0x000000FFU & ColorDWORD),
                    (int)(0x0000FF00U & ColorDWORD) >> 8, (int)(0x00FF0000U & ColorDWORD) >> 16);
            }

            internal void SetColor(Color color)
            {
                ColorDWORD = color.R + (color.G << 8) + (color.B << 16);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public int cbSize;
            public Coord dwSize;
            public Coord dwCursorPosition;
            public short wAttributes;
            public SmallRect srWindow;
            public Coord dwMaximumWindowSize;

            public ushort wPopupAttributes;
            public bool bFullscreenSupported;

            internal ColorRef black;
            internal ColorRef darkBlue;
            internal ColorRef darkGreen;
            internal ColorRef darkCyan;
            internal ColorRef darkRed;
            internal ColorRef darkMagenta;
            internal ColorRef darkYellow;
            internal ColorRef gray;
            internal ColorRef darkGray;
            internal ColorRef blue;
            internal ColorRef green;
            internal ColorRef cyan;
            internal ColorRef red;
            internal ColorRef magenta;
            internal ColorRef yellow;
            internal ColorRef white;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CONSOLE_FONT_INFO_EX
        {
            public uint cbSize;
            public uint nFont;
            public Coord dwFontSize;
            public int FontFamily;
            public int FontWeight;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] // Edit sizeconst if the font name is too big
            public string FaceName;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputRecord
        {
            public const ushort KEY_EVENT = 0x0001,
                MOUSE_EVENT = 0x0002,
                WINDOW_BUFFER_SIZE_EVENT = 0x0004; //more

            [FieldOffset(0)] public ushort EventType;
            [FieldOffset(4)] public KeyEvent KeyEvent;
            [FieldOffset(4)] public MouseEvent MouseEvent;
            [FieldOffset(4)] public WindowSizeEvent WindowBufferSizeEvent;
        }

        public struct MouseEvent
        {
            public Coord dwMousePosition;

            public const uint FROM_LEFT_1ST_BUTTON_PRESSED = 0x0001,
                FROM_LEFT_2ND_BUTTON_PRESSED = 0x0004,
                FROM_LEFT_3RD_BUTTON_PRESSED = 0x0008,
                FROM_LEFT_4TH_BUTTON_PRESSED = 0x0010,
                RIGHTMOST_BUTTON_PRESSED = 0x0002;

            public uint dwButtonState;

            public const int CAPSLOCK_ON = 0x0080,
                ENHANCED_KEY = 0x0100,
                LEFT_ALT_PRESSED = 0x0002,
                LEFT_CTRL_PRESSED = 0x0008,
                NUMLOCK_ON = 0x0020,
                RIGHT_ALT_PRESSED = 0x0001,
                RIGHT_CTRL_PRESSED = 0x0004,
                SCROLLLOCK_ON = 0x0040,
                SHIFT_PRESSED = 0x0010;

            public uint dwControlKeyState;

            public const int DOUBLE_CLICK = 0x0002,
                MOUSE_HWHEELED = 0x0008,
                MOUSE_MOVED = 0x0001,
                MOUSE_WHEELED = 0x0004;

            public uint dwEventFlags;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct KeyEvent
        {
            [FieldOffset(0)] public bool bKeyDown;
            [FieldOffset(4)] public ushort wRepeatCount;
            [FieldOffset(6)] public ushort wVirtualKeyCode;
            [FieldOffset(8)] public ushort wVirtualScanCode;
            [FieldOffset(10)] public char UnicodeChar;
            [FieldOffset(10)] public byte AsciiChar;

            public const int CAPSLOCK_ON = 0x0080,
                ENHANCED_KEY = 0x0100,
                LEFT_ALT_PRESSED = 0x0002,
                LEFT_CTRL_PRESSED = 0x0008,
                NUMLOCK_ON = 0x0020,
                RIGHT_ALT_PRESSED = 0x0001,
                RIGHT_CTRL_PRESSED = 0x0004,
                SCROLLLOCK_ON = 0x0040,
                SHIFT_PRESSED = 0x0010;

            [FieldOffset(12)] public uint dwControlKeyState;
        }

        public struct WindowSizeEvent
        {
            public Coord dwSize;
        }

        #endregion
    }
}
