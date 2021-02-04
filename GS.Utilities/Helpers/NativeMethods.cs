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

using System.Runtime.InteropServices;

namespace GS.Utilities.Helpers
{
    public static class NativeMethods
    {
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;

        private enum RM_APP_TYPE
        {
            //RmUnknownApp = 0,
            //RmMainWindow = 1,
            //RmOtherWindow = 2,
            //RmService = 3,
            //RmExplorer = 4,
            //RmConsole = 5,
            //RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RM_UNIQUE_PROCESS
        {
            public readonly int dwProcessId;
            private readonly System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        internal static extern int RmRegisterResources(uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        internal static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        internal static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        internal static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RM_PROCESS_INFO
        {
            public readonly RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            private readonly string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            private readonly string strServiceShortName;

            private readonly RM_APP_TYPE ApplicationType;
            private readonly uint AppStatus;
            private readonly uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)] private readonly bool bRestartable;
        }

    }
}
