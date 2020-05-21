namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class HandControlV 
    {
        public HandControlV()
        {
            DataContext = new HandControlVM(); 
            InitializeComponent();
        }
    }
}
