using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using NativeMethods = GS.Server.Helpers.NativeMethods;

namespace GS.Server.Windows
{
    public class HandControlVM : ObservableObject, IDisposable
    {
        #region Fields

        private readonly SkyTelescopeVM _skyTelescopeVM;

        #endregion

        public HandControlVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Hand Control Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;

                    SetHCFlipsVisability();
                    SpiralHcSpeeds = new List<int>(Enumerable.Range(1, 8));
                    SpiralPauses = new List<int>(Enumerable.Range(0, 61));

                    Title = Application.Current.Resources["lbHc"].ToString();
                    ScreenEnabled = SkyServer.IsMountRunning;
                    HcWinVisability = false;
                    TopMost = true;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region ViewModel

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enable or Disable screen items if connected
        /// </summary>
        private bool _screenEnabled;
        public bool ScreenEnabled
        {
            get => _screenEnabled;
            set
            {
                if (_screenEnabled == value) return;
                _screenEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _hcWinVisability;
        public bool HcWinVisability
        {
            get => _hcWinVisability;
            set
            {
                if (_hcWinVisability == value) return;
                _hcWinVisability = value;
                OnPropertyChanged();
            }
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
                         ScreenEnabled = SkyServer.IsMountRunning;
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        #endregion

        #region Hand Controller

        private double _hcspeed;
        public double HcSpeed
        {
            get
            {
                _hcspeed = (double)SkySettings.HcSpeed;
                return _hcspeed;
            }
            set
            {
                if (Math.Abs(_hcspeed - value) < 0.00001) return;
                if (Enum.IsDefined(typeof(SlewSpeed), Convert.ToInt32(value)) == false) return;
                _hcspeed = value;
                SkySettings.HcSpeed = (SlewSpeed)value;
                Synthesizer.Speak(SkySettings.HcSpeed.ToString());
                OnPropertyChanged();
            }
        }

        private bool _flipns;
        public bool FlipNS
        {
            get => _flipns;
            set
            {
                if (_flipns == value) return;
                _flipns = value;
                OnPropertyChanged();
            }
        }

        private bool _flipew;
        public bool FlipEW
        {
            get => _flipew;
            set
            {
                if (_flipew == value) return;
                _flipew = value;
                OnPropertyChanged();
            }
        }

        private bool _nsEnabled;
        public bool NSEnabled
        {
            get => _nsEnabled;
            set
            {
                if (_nsEnabled == value) return;
                _nsEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _ewEnabled;
        public bool EWEnabled
        {
            get => _ewEnabled;
            set
            {
                if (_ewEnabled == value) return;
                _ewEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool HcAntiRa
        {
            get => SkySettings.HcAntiRa;
            set
            {
                if (SkySettings.HcAntiRa == value) return;
                SkySettings.HcAntiRa = value;
                OnPropertyChanged();
            }
        }

        public bool HcAntiDec
        {
            get => SkySettings.HcAntiDec;
            set
            {
                if (SkySettings.HcAntiDec == value) return;
                SkySettings.HcAntiDec = value;
                OnPropertyChanged();
            }
        }

        private void SetHCFlipsVisability()
        {
            switch (HcMode)
            {
                case HCMode.Axes:
                    EWEnabled = true;
                    NSEnabled = true;
                    break;
                //case HCMode.Compass:
                //    EWVisability = false;
                //    NSVisability = false;
                //    break;
                case HCMode.Guiding:
                    EWEnabled = false;
                    NSEnabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public HCMode HcMode
        {
            get => SkySettings.HcMode;
            set
            {
                SkySettings.HcMode = value;
                SetHCFlipsVisability();
                OnPropertyChanged();
            }
        }

        private ICommand _hcSpeedupCommand;
        public ICommand HcSpeedupCommand
        {
            get
            {
                var command = _hcSpeedupCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcSpeedupCommand = new RelayCommand(
                    param => SpeedupCommand()
                );
            }
        }
        private void SpeedupCommand()
        {
            try
            {
                var currentspeed = HcSpeed;
                if (currentspeed < 8)
                {
                    HcSpeed++;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcSpeeddownCommand;
        public ICommand HcSpeeddownCommand
        {
            get
            {
                var command = _hcSpeeddownCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcSpeeddownCommand = new RelayCommand(
                    param => SpeeddownCommand()
                );
            }
        }
        private void SpeeddownCommand()
        {
            try
            {
                var currentspeed = HcSpeed;
                if (currentspeed > 0)
                {
                    HcSpeed--;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseDownLeftCommand;
        public ICommand HcMouseDownLeftCommand
        {
            get
            {
                var command = _hcMouseDownLeftCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownLeftCommand = new RelayCommand(param => HcMouseDownLeft());
            }
            set => _hcMouseDownLeftCommand = value;
        }
        private void HcMouseDownLeft()
        {
            try
            {
                if (SkyServer.AtPark)
                {
                    _skyTelescopeVM.BlinkParked();
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    return;
                }
                StartSlew(FlipEW && EWEnabled ? SlewDirection.SlewRight : SlewDirection.SlewLeft);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseUpLeftCommand;
        public ICommand HcMouseUpLeftCommand
        {
            get
            {
                var command = _hcMouseUpLeftCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseUpLeftCommand = new RelayCommand(param => HcMouseUpLeft());
            }
            set => _hcMouseUpLeftCommand = value;
        }
        private void HcMouseUpLeft()
        {
            try
            {
                StartSlew(SlewDirection.SlewNoneRa);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseDownRightCommand;
        public ICommand HcMouseDownRightCommand
        {
            get
            {
                var command = _hcMouseDownRightCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownRightCommand = new RelayCommand(param => HcMouseDownRight());
            }
            set => _hcMouseDownRightCommand = value;
        }
        private void HcMouseDownRight()
        {
            try
            {
                if (SkyServer.AtPark)
                {
                    _skyTelescopeVM.BlinkParked();
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    return;
                }
                StartSlew(FlipEW && EWEnabled ? SlewDirection.SlewLeft : SlewDirection.SlewRight);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseUpRightCommand;
        public ICommand HcMouseUpRightCommand
        {
            get
            {
                var command = _hcMouseUpRightCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseUpRightCommand = new RelayCommand(param => HcMouseUpRight());
            }
            set => _hcMouseUpRightCommand = value;
        }
        private void HcMouseUpRight()
        {
            try
            {
                StartSlew(SlewDirection.SlewNoneRa);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseDownUpCommand;
        public ICommand HcMouseDownUpCommand
        {
            get
            {
                var command = _hcMouseDownUpCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownUpCommand = new RelayCommand(param => HcMouseDownUp());
            }
            set => _hcMouseDownUpCommand = value;
        }
        private void HcMouseDownUp()
        {
            try
            {
                if (SkyServer.AtPark)
                {
                    _skyTelescopeVM.BlinkParked();
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    return;
                }
                StartSlew(FlipNS && NSEnabled ? SlewDirection.SlewDown : SlewDirection.SlewUp);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseUpUpCommand;
        public ICommand HcMouseUpUpCommand
        {
            get
            {
                var command = _hcMouseUpUpCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseUpUpCommand = new RelayCommand(param => HcMouseUpUp());
            }
            set => _hcMouseUpUpCommand = value;
        }
        private void HcMouseUpUp()
        {
            try
            {
                StartSlew(SlewDirection.SlewNoneDec);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseDownDownCommand;
        public ICommand HcMouseDownDownCommand
        {
            get
            {
                var command = _hcMouseDownDownCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownDownCommand = new RelayCommand(param => HcMouseDownDown());
            }
            set => _hcMouseDownDownCommand = value;
        }
        private void HcMouseDownDown()
        {
            try
            {
                if (SkyServer.AtPark)
                {
                    _skyTelescopeVM.BlinkParked();
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    return;
                }
                StartSlew(FlipNS && NSEnabled ? SlewDirection.SlewUp : SlewDirection.SlewDown);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseUpDownCommand;
        public ICommand HcMouseUpDownCommand
        {
            get
            {
                var command = _hcMouseUpDownCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseUpDownCommand = new RelayCommand(param => HcMouseUpDown());
            }
            set => _hcMouseUpDownCommand = value;
        }
        private void HcMouseUpDown()
        {
            try
            {
                StartSlew(SlewDirection.SlewNoneDec);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseDownStopCommand;
        public ICommand HcMouseDownStopCommand
        {
            get
            {
                var command = _hcMouseDownStopCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownStopCommand = new RelayCommand(param => HcMouseDownStop());
            }
            set => _hcMouseDownStopCommand = value;
        }
        private void HcMouseDownStop()
        {
            try
            {
                _ctsSpiral?.Cancel();
                SkyServer.AbortSlew(true);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _openHCWindowCmd;
        public ICommand OpenHCWindowCmd
        {
            get
            {
                var cmd = _openHCWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openHCWindowCmd = new RelayCommand(param => OpenHcWindow());
            }
        }
        private void OpenHcWindow()
        {
            try
            {
                //do nothing
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private static void StartSlew(SlewDirection direction)
        {
            if (SkyServer.AtPark)
            {
                return;
            }

            var HcMode = SkySettings.HcMode;
            var HcAntiDec = SkySettings.HcAntiDec;
            var HcAntiRa = SkySettings.HcAntiRa;
            var DecBacklash = SkySettings.DecBacklash;
            var RaBacklash = SkySettings.RaBacklash;

            var speed = SkySettings.HcSpeed;
            switch (direction)
            {
                case SlewDirection.SlewEast:
                case SlewDirection.SlewRight:
                    SkyServer.HcMoves(speed, SlewDirection.SlewEast, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                case SlewDirection.SlewWest:
                case SlewDirection.SlewLeft:
                    SkyServer.HcMoves(speed, SlewDirection.SlewWest, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                case SlewDirection.SlewNorth:
                case SlewDirection.SlewUp:
                    SkyServer.HcMoves(speed, SlewDirection.SlewNorth, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                case SlewDirection.SlewSouth:
                case SlewDirection.SlewDown:
                    SkyServer.HcMoves(speed, SlewDirection.SlewSouth, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                case SlewDirection.SlewNoneRa:
                    SkyServer.HcMoves(speed, SlewDirection.SlewNoneRa, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                case SlewDirection.SlewNoneDec:
                    SkyServer.HcMoves(speed, SlewDirection.SlewNoneDec, HcMode, HcAntiRa, HcAntiDec, RaBacklash, DecBacklash);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Window Info

        private bool _topMost;
        public bool TopMost
        {
            get => _topMost;
            set
            {
                if (_topMost == value) return;
                _topMost = value;
                OnPropertyChanged();
            }
        }

        private ICommand _minimizeWindowCommand;
        public ICommand MinimizeWindowCommand
        {
            get
            {
                var command = _minimizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _minimizeWindowCommand = new RelayCommand(
                    param => MinimizeWindow()
                );
            }
        }
        private void MinimizeWindow()
        {
            Windowstate = WindowState.Minimized;
        }

        private ICommand _maxmizeWindowCommand;
        public ICommand MaximizeWindowCommand
        {
            get
            {
                var command = _maxmizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _maxmizeWindowCommand = new RelayCommand(
                    param => MaxmizeWindow()
                );
            }
        }
        private void MaxmizeWindow()
        {
            Windowstate = Windowstate != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private ICommand _normalWindowCommand;
        public ICommand NormalWindowCommand
        {
            get
            {
                var command = _normalWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _normalWindowCommand = new RelayCommand(
                    param => NormalWindow()
                );
            }
        }
        private void NormalWindow()
        {
            Windowstate = WindowState.Normal;
        }

        private ICommand _openCloseWindowCmd;
        public ICommand OpenCloseWindowCmd
        {
            get
            {
                var cmd = _openCloseWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openCloseWindowCmd = new RelayCommand(
                    param => CloseWindow()
                );
            }
        }
        private void CloseWindow()
        {
            var win = Application.Current.Windows.OfType<HandControlV>().FirstOrDefault();
            win?.Close();
        }

        private WindowState _windowstate;
        public WindowState Windowstate
        {
            get => _windowstate;
            set
            {
                _windowstate = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Locked Mouse

        private bool _lockOn;
        public bool LockOn
        {
            get => _lockOn;
            set
            {
                _lockOn = value;
                if (value)
                {
                    HideMouse();
                    var msg = $"{Application.Current.Resources["msgMouseLock0"]}";
                    //msg += $"{Application.Current.Resources["msgMouseLock1"]}{Environment.NewLine}";
                    //msg += $"{Application.Current.Resources["msgMouseLock2"]}";
                    OpenDialog($"{msg}", $"{ Application.Current.Resources["capMouseLock"]}");
                }
                else
                {
                    NativeMethods.ClipCursor(IntPtr.Zero);
                    IsDialogOpen = false;
                }
                OnPropertyChanged();
            }
        }

        private bool _radecLockedMouse;
        public bool RaDecLockedMouse
        {
            get => _radecLockedMouse;
            set
            {
                if (_radecLockedMouse == value) return;
                _radecLockedMouse = value;
                OnPropertyChanged();
            }
        }

        private static void HideMouse()
        {
            var point = NativeMethods.GetCursorPosition();
            var r = new Rectangle((int)point.X, (int)point.Y, (int)point.X + 2, (int)point.Y + 2);
            NativeMethods.ClipCursor(ref r);
        }

        private ICommand _pressKeyDownCmd;
        public ICommand PressAnyKeyDownCmd
        {
            get
            {
                var cmd = _pressKeyDownCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_pressKeyDownCmd = new RelayCommand(
                    param => PressAnyKeyDown()
                ));
            }
        }
        private void PressAnyKeyDown()
        {
            try
            {
                LockOn = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _clickLockedMouseDownCmd;
        public ICommand ClickLockedMouseDownCmd
        {
            get
            {
                var cmd = _clickLockedMouseDownCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickLockedMouseDownCmd = new RelayCommand(
                    param => ClickLockedMouseDown((MouseEventArgs)param)
                ));
            }
        }
        private void ClickLockedMouseDown(MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (!LockOn)
                    {
                        LockOn = true;
                        return;
                    }

                    if (RaDecLockedMouse)
                    {
                        HcMouseDownLeft();
                    }
                    else
                    {
                        HcMouseDownUp();
                    }
                }

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (RaDecLockedMouse)
                    {
                        HcMouseDownRight();
                    }
                    else
                    {
                        HcMouseDownDown();
                    }
                }

                if (e.XButton1 == MouseButtonState.Pressed)
                {
                    _ = SpiralDownCmd();
                }

                if (e.XButton2 == MouseButtonState.Pressed)
                {
                    SkyServer.AbortSlew(true);
                }

                if (e.MiddleButton != MouseButtonState.Pressed) return;
                RaDecLockedMouse = !RaDecLockedMouse;
                var axis = RaDecLockedMouse ? $"{Application.Current.Resources["lbTopRa"]}" : $"{Application.Current.Resources["lbTopDec"]}";
                if (!string.IsNullOrEmpty(axis)) Synthesizer.Speak(axis);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _clickLockedMouseUpCmd;
        public ICommand ClickLockedMouseUpCmd
        {
            get
            {
                var cmd = _clickLockedMouseUpCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickLockedMouseUpCmd = new RelayCommand(
                    param => ClickLockedMouseUp((MouseEventArgs)param)
                ));
            }
        }
        private void ClickLockedMouseUp(MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    if (RaDecLockedMouse)
                    {
                        HcMouseUpLeft();
                    }
                    else
                    {
                        HcMouseUpUp();
                    }
                }

                if (e.RightButton == MouseButtonState.Released)
                {
                    if (RaDecLockedMouse)
                    {
                        HcMouseUpRight();
                    }
                    else
                    {
                        HcMouseUpDown();
                    }
                }

                if (e.XButton1 == MouseButtonState.Released)
                {
                    SpiralUpCmd();
                }

                if (e.XButton2 == MouseButtonState.Released)
                {

                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _scrollMouseWheelCmd;
        public ICommand ScrollMouseWheelCmd
        {
            get
            {
                var cmd = _scrollMouseWheelCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _scrollMouseWheelCmd = new RelayCommand(
                    param => ScrollMouseWheel((MouseWheelEventArgs)param));
            }
        }
        private void ScrollMouseWheel(MouseWheelEventArgs e)
        {
            try
            {
                if(e == null) { return; }
                if (e.Delta > 0)
                {
                    HcSpeed += 1; 
                }
                if (e.Delta >= 0) return;
                HcSpeed -= 1; 
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        #endregion

        #region Spiral Search

        private CancellationTokenSource _ctsSpiral;
        private CancellationToken _ctSpiral;

        public List<int> SpiralHcSpeeds { get; }
        public List<int> SpiralPauses { get; }

        private int _spiralFov;
        public int SpiralFov
        {
            get
            {
                _spiralFov = SkySettings.SpiralFov;
                return _spiralFov;
            }
            set
            {
                if (_spiralFov == value) return;
                _spiralFov = value;
                SkySettings.SpiralFov = value;
                OnPropertyChanged();
            }
        }

        private int _spiralSpeed;
        public int SpiralSpeed
        {
            get
            {
                _spiralSpeed = SkySettings.SpiralSpeed;
                return _spiralSpeed;
            }
            set
            {
                if (_spiralSpeed == value) return;
                _spiralSpeed = value;
                SkySettings.SpiralSpeed = value;
                OnPropertyChanged();
            }
        }

        private int _spiralPause;
        public int SpiralPause
        {
            get
            {
                _spiralPause = SkySettings.SpiralPause;
                return _spiralPause;
            }
            set
            {
                if (_spiralPause == value) return;
                _spiralPause = value;
                SkySettings.SpiralPause = value;
                OnPropertyChanged();
            }
        }

        private bool _isSpiralSearching;
        public bool IsSpiralSearching
        {
            get => _isSpiralSearching;
            set
            {
                if (_isSpiralSearching == value) return;
                _isSpiralSearching = value;
                OnPropertyChanged();
            }
        }

        private ICommand _hcMouseDownSpiralCmd;
        public ICommand HcMouseDownSpiralCmd
        {
            get
            {
                var command = _hcMouseDownSpiralCmd;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseDownSpiralCmd = new RelayCommand(
                    async param => await SpiralDownCmd()
                );
            }
        }
        private async Task SpiralDownCmd()
        {
            try
            {
                _ctsSpiral?.Cancel();
                _ctsSpiral = new CancellationTokenSource();
                _ctSpiral = _ctsSpiral.Token;
                await Task.Run(() => SpiralSearch(_ctSpiral), _ctSpiral);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private ICommand _hcMouseUpSpiralCmd;
        public ICommand HcMouseUpSpiralCmd
        {
            get
            {
                var command = _hcMouseUpSpiralCmd;
                if (command != null)
                {
                    return command;
                }

                return _hcMouseUpSpiralCmd = new RelayCommand(
                    param => SpiralUpCmd()
                );
            }
        }
        private void SpiralUpCmd()
        {
            try
            {
                _ctsSpiral?.Cancel();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private bool _isHcSettingsDialogOpen;
        public bool IsHcSettingsDialogOpen
        {
            get => _isHcSettingsDialogOpen;
            set
            {
                if (_isHcSettingsDialogOpen == value) return;
                _isHcSettingsDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _hcSettingsContent;
        public object HcSettingsContent
        {
            get => _hcSettingsContent;
            set
            {
                if (_hcSettingsContent == value) return;
                _hcSettingsContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openHcSettingsDialogCmd;
        public ICommand OpenHcSettingsDialogCmd
        {
            get
            {
                var cmd = _openHcSettingsDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openHcSettingsDialogCmd = new RelayCommand(
                    param => OpenHcSettings()
                );
            }
        }
        private void OpenHcSettings()
        {
            try
            {
                HcSettingsContent = new HcSettingsDialog();
                IsHcSettingsDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }

        }

        private ICommand _acceptHcSettingsDialogCmd;
        public ICommand AcceptHcSettingsDialogCmd
        {
            get
            {
                var cmd = _acceptHcSettingsDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _acceptHcSettingsDialogCmd = new RelayCommand(
                    param => AcceptHcSettings()
                );
            }
        }
        private void AcceptHcSettings()
        {
            try
            {
                IsHcSettingsDialogOpen = false;

            }
            catch (Exception ex)
            {
                IsHcSettingsDialogOpen = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
        }

        private async Task SpiralSearch(CancellationToken _ctSpiral)
        {
            var isTracking = SkyServer.Tracking;
            SkyServer.TrackingSpeak = false;
            IsSpiralSearching = true;
            var speed = SkyServer.GetSlewSpeed(SkySettings.HcSpeed);
            var fov = SkySettings.SpiralFov;
            var pause = SkySettings.SpiralPause;

            try
            {
                SkyServer.AbortSlew(false);
                var count = 0;

                while (!_ctSpiral.IsCancellationRequested)
                {
                    count++;
                    Synthesizer.Speak(Application.Current.Resources["msgSearching"].ToString());

                    // move up
                    for (var i = 0; i < count; i++)
                    {
                        if (_ctSpiral.IsCancellationRequested) return;
                        HcMouseDownUp();
                        var decdist = fov / (speed * 3600);
                        var stepms = (int)(decdist * 1000);
                        Debug.WriteLine($"Up: {decdist}, {stepms}, {count}");
                        await Tasks.DelayHandler(stepms, _ctSpiral);
                        HcMouseUpUp();
                        if (_ctSpiral.IsCancellationRequested) return;
                        if (pause <= 0) continue;
                        SkyServer.TrackingSpeak = false;
                        SkyServer.Tracking = false;
                        SkyServer.Tracking = isTracking;
                        await Tasks.DelayHandler(pause * 1000, _ctSpiral);
                    }

                    // move right
                    for (var i = 0; i < count; i++)
                    {
                        if (_ctSpiral.IsCancellationRequested) return;
                        HcMouseDownRight();
                        var rastep = fov * (1 / Math.Cos(Units.Deg2Rad1(SkyServer.Declination)));
                        var radist = rastep / (speed * 3600);
                        var stepms = (int)(radist * 1000);
                        Debug.WriteLine($"Right: {rastep}, {radist}, {stepms}, {count}");
                        await Tasks.DelayHandler(stepms, _ctSpiral);
                        HcMouseUpRight();
                        if (_ctSpiral.IsCancellationRequested) return;
                        if (pause <= 0) continue;
                        SkyServer.TrackingSpeak = false;
                        SkyServer.Tracking = false;
                        SkyServer.Tracking = isTracking;
                        await Tasks.DelayHandler(pause * 1000, _ctSpiral);
                    }

                    count++;

                    // move down
                    for (var i = 0; i < count; i++)
                    {
                        if (_ctSpiral.IsCancellationRequested) return;
                        HcMouseDownDown();
                        var decdist = fov / (speed * 3600);
                        var stepms = (int)(decdist * 1000);
                        Debug.WriteLine($"Down: {decdist}, {stepms}, {count}");
                        await Tasks.DelayHandler(stepms, _ctSpiral);
                        HcMouseUpDown();
                        if (_ctSpiral.IsCancellationRequested) return;
                        if (pause <= 0) continue;
                        SkyServer.TrackingSpeak = false;
                        SkyServer.Tracking = false;
                        SkyServer.Tracking = isTracking;
                        await Tasks.DelayHandler(pause * 1000, _ctSpiral);
                    }

                    // move left
                    for (var i = 0; i < count; i++)
                    {
                        if (_ctSpiral.IsCancellationRequested) return;
                        HcMouseDownLeft();
                        var rastep = fov * (1 / Math.Cos(Units.Deg2Rad1(SkyServer.Declination)));
                        var radist = rastep / (speed * 3600);
                        var stepms = (int)(radist * 1000);
                        Debug.WriteLine($"Left: {rastep}, {radist}, {stepms}, {count}");
                        await Tasks.DelayHandler(stepms, _ctSpiral);
                        HcMouseUpLeft();
                        if (_ctSpiral.IsCancellationRequested) return;
                        if (pause <= 0) continue;
                        SkyServer.TrackingSpeak = false;
                        SkyServer.Tracking = false;
                        SkyServer.Tracking = isTracking;
                        await Tasks.DelayHandler(pause * 1000, _ctSpiral);
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["Error"]}");
            }
            finally
            {
                SkyServer.TrackingSpeak = false;
                SkyServer.Tracking = false;
                SkyServer.Tracking = isTracking;
                SkyServer.TrackingSpeak = true;
                IsSpiralSearching = false;
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
            LockOn = false;
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
            _ctsSpiral?.Cancel();
            _ctsSpiral?.Dispose();
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~HandControlVM()
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
            }
            NativeMethods.ClipCursor(IntPtr.Zero);
        }
        #endregion
    }
}
