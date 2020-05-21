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
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ASCOM.DeviceInterface;
using GS.Server.Controls.Dialogs;
using GS.Server.SkyTelescope;

namespace GS.Server.Test
{
    public class TestVM : ObservableObject, IPageVM, IDisposable
    {
        public string TopName => "Test";
        public string BottomName => "Test";
        public int Uid => 7;

        private readonly SkyTelescopeVM _skyTelescopeVM;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private Oscillation _oscillation;

        public TestVM()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading FocuserVM" };
            MonitorLog.LogToMonitor(monitorItem);

            if (_skyTelescopeVM == null) _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;

            Intervals = new List<double>(Numbers.InclusiveRange(0, 300, 15));
            Interval = 15;

            StepLashs = new List<int>(Enumerable.Range(0, 21));
            StepLash = 0;

            LashList = new List<int>(Enumerable.Range(0, 200));

            IsRunning = false;


        }

        #region Backlash Test

        private ObservableCollection<OscillationResult> _oscillationList;

        public ObservableCollection<OscillationResult> OscillationList
        {
            get => _oscillationList;
            set
            {
                _oscillationList = value;
                OnPropertyChanged();
            }
        }

        private bool _isrunning;
        public bool IsRunning
        {
            get => _isrunning;
            set
            {
                _isrunning = value;
                IsEnabled = !value;
                OnPropertyChanged();
            }
        }
        private bool _isenabled;
        public bool IsEnabled
        {
            get => _isenabled;
            set
            {
                _isenabled = value;
                OnPropertyChanged();
            }
        }
        private int _prevLash;
        public int PrevLash
        {
            get => _prevLash;
            set
            {
                _prevLash = value;
                OnPropertyChanged();
            }

        }
        public List<int> StepLashs { get; set; }
        private int _stepLash;
        public int StepLash
        {
            get => _stepLash;
            set
            {
                _stepLash = value;
                OnPropertyChanged();
            }

        }

        public List<int> LashList { get; set; }

        public List<double> Intervals { get; set; }
        private double _interval;
        public double Interval
        {
            get => _interval;
            set
            {
                _interval = value;
                OnPropertyChanged();
            }
        }
        public int Lash
        {
            get => _skyTelescopeVM.DecBacklash;
            set
            {
                _skyTelescopeVM.DecBacklash = value;
                OnPropertyChanged();
            }

        }

