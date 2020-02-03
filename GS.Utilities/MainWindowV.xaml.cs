using System;

namespace GS.Utilities
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowV 
    {
        public MainWindowV()
        {
            InitializeComponent();
            DataContext = new MainWindowVM();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Settings.Save();
        }

        public void Dispose()
        {
            // var vm = (MainWindowVM)DataContext;

        }
    }
}
