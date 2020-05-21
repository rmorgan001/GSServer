namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ModelV
    {
        public ModelV()
        {
            DataContext = new ModelVM(); 
            InitializeComponent();
        }
    }
}
