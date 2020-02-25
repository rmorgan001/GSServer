using GS.Server.Helpers;
using System;
using System.Runtime.InteropServices;

namespace GS.Server.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            StartOnTop();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            GSServer.SaveAllAppSettings();
        }

        private void StartOnTop()
        {
            // Topmost = true;
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            //  Topmost = Properties.Server.Default.StartOnTop;
            Memory.FlushMemory();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            InvalidateMeasure();

        }
    }
}
