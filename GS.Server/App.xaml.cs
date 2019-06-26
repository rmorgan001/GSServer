using System.Runtime.InteropServices;
using System.Windows;
using GS.Server.Main;


namespace GS.Server
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var app = new MainWindow();
            var context = new MainWindowVM();
            app.DataContext = context;
            app.Show();
        }
    }
}
