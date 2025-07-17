using System;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ModelV
    {
        public ModelV()
        {
            DataContext = new ModelVm(); 
            InitializeComponent();
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
