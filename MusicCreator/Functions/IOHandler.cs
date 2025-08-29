using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicCreator.Functions
{
    public static class IOHandler
    {
        public static IntPtr _hwnd;

       

        /// <summary>
        /// Simulated Shift down
        /// </summary>
        public static void ShiftDown()
        {
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = 0 }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
        /// <summary>
        /// Simulates Shift up event
        /// </summary>
        public static void ShiftUp()
        {
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KEYEVENTF_KEYUP }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }


        /// <summary>
        /// Puts focus on the "next" window
        /// </summary>
        /// <param name="targetHwnd"></param>
        public static void FocusWindowRobust(IntPtr targetHwnd)
        {
            // 1) Show utan aktivering (om fönstret var minimerat)
            ShowWindowAsync(targetHwnd, SW_SHOWNORMAL);

            // 2) Alt-tricket — Windows låter program sätta foreground om Alt är “aktivt”
            SendAltNudge();

            // 3) Koppla trådar temporärt för att få SetForegroundWindow att bita
            uint targetTid = GetWindowThreadProcessId(targetHwnd, out _);
            uint thisTid = GetCurrentThreadId();

            bool attached = false;
            try
            {
                attached = AttachThreadInput(thisTid, targetTid, true);
                BringWindowToTop(targetHwnd);
                SetForegroundWindow(targetHwnd);
            }
            finally
            {
                if (attached) AttachThreadInput(thisTid, targetTid, false);
            }
        }
        /// <summary>
        /// Scroll up and down. + for moving up, - for moving down
        /// </summary>
        /// <param name="clicks"></param>
        public static void ScrollVertical(int clicks)
        {
            int delta = clicks * WHEEL_DELTA;
            var inputs = new INPUT[]
            {
        new INPUT { type = INPUT_MOUSE, U = new InputUnion {
            mi = new MOUSEINPUT { mouseData = (uint)delta, dwFlags = MOUSEEVENTF_WHEEL }
        } }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(200);
        }
        /// <summary>
        /// Scrolls left or right. + for moving left, - for moving right
        /// </summary>
        /// <param name="clicks"></param>
        public static void ScrollHorizontal(int clicks)
        {
            int delta = clicks * WHEEL_DELTA; // >0 = höger, <0 = vänster
            var inputs = new INPUT[]
            {
        new INPUT { type = INPUT_MOUSE, U = new InputUnion {
            mi = new MOUSEINPUT { mouseData = (uint)delta, dwFlags = MOUSEEVENTF_HWHEEL }
        } }
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        /// <summary>
        /// Scrolls left or right. + for moving left, - for moving right
        /// </summary>
        /// <param name="clicks"></param>
        public static async Task ShiftScrollAsync(int clicks, int stepDelayMs = 50)
        {
            int steps = Math.Abs(clicks);
            if (steps == 0) return;

            int delta = Math.Sign(clicks) * WHEEL_DELTA;              // +120 eller −120
            uint udelta = unchecked((uint)delta);                     // viktigt för negativa

            // SHIFT down
            var shiftDown = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = 0 }
                }
            };
            SendInput(1, new[] { shiftDown }, Marshal.SizeOf(typeof(INPUT)));
            await Task.Delay(10);

            // Wheel stegvis
            var wheel = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT { mouseData = udelta, dwFlags = MOUSEEVENTF_WHEEL }
                }
            };

            for (int i = 0; i < steps; i++)
            {
                SendInput(1, new[] { wheel }, Marshal.SizeOf(typeof(INPUT)));
                await Task.Delay(stepDelayMs);                        // ge appen tid; testa 40–80 ms
            }

            // SHIFT up
            var shiftUp = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KEYEVENTF_KEYUP }
                }
            };
            await Task.Delay(10);
            SendInput(1, new[] { shiftUp }, Marshal.SizeOf(typeof(INPUT)));
        }

        // —— Always-on-top (TopMost) ——
        public static void SetAlwaysOnTop(bool enable)
        {
            SetWindowPos(_hwnd,
                enable ? HWND_TOPMOST : HWND_NOTOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        // —— Click-through på/av ——
        public static void EnableClickThrough()
        {
            var ex = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
            ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, (IntPtr)ex);
        }

        public static void DisableClickThrough()
        {
            var ex = GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
            ex &= ~WS_EX_TRANSPARENT;
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, (IntPtr)ex);
        }

        public static void ApplyStyleChanges()
        {
            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
        // NEW: liten ALT-nudge via SendInput
        public static void SendAltNudge()
        {
            INPUT[] inputs = new INPUT[]
            {
                new INPUT{ type = 1, U = new InputUnion{ ki = new KEYBDINPUT{ wVk = VK_MENU } } },
                new INPUT{ type = 1, U = new InputUnion{ ki = new KEYBDINPUT{ wVk = VK_MENU, dwFlags = KEYEVENTF_KEYUP } } },
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // NEW: ABSOLUTE-koordinater över hela virtuella skrivbordet
        public static (int ax, int ay) ToAbsolute(Point screenPoint)
        {
            int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            double nx = (screenPoint.X - vx) / vw; // 0..1
            double ny = (screenPoint.Y - vy) / vh; // 0..1

            int ax = (int)Math.Round(nx * 65535.0);
            int ay = (int)Math.Round(ny * 65535.0);
            return (ax, ay);
        }

        public static void SendAbsoluteClick(Point screenPoint)
        {
            var (ax, ay) = ToAbsolute(screenPoint);

            INPUT[] inputs = new INPUT[]
            {
                // flytta absolut (många spel kräver faktiskt move)
                new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dx = (int)ax, dy = (int)ay, dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE } } },
                new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } } },
                new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } } },
            };
            INPUT[] inputs2 = new INPUT[]
            {
               new INPUT { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } } },
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(200);
            SendInput((uint)inputs2.Length, inputs2, Marshal.SizeOf(typeof(INPUT)));

        }

        #region Win32
        const int WHEEL_DELTA = 120;

        const uint INPUT_MOUSE = 0;
        const uint INPUT_KEYBOARD = 1;

        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;

        const ushort VK_SHIFT = 0x10;



        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;



        // Show/Foreground
       
        [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOWNORMAL = 1;

        // Thread attach
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        // Window styles
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);

        // Input
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT { public uint type; public InputUnion U; }

        [StructLayout(LayoutKind.Explicit)]
        public  struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki; // NEW: keyboard för Alt-nudge
        }

        [StructLayout(LayoutKind.Sequential)]
        public  struct MOUSEINPUT
        {
            public int dx; public int dy; public uint mouseData;
            public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public  struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Mouse flags
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Keyboard
        public const ushort VK_MENU = 0x12; // Alt
        public const uint KEYEVENTF_KEYUP = 0x0002;

        // Virtual screen metrics
        [DllImport("user32.dll")] public static extern int GetSystemMetrics(int nIndex);
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        // SetWindowPos
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        #endregion
    }
}
