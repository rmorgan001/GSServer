/* Copyright(C) 2019-2020  Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Cdc;
using GS.Server.Gps;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Shared;
using HelixToolkit.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using NativeMethods = GS.Server.Helpers.NativeMethods;
using Point = System.Windows.Point;

namespace GS.Server.SkyTelescope
{
    public sealed class SkyTelescopeVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields
        private static readonly Util _util = new Util();
        public string TopName => "SkyWatcher";
        public string BottomName => "Telescope";
        public int Uid => 0;
        public static SkyTelescopeVM _skyTelescopeVM;
        private CancellationTokenSource _ctsPark;
        private CancellationToken _ctPark;
        #endregion

        public SkyTelescopeVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Loading SkyTelescopeVM"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    _skyTelescopeVM = this;
                    LoadImages();  // load front image
                    if (!Properties.Server.Default.SkyWatcher) return; // Show in Tab?

                    // Deals with applications trying to open the setup dialog more than once. 
                    OpenSetupDialog = SkyServer.OpenSetupDialog;
                    SkyServer.OpenSetupDialog = true;
                    SettingsGridEnabled = true;

                    // setup property events to monitor
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    MonitorQueue.StaticPropertyChanged += PropertyChangedMonitorQueue;
                    SkySystem.StaticPropertyChanged += PropertyChangedSkySystem;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;
                    Shared.Settings.StaticPropertyChanged += PropertyChangedMonitorLog;
                    Synthesizer.StaticPropertyChanged += PropertyChangedSynthesizer;
                    Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;

                    // dropdown lists
                    GuideRateOffsetList = new List<double>(Numbers.InclusiveRange(10, 100, 10));
                    MaxSlewRates = new List<double>(Numbers.InclusiveRange(2.0, 5));
                    HourAngleLimits = new List<double>(Numbers.InclusiveRange(0, 45, 1));
                    Range90 = new List<int>(Enumerable.Range(0, 90));
                    Range179 = new List<int>(Enumerable.Range(0, 180));
                    LatitudeRangeNS = new List<string>() { "N", "S" };
                    LongitudeRangeEW = new List<string>() { "E", "W" };
                    DecRange = new List<int>(Enumerable.Range(-90, 181));
                    Hours = new List<int>(Enumerable.Range(0, 24));
                    Range60 = new List<int>(Enumerable.Range(0, 60));
                    St4GuideRates = new List<double> { 1.0, 0.75, 0.50, 0.25, 0.125 };
                    Temperatures = new List<double>(Numbers.InclusiveRange(-50, 60, 1.0));
                    AutoHomeLimits = new List<int>(Enumerable.Range(20, 160));
                    DecOffsets = new List<int>() { 0, -90, 90 };
                    MinPulseList = new List<int>(Enumerable.Range(5, 46));
                    RaBacklashList = new List<int>(Enumerable.Range(0, 1001));
                    DecBacklashList = new List<int>(Enumerable.Range(0, 1001));
                    var extendedlist = new List<int>(Numbers.InclusiveIntRange(1000, 3000, 100));
                    RaBacklashList = RaBacklashList.Concat(extendedlist);
                    DecBacklashList = DecBacklashList.Concat(extendedlist);
                    //SpiralHcSpeeds = new List<int>(Enumerable.Range(1, 8));
                    //SpiralPauses = new List<int>(Enumerable.Range(0, 61));

                    // defaults
                    AtPark = SkyServer.AtPark;
                    ConnectButtonContent = Application.Current.Resources["skyConnect"].ToString();
                    VoiceState = Synthesizer.VoiceActive;
                    ParkSelection = ParkPositions.FirstOrDefault();
                    ParkSelectionSetting = ParkPositions.FirstOrDefault();
                    SetHCFlipsVisibility();
                    RightAscension = "00h 00m 00s";
                    Declination = "00° 00m 00s";
                    Azimuth = "00° 00m 00s";
                    Altitude = "00° 00m 00s";
                    Lha = "00h 00m 00s";
                    ModelOn = SkySettings.ModelOn;
                    SetTrackingIcon(SkySettings.TrackingRate);
                    SetParkLimitSelection(SkySettings.ParkLimitName);

                    HcWinVisibility = true;
                    ModelWinVisibility = true;
                    DebugVisibility = SkySettings.Diagnostics;
                    PecShow = SkyServer.PecShow;
                }

                // check to make sure window is visible then connect if requested.
                MountState = SkyServer.IsMountRunning;
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
                SkyServer.IsMountRunning = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #region View Model Items

        private static bool _pecShow;
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

        private bool _settingsGridEnabled;
        public bool SettingsGridEnabled
        {
            get => _settingsGridEnabled;
            set
            {
                if (_settingsGridEnabled == value) return;
                _settingsGridEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Property changes from settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                     case "Longitude":
                         UpdateLongitude();
                         break;
                     case "Latitude":
                         UpdateLatitude();
                         break;
                     case "Elevation":
                         Elevation = SkySettings.Elevation;
                         break;
                     case "Diagnostics":
                         DebugVisibility = SkySettings.Diagnostics;
                         break;
                     case "ParkPositions":
                         OnPropertyChanged($"ParkPositions");
                         break;
                     case "DecBacklash":
                         DecBacklash = SkySettings.DecBacklash;
                         break;
                     case "RaBacklash":
                         RaBacklash = SkySettings.RaBacklash;
                         break;
                     case "MinPulseDec":
                         MinPulseDec = SkySettings.MinPulseDec;
                         break;
                     case "MinPulseRa":
                         MinPulseRa = SkySettings.MinPulseRa;
                         break;
                     case "ModelOn":
                         ModelOn = SkySettings.ModelOn;
                         break;
                     case "TrackingRate":
                         TrackingRate = SkySettings.TrackingRate;
                         break;
                     case "ParkLimitName":
                         SetParkLimitSelection(SkySettings.ParkLimitName);
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
                                case "Altitude":
                                    Altitude = _util.DegreesToDMS(SkyServer.Altitude, "° ", ":", "", 2);
                                    break;
                                case "Azimuth":
                                    Azimuth = _util.DegreesToDMS(SkyServer.Azimuth, "° ", ":", "", 2);
                                    break;
                                case "CanPec":
                                    PPecEnabled = SkyServer.CanPPec;
                                    break;
                                case "DeclinationXForm":
                                    Declination = _util.DegreesToDMS(SkyServer.DeclinationXForm, "° ", ":", "", 2);
                                    break;
                                case "CanHomeSensor":
                                    AutoHomeEnabled = SkyServer.CanHomeSensor;
                                    break;
                                case "OpenSetupDialog":
                                    OpenSetupDialog = SkyServer.OpenSetupDialog;
                                    break;
                                case "Lha":
                                    Lha = _util.HoursToHMS(SkyServer.Lha, "h ", ":", "", 2);
                                    break;
                                case "RightAscensionXForm":
                                    RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                                    Rotate();
                                    GetDebugProperties();
                                    break;
                                case "IsHome":
                                    IsHome = SkyServer.IsHome;
                                    break;
                                case "AtPark":
                                    AtPark = SkyServer.AtPark;
                                    break;
                                case "IsSlewing":
                                    IsSlewing = SkyServer.IsSlewing;
                                    break;
                                case "Tracking":
                                    IsTracking = SkyServer.Tracking;
                                    break;
                                case "IsSideOfPier":
                                    IsSideOfPier = SkyServer.IsSideOfPier;
                                    break;
                                case "LimitAlarm":
                                    LimitAlarm = SkyServer.LimitAlarm;
                                    break;
                                case "MountError":
                                    MountError = SkyServer.MountError;
                                    break;
                                case "AlertState":
                                    AlertState = SkyServer.AlertState;
                                    break;
                                case "PecTrainInProgress":
                                    PecTrainInProgress = SkyServer.PecTrainInProgress;
                                    break;
                                case "PecTrainOn":
                                    PecTrainOn = SkyServer.PecTraining;
                                    break;
                                case "Longitude":
                                    UpdateLongitude();
                                    break;
                                case "Latitude":
                                    UpdateLatitude();
                                    break;
                                case "Elevation":
                                    Elevation = SkySettings.Elevation;
                                    break;
                                case "IsSimulatorConnected":
                                    // no status kept for the simulator
                                    break;
                                case "IsMountRunning":
                                    MountState = SkyServer.IsMountRunning;
                                    break;
                                case "AutoHomeProgressBar":
                                    AutoHomeProgressBar = SkyServer.AutoHomeProgressBar;
                                    break;
                                case "ParkSelected":
                                    ParkSelection = SkyServer.ParkSelected;
                                    break;
                                case "TrackingRate":
                                    CurTrackingRate = $"{SkyServer.TrackingRate}";
                                    break;
                                case "PecBinNow":
                                    PecBinNow = SkyServer.PecBinNow.Item1;
                                    break;
                                case "PecOn":
                                    if (SkyServer.PecOn) { PecState = true; }
                                    if (!SkyServer.PPecOn && !SkyServer.PecOn) { PecState = false; }
                                    PecBadgeContent = SkyServer.PecOn ? Application.Current.Resources["PecBadge"].ToString() : "";
                                    break;
                                case "PPecOn":
                                    PPecOn = SkyServer.PPecOn;
                                    if (SkyServer.PPecOn) { PecState = true; }
                                    if (!SkyServer.PPecOn && !SkyServer.PecOn) { PecState = false; }
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

        /// <summary>
        /// Used in the bottom bar to show the monitor is running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedMonitorLog(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case "Start":
                        MonitorState = Shared.Settings.StartMonitor;
                        break;
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

        /// <summary>
        /// Used in the bottom bar to show the monitor is running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSynthesizer(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case "VoiceActive":
                        VoiceState = Synthesizer.VoiceActive;
                        break;
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

        /// <summary>
        /// Property changes from system
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSkySystem(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "ConnectSerial":
                                IsConnected = SkySystem.ConnectSerial;
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

        /// <summary>
        /// Property changes from monitor queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedMonitorQueue(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "WarningState":
                                WarningState = MonitorQueue.WarningState;
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

        /// <summary>
        /// Property changes from option settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "AccentColor":
                            case "ModelType":
                                LoadGEM();
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

        /// <summary>
        /// Holds and shows reported error from the server
        /// </summary>
        private Exception _mountError;
        public Exception MountError
        {
            get => _mountError;
            set
            {
                _mountError = value;
                if (value == null) return;
                OpenDialog(value.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        public IList<string> ImageFiles;
        private string _imageFile;
        public string ImageFile
        {
            get => _imageFile;
            set
            {
                if (_imageFile == value) return;
                _imageFile = value;
                OnPropertyChanged();
            }
        }

        private void LoadImages()
        {
            if (!string.IsNullOrEmpty(ImageFile)) return;
            var random = new Random();
            ImageFiles = new List<string> { "M33.png", "Horsehead.png", "NGC6992.png", "Orion.png" };
            ImageFile = "../Resources/" + ImageFiles[random.Next(ImageFiles.Count)];
        }

        private void CloseDialogs(bool screen)
        {
            if (screen)
            {
                ScreenEnabled = false;
                return;
            }

            //IsDialogOpen = false;
            IsAutoHomeDialogOpen = false;
            IsFlipDialogOpen = false;
            IsHomeResetDialogOpen = false;
            IsRaGoToDialogOpen = false;
            IsRaGoToSyncDialogOpen = false;
            IsSchedulerDialogOpen = false;
            IsLimitDialogOpen = false;
            IsPPecDialogOpen = false;
            IsParkAddDialogOpen = false;
            IsParkDeleteDialogOpen = false;
            IsGpsDialogOpen = false;
            IsCdcDialogOpen = false;
            //IsHcSettingsDialogOpen = false;

            ScreenEnabled = SkyServer.IsMountRunning;
        }

        #endregion

        #region Drawer Settings 

        // alternative listing of ports
        public IList<string> AllComPorts
        {
            get
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                {
                    var portNames = System.IO.Ports.SerialPort.GetPortNames();
                    var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                    var portList = portNames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();

                    foreach (var s in portList)
                    {
                        Console.WriteLine(s);
                    }

                    return portList;
                }
            }
        }

        public IList<int> ComPorts
        {
            get
            {
                var ports = new List<int>();
                foreach (var item in System.IO.Ports.SerialPort.GetPortNames())
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var tmp = Strings.GetNumberFromString(item);
                    if (tmp.HasValue)
                    {
                        ports.Add((int)tmp);
                    }
                }
                return ports;
            }
        }
        public int ComPort
        {
            get => SkySettings.ComPort;
            set
            {
                if (value == SkySettings.ComPort) return;
                SkySettings.ComPort = value;
                OnPropertyChanged();
            }
        }
        public SerialSpeed BaudRate
        {
            get => SkySettings.BaudRate;
            set
            {
                if (value == SkySettings.BaudRate) return;
                SkySettings.BaudRate = value;
                OnPropertyChanged();
            }
        }
        public double SiderealRate
        {
            get => SkySettings.SiderealRate;
            set
            {
                SkySettings.SiderealRate = value;
                OnPropertyChanged();
            }
        }
        public double LunarRate
        {
            get => SkySettings.LunarRate;
            set
            {
                SkySettings.LunarRate = value;
                OnPropertyChanged();
            }
        }
        public double SolarRate
        {
            get => SkySettings.SolarRate;
            set
            {
                SkySettings.SolarRate = value;
                OnPropertyChanged();
            }
        }
        public double KingRate
        {
            get => SkySettings.KingRate;
            set
            {
                SkySettings.KingRate = value;
                OnPropertyChanged();
            }
        }
        public IList<double> St4GuideRates { get; }
        public double St4GuideRate
        {
            get
            {
                double ret;
                switch (SkySettings.St4GuideRate)
                {
                    case 0:
                        ret = 1.0;
                        break;
                    case 1:
                        ret = 0.75;
                        break;
                    case 2:
                        ret = 0.50;
                        break;
                    case 3:
                        ret = 0.25;
                        break;
                    case 4:
                        ret = 0.125;
                        break;
                    default:
                        ret = 0.50;
                        break;
                }
                return ret;
            }
            set
            {
                int ret;
                switch (value)
                {
                    case 1.0:
                        ret = 0;
                        break;
                    case .75:
                        ret = 1;
                        break;
                    case .50:
                        ret = 2;
                        break;
                    case .25:
                        ret = 3;
                        break;
                    case .125:
                        ret = 4;
                        break;
                    default:
                        ret = 2;
                        break;
                }
                SkySettings.St4GuideRate = ret;
                OnPropertyChanged();
            }
        }
        public IList<double> HourAngleLimits { get; }
        public double HourAngleLimit
        {
            get => SkySettings.HourAngleLimit;
            set
            {
                SkySettings.HourAngleLimit = value;
                OnPropertyChanged();
            }
        }
        public AlignmentModes AlignmentMode
        {
            get => SkySettings.AlignmentMode;
            set
            {
                SkySettings.AlignmentMode = value;
                OnPropertyChanged();

            }
        }
        public EquatorialCoordinateType EquatorialCoordinateType
        {
            get => SkySettings.EquatorialCoordinateType;
            set
            {
                SkySettings.EquatorialCoordinateType = value;
                OnPropertyChanged();
            }
        }
        public DriveRates TrackingRate
        {
            get => SkySettings.TrackingRate;
            set
            {
                SkySettings.TrackingRate = value;
                SetTrackingIcon(value);
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
        public IList<int> MinPulseList { get; }
        public int MinPulseDec
        {
            get => SkySettings.MinPulseDec;
            set
            {
                SkySettings.MinPulseDec = value;
                OnPropertyChanged();
            }
        }
        public int MinPulseRa
        {
            get => SkySettings.MinPulseRa;
            set
            {
                SkySettings.MinPulseRa = value;
                OnPropertyChanged();
            }
        }
        public MountType Mount
        {
            get => SkySettings.Mount;
            set
            {
                if (value == SkySettings.Mount) return;
                SkySettings.Mount = value;
                OnPropertyChanged();
            }
        }
        public IList<double> GuideRateOffsetList { get; }
        public double GuideRateOffsetX
        {
            get => SkySettings.GuideRateOffsetX * 100;
            set
            {
                if (Math.Abs((Convert.ToDouble(value) / 100) - SkySettings.GuideRateOffsetX) < 0.0) return;
                SkySettings.GuideRateOffsetX = (Convert.ToDouble(value) / 100);
                OnPropertyChanged();
            }
        }
        public double GuideRateOffsetY
        {
            get => SkySettings.GuideRateOffsetY * 100;
            set
            {
                if (Math.Abs((Convert.ToDouble(value) / 100) - SkySettings.GuideRateOffsetY) < 0.0) return;
                SkySettings.GuideRateOffsetY = (Convert.ToDouble(value) / 100);
                OnPropertyChanged();
            }
        }
        public IList<double> MaxSlewRates { get; }
        public double MaxSlewRate
        {
            get => SkySettings.MaxSlewRate;
            set
            {
                SkySettings.MaxSlewRate = value;
                OnPropertyChanged();
            }
        }
        public IList<double> Temperatures { get; }
        public double Temperature
        {
            get => SkySettings.Temperature;
            set
            {
                SkySettings.Temperature = value;
                OnPropertyChanged();
            }
        }
        public bool EncodersOn
        {
            get => SkySettings.Encoders;
            set
            {
                if (value == SkySettings.Encoders) return;
                SkySettings.Encoders = value;
                OnPropertyChanged();
            }
        }
        public bool FullCurrent
        {
            get => SkySettings.FullCurrent;
            set
            {
                if (value == SkySettings.FullCurrent) return;
                SkySettings.FullCurrent = value;
                OnPropertyChanged();
            }
        }
        public bool AlternatingPPec
        {
            get => SkySettings.AlternatingPPec;
            set
            {
                if (value == SkySettings.AlternatingPPec) return;
                SkySettings.AlternatingPPec = value;
                OnPropertyChanged();
            }
        }
        public bool DecPulseToGoTo
        {
            get => SkySettings.DecPulseToGoTo;
            set
            {
                if (value == SkySettings.DecPulseToGoTo) return;
                SkySettings.DecPulseToGoTo = value;
                OnPropertyChanged();
            }
        }
        public bool Refraction
        {
            get => SkySettings.Refraction;
            set
            {
                SkySettings.Refraction = value;
                OnPropertyChanged();
            }
        }
        public bool SyncLimitOn
        {
            get => SkySettings.SyncLimitOn;
            set
            {
                SkySettings.SyncLimitOn = value;
                OnPropertyChanged();
            }
        }
        public IList<string> LatitudeRangeNS { get; }
        public string Lat0
        {
            get => SkySettings.Latitude < 0 ? "S" : "N";
            set
            {
                var a = Math.Abs(SkySettings.Latitude);
                SkySettings.Latitude = value == "S" ? -a : a;
                OnPropertyChanged();
            }
        }
        public IList<int> Range179 { get; }
        public IList<int> Range90 { get; }
        public int Lat1
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Latitude * 3600);
                var deg = sec / 3600;
                return Math.Abs(deg);
            }
            set
            {
                var l = Math.Abs(Principles.Units.Deg2Dou(value, Lat2, Lat3));
                if (Lat0 == "S") l = -l;
                if (Math.Abs(l - SkySettings.Latitude) < 0.0000000000001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }
        public IList<int> Range60 { get; }
        public int Lat2
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Latitude * 3600);
                sec = Math.Abs(sec % 3600);
                var min = sec / 60;
                return min;
            }
            set
            {
                var l = Math.Abs(Principles.Units.Deg2Dou(Lat1, value, Lat3));
                if (Lat0 == "S") l = -l;
                if (Math.Abs(l - SkySettings.Latitude) < 0.0000000000001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }
        public double Lat3
        {
            get
            {
                var sec = SkySettings.Latitude * 3600;
                sec = Math.Abs(sec % 3600);
                sec %= 60;
                return Math.Abs(Math.Round(sec, 3));
            }
            set
            {
                var l = Math.Abs(Principles.Units.Deg2Dou(Lat1, Lat2, value));
                if (Lat0 == "S") l = -l;
                if (Math.Abs(l - SkySettings.Latitude) < 0.0000000000001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }
        public IList<string> LongitudeRangeEW { get; }
        public string Long0
        {
            get => SkySettings.Longitude < 0 ? "W" : "E";
            set
            {
                var a = Math.Abs(SkySettings.Longitude);
                SkySettings.Longitude = value == "W" ? -a : a;
                OnPropertyChanged();
            }
        }
        public int Long1
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Longitude * 3600);
                var deg = sec / 3600;
                return Math.Abs(deg);
            }
            set
            {
                var l = Math.Abs(Principles.Units.Deg2Dou(value, Long2, Long3));
                if (Long0 == "W") l = -l;
                if (Math.Abs(l - SkySettings.Longitude) < 0.0000000000001) return;
                SkySettings.Longitude = l;
                OnPropertyChanged();
            }
        }
        public int Long2
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Longitude * 3600);
                sec = Math.Abs(sec % 3600);
                var min = sec / 60;
                return min;
            }
            set
            {
                var l = Principles.Units.Deg2Dou(Long1, value, Long3);
                if (Long0 == "W") l = -l;
                if (Math.Abs(l - SkySettings.Longitude) < 0.0000000000001) return;
                SkySettings.Longitude = l;
                OnPropertyChanged();
            }
        }
        public double Long3
        {
            get
            {
                var sec = SkySettings.Longitude * 3600;
                sec = Math.Abs(sec % 3600);
                sec %= 60;
                return Math.Abs(Math.Round(sec, 3));
            }
            set
            {
                var l = Principles.Units.Deg2Dou(Long1, Long2, value);
                if (Long0 == "W") l = -l;
                if (Math.Abs(l - SkySettings.Longitude) < 0.0000000000001) return;
                SkySettings.Longitude = l;
                OnPropertyChanged();
            }
        }
        private void UpdateLongitude()
        {
            OnPropertyChanged($"Long0");
            OnPropertyChanged($"Long1");
            OnPropertyChanged($"Long2");
            OnPropertyChanged($"Long3");
        }
        private void UpdateLatitude()
        {
            OnPropertyChanged($"Lat0");
            OnPropertyChanged($"Lat1");
            OnPropertyChanged($"Lat2");
            OnPropertyChanged($"Lat3");
        }
        public double Elevation
        {
            get => SkySettings.Elevation;
            set
            {
                SkySettings.Elevation = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickSaveParkCommand;
        public ICommand ClickSaveParkCommand
        {
            get
            {
                var command = _clickSaveParkCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickSaveParkCommand = new RelayCommand(
                    param => ClickSavePark()
                );
            }
        }
        private void ClickSavePark()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (ParkSelectionSetting == null)
                    {
                        OpenDialog($"{Application.Current.Resources["skyNoSelected"]}");
                        return;
                    }
                    var parkcoords = Axes.MountAxis2Mount();
                    ParkSelectionSetting.X = parkcoords[0];
                    ParkSelectionSetting.Y = parkcoords[1];

                    var parkToUpdate = ParkPositions.FirstOrDefault(p => p.Name == ParkSelectionSetting.Name);
                    if (parkToUpdate == null) return;

                    parkToUpdate.X = parkcoords[0];
                    parkToUpdate.Y = parkcoords[1];
                    SkySettings.ParkPositions = ParkPositions;
                    OpenDialog($"{Application.Current.Resources["skyParkSaved"]} {parkToUpdate.Name}");
                    Synthesizer.Speak(Application.Current.Resources["vceParkSet"].ToString());
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

        private ICommand _clickSaveSettingCommand;
        public ICommand ClickSaveSettingsCommand
        {
            get
            {
                var command = _clickSaveSettingCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickSaveSettingCommand = new RelayCommand(
                    param => ClickSaveSettings()
                );
            }
        }
        private void ClickSaveSettings()
        {
            try
            {
                using (new WaitCursor())
                {
                    GSServer.SaveAllAppSettings();
                    SkyServer.OpenSetupDialogFinished = true;
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

        private ICommand _clickCloseSettingCommand;
        public ICommand ClickCloseSettingsCommand
        {
            get
            {
                var command = _clickCloseSettingCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickCloseSettingCommand = new RelayCommand(
                    param => ClickCloseSettings()
                );
            }
        }
        private void ClickCloseSettings()
        {
            try
            {
                using (new WaitCursor())
                {
                    GSServer.SaveAllAppSettings();
                    SkyServer.OpenSetupDialog = false;
                    SkyServer.OpenSetupDialogFinished = true;
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

        private ICommand _clickResetSiderealRateCommand;
        public ICommand ClickResetSiderealRateCommand
        {
            get
            {
                var command = _clickResetSiderealRateCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickResetSiderealRateCommand = new RelayCommand(
                    param => ClickResetSiderealRate()
                );
            }
        }
        private void ClickResetSiderealRate()
        {
            SiderealRate = 15.0410671787;
        }

        private ICommand _clickResetSolarRateCommand;
        public ICommand ClickResetSolarRateCommand
        {
            get
            {
                var command = _clickResetSolarRateCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickResetSolarRateCommand = new RelayCommand(
                    param => ClickResetSolarRate()
                );
            }
        }
        private void ClickResetSolarRate()
        {
            SolarRate = 15;
        }

        private ICommand _clickResetLunarRateCommand;
        public ICommand ClickResetLunarRateCommand
        {
            get
            {
                var command = _clickResetLunarRateCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickResetLunarRateCommand = new RelayCommand(
                    param => ClickResetLunarRate()
                );
            }
        }
        private void ClickResetLunarRate()
        {
            LunarRate = 14.685;
        }

        private ICommand _clickResetKingRateCommand;
        public ICommand ClickResetKingRateCommand
        {
            get
            {
                var command = _clickResetKingRateCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickResetKingRateCommand = new RelayCommand(
                    param => ClickResetKingRate()
                );
            }
        }
        private void ClickResetKingRate()
        {
            KingRate = 15.0369;
        }

        #endregion

        #region Debug
        private void GetDebugProperties()
        {
            if (!DebugVisibility) return;
            ActualAxisX = $"{Numbers.TruncateD(SkyServer.ActualAxisX, 2)}";
            ActualAxisY = $"{Numbers.TruncateD(SkyServer.ActualAxisY, 2)}";
            SiderealTime = _util.HoursToHMS(SkyServer.SiderealTime);
        }

        private string _actualAxisX;
        public string ActualAxisX
        {
            get => _actualAxisX;
            private set
            {
                if (_actualAxisX == value) return;
                _actualAxisX = value;
                OnPropertyChanged();
            }
        }

        private string _actualAxisY;
        public string ActualAxisY
        {
            get => _actualAxisY;
            private set
            {
                if (_actualAxisY == value) return;
                _actualAxisY = value;
                OnPropertyChanged();
            }
        }

        private string _curTrackingRate;
        public string CurTrackingRate
        {
            get => _curTrackingRate;
            private set
            {
                if (_curTrackingRate == value) return;
                _curTrackingRate = value;
                OnPropertyChanged();
            }
        }

        private bool _debugVisibility;
        public bool DebugVisibility
        {
            get => _debugVisibility;
            set
            {
                if (value == _debugVisibility) return;
                _debugVisibility = value;
                SkySettings.Diagnostics = value;
                OnPropertyChanged();
                if (!value) return;
                GetDebugProperties();
            }
        }

        private int _pecBinNow;
        public int PecBinNow
        {
            get => _pecBinNow;
            private set
            {
                if (_pecBinNow == value) return;
                _pecBinNow = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Top Bar Control

        private string _altitude;
        public string Altitude
        {
            get => _altitude;
            set
            {
                if (value == _altitude) return;
                _altitude = value;
                OnPropertyChanged();
            }
        }

        private string _azimuth;
        public string Azimuth
        {
            get => _azimuth;
            set
            {
                if (value == _azimuth) return;
                _azimuth = value;
                OnPropertyChanged();
            }
        }

        private string _declination;
        public string Declination
        {
            get => _declination;
            set
            {
                if (value == _declination) return;
                _declination = value;
                OnPropertyChanged();
            }
        }

        private string _lha;
        public string Lha
        {
            get => _lha;
            set
            {
                if (value == _lha) return;
                _lha = value;
                OnPropertyChanged();
            }
        }

        private bool _openSetupDialog;
        public bool OpenSetupDialog
        {
            get => _openSetupDialog;
            set
            {
                if (value == _openSetupDialog) return;
                _openSetupDialog = value;
                OnPropertyChanged();
                // forces the updating of the com ports
                OnPropertyChanged($"ComPorts");
            }
        }

        private string _rightAscension;
        public string RightAscension
        {
            get => _rightAscension;
            set
            {
                if (value == _rightAscension) return;
                _rightAscension = value;
                OnPropertyChanged();
            }
        }

        private string _siderealTime;
        public string SiderealTime
        {
            get => _siderealTime;
            set
            {
                if (value == _siderealTime) return;
                _siderealTime = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Button Control

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
                        BlinkParked();
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

        private bool _isHomeResetDialogOpen;
        public bool IsHomeResetDialogOpen
        {
            get => _isHomeResetDialogOpen;
            set
            {
                if (_isHomeResetDialogOpen == value) return;
                _isHomeResetDialogOpen = value;
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

        private ICommand _openHomeResetDialogCommand;
        public ICommand OpenHomeResetDialogCommand
        {
            get
            {
                var command = _openHomeResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openHomeResetDialogCommand = new RelayCommand(
                    param => OpenHomeResetDialog()
                );
            }
        }
        private void OpenHomeResetDialog()
        {
            try
            {
                if (SkyServer.Tracking)
                {
                    OpenDialog(Application.Current.Resources["skyStopMount"].ToString());
                    return;
                }
                HomeResetContent = new HomeResetDialog();
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

        private ICommand _acceptHomeResetDialogCommand;
        public ICommand AcceptHomeResetDialogCommand
        {
            get
            {
                var command = _acceptHomeResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptHomeResetDialogCommand = new RelayCommand(
                    param => AcceptHomeResetDialog()
                );
            }
        }
        private void AcceptHomeResetDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.IsMountRunning) return;
                    SkyServer.ResetHomePositions();
                    Synthesizer.Speak(Application.Current.Resources["vceHomeSet"].ToString());
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

        private ICommand _cancelHomeResetDialogCommand;
        public ICommand CancelHomeResetDialogCommand
        {
            get
            {
                var command = _cancelHomeResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelHomeResetDialogCommand = new RelayCommand(
                    param => CancelHomeResetDialog()
                );
            }
        }
        private void CancelHomeResetDialog()
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
                SchedulerContent = new SchedulerDialog();
                IsSchedulerDialogOpen = true;
                if (ScheduleParkOn) return;
                FutureParkDate = DateTime.Now + TimeSpan.FromSeconds(60);
                FutureParkTime = $"{DateTime.Now + TimeSpan.FromSeconds(60):HH:mm}";
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
                if (_scheduleParkOn == value) return;
                if (value)
                {
                    if (!ValidParkEvent()) { return; }
                    _ctsPark = new CancellationTokenSource();
                    _ctPark = _ctsPark.Token;
                    var oktime = TimeSpan.TryParse(FutureParkTime, out var ftime);
                    var okdate = DateTime.TryParse(FutureParkDate.ToString(), out var fdate);
                    if (okdate && oktime)
                    {
                        var fdatetime = fdate.Date + ftime;
                        ScheduleAction(ClickPark, fdatetime, _ctPark);

                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Interface,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod().Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Park:{fdatetime}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                }
                else
                {
                    if (_ctsPark != null)
                    {
                        if (!_ctsPark.IsCancellationRequested)
                        {
                            _ctsPark?.Cancel();
                        }
                        _ctsPark?.Dispose();
                        SchedulerBadgeContent = string.Empty;
                    }
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

        private bool ValidParkEvent()
        {
            var oktime = TimeSpan.TryParse(FutureParkTime, out var ftime);
            if (!oktime)
            {
                OpenDialog($"{Application.Current.Resources["skyParkEvent1"]}", $"{Application.Current.Resources["exError"]}");
                return false;
            }
            var okdate = DateTime.TryParse(FutureParkDate.ToString(), out var fdate);
            if (!okdate)
            {
                OpenDialog($"{Application.Current.Resources["skyParkEvent2"]}", $"{Application.Current.Resources["exError"]}");
                return false;
            }
            var fdatetime = fdate.Date + ftime;
            if (fdatetime < DateTime.Now)
            {
                OpenDialog($"{Application.Current.Resources["skyParkEvent3"]}", $"{Application.Current.Resources["exError"]}");
                return false;
            }

            return true;
        }

        public async void ScheduleAction(Action action, DateTime ExecutionTime, CancellationToken token)
        {
            try
            {
                SchedulerBadgeContent = "On";
                await Task.Delay((int)ExecutionTime.Subtract(DateTime.Now).TotalMilliseconds, token);
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Information,
                    Method = "ScheduleAction",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{action.Method}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (!SkyServer.AtPark)
                {
                    action();
                }
                ScheduleParkOn = false;
                SchedulerBadgeContent = string.Empty;
            }
            catch (TaskCanceledException ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Information,
                    Method = "ScheduleAction",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Information,
                    Method = "ScheduleAction",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
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
        #endregion

        #region RA Coord GoTo Control

        public IList<int> Hours { get; }

        private double _raHours;
        public double RaHours
        {
            get => _raHours;
            set
            {
                if (Math.Abs(value - _raHours) < 0.00001) return;
                _raHours = value;
                OnPropertyChanged();
            }
        }

        private double _raMinutes;
        public double RaMinutes
        {
            get => _raMinutes;
            set
            {
                if (Math.Abs(value - _raMinutes) < 0.00001) return;
                _raMinutes = value;
                OnPropertyChanged();
            }
        }

        private double _raSeconds;
        public double RaSeconds
        {
            get => _raSeconds;
            set
            {
                if (Math.Abs(value - _raSeconds) < 0.00001) return;
                _raSeconds = value;
                OnPropertyChanged();
            }
        }

        public IList<int> DecRange { get; }

        private double _decDegrees;
        public double DecDegrees
        {
            get => _decDegrees;
            set
            {
                if (Math.Abs(value - _decDegrees) < 0.00001) return;
                _decDegrees = value;
                OnPropertyChanged();
            }
        }

        private double _decMinutes;
        public double DecMinutes
        {
            get => _decMinutes;
            set
            {
                if (Math.Abs(value - _decMinutes) < 0.00001) return;
                _decMinutes = value;
                OnPropertyChanged();
            }
        }

        private double _decSeconds;
        public double DecSeconds
        {
            get => _decSeconds;
            set
            {
                if (Math.Abs(value - _decSeconds) < 0.00001) return;
                _decSeconds = value;
                OnPropertyChanged();
            }
        }
        public double GoToDec => Principles.Units.Deg2Dou(DecDegrees, DecMinutes, DecSeconds);
        public double GoToRa => Principles.Units.Ra2Dou(RaHours, RaMinutes, RaSeconds);
        public string GoToDecString => _util.DegreesToDMS(GoToDec, "° ", "m ", "s", 3);
        public string GoToRaString => _util.HoursToHMS(GoToRa, "h ", "m ", "s", 3);

        private ICommand _populateGoToRaDec;
        public ICommand PopulateGoToRaDecCommand
        {
            get
            {
                var dec = _populateGoToRaDec;
                if (dec != null)
                {
                    return dec;
                }

                return _populateGoToRaDec = new RelayCommand(
                    param => PopulateGoToRaDec()
                );
            }
        }
        private void PopulateGoToRaDec()
        {
            try
            {
                using (new WaitCursor())
                {
                    var ra = _util.HoursToHMS(SkyServer.RightAscensionXForm, ":", ":", ":", 3);
                    var ras = ra.Split(':');
                    RaHours = Convert.ToDouble(ras[0]);
                    RaMinutes = Convert.ToDouble(ras[1]);
                    RaSeconds = Convert.ToDouble(ras[2]);

                    var dec = _util.HoursToHMS(SkyServer.DeclinationXForm, ":", ":", ":", 3);
                    var decs = dec.Split(':');
                    DecDegrees = Convert.ToDouble(decs[0]);
                    DecMinutes = Convert.ToDouble(decs[1]);
                    DecSeconds = Convert.ToDouble(decs[2]);
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

        // goto dialog
        private bool _isRaGoToDialogOpen;
        public bool IsRaGoToDialogOpen
        {
            get => _isRaGoToDialogOpen;
            set
            {
                if (_isRaGoToDialogOpen == value) return;
                _isRaGoToDialogOpen = value;
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _raGoToContent;
        public object RaGoToContent
        {
            get => _raGoToContent;
            set
            {
                if (_raGoToContent == value) return;
                _raGoToContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openRaGoToDialogCommand;
        public ICommand OpenRaGoToDialogCommand
        {
            get
            {
                var command = _openRaGoToDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openRaGoToDialogCommand = new RelayCommand(
                    param => OpenRaGoToDialog()
                );
            }
        }
        private void OpenRaGoToDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var AltAz = Coordinate.RaDec2AltAz(GoToRa, GoToDec, SkyServer.SiderealTime,
                        SkySettings.Latitude);
                    if (AltAz[0] < 0)
                    {
                        OpenDialog($"{Application.Current.Resources["goTargetBelow"]}: {AltAz[1]} Alt: {AltAz[0]}");
                        return;
                    }

                    RaGoToContent = new RaGoToDialog();
                    IsRaGoToDialogOpen = true;
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

        private ICommand _acceptRaGoToDialogCommand;
        public ICommand AcceptRaGoToDialogCommand
        {
            get
            {
                var command = _acceptRaGoToDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptRaGoToDialogCommand = new RelayCommand(
                    param => AcceptRaGoToDialog()
                );
            }
        }
        private void AcceptRaGoToDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkySettings.CanSlewAsync) return;
                    if (AtPark)
                    {
                        BlinkParked();
                        return;
                    }

                    var radec = Transforms.CoordTypeToInternal(GoToRa, GoToDec);
                    SkyServer.SlewRaDec(radec.X, radec.Y);
                    IsRaGoToDialogOpen = false;
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

        private ICommand _cancelRaGoToDialogCommand;
        public ICommand CancelRaGoToDialogCommand
        {
            get
            {
                var command = _cancelRaGoToDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelRaGoToDialogCommand = new RelayCommand(
                    param => CancelRaGoToDialog()
                );
            }
        }
        private void CancelRaGoToDialog()
        {
            try
            {
                IsRaGoToDialogOpen = false;
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

        // Sync dialog
        private bool _isRaGoToSyncDialogOpen;
        public bool IsRaGoToSyncDialogOpen
        {
            get => _isRaGoToSyncDialogOpen;
            set
            {
                if (_isRaGoToSyncDialogOpen == value) return;
                _isRaGoToSyncDialogOpen = value;
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _raGoToSyncContent;
        public object RaGoToSyncContent
        {
            get => _raGoToSyncContent;
            set
            {
                if (_raGoToSyncContent == value) return;
                _raGoToSyncContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openRaGoToSyncDialogCmd;
        public ICommand OpenRaGoToSyncDialogCmd
        {
            get
            {
                var cmd = _openRaGoToSyncDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openRaGoToSyncDialogCmd = new RelayCommand(
                    param => OpenRaGoToSyncDialog()
                );
            }
        }
        private void OpenRaGoToSyncDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var AltAz = Coordinate.RaDec2AltAz(GoToRa, GoToDec, SkyServer.SiderealTime,
                        SkySettings.Latitude);
                    if (AltAz[0] < 0)
                    {
                        OpenDialog($"{Application.Current.Resources["goTargetBelow"]}: {AltAz[1]} Alt: {AltAz[0]}");
                        return;
                    }

                    RaGoToSyncContent = new RaGoToSyncDialog();
                    IsRaGoToSyncDialogOpen = true;
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

        private ICommand _acceptRaGoToSyncDialogCmd;
        public ICommand AcceptRaGoToSyncDialogCmd
        {
            get
            {
                var cmd = _acceptRaGoToSyncDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _acceptRaGoToSyncDialogCmd = new RelayCommand(
                    param => AcceptRaGoToSyncDialog()
                );
            }
        }
        private void AcceptRaGoToSyncDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkySettings.CanSlewAsync) return;
                    if (SkyServer.IsSlewing)
                    {
                        OpenDialog($"{Application.Current.Resources["goSlewing"]}", $"{Application.Current.Resources["exError"]}");
                        return;
                    }
                    if (AtPark)
                    {
                        BlinkParked();
                        return;
                    }

                    var radec = Transforms.CoordTypeToInternal(GoToRa, GoToDec);
                    var result = SkyServer.CheckRaDecSyncLimit(radec.X, radec.Y);

                    if (!result)
                    {
                        OpenDialog($"{Application.Current.Resources["goOutLimits"]}", $"{Application.Current.Resources["exError"]}");
                        return;
                    }
                    SkyServer.TargetDec = radec.Y;
                    SkyServer.TargetRa = radec.X;
                    SkyServer.SyncToTargetRaDec();
                    IsRaGoToSyncDialogOpen = false;
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

        private ICommand _cancelRaGoToSyncDialogCmd;
        public ICommand CancelRaGoToSyncDialogCmd
        {
            get
            {
                var cmd = _cancelRaGoToSyncDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _cancelRaGoToSyncDialogCmd = new RelayCommand(
                    param => CancelRaGoToSyncDialog()
                );
            }
        }
        private void CancelRaGoToSyncDialog()
        {
            try
            {
                IsRaGoToSyncDialogOpen = false;
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

        #region PPEC Control

        private bool _pPecEnabled;
        public bool PPecEnabled
        {
            get => _pPecEnabled;
            set
            {
                if (_pPecEnabled == value) return;
                _pPecEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool PPecOn
        {
            get => SkyServer.PPecOn;
            set
            {
                if (SkyServer.PPecOn == value) return;
                SkyServer.PPecOn = value;
                Synthesizer.Speak(value ? Application.Current.Resources["vcePeckOn"].ToString() : Application.Current.Resources["vcePeckOff"].ToString());
                OnPropertyChanged();
            }
        }

        private bool _pecTrainOn;
        public bool PecTrainOn
        {
            get => _pecTrainOn;
            set
            {
                if (PecTrainOn == value) return;
                _pecTrainOn = value;
                OnPropertyChanged();
            }
        }

        private bool _pecTrainInProgress;
        public bool PecTrainInProgress
        {
            get => _pecTrainInProgress;
            set
            {
                if (PecTrainInProgress == value) return;
                _pecTrainInProgress = value;
                if (!value) PecTrainOn = false;
                OnPropertyChanged();
            }
        }

        private bool _isPPecDialogOpen;
        public bool IsPPecDialogOpen
        {
            get => _isPPecDialogOpen;
            set
            {
                if (_isPPecDialogOpen == value) return;
                _isPPecDialogOpen = value;
                CloseDialogs(value);
                OnPropertyChanged();
            }
        }

        private object _pPecContent;
        public object PPecContent
        {
            get => _pPecContent;
            set
            {
                if (_pPecContent == value) return;
                _pPecContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openPPecDialogCommand;
        public ICommand OpenPPecDialogCommand
        {
            get
            {
                var command = _openPPecDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openPPecDialogCommand = new RelayCommand(
                    param => OpenPPecDialog()
                );
            }
        }
        private void OpenPPecDialog()
        {
            try
            {
                if (SkyServer.Tracking || SkyServer.PecTrainInProgress)
                {
                    PPecContent = new PpecDialog();
                    IsPPecDialogOpen = true;
                }
                else
                {
                    PecTrainOn = false;
                    OpenDialog(Application.Current.Resources["ppTrackingOn"].ToString());
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

        private ICommand _acceptPPecDialogCommand;
        public ICommand AcceptPPecDialogCommand
        {
            get
            {
                var command = _acceptPPecDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptPPecDialogCommand = new RelayCommand(
                    param => AcceptPPecDialog()
                );
            }
        }
        private void AcceptPPecDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.PecTraining = !SkyServer.PecTraining;
                    IsPPecDialogOpen = false;
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

        private ICommand _cancelPPecDialogCommand;
        public ICommand CancelPPecDialogCommand
        {
            get
            {
                var command = _cancelPPecDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelPPecDialogCommand = new RelayCommand(
                    param => CancelPPecDialog()
                );
            }
        }
        private void CancelPPecDialog()
        {
            try
            {
                PecTrainOn = !PecTrainOn;
                IsPPecDialogOpen = false;
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

        private bool _flipNS;
        public bool FlipNS
        {
            get => _flipNS;
            set
            {
                if (_flipNS == value) return;
                _flipNS = value;
                OnPropertyChanged();
            }
        }

        private bool _flipEW;
        public bool FlipEW
        {
            get => _flipEW;
            set
            {
                if (_flipEW == value) return;
                _flipEW = value;
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

        private void SetHCFlipsVisibility()
        {
            switch (HcMode)
            {
                case HCMode.Axes:
                    EWEnabled = true;
                    NSEnabled = true;
                    break;
                //case HCMode.Compass:
                //    EWVisibility = false;
                //    NSVisibility = false;
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
                    BlinkParked();
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
                    BlinkParked();
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
                    BlinkParked();
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
                    BlinkParked();
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
                var win = Application.Current.Windows.OfType<HandControlV>().FirstOrDefault();
                if (win != null) return;
                var bWin = new HandControlV();
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

        private void StartSlew(SlewDirection direction)
        {
            if (SkyServer.AtPark)
            {
                return;
            }
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
                    if (SpiralOutCmd.CanExecute(null))
                        SpiralOutCmd.Execute(null);
                }

                if (e.XButton2 == MouseButtonState.Pressed)
                {
                    if (SpiralInCmd.CanExecute(null))
                        SpiralInCmd.Execute(null);
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

        private ICommand _spiralInCmd;
        public ICommand SpiralInCmd
        {
            get
            {
                var command = _spiralInCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralInCmd = new RelayCommand(
                    param => SpiralIn()
                );
            }
        }
        private void SpiralIn()
        {
            try
            {
                if (AtPark)
                {
                    LogSpiral($"SpiralIn: Parked", MonitorType.Warning);
                    return;
                }

                if (!SkyServer.Tracking) SkyServer.Tracking = true;
                SpiralMove(-1);
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

        private ICommand _spiralOutCmd;
        public ICommand SpiralOutCmd
        {
            get
            {
                var command = _spiralOutCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralOutCmd = new RelayCommand(
                    param => SpiralOut()
                );
            }
        }
        private void SpiralOut()
        {
            try
            {
                if (AtPark)
                {
                    LogSpiral($"SpiralOut: Parked", MonitorType.Warning);
                    return;
                }
                if (!SkyServer.Tracking) SkyServer.Tracking = true;
                SpiralMove(1);
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

        private ICommand _abortCmd;
        public ICommand AbortCmd
        {
            get
            {
                var command = _abortCmd;
                if (command != null)
                {
                    return command;
                }

                return _abortCmd = new RelayCommand(
                    param => Abort()
                );
            }
        }
        private void Abort()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.AbortSlew(true);
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

        private ICommand _syncCmd;
        public ICommand SyncCmd
        {
            get
            {
                var command = _syncCmd;
                if (command != null)
                {
                    return command;
                }

                return _syncCmd = new RelayCommand(
                    param => Sync()
                );
            }
        }
        private void Sync()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.SyncToTargetRaDec();
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

        private ICommand _spiralGenerateCmd;
        public ICommand SpiralGenerateCmd
        {
            get
            {
                var cmd = _spiralGenerateCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _spiralGenerateCmd = new RelayCommand(
                    param => SpiralGenerate()
                );
            }
        }
        private void SpiralGenerate()
        {
            try
            {
                if (AtPark) return;
                if (!SkyServer.Tracking) SkyServer.Tracking = true;
                GenerateSpiral();
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
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
            }
        }

        private static void GenerateSpiral()
        {
            var width = SkySettings.SpiralWidth / 15.0;
            var height = SkySettings.SpiralHeight;
            var raAdd = Principles.Units.Ra2Dou(0, 0, width);
            var decAdd = Principles.Units.Deg2Dou(0, 0, height);

            var count = 0;
            var index = 0;
            var ra = SkyServer.RightAscensionXForm;
            var dec = SkyServer.DeclinationXForm;

            SkyServer.SpiralCollection.Clear();
            SkySettings.SpiralDistance = 0;
            var a = new SpiralPoint { RaDec = new Point(ra, dec), Index = index, Status = SpiralPointStatus.Current };
            SkyServer.SpiralCollection.Add(a);

            while (count < 7 || index < 49)
            {
                count++;

                //down
                for (var i = 0; i < count; i++)
                {
                    index++;
                    ra -= raAdd;
                    ra = Range.Range24(ra);
                    var d = new SpiralPoint { RaDec = new Point(ra, dec), Index = index, Status = SpiralPointStatus.Clear };
                    SkyServer.SpiralCollection.Add(d);
                }
                //right
                for (var i = 0; i < count; i++)
                {
                    index++;
                    dec += decAdd;
                    dec = Range.Range90(dec);
                    var r = new SpiralPoint { RaDec = new Point(ra, dec), Index = index, Status = SpiralPointStatus.Clear };
                    SkyServer.SpiralCollection.Add(r);
                }
                count++;
                //up
                for (var i = 0; i < count; i++)
                {
                    index++;
                    ra += raAdd;
                    ra = Range.Range24(ra);
                    var u = new SpiralPoint { RaDec = new Point(ra, dec), Index = index, Status = SpiralPointStatus.Clear };
                    SkyServer.SpiralCollection.Add(u);
                }
                //left
                for (var i = 0; i < count; i++)
                {
                    index++;
                    dec -= decAdd;
                    dec = Range.Range90(dec);
                    var l = new SpiralPoint { RaDec = new Point(ra, dec), Index = index, Status = SpiralPointStatus.Clear };
                    SkyServer.SpiralCollection.Add(l);
                }
            }
            SkyServer.SpiralChanged = true;

            //create reset limit - measure distance to farthest points on the spiral and chooses the largest
            var farthest = 1.0;
            const double extraSpace = .4;
            var corner = SkyServer.SpiralCollection[30];
            if (corner != null)
            {
                var distance = Calculations.AngularDistance(a.RaDec.X, a.RaDec.Y, corner.RaDec.X, corner.RaDec.Y);
                if (distance > farthest) farthest = distance + distance * extraSpace;
            }
            corner = SkyServer.SpiralCollection[36];
            if (corner != null)
            {
                var distance = Calculations.AngularDistance(a.RaDec.X, a.RaDec.Y, corner.RaDec.X, corner.RaDec.Y);
                if (distance > farthest) farthest = distance + distance * extraSpace;
            }
            corner = SkyServer.SpiralCollection[42];
            if (corner != null)
            {
                var distance = Calculations.AngularDistance(a.RaDec.X, a.RaDec.Y, corner.RaDec.X, corner.RaDec.Y);
                if (distance > farthest) farthest = distance + distance * extraSpace;
            }
            corner = SkyServer.SpiralCollection[48];
            if (corner != null)
            {
                var distance = Calculations.AngularDistance(a.RaDec.X, a.RaDec.Y, corner.RaDec.X, corner.RaDec.Y);
                if (distance > farthest) farthest = distance + distance * extraSpace;
            }

            SkySettings.SpiralDistance = Math.Round(farthest, 1);

            LogSpiral($"GenerateSpiral: {SkyServer.SpiralCollection.Count}", MonitorType.Information);
            Synthesizer.Speak(Application.Current.Resources["1021New"].ToString());
        }

        private static void SpiralMove(int amt)
        {
            if (SkyServer.SpiralCollection.Count == 0) GenerateSpiral();
            if (!SkyServer.SpiralCollection.Exists(x => x.Status == SpiralPointStatus.Current))
            {
                LogSpiral($"{amt}, Can't find Current", MonitorType.Warning);
                return;
            }

            var currentpoint = SkyServer.SpiralCollection.Find(x => x.Status == SpiralPointStatus.Current);
            var index = currentpoint.Index + amt;
            if (index < 0)
            {
                Synthesizer.Speak(Application.Current.Resources["1021Center"].ToString());
                return;
            }
            if (index > 48)
            {
                Synthesizer.Speak(Application.Current.Resources["1021End"].ToString());
                return;
            }

            if (!SkyServer.SpiralCollection.Exists(x => x.Index == index))
            {
                LogSpiral($"{amt}, Index not Exists", MonitorType.Warning);
                return;
            }
            var newpoint = SkyServer.SpiralCollection.Find(x => x.Index == index);
            if (newpoint?.RaDec == null)
            {
                LogSpiral($"{amt}, Index not Found", MonitorType.Warning);
                return;
            }

            LogSpiral($"Spiral Move: {_util.HoursToHMS(newpoint.RaDec.X, "h ", "m ", "s", 2)}, {_util.DegreesToDMS(newpoint.RaDec.Y, "° ", "m ", "s", 2)}, {amt}", MonitorType.Information);

            var radec = Transforms.CoordTypeToInternal(newpoint.RaDec.X, newpoint.RaDec.Y);

            // check for possible flip
            if (SkySettings.SpiralLimits)
            {
                var flipRequired = Axes.IsFlipRequired(new[] { radec.X, radec.Y });
                if (flipRequired)
                {
                    LogSpiral(Application.Current.Resources["1021FlipLimit"].ToString(), MonitorType.Warning);
                    Synthesizer.Speak(Application.Current.Resources["1021FlipLimit"].ToString());
                    return;
                }
            }

            SkyServer.SlewRaDec(radec.X, radec.Y);
            currentpoint.Status = SpiralPointStatus.Visited;
            newpoint.Status = SpiralPointStatus.Current;
            SkyServer.SpiralChanged = true;
        }

        private static void LogSpiral(string msg, MonitorType typ)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = typ,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        #endregion

        #region Backlash

        public IEnumerable<int> RaBacklashList { get; }

        public IEnumerable<int> DecBacklashList { get; }

        public int DecBacklash
        {
            get => SkySettings.DecBacklash;
            set
            {
                if (SkySettings.DecBacklash == value) return;
                SkySettings.DecBacklash = value;
                OnPropertyChanged();
            }
        }

        public int RaBacklash
        {
            get => SkySettings.RaBacklash;
            set
            {
                if (SkySettings.RaBacklash == value) return;
                SkySettings.RaBacklash = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Bottom Bar Control

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

        private bool _isSlewing;
        public bool IsSlewing
        {
            get => _isSlewing;
            set
            {
                if (IsSlewing == value) return;
                _isSlewing = value;
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

        private string _trackingRateIcon;
        public string TrackingRateIcon
        {
            get => _trackingRateIcon;
            set
            {
                if (_trackingRateIcon == value) return;
                _trackingRateIcon = value;
                OnPropertyChanged();
            }
        }

        private void SetTrackingIcon(DriveRates rate)
        {
            switch (rate)
            {
                case DriveRates.driveSidereal:
                    TrackingRateIcon = "Earth";
                    break;
                case DriveRates.driveLunar:
                    TrackingRateIcon = "NightSky";
                    break;
                case DriveRates.driveSolar:
                    TrackingRateIcon = "WhiteBalanceSunny";
                    break;
                case DriveRates.driveKing:
                    TrackingRateIcon = "ChessKing";
                    break;
                default:
                    TrackingRateIcon = "Help";
                    break;
            }
        }

        private PierSide _isSideOfPier;
        public PierSide IsSideOfPier
        {
            get => _isSideOfPier;
            set
            {
                if (value == _isSideOfPier) return;
                _isSideOfPier = value;
                OnPropertyChanged();
                BlinkSop();
            }
        }

        private bool _limitAlarm;
        public bool LimitAlarm
        {
            get => _limitAlarm;
            set
            {
                if (LimitAlarm == value) return;
                _limitAlarm = value;
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

        private ParkPosition _parkLimitSelection;
        public ParkPosition ParkLimitSelection
        {
            get => _parkLimitSelection;
            set
            {
                if (_parkLimitSelection == value) return;
                _parkLimitSelection = value;
                SkySettings.ParkLimitName = value.Name;
                OnPropertyChanged();
            }
        }

        private bool _warningState;
        public bool WarningState
        {
            get => _warningState;
            set
            {
                if (_warningState == value) return;
                _warningState = value;
                OnPropertyChanged();
            }
        }

        private bool _alertState;
        public bool AlertState
        {
            get => _alertState;
            set
            {
                if (AlertState == value) return;
                _alertState = value;
                OnPropertyChanged();
            }
        }

        private bool _voiceState;
        public bool VoiceState
        {
            get => _voiceState;
            set
            {
                if (value == VoiceState) return;
                _voiceState = value;
                OnPropertyChanged();
            }
        }
        public bool MonitorState
        {
            get => Shared.Settings.StartMonitor;
            set
            {
                if (value == MonitorState) return;
                OnPropertyChanged();
            }
        }

        private bool _pecState;
        public bool PecState
        {
            get => _pecState;
            set
            {
                if (_pecState == value) return;
                _pecState = value;
                OnPropertyChanged();
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public bool MountState
        {
            get => SkyServer.IsMountRunning;
            set
            {
                ScreenEnabled = value;
                ConnectButtonContent = value ? Application.Current.Resources["skyDisconnect"].ToString() : Application.Current.Resources["skyConnect"].ToString();
            }
        }

        private string _connectButtonContent;
        public string ConnectButtonContent
        {
            get => _connectButtonContent;
            set
            {
                if (ConnectButtonContent == value) return;
                _connectButtonContent = value;
                OnPropertyChanged();
            }
        }

        private bool _parkedBlinker;
        public bool ParkedBlinker
        {
            get => _parkedBlinker;
            set
            {
                _parkedBlinker = value;
                OnPropertyChanged();
            }
        }
        public void BlinkParked()
        {
            for (var i = 0; i < 10; i++)
            {
                ParkedBlinker = !ParkedBlinker;
            }
        }

        private bool _sopBlinker;
        public bool SopBlinker
        {
            get => _sopBlinker;
            set
            {
                _sopBlinker = value;
                OnPropertyChanged();
            }
        }
        public void BlinkSop()
        {
            for (var i = 0; i < 4; i++)
            {
                SopBlinker = !SopBlinker;
            }
        }

        private bool _isLimitDialogOpen;
        public bool IsLimitDialogOpen
        {
            get => _isLimitDialogOpen;
            set
            {
                if (_isLimitDialogOpen == value) return;
                _isLimitDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _limitContent;
        public object LimitContent
        {
            get => _limitContent;
            set
            {
                if (_limitContent == value) return;
                _limitContent = value;
                OnPropertyChanged();
            }
        }

        private void SetParkLimitSelection(string name)
        {
            var found = ParkPositions.Find(x => x.Name == name);
            if (found != null) // did not find match in list
            {
                ParkLimitSelection = found;
            }
            else
            {
                ParkLimitSelection = ParkPositions.FirstOrDefault();
            }
        }

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
                    LimitTracking = SkySettings.LimitTracking;
                    LimitPark = SkySettings.LimitPark;
                    SetParkLimitSelection(SkySettings.ParkLimitName);
                    if (!LimitPark && !LimitTracking) { LimitNothing = true; }
                    if (LimitPark || LimitTracking) { LimitNothing = false; }
                    LimitContent = new LimitDialog();
                    IsLimitDialogOpen = true;
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
                    IsLimitDialogOpen = false;
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

        private ICommand _clearWarningCommand;
        public ICommand ClearWarningCommand
        {
            get
            {
                var command = _clearWarningCommand;
                if (command != null)
                {
                    return command;
                }

                return _clearWarningCommand = new RelayCommand(
                    param => ClearWarningState()
                );
            }
        }
        private static void ClearWarningState()
        {
            MonitorQueue.WarningState = false;
        }

        private ICommand _clearErrorsCommand;
        public ICommand ClearErrorsCommand
        {
            get
            {
                var command = _clearErrorsCommand;
                if (command != null)
                {
                    return command;
                }

                return _clearErrorsCommand = new RelayCommand(
                    param => ClearErrorAlert()
                );
            }
        }
        private static void ClearErrorAlert()
        {
            SkyServer.AlertState = false;
        }

        private ICommand _clickConnectCommand;
        public ICommand ClickConnectCommand
        {
            get
            {
                var command = _clickConnectCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickConnectCommand = new RelayCommand(
                    param => ClickConnect()
                );
            }
        }
        private void ClickConnect()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (SkyServer.IsAutoHomeRunning)
                    {
                        StopAutoHomeDialog();
                        return;
                    }
                    SkyServer.IsMountRunning = !SkyServer.IsMountRunning;
                }

                if (SkyServer.IsMountRunning)
                {
                    WarningState = false;
                    AlertState = false;
                    HomePositionCheck();
                }
                else
                {
                    CloseDialogs(false);
                }
            }
            catch (Exception ex)
            {
                SkyServer.SkyErrorHandler(ex);
            }
        }

        private void HomePositionCheck()
        {
            if (SkyServer.AtPark) return;
            if (!SkySettings.HomeWarning) return;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    break;
                case MountType.SkyWatcher:
                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            break;
                        case AlignmentModes.algPolar:
                            break;
                        case AlignmentModes.algGermanPolar:
                            var msg = Application.Current.Resources["skyHome1"].ToString();
                            msg += Environment.NewLine + Application.Current.Resources["skyHome2"];
                            msg += Environment.NewLine + Application.Current.Resources["skyHome3"];
                            OpenDialog(msg);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ICommand _clickMountInfoDialogCommand;
        public ICommand ClickMountInfoDialogCommand
        {
            get
            {
                var command = _clickMountInfoDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickMountInfoDialogCommand = new RelayCommand(
                    param => ClickMountInfoDialog()
                );
            }
        }
        private void ClickMountInfoDialog()
        {
            try
            {
                var canppec = SkyServer.CanPPec ? $"{Application.Current.Resources["mntSupported"]}" : $"{Application.Current.Resources["mntNotSupported"]}";
                var canhome = SkyServer.CanHomeSensor ? $"{Application.Current.Resources["mntSupported"]}" : $"{Application.Current.Resources["mntNotSupported"]}";
                var msg = $"{Application.Current.Resources["mntMount"]} {SkyServer.MountName}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntVersion"]} {SkyServer.MountVersion}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntStepsRa"]} {SkyServer.StepsPerRevolution[0]}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntStepsDec"]} {SkyServer.StepsPerRevolution[1]}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntPEC"]} {canppec}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntHomeSensor"]} {canhome}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntRaSteps"]} {Math.Round(SkyServer.StepsPerRevolution[0] / 360.0 / 3600, 2)}" + Environment.NewLine;
                msg += $"{Application.Current.Resources["mntDecSteps"]} {Math.Round(SkyServer.StepsPerRevolution[1] / 360.0 / 3600, 2)}" + Environment.NewLine;

                OpenDialog(msg, $"{Application.Current.Resources["mntInfo"]}");
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
            DialogCaption = caption ?? Application.Current.Resources["diaDialog"].ToString();
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
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private static void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
        }

        #endregion

        #region GPS Dialog

        private bool _allowTimeChange;
        public bool AllowTimeChange
        {
            get => _allowTimeChange;
            set
            {
                if (_allowTimeChange == value) return;
                _allowTimeChange = value;
                OnPropertyChanged();
            }
        }

        private bool _allowTimeVis;
        public bool AllowTimeVis
        {
            get => _allowTimeVis;
            set
            {
                if (_allowTimeVis == value) return;
                _allowTimeVis = value;
                OnPropertyChanged();
            }
        }

        private bool _gpsGga;
        public bool GpsGga
        {
            get => _gpsGga;
            set
            {
                if (_gpsGga == value) return;
                _gpsGga = value;
                OnPropertyChanged();
            }
        }

        private bool _gpsRmc;
        public bool GpsRmc
        {
            get => _gpsRmc;
            set
            {
                if (_gpsRmc == value) return;
                _gpsRmc = value;
                OnPropertyChanged();
            }
        }

        private DateTime _gpsPcTime;
        public DateTime GpsPcTime
        {
            get => _gpsPcTime;
            set
            {
                if (_gpsPcTime == value) return;
                _gpsPcTime = value;
                OnPropertyChanged();
            }
        }

        private DateTime _gpsTime;
        public DateTime GpsTime
        {
            get => _gpsTime;
            set
            {
                if (_gpsTime == value) return;
                _gpsTime = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _gpsSpan;
        public TimeSpan GpsSpan
        {
            get => _gpsSpan;
            set
            {
                if (_gpsSpan == value) return;
                _gpsSpan = value;
                OnPropertyChanged();
            }
        }

        private string _nmeaTag;
        public string NmeaTag
        {
            get => _nmeaTag;
            set
            {
                if (_nmeaTag == value) return;
                _nmeaTag = value;
                OnPropertyChanged();
            }
        }
        private string NmeaSentence { get; set; }
        private bool _hasGpsData;
        public bool HasGSPData
        {
            get => _hasGpsData;
            set
            {
                if (_hasGpsData == value) return;
                _hasGpsData = value;
                OnPropertyChanged();
            }
        }
        public double GpsLat { get; set; }
        private string _gpsLatString;
        public string GpsLatString
        {
            get => _gpsLatString;
            set
            {
                if (value == _gpsLatString) return;
                _gpsLatString = value;
                OnPropertyChanged();
            }
        }
        public double GpsLong { get; set; }
        private string _gpsLongString;
        public string GpsLongString
        {
            get => _gpsLongString;
            set
            {
                if (value == _gpsLongString) return;
                _gpsLongString = value;
                OnPropertyChanged();
            }
        }
        private double _gpsElevation;
        public double GpsElevation
        {
            get => _gpsElevation;
            set
            {
                if (Math.Abs(value - _gpsElevation) < 0.00001) return;
                _gpsElevation = value;
                OnPropertyChanged();
            }
        }
        public int GpsComPort
        {
            get => SkySettings.GpsComPort;
            set
            {
                if (value == SkySettings.GpsComPort) return;
                SkySettings.GpsComPort = value;
                OnPropertyChanged();
            }
        }
        public bool IsGpsRunning { get; set; }
        public SerialSpeed GpsBaudRate
        {
            get => SkySettings.GpsBaudRate;
            set
            {
                if (value == SkySettings.GpsBaudRate) return;
                SkySettings.GpsBaudRate = value;
                OnPropertyChanged();
            }
        }
        private ICommand _populateGps;
        public ICommand PopulateGpsCommand
        {
            get
            {
                var gps = _populateGps;
                if (gps != null)
                {
                    return gps;
                }

                return _populateGps = new RelayCommand(
                    param => PopulateGps()
                );
            }
        }
        private void PopulateGps()
        {
            try
            {
                using (new WaitCursor())
                {
                    //var ra = _util.HoursToHMS(SkyServer.RightAscension, ":", ":", ":", 3);
                    //var ras = ra.Split(':');
                    //RaHours = Convert.ToDouble(ras[0]);
                    //RaMinutes = Convert.ToDouble(ras[1]);
                    //RaSeconds = Convert.ToDouble(ras[2]);

                    //var dec = _util.HoursToHMS(SkyServer.Declination, ":", ":", ":", 3);
                    //var decs = dec.Split(':');
                    //DecDegrees = Convert.ToDouble(decs[0]);
                    //DecMinutes = Convert.ToDouble(decs[1]);
                    //DecSeconds = Convert.ToDouble(decs[2]);
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
        private bool _isGpsDialogOpen;
        public bool IsGpsDialogOpen
        {
            get => _isGpsDialogOpen;
            set
            {
                if (_isGpsDialogOpen == value) return;
                _isGpsDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _gpsContent;
        public object GpsContent
        {
            get => _gpsContent;
            set
            {
                if (_gpsContent == value) return;
                _gpsContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openGpsDialogCommand;
        public ICommand OpenGpsDialogCommand
        {
            get
            {
                var command = _openGpsDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openGpsDialogCommand = new RelayCommand(
                    param => OpenGpsDialog()
                );
            }
        }
        private void OpenGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    HasGSPData = false;
                    NmeaTag = "N/A";
                    GpsLong = 0.0;
                    GpsLongString = $"{GpsLong}";
                    GpsLat = 0.0;
                    GpsLatString = $"{GpsLat}";
                    GpsElevation = 0.0;
                    GpsSpan = new TimeSpan(0);
                    GpsPcTime = new DateTime();
                    GpsTime = new DateTime();
                    GpsGga = true;
                    GpsRmc = false;
                    AllowTimeChange = false;
                    AllowTimeVis = SystemInfo.IsAdministrator();

                    GpsContent = new GpsDialog();
                    IsGpsDialogOpen = true;
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

        private ICommand _acceptGpsDialogCommand;
        public ICommand AcceptGpsDialogCommand
        {
            get
            {
                var command = _acceptGpsDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptGpsDialogCommand = new RelayCommand(
                    param => AcceptGpsDialog()
                );
            }
        }
        private void AcceptGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (Math.Abs(GpsLat) > 0.0 && Math.Abs(GpsLong) > 0.0)
                    {
                        SkySettings.Latitude = GpsLat;
                        SkySettings.Longitude = GpsLong;
                        SkySettings.Elevation = GpsElevation;
                    }

                    if (AllowTimeChange && AllowTimeVis)
                    {
                        if (Math.Abs(GpsSpan.TotalSeconds) > 300)
                        {
                            OpenDialog(Application.Current.Resources["gpsTimeLimit"].ToString());
                        }
                        else
                        {
                            var msg = Time.SetSystemUTCTime(HiResDateTime.UtcNow.ToLocalTime().Add(GpsSpan));
                            if (msg != string.Empty) OpenDialog($"{Application.Current.Resources["gpsTimeError"]}: {msg}");
                        }
                    }

                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"{NmeaSentence}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    IsGpsDialogOpen = false;
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

        private ICommand _retrieveGpsDialogCommand;
        public ICommand RetrieveGpsDialogCommand
        {
            get
            {
                var command = _retrieveGpsDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _retrieveGpsDialogCommand = new RelayCommand(
                    param => RetrieveGpsDialog()
                );
            }
        }
        private void RetrieveGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!GpsGga && !GpsRmc) return;
                    if (IsGpsRunning) return;
                    IsGpsRunning = true;
                    HasGSPData = false;
                    var gpsHardware = new GpsHardware(GpsComPort, GpsBaudRate) { Gga = GpsGga, Rmc = GpsRmc };
                    gpsHardware.GpsOn();
                    var stopwatch = Stopwatch.StartNew();
                    while (gpsHardware.GpsRunning && stopwatch.Elapsed.TotalSeconds < 30)
                    {
                        if (gpsHardware.HasData) break;
                    }


                    if (gpsHardware.HasData)
                    {
                        GpsLong = gpsHardware.Longitude;
                        GpsLongString = _util.DegreesToDMS(GpsLong, "° ", ":", "", 2);
                        GpsLat = gpsHardware.Latitude;
                        GpsLatString = _util.DegreesToDMS(GpsLat, "° ", ":", "", 2);
                        GpsElevation = gpsHardware.Altitude;
                        NmeaTag = gpsHardware.NmEaTag;
                        GpsPcTime = gpsHardware.PcUtcNow.ToLocalTime();
                        GpsTime = gpsHardware.TimeStamp.ToLocalTime();
                        GpsSpan = gpsHardware.TimeSpan;
                        NmeaSentence = gpsHardware.NmEaSentence;
                        HasGSPData = true;
                        gpsHardware.GpsOff();
                    }
                    else
                    {
                        gpsHardware.GpsOff();
                        OpenDialog($"{Application.Current.Resources["gpsNoData"]}{GpsComPort}{Environment.NewLine}{gpsHardware.NmEaSentence}");
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
                IsGpsDialogOpen = false;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
            finally
            {
                IsGpsRunning = false;
            }
        }

        private ICommand _cancelGpsDialogCommand;
        public ICommand CancelGpsDialogCommand
        {
            get
            {
                var command = _cancelGpsDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelGpsDialogCommand = new RelayCommand(
                    param => CancelGpsDialog()
                );
            }
        }
        private void CancelGpsDialog()
        {
            try
            {
                IsGpsDialogOpen = false;
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

        #region CdC Dialog
        public double CdcLat { get; set; }
        private string _cdcLatString;
        public string CdcLatString
        {
            get => Math.Abs(CdcLat) <= 0 ? "0" : _cdcLatString;
            set
            {
                if (value == _cdcLatString) return;
                _cdcLatString = value;
                OnPropertyChanged();
            }
        }

        public double CdcLong { get; set; }
        private string _cdcLongString;
        public string CdcLongString
        {
            get => Math.Abs(CdcLong) <= 0 ? "0" : _cdcLongString;
            set
            {
                if (value == _cdcLongString) return;
                _cdcLongString = value;
                OnPropertyChanged();
            }
        }

        private double _cdcElevation;
        public double CdcElevation
        {
            get => _cdcElevation;
            set
            {
                if (Math.Abs(value - _cdcElevation) < 0.00001) return;
                _cdcElevation = value;
                OnPropertyChanged();
            }
        }

        private int _cdcPortNumber;
        public int CdcPortNumber
        {
            get => Properties.SkyTelescope.Default.CdCport;
            set
            {
                if (value == _cdcPortNumber) return;
                _cdcPortNumber = value;
                Properties.SkyTelescope.Default.CdCport = value;
                OnPropertyChanged();
            }
        }

        private string _cdcIpAddress;
        public string CdcIpAddress
        {
            get => Properties.SkyTelescope.Default.CdCip;
            set
            {
                if (value == _cdcIpAddress) return;
                _cdcIpAddress = value;
                Properties.SkyTelescope.Default.CdCip = value;
                OnPropertyChanged();
            }
        }

        private ICommand _populateCdc;
        public ICommand PopulateCdcCommand
        {
            get
            {
                var cdc = _populateCdc;
                if (cdc != null)
                {
                    return cdc;
                }

                return _populateCdc = new RelayCommand(
                    param => PopulateCdc()
                );
            }
        }
        private void PopulateCdc()
        {
            try
            {
                using (new WaitCursor())
                {
                    CdcElevation = SkySettings.Elevation;
                    CdcLong = SkySettings.Longitude;
                    CdcLongString = _util.DegreesToDMS(CdcLong, "° ", ":", "", 2);
                    CdcLat = SkySettings.Latitude;
                    CdcLatString = _util.DegreesToDMS(CdcLat, "° ", ":", "", 2);
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

        private bool _isCdcDialogOpen;
        public bool IsCdcDialogOpen
        {
            get => _isCdcDialogOpen;
            set
            {
                if (_isCdcDialogOpen == value) return;
                _isCdcDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _cdcContent;
        public object CdcContent
        {
            get => _cdcContent;
            set
            {
                if (_cdcContent == value) return;
                _cdcContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openCdcDialogCommand;
        public ICommand OpenCdcDialogCommand
        {
            get
            {
                var command = _openCdcDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openCdcDialogCommand = new RelayCommand(
                    param => OpenCdcDialog()
                );
            }
        }
        private void OpenCdcDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    CdcContent = new CdcDialog();
                    PopulateCdc();
                    IsCdcDialogOpen = true;
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

        private ICommand _acceptCdcDialogCommand;
        public ICommand AcceptCdcDialogCommand
        {
            get
            {
                var command = _acceptCdcDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptCdcDialogCommand = new RelayCommand(
                    param => AcceptCdcDialog()
                );
            }
        }
        private void AcceptCdcDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkySettings.Latitude = CdcLat;
                    SkySettings.Longitude = CdcLong;
                    SkySettings.Elevation = CdcElevation;
                    IsCdcDialogOpen = false;
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

        private ICommand _retrieveCdcDialogCommand;
        public ICommand RetrieveCdcDialogCommand
        {
            get
            {
                var command = _retrieveCdcDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _retrieveCdcDialogCommand = new RelayCommand(
                    param => RetrieveCdcDialog()
                );
            }
        }
        private void RetrieveCdcDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var cdcServer = new CdcServer(CdcIpAddress, CdcPortNumber);
                    var darray = cdcServer.GetObs();
                    CdcLat = darray[0];
                    CdcLatString = _util.DegreesToDMS(CdcLat, "° ", ":", "", 2);
                    CdcLong = darray[1];
                    CdcLongString = _util.DegreesToDMS(CdcLong, "° ", ":", "", 2);
                    CdcElevation = darray[2];
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

        private ICommand _sendObsCdcDialogCommand;
        public ICommand SendObsCdcDialogCommand
        {
            get
            {
                var command = _sendObsCdcDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _sendObsCdcDialogCommand = new RelayCommand(
                    param => SendObsCdcDialog()
                );
            }
        }
        private void SendObsCdcDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var cdcServer = new CdcServer(CdcIpAddress, CdcPortNumber);
                    cdcServer.SetObs(SkySettings.Latitude, SkySettings.Longitude, SkySettings.Elevation);
                    IsCdcDialogOpen = false;
                    OpenDialog("Data sent: Open CdC and save the observatory location");
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

        private ICommand _cancelCdcDialogCommand;
        public ICommand CancelCdcDialogCommand
        {
            get
            {
                var command = _cancelCdcDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelCdcDialogCommand = new RelayCommand(
                    param => CancelCdcDialog()
                );
            }
        }
        private void CancelCdcDialog()
        {
            try
            {
                IsCdcDialogOpen = false;
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

        #region Viewport3D

        private double xAxisOffset;
        private double yAxisOffset;
        private double zAxisOffset;

        private bool _modelWinVisibility;
        public bool ModelWinVisibility
        {
            get => _modelWinVisibility;
            set
            {
                if (_modelWinVisibility == value) return;
                _modelWinVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool _cameraVis;
        public bool CameraVis
        {
            get => _cameraVis;
            set
            {
                if (_cameraVis == value) return;
                _cameraVis = value;
                OnPropertyChanged();
            }
        }

        private Point3D _position;
        public Point3D Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                OnPropertyChanged();
            }
        }

        private Vector3D _lookDirection;
        public Vector3D LookDirection
        {
            get => _lookDirection;
            set
            {
                if (_lookDirection == value) return;
                _lookDirection = value;
                OnPropertyChanged();
            }
        }

        private Vector3D _upDirection;
        public Vector3D UpDirection
        {
            get => _upDirection;
            set
            {
                if (_upDirection == value) return;
                _upDirection = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Media.Media3D.Model3D _model;
        public System.Windows.Media.Media3D.Model3D Model
        {
            get => _model;
            set
            {
                if (_model == value) return;
                _model = value;
                OnPropertyChanged();
            }
        }
        public bool ModelOn
        {
            get => SkySettings.ModelOn;
            set
            {
                SkySettings.ModelOn = value;
                if (value)
                {
                    Rotate();
                    LoadGEM();
                }
                OnPropertyChanged();
            }
        }

        private double _xAxis;
        public double XAxis
        {
            get => _xAxis;
            set
            {
                _xAxis = value;
                XAxisOffset = value + xAxisOffset;
                OnPropertyChanged();
            }
        }

        private double _yAxis;
        public double YAxis
        {
            get => _yAxis;
            set
            {
                _yAxis = value;
                YAxisOffset = value + yAxisOffset;
                OnPropertyChanged();
            }
        }

        private double _zAxis;
        public double ZAxis
        {
            get => _zAxis;
            set
            {
                _zAxis = value;
                ZAxisOffset = zAxisOffset - value;
                OnPropertyChanged();
            }
        }

        private double _xAxisOffset;
        public double XAxisOffset
        {
            get => _xAxisOffset;
            set
            {
                _xAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private double _yAxisOffset;
        public double YAxisOffset
        {
            get => _yAxisOffset;
            set
            {
                _yAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private double _zAxisOffset;
        public double ZAxisOffset
        {
            get => _zAxisOffset;
            set
            {
                _zAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private Material _compass;
        public Material Compass
        {
            get => _compass;
            set
            {
                _compass = value;
                OnPropertyChanged();
            }
        }
        private void LoadGEM()
        {
            try
            {
                CameraVis = false;

                //camera direction
                LookDirection = Settings.Settings.ModelLookDirection2;
                UpDirection = Settings.Settings.ModelUpDirection2;
                Position = Settings.Settings.ModelPosition2;

                //offset for model to match start position
                xAxisOffset = 90;
                yAxisOffset = -90;
                zAxisOffset = 0;

                //start position
                XAxis = -90;
                YAxis = 90;
                ZAxis = Math.Round(Math.Abs(SkySettings.Latitude), 2);

                //load model and compass
                var import = new ModelImporter();
                var model = import.Load(Shared.Model3D.GetModelFile(Settings.Settings.ModelType));
                Compass = MaterialHelper.CreateImageMaterial(Shared.Model3D.GetCompassFile(SkyServer.SouthernHemisphere), 100);

                //color OTA
                var accentColor = Settings.Settings.AccentColor;
                if (!string.IsNullOrEmpty(accentColor))
                {
                    var swatches = new SwatchesProvider().Swatches;
                    foreach (var swatch in swatches)
                    {
                        if (swatch.Name != Settings.Settings.AccentColor) continue;
                        var converter = new BrushConverter();
                        var accentbrush = (Brush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());

                        var materialota = MaterialHelper.CreateMaterial(accentbrush);
                        if (model.Children[0] is GeometryModel3D ota) ota.Material = materialota;
                    }
                }
                //color weights
                var materialweights = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromRgb(64, 64, 64)));
                if (model.Children[1] is GeometryModel3D weights) weights.Material = materialweights;
                //color bar
                var materialbar = MaterialHelper.CreateMaterial(Brushes.Gainsboro);
                if (model.Children[2] is GeometryModel3D bar) bar.Material = materialbar;

                Model = model;
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
        private void Rotate()
        {
            if (!ModelOn) return;

            var axes = Shared.Model3D.RotateModel(SkySettings.Mount.ToString(), SkyServer.ActualAxisX,
                SkyServer.ActualAxisY, SkyServer.SouthernHemisphere);

            YAxis = axes[0];
            XAxis = axes[1];
        }

        private ICommand _openModelWindowCmd;
        public ICommand OpenModelWindowCmd
        {
            get
            {
                var cmd = _openModelWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openModelWindowCmd = new RelayCommand(param => OpenModelWindow());
            }
        }
        private void OpenModelWindow()
        {
            try
            {
                var win = Application.Current.Windows.OfType<ModelV>().FirstOrDefault();
                if (win != null) return;
                var bWin = new ModelV();
                var _modelVM = ModelVM._modelVM;
                _modelVM.WinHeight = 320;
                _modelVM.WinWidth = 250;
                _modelVM.Position = Position;
                _modelVM.LookDirection = LookDirection;
                _modelVM.UpDirection = UpDirection;
                _modelVM.ImageFile = ImageFile;
                _modelVM.CameraIndex = 2;
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

        private ICommand _openResetViewCmd;
        public ICommand OpenResetViewCmd
        {
            get
            {
                var cmd = _openResetViewCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openResetViewCmd = new RelayCommand(param => OpenResetView());
            }
        }
        private void OpenResetView()
        {
            try
            {
                Settings.Settings.ModelLookDirection2 = new Vector3D(-900, -1100, -400);
                Settings.Settings.ModelUpDirection2 = new Vector3D(.35, .43, .82);
                Settings.Settings.ModelPosition2 = new Point3D(900, 1100, 800);
                LoadGEM();
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
            _ctsPark?.Cancel();
            _ctsPark?.Dispose();
            //_ctsSpiral?.Cancel();
            //_ctsSpiral?.Dispose();
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~SkyTelescopeVM()
        {
            Settings.Settings.ModelLookDirection2 = LookDirection;
            Settings.Settings.ModelUpDirection2 = UpDirection;
            Settings.Settings.ModelPosition2 = Position;
            Settings.Settings.Save();

            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _util?.Dispose();
            }

            // free native resources if there are any.
            NativeMethods.ClipCursor(IntPtr.Zero);
        }
        #endregion
    }
}
