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
                    SkyTelescopeVM._skyTelescopeVM.RaDecDialogActive = false;
                    SkyTelescopeVM._skyTelescopeVM.AltAzDialogActive = true;
                    break;
                case "AltAzGoTo":
                    SkyTelescopeVM._skyTelescopeVM.RaDecDialogActive = true;
                    SkyTelescopeVM._skyTelescopeVM.AltAzDialogActive = false;
                    break;
            }
        }
    }
}
 