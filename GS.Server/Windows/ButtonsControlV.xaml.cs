﻿namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ButtonsControlV 
    {
        public ButtonsControlV()
        {
            DataContext = new ButtonsControlVM(); 
            InitializeComponent();
        }
    }
}
