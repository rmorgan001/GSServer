/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using LiveCharts.Events;

namespace GS.ChartViewer
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
                    vm.FormatterX = x => new DateTime((long)x).ToString("HH:mm:ss.FFF");
                    return;
                case double _ when (currentRange < TimeSpan.TicksPerSecond * 1000):
                    vm.FormatterX = x => new DateTime((long)x).ToString("HH:mm:ss");
                    return;
                case double _ when (currentRange < TimeSpan.TicksPerSecond * 3000):
                    vm.FormatterX = x => new DateTime((long)x).ToString("t");
                    return;
                default:
                    vm.FormatterX = x => new DateTime((long)x).ToString("d");
                    break;
            }

        }

        public void Dispose()
        {
           // var vm = (MainWindowVM)DataContext;

        }
    }
}
