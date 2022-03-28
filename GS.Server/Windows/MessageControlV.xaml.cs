using System;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MessageControlV
    {
        public MessageControlV(string caption, string msg)
        {
            DataContext = new MessageControlVM(caption, msg); 
            InitializeComponent();
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
