using System.Runtime.InteropServices;

namespace GS.Utilities.Controls.Dialogs
{
    [ComVisible(false)]
    public partial class TwoButtonMessageDialog
    {
        private TwoButtonMessageDialog()
        {
            InitializeComponent();
        }
        public TwoButtonMessageDialog(TwoButtonMessageDialogVM viewModel) : this()
        {
            this.DataContext = viewModel;
        }

    }
}
