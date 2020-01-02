using GS.Server.Main;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;


namespace GS.Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class App
    {
        // give the mutex a  unique name
        private const string MutexName = "Green Swamp Server";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        private bool createdNew;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (createdNew) return;
            // if the mutex already exists, notify and quit
            MessageBox.Show("GS Server is already running","Error",MessageBoxButton.OK,MessageBoxImage.Exclamation);
            Current.Shutdown(0);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;
            base.OnStartup(e);
            var app = new MainWindow();
            var context = new MainWindowVM();
            app.DataContext = context;
            app.Show();
        }
    }
}
