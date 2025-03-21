using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Interaction logic for TelescopeView.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class SkyTelescopeV
    {
        public SkyTelescopeV()
        {
            InitializeComponent();
        }

        private void GoToDialog_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var name = ((FrameworkElement)sender).Name;
            switch (name)
            {
                case "RaDecGoTo":
                    SkyTelescopeVm.ASkyTelescopeVm.RaDecDialogActive = false;
                    SkyTelescopeVm.ASkyTelescopeVm.AltAzDialogActive = true;
                    break;
                case "AltAzGoTo":
                    SkyTelescopeVm.ASkyTelescopeVm.RaDecDialogActive = true;
                    SkyTelescopeVm.ASkyTelescopeVm.AltAzDialogActive = false;
                    break;
            }
        }
    }
}
 