/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Runtime.InteropServices;
using System.Windows;

namespace GS.Server.Helpers
{
    internal static class NativeMethods
    {
        [DllImport("ole32.dll")]
        internal static extern int CoRegisterClassObject(
            [In] ref Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);

        /// <summary>
        /// Called by a COM EXE Server that can register multiple class objects 
        /// to inform COM about all registered classes, and permits activation 
        /// requests for those class objects. 
        /// This function causes OLE to inform the SCM about all the registered 
        /// classes, and begins letting activation requests into the server process.
        /// </summary>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        internal static extern int CoResumeClassObjects();

        /// <summary>
        /// Prevents any new activation requests from the SCM on all class objects
        /// registered within the process. Even though a process may call this API, 
        /// the process still must call CoRevokeClassObject for each CLSID it has 
        /// registered, in the apartment it registered in.
        /// </summary>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        internal static extern int CoSuspendClassObjects();

        /// <summary>
        /// CoRevokeClassObject() is used to unregister a Class Factory
        /// from COM's internal table of Class Factories.
        /// </summary>
        /// <param name="dwRegister"></param>
        /// <returns></returns>
        [DllImport("ole32.dll")]
        internal static extern int CoRevokeClassObject(uint dwRegister);

        /// <summary>
        /// PostThreadMessage() allows us to post a Windows Message to
        /// a specific thread (identified by its thread id).
        /// We will need this API to post a WM_QUIT message to the main 
        /// thread in order to terminate this application.
        /// </summary>
        /// <param name="idThread"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        internal static extern bool PostThreadMessage(uint idThread, uint msg, UIntPtr wParam,
            IntPtr lParam);

        /// <summary>
        /// GetCurrentThreadId() allows us to obtain the thread id of the
        /// calling thread. This allows us to post the WM_QUIT message to
        /// the main thread.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        /// <summary>
        /// SetProcessWorkingSetSize is typically used to increase the amount of RAM allocated
        /// for a process. Or to force a trim when the app knows that it is going to
        /// be idle for a long time. 
        /// </summary>
        /// <param name="process"></param>
        /// <param name="minimumWorkingSetSize"></param>
        /// <param name="maximumWorkingSetSize"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        /// <summary>
        /// Used for SleepMode.cs to stop pc from going into seel mode.
        /// </summary>
        /// <param name="nInputs"></param>
        /// <param name="pInputs"></param>
        /// <param name="cbSize"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);
        
        /// <summary>
        /// Limits mouse movements to a rectangle
        /// </summary>
        /// <param name="rect"></param>
        [DllImport("user32.dll")]
        internal static extern void ClipCursor(ref System.Drawing.Rectangle rect);

        /// <summary>
        /// Resets mouse movements by passing null pointer
        /// </summary>
        /// <param name="rect"></param>
        [DllImport("user32.dll")]
        internal static extern void ClipCursor(IntPtr rect);

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        public static Point GetCursorPosition()
        {
            GetCursorPos(out var lpPoint);
            // NOTE: If you need error handling
            // bool success = GetCursorPos(out lpPoint);
            // if (!success)
            return lpPoint;
        }
    }

    /// <summary>
    ///  Used for SleepMode.cs to stop pc screen saver
    /// </summary>
    internal struct INPUT
    {
#pragma warning disable 0649
        public int TYPE;
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
#pragma warning restore 0649
    }

    /// <summary>
    /// Struct representing a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        private readonly int X;
        private readonly int Y;

        public static implicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }
    }


}