        private ICommand _clickopencmd;
        public ICommand ClickOpenCmd
        {
            get
            {
                return _clickopencmd ?? (_clickopencmd = new RelayCommand(
                           param => ClickOpen()
                       ));
            }
        }
        private  void ClickOpen()
        {
            try
            {
                

            }
            catch (Exception ex)
            {
                IsRunning = false;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickstartcmd;
        public ICommand ClickStartCmd
        {
            get
            {
                return _clickstartcmd ?? (_clickstartcmd = new RelayCommand(
                           param => ClickStart()
                       ));
            }
        }
        private async void ClickStart()
        {
            try
            {
                if ((int) Interval == 0) return;
                _cts = new CancellationTokenSource();
                _ct = _cts.Token;
                IsRunning = true;
                var KeepRunning = true;
                OscillationList = new ObservableCollection<OscillationResult>();
                var task = Task.Run(() =>
                {
                    while (KeepRunning)
                    {
                        if (_ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            // ct.ThrowIfCancellationRequested();
                            KeepRunning = false;
                        }
                        else
                        {
                            //run for interval
                            _oscillation = new Oscillation();
                            var starttime = HiResDateTime.UtcNow;
                            MonitorLog.GetPulses = true;
                            SkyServer.MonitorPulse = true;
                            MonitorQueue.StaticPropertyChanged += PropertyChangedMonitor;

                            var stopwatch = Stopwatch.StartNew();
                            while (stopwatch.Elapsed < TimeSpan.FromSeconds(Interval))
                            {
                                //
                                if (_ct.IsCancellationRequested)
                                {
                                    KeepRunning = false;
                                    break;
                                }
                                Thread.Sleep(100);

                            }
                            stopwatch.Stop();

                            //stop interval
                            SkyServer.MonitorPulse = false;
                            MonitorLog.GetPulses = false;
                            MonitorQueue.StaticPropertyChanged -= PropertyChangedMonitor;
                            var endtime = HiResDateTime.UtcNow;

                            //process oscillations
                           var result = _oscillation.GetResult(GuideDirections.guideNorth, starttime, endtime);
                           if (result != null)
                            {
                                ThreadContext.InvokeOnUiThread(
                                    delegate { OscillationList.Add(result); }, _ct);

                                var monitorItem = new MonitorEntry
                                {
                                    Datetime = HiResDateTime.UtcNow,
                                    Device = MonitorDevice.Server,
                                    Category = MonitorCategory.Interface,
                                    Type = MonitorType.Information,
                                    Method = MethodBase.GetCurrentMethod().Name,
                                    Thread = Thread.CurrentThread.ManagedThreadId,
                                    Message =
                                        $"result:{Lash},{result.StartTime},{result.EndTime},{result.AvgStrength},{result.AvgStrength1},{result.Direction1},{result.Percent1},{result.AvgStrength2},{result.Direction2},{result.Percent2}"
                                };
                                MonitorLog.LogToMonitor(monitorItem);
                            }

                            _oscillation = null;
                        }
                    }
                }, _ct);
                await task;
                task.Wait(_ct);
                IsRunning = false;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickstopcmd;
        public ICommand ClickStopCmd
        {
            get
            {
                return _clickstopcmd ?? (_clickstopcmd = new RelayCommand(
                           param => ClickStop()
                       ));
            }
        }
        private void ClickStop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickcancelcmd;
        public ICommand ClickCancelCmd
        {
            get
            {
                return _clickcancelcmd ?? (_clickcancelcmd = new RelayCommand(
                           param => ClickCancel()
                       ));
            }
        }
        private void ClickCancel()
        {
            try
            {
               ClickStop();
               Lash = PrevLash;
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        private void PropertyChangedMonitor(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "PulseEntry":
                                if (_oscillation != null)
                                {
                                    var pulse = MonitorQueue.PulseEntry;
                                    var pair = new OscillationPair
                                    {
                                        Data = Math.Abs(pulse.Duration),
                                        TimeStamp = pulse.StartTime
                                    };
                                    switch (pulse.Axis)
                                    {
                                        case 0:
                                            pair.Direction = pulse.Rate < 0 ? GuideDirections.guideEast : GuideDirections.guideWest;
                                            break;
                                        case 1:
                                            pair.Direction = pulse.Rate < 0 ? GuideDirections.guideNorth : GuideDirections.guideSouth;
                                            break;
                                    }
                                    _oscillation.AddOscillation(pair);
                                    var monitorItem = new MonitorEntry
                                    {
                                        Datetime = HiResDateTime.UtcNow,
                                        Device = MonitorDevice.Server,
                                        Category = MonitorCategory.Interface,
                                        Type = MonitorType.Information,
                                        Method = MethodBase.GetCurrentMethod().Name,
                                        Thread = Thread.CurrentThread.ManagedThreadId,
                                        Message = $"Pair: {pair.Data},{pair.Direction},{pair.TimeStamp}"
                                    };
                                    MonitorLog.LogToMonitor(monitorItem);
                                }
                                break;
                        }
                    }, _ct);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
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
                return _openDialogCommand ?? (_openDialogCommand = new RelayCommand(
                           param => OpenDialog(null)
                       ));
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (msg != null) DialogMsg = msg;
            DialogCaption = caption ?? Application.Current.Resources["msgDialog"].ToString();
            DialogContent = new DialogOK();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                return _clickOkDialogCommand ?? (_clickOkDialogCommand = new RelayCommand(
                           param => ClickOkDialog()
                       ));
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
                return _clickCancelDialogCommand ?? (_clickCancelDialogCommand = new RelayCommand(
                           param => ClickCancelDialog()
                       ));
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
                return _runMessageDialog ?? (_runMessageDialog = new RelayCommand(
                           param => ExecuteMessageDialog()
                       ));
            }
        }
        private async void ExecuteMessageDialog()
        {
            //let's set up a little MVVM, cos that's what the cool kids are doing:
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
        ~TestVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
                _skyTelescopeVM?.Dispose();
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
