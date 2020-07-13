using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace GS.Server.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class HcSettingsDialog
    {
        public HcSettingsDialog()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
}
}
