/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GS.Server.Helpers
{
    /// <summary>
    /// Holds Windows power requests (system, execution and display required) while a mount is connected,
    /// so the PC does not idle to sleep or throttle the process (Modern Standby) during long waits.
    /// One request handle is created on <see cref="Acquire"/> and reused by <see cref="Reassert"/>
    /// until <see cref="Release"/> is called.
    /// </summary>
    internal static class PowerRequestHelper
    {
        private static readonly object RequestLock = new object();
        private static readonly PowerRequestType[] RequestTypes =
        {
            PowerRequestType.SystemRequired,
            PowerRequestType.ExecutionRequired,
            PowerRequestType.DisplayRequired
        };

        private static IntPtr _handle = IntPtr.Zero;

        private enum PowerRequestType
        {
            DisplayRequired = 0,
            SystemRequired = 1,
            ExecutionRequired = 3
        }

        private const uint PowerRequestContextVersion = 0;
        private const uint PowerRequestContextSimpleString = 0x1;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        // Full size of the native REASON_CONTEXT, the union is sized by the Detailed member
        [StructLayout(LayoutKind.Sequential)]
        private struct ReasonContext
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string SimpleReasonString;
            public uint DetailedReasonId;
            public uint DetailedReasonStringCount;
            public IntPtr DetailedReasonStrings;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr PowerCreateRequest(ref ReasonContext context);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PowerSetRequest(IntPtr powerRequest, PowerRequestType requestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PowerClearRequest(IntPtr powerRequest, PowerRequestType requestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Creates the power request handle if needed and sets all request types.
        /// </summary>
        /// <param name="reason">Reason text shown by powercfg /requests</param>
        public static void Acquire(string reason)
        {
            lock (RequestLock)
            {
                if (_handle == IntPtr.Zero)
                {
                    var context = new ReasonContext
                    {
                        Version = PowerRequestContextVersion,
                        Flags = PowerRequestContextSimpleString,
                        SimpleReasonString = reason
                    };

                    var handle = PowerCreateRequest(ref context);
                    if (handle == IntPtr.Zero || handle == InvalidHandleValue)
                    {
                        Log(MonitorType.Warning, $"PowerCreateRequest failed|{new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                        return;
                    }

                    _handle = handle;
                    Log(MonitorType.Information, $"PowerCreateRequest succeeded|{reason}");
                }

                SetRequests("Set");
            }
        }

        /// <summary>
        /// Sets all request types again on the existing handle, e.g. after a power resume. No action if not acquired.
        /// </summary>
        public static void Reassert()
        {
            lock (RequestLock)
            {
                if (_handle == IntPtr.Zero) { return; }
                SetRequests("Reassert");
            }
        }

        /// <summary>
        /// Clears all request types and closes the handle. No action if not acquired.
        /// </summary>
        public static void Release()
        {
            lock (RequestLock)
            {
                if (_handle == IntPtr.Zero) { return; }

                foreach (var requestType in RequestTypes)
                {
                    if (PowerClearRequest(_handle, requestType))
                    {
                        Log(MonitorType.Information, $"PowerClearRequest succeeded|{requestType}");
                    }
                    else
                    {
                        Log(MonitorType.Warning, $"PowerClearRequest failed|{requestType}|{new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                    }
                }

                if (!CloseHandle(_handle))
                {
                    Log(MonitorType.Warning, $"PowerRequest CloseHandle failed|{new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                }

                _handle = IntPtr.Zero;
            }
        }

        // Caller must hold RequestLock
        private static void SetRequests(string action)
        {
            foreach (var requestType in RequestTypes)
            {
                if (PowerSetRequest(_handle, requestType))
                {
                    Log(MonitorType.Information, $"PowerSetRequest succeeded|{action}|{requestType}");
                }
                else
                {
                    Log(MonitorType.Warning, $"PowerSetRequest failed|{action}|{requestType}|{new Win32Exception(Marshal.GetLastWin32Error()).Message}");
                }
            }
        }

        private static void Log(MonitorType type, string message, [CallerMemberName] string method = "")
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = type,
                Method = method,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = message
            };
            MonitorLog.LogToMonitor(monitorItem);
        }
    }
}
