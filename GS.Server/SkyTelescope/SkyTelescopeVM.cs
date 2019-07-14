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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Server.Cdc;
using GS.Server.Domain;
using GS.Server.Gps;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Shared;
using MaterialDesignThemes.Wpf;

namespace GS.Server.SkyTelescope
{
    public sealed class SkyTelescopeVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        private readonly Util _util = new Util();
        public string TopName => "SkyWatcher";
        public string BottomName => "Telescope";
        public int Uid => 0;
        public static SkyTelescopeVM _skyTelescopeVM;
        
        #endregion

        #region View Model Items

        /// <summary>
        /// Constructor
        /// </summary>
        public SkyTelescopeVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    DebugVisability = false;
                    _skyTelescopeVM = this;

                    //Show in Tab?
                    if (!Properties.Server.Default.SkyWatcher) return;

                    var monitorItem = new MonitorEntry
                    {
                        Datetime = Principles.HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Loading SkyTelescopeVM"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    // defaults for the view
                    // FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
                    RightAscension = "00h 00m 00s";
                    Declination = "00° 00m 00s";
                    Azimuth = "00° 00m 00s";
                    Altitude = "00° 00m 00s";

                    // Deals with applications trying to open the setup dialog more than once. 
                    OpenSetupDialog = SkyServer.OpenSetupDialog;
                    SkyServer.OpenSetupDialog = true;

                    // setup property events to monitor
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    MonitorQueue.StaticPropertyChanged += PropertyChangedMonitorQueue;
                    SkySystem.StaticPropertyChanged += PropertyChangedSkySystem;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;
                    Shared.Settings.StaticPropertyChanged += PropertyChangedMonitorLog;
                    Synthesizer._staticPropertyChanged += PropertyChangedSynthesizer;

                    GuideRateOffsetXs = new List<double>(Numbers.InclusiveRange(10, 100,10));
                    GuideRateOffsetYs = new List<double>(Numbers.InclusiveRange(10, 100, 10));
                    MaxSlewRates = new List<double>(Numbers.InclusiveRange(2.0, 5));
                    HourAngleLimits = new List<double>(Numbers.InclusiveRange(0, 45, 1));
                    LatitudeRange = new List<int>(Enumerable.Range(-89, 179));
                    LongitudeRange = new List<int>(Enumerable.Range(-179, 359));
                    Hours = new List<int>(Enumerable.Range(0, 24));
                    Minutes = new List<int>(Enumerable.Range(0, 60));
                    Seconds = new List<int>(Enumerable.Range(0, 60));
                    St4Guiderates = new List<double> {1.0, 0.75, 0.50, 0.25, 0.125};
                    Temperatures = new List<double>(Numbers.InclusiveRange(-50, 60,1.0));

                    // initial view items
                    AtPark = SkyServer.AtPark;
                    ConnectButtonContent = "Connect";
                    VoiceState = Synthesizer.VoiceActive;

                }

