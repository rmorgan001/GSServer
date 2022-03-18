/* Copyright(C) 2019-2021-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GS.Server.Controls.Dialogs;
using GS.Server.SkyTelescope;
using GS.Server.Windows;
using GS.Shared.Command;

namespace GS.Server.Snap
{
    public class SnapVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        public string TopName => "Snap";
        public string BottomName => "";
        public int Uid => 9;

        private CancellationTokenSource ctsSnap1;
        private CancellationTokenSource ctsSnap2;
        
        #endregion

        public SnapVM()
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading SnapVM" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;

                SnapEnabled = false;

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #region ViewModel

        private bool _snapEnabled;
        public bool SnapEnabled
        {
            get => _snapEnabled;
            set
            {
                if (_snapEnabled == value) return;
                
                if (value)
                {
                    if (!SkyServer.IsMountRunning)
                    {
                        OpenDialog($"{Application.Current.Resources["snapConn"]}", $"{Application.Current.Resources["exError"]}");
                        _snapEnabled = false;
                        OnPropertyChanged();
                        return;
                    }

                    using (new WaitCursor())
                    {
                        Snap1Trigger(false);
                        Snap2Trigger(false);
                    }

                    if (!SkyServer.SnapPort1Result && !SkyServer.SnapPort2Result)
                    {
                        OpenDialog($"{Application.Current.Resources["snapSupport"]}", $"{Application.Current.Resources["exError"]}");
                        _snapEnabled = false;
                        OnPropertyChanged();
                        return;
                    }
                }
                else
                {
                    ctsSnap1?.Cancel();
                    ctsSnap2?.Cancel();
                    Thread.Sleep(500);
                    SkyServer.SnapPort1Result = false;
                    SkyServer.SnapPort2Result = false;
                }
                SetDefaults();
                _snapEnabled = value;
                OnPropertyChanged();

               var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Snap|{value}|{SkyServer.SnapPort1Result}|{SkyServer.SnapPort2Result}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private void SetDefaults()
        {
            Snap1Enabled = SkyServer.SnapPort1Result;
            Snap2Enabled = SkyServer.SnapPort2Result;

            Snap1Loops = 1;
            Snap2Loops = 1;
            Snap1Timer = 1;
            Snap2Timer = 1;
            Snap1Delay = 1;
            Snap2Delay = 1;

            Snap1GaugeValue = 0;
            Snap2GaugeValue = 0;
        }

        /// <summary>
        /// Property changes from the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSkyServer(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "IsMountRunning":
                         if (!SkyServer.IsMountRunning)
                         {
                             SnapEnabled = false;
                         }
                         break;
                     case "SnapPort1Result":
                         Snap1Enabled = SkyServer.SnapPort1Result;
                         break;
                     case "SnapPort2Result":
                         Snap2Enabled = SkyServer.SnapPort2Result;
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #endregion

        #region Snap1
        private bool Snap1Pause { get; set; }
        private bool Snap1Running { get; set; }

        private bool _snap1Enabled;
        public bool Snap1Enabled
        {
            get => _snap1Enabled;
            set
            {
                if (_snap1Enabled == value) return;
                _snap1Enabled = value;
                OnPropertyChanged();
            }
        }

        private double _snap1Timer;
        public double Snap1Timer
        {
            get => _snap1Timer;
            set
            {
                value = Math.Abs(value);
                if (Math.Abs(_snap1Timer - value) < 0.0) return;
                _snap1Timer = value;
                OnPropertyChanged();
            }
        }

        private int _snap1Loops;
        public int Snap1Loops
        {
            get => _snap1Loops;
            set
            {
                value = Math.Abs(value);
                if (_snap1Loops == value) return;
                _snap1Loops = value;
                OnPropertyChanged();
            }
        }

        private double _snap1Delay;
        public double Snap1Delay
        {
            get => _snap1Delay;
            set
            {
                value = Math.Abs(value);
                if (Math.Abs(_snap1Delay - value) < 0.0001) return;
                _snap1Delay = value;
                OnPropertyChanged();
            }
        }

        private int _snap1GaugeValue;
        public int Snap1GaugeValue
        {
            get => _snap1GaugeValue;
            set
            {
                _snap1GaugeValue = value;
                OnPropertyChanged();
            }
        }

        private int _snap1GaugeMax;
        public int Snap1GaugeMax
        {
            get => _snap1GaugeMax;
            set
            {
                if (_snap1GaugeMax == value) return;
                _snap1GaugeMax = value;
                OnPropertyChanged();
            }
        }

        private string _snap1StartBadgeContent;
        public string Snap1StartBadgeContent
        {
            get => _snap1StartBadgeContent;
            set
            {
                _snap1StartBadgeContent = value;
                OnPropertyChanged();
            }
        }
        
        private string _snap1PauseBadgeContent;
        public string Snap1PauseBadgeContent
        {
            get => _snap1PauseBadgeContent;
            set
            {
                _snap1PauseBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickSnap1StartCmd;
        public ICommand ClickSnap1StartCmd
        {
            get
            {
                var command = _clickSnap1StartCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickSnap1StartCmd = new RelayCommand(
                    param => ClickSnap1Start()
                );
            }
        }
        private void ClickSnap1Start()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (Snap1Running)
                    {
                        Snap1Stop();
                    }
                    else
                    {
                        if (Snap1Timer <= 0)
                        {
                            OpenDialog(Application.Current.Resources["snapTimerEr"].ToString(),
                                $"{Application.Current.Resources["exError"]}");
                            return;
                        }

                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.UI,
                            Category = MonitorCategory.Interface,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Snap 1|{SkyServer.SnapPort1Result}|{Snap1Timer}|{Snap1Loops}|{Snap1Delay}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                        Snap1LoopAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _clickSnap1PauseCmd;
        public ICommand ClickSnap1PauseCmd
        {
            get
            {
                var command = _clickSnap1PauseCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickSnap1PauseCmd = new RelayCommand(
                    param => ClickSnap1Pause()
                );
            }
        }
        private void ClickSnap1Pause()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!Snap1Running)
                    {
                        Snap1Pause = false;
                    }
                    else
                    {
                        Snap1Pause = !Snap1Pause;
                    }

                    Snap1PauseBadgeContent = Snap1Pause ? Application.Current.Resources["snapOn"].ToString() : "";
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private void Snap1Stop()
        {
            ctsSnap1?.Cancel();
            ctsSnap1?.Dispose();
            ctsSnap1 = null;
            Snap1Pause = false;
            Snap1StartBadgeContent = "";
            Snap1PauseBadgeContent = "";
        }

        private void Snap1Start()
        {
            Snap1Running = true;
            Snap1Pause = false;
            Snap1StartBadgeContent = Application.Current.Resources["snapOn"].ToString();
            Snap1PauseBadgeContent = "";
            Snap1GaugeMax = Snap1Loops == 0 ? 1 : Snap1Loops;
        }

        private void Snap1Trigger(bool on)
        {
            if (!SkyServer.IsMountRunning)
            {
                ctsSnap1?.Cancel();
                return;
            }
            // take snap
            SkyServer.SnapPort1 = on;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SkyServer.SimTasks(MountTaskName.SetSnapPort1);
                    break;
                case MountType.SkyWatcher:
                    SkyServer.SkyTasks(MountTaskName.SetSnapPort1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void Snap1LoopAsync()
        {
            try
            {
                if (ctsSnap1 == null) ctsSnap1 = new CancellationTokenSource();
                var ct = ctsSnap1.Token;
                var task = Task.Run(() =>
                {
                    Snap1Start();
                    var KeepRunning = true;
                    Snap1GaugeValue = 0;
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            //  KeepRunning = false;
                            break;
                        }

                        if (!Snap1Running)
                        {
                            ctsSnap1.Cancel();
                            break;
                        }

                        if (!Snap1Enabled)
                        {
                            ctsSnap1.Cancel();
                            break;
                        }

                        if (Snap1GaugeValue >= Snap1Loops)
                        {
                            ctsSnap1.Cancel();
                            break;
                        }

                        if (Snap1Pause)
                        {
                            Thread.Sleep(500);
                            continue;
                        }

                        // take snap
                        Snap1Trigger(true);
                        var swSnap = Stopwatch.StartNew();
                        while (swSnap.Elapsed.TotalMilliseconds < TimeSpan.FromSeconds(Snap1Timer).TotalMilliseconds)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                KeepRunning = false;
                                break;
                            }

                            Thread.Sleep(10);
                        }

                        Snap1Trigger(false);

                        // Delay
                        var swD = Stopwatch.StartNew();
                        while (swD.Elapsed.TotalMilliseconds < TimeSpan.FromSeconds(Snap1Delay).TotalMilliseconds)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                KeepRunning = false;
                                break;
                            }

                            Thread.Sleep(100);
                        }

                        Snap1GaugeValue++;
                    }
                }, ct);
                await task;
                task.Wait(ct);
                if (SkyServer.SnapPort1)
                {
                    Snap1Trigger(false);
                }
                Snap1Stop();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                Snap1Stop();
                OpenDialog(ex.Message);
            }
            finally
            {
                Snap1Running = false;
            }
        }

        #endregion

        #region Snap2

        private bool Snap2Pause { get; set; }
        private bool Snap2Running { get; set; }

        private bool _snap2Enabled;
        public bool Snap2Enabled
        {
            get => _snap2Enabled;
            set
            {
                if (_snap2Enabled == value) return;
                _snap2Enabled = value;
                OnPropertyChanged();
            }
        }

        private double _snap2Timer;
        public double Snap2Timer
        {
            get => _snap2Timer;
            set
            {
                value = Math.Abs(value);
                if (Math.Abs(_snap2Timer - value) < 0.0) return;
                _snap2Timer = value;
                OnPropertyChanged();
            }
        }

        private int _snap2Loops;
        public int Snap2Loops
        {
            get => _snap2Loops;
            set
            {
                value = Math.Abs(value);
                if (_snap2Loops == value) return;
                _snap2Loops = value;
                OnPropertyChanged();
            }
        }

        private double _snap2Delay;
        public double Snap2Delay
        {
            get => _snap2Delay;
            set
            {
                value = Math.Abs(value);
                if (Math.Abs(_snap2Delay - value) < 0.0001) return;
                _snap2Delay = value;
                OnPropertyChanged();
            }
        }

        private int _snap2GaugeValue;
        public int Snap2GaugeValue
        {
            get => _snap2GaugeValue;
            set
            {
                _snap2GaugeValue = value;
                OnPropertyChanged();
            }
        }

        private int _snap2GaugeMax;
        public int Snap2GaugeMax
        {
            get => _snap2GaugeMax;
            set
            {
                if (_snap2GaugeMax == value) return;
                _snap2GaugeMax = value;
                OnPropertyChanged();
            }
        }

        private string _snap2StartBadgeContent;
        public string Snap2StartBadgeContent
        {
            get => _snap2StartBadgeContent;
            set
            {
                _snap2StartBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private string _snap2PauseBadgeContent;
        public string Snap2PauseBadgeContent
        {
            get => _snap2PauseBadgeContent;
            set
            {
                _snap2PauseBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickSnap2StartCmd;
        public ICommand ClickSnap2StartCmd
        {
            get
            {
                var command = _clickSnap2StartCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickSnap2StartCmd = new RelayCommand(
                    param => ClickSnap2Start()
                );
            }
        }
        private void ClickSnap2Start()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (Snap2Running)
                    {
                        Snap2Stop();
                    }
                    else
                    {
                        if (Snap2Timer <= 0)
                        {
                            OpenDialog(Application.Current.Resources["snapTimerEr"].ToString(),
                                $"{Application.Current.Resources["exError"]}");
                            return;
                        }

                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.UI,
                            Category = MonitorCategory.Interface,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Snap 2|{SkyServer.SnapPort2Result}|{Snap2Timer}|{Snap2Loops}|{Snap2Delay}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                        Snap2LoopAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _clickSnap2PauseCmd;
        public ICommand ClickSnap2PauseCmd
        {
            get
            {
                var command = _clickSnap2PauseCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickSnap2PauseCmd = new RelayCommand(
                    param => ClickSnap2Pause()
                );
            }
        }
        private void ClickSnap2Pause()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!Snap2Running)
                    {
                        Snap2Pause = false;
                    }
                    else
                    {
                        Snap2Pause = !Snap2Pause;
                    }

                    Snap2PauseBadgeContent = Snap2Pause ? Application.Current.Resources["snapOn"].ToString() : "";
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private void Snap2Stop()
        {
            ctsSnap2?.Cancel();
            ctsSnap2?.Dispose();
            ctsSnap2 = null;
            Snap2Pause = false;
            Snap2StartBadgeContent = "";
            Snap2PauseBadgeContent = "";
        }

        private void Snap2Start()
        {
            Snap2Running = true;
            Snap2Pause = false;
            Snap2StartBadgeContent = Application.Current.Resources["snapOn"].ToString();
            Snap2PauseBadgeContent = "";
            Snap2GaugeMax = Snap2Loops == 0 ? 1 : Snap2Loops;
        }

        private void Snap2Trigger(bool on)
        {
            if (!SkyServer.IsMountRunning)
            {
                ctsSnap2?.Cancel();
                return;
            }
            // take snap
            SkyServer.SnapPort2 = on;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SkyServer.SimTasks(MountTaskName.SetSnapPort2);
                    break;
                case MountType.SkyWatcher:
                    SkyServer.SkyTasks(MountTaskName.SetSnapPort2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void Snap2LoopAsync()
        {
            try
            {
                if (ctsSnap2 == null) ctsSnap2 = new CancellationTokenSource();
                var ct = ctsSnap2.Token;
                var task = Task.Run(() =>
                {
                    Snap2Start();
                    var KeepRunning = true;
                    Snap2GaugeValue = 0;
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            //  KeepRunning = false;
                            break;
                        }

                        if (!Snap2Running)
                        {
                            ctsSnap2.Cancel();
                            break;
                        }

                        if (!Snap2Enabled)
                        {
                            ctsSnap2.Cancel();
                            break;
                        }

                        if (Snap2GaugeValue >= Snap2Loops)
                        {
                            ctsSnap2.Cancel();
                            break;
                        }

                        if (Snap2Pause)
                        {
                            Thread.Sleep(500);
                            continue;
                        }

                        // take snap
                        Snap2Trigger(true);
                        var swSnap = Stopwatch.StartNew();
                        while (swSnap.Elapsed.TotalMilliseconds < TimeSpan.FromSeconds(Snap2Timer).TotalMilliseconds)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                KeepRunning = false;
                                break;
                            }

                            Thread.Sleep(10);
                        }

                        Snap2Trigger(false);

                        // Delay
                        var swD = Stopwatch.StartNew();
                        while (swD.Elapsed.TotalMilliseconds < TimeSpan.FromSeconds(Snap2Delay).TotalMilliseconds)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                KeepRunning = false;
                                break;
                            }

                            Thread.Sleep(100);
                        }

                        Snap2GaugeValue++;
                    }
                }, ct);
                await task;
                task.Wait(ct);
                if (SkyServer.SnapPort2)
                {
                    Snap2Trigger(false);
                }

                Snap2Stop();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                Snap2Stop();
                OpenDialog(ex.Message);
            }
            finally
            {
                Snap2Running = false;
            }
        }

        #endregion

        #region Dialog

        private string _dialogMsg;
        public string DialogMsg
        {
            get => _dialogMsg;
            set
            {
                if (_dialogMsg == value) return;
                _dialogMsg = value;
                OnPropertyChanged();
            }
        }

        private string _dialogCaption;
        public string DialogCaption
        {
            get => _dialogCaption;
            set
            {
                if (_dialogCaption == value) return;
                _dialogCaption = value;
                OnPropertyChanged();
            }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                if (_isDialogOpen == value) return;
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _dialogContent;
        public object DialogContent
        {
            get => _dialogContent;
            set
            {
                if (_dialogContent == value) return;
                _dialogContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openDialogCommand;
        public ICommand OpenDialogCommand
        {
            get
            {
                var command = _openDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openDialogCommand = new RelayCommand(
                    param => OpenDialog(null)
                );
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (IsDialogOpen)
            {
                OpenDialogWin(msg, caption);
            }
            else
            {
                if (msg != null) DialogMsg = msg;
                DialogCaption = caption ?? Application.Current.Resources["diaDialog"].ToString();
                DialogContent = new DialogOK();
                IsDialogOpen = true;
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.UI,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        private void OpenDialogWin(string msg, string caption = null)
        {
            //Open as new window
            var bWin = new MessageControlV(caption, msg) { Owner = Application.Current.MainWindow };
            bWin.Show();
        }

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                var command = _clickOkDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickOkDialogCommand = new RelayCommand(
                    param => ClickOkDialog()
                );
            }
        }
        private void ClickOkDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _clickCancelDialogCommand;
        public ICommand ClickCancelDialogCommand
        {
            get
            {
                var command = _clickCancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickCancelDialogCommand = new RelayCommand(
                    param => ClickCancelDialog()
                );
            }
        }
        private void ClickCancelDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _runMessageDialog;
        public ICommand RunMessageDialogCommand
        {
            get
            {
                var dialog = _runMessageDialog;
                if (dialog != null)
                {
                    return dialog;
                }

                return _runMessageDialog = new RelayCommand(
                    param => ExecuteMessageDialog()
                );
            }
        }
        private async void ExecuteMessageDialog()
        {
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
        }

        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~SnapVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                SkyServer.StaticPropertyChanged -= PropertyChangedSkyServer;
                ctsSnap1?.Cancel();
                ctsSnap1?.Dispose();
                ctsSnap1 = null;
                ctsSnap2?.Cancel();
                ctsSnap2?.Dispose();
                ctsSnap2 = null;
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion
    }
}
