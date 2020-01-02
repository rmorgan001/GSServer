using LiveCharts.Events;
using System;

namespace GS.LogView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowVM();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Settings.Save();
        }

        private void Axis_OnRangeChanged(RangeChangedEventArgs eventargs)
        {
            var vm = (MainWindowVM)DataContext;
            var currentRange = eventargs.Range;
            var tmpRange = currentRange;
            var time = TimeSpan.FromTicks((long)tmpRange);
            vm.RangeTxt = time.ToString(@"hh\:mm\:ss");

            switch (currentRange)
            {
                case double _ when (currentRange < TimeSpan.TicksPerSecond * 300):
                    vm.XFormatter = x => new DateTime((long)x).ToString("HH:mm:ss.FFF");
                    return;
                case double _ when (currentRange < TimeSpan.TicksPerSecond * 1000):
                    vm.XFormatter = x => new DateTime((long)x).ToString("HH:mm:ss");
                    return;
                case double _ when (currentRange < TimeSpan.TicksPerSecond * 3000):
                    vm.XFormatter = x => new DateTime((long)x).ToString("t");
                    return;
                default:
                    vm.XFormatter = x => new DateTime((long)x).ToString("d");
                    break;
            }

        }

        public void Dispose()
        {
            var vm = (MainWindowVM)DataContext;
            vm.RaValues.Dispose();
            vm.DecValues.Dispose();
        }
    }
}
