using System;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ModelV
    {
        private readonly ModelVm _viewModel;

        public ModelV()
        {
            _viewModel = new ModelVm();
            DataContext = _viewModel;
            try
            {
                InitializeComponent();
            }
            catch
            {
                _viewModel.Dispose();
                throw;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                base.OnClosed(e);
            }
            finally
            {
                _viewModel.Dispose();
                DataContext = null;
            }
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
