using GS.Server.Windows;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace GS.Server.Controls
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class HardwareLimits
    {
        public HardwareLimits()
        {
            InitializeComponent();
            // Set the DataContext to the HardwareLimits ViewModel
            DataContext = new HardwareLimitsVm();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true; // Dialog behaviour, no window drag 
        }
    }
}
