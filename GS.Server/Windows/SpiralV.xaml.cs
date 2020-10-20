

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace GS.Server.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SpiralV 
    {
        public SpiralV()
        {
            DataContext = new SpiralVM(); 
            InitializeComponent();
        }

        private void LoadPoints()
        {
            var icons = new List<PackIcon>
            {
                C0, C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22,
                C23, C24, C25, C26, C27, C28, C29, C30, C31, C32, C33, C34, C35, C36, C37, C38, C39, C40, C41, C42, C43,
                C44, C45, C46, C47, C48
            };

            var points = new PointCollection();
            foreach (var p in icons)
            {
                var x = Convert.ToInt32(p.ActualHeight / 2);
                var y = Convert.ToInt32(p.ActualWidth / 2);
                points.Add(p.TransformToAncestor(SpiralGrid).Transform(new Point(x, y)));
            }
            SpiralLine.Points = points;
        }

        private void SpiralV_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadPoints();
        }

        private void SpiralV_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoadPoints();
        }
    }
}
