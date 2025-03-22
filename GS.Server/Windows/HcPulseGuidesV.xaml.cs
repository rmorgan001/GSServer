using System;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class HcPulseGuidesV 
    {
        public HcPulseGuidesV()
        {
            DataContext = new HcPulseGuidesVm(); 
            InitializeComponent();
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
