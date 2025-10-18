using System;
using System.Runtime.InteropServices;
using ASCOM.DeviceInterface;

namespace GS.Server.Model3D
{
    /// <summary>
    /// Interaction logic for FocuserView.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class Model3DV
    {
        public Model3DV()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Combo Box list opened event handler to filter list items by Alignment Mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModelListFilter(object sender, EventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox)sender;
            // Reset filter to ensure all items are in list
            cmb.Items.Filter = null;
            // Apply filter based on AlignmentMode
            if (SkyTelescope.SkySettings.AlignmentMode == AlignmentModes.algPolar)
            {
                cmb.Items.Filter = x => ((string)x).Contains("Dual");
            }
            else
            {
                cmb.Items.Filter = x => !((string)x).Contains("Dual");
            }
        }
    }
}
