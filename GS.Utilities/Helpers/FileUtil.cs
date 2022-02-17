/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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

    https://stackoverflow.com/questions/1304/how-to-check-for-file-lock/20623302#20623302
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable UnusedMember.Local

namespace GS.Utilities.Helpers
{
    public static class FileUtil
    {
        private const int RmRebootReasonNone = 0;
        
        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
        /// 
        /// </remarks>
        public static List<Process> WhoIsLocking(string path)
        {
            var key = Guid.NewGuid().ToString();
            var processes = new List<Process>();

            var res = NativeMethods.RmStartSession(out var handle, 0, key);

            if (res != 0)
                throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                var resources = new[] { path }; // Just checking on one resource.

                res = NativeMethods.RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0)
                    throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = NativeMethods.RmGetList(handle, out var pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    var processInfo = new NativeMethods.RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = NativeMethods.RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);

                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (var i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else
                        throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0)
                    throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                NativeMethods.RmEndSession(handle);
            }

            return processes;
        }

        //public static List<Module> CollectModules(Process process)
        //{
        //    List<Module> collectedModules = new List<Module>();

        //    IntPtr[] modulePointers = new IntPtr[0];
        //    int bytesNeeded = 0;

        //    // Determine number of modules
        //    if (!Native.EnumProcessModulesEx(process.Handle, modulePointers, 0, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
        //    {
        //        return collectedModules;
        //    }

        //    int totalNumberofModules = bytesNeeded / IntPtr.Size;
        //    modulePointers = new IntPtr[totalNumberofModules];

        //    // Collect modules from the process
        //    if (Native.EnumProcessModulesEx(process.Handle, modulePointers, bytesNeeded, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
        //    {
        //        for (int index = 0; index < totalNumberofModules; index++)
        //        {
        //            StringBuilder moduleFilePath = new StringBuilder(1024);
        //            Native.GetModuleFileNameEx(process.Handle, modulePointers[index], moduleFilePath, (uint)(moduleFilePath.Capacity));

        //            string moduleName = Path.GetFileName(moduleFilePath.ToString());
        //            Native.ModuleInformation moduleInformation = new Native.ModuleInformation();
        //            Native.GetModuleInformation(process.Handle, modulePointers[index], out moduleInformation, (uint)(IntPtr.Size * (modulePointers.Length)));

        //            // Convert to a normalized module and add it to our list
        //            Module module = new Module(moduleName, moduleInformation.lpBaseOfDll, moduleInformation.SizeOfImage);
        //            collectedModules.Add(module);
        //        }
        //    }

        //    return collectedModules;
        //}
    }



    //public class Native
    //{
    //    [StructLayout(LayoutKind.Sequential)]
    //    public struct ModuleInformation
    //    {
    //        public IntPtr lpBaseOfDll;
    //        public uint SizeOfImage;
    //        public IntPtr EntryPoint;
    //    }

    //    internal enum ModuleFilter
    //    {
    //        ListModulesDefault = 0x0,
    //        ListModules32Bit = 0x01,
    //        ListModules64Bit = 0x02,
    //        ListModulesAll = 0x03,
    //    }

    //    [DllImport("psapi.dll")]
    //    public static extern bool EnumProcessModulesEx(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] [In][Out] IntPtr[] lphModule, int cb, [MarshalAs(UnmanagedType.U4)] out int lpcbNeeded, uint dwFilterFlag);

    //    [DllImport("psapi.dll")]
    //    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] uint nSize);

    //    [DllImport("psapi.dll", SetLastError = true)]
    //    public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out ModuleInformation lpmodinfo, uint cb);
    //}
    //public class Module
    //{
    //    public Module(string moduleName, IntPtr baseAddress, uint size)
    //    {
    //        this.ModuleName = moduleName;
    //        this.BaseAddress = baseAddress;
    //        this.Size = size;
    //    }

    //    public string ModuleName { get; set; }
    //    public IntPtr BaseAddress { get; set; }
    //    public uint Size { get; set; }
    //}

}
