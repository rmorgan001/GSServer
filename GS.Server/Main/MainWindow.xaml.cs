using GS.Server.Helpers;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

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
            SetOnScreen(this);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            InvalidateMeasure();

        }

        /// <summary>
        /// Make sure startup window is within the visible screen area
        /// </summary>
        /// <param name="win"></param>
        /// <remarks>https://stackoverflow.com/questions/987018/determining-if-a-form-is-completely-off-screen/987090</remarks>
        public void SetOnScreen(Window win)
        {
            if (win == null) return;
            var windowRect = new System.Drawing.Rectangle((int)win.Left, (int)win.Top, (int)win.Width, (int)win.Height);

            if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(windowRect))) return;
            win.Top = 10;
            win.Left = 10;
            win.Height = 510;
            win.Width = 850;
            win.WindowState = WindowState.Normal;
        }
    }
}
