/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GS.Shared;

namespace GS.Server.Helpers
{

    public class SleepMode
    {
        private const int INPUT_MOUSE = 0;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private CancellationTokenSource _ctsSleepMode;

        private bool _sleepMode;
        public bool Sleep
        {
            get => _sleepMode;
            set
            {
                if (_sleepMode == value) return;
                _sleepMode = value;
                if (value) return;
                _ctsSleepMode?.Cancel();
                _ctsSleepMode?.Dispose();
                _ctsSleepMode = null;
            }
        }

        public void SleepOn()
        {
            Sleep = true;
            SleepModeLoopAsync();
        }

        public void SleepOff()
        {
            Sleep = false;
        }

        private async void SleepModeLoopAsync()
        {
            try
            {
                if (_ctsSleepMode == null) _ctsSleepMode = new CancellationTokenSource();
                var moved = false;
                var ct = _ctsSleepMode.Token;
                var KeepRunning = true;
                var task = Task.Run(() =>
                {
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            // ct.ThrowIfCancellationRequested();
                            KeepRunning = false;
                        }
                        else
                        {
                            MouseMove(0, 0);
                            if (moved)
                                MouseMove(6, 6);
                            else // zag
                            {
                                MouseMove(-6, -6);
                            }
                            moved = !moved;
                            Thread.Sleep(45000);
                        }
                    }
                    KeepRunning = false;
                }, ct);
                await task;
                task.Wait(ct);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static void MouseMove(int dx, int dy)
        {
            var inp = new INPUT
            {
                TYPE = INPUT_MOUSE,
                dx = dx,
                dy = dy,
                mouseData = 0,
                dwFlags = MOUSEEVENTF_MOVE,
                time = 0,
                dwExtraInfo = (IntPtr)0
            };

            if (NativeMethods.SendInput(1, ref inp, Marshal.SizeOf(inp)) != 1)
                throw new Win32Exception();
        }
    }
}
