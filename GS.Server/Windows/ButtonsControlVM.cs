/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class ButtonsControlVm : ObservableObject, IDisposable
    {
        #region Fields

        private readonly SkyTelescopeVm _skyTelescopeVm;

        #endregion

        public ButtonsControlVm()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Ui, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Hand Control Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    _skyTelescopeVm = SkyTelescopeVm.ASkyTelescopeVm;
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;
                    ParkPositionViewModel.Instance.PropertyChanged += PropertyChangedParkPositionViewModel;

                    Title = "GS";
                    ScreenEnabled = SkyServer.IsMountRunning;
                    ButtonsWinVisibility = false;
                    TopMost = true;

                    if (ParkPositions.Count > 0)
                    {
                        ParkSelection = ParkPositions.FirstOrDefault();
                        ParkSelectionSetting = ParkPositions.FirstOrDefault();
                    }
                    AtPark = SkyServer.AtPark;
                    IsHome = SkyServer.IsHome;
                    IsLimits = SkyServer.IsLimits;
                    IsTracking = SkyServer.Tracking || SkyServer.SlewState == SlewType.SlewRaDec;
                    IsMoveAxisActive = SkyServer.Tracking && SkyServer.MoveAxisActive;
                    TrackingRate = SkySettings.TrackingRate;
                    _skyTelescopeVm.TrackingRate = SkySettings.TrackingRate;
                    AxisTrackingLimits = new List<double>(Numbers.InclusiveRange(0, 15, 1));
                    AxisHzTrackingLimits = new List<double>(Numbers.InclusiveRange(-20, 20, 1));

                    AutoHomeLimits = new List<int>(Enumerable.Range(20, 160));
                    DecOffsets = new List<int>() { 0, -90, 90 };
                    AutoHomeAxisValues = new List<double>(Numbers.InclusiveRange(85.0, 95.0));
                    AutoHomeEnabled = SkyServer.CanHomeSensor;
                    AutoHomeProgressBar = SkyServer.AutoHomeProgressBar;
                    IsGermanPolarMode = (SkySettings.AlignmentMode == AlignmentModes.algGermanPolar);
                    // Pec button
                    PecShow = SkyServer.PecShow;
                    PecOn = SkyServer.PecOn;
                    SchedulerShow = false;
                    // Flip Button
                    FlipButtonTip = GetResourceByMode("botTipSOP");
                    FlipButtonContent = GetResourceByMode("btnFlip");
                    // Flip Dialog
                    FlipDialogHeader = GetResourceByMode("btnFlip");
                    FlipDialogText = GetResourceByMode("btnContinueFlip");
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Ui, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        /// <summary>
        /// get text from resource indexed by Alignment mode
        /// </summary>
        /// <param name="resource">Resource key</param>
        /// <returns></returns>
        private string GetResourceByMode(string resource)
        {
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    resource += "Az";
                    break;
                case AlignmentModes.algPolar:
                    resource += "Polar";
                    break;
                case AlignmentModes.algGermanPolar:
                default:
                    break;
            }
            return $"{Application.Current.Resources[resource]}";
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
                     case "IsAutoHomeRunning":
                         IsAutoHomeDialogOpen = SkyServer.IsAutoHomeRunning;
                         break;
                     case "CanHomeSensor":
                         AutoHomeEnabled = SkyServer.CanHomeSensor;
                         break;
                     case "IsHome":
                         IsHome = SkyServer.IsHome;
                         break;
                     case "IsLimits":
                         IsLimits= SkyServer.IsLimits;
                         break;
                     case "IsMountRunning":
                         ScreenEnabled = SkyServer.IsMountRunning;
                         break;
                     case "Tracking":
                         IsTracking = SkyServer.Tracking || SkyServer.SlewState == SlewType.SlewRaDec;
                         break;
                     case "MoveAxisActive":
                         IsMoveAxisActive = SkyServer.Tracking && SkyServer.MoveAxisActive;
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
                    Device = MonitorDevice.Ui,
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
                     case "TrackingRate":
                         TrackingRate = SkySettings.TrackingRate;
                         break;
                     case "HcSpeed":
                         //HcSpeed = (double)SkySettings.HcSpeed;
                         break;
                     case "AlignmentMode":
                        OnPropertyChanged(nameof(ParkPositions));
                        if (ParkPositions.Count > 0)
                        {
                            ParkSelection = ParkPositions.FirstOrDefault();
                            ParkSelectionSetting = ParkPositions.FirstOrDefault();
                        }
                        switch (SkySettings.AlignmentMode)
                         {
                             case AlignmentModes.algAltAz:
                                 SkySettings.CanSetPierSide = false;
                                 break;
                             case AlignmentModes.algPolar:
                                 break;
                             case AlignmentModes.algGermanPolar:
                                 SkySettings.CanSetPierSide = SkySettings.HourAngleLimit != 0;
                                 break;
                         }
                         // ReSharper disable ExplicitCallerInfoArgument
                         OnPropertyChanged("ParkPositions");
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private void PropertyChangedParkPositionViewModel(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "SettingsSelection":
                         OnPropertyChanged(nameof(ParkSelectionSetting));
                         break;
                     case "Selection":
                         OnPropertyChanged(nameof(ParkSelection));
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
            IsSchedulerDialogOpen = false;

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

        private bool _atPark;
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

        private bool _isHome;
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

        private bool _isLimits;
        public bool IsLimits
        {
            get => _isLimits;
            set
            {
                if (IsLimits == value) return;
                _isLimits = value;
                LimitsBadgeContent = value ? Application.Current.Resources["btnHintTracking"].ToString() : "";
                OnPropertyChanged();
            }
        }

        // Meridian Limits Options
        private void SetParkLimitSelection(string name)
        {
            var found = ParkPositions.FirstOrDefault(x => x.Name == name);
            ParkLimitSelection = found ?? ParkPositions.FirstOrDefault();
        }

        public IList<double> AxisTrackingLimits { get; }
        public double AxisTrackingLimit
        {
            get => SkySettings.AxisTrackingLimit;
            set
            {
                SkySettings.AxisTrackingLimit = value;
                OnPropertyChanged();
            }
        }
        public IList<double> AxisHzTrackingLimits { get; }
        public double AxisHzTrackingLimit
        {
            get => SkySettings.AxisHzTrackingLimit;
            set
            {
                SkySettings.AxisHzTrackingLimit = value;
                OnPropertyChanged();
            }
        }

        private bool _limitTracking;
        public bool LimitTracking
        {
            get => _limitTracking;
            set
            {
                _limitTracking = value;
                SkySettings.LimitTracking = value;
                OnPropertyChanged();
            }
        }

        private bool _limitPark;
        public bool LimitPark
        {
            get => _limitPark;
            set
            {
                _limitPark = value;
                SkySettings.LimitPark = value;
                OnPropertyChanged();
            }
        }

        private ParkPosition _parkLimitSelection;
        public ParkPosition ParkLimitSelection
        {
            get => _parkLimitSelection;
            set
            {
                if (value is null || _parkLimitSelection == value) return;
                _parkLimitSelection = value;
                SkySettings.ParkLimitName = value.Name;
                OnPropertyChanged();
            }
        }

        private bool _limitNothing;
        public bool LimitNothing
        {
            get => _limitNothing;
            set
            {
                _limitNothing = value;
                OnPropertyChanged();
            }
        }

        // Horizon Limits Options
        private void SetParkHzLimitSelection(string name)
        {
            var found = ParkPositions.FirstOrDefault(x => x.Name == name);
            ParkHzLimitSelection = found ?? ParkPositions.FirstOrDefault();
        }

        private bool _hzlimitTracking;
        public bool HzLimitTracking
        {
            get => _hzlimitTracking;
            set
            {
                _hzlimitTracking = value;
                SkySettings.HzLimitTracking = value;
                OnPropertyChanged();
            }
        }

        private bool _hzlimitPark;
        public bool HzLimitPark
        {
            get => _hzlimitPark;
            set
            {
                _hzlimitPark = value;
                SkySettings.HzLimitPark = value;
                OnPropertyChanged();
            }
        }

        private ParkPosition _parkHzLimitSelection;
        public ParkPosition ParkHzLimitSelection
        {
            get => _parkHzLimitSelection;
            set
            {
                if (value is null || _parkHzLimitSelection == value) return;
                _parkHzLimitSelection = value;
                SkySettings.ParkHzLimitName = value.Name;
                OnPropertyChanged();
            }
        }

        private bool _hzlimitNothing;
        public bool HzLimitNothing
        {
            get => _hzlimitNothing;
            set
            {
                _hzlimitNothing = value;
                OnPropertyChanged();
            }
        }

        // Commands
        private ICommand _openLimitDialogCommand;
        public ICommand OpenLimitDialogCommand
        {
            get
            {
                var command = _openLimitDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openLimitDialogCommand = new RelayCommand(
                    param => OpenLimitDialog()
                );
            }
        }
        private void OpenLimitDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    //Meridian
                    LimitTracking = SkySettings.LimitTracking;
                    LimitPark = SkySettings.LimitPark;
                    SetParkLimitSelection(SkySettings.ParkLimitName);
                    if (!LimitPark && !LimitTracking) { LimitNothing = true; }
                    if (LimitPark || LimitTracking) { LimitNothing = false; }
                    //Horizon
                    HzLimitTracking = SkySettings.HzLimitTracking;
                    HzLimitPark = SkySettings.HzLimitPark;
                    SetParkHzLimitSelection(SkySettings.ParkHzLimitName);
                    if (!HzLimitPark && !HzLimitTracking) { HzLimitNothing = true; }
                    if (HzLimitPark || HzLimitTracking) { HzLimitNothing = false; }

                    DialogContent = new LimitDialog();
                    IsDialogOpen = true;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private ICommand _okLimitDialogCommand;
        public ICommand OkLimitDialogCommand
        {
            get
            {
                var command = _okLimitDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _okLimitDialogCommand = new RelayCommand(
                    param => OkLimitDialog()
                );
            }
        }
        private void OkLimitDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    IsDialogOpen = false;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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


        private bool _isMoveAxisActive;
        public bool IsMoveAxisActive
        {
            get => _isMoveAxisActive;
            set
            {
                if (_isMoveAxisActive == value) return;
                _isMoveAxisActive = value;
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
                _skyTelescopeVm.TrackingRate = SkySettings.TrackingRate;
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
                if (SkyServer.PecOn) { _skyTelescopeVm.PecState = true; }
                if (!SkyServer.PPecOn && !SkyServer.PecOn) { _skyTelescopeVm.PecState = false; }
                PecBadgeContent = SkyServer.PecOn ? Application.Current.Resources["PecBadge"].ToString() : "";
            }
        }

        public ObservableCollection<ParkPosition> ParkPositions => ParkPositionViewModel.Instance.Positions;

        public ParkPosition ParkSelection
        {
            get => ParkPositionViewModel.Instance.Selection;
            set => ParkPositionViewModel.Instance.Selection = value;
        }

        public ParkPosition ParkSelectionSetting
        {
            get => ParkPositionViewModel.Instance.SettingsSelection;
            set => ParkPositionViewModel.Instance.SettingsSelection = value;
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

        private bool _isParkDialogOpen;
        public bool IsParkDialogOpen
        {
            get => _isParkDialogOpen;
            set
            {
                if (_isParkDialogOpen == value) return;
                _isParkDialogOpen = value;
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _parkContent;
        public object ParkContent
        {
            get => _parkContent;
            set
            {
                if (_parkContent == value) return;
                _parkContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openParkDialogCmd;
        public ICommand OpenParkDialogCmd
        {
            get
            {
                var command = _openParkDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _openParkDialogCmd = new RelayCommand(
                    param => OpenParkDialog()
                );
            }
        }

        private void OpenParkDialog()
        {
            try
            {
                DialogContent = new ParkDialog();
                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private ICommand _acceptParkDialogCmd;
        public ICommand AcceptParkDialogCmd
        {
            get
            {
                var command = _acceptParkDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptParkDialogCmd = new RelayCommand(
                    param => AcceptParkDialog()
                );
            }
        }
        private void AcceptParkDialog()
        {
            IsDialogOpen = false;
            ClickPark();
        }

        private ICommand _cancelParkDialogCmd;
        public ICommand CancelParkDialogCmd
        {
            get
            {
                var command = _cancelParkDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cancelParkDialogCmd = new RelayCommand(
                    param => CancelParkDialog()
                );
            }
        }

        private void CancelParkDialog()
        {
            try
            {
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

                    var axes = Axes.MountAxis2Mount();
                    if (axes == null) return;

                    ParkPositionViewModel.Instance.AddPosition(ParkNewName.Trim(), axes[0], axes[1]);
                    IsParkAddDialogOpen = false;
                }
            }
            catch (InvalidOperationException ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Warning,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = ex.Message
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    ParkPositionViewModel.Instance.DeletePosition(ParkSelectionSetting);
                    IsParkDeleteDialogOpen = false;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                        if (SkySettings.TrackAfterUnpark)
                        {
                            SkyServer.Tracking = (SkySettings.AlignmentMode != AlignmentModes.algAltAz);
                        }
                        else
                        {
                            SkyServer.Tracking = false;
                        }
                    }
                    else
                    {
                        SkyServer.ParkSelected = ParkSelection;
                        SkyServer.GoToPark();
                    }
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Ui,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod()?.Name,
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
                    Device = MonitorDevice.Ui,
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

        private string _limitsBadgeContent;
        public string LimitsBadgeContent
        {
            get => _limitsBadgeContent;
            set
            {
                if (LimitsBadgeContent == value) return;
                _limitsBadgeContent = value;
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
                        _skyTelescopeVm.BlinkParked();
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

        private string _flipButtonContent;
        public string FlipButtonContent
        {
            get => _flipButtonContent;
            set
            {
                if (_flipButtonContent == value) return;
                _flipButtonContent = value;
                OnPropertyChanged();
            }
        }

        private string _flipButtonTip;
        public string FlipButtonTip
        {
            get => _flipButtonTip;
            set
            {
                if (_flipButtonTip == value) return;
                _flipButtonTip = value;
                OnPropertyChanged();
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

        #region Scheduler dialog
        private bool _isSchedulerDialogOpen;
        public bool IsSchedulerDialogOpen
        {
            get => _isSchedulerDialogOpen;
            set
            {
                if (_isSchedulerDialogOpen == value) return;
                _isSchedulerDialogOpen = value;
                _skyTelescopeVm.CloseDialogs(value);
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
                if (_skyTelescopeVm.FutureParkDate == null)
                {
                    _skyTelescopeVm.FutureParkDate = DateTime.Now + TimeSpan.FromSeconds(60);
                }
                if (_skyTelescopeVm.FutureParkTime == null)
                {
                    _skyTelescopeVm.FutureParkTime = $"{DateTime.Now + TimeSpan.FromSeconds(60):HH:mm}";
                }

                FutureParkDate = _skyTelescopeVm.FutureParkDate;
                FutureParkTime = _skyTelescopeVm.FutureParkTime;
                ScheduleParkOn = _skyTelescopeVm.ScheduleParkOn;

                SchedulerContent = new SchedulerDialog();
                IsSchedulerDialogOpen = true;

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

        private bool _scheduleParkOn;
        public bool ScheduleParkOn
        {
            get => _scheduleParkOn;
            set
            {
                if (value)
                {
                    _skyTelescopeVm.FutureParkDate = FutureParkDate;
                    _skyTelescopeVm.FutureParkTime = FutureParkTime;
                    _skyTelescopeVm.ScheduleParkOn = true;
                }
                else
                {
                    _skyTelescopeVm.ScheduleParkOn = false;
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
        #endregion

        private ICommand _clickLimitsCmd;
        public ICommand ClickLimitsCommand
        {
            get
            {
                var command = _clickLimitsCmd;
                if (command != null)
                {
                    return command;
                }

                return _clickLimitsCmd = new RelayCommand(
                    param => ClickLimits()
                );
            }
        }
        private void ClickLimits()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.IsLimits = !SkyServer.IsLimits;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

                var found = ParkPositions.FirstOrDefault(x => x.Name == value.Name && Math.Abs(x.X - value.X) <= 0.00001 && Math.Abs(x.Y - value.Y) <= 0.00001);
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

        #region Home Dialog

        private bool _isHomeResetDialogOpen;
        public bool IsHomeResetDialogOpen
        {
            get => _isHomeResetDialogOpen;
            set
            {
                if (_isHomeResetDialogOpen == value) return;
                _isHomeResetDialogOpen = value;
                _skyTelescopeVm.CloseDialogs(value);
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
        //            Device = MonitorDevice.Ui,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod()?.Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message}|{ex.StackTrace}"
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
        //            Device = MonitorDevice.Ui,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod()?.Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message}|{ex.StackTrace}"
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
        //            Device = MonitorDevice.Ui,
        //            Category = MonitorCategory.Interface,
        //            Type = MonitorType.Error,
        //            Method = MethodBase.GetCurrentMethod()?.Name,
        //            Thread = Thread.CurrentThread.ManagedThreadId,
        //            Message = $"{ex.Message}|{ex.StackTrace}"
        //        };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        SkyServer.AlertState = true;
        //        OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
        //    }
        //}

        private bool _isHomeDialogOpen;
        public bool IsHomeDialogOpen
        {
            get => _isHomeDialogOpen;
            set
            {
                if (_isHomeDialogOpen == value) return;
                _isHomeDialogOpen = value;
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _homeContent;
        public object HomeContent
        {
            get => _homeContent;
            set
            {
                if (_homeContent == value) return;
                _homeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openHomeDialogCmd;
        public ICommand OpenHomeDialogCmd
        {
            get
            {
                var command = _openHomeDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _openHomeDialogCmd = new RelayCommand(
                    param => OpenHomeDialog()
                );
            }
        }
        private void OpenHomeDialog()
        {
            try
            {
                DialogContent = new HomeDialog();
                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private ICommand _acceptHomeDialogCmd;
        public ICommand AcceptHomeDialogCmd
        {
            get
            {
                var command = _acceptHomeDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptHomeDialogCmd = new RelayCommand(
                    param => AcceptHomeDialog()
                );
            }
        }
        private void AcceptHomeDialog()
        {
            IsDialogOpen = false;
            ClickHome();
        }

        private ICommand _cancelHomeDialogCmd;
        public ICommand CancelHomeDialogCmd
        {
            get
            {
                var command = _cancelHomeDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cancelHomeDialogCmd = new RelayCommand(
                    param => CancelHomeDialog()
                );
            }
        }
        private void CancelHomeDialog()
        {
            try
            {
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                Device = MonitorDevice.Ui,
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

        private bool _decOffsetIsVisible;
        public bool DecOffsetIsVisible
        {
            get => _decOffsetIsVisible;
            set
            {
                if (_decOffsetIsVisible == value) return;
                _decOffsetIsVisible = value;
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

        private bool _isGermanPolarMode;
        public bool IsGermanPolarMode
        {
            get => _isGermanPolarMode;
            set
            {
                if (_isGermanPolarMode == value) return;
                _isGermanPolarMode = value;
                OnPropertyChanged();
            }
        }

        public IList<double> AutoHomeAxisValues { get; }

        public double AutoHomeAxisX
        {
            get => _skyTelescopeVm.AutoHomeAxisX;
            set
            {
                _skyTelescopeVm.AutoHomeAxisX = value;
                OnPropertyChanged();
            }
        }

        public double AutoHomeAxisY
        {
            get => _skyTelescopeVm.AutoHomeAxisY;
            set
            {
                _skyTelescopeVm.AutoHomeAxisY = value;
                OnPropertyChanged();
            }
        }

        private ICommand _resetSensorPositionCommand;
        public ICommand ResetSensorPositionCommand
        {
            get
            {
                var command = _resetSensorPositionCommand;
                if (command != null)
                {
                    return command;
                }

                return _resetSensorPositionCommand = new RelayCommand(
                    param => ResetSensorPosition()
                );
            }
        }
        private void ResetSensorPosition()
        {
            try
            {
                AutoHomeAxisX = 90.0;
                AutoHomeAxisY = 90.0;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "AutoHome sensor positions reset to default: Ra=90.0, Dec=90.0"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private bool _isAutoHomeDialogOpen;
        public bool IsAutoHomeDialogOpen
        {
            get => _isAutoHomeDialogOpen;
            set
            {
                if (_isAutoHomeDialogOpen == value) return;
                _isAutoHomeDialogOpen = value;
                CloseDialogs(value);
                _skyTelescopeVm.CloseDialogs(value);
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
                    DecOffsetIsVisible = SkySettings.AlignmentMode == AlignmentModes.algGermanPolar;
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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
                    Device = MonitorDevice.Ui,
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

        #region Flip Dialog

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
                DialogContent = new FlipDialog();
                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                IsDialogOpen = false;
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
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        private bool _isFlipDialogOpen;
        public bool IsFlipDialogOpen
        {
            get => _isFlipDialogOpen;
            set
            {
                if (_isFlipDialogOpen == value) return;
                _isFlipDialogOpen = value;
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

        private bool _flipOnGoto;
        public bool FlipOnGoto
        {
            get => _flipOnGoto;
            set
            {
                _flipOnGoto = value;
                OnPropertyChanged();
            }
        }

        private ICommand _acceptFlipGoToDialogCmd;
        public ICommand AcceptFlipGoToDialogCmd
        {
            get
            {
                var command = _acceptFlipGoToDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptFlipGoToDialogCmd = new RelayCommand(
                    param => AcceptFlipGoToDialog()
                );
            }
        }
        private void AcceptFlipGoToDialog()
        {
            try
            {
                SkyServer.FlipOnNextGoto = FlipOnGoto;
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                IsDialogOpen = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private string _flipDialogHeader;

        public string FlipDialogHeader
        {
            get => _flipDialogHeader;
            set
            {
                if (_flipDialogHeader == value) return;
                _flipDialogHeader = value;
                OnPropertyChanged();
            }
        }

        private string _flipDialogText;

        public string FlipDialogText
        {
            get => _flipDialogText;
            set
            {
                if (_flipDialogText == value) return;
                _flipDialogText = value;
                OnPropertyChanged();
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
        ~ButtonsControlVm()
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