                // check to make sure window is visable then connect if requested.
                MountState = SkyServer.IsMountRunning;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                SkyServer.IsMountRunning = false;
                OpenDialog(ex.Message);
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
                     case "Longitude":
                         UpdateLongitude();
                         break;
                     case "Latitude":
                         UpdateLatitude();
                         break;
                     case "Elevation":
                         Elevation = SkySettings.Elevation;
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                            case "DeclinationXform":
                                 Declination = _util.DegreesToDMS(SkyServer.DeclinationXform, "° ", ":", "", 2);
                                 break;
                            case "OpenSetupDialog":
                                OpenSetupDialog = SkyServer.OpenSetupDialog;
                                break;
                            case "RightAscensionXform":
                                RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXform, "h ", ":", "", 2);
                                break;
                            case "SiderealTime":
                                if (DebugVisability) SiderealTime = _util.HoursToHMS(SkyServer.SiderealTime);
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
                            case "AlertState":
                                AlertState = SkyServer.AlertState;
                                break;
                            case "PecTrainInProgress":
                                PecTrainInProgress = SkyServer.PecTrainInProgress;
                                break;
                            case "PecOn":
                                PpecOn = SkyServer.Pec;
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
                            case "MountAxisX":
                                if(DebugVisability) MountAxisX = $"{Numbers.TruncateD(SkyServer.MountAxisX, 15)}";
                                    break;
                            case "MountAxisY":
                                if (DebugVisability) MountAxisY =  $"{Numbers.TruncateD(SkyServer.MountAxisY, 15)}";
                                    break;
                            case "MountError":
                                MountError = SkyServer.MountError;
                                break;
                            case "ActualAxisX":
                                if (DebugVisability) ActualAxisX = $"{Numbers.TruncateD(SkyServer.ActualAxisX, 15)}";
                                break;
                            case "ActualAxisY":
                                if (DebugVisability) ActualAxisY = $"{Numbers.TruncateD(SkyServer.ActualAxisY, 15)}";
                                    break;
                            }
                        });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                OpenDialog(value.Message);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// CA1001: Types that own disposable fields should be disposable
        /// </summary>
        public void Dispose()
        {
            ((IDisposable) _util)?.Dispose();
        }


        #endregion

        #region Drawer Settings 

        public IList<int> ComPorts { get
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
            get => SkySystem.ComPort;
            set
            {
                if (value == SkySystem.ComPort) return;
                SkySystem.ComPort = value;
                OnPropertyChanged();
            }
        }

        public SerialSpeed BaudRate
        {
            get => SkySystem.BaudRate;
            set
            {
                if (value == SkySystem.BaudRate) return;
                SkySystem.BaudRate = value;
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

        public int DecBacklash
        {
            get => SkySettings.DecBacklash;
            set
            {
                SkySettings.DecBacklash = value;
                OnPropertyChanged();
            }
        }

        public IList<double> St4Guiderates { get; }
        public double St4Guiderate
        {
            get
            {
                double ret;
                switch (SkySettings.St4Guiderate)
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
                SkySettings.St4Guiderate = ret;
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
                OnPropertyChanged();
            }
        }

        public MountType Mount
        {
            get => SkySystem.Mount;
            set
            {
                if (value == SkySystem.Mount) return;
                SkySystem.Mount = value;
                OnPropertyChanged();
            }
        }

        public IList<double> GuideRateOffsetXs { get; }
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
        
        public IList<double> GuideRateOffsetYs { get; }
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

        public bool AlternatingPpec
        {
            get => SkySettings.AlternatingPpec;
            set
            {
                if (value == SkySettings.AlternatingPpec) return;
                SkySettings.AlternatingPpec = value;
                OnPropertyChanged();
            }
        }

        public bool DecPulseToGoTo
        {
            get => SkyServer.DecPulseToGoTo;
            set
            {
                if (value == SkyServer.DecPulseToGoTo) return;
                SkyServer.DecPulseToGoTo = value;
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

        public IList<int> LatitudeRange { get; }
        public int Lat1
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Latitude * 3600);
                var deg = sec / 3600;
                return deg;
            }
            set
            {
                var l = Principles.Units.Deg2Dou(value, Lat2, Lat3);
                if (Math.Abs(l - SkySettings.Latitude) < 0.00001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }

        public IList<int> Minutes { get; }
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
                var l = Principles.Units.Deg2Dou(Lat1, value, Lat3);
                if (Math.Abs(l - SkySettings.Latitude) < 0.00001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }

        public IList<int> Seconds { get; }
        public double Lat3
        {
            get
            {
                var sec = SkySettings.Latitude * 3600;
                sec = Math.Abs(sec % 3600);
                sec %= 60;
                return Math.Round(sec,3);
            }
            set
            {
                var l = Principles.Units.Deg2Dou(Lat1, Lat2, value);
                if (Math.Abs(l - SkySettings.Latitude) < 0.00001) return;
                SkySettings.Latitude = l;
                OnPropertyChanged();
            }
        }

        public IList<int> LongitudeRange { get; }
        public int Long1
        {
            get
            {
                var sec = (int)Math.Round(SkySettings.Longitude * 3600);
                var deg = sec / 3600;
                return deg;
            }
            set
            {
                var l = Principles.Units.Deg2Dou(value, Long2, Long3);
                if (Math.Abs(l - SkySettings.Longitude) < 0.00001) return;
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
                if (Math.Abs(l - SkySettings.Longitude) < 0.00001) return;
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
                return Math.Round(sec, 3);
            }
            set
            {
                var l = Principles.Units.Deg2Dou(Long1, Long2, value);
                if (Math.Abs(l - SkySettings.Longitude) < 0.00001) return;
                SkySettings.Longitude = l;
                OnPropertyChanged();
            }
        }

        private void UpdateLongitude()
        {
            OnPropertyChanged($"Long1");
            OnPropertyChanged($"Long2");
            OnPropertyChanged($"Long3");
        }

        private void UpdateLatitude()
        {
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
                return _clickSaveParkCommand ?? (_clickSaveParkCommand = new RelayCommand(
                           param => ClickSavePark()
                       ));
            }
        }
        private void ClickSavePark()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.SetParkAxis();
                }
}
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickSaveSettingcommand;
        public ICommand ClickSaveSettingsCommand
        {
            get
            {
                return _clickSaveSettingcommand ?? (_clickSaveSettingcommand = new RelayCommand(
                           param => ClickSaveSettings()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickCloseSettingcommand;
        public ICommand ClickCloseSettingsCommand
        {
            get
            {
                return _clickCloseSettingcommand ?? (_clickCloseSettingcommand = new RelayCommand(
                           param => ClickCloseSettings()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickResetSiderealRateCommand;
        public ICommand ClickResetSiderealRateCommand
        {
            get
            {
                return _clickResetSiderealRateCommand ?? (_clickResetSiderealRateCommand = new RelayCommand(
                           param => ClickResetSiderealRate()
                       ));
            }
        }
        private void ClickResetSiderealRate()
        {
            SiderealRate = 15.041;
        }

        private ICommand _clickResetSolarRateCommand;
        public ICommand ClickResetSolarRateCommand
        {
            get
            {
                return _clickResetSolarRateCommand ?? (_clickResetSolarRateCommand = new RelayCommand(
                           param => ClickResetSolarRate()
                       ));
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
                return _clickResetLunarRateCommand ?? (_clickResetLunarRateCommand = new RelayCommand(
                           param => ClickResetLunarRate()
                       ));
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
                return _clickResetKingRateCommand ?? (_clickResetKingRateCommand = new RelayCommand(
                           param => ClickResetKingRate()
                       ));
            }
        }
        private void ClickResetKingRate()
        {
            KingRate = 15.0369;
        }

        #endregion

        #region Debug

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

        private string _mountAxisX;
        public string MountAxisX
        {
            get => _mountAxisX;
            private set
            {
                if (_mountAxisX == value) return;
                _mountAxisX = value;
                OnPropertyChanged();
            }
        }

        private string _mountAxisY;
        public string MountAxisY
        {
            get => _mountAxisY;
            private set
            {
                if (_mountAxisY == value) return;
                _mountAxisY = value;
                OnPropertyChanged();
            }
        }

        private bool _debugVisability;
        public bool DebugVisability
        {
            get => _debugVisability;
            set
            {
                if (value == _debugVisability) return;
                _debugVisability = value;
                SkyServer.Debug = value;
                OnPropertyChanged();
            }
        }

        private ICommand _flipSopCommand;
        public ICommand ClickFlipSopCommand
        {
            get
            {
                return _flipSopCommand ?? (_flipSopCommand = new RelayCommand(param => FlipSop()));
            }
            set => _flipSopCommand = value;
        }
        private void FlipSop()
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _testCommand;
        public ICommand ClickTestCommand
        {
            get
            {
                return _testCommand ?? (_testCommand = new RelayCommand(param => Test()));
            }
            set => _testCommand = value;
        }
        private void Test()
        {
            try
            {
                Principles.Alignment.Test2Star();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region Top Bar Control

        public IList<int> Hours { get; }

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

        private ICommand _clickparkcommand;
        public ICommand ClickParkCommand
        {
            get
            {
                return _clickparkcommand ?? (_clickparkcommand = new RelayCommand(
                           param => ClickPark()
                       ));
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
                        SkyServer.GoToPark();
                    }
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = Principles.HiResDateTime.UtcNow,
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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

        private ICommand _clickhomecommand;
        public ICommand ClickHomeCommand
        {
            get
            {
                return _clickhomecommand ?? (_clickhomecommand = new RelayCommand(
                           param => ClickHome()
                       ));
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
                        Synthesizer.Speak("Parked");
                        return;
                    }
                    SkyServer.GoToHome();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickstopcommand;
        public ICommand ClickStopCommand
        {
            get
            {
                return _clickstopcommand ?? (_clickstopcommand = new RelayCommand(
                           param => ClickStop()
                       ));
            }
        }
        private void ClickStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (!SkyServer.IsMountRunning) return;
                    SkyServer.AbortSlew();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickTrackingcommand;
        public ICommand ClickTrackingCommand
        {
            get
            {
                return _clickTrackingcommand ?? (_clickTrackingcommand = new RelayCommand(
                           param => ClickTracking()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                return _openHomeResetDialogCommand ?? (_openHomeResetDialogCommand = new RelayCommand(
                           param => OpenHomeResetDialog()
                       ));
            }
        }
        private void OpenHomeResetDialog()
        {
            try
            {
                if (SkyServer.Tracking)
                {
                    OpenDialog("Tracking off or stop mount before setting home position");
                    return;
                }
                HomeResetContent = new HomeResetDialog();
                IsHomeResetDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }

        }

        private ICommand _acceptHomeResetDialogCommand;
        public ICommand AcceptHomeResetDialogCommand
        {
            get
            {
                return _acceptHomeResetDialogCommand ?? (_acceptHomeResetDialogCommand = new RelayCommand(
                           param => AcceptHomeResetDialog()
                       ));
            }
        }
        private void AcceptHomeResetDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.ResetHomePositions();
                    Synthesizer.Speak("Home Set");
                    IsHomeResetDialogOpen = false;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelHomeResetDialogCommand;
        public ICommand CancelHomeResetDialogCommand
        {
            get
            {
                return _cancelHomeResetDialogCommand ?? (_cancelHomeResetDialogCommand = new RelayCommand(
                           param => CancelHomeResetDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickMountInfoDialogCommand;
        public ICommand ClickMountInfoDialogCommand
        {
            get
            {
                return _clickMountInfoDialogCommand ?? (_clickMountInfoDialogCommand = new RelayCommand(
                           param => ClickMountInfoDialog()
                       ));
            }
        }
        private void ClickMountInfoDialog()
        {
            try
            {
                var msg = $"Mount: {SkyServer.MountName}" + Environment.NewLine;
                        msg += $"Version: {SkyServer.MountVersion}" + Environment.NewLine;
                        msg += $"StepsRa: {SkyServer.StepsPerRevolution[0]}" + Environment.NewLine;
                        msg += $"StepsDec: {SkyServer.StepsPerRevolution[1]}" + Environment.NewLine;
                OpenDialog(msg);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }
        
        #endregion

        #region RA Coord GoTo Control

        private double _rahours;
        public double RaHours
        {
            get => _rahours;
            set
            {
                if (Math.Abs(value - _rahours) < 0.00001) return;
                _rahours = value;
                OnPropertyChanged();
            }
        }

        private double _raminutes;
        public double RaMinutes
        {
            get => _raminutes;
            set
            {
                if (Math.Abs(value - _raminutes) < 0.00001) return;
                _raminutes = value;
                OnPropertyChanged();
            }
        }

        private double _raseconds;
        public double RaSeconds
        {
            get => _raseconds;
            set
            {
                if (Math.Abs(value - _raseconds) < 0.00001) return;
                _raseconds = value;
                OnPropertyChanged();
            }
        }

        private double _decdegrees;
        public double DecDegrees
        {
            get => _decdegrees;
            set
            {
                if (Math.Abs(value - _decdegrees) < 0.00001) return;
                _decdegrees = value;
                OnPropertyChanged();
            }
        }

        private double _decminutes;
        public double DecMinutes
        {
            get => _decminutes;
            set
            {
                if (Math.Abs(value - _decminutes) < 0.00001) return;
                _decminutes = value;
                OnPropertyChanged();
            }
        }

        private double _decseconds;
        public double DecSeconds
        {
            get => _decseconds;
            set
            {
                if (Math.Abs(value - _decseconds) < 0.00001) return;
                _decseconds = value;
                OnPropertyChanged();
            }
        }

        private ICommand _populateGoToRaDec;
        public ICommand PopulateGoToRaDecCommand
        {
            get
            {
                return _populateGoToRaDec ?? (_populateGoToRaDec = new RelayCommand(
                           param => PopulateGoToRaDec()
                       ));
            }
        }
        private void PopulateGoToRaDec()
        {
            try
            {
                using (new WaitCursor())
                {
                    var ra = _util.HoursToHMS(SkyServer.RightAscensionXform, ":", ":", ":", 3);
                    var ras = ra.Split(':');
                    RaHours = Convert.ToDouble(ras[0]);
                    RaMinutes = Convert.ToDouble(ras[1]);
                    RaSeconds = Convert.ToDouble(ras[2]);

                    var dec = _util.HoursToHMS(SkyServer.DeclinationXform, ":", ":", ":", 3);
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private bool _isRaGoToDialogOpen;
        public bool IsRaGoToDialogOpen
        {
            get => _isRaGoToDialogOpen;
            set
            {
                if (_isRaGoToDialogOpen == value) return;
                _isRaGoToDialogOpen = value;
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
                return _openRaGoToDialogCommand ?? (_openRaGoToDialogCommand = new RelayCommand(
                           param => OpenRaGoToDialog()
                       ));
            }
        }
        private void OpenRaGoToDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var AltAz = Principles.Coordinate.RaDec2AltAz(GoToRa, GoToDec, SkyServer.SiderealTime,
                        SkySettings.Latitude);
                    if (AltAz[0] < 0)
                    {
                        OpenDialog($"Target Below Horizon Az: {AltAz[1]} Alt: {AltAz[0]}");
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _acceptRaGoToDialogCommand;
        public ICommand AcceptRaGoToDialogCommand
        {
            get
            {
                return _acceptRaGoToDialogCommand ?? (_acceptRaGoToDialogCommand = new RelayCommand(
                           param => AcceptRaGoToDialog()
                       ));
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

                    var vec = Transforms.CoordTypeToInternal(GoToRa, GoToDec);
                    SkyServer.SlewRaDec(vec.X, vec.Y);
                    IsRaGoToDialogOpen = false;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        public double GoToDec => Principles.Units.Deg2Dou(DecDegrees, DecMinutes, DecSeconds);
        public double GoToRa => Principles.Units.Ra2Dou(RaHours, RaMinutes, RaSeconds);

        public string GoToDecString => _util.DegreesToDMS(GoToDec, "° ", "m ", "s", 3);
        public string GoToRaString => _util.HoursToHMS(GoToRa, "h ", "m ", "s", 3);

        private ICommand _cancelRaGoToDialogCommand;
        public ICommand CancelRaGoToDialogCommand
        {
            get
            {
                return _cancelRaGoToDialogCommand ?? (_cancelRaGoToDialogCommand = new RelayCommand(
                           param => CancelRaGoToDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region PPEC Control

        public bool PpecOn
        {
            get => SkyServer.Pec;
            set
            {
                if (PpecOn == value) return;
                SkyServer.Pec = value;
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

        private bool _isPpecDialogOpen;
        public bool IsPpecDialogOpen
        {
            get => _isPpecDialogOpen;
            set
            {
                if (_isPpecDialogOpen == value) return;
                _isPpecDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _ppecContent;
        public object PpecContent
        {
            get => _ppecContent;
            set
            {
                if (_ppecContent == value) return;
                _ppecContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openPpecDialogCommand;
        public ICommand OpenPpecDialogCommand
        {
            get
            {
                return _openPpecDialogCommand ?? (_openPpecDialogCommand = new RelayCommand(
                           param => OpenPPecDialog()
                       ));
            }
        }
        private void OpenPPecDialog()
        {
            try
            {
                if (SkyServer.Tracking || SkyServer.PecTrainInProgress)
                {
                    PpecContent = new PpecDialog();
                    IsPpecDialogOpen = true;
                }
                else
                {
                    PecTrainOn = false;
                    OpenDialog("Tracking must be on first");
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }

        }

        private ICommand _acceptPpecDialogCommand;
        public ICommand AcceptPpecDialogCommand
        {
            get
            {
                return _acceptPpecDialogCommand ?? (_acceptPpecDialogCommand = new RelayCommand(
                           param => AcceptPpecDialog()
                       ));
            }
        }
        private void AcceptPpecDialog()
        {
            try
            {
            using (new WaitCursor())
            {
                SkyServer.PecTraining = !SkyServer.PecTraining;
                IsPpecDialogOpen = false;
            }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelPpecDialogCommand;
        public ICommand CancelPpecDialogCommand
        {
            get
            {
                return _cancelPpecDialogCommand ?? (_cancelPpecDialogCommand = new RelayCommand(
                           param => CancelPpecDialog()
                       ));
            }
        }
        private void CancelPpecDialog()
        {
            try
            {
                PecTrainOn = !PecTrainOn;
                IsPpecDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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

        private ICommand _hcSpeedupCommand;
        public ICommand HcSpeedupCommand
        {
            get
            {
                return _hcSpeedupCommand ?? (_hcSpeedupCommand = new RelayCommand(
                           param => SpeedupCommand()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcSpeeddownCommand;
        public ICommand HcSpeeddownCommand
        {
            get
            {
                return _hcSpeeddownCommand ?? (_hcSpeeddownCommand = new RelayCommand(
                           param => SpeeddownCommand()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseDownLeftCommand;
        public ICommand HcMouseDownLeftCommand
        {
            get
            {
                return _hcMouseDownLeftCommand ?? (_hcMouseDownLeftCommand = new RelayCommand(param => HcMouseDownLeft()));
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
                    Synthesizer.Speak("Parked");
                    return;
                }
                StartSlew(FlipEW ? SlewDirection.SlewRight : SlewDirection.SlewLeft);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseUpLeftCommand;
        public ICommand HcMouseUpLeftCommand
        {
            get
            {
                return _hcMouseUpLeftCommand ?? (_hcMouseUpLeftCommand = new RelayCommand(param => HcMouseUpLeft()));
            }
            set => _hcMouseUpLeftCommand = value;
        }
        private void HcMouseUpLeft()
        {
            try
            {
                StartSlew(SlewDirection.SlewNone);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseDownRightCommand;
        public ICommand HcMouseDownRightCommand
        {
            get
            {
                return _hcMouseDownRightCommand ?? (_hcMouseDownRightCommand = new RelayCommand(param => HcMouseDownRight()));
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
                    Synthesizer.Speak("Parked");
                    return;
                }
                StartSlew(FlipEW ? SlewDirection.SlewLeft : SlewDirection.SlewRight);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseUpRightCommand;
        public ICommand HcMouseUpRightCommand
        {
            get
            {
                return _hcMouseUpRightCommand ?? (_hcMouseUpRightCommand = new RelayCommand(param => HcMouseUpRight()));
            }
            set => _hcMouseUpRightCommand = value;
        }
        private void HcMouseUpRight()
        {
            try
            {
                StartSlew(SlewDirection.SlewNone);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseDownUpCommand;
        public ICommand HcMouseDownUpCommand
        {
            get
            {
                return _hcMouseDownUpCommand ?? (_hcMouseDownUpCommand = new RelayCommand(param => HcMouseDownUp()));
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
                    Synthesizer.Speak("Parked");
                    return;
                }
                StartSlew(FlipNS ? SlewDirection.SlewDown : SlewDirection.SlewUp);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseUpUpCommand;
        public ICommand HcMouseUpUpCommand
        {
            get
            {
                return _hcMouseUpUpCommand ?? (_hcMouseUpUpCommand = new RelayCommand(param => HcMouseUpUp()));
            }
            set => _hcMouseUpUpCommand = value;
        }
        private void HcMouseUpUp()
        {
            try
            {
                StartSlew(SlewDirection.SlewNone);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }
        
        private ICommand _hcMouseDownDownCommand;
        public ICommand HcMouseDownDownCommand
        {
            get
            {
                return _hcMouseDownDownCommand ?? (_hcMouseDownDownCommand = new RelayCommand(param => HcMouseDownDown()));
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
                    Synthesizer.Speak("Parked");
                    return;
                }
                StartSlew(FlipNS ? SlewDirection.SlewUp : SlewDirection.SlewDown);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseUpDownCommand;
        public ICommand HcMouseUpDownCommand
        {
            get
            {
                return _hcMouseUpDownCommand ?? (_hcMouseUpDownCommand = new RelayCommand(param => HcMouseUpDown()));
            }
            set => _hcMouseUpDownCommand = value;
        }
        private void HcMouseUpDown()
        {
            try
            {
                StartSlew(SlewDirection.SlewNone);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _hcMouseDownStopCommand;
        public ICommand HcMouseDownStopCommand
        {
            get
            {
                return _hcMouseDownStopCommand ?? (_hcMouseDownStopCommand = new RelayCommand(param => HcMouseDownStop()));
            }
            set => _hcMouseDownStopCommand = value;
        }
        private void HcMouseDownStop()
        {
            try
            {
                SkyServer.AbortSlew();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private static void StartSlew(SlewDirection direction)
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
                    SkyServer.HcMoves(speed, SlewDirection.SlewEast);
                    break;
                case SlewDirection.SlewWest:
                case SlewDirection.SlewLeft:
                    SkyServer.HcMoves(speed, SlewDirection.SlewWest);
                    break;
                case SlewDirection.SlewNorth:
                case SlewDirection.SlewUp:
                    SkyServer.HcMoves(speed, SlewDirection.SlewNorth);
                    break;
                case SlewDirection.SlewSouth:
                case SlewDirection.SlewDown:
                    SkyServer.HcMoves(speed, SlewDirection.SlewSouth);
                    break;
                case SlewDirection.SlewNone:
                    SkyServer.HcMoves(speed, SlewDirection.SlewNone);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Bottom Bar Control

        public bool _ishome;
        public bool IsHome
        {
            get => _ishome;
            set
            {
                if (IsHome == value) return;
                _ishome = value;
                HomeBadgeContent = value ? "home" : "";
                OnPropertyChanged();
            }
        }

        public bool _atpark;
        public bool AtPark
        {
            get => _atpark;
            set
            {
                _atpark = value;
                ParkButtonContent = value ? "UnPark" : "Park";
                ParkBadgeContent = value ? "parked" : "";
                OnPropertyChanged();
            }
        }

        private string _parkbuttoncontent;
        public string ParkButtonContent
        {
            get => _parkbuttoncontent;
            set
            {
                if (ParkButtonContent == value) return;
                _parkbuttoncontent = value;
                OnPropertyChanged();
            }
        }

        private bool _isslewing;
        public bool IsSlewing
        {
            get => _isslewing;
            set
            {
                if (IsSlewing == value) return;
                _isslewing = value;
                OnPropertyChanged();
            }
        }

        private bool _istracking;
        public bool IsTracking
        {
            get => _istracking;
            set
            {
                if (IsTracking == value) return;
                _istracking = value;
                TrackingBadgeContent = value ? "on" : "";
                OnPropertyChanged();
            }
        }

        private bool _trackingon;
        public bool TrackingOn
        {
            get => _trackingon;
            set
            {
                if (TrackingOn == value) return;
                _trackingon = value;
                SkyServer.Tracking = value;
                OnPropertyChanged();
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

        private bool _limitalarm;
        public bool LimitAlarm
        {
            get => _limitalarm;
            set
            {
                if (LimitAlarm == value) return;
                _limitalarm = value;
                OnPropertyChanged();
            }
        }

        private bool _warningstate;
        public bool WarningState
        {
            get => _warningstate;
            set
            {
                if (_warningstate == value) return;
                _warningstate = value;
                OnPropertyChanged();
            }
        }

        private bool _alertstate;
        public bool AlertState
        {
            get => _alertstate;
            set
            {
                if (AlertState == value) return;
                _alertstate = value;
                OnPropertyChanged();
            }
        }

        private bool _voicestate;
        public bool VoiceState
        {
            get => _voicestate;
            set
            {
                if (value == VoiceState) return;
                _voicestate = value;
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
                ConnectButtonContent = value ? "Disconnect" : "Connect";
            }
        }

        private string _connectbuttoncontent;
        public string ConnectButtonContent
        {
            get => _connectbuttoncontent;
            set
            {
                if (ConnectButtonContent == value) return;
                _connectbuttoncontent = value;
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

        private ICommand _clearWarningCommand;
        public ICommand ClearWarningCommand
        {
            get
            {
                return _clearWarningCommand ?? (_clearWarningCommand = new RelayCommand(
                           param => ClearWarningState()
                       ));
            }
        }
        private void ClearWarningState()
        {
            MonitorQueue.WarningState = false;
        }

        private ICommand _clearLimitCommand;
        public ICommand ClearLimitCommand
        {
            get
            {
                return _clearLimitCommand ?? (_clearLimitCommand = new RelayCommand(
                           param => ClearLimitAlarm()
                       ));
            }
        }
        private void ClearLimitAlarm()
        {
            SkyServer.AscomOn = true;
            SkyServer.LimitAlarm = false;
        }

        private ICommand _clearErrorsCommand;
        public ICommand ClearErrorsCommand
        {
            get
            {
                return _clearErrorsCommand ?? (_clearErrorsCommand = new RelayCommand(
                           param => ClearErrorAlert()
                       ));
            }
        }
        private void ClearErrorAlert()
        {
            SkyServer.AlertState = false;
        }

        private ICommand _clickconnectcommand;
        public ICommand ClickConnectCommand
        {
            get
            {
                return _clickconnectcommand ?? (_clickconnectcommand = new RelayCommand(
                           param => ClickConnect()
                       ));
            }
        }
        private void ClickConnect()
        {
            try
            {
                using (new WaitCursor())
                {
                   SkyServer.IsMountRunning = !SkyServer.IsMountRunning;
                }

                if (SkyServer.IsMountRunning)
                {
                    WarningState = false;
                    AlertState = false;
                    HomePositionCheck();
                }
            }
            catch (Exception ex)
            {
                SkyServer.SkyErrorHandler(ex);
            }
        }

        private void HomePositionCheck()
        {
            if (SkyServer.AtPark ) return;
            if (!SkySettings.HomeWarning) return;

            switch (SkySystem.Mount)
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
                            var msg = @"When starting normally place the mount in the 'Home' position.";
                            msg += Environment.NewLine + @"Home Position is counterweight down and declination pointing towards the pole.";
                            msg += Environment.NewLine + @"Click the 'SetHome' button before or after obtaining a polar alignment.";

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

        #endregion

        #region Dialog Message

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
        private void OpenDialog(string msg)
        {
            if (msg != null) DialogMsg = msg;
             DialogContent = new Dialog();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = Principles.HiResDateTime.UtcNow,
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

        #region GPS Dialog

        private string _nmeaSentence;
        public string NmeaSentence
        {
            get => _nmeaSentence;
            set
            {
                if (_nmeaSentence == value) return;
                _nmeaSentence = value;
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

        private int _gpsComPort;
        public int GpsComPort
        {
            get => Properties.SkyTelescope.Default.GpsPort;
            set
            {
                if (value == _gpsComPort) return;
                _gpsComPort = value;
                Properties.SkyTelescope.Default.GpsPort = value;
                OnPropertyChanged();
            }
        }

        private ICommand _populateGps;
        public ICommand PopulateGpsCommand
        {
            get
            {
                return _populateGps ?? (_populateGps = new RelayCommand(
                           param => PopulateGps()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
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
                return _openGpsDialogCommand ?? (_openGpsDialogCommand = new RelayCommand(
                           param => OpenGpsDialog()
                       ));
            }
        }
        private void OpenGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    GpsContent = new GpsDialog();
                    IsGpsDialogOpen = true;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _acceptGpsDialogCommand;
        public ICommand AcceptGpsDialogCommand
        {
            get
            {
                return _acceptGpsDialogCommand ?? (_acceptGpsDialogCommand = new RelayCommand(
                           param => AcceptGpsDialog()
                       ));
            }
        }
        private void AcceptGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkySettings.Latitude = GpsLat;
                    SkySettings.Longitude = GpsLong;
                    SkySettings.Elevation = GpsElevation;
                    IsGpsDialogOpen = false;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _retrieveGpsDialogCommand;
        public ICommand RetrieveGpsDialogCommand
        {
            get
            {
                return _retrieveGpsDialogCommand ?? (_retrieveGpsDialogCommand = new RelayCommand(
                           param => RetrieveGpsDialog()
                       ));
            }
        }
        private void RetrieveGpsDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var gpsHardware = new GpsHardware(GpsComPort);
                        gpsHardware.GpsOn();
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    while (stopwatch.Elapsed.Seconds < 10)
                    {
                        if (gpsHardware.HasData) break;
                    }
                    stopwatch.Reset();
                    gpsHardware.GpsOff();

                    if (gpsHardware.HasData)
                    {
                        GpsLong = gpsHardware.Longitude;
                        GpsLongString = _util.DegreesToDMS(GpsLong, "° ", ":", "", 2);
                        GpsLat = gpsHardware.Latitude;
                        GpsLatString = _util.DegreesToDMS(GpsLat, "° ", ":", "", 2);
                        GpsElevation = gpsHardware.Altitude;
                        NmeaSentence = gpsHardware.NmeaSentence;
                    }
                    else
                    {
                        OpenDialog($"No data found on COM port {GpsComPort}, please try again");
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelGpsDialogCommand;
        public ICommand CancelGpsDialogCommand
        {
            get
            {
                return _cancelGpsDialogCommand ?? (_cancelGpsDialogCommand = new RelayCommand(
                           param => CancelGpsDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
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
                return _populateCdc ?? (_populateCdc = new RelayCommand(
                           param => PopulateCdc()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
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
                return _openCdcDialogCommand ?? (_openCdcDialogCommand = new RelayCommand(
                           param => OpenCdcDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _acceptCdcDialogCommand;
        public ICommand AcceptCdcDialogCommand
        {
            get
            {
                return _acceptCdcDialogCommand ?? (_acceptCdcDialogCommand = new RelayCommand(
                           param => AcceptCdcDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _retrieveCdcDialogCommand;
        public ICommand RetrieveCdcDialogCommand
        {
            get
            {
                return _retrieveCdcDialogCommand ?? (_retrieveCdcDialogCommand = new RelayCommand(
                           param => RetrieveCdcDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _sendObsCdcDialogCommand;
        public ICommand SendObsCdcDialogCommand
        {
            get
            {
                return _sendObsCdcDialogCommand ?? (_sendObsCdcDialogCommand = new RelayCommand(
                           param => SendObsCdcDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelCdcDialogCommand;
        public ICommand CancelCdcDialogCommand
        {
            get
            {
                return _cancelCdcDialogCommand ?? (_cancelCdcDialogCommand = new RelayCommand(
                           param => CancelCdcDialog()
                       ));
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
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #endregion
    }
}
