using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
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
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Hand Control Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    SetHCFlipsVisibility();

                    Title = Application.Current.Resources["hcHc"].ToString();
                    ScreenEnabled = SkyServer.IsMountRunning;
                    HcWinVisibility = false;
                    TopMost = true;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
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

        private bool _hcWinVisibility;
        public bool HcWinVisibility
        {
            get => _hcWinVisibility;
            set
            {
                if (_hcWinVisibility == value) return;
                _hcWinVisibility = value;
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
                    Device = MonitorDevice.Server,
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

        private void PropertyChangedSkySettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "HcSpeed":
                         HcSpeed = (double)SkySettings.HcSpeed;
                         break;
                     case "HcFlipEW":
                         FlipEW = SkySettings.HcFlipEW;
                         break;
                     case "HcFlipNS":
                         FlipNS = SkySettings.HcFlipNS;
                         break;
                     case "HcAntiRa":
                         HcAntiRa = SkySettings.HcAntiRa;
                         break;
                     case "HcAntiDec":
                         HcAntiDec = SkySettings.HcAntiDec;
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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

        #region Hand Controller

        private double _hcSpeed;
        public double HcSpeed
        {
            get
            {
                _hcSpeed = Convert.ToInt32(SkySettings.HcSpeed);
                return _hcSpeed;
            }
            set
            {
                var tmp = Convert.ToInt32(value);
                if (Math.Abs(_hcSpeed - tmp) < 0.0) return;
                _hcSpeed = tmp;
                SkySettings.HcSpeed = (SlewSpeed)tmp;
                Synthesizer.Speak(SkySettings.HcSpeed.ToString());
                OnPropertyChanged();
            }
        }

        public bool FlipNS
        {
            get => SkySettings.HcFlipNS;
            set
            {
                SkySettings.HcFlipNS = value;
                OnPropertyChanged();
            }
        }

        public bool FlipEW
        {
            get => SkySettings.HcFlipEW;
            set
            {
                SkySettings.HcFlipEW = value;
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
                SkySettings.HcAntiRa = value;
                OnPropertyChanged();
            }
        }

        public bool HcAntiDec
        {
            get => SkySettings.HcAntiDec;
            set
            {
                SkySettings.HcAntiDec = value;
                OnPropertyChanged();
            }
        }

        private void SetHCFlipsVisibility()
        {
            switch (HcMode)
            {
                case HCMode.Axes:
                    EWEnabled = true;
                    NSEnabled = true;
                    break;
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
                SetHCFlipsVisibility();
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
                    Device = MonitorDevice.Server,
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

        private ICommand _hcSpeedDownCommand;
        public ICommand HcSpeedDownCommand
        {
            get
            {
                var command = _hcSpeedDownCommand;
                if (command != null)
                {
                    return command;
                }

                return _hcSpeedDownCommand = new RelayCommand(
                    param => SpeedDownCommand()
                );
            }
        }
        private void SpeedDownCommand()
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                //_ctsSpiral?.Cancel();
                SkyServer.AbortSlew(true);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
            WindowStates = WindowState.Minimized;
        }

        private ICommand _maximizeWindowCommand;
        public ICommand MaximizeWindowCommand
        {
            get
            {
                var command = _maximizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _maximizeWindowCommand = new RelayCommand(
                    param => MaximizeWindow()
                );
            }
        }
        private void MaximizeWindow()
        {
            WindowStates = WindowStates != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
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
            WindowStates = WindowState.Normal;
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

        private WindowState _windowState;
        public WindowState WindowStates
        {
            get => _windowState;
            set
            {
                _windowState = value;
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
                    Synthesizer.Speak(Application.Current.Resources["1018MouseLock"].ToString());
                }
                else
                {
                    NativeMethods.ClipCursor(IntPtr.Zero);
                    IsDialogOpen = false;
                }
                OnPropertyChanged();
            }
        }

        private bool _raDecLockedMouse;
        public bool RaDecLockedMouse
        {
            get => _raDecLockedMouse;
            set
            {
                if (_raDecLockedMouse == value) return;
                _raDecLockedMouse = value;
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
                    Device = MonitorDevice.Server,
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
                    if (_skyTelescopeVM.SpiralOutCmd.CanExecute(null))
                        _skyTelescopeVM.SpiralOutCmd.Execute(null);
                }

                if (e.XButton2 == MouseButtonState.Pressed)
                {
                    if (_skyTelescopeVM.SpiralInCmd.CanExecute(null))
                        _skyTelescopeVM.SpiralInCmd.Execute(null);
                }

                if (e.MiddleButton != MouseButtonState.Pressed) return;
                RaDecLockedMouse = !RaDecLockedMouse;
                var axis = RaDecLockedMouse ? $"{Application.Current.Resources["topRa"]}" : $"{Application.Current.Resources["topDec"]}";
                if (!string.IsNullOrEmpty(axis)) Synthesizer.Speak(axis);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
                if (e == null) { return; }
                if (e.Delta > 0)
                {
                    if (HcSpeed >= 8)
                    {
                        HcSpeed = 8;
                        return;
                    }
                    HcSpeed += 1;
                }
                if (e.Delta >= 0) return;
                if (HcSpeed <= 1)
                {
                    HcSpeed = 1;
                    return;
                }
                HcSpeed -= 1;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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

        #endregion

        #region Spiral Window

        private ICommand _openSpiralWindowCmd;
        public ICommand OpenSpiralWindowCmd
        {
            get
            {
                var cmd = _openSpiralWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openSpiralWindowCmd = new RelayCommand(param => OpenSpiralWindow());
            }
        }
        private void OpenSpiralWindow()
        {
            try
            {
                var win = Application.Current.Windows.OfType<SpiralV>().FirstOrDefault();
                if (win != null) return;
                var bWin = new SpiralV();
                bWin.Show();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
                Device = MonitorDevice.Server,
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
            //_ctsSpiral?.Cancel();
            //_ctsSpiral?.Dispose();
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
