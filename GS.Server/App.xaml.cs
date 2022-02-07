using System;
using System.Linq;
using System.Reflection;
using GS.Server.Main;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using GS.Shared;
using System.Text;
using System.IO;

namespace GS.Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class App : IDisposable
    {
        // give the mutex a  unique name
        private const string MutexName = "Green Swamp Server";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        private readonly bool createdNew;
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (createdNew) return;
            // if the mutex already exists, notify and quit
            MessageBox.Show("GS Server is already running", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Current.Shutdown(0);
        }

        /// <summary>
        /// Hack to get around the strong name and material design not load correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            return assembly;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (Server.Properties.Server.Default.DisableHardwareAcceleration)
            {
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            base.OnStartup(e);
            Languages.SetLanguageDictionary(false, LanguageApp.GSServer);
            var app = new MainWindow();
            var context = new MainWindowVM();
            app.DataContext = context;
            app.Show();
        }

        #region Application exception handling ...
        /// <summary>
        /// Application level exception handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            DispatcherUnhandledException -= Application_DispatcherUnhandledException;
            WriteException(e.Exception);
            MessageBox.Show("An unexpected error has occurred within GSS. Error details have been logged and Gems will now close.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            this.Shutdown();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            WriteException((Exception)e.ExceptionObject);
            MessageBox.Show("An unexpected error has occurred within GSS. Error details have been logged and Gems will now close.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown();
        }


        private void WriteException(Exception e)
        {
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileLocation = Path.Combine(logPath, "GSServer\\");

            string logFileName = Path.Combine(fileLocation, Path.GetRandomFileName() + ".err");
            StringBuilder sb = new StringBuilder();
            WriteException(sb, e);
            using (TextWriter logWriter = new StreamWriter(logFileName))
            {
                logWriter.Write(sb.ToString());
                logWriter.Close();
            }


        }

        /// <summary>
        /// Pushes exception details into a StringBuilder recursively as long as InnerExceptions exist.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="e"></param>
        private void WriteException(StringBuilder sb, Exception e)
        {
            sb.AppendLine(e.GetType().Name);
            if (!string.IsNullOrWhiteSpace(e.Message))
            {
                sb.AppendLine("Exception: " + e.Message);
            }
            if (!string.IsNullOrWhiteSpace(e.Source))
            {
                sb.AppendLine("Source: " + e.Source);
            }
            if (!string.IsNullOrWhiteSpace(e.StackTrace))
            {
                sb.AppendLine("Stack Trace");
                sb.AppendLine(e.StackTrace);
            }
            if (e.InnerException != null)
            {
                sb.AppendLine("Inner Exception");
                WriteException(sb, e.InnerException);
            }
        }

        #endregion


        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~App()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mutex?.Dispose();
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion
    }
}
