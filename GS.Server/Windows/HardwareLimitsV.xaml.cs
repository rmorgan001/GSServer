using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for HardwareLimits.xaml
    /// </summary>
    public partial class HardwareLimitsV : Window
    {
        public HardwareLimitsV()
        {
            InitializeComponent();
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
