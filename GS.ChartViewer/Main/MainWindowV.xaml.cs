/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.ChartViewer.Helpers;
using LiveCharts.Events;

namespace GS.ChartViewer.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindowV : IDisposable
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

        private void Axis_OnRangeChanged(RangeChangedEventArgs eventArgs)
        {
            var vm = (MainWindowVM)DataContext;
            var currentRange = eventArgs.Range;
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

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~MainWindowV()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            MouseLeftButtonDown += delegate { DragMove(); };
        }
    }
}
