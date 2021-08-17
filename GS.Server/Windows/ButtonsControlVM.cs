using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using ASCOM.DeviceInterface;
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
    public class ButtonsControlVM : ObservableObject, IDisposable
    {
        #region Fields

        private readonly SkyTelescopeVM _skyTelescopeVM;

        #endregion

        public ButtonsControlVM()
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
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    Title = "GS";
                    ScreenEnabled = SkyServer.IsMountRunning;
                    ButtonsWinVisibility = false;
                    TopMost = true;

                    ParkSelection = ParkPositions.FirstOrDefault();
                    ParkSelectionSetting = ParkPositions.FirstOrDefault();
                    AtPark = SkyServer.AtPark;
                    IsHome = SkyServer.IsHome;
                    IsTracking = SkyServer.Tracking;
                    TrackingRate = SkySettings.TrackingRate;
                    _skyTelescopeVM.TrackingRate = SkySettings.TrackingRate;

                    AutoHomeLimits = new List<int>(Enumerable.Range(20, 160));
                    DecOffsets = new List<int>() { 0, -90, 90 };
                    AutoHomeEnabled = SkyServer.CanHomeSensor;
                    AutoHomeProgressBar = SkyServer.AutoHomeProgressBar;
                    PecShow = SkyServer.PecShow;
                    PecOn = SkyServer.PecOn;
                    SchedulerShow = false;
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
                     case "AtPark":
                         AtPark = SkyServer.AtPark;
                         break;
                     case "AutoHomeProgressBar":
                         AutoHomeProgressBar = SkyServer.AutoHomeProgressBar;
                         break;
                     case "CanHomeSensor":
                         AutoHomeEnabled = SkyServer.CanHomeSensor;
                         break;
                     case "IsHome":
                         IsHome = SkyServer.IsHome;
                         break;
                     case "IsMountRunning":
                         ScreenEnabled = SkyServer.IsMountRunning;
                         break;
                     case "Tracking":
                         IsTracking = SkyServer.Tracking;
                         break;
                     case "ParkSelected":
                         ParkSelection = SkyServer.ParkSelected;
                         break;
                     case "PecOn":
                         PecOn = SkyServer.PecOn;
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
                     case "TrackingRate":
                         TrackingRate = SkySettings.TrackingRate;
                         break;
                     case "HcSpeed":
                         //HcSpeed = (double)SkySettings.HcSpeed;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        public void CloseDialogs(bool screen)
        {
            if (screen)
            {
                ScreenEnabled = false;
                return;
            }

            IsDialogOpen = false;
            IsAutoHomeDialogOpen = false;
            IsFlipDialogOpen = false;
            IsHomeResetDialogOpen = false;
           // IsRaGoToDialogOpen = false;
           // IsRaGoToSyncDialogOpen = false;
            IsSchedulerDialogOpen = false;
           // IsLimitDialogOpen = false;
           // IsPPecDialogOpen = false;
            IsParkAddDialogOpen = false;
            IsParkDeleteDialogOpen = false;
           // IsGpsDialogOpen = false;
           // IsCdcDialogOpen = false;
            //IsHcSettingsDialogOpen = false;

            ScreenEnabled = SkyServer.IsMountRunning;
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
            var win = Application.Current.Windows.OfType<ButtonsControlV>().FirstOrDefault();
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

        #region Button Control

        public bool _atPark;
        public bool AtPark
        {
            get => _atPark;
            set
            {
                _atPark = value;
                ParkButtonContent = value ? Application.Current.Resources["btnUnPark"].ToString() : Application.Current.Resources["btnPark"].ToString();
                ParkBadgeContent = value ? SkySettings.ParkName : "";
                OnPropertyChanged();
            }
        }

        private string _parkButtonContent;
        public string ParkButtonContent
        {
            get => _parkButtonContent;
            set
            {
                if (ParkButtonContent == value) return;
                _parkButtonContent = value;
                OnPropertyChanged();
            }
        }

        public bool _isHome;
        public bool IsHome
        {
            get => _isHome;
            set
            {
                if (IsHome == value) return;
                _isHome = value;
                HomeBadgeContent = value ? Application.Current.Resources["btnBadgeHome"].ToString() : "";
                OnPropertyChanged();
            }
        }

        private bool _isTracking;
        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                if (IsTracking == value) return;
                _isTracking = value;
                TrackingBadgeContent = value ? Application.Current.Resources["btnHintTracking"].ToString() : "";
                OnPropertyChanged();
            }
        }

        private DriveRates _trackingRate;
        public DriveRates TrackingRate
        {
            get => _trackingRate;
            set
            {
                if (_trackingRate == value) return;
                _trackingRate = value;
                SkySettings.TrackingRate = value;
                _skyTelescopeVM.TrackingRate = SkySettings.TrackingRate;
                if (SkyServer.Tracking)
                {
                    SkyServer.TrackingSpeak = false;
                    SkyServer.Tracking = false;
                    SkyServer.Tracking = true;
                    SkyServer.TrackingSpeak = true;
                }
                OnPropertyChanged();
            }
        }

        private bool _buttonsWinVisibility;
        public bool ButtonsWinVisibility
        {
            get => _buttonsWinVisibility;
            set
            {
                if (_buttonsWinVisibility == value) return;
                _buttonsWinVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool _pecShow;
        /// <summary>
        /// sets up bool to load a test tab
        /// </summary>
        public bool PecShow
        {
            get => _pecShow;
            set
            {
                if (_pecShow == value) { return; }
                _pecShow = value;
                OnPropertyChanged();
            }
        }

        private bool _schedulerShow;
        public bool SchedulerShow
        {
            get => _schedulerShow;
            set
            {
                if (_schedulerShow == value) { return; }
                _schedulerShow = value;
                OnPropertyChanged();
            }
        }

        private bool _pecOn;
        public bool PecOn
        {
            get => _pecOn;
            set
            {
                _pecOn = value;
                if (SkyServer.PecOn) { _skyTelescopeVM.PecState = true; }
                if (!SkyServer.PPecOn && !SkyServer.PecOn) { _skyTelescopeVM.PecState = false; }
                PecBadgeContent = SkyServer.PecOn ? Application.Current.Resources["PecBadge"].ToString() : "";
            }
        }

        private List<ParkPosition> _parkPositions;
        public List<ParkPosition> ParkPositions
        {
            get => SkySettings.ParkPositions;
            set
            {
                if (_parkPositions == value) return;
                _parkPositions = value;
                SkySettings.ParkPositions = value;
                OnPropertyChanged();
            }
        }

        private ParkPosition _parkSelection;
        public ParkPosition ParkSelection
        {
            get => _parkSelection;
            set
            {
                if (_parkSelection == value) return;

                var found = ParkPositions.Find(x => x.Name == value.Name && Math.Abs(x.X - value.X) <= 0 && Math.Abs(x.Y - value.Y) <= 0);
                if (found == null) // did not find match in list
                {
                    ParkPositions.Add(value);
                    _parkSelection = value;
                    SkyServer.ParkSelected = value;
                }
                else
                {
                    _parkSelection = found;
                    SkyServer.ParkSelected = found;
                }
                OnPropertyChanged();
            }
        }

        private ParkPosition _parkSelectionSetting;
        public ParkPosition ParkSelectionSetting
        {
            get => _parkSelectionSetting;
            set
            {
                if (_parkSelectionSetting == value) return;
                _parkSelectionSetting = value;
                OnPropertyChanged();
            }
        }

        private string _parkNewName;
        public string ParkNewName
        {
            get => _parkNewName;
            set
            {
                if (_parkNewName == value) return;
                _parkNewName = value;
                OnPropertyChanged();
            }
        }

        private string _parkName;
        public string ParkName
        {
            get => SkySettings.ParkName;
            set
            {
                if (_parkName == value) return;
                _parkName = value;
                SkySettings.ParkName = value;
                OnPropertyChanged();
            }
        }

        private bool _isParkAddDialogOpen;
        public bool IsParkAddDialogOpen
        {
            get => _isParkAddDialogOpen;
            set
            {
                if (_isParkAddDialogOpen == value) return;
                _isParkAddDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _parkAddContent;
        public object ParkAddContent
        {
            get => _parkAddContent;
            set
            {
                if (_parkAddContent == value) return;
                _parkAddContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openParkAddDialogCommand;
        public ICommand OpenParkAddDialogCommand
        {
            get
            {
                var command = _openParkAddDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openParkAddDialogCommand = new RelayCommand(
                    param => OpenParkAddDialog()
                );
            }
        }
        private void OpenParkAddDialog()
        {
            try
            {
                ParkNewName = null;
                ParkAddContent = new ParkAddDialog();
                IsParkAddDialogOpen = true;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _acceptParkAddDialogCommand;
        public ICommand AcceptParkAddDialogCommand
        {
            get
            {
                var command = _acceptParkAddDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptParkAddDialogCommand = new RelayCommand(
                    param => AcceptParkAddDialog()
                );
            }
        }
        private void AcceptParkAddDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (string.IsNullOrEmpty(ParkNewName)) return;
                    var pp = new ParkPosition { Name = ParkNewName.Trim() };
                    ParkPositions.Add(pp);
                    SkySettings.ParkPositions = ParkPositions;
                    ParkSelectionSetting = pp;
                    ParkSelection = ParkPositions.FirstOrDefault();
                    IsParkAddDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelParkAddDialogCommand;
        public ICommand CancelParkAddDialogCommand
        {
            get
            {
                var command = _cancelParkAddDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelParkAddDialogCommand = new RelayCommand(
                    param => CancelParkAddDialog()
                );
            }
        }
        private void CancelParkAddDialog()
        {
            try
            {
                IsParkAddDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private bool _isParkDeleteDialogOpen;
        public bool IsParkDeleteDialogOpen
        {
            get => _isParkDeleteDialogOpen;
            set
            {
                if (_isParkDeleteDialogOpen == value) return;
                _isParkDeleteDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _parkDeleteContent;
        public object ParkDeleteContent
        {
            get => _parkDeleteContent;
            set
            {
                if (_parkDeleteContent == value) return;
                _parkDeleteContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openParkDeleteDialogCommand;
        public ICommand OpenParkDeleteDialogCommand
        {
            get
            {
                var command = _openParkDeleteDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openParkDeleteDialogCommand = new RelayCommand(
                    param => OpenParkDeleteDialog()
                );
            }
        }
        private void OpenParkDeleteDialog()
        {
            try
            {
                ParkDeleteContent = new ParkDeleteDialog();
                IsParkDeleteDialogOpen = true;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _acceptParkDeleteDialogCommand;
        public ICommand AcceptParkDeleteDialogCommand
        {
            get
            {
                var command = _acceptParkDeleteDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptParkDeleteDialogCommand = new RelayCommand(
                    param => AcceptParkDeleteDialog()
                );
            }
        }
        private void AcceptParkDeleteDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (ParkSelectionSetting == null) return;
                    //if (ParkPositions.Count == 1) return;
                    ParkPositions.Remove(ParkSelectionSetting);
                    SkySettings.ParkPositions = ParkPositions;
                    ParkSelectionSetting = ParkPositions.FirstOrDefault();
                    ParkSelection = ParkPositions.FirstOrDefault();
                    IsParkDeleteDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelParkDeleteDialogCommand;
        public ICommand CancelParkDeleteDialogCommand
        {
            get
            {
                var command = _cancelParkDeleteDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelParkDeleteDialogCommand = new RelayCommand(
                    param => CancelParkDeleteDialog()
                );
            }
        }
        private void CancelParkDeleteDialog()
        {
            try
            {
                IsParkDeleteDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _clickParkCmd;
        public ICommand ClickParkCommand
        {
            get
            {
                var command = _clickParkCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickParkCmd = new RelayCommand(
                    param => ClickPark()
                );
            }
        }
        private void ClickPark()
        {
            try
            {
                using (new WaitCursor())
                {
                    var parked = SkyServer.AtPark;
                    if (parked)
                    {
                        SkyServer.AtPark = false;
                        SkyServer.Tracking = true;
                    }
                    else
                    {
                        SkyServer.ParkSelected = ParkSelection;
                        SkyServer.GoToPark();
                    }
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"{parked}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private string _parkBadgeContent;
        public string ParkBadgeContent
        {
            get => _parkBadgeContent;
            set
            {
                if (ParkBadgeContent == value) return;
                _parkBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private string _homeBadgeContent;
        public string HomeBadgeContent
        {
            get => _homeBadgeContent;
            set
            {
                if (HomeBadgeContent == value) return;
                _homeBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private string _pecBadgeContent;
        public string PecBadgeContent
        {
            get => _pecBadgeContent;
            set
            {
                if (_pecBadgeContent == value) return;
                _pecBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private string _trackingBadgeContent;
        public string TrackingBadgeContent
        {
            get => _trackingBadgeContent;
            set
            {
                if (TrackingBadgeContent == value) return;
                _trackingBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickHomeCmd;
        public ICommand ClickHomeCommand
        {
            get
            {
                var command = _clickHomeCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickHomeCmd = new RelayCommand(
                    param => ClickHome()
                );
            }
        }
        private void ClickHome()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (SkyServer.AtPark)
                    {
                        _skyTelescopeVM.BlinkParked();
                        Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                        return;
                    }
                    SkyServer.GoToHome();
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _clickStopCmd;
        public ICommand ClickStopCommand
        {
            get
            {
                var command = _clickStopCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickStopCmd = new RelayCommand(
                    param => ClickStop()
                );
            }
        }
        private void ClickStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.IsMountRunning) return;
                    SkyServer.StopAxes();
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _clickTrackingCmd;
        public ICommand ClickTrackingCommand
        {
            get
            {
                var command = _clickTrackingCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickTrackingCmd = new RelayCommand(
                    param => ClickTracking()
                );
            }
        }
        private void ClickTracking()
        {
            try
            {
                using (new WaitCursor())
                {
                    var istracking = SkyServer.Tracking;
                    if (!istracking && SkyServer.AtPark)
                    {
                        SkyServer.AtPark = false;
                    }
                    SkyServer.Tracking = !SkyServer.Tracking;
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

       
        private bool _isFlipDialogOpen;
        public bool IsFlipDialogOpen
        {
            get => _isFlipDialogOpen;
            set
            {
                if (_isFlipDialogOpen == value) return;
                _isFlipDialogOpen = value;
                _skyTelescopeVM.CloseDialogs(value);
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _flipContent;
        public object FlipContent
        {
            get => _flipContent;
            set
            {
                if (_flipContent == value) return;
                _flipContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openFlipDialogCommand;
        public ICommand OpenFlipDialogCommand
        {
            get
            {
                var command = _openFlipDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openFlipDialogCommand = new RelayCommand(
                    param => OpenFlipDialog()
                );
            }
        }
        private void OpenFlipDialog()
        {
            try
            {
                FlipContent = new FlipDialog();
                IsFlipDialogOpen = true;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _acceptFlipDialogCommand;
        public ICommand AcceptFlipDialogCommand
        {
            get
            {
                var command = _acceptFlipDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptFlipDialogCommand = new RelayCommand(
                    param => AcceptFlipDialog()
                );
            }
        }
        private void AcceptFlipDialog()
        {
            try
            {
                if (!SkyServer.IsMountRunning) return;
                var sop = SkyServer.SideOfPier;
                switch (sop)
                {
                    case PierSide.pierEast:
                        SkyServer.SideOfPier = PierSide.pierWest;
                        break;
                    case PierSide.pierUnknown:
                        OpenDialog($"PierSide: {PierSide.pierUnknown}");
                        break;
                    case PierSide.pierWest:
                        SkyServer.SideOfPier = PierSide.pierEast;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                IsFlipDialogOpen = false;
            }
            catch (Exception ex)
            {
                IsFlipDialogOpen = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelFlipDialogCommand;
        public ICommand CancelFlipDialogCommand
        {
            get
            {
                var command = _cancelFlipDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelFlipDialogCommand = new RelayCommand(
                    param => CancelFlipDialog()
                );
            }
        }
        private void CancelFlipDialog()
        {
            try
            {
                IsFlipDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private string _schedulerBadgeContent;
        public string SchedulerBadgeContent
        {
            get => _schedulerBadgeContent;
            set
            {
                if (_schedulerBadgeContent == value) return;
                _schedulerBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private bool _isSchedulerDialogOpen;
        public bool IsSchedulerDialogOpen
        {
            get => _isSchedulerDialogOpen;
            set
            {
                if (_isSchedulerDialogOpen == value) return;
                _isSchedulerDialogOpen = value;
                _skyTelescopeVM.CloseDialogs(value);
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _schedulerContent;
        public object SchedulerContent
        {
            get => _schedulerContent;
            set
            {
                if (_schedulerContent == value) return;
                _schedulerContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openSchedulerDialogCmd;
        public ICommand OpenSchedulerDialogCmd
        {
            get
            {
                var cmd = _openSchedulerDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openSchedulerDialogCmd = new RelayCommand(
                    param => OpenSchedulerDialog()
                );
            }
        }
        private void OpenSchedulerDialog()
        {
            try
            {
                if (_skyTelescopeVM.FutureParkDate == null)
                {
                    _skyTelescopeVM.FutureParkDate = DateTime.Now + TimeSpan.FromSeconds(60);
                }
                if (_skyTelescopeVM.FutureParkTime == null)
                {
                    _skyTelescopeVM.FutureParkTime = $"{DateTime.Now + TimeSpan.FromSeconds(60):HH:mm}";
                }

                FutureParkDate = _skyTelescopeVM.FutureParkDate;
                FutureParkTime = _skyTelescopeVM.FutureParkTime;
                ScheduleParkOn = _skyTelescopeVM.ScheduleParkOn;

                SchedulerContent = new SchedulerDialog();
                IsSchedulerDialogOpen = true;

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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _acceptSchedulerDialogCmd;
        public ICommand AcceptSchedulerDialogCmd
        {
            get
            {
                var cmd = _acceptSchedulerDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _acceptSchedulerDialogCmd = new RelayCommand(
                    param => AcceptSchedulerDialog()
                );
            }
        }
        private void AcceptSchedulerDialog()
        {
            try
            {
                IsSchedulerDialogOpen = false;

            }
            catch (Exception ex)
            {
                IsSchedulerDialogOpen = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelSchedulerDialogCmd;
        public ICommand CancelSchedulerDialogCmd
        {
            get
            {
                var cmd = _cancelSchedulerDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _cancelSchedulerDialogCmd = new RelayCommand(
                    param => CancelSchedulerDialog()
                );
            }
        }
        private void CancelSchedulerDialog()
        {
            try
            {
                IsSchedulerDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private bool _scheduleParkOn;
        public bool ScheduleParkOn
        {
            get => _scheduleParkOn;
            set
            {
                if (value)
                {
                    _skyTelescopeVM.FutureParkDate = FutureParkDate;
                    _skyTelescopeVM.FutureParkTime = FutureParkTime;
                    _skyTelescopeVM.ScheduleParkOn = true;
                }
                else
                {
                    _skyTelescopeVM.ScheduleParkOn = false;
                }
                _scheduleParkOn = value;
                OnPropertyChanged();
            }
        }

        private string _futureParkTime;
        public string FutureParkTime
        {
            get => _futureParkTime;
            set
            {
                if (_futureParkTime == value) return;
                ScheduleParkOn = false;
                _futureParkTime = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _futureParkDate;
        public DateTime? FutureParkDate
        {
            get => _futureParkDate;
            set
            {
                if (_futureParkDate == value) return;
                ScheduleParkOn = false;
                _futureParkDate = value;
                OnPropertyChanged();
            }
        }


        private ICommand _clickPecOnCmd;
        public ICommand ClickPecOnCmd
        {
            get
            {
                var command = _clickPecOnCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickPecOnCmd = new RelayCommand(
                    param => ClickPecOn()
                );
            }
        }
        private void ClickPecOn()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.PecOn = !SkySettings.PecOn;
                }

            }
            catch (Exception ex)
            {
                SkyServer.SkyErrorHandler(ex);
            }
        }

        private ICommand _openButtonsWindowCmd;
        public ICommand OpenButtonsWindowCmd
        {
            get
            {
                var cmd = _openButtonsWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openButtonsWindowCmd = new RelayCommand(param => OpenButtonsWindow());
            }
        }
        private void OpenButtonsWindow()
        {
            try
            {
                var win = Application.Current.Windows.OfType<ButtonsControlV>().FirstOrDefault();
                if (win != null) return;
                var bWin = new ButtonsControlV();
                bWin.Show();
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }
        #endregion

        #region ReSync Dialog

        private ReSyncMode _syncMode;
        public ReSyncMode SyncMode
        {
            get => _syncMode;
            set
            {
                if (_syncMode == value) { return; }
                _syncMode = value;
                OnPropertyChanged();
            }
        }

        private ParkPosition _reSyncParkSelection;
        public ParkPosition ReSyncParkSelection
        {
            get => _reSyncParkSelection;
            set
            {
                if (_reSyncParkSelection == value) { return; }

                var found = ParkPositions.Find(x => x.Name == value.Name && Math.Abs(x.X - value.X) <= 0 && Math.Abs(x.Y - value.Y) <= 0);
                if (found == null) // did not find match in list
                {
                    ParkPositions.Add(value);
                    _reSyncParkSelection = value;
                }
                else
                {
                    _reSyncParkSelection = found;
                }
                OnPropertyChanged();
            }
        }

        private ICommand _openReSyncDialogCmd;
        public ICommand OpenReSyncDialogCmd
        {
            get
            {
                var command = _openReSyncDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _openReSyncDialogCmd = new RelayCommand(
                    param => OpenReSyncDialog()
                );
            }
        }
        private void OpenReSyncDialog()
        {
            try
            {
                if (SkyServer.Tracking || SkyServer.IsSlewing)
                {
                    OpenDialog(Application.Current.Resources["skyStopMount"].ToString());
                    return;
                }

                SyncMode = ReSyncMode.Home;
                ReSyncParkSelection = ParkSelection;
                HomeResetContent = new ReSyncDialog();
                IsHomeResetDialogOpen = true;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _acceptReSyncDialogCmd;
        public ICommand AcceptReSyncDialogCmd
        {
            get
            {
                var command = _acceptReSyncDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptReSyncDialogCmd = new RelayCommand(
                    param => AcceptReSyncDialog()
                );
            }
        }
        private void AcceptReSyncDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.IsMountRunning) { return; }

                    switch (SyncMode)
                    {
                        case ReSyncMode.Home:
                            SkyServer.ReSyncAxes();
                            break;
                        case ReSyncMode.Park:
                            if (ReSyncParkSelection != null)
                            {
                                SkyServer.ReSyncAxes(ReSyncParkSelection);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    Synthesizer.Speak(Application.Current.Resources["vceSync"].ToString());
                    IsHomeResetDialogOpen = false;
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelReSyncDialogCmd;
        public ICommand CancelReSyncDialogCmd
        {
            get
            {
                var command = _cancelReSyncDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cancelReSyncDialogCmd = new RelayCommand(
                    param => CancelReSyncDialog()
                );
            }
        }
        private void CancelReSyncDialog()
        {
            try
            {
                IsHomeResetDialogOpen = false;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }
        

        private bool _isHomeResetDialogOpen;
        public bool IsHomeResetDialogOpen
        {
            get => _isHomeResetDialogOpen;
            set
            {
                if (_isHomeResetDialogOpen == value) return;
                _isHomeResetDialogOpen = value;
                _skyTelescopeVM.CloseDialogs(value);
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _homeResetContent;
        public object HomeResetContent
        {
            get => _homeResetContent;
            set
            {
                if (_homeResetContent == value) return;
                _homeResetContent = value;
                OnPropertyChanged();
            }
        }

        //private ICommand _openHomeResetDialogCommand;
        //public ICommand OpenHomeResetDialogCommand
        //{
        //    get
        //    {
        //        var command = _openHomeResetDialogCommand;
        //        if (command != null)
        //        {
        //            return command;
        //        }

        //        return _openHomeResetDialogCommand = new RelayCommand(
        //            param => OpenHomeResetDialog()
        //        );
        //    }
        //}
        //private void OpenHomeResetDialog()
        //{
        //    try
        //    {
        //        if (SkyServer.Tracking)
        //        {
        //            OpenDialog(Application.Current.Resources["skyStopMount"].ToString());
        //            return;
        //        }
        //        HomeResetContent = new HomeResetDialog();
        //        IsHomeResetDialogOpen = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //        {
        //            Datetime = HiResDateTime.UtcNow,
        //            Device = MonitorDevice.Telescope,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod().Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message},{ex.StackTrace}"
        //        };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        SkyServer.AlertState = true;
        //        OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
        //    }

        //}

        //private ICommand _acceptHomeResetDialogCommand;
        //public ICommand AcceptHomeResetDialogCommand
        //{
        //    get
        //    {
        //        var command = _acceptHomeResetDialogCommand;
        //        if (command != null)
        //        {
        //            return command;
        //        }

        //        return _acceptHomeResetDialogCommand = new RelayCommand(
        //            param => AcceptHomeResetDialog()
        //        );
        //    }
        //}
        //private void AcceptHomeResetDialog()
        //{
        //    try
        //    {
        //        using (new WaitCursor())
        //        {
        //            if (!SkyServer.IsMountRunning) return;
        //            SkyServer.ResetHomePositions();
        //            Synthesizer.Speak(Application.Current.Resources["vceHomeSet"].ToString());
        //            IsHomeResetDialogOpen = false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //        {
        //            Datetime = HiResDateTime.UtcNow,
        //            Device = MonitorDevice.Telescope,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod().Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message},{ex.StackTrace}"
        //        };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        SkyServer.AlertState = true;
        //        OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
        //    }
        //}

        //private ICommand _cancelHomeResetDialogCommand;
        //public ICommand CancelHomeResetDialogCommand
        //{
        //    get
        //    {
        //        var command = _cancelHomeResetDialogCommand;
        //        if (command != null)
        //        {
        //            return command;
        //        }

        //        return _cancelHomeResetDialogCommand = new RelayCommand(
        //            param => CancelHomeResetDialog()
        //        );
        //    }
        //}
        //private void CancelHomeResetDialog()
        //{
        //    try
        //    {
        //        IsHomeResetDialogOpen = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //        {
        //            Datetime = HiResDateTime.UtcNow,
        //            Device = MonitorDevice.Telescope,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod().Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message},{ex.StackTrace}"
        //        };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        SkyServer.AlertState = true;
        //        OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
        //    }
        //}


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
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
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

        #region AutoHome Dialog

        private bool _autoHomeEnabled;
        public bool AutoHomeEnabled
        {
            get => _autoHomeEnabled;
            set
            {
                if (_autoHomeEnabled == value) return;
                _autoHomeEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _startEnabled;
        public bool StartEnabled
        {
            get => _startEnabled;
            set
            {
                if (_startEnabled == value) return;
                _startEnabled = value;
                OnPropertyChanged();
            }
        }

        private int _autoHomeProgressBar;
        public int AutoHomeProgressBar
        {
            get => _autoHomeProgressBar;
            set
            {
                if (_autoHomeProgressBar == value) return;
                _autoHomeProgressBar = value;
                if (value > 99)
                {
                    IsAutoHomeDialogOpen = false;
                    SkyServer.AutoHomeProgressBar = 0;
                }
                OnPropertyChanged();
            }
        }

        public IList<int> DecOffsets { get; }
        private int _decOffset;

        public int DecOffset
        {
            get => _decOffset;
            set
            {
                if (_decOffset == value) return;
                _decOffset = value;
                OnPropertyChanged();
            }
        }

        public IList<int> AutoHomeLimits { get; }
        private int _autoHomeLimit;
        public int AutoHomeLimit
        {
            get => _autoHomeLimit;
            set
            {
                if (_autoHomeLimit == value) return;
                _autoHomeLimit = value;
                OnPropertyChanged();
            }
        }

        private bool _isAutoHomeDialogOpen;
        public bool IsAutoHomeDialogOpen
        {
            get => _isAutoHomeDialogOpen;
            set
            {
                if (_isAutoHomeDialogOpen == value) return;
                _isAutoHomeDialogOpen = value;
                CloseDialogs(value);
                _skyTelescopeVM.CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _autoHomeContent;
        public object AutoHomeContent
        {
            get => _autoHomeContent;
            set
            {
                if (_autoHomeContent == value) return;
                _autoHomeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openAutoHomeDialogCommand;
        public ICommand OpenAutoHomeDialogCommand
        {
            get
            {
                var command = _openAutoHomeDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openAutoHomeDialogCommand = new RelayCommand(
                    param => OpenAutoHomeDialog()
                );
            }
        }
        private void OpenAutoHomeDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.CanHomeSensor)
                    {
                        OpenDialog($"{Application.Current.Resources["1021NoHomeSensor"]}");
                        return;
                    }
                    AutoHomeContent = new AutoHomeDialog();
                    StartEnabled = true;
                    SkyServer.AutoHomeProgressBar = 0;
                    AutoHomeLimit = 100;
                    IsAutoHomeDialogOpen = true;
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _startAutoHomeDialogCommand;
        public ICommand StartAutoHomeDialogCommand
        {
            get
            {
                var command = _startAutoHomeDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _startAutoHomeDialogCommand = new RelayCommand(
                    param => StartAutoHomeDialog());
            }
        }
        private void StartAutoHomeDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.IsMountRunning) return;
                    //start auto home
                    StartEnabled = false;
                    SkyServer.AutoHomeProgressBar = 0;
                    SkyServer.AutoHomeStop = false;
                    SkyServer.AutoHomeAsync(AutoHomeLimit, DecOffset);
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        private ICommand _stopAutoHomeDialogCommand;
        public ICommand StopAutoHomeDialogCommand
        {
            get
            {
                var command = _stopAutoHomeDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _stopAutoHomeDialogCommand = new RelayCommand(
                    param => StopAutoHomeDialog()
                );
            }
        }
        private void StopAutoHomeDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    // stop auto home
                    SkyServer.AutoHomeStop = true;
                    StartEnabled = true;
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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelAutoHomeDialogCommand;
        public ICommand CancelAutoHomeDialogCommand
        {
            get
            {
                var command = _cancelAutoHomeDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelAutoHomeDialogCommand = new RelayCommand(
                    param => CancelAutoHomeDialog()
                );
            }
        }
        private void CancelAutoHomeDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    // cancel auto home
                    SkyServer.AutoHomeStop = true;
                    IsAutoHomeDialogOpen = false;

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

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
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
        ~ButtonsControlVM()
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
