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
using ASCOM;
using GS.Server.Helpers;
using GS.Shared;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using GS.Server.SkyTelescope;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace GS.Server
{
    public static class GSServer
    {
        #region Fields

        public static event PropertyChangedEventHandler StaticPropertyChanged;
        private static int _objInUse; // Keeps a count on the total number of objects alive.
        private static int _serverLocks; // Keeps a lock count on this application.
        private static ArrayList _comObjectTypes; // Served COM object types
        private static ArrayList _classFactories; // Served COM object class factories
        private const string SAppId = "{34ACA6EC-28AB-4B74-9F53-A11EE7847C2A}"; // Our AppId
        private static readonly object LockObject = new object();
        private static bool _removeProfile;
        private static BackgroundWorker _bgWorker;
        private const string _driverName = "ASCOM.GS.Sky.Telescope.dll";
        private const string _apiName = "GS.SkyApi.dll";

        #endregion

        #region Properties

        /// <summary>
        ///  Tracking this stops multiple instances of the setupdialog
        ///  window from opening when called from the driver
        /// </summary>
        public static bool SetupDialogOpen { get; set; }

        private static int _appCount;
        public static int AppCount
        {
            get => _appCount;
            set
            {
                _appCount = value;
                OnStaticPropertyChanged();
            }
        }

        public static void SaveAllAppSettings()
        {
            Properties.Server.Default.Save();
            Properties.SkyTelescope.Default.Save();
            Properties.Gamepad.Default.Save();
            Settings.Settings.Save();
            Shared.Settings.Save();
        }

        /// <summary>
        /// This property returns the main thread's id.
        /// </summary>
        private static uint MainThreadId { get; set; } // Stores the main thread's thread id.

        /// <summary>
        /// Used to tell if started by COM or manually True if server started by COM (-embedding)
        /// </summary>
        private static bool StartedByCom { get; set; }

        #endregion

        #region Server Lock, Object Counting, and AutoQuit on COM startup

        /// <summary>
        /// Returns the total number of objects alive currently.
        /// </summary>
        public static int ObjectsCount
        {
            get
            {
                lock (LockObject)
                {
                    return _objInUse;
                }
            }
        }

        /// <summary>
        /// This method performs a thread-safe incrementation of the objects count.
        /// </summary>
        public static void CountObject()
        {
            Interlocked.Increment(ref _objInUse);
            ++AppCount;

        }

        /// <summary>
        /// This method performs a thread-safe decrementation the objects count.
        /// </summary>
        public static void UncountObject()
        {
            Interlocked.Decrement(ref _objInUse);
            --AppCount;
        }

        /// <summary>
        /// Returns the current server lock count.
        /// </summary>
        private static int ServerLockCount
        {
            get
            {
                lock (LockObject)
                {
                    return _serverLocks;
                }
            }
        }

        /// <summary>
        /// This method performs a thread-safe incrementation the server lock count.
        /// </summary>
        public static void CountLock() => Interlocked.Increment(ref _serverLocks);

        /// <summary>
        /// This method performs a thread-safe decrementation the server lock count.
        /// </summary>
        public static void UncountLock() => Interlocked.Decrement(ref _serverLocks);

        // AttemptToTerminateServer() will check to see if the objects count and the server 
        // lock count have both dropped to zero.
        //
        // If so, and if we were started by COM, we post a WM_QUIT message to the main thread's
        // message loop. This will cause the message loop to exit and hence the termination 
        // of this application. If hand-started, then just trace that it WOULD exit now.
        //
        public static void ExitIf()
        {
            lock (LockObject)
            {
                if ((ObjectsCount > 0) || (ServerLockCount > 0)) return;
                if (!StartedByCom) return;
                SaveAllAppSettings();

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Server Exiting" };
                MonitorLog.LogToMonitor(monitorItem);

                var wParam = new UIntPtr(0);
                var lParam = new IntPtr(0);
                NativeMethods.PostThreadMessage(MainThreadId, 0x0012, wParam, lParam);
                Environment.Exit(0);
            }
        }

        #endregion

        #region Methods Load and Register Drivers, Garbadge Collection

        /// <summary>
        /// Load the assemblies that contain the classes that we will serve via COM. These will be located in the same folder as our executable.   
        /// </summary>
        /// <returns></returns>
        private static bool LoadComObjectAssemblies()
        {

            _comObjectTypes = new ArrayList();

            // put everything into one folder, the same as the server.
            var assyPath = Assembly.GetEntryAssembly()?.Location;
            assyPath = Path.GetDirectoryName(assyPath);
            if (!Directory.Exists(assyPath))
            {
                var msg = @"Unable to locate drivers directory - " + assyPath;

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);

                return false;
            }

            var dir = new DirectoryInfo(assyPath);
            var files = new List<FileInfo>();
            var driverpath = dir + "\\" + _driverName;
            if (File.Exists(driverpath)) { files.Add(new FileInfo(driverpath)); }

            var apipath = dir +  "\\" + _apiName;
            if (File.Exists(apipath)) { files.Add(new FileInfo(apipath)); }

            var dllfiles = files.ToArray();
            if (dllfiles.Length == 0)
            {
                var msg = @"Unable to locate any drivers - " + assyPath;

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);

                return false;
            }

            foreach (var fi in dllfiles)
            {
                var aPath = fi.FullName;
                //
                // First try to load the assembly and get the types for
                // the class and the class factory. If this doesn't work ????
                //
                try
                {
                    var so = Assembly.LoadFrom(aPath);
                    //PWGS Get the types in the assembly
                    var types = so.GetTypes();
                    foreach (var type in types)
                    {
                        // PWGS Now checks the type rather than the assembly
                        // Check to see if the type has the ServedClassName attribute, only use it if it does.
                        MemberInfo info = type;

                        var attrbutes = info.GetCustomAttributes(typeof(ServedClassNameAttribute), false);
                        if (attrbutes.Length > 0)
                        {
                            var monitorItem = new MonitorEntry
                            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{type.Assembly}" };
                            MonitorLog.LogToMonitor(monitorItem);

                            _comObjectTypes.Add(type); //PWGS - much simpler
                        }
                        var gssAttrbutes = info.GetCustomAttributes(typeof(GssAttribute), false);
                        if (gssAttrbutes.Length > 0)
                        {
                            var a = (GssAttribute)gssAttrbutes[0];

                            var monitorItem = new MonitorEntry
                            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{type.Assembly},{a.DisplayName}" };
                            MonitorLog.LogToMonitor(monitorItem);

                            _comObjectTypes.Add(type); //PWGS - much simpler
                        }
                    }
                }
                catch (BadImageFormatException ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    // Probably an attempt to load a Win32 DLL (i.e. not a .net assembly)
                    // Just swallow the exception and continue to the next item.
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Test if running elevated
        /// </summary>
        private static bool IsAdministrator
        {
            get
            {
                var i = WindowsIdentity.GetCurrent();
                var p = new WindowsPrincipal(i);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Elevate by re-running ourselves with elevation dialog
        /// </summary>
        /// <param name="arg"></param>
        private static void ElevateSelf(string arg)
        {
            var si = new ProcessStartInfo
            {
                Arguments = arg,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            };
            try
            {
                Process.Start(si);
            }
            catch (Win32Exception)
            {
                var msg = @"The GSServer was not " + (arg == "/register" ? "registered" : "unregistered") +
                          @" because you did not allow it.";
                MessageBox.Show(msg, @"GSServer", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), @"GSServer", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        // Do everything to register this for COM. Never use REGASM on
        // this exe assembly! It would create InProcServer32 entries 
        // which would prevent proper activation!
        //
        // Using the list of COM object types generated during dynamic
        // assembly loading, it registers each one for COM as served by our
        // exe/local server, as well as registering it for ASCOM. It also
        // adds DCOM info for the local server itself, so it can be activated
        // via an outboiud connection from TheSky.
        private static void RegisterObjects()
        {
            if (!IsAdministrator)
            {
                ElevateSelf("/register");
                return;
            }
            //
            // If reached here, we're running elevated
            //

            var assy = Assembly.GetExecutingAssembly();
            var attr = Attribute.GetCustomAttribute(assy, typeof(AssemblyTitleAttribute));
            var assyTitle = ((AssemblyTitleAttribute)attr).Title;
            attr = Attribute.GetCustomAttribute(assy, typeof(AssemblyDescriptionAttribute));
            var assyDescription = ((AssemblyDescriptionAttribute)attr).Description;

            //
            // Local server's DCOM/AppID information
            //
            try
            {
                //
                // HKCR\APPID\appid
                //
                using (var key = Registry.ClassesRoot.CreateSubKey("APPID\\" + SAppId))
                {
                    if (key != null)
                    {
                        key.SetValue(null, assyDescription);
                        key.SetValue("AppID", SAppId);
                        key.SetValue("AuthenticationLevel", 1, RegistryValueKind.DWord);
                        key.SetValue("RunAs", "Interactive User", RegistryValueKind.String);
                    }
                }

                //
                // HKCR\APPID\exename.ext
                //
                using (var key = Registry.ClassesRoot.CreateSubKey(
                    $"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}"))
                {
                    key?.SetValue("AppID", SAppId);
                }
            }
            catch (Exception ex)
            {
                // // LocalSystem.TraceLogItem("GreenSwamp Localserver", ex.Message);
                var msg = @"Error while registering the server:" + ex;
                MessageBox.Show(msg, @"GS Server", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                return;
            }

            //
            // For each of the driver assemblies
            //
            foreach (Type type in _comObjectTypes)
            {
                var bFail = false;
                try
                {
                    //
                    // HKCR\CLSID\clsid
                    //
                    var clsid = Marshal.GenerateGuidForType(type).ToString("B");
                    var progid = Marshal.GenerateProgIdForType(type);
                    //PWGS Generate device type from the Class name
                    var deviceType = type.Name;

                    using (var key = Registry.ClassesRoot.CreateSubKey($"CLSID\\{clsid}"))
                    {
                        if (key != null)
                        {
                            key.SetValue(null,
                                progid); // Could be assyTitle/Desc??, but .NET components show ProgId here
                            key.SetValue("AppId", SAppId);
                            using (var key2 = key.CreateSubKey("Implemented Categories"))
                            {
                                key2?.CreateSubKey("{5F2A3C41-7FBC-41A3-A04C-31CA94D06407}");
                            }

                            using (var key2 = key.CreateSubKey("ProgId"))
                            {
                                key2?.SetValue(null, progid);
                            }

                            key.CreateSubKey("Programmable");
                            using (var key2 = key.CreateSubKey("LocalServer32"))
                            {
                                key2?.SetValue(null, Application.ExecutablePath);
                            }
                        }
                    }

                    //
                    // HKCR\CLSID\progid
                    //
                    using (var key = Registry.ClassesRoot.CreateSubKey(progid))
                    {
                        if (key != null)
                        {
                            key.SetValue(null, assyTitle);
                            using (var key2 = key.CreateSubKey("CLSID"))
                            {
                                key2?.SetValue(null, clsid);
                            }
                        }
                    }
                    //
                    // ASCOM 
                    //

                    // Pull the display name from the ServedClassName attribute.
                    attr = Attribute.GetCustomAttribute(type, typeof(ServedClassNameAttribute)); //PWGS Changed to search type for attribute rather than assembly
                    if (attr != null)
                    {
                        var chooserName = ((ServedClassNameAttribute)attr).DisplayName ?? "MultiServer";
                        using (var p = new ASCOM.Utilities.Profile())
                        {
                            p.DeviceType = deviceType;
                            p.Register(progid, chooserName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Error while registering the server:" + ex,
                        @"GS Server", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    bFail = true;
                }
                if (bFail) break;
            }
        }

        // Remove all traces of this from the registry. 
        // If the above does AppID/DCOM stuff, this would have
        // to remove that stuff too.
        private static void UnRegisterObjects()
        {
            if (!IsAdministrator)
            {
                ElevateSelf("/unregister");
                return;
            }

            //
            // Local server's DCOM/AppID information
            //
            Registry.ClassesRoot.DeleteSubKey($"APPID\\{SAppId}", false);
            Registry.ClassesRoot.DeleteSubKey(
                $"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}",
                false);

            //
            // For each of the driver assemblies
            //
            foreach (Type type in _comObjectTypes)
            {
                var clsid = Marshal.GenerateGuidForType(type).ToString("B");
                var progid = Marshal.GenerateProgIdForType(type);
                var deviceType = type.Name;
                //
                // Best efforts
                //
                //
                // HKCR\progid
                //
                Registry.ClassesRoot.DeleteSubKey($"{progid}\\CLSID", false);
                Registry.ClassesRoot.DeleteSubKey(progid, false);
                //
                // HKCR\CLSID\clsid
                //
                Registry.ClassesRoot.DeleteSubKey(
                    $"CLSID\\{clsid}\\Implemented Categories\\{{5F2A3C41-7FBC-41A3-A04C-31CA94D06407}}", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\Implemented Categories", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\ProgId", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\LocalServer32", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\Programmable", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}", false);
                try
                {
                    var gssAttrbutes = type.GetCustomAttributes(typeof(GssAttribute), false);
                    if (gssAttrbutes.Length <= 0)
                    {
                        // ASCOM: Comment out this section so the profile settings with not be removed when install a driver update
                        if (_removeProfile)
                        {
                            using (var p = new ASCOM.Utilities.Profile())
                            {
                                p.DeviceType = deviceType;
                                p.Unregister(progid);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message},{ex.Source},{ex.StackTrace},{ex.InnerException}" };
                    MonitorLog.LogToMonitor(monitorItem);

                }
            }
        }

        /// <summary>
        /// On startup, we register the class factories of the COM objects
        /// that we serve. This requires the class facgtory name to be
        /// equal to the served class name + "ClassFactory".
        /// </summary>
        private static void RegisterClassFactories()
        {
            _classFactories = new ArrayList();
            foreach (Type type in _comObjectTypes)
            {
                var factory = new ClassFactory(type); // Use default context & flags
                _classFactories.Add(factory);
                if (factory.RegisterClassObject()) continue;
                var msg = @"Failed to register class factory for " + type.Name;
                MessageBox.Show(msg, @"GSServer", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);

                return;
            }
            ClassFactory.ResumeClassObjects(); // Served objects now go live

        }

        /// <summary>
        /// On shutdown, we unregister the class factories of the COM objects
        /// </summary>
        private static void RevokeClassFactories()
        {
            ClassFactory.SuspendClassObjects(); // Prevent race conditions
            foreach (ClassFactory factory in _classFactories)
                factory.RevokeClassObject();
        }

        // ProcessArguments() will process the command-line arguments
        // If the return value is true, we carry on and start this application.
        // If the return value is false, we terminate this application immediately.
        private static bool ProcessArguments(IReadOnlyList<string> args)
        {
            var bRet = true;
            if (args.Count > 0)
            {
                // should the profile be removed?
                if (args.Count > 1)
                    if (args[1].ToLower() == "-unprofile" || args[1].ToLower() == @"/unprofile")
                        _removeProfile = true;

                switch (args[0].ToLower())
                {
                    case "-embedding":
                        StartedByCom = true; // Indicate COM started us
                        break;
                    case "-register":
                    case @"/register":
                    case "-regserver": // Emulate VB6
                    case @"/regserver":
                        RegisterObjects(); // Register each served object
                        bRet = false;
                        break;
                    case "-unregister":
                    case @"/unregister":
                    case "-unregserver": // Emulate VB6
                    case @"/unregserver":
                        UnRegisterObjects(); //Un-register each served object
                        bRet = false;
                        break;
                    case @"/pec":
                        SkyServer.PecShow = true;
                        break;
                    case @"/en":
                        Shared.Settings.Language = "en-US";
                        break;
                    case @"/fr":
                        Shared.Settings.Language = "fr-FR";
                        break;
                    default:
                        MessageBox.Show(
                            @"Unknown argument: " + args[0] + @"Valid are : -register, -unregister and -embedding",
                            @"GSServer", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
            }
            else
                StartedByCom = false;

            return bRet;
        }

        /// <summary>
        /// When all calling applications disconnect this will destroy
        /// the main window in the Garbage Collector.
        /// </summary>
        private static void GarbageCollection_DoWork(object sender, DoWorkEventArgs e)
        {
            var interval = (int)e.Argument;
            while (!_bgWorker.CancellationPending)
            {
                Thread.Sleep(interval);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
        }

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Main Server Entry Point

        /// <summary>
        /// SERVER ENTRY POINT
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        private static void Main(string[] args)
        {

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Main Thread" };
            MonitorLog.LogToMonitor(monitorItem);

            if (!LoadComObjectAssemblies()) return; // Load served COM class assemblies, get types
            if (!ProcessArguments(args)) return; // Register/UnRegister

            // Initialize critical member variables.
            _objInUse = 0; _serverLocks = 0; MainThreadId = NativeMethods.GetCurrentThreadId();
            Thread.CurrentThread.Name = "Main Thread";

            // Register the class factories of the served objects
            RegisterClassFactories();

            // Start up the garbage collection thread.
            _bgWorker = new BackgroundWorker
            { WorkerSupportsCancellation = true };
            _bgWorker.DoWork += GarbageCollection_DoWork;
            _bgWorker.RunWorkerAsync(5000);

            // Start the message loop. This serializes incoming calls to our
            // served COM objects, making this act like the VB6 equivalent!
            try
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();

            }
            catch (Exception ex)
            {
                monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.Source},{ex.StackTrace},{ex.InnerException}" };
                MonitorLog.LogToMonitor(monitorItem);

                var str = $"Fatal error in GS Server: {ex.Message}";

                monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"GSServer: {str}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                var exi = ex.InnerException;
                if (ex.InnerException != null)
                {
                    monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"GSServer: {exi?.InnerException?.Message}, {exi?.InnerException?.StackTrace}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }

                MessageBox.Show(str); throw;
            }
            finally
            {
                // Revoke the class factories immediately.
                // Don't wait until the thread has stopped before
                // we perform revocation!!!
                RevokeClassFactories();
                // Cancel the garbage collection thread.
                _bgWorker.CancelAsync();
            }
        }

        #endregion
    }
}
