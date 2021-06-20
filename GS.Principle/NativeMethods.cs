/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GS.Principles
{
    public static class NativeMethods
    {
        #region Multimedia Timer

        // Gets timer capabilities.
        [DllImport("winmm.dll", EntryPoint = "timeGetDevCaps")]
        internal static extern int TimeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);

        // Creates and starts the timer.
        [DllImport("winmm.dll", EntryPoint = "timeSetEvent")]
        internal static extern int TimeSetEvent(int delay, int resolution, MediaTimer.TimeProc proc, ref int user, int mode);

        // Stops and destroys the timer.
        [DllImport("winmm.dll", EntryPoint = "timeKillEvent")]
        internal static extern int TimeKillEvent(int id);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetLocalTime(ref Time.SYSTEMTIME time);

        #endregion

        #region Time

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        internal static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        [DllImport("Kernel32.dll")]
        internal static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        internal static extern bool QueryPerformanceFrequency(out long lpFrequency);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef handle, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        /// <summary>
        /// Bring a window to the top most even if it is minimized
        /// </summary>
        public static void SetForegroundWindow(string name)
        {
            const int SW_RESTORE = 9;
            var objProcesses = Process.GetProcessesByName(name);
            if (objProcesses.Length <= 0) { return; }
            var handle = objProcesses[0].MainWindowHandle;
            if (IsIconic(handle)){ShowWindowAsync(new HandleRef(null, handle), SW_RESTORE);}
            SetForegroundWindow(objProcesses[0].MainWindowHandle);
        }

        #endregion
    }
}
