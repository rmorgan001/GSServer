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
// ReSharper disable RedundantAssignment
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.AutoHome;
using GS.Server.Helpers;
using GS.Shared;
using GS.Simulator;
using GS.SkyWatcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GS.Server.Pec;
using GS.Server.Windows;
using GS.Server.Alignment;
using AxisStatus = GS.Simulator.AxisStatus;
using Range = GS.Principles.Range;

namespace GS.Server.SkyTelescope
{
    public static class SkyServer
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Fields

        const double _siderealRate = 15.0410671786691;

        private static readonly Util _util = new Util();
        private static readonly object _timerLock = new object();
        private static MediaTimer _mediaTimer;

        // Slew and HC speeds
        private static double SlewSpeedOne;
        private static double SlewSpeedTwo;
        private static double SlewSpeedThree;
        private static double SlewSpeedFour;
        private static double SlewSpeedFive;
        private static double SlewSpeedSix;
        private static double SlewSpeedSeven;
        public static double SlewSpeedEight;

        // HC Anti-Backlash
        private static HcPrevMove HcPrevMoveRa;
        private static HcPrevMove HcPrevMoveDec;
        private static readonly IList<double> HcPrevMovesDec = new List<double>();

        private static Vector _homeAxes;
        private static Vector _mountAxes;
        private static Vector _targetAxes;
        private static Vector _altAzSync;

        public static readonly List<SpiralPoint> SpiralCollection;

        // AlignmentModel
        public static readonly AlignmentModel AlignmentModel;

        #endregion Fields 

        static SkyServer()
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Loading SkyServer"
                };
                MonitorLog.LogToMonitor(monitorItem);

                // load default or user property settings
                SkySettings.Load();

                // load some things
                Defaults();

                SpiralCollection = new List<SpiralPoint>();

                // set local to NaN for constructor
                _mountAxes = new Vector(double.NaN, double.NaN);

                // initialise the alignment model
                AlignmentSettings.Load();
                AlignmentModel = new AlignmentModel(
                    SkySettings.Latitude,
                    SkySettings.Longitude,
                    SkySettings.Elevation)
                {
                    IsAlignmentOn = (AlignmentShow && AlignmentSettings.IsAlignmentOn),
                    ThreePointAlgorithm = ThreePointAlgorithmEnum.BestCentre
                };
                AlignmentModel.Notification += AlignmentModel_Notification;

                // attach handler to watch for SkySettings changing.
                SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                // attach handler to watch for AlignmentSettings changing;
                AlignmentSettings.StaticPropertyChanged += PropertyChangedAlignmentSettings;

                // attach handler to watch for pulses changing;
                SkyQueue.StaticPropertyChanged += PropertyChangedSkyQueue;

                // attach handler to watch for pulses changing;
                MountQueue.StaticPropertyChanged += PropertyChangedMountQueue;

            }
            catch (Exception ex)
            {
                // oops now what happened?
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                AlertState = true;
                throw;
            }
        }

        #region Property Settings 

        #region Backers
        private static double _actualAxisX;
        private static double _actualAxisY;
        private static bool _alertState;
        private static Vector _altAzm;
        private static int _autoHomeProgressBar;
        private static bool _autoHomeStop;
        private static bool _asComOn;
        private static bool _canHomeSensor;
        private static string _capabilities;
        private static double _declinationXForm;
        private static Vector _guideRate;
        private static bool _isAutoHomeRunning;
        private static bool _isHome;
        private static PierSide _isSideOfPier;
        private static bool _isSlewing;
        private static Exception _lastAutoHomeError;
        private static double _lha;
        private static bool _limitAlarm;
        private static bool _mountRunning;
        private static bool _monitorPulse;
        private static double _mountAxisX;
        private static double _mountAxisY;
        private static Exception _mountError;
        private static bool _openSetupDialog;
        private static ParkPosition _parkSelected;
        private static bool _canPPec;
        private static bool _canPolarLed;
        private static bool _canAdvancedCmdSupport;
        private static Vector _raDec;
        private static Vector _rateAxes;
        private static Vector _rateRaDec;
        private static double _rightAscensionXForm;
        private static double _slewSettleTime;
        private static double _siderealTime;
        private static bool _spiralChanged;
        private static Vector _targetRaDec;
        private static TrackingMode _trackingMode;
        private static bool _tracking; //off
        private static bool _snapPort1Result;
        private static bool _snapPort2Result;
        #endregion

        /// <summary>
        /// UI display for actual positions in degrees
        /// </summary>
        public static double ActualAxisX
        {
            get => _actualAxisX;
            private set
            {
                if (Math.Abs(value - _actualAxisX) < 0.000000000000001) { return; }
                _actualAxisX = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// UI display for actual positions in degrees
        /// </summary>
        public static double ActualAxisY
        {
            get => _actualAxisY;
            private set
            {
                if (Math.Abs(value - _actualAxisY) < 0.000000000000001) { return; }
                _actualAxisY = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Alerts to UI
        /// </summary>
        public static bool AlertState
        {
            get => _alertState;
            set
            {
                if (value == AlertState) { return; }
                if (value) { Synthesizer.Speak(Application.Current.Resources["vceError"].ToString()); }
                _alertState = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Positions converted from mount
        /// </summary>
        public static double Altitude
        {
            get => _altAzm.Y;
            private set
            {
                if (Math.Abs(value - _altAzm.Y) < 0.000000000000001) { return; }
                _altAzm.Y = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Positions converted from mount
        /// </summary>
        public static double Azimuth
        {
            get => _altAzm.X;
            private set
            {
                if (Math.Abs(value - _altAzm.X) < 0.000000000000001) { return; }
                _altAzm.X = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// within degree range to trigger home
        /// </summary>
        public static bool AtHome
        {
            get
            {
                var h = new Vector(_homeAxes.X, _homeAxes.Y);
                var m = new Vector(Math.Abs(_mountAxes.X), _mountAxes.Y); // Abs is for S Hemisphere, hack for home position
                var x = (m - h);
                var r = x.LengthSquared < 0.01;
                return r;
            }
        }

        /// <summary>
        /// Is at park position
        /// </summary>
        public static bool AtPark
        {
            get => SkySettings.AtPark;
            set
            {
                SkySettings.AtPark = value;
                OnStaticPropertyChanged();
                Synthesizer.Speak(value ? Application.Current.Resources["vceParked"].ToString() : Application.Current.Resources["vceUnParked"].ToString());

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// UI progress bar for autoHome 
        /// </summary>
        public static int AutoHomeProgressBar
        {
            get => _autoHomeProgressBar;
            set
            {
                _autoHomeProgressBar = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Cancel button status for auto home
        /// </summary>
        public static bool AutoHomeStop
        {
            get => _autoHomeStop;
            set
            {
                _autoHomeStop = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Allow asCom driver to process calls
        /// </summary>
        public static bool AsComOn
        {
            get => _asComOn;
            set
            {
                _asComOn = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Support for a home sensor
        /// </summary>
        public static bool CanHomeSensor
        {
            get => _canHomeSensor;
            private set
            {
                if (_canHomeSensor == value) { return; }
                _canHomeSensor = value;
                OnStaticPropertyChanged();
            }
        }

        public static string Capabilities
        {
            get => _capabilities;
            private set
            {
                if (_capabilities == value) { return; }
                _capabilities = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Positions converted from mount
        /// </summary>
        public static double Declination
        {
            get => _raDec.Y;
            private set
            {
                if (Math.Abs(value - _raDec.Y) < 0.000000000000001) { return; }
                _raDec.Y = value;
                DeclinationXForm = value;
            }
        }

        /// <summary>
        /// UI display for converted dec
        /// </summary>
        public static double DeclinationXForm
        {
            get => _declinationXForm;
            private set
            {
                var dec = Transforms.DecToCoordType(RightAscension, value);
                _declinationXForm = dec;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// The right ascension tracking rate tracking rate (arc seconds per second, default = 0.0) 
        /// </summary>
        public static double RateRa
        {
            get => _rateRaDec.X;
            set
            {
                _rateRaDec.X = value;
                object _;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdRate(0, Axis.Axis1, _rateRaDec.X);
                        break;
                    case MountType.SkyWatcher:
                        var rate = GetSlewRate();
                        _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateRaDec.X}|{SkyTrackingOffset[0]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Factor to covert steps, Sky Watcher in rad
        /// </summary>
        private static double[] FactorStep { get; set; }

        /// <summary>
        /// The current Declination movement rate offset for telescope guiding (degrees/sec) 
        /// </summary>
        public static double GuideRateDec
        {
            get => _guideRate.Y;
            set => _guideRate.Y = value;
        }

        /// <summary>
        /// The current Right Ascension movement rate offset for telescope guiding (degrees/sec) 
        /// </summary>
        public static double GuideRateRa
        {
            get => _guideRate.X;
            set => _guideRate.X = value;
        }

        /// <summary>
        /// Checks if the auto home async process is running
        /// </summary>
        public static bool IsAutoHomeRunning
        {
            get => _isAutoHomeRunning;
            private set
            {
                _isAutoHomeRunning = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// UI indicator for at home
        /// </summary>
        public static bool IsHome
        {
            get => _isHome;
            private set
            {
                if (value == _isHome) { return; }
                _isHome = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// UI indicator
        /// </summary>
        public static PierSide IsSideOfPier
        {
            get => _isSideOfPier;
            private set
            {
                if (value == IsSideOfPier) return;
                Synthesizer.Speak(value.ToString());
                _isSideOfPier = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}|{_mountAxes.Y}|{_mountAxes.Y < 90 || _mountAxes.Y.IsEqualTo(90, 0.0000000001)}|{_mountAxes.Y > -90 || _mountAxes.Y.IsEqualTo(-90, 0.0000000001)} "
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// status for goto
        /// </summary>
        public static bool IsSlewing
        {
            get => _isSlewing;
            private set
            {
                if (_isSlewing == value) { return; }
                _isSlewing = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Starts/Stops current selected mount
        /// </summary>
        public static bool IsMountRunning
        {
            get
            {
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _mountRunning = MountQueue.IsRunning;
                        break;
                    case MountType.SkyWatcher:
                        _mountRunning = SkyQueue.IsRunning;
                        break;
                }

                return _mountRunning;
            }
            set
            {
                _mountRunning = value;
                if (value)
                {
                    MountStart();
                }
                else
                {
                    MountStop();
                }

                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Pulse reporting to driver
        /// </summary>
        public static bool IsPulseGuiding => IsPulseGuidingDec || IsPulseGuidingRa;

        /// <summary>
        /// Checks if the auto home async process is running
        /// </summary>
        public static Exception LastAutoHomeError
        {
            get => _lastAutoHomeError;
            private set
            {
                _lastAutoHomeError = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// applies backlash to pulse
        /// </summary>
        private static GuideDirections LastDecDirection { get; set; }

        /// <summary>
        /// Local Hour Angle
        /// </summary>
        public static double Lha
        {
            get => _lha;
            private set
            {
                if (Math.Abs(value - _lha) < 0.000000000000001) { return; }
                _lha = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// UI indicator for axes limits
        /// </summary>
        public static bool LimitAlarm
        {
            get => _limitAlarm;
            private set
            {
                if (LimitAlarm == value) return;
                _limitAlarm = value;
                if (value) Synthesizer.Speak(Application.Current.Resources["vceLimit"].ToString());
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Warning,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}|{ActualAxisX}|{ActualAxisY}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// use monitoring for charts
        /// </summary>
        public static bool MonitorPulse
        {
            private get => _monitorPulse;
            set
            {
                if (_monitorPulse == value) return;
                _monitorPulse = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SimTasks(MountTaskName.MonitorPulse);
                SkyTasks(MountTaskName.MonitorPulse);
            }
        }

        /// <summary>
        /// UI diagnostics option
        /// </summary>
        public static double MountAxisX
        {
            get => _mountAxisX;
            private set
            {
                if (Math.Abs(value - _mountAxisX) < 0.000000000000001) return;
                _mountAxisX = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// UI diagnostics option
        /// </summary>
        public static double MountAxisY
        {
            get => _mountAxisY;
            private set
            {
                if (Math.Abs(value - _mountAxisY) < 0.000000000000001) return;
                _mountAxisY = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Mount name
        /// </summary>
        public static string MountName { get; private set; }

        /// <summary>
        /// Controller board version
        /// </summary>
        public static string MountVersion { get; private set; }

        /// <summary>
        /// Used to inform and show error on the UI thread
        /// </summary>
        public static Exception MountError
        {
            get => _mountError;
            private set
            {
                _mountError = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Persistence of the rtf document while switching tabs
        /// </summary>
        public static string Notes { get; set; }

        /// <summary>
        /// Opens and tracks settings screen
        /// </summary>
        public static bool OpenSetupDialog
        {
            get => _openSetupDialog;
            set
            {
                _openSetupDialog = value;
                OpenSetupDialogFinished = !value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Ensures settings only open once
        /// </summary>
        public static bool OpenSetupDialogFinished { get; set; }

        /// <summary>
        /// Park position selected set from UI or asCom
        /// </summary>
        public static ParkPosition ParkSelected
        {
            get => _parkSelected;
            set
            {
                if (_parkSelected != null)
                {
                    if (_parkSelected.Name == value.Name && Math.Abs(_parkSelected.X - value.X) < 0 &&
                        Math.Abs(_parkSelected.Y - value.Y) < 0) { return; }
                }
                _parkSelected = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Is Dec pulse guiding
        /// </summary>
        public static bool IsPulseGuidingDec { get; set; }

        /// <summary>
        /// Is Ra pulse guiding
        /// </summary>
        public static bool IsPulseGuidingRa { get; set; }

        /// <summary>
        /// Positions converted from mount
        /// </summary>
        public static double RightAscension
        {
            get => _raDec.X;
            private set
            {
                if (Math.Abs(value - _raDec.X) < 0.000000000000001) return;
                _raDec.X = value;
                RightAscensionXForm = value;
            }
        }

        /// <summary>
        /// Move the telescope in one axis at the given rate
        /// </summary>
        public static double RateAxisDec
        {
            private get => _rateAxes.Y;
            set
            {
                _rateAxes.Y = value;
                if (Math.Abs(value) > 0)
                {
                    IsSlewing = true;
                    SlewState = SlewType.SlewMoveAxis;
                }
                else
                {
                    IsSlewing = false;
                    SlewState = SlewType.SlewNone;
                }

                object _;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdRateAxis(0, Axis.Axis2, _rateAxes.Y);
                        break;
                    case MountType.SkyWatcher:
                        var rate = GetSlewRate();
                        _ = new SkyAxisSlew(0, AxisId.Axis2, rate.Y);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateAxes.Y}|{SkyTrackingOffset[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Move the telescope in one axis at the given rate
        /// </summary>
        public static double RateAxisRa
        {
            private get => _rateAxes.X;
            set
            {
                _rateAxes.X = value;
                if (Math.Abs(value) > 0)
                {
                    IsSlewing = true;
                    SlewState = SlewType.SlewMoveAxis;
                }
                else
                {
                    IsSlewing = false;
                    SlewState = SlewType.SlewNone;
                    //if (Tracking) //off
                    //{
                    //    SetTracking();
                    //}
                }

                object _;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdRateAxis(0, Axis.Axis1, _rateAxes.X);
                        break;
                    case MountType.SkyWatcher:
                        var rate = GetSlewRate();
                        _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateAxes.X}|{SkyTrackingOffset[0]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// The declination tracking rate (arc seconds per second, default = 0.0) 
        /// </summary>
        public static double RateDec
        {
            get => _rateRaDec.Y;
            set
            {
                _rateRaDec.Y = value;
                object _;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdRate(0, Axis.Axis2, _rateRaDec.Y);
                        break;
                    case MountType.SkyWatcher:
                        var rate = GetSlewRate();
                        _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateRaDec.Y}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// UI display for converted ra
        /// </summary>
        public static double RightAscensionXForm
        {
            get => _rightAscensionXForm;
            private set
            {
                var ra = Transforms.RaToCoordType(value, Declination);
                _rightAscensionXForm = ra;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// time in seconds for mount to settle after slew
        /// </summary>
        public static double SlewSettleTime
        {
            get => _slewSettleTime;
            set
            {
                if (Math.Abs(_slewSettleTime - value) <= 0) return;
                _slewSettleTime = value;
            }
        }

        /// <summary>
        /// Moves mount to other side
        /// </summary>
        public static PierSide SideOfPier
        {
            get
            {
                if (SouthernHemisphere)
                {
                    //return _mountAxes.Y <= 90 && _mountAxes.Y >= -90 ? PierSide.pierWest : PierSide.pierEast;
                    // replaced with ...
                    if (_mountAxes.Y < 90.0000000001 && _mountAxes.Y > -90.0000000001)
                    {
                        return PierSide.pierWest;
                    }
                    return PierSide.pierEast;
                }

                // return _mountAxes.Y <= 90 && _mountAxes.Y >= -90 ? PierSide.pierEast : PierSide.pierWest;
                // replaced with ...
                if (_mountAxes.Y < 90.0000000001 && _mountAxes.Y > -90.0000000001)
                {
                    return PierSide.pierEast;
                }
                return PierSide.pierWest;
            }
            set
            {
                var a = Axes.GetAltAxisPosition(new[] { ActualAxisX, ActualAxisY });
                var b = Axes.AxesAppToMount(a);

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}|{SkySettings.HourAngleLimit}|{b[0]}|{b[1]}"
                };

                if (b[0] >= SkySettings.HourAngleLimit + 180 || b[0] <= -SkySettings.HourAngleLimit)
                {
                    monitorItem.Type = MonitorType.Warning;
                    MonitorLog.LogToMonitor(monitorItem);
                    throw new InvalidOperationException(
                        $"SideOfPier ({value}) is outside the range of set Meridian Limits: {SkySettings.HourAngleLimit}");
                }
                MonitorLog.LogToMonitor(monitorItem);
                SlewAxes(b[0], b[1], SlewType.SlewRaDec);
            }
        }

        /// <summary>
        /// Local time
        /// </summary>
        public static double SiderealTime
        {
            get => _siderealTime;
            private set
            {
                _siderealTime = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Total steps per 360
        /// </summary>
        public static long[] StepsPerRevolution { get; private set; }

        /// <summary>
        /// :b Timer Freq
        /// </summary>
        public static long[] StepsTimeFreq { get; private set; }

        private static double[] Steps { get; set; }

        /// <summary>
        /// Total worm teeth
        /// </summary>
        public static int[] WormTeethCount { get; private set; }

        /// <summary>
        /// Total worm step per 360
        /// </summary>
        public static double[] StepsWormPerRevolution { get; private set; }

        /// <summary>
        /// Set for all types of go tos
        /// </summary>
        public static SlewType SlewState { get; private set; }

        /// <summary>
        /// Camera Port
        /// </summary>
        public static bool SnapPort1 { get; set; }

        public static bool SnapPort2 { get; set; }

        public static bool SnapPort1Result
        {
            get => _snapPort1Result;
            set
            {
                if (_snapPort1Result == value) { return; }
                _snapPort1Result = value;
                OnStaticPropertyChanged();
            }
        }

        public static bool SnapPort2Result
        {
            get => _snapPort2Result;
            set
            {
                if (_snapPort2Result == value) { return; }
                _snapPort2Result = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Southern alignment status
        /// </summary>
        public static bool SouthernHemisphere => SkySettings.Latitude < 0;

        /// <summary>
        /// Used to notify spiral search list was modified
        /// </summary>
        public static bool SpiralChanged
        {
            get => _spiralChanged;
            set
            {
                _spiralChanged = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Dec target for slewing
        /// </summary>
        public static double TargetDec
        {
            get => _targetRaDec.Y;
            set => _targetRaDec.Y = value;
        }

        /// <summary>
        /// Ra target for slewing
        /// </summary>
        public static double TargetRa
        {
            get => _targetRaDec.X;
            set => _targetRaDec.X = value;
        }

        /// <summary>
        /// Counts any overlapping events with updating UI that might occur
        /// should always be 0 or event interval is too fast
        /// </summary>
        private static int TimerOverruns { get; set; }

        /// <summary>
        /// Turn off voice for tracking
        /// </summary>
        public static bool TrackingSpeak { private get; set; }

        /// <summary>
        /// Tracking status
        /// </summary>
        public static bool Tracking
        {
            get => _trackingMode != TrackingMode.Off;
            set
            {
                if (value == _tracking) { return; } //off

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (value)
                {
                    if (AtPark)
                    {
                        if (TrackingSpeak) { Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString()); }
                        throw new ASCOM.ParkedException(Application.Current.Resources["exParked"].ToString());
                    }

                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            _trackingMode = TrackingMode.AltAz;
                            if (TrackingSpeak) Synthesizer.Speak(Application.Current.Resources["vceTrackingOn"].ToString());
                            break;
                        case AlignmentModes.algGermanPolar:
                        case AlignmentModes.algPolar:
                            _trackingMode = SouthernHemisphere ? TrackingMode.EqS : TrackingMode.EqN;
                            if (TrackingSpeak) Synthesizer.Speak(Application.Current.Resources["vceTrackingOn"].ToString());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    if (TrackingSpeak && _trackingMode != TrackingMode.Off) Synthesizer.Speak(Application.Current.Resources["vceTrackingOff"].ToString());
                    _trackingMode = TrackingMode.Off;
                    IsPulseGuidingDec = false; // Ensure pulses are off
                    IsPulseGuidingRa = false;
                }
                _tracking = value; //off

                SetTracking();
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Simulator Items

        /// <summary>
        /// Sim GOTO slew
        /// </summary>
        /// <returns></returns>
        private static int SimGoTo(double[] target, bool trackingState)
        {

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|{ActualAxisY}|to|{target[0]}|{target[1]}|tracking|{trackingState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returncode = 0;
            const int timer = 120; //  stop slew after seconds
            var stopwatch = Stopwatch.StartNew();

            SimTasks(MountTaskName.StopAxes);
            var simTarget = GetSyncedAxes(Axes.AxesAppToMount(target));

            #region First Slew

            // time could be off a bit may need to deal with each axis separate
            object _ = new CmdAxisGoToTarget(0, Axis.Axis1, simTarget[0]);
            _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(100);
                if (SlewState == SlewType.SlewNone) break;
                var statusx = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                var axis1Status = (AxisStatus)MountQueue.GetCommandResult(statusx).Result;
                var axis1Stopped = axis1Status.Stopped;

                var statusy = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                var axis2Status = (AxisStatus)MountQueue.GetCommandResult(statusy).Result;
                var axis2Stopped = axis2Status.Stopped;

                if (!axis1Stopped || !axis2Stopped) continue;
                if (SlewSettleTime > 0) Tasks.DelayHandler(TimeSpan.FromSeconds(SlewSettleTime).Milliseconds); // post-slew settling time

                break;
            }
            stopwatch.Stop();

            AxesStopValidate();
            monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"GoToSeconds|{stopwatch.Elapsed.TotalSeconds}|SimTarget|{simTarget[0]}|{simTarget[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            #endregion

            #region Final precision slew
            if (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Task decTask = Task.Run(() => SimPrecisionGotoDec(simTarget[1]));
                Task raTask = Task.Run(() => SimPrecisionGoToRA(target, trackingState, stopwatch));

                Task.WaitAll(decTask, raTask);
            }
            #endregion

            SimTasks(MountTaskName.StopAxes);//make sure all axes are stopped
            return returncode;
        }

        /// <summary>
        /// Performs a final precision slew of the Dec axis to target if necessary.
        /// </summary>
        /// <param name="simTargetDec"></param>
        /// <returns></returns>
        private static int SimPrecisionGotoDec(double simTargetDec)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisY}|to|{simTargetDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returncode = 0;
            var gotoPrecision = SkySettings.GotoPrecision;
            var maxtries = 0;

            while (true)
            {
                if (maxtries > 3) { break; }
                maxtries++;

                // Calculate error
                var rawPositions = GetRawDegrees();
                if (rawPositions == null || double.IsNaN(rawPositions[1])) { break; }
                var deltaDegree = Math.Abs(simTargetDec - rawPositions[1]);

                if (deltaDegree < gotoPrecision) { break; }
                if (SlewState == SlewType.SlewNone) { break; } //check for a stop

                object _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTargetDec); //move to target DEC

                // track movement until axis is stopped
                var stopwatch1 = Stopwatch.StartNew();
                while (stopwatch1.Elapsed.TotalMilliseconds < 2000)
                {
                    if (SlewState == SlewType.SlewNone) { break; }
                    var deltay = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                    var axis2Status = (AxisStatus)(MountQueue.GetCommandResult(deltay).Result);
                    if (axis2Status.Stopped) { break; }
                }

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"DeltaDegrees|{deltaDegree}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            return returncode;
        }

        /// <summary>
        /// Perform a final precision slew of the RA axis to target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trackingState"></param>
        /// <param name="stopwatch"></param>
        /// <returns></returns>
        private static int SimPrecisionGoToRA(double[] target, bool trackingState, Stopwatch stopwatch)
        {

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|to|{target[0]}|state|{trackingState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returnCode = 0;
            if (!trackingState) return returnCode;
            //attempt precision moves to target
            var gotoPrecision = SkySettings.GotoPrecision;
            var rate = CurrentTrackingRate();
            var deltaTime = stopwatch.Elapsed.TotalSeconds;
            var maxtries = 0;

            while (true)
            {
                if (maxtries > 3) { break; }
                maxtries++;
                stopwatch.Reset();
                stopwatch.Start();

                //calculate new target position
                var deltaDegree = rate * deltaTime;
                if (deltaDegree < gotoPrecision) { break; }

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Deltas|Rate|{rate}|Time|{deltaTime}|Degree|{deltaDegree}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                target[0] += deltaDegree;
                var deltaTarget = GetSyncedAxes(Axes.AxesAppToMount(target));

                if (SlewState == SlewType.SlewNone) { break; } //check for a stop

                object _ = new CmdAxisGoToTarget(0, Axis.Axis1, deltaTarget[0]);//move to new target

                // check for axis stopped
                var stopwatch1 = Stopwatch.StartNew();
                while (stopwatch1.Elapsed.TotalMilliseconds < 2000)
                {
                    if (SlewState == SlewType.SlewNone) { break; }
                    var deltax = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                    var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltax).Result;
                    if (!axis1Status.Slewing) { break; }
                }

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Precision|NewTarget|{target[0]}|Time|{deltaTime}|Degree|{deltaDegree}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                deltaTime = stopwatch.Elapsed.TotalSeconds; //take the time and move again
            }

            return returnCode;
        }

        /// <summary>
        /// Creates tasks that are put in the MountQueue
        /// </summary>
        /// <param name="taskName"></param>
        public static void SimTasks(MountTaskName taskName)
        {
            if (!IsMountRunning) return;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{taskName}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            object _;

            switch (SkySettings.Mount)
            {
                case MountType.SkyWatcher:
                    break;
                case MountType.Simulator:
                    switch (taskName)
                    {
                        case MountTaskName.AllowAdvancedCommandSet:
                            break;
                        case MountTaskName.AlternatingPpec:
                            break;
                        case MountTaskName.CanAdvancedCmdSupport:
                            CanAdvancedCmdSupport = false;
                            break;
                        case MountTaskName.CanPpec:
                            CanPPec = false;
                            break;
                        case MountTaskName.CanPolarLed:
                            CanPolarLed = false;
                            break;
                        case MountTaskName.CanHomeSensor:
                            var canHomeCmda = new GetHomeSensorCapability(MountQueue.NewId);
                            bool.TryParse(Convert.ToString(MountQueue.GetCommandResult(canHomeCmda).Result), out bool hasHome);
                            CanHomeSensor = hasHome;
                            break;
                        case MountTaskName.DecPulseToGoTo:
                            break;
                        case MountTaskName.Encoders:
                            break;
                        case MountTaskName.FullCurrent:
                            break;
                        case MountTaskName.LoadDefaults:
                            break;
                        case MountTaskName.StopAxes:
                            _ = new CmdAxisStop(0, Axis.Axis1);
                            _ = new CmdAxisStop(0, Axis.Axis2);
                            break;
                        case MountTaskName.InstantStopAxes:
                            break;
                        case MountTaskName.SetSouthernHemisphere:
                            break;
                        case MountTaskName.SyncAxes:
                            var sync = Axes.AxesAppToMount(new[] { _mountAxes.X, _mountAxes.Y });
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, sync[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, sync[1]);
                            break;
                        case MountTaskName.SyncTarget:
                            var xy = Axes.RaDecToAxesXY(new[] { TargetRa, TargetDec });
                            var targ = Axes.AxesAppToMount(new[] { xy[0], xy[1] });
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, targ[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, targ[1]);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] { _altAzSync.Y, _altAzSync.X });
                            var altaz = Axes.AxesAppToMount(new[] { yx[1], yx[0] });
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, altaz[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, altaz[1]);
                            break;
                        case MountTaskName.MonitorPulse:
                            _ = new CmdSetMonitorPulse(0, MonitorPulse);
                            break;
                        case MountTaskName.Pec:
                            break;
                        case MountTaskName.PecTraining:
                            break;
                        case MountTaskName.Capabilities:
                            Capabilities = @"N/A";
                            break;
                        case MountTaskName.SetSt4Guiderate:
                            break;
                        case MountTaskName.SetSnapPort1:
                            _ = new CmdSnapPort(0, 1, SnapPort1);
                            SnapPort1Result = false;
                            break;
                        case MountTaskName.SetSnapPort2:
                            _ = new CmdSnapPort(0, 2, SnapPort2);
                            SnapPort2Result = true;
                            break;
                        case MountTaskName.MountName:
                            var mountName = new CmdMountName(MountQueue.NewId);
                            MountName = (string)MountQueue.GetCommandResult(mountName).Result;
                            break;
                        case MountTaskName.GetAxisVersions:
                            break;
                        case MountTaskName.GetAxisStrVersions:
                            break;
                        case MountTaskName.MountVersion:
                            var mountVersion = new CmdMountVersion(MountQueue.NewId);
                            MountVersion = (string)MountQueue.GetCommandResult(mountVersion).Result;
                            break;
                        case MountTaskName.StepsPerRevolution:
                            var spr = new CmdSpr(MountQueue.NewId);
                            var sprnum = (long)MountQueue.GetCommandResult(spr).Result;
                            StepsPerRevolution = new[] { sprnum, sprnum };
                            break;
                        case MountTaskName.StepsWormPerRevolution:
                            var spw = new CmdSpw(MountQueue.NewId);
                            var spwnum = (double)MountQueue.GetCommandResult(spw).Result;
                            StepsWormPerRevolution = new[] { spwnum, spwnum };
                            break;
                        case MountTaskName.SetHomePositions:
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, _homeAxes.X);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, _homeAxes.Y);
                            break;
                        case MountTaskName.GetFactorStep:
                            var factorStep = new CmdFactorSteps(MountQueue.NewId);
                            FactorStep[0] = (double)MountQueue.GetCommandResult(factorStep).Result;
                            FactorStep[1] = FactorStep[0];
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(taskName), taskName, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region SkyWatcher Items

        private static Vector _trackingOffsetRate;

        /// <summary>
        /// Custom Tracking Offset for RA calculate into arc seconds per sec
        /// </summary>
        public static double TrackingOffsetRaRate
        {
            get => _trackingOffsetRate.X;
            private set => _trackingOffsetRate.X = value;
        }

        /// <summary>
        /// Custom Tracking Offset for Dec calculate into arc seconds per sec
        /// </summary>
        public static double TrackingOffsetDecRate
        {
            get => _trackingOffsetRate.Y;
            private set => _trackingOffsetRate.Y = value;
        }

        /// <summary>
        /// Adjust tracking rate for Custom Mount Gearing Offset settings
        /// </summary>
        /// <returns>difference in rates</returns>
        private static void CalcCustomTrackingOffset()
        {
            _trackingOffsetRate = new Vector(0.0, 0.0);

            //calculate mount sidereal :I, add offset to :I, Calculate new rate, Add rate difference to rate
            if (SkySettings.Mount != MountType.SkyWatcher) { return; } //only use for sky watcher mounts

            if (SkySettings.CustomGearing == false) { return; }

            var ratioFactor = (double)StepsTimeFreq[0] / StepsPerRevolution[0] * 1296000.0;  //generic factor for calc
            var siderealI = ratioFactor / _siderealRate;
            siderealI += SkySettings.CustomRaTrackingOffset;  //calc :I and add offset
            var newRate = ratioFactor / siderealI; //calc new rate from offset
            TrackingOffsetRaRate = _siderealRate - newRate;

            ratioFactor = (double)StepsTimeFreq[1] / StepsPerRevolution[1] * 1296000.0;  //generic factor for calc
            siderealI = ratioFactor / _siderealRate;
            siderealI += SkySettings.CustomDecTrackingOffset;  //calc :I and add offset
            newRate = ratioFactor / siderealI; //calc new rate from offset
            TrackingOffsetDecRate = _siderealRate - newRate;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{TrackingOffsetRaRate}|{TrackingOffsetDecRate}" };
            MonitorLog.LogToMonitor(monitorItem);

        }

        // used to combine multiple sources for a single slew rate
        // include tracking, hand controller, etc..
        private static Vector SkyHCRate;
        private static Vector SkyTrackingRate;
        private static readonly int[] SkyTrackingOffset = { 0, 0 }; // Store for custom mount :I offset

        /// <summary>
        /// combines multiple rates for a single slew rate
        /// </summary>
        /// <returns></returns>
        private static Vector GetSlewRate()
        {
            var change = new Vector();
            change += SkyTrackingRate; // Tracking

            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                change = ConvertRateToAltAz(change.X);
            }

            change += SkyHCRate; // Hand controller
            change += _rateAxes; // MoveAxis at the given rate
            change += _rateRaDec;// External tracking rate

            //todo might be a good place to reject for axis limits
            CheckAxisLimits();

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{change}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            return change;
        }

        /// <summary>
        /// SkyWatcher GOTO slew
        /// </summary>
        /// <returns></returns>
        private static int SkyGoTo(double[] target, bool trackingState)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|{ActualAxisY}|to|{target[0]}|{target[1]}|tracking|{trackingState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returncode = 0;
            //  stop slew after 80 seconds
            const int timer = 120;// increased from 80 to 120, slew is taking longer for new commands
            var stopwatch = Stopwatch.StartNew();

            SkyTasks(MountTaskName.StopAxes);
            var skyTarget = GetSyncedAxes(Axes.AxesAppToMount(target));

            #region First Slew
            // time could be off a bit may need to deal with each axis separate
            object _ = new SkyAxisGoToTarget(0, AxisId.Axis1, skyTarget[0]);
            _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(100);
                if (SlewState == SlewType.SlewNone) { break; }

                var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                var axis1Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);

                var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                var axis2Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusy).Result);

                if (!axis1Stopped || !axis2Stopped) { continue; }

                if (SlewSettleTime > 0)
                {
                    Tasks.DelayHandler(TimeSpan.FromSeconds(SlewSettleTime).Milliseconds);// post-slew settling time
                }
                break;
            }
            stopwatch.Stop();
            AxesStopValidate();
            monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Current|{_util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|{_util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Seconds|{stopwatch.Elapsed.TotalSeconds}|Target|{skyTarget[0]}|{skyTarget[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            #endregion

            #region Final precision slew
            if (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Task decTask = Task.Run(() => SkyPrecisionGotoDec(skyTarget[1]));
                Task raTask = Task.Run(() => SkyPrecisionGoToRA(target, trackingState, stopwatch));
                Task.WaitAll(decTask, raTask);
            }
            #endregion

            SkyTasks(MountTaskName.StopAxes); //make sure all axes are stopped
            return returncode;
        }

        /// <summary>
        /// Performs a final precision slew of the Dec axis to target if necessary.
        /// </summary>
        /// <param name="skyTargetDec"></param>
        /// <returns></returns>
        private static int SkyPrecisionGotoDec(double skyTargetDec)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisY}|to|{skyTargetDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returncode = 0;
            var gotoPrecision = SkySettings.GotoPrecision;
            var maxtries = 0;

            while (true)
            {
                if (maxtries > 3) { break; }
                maxtries++;

                // Calculate error
                var rawPositions = GetRawDegrees();
                if (rawPositions == null || double.IsNaN(rawPositions[1])) { break; }
                var deltaDegree = Math.Abs(skyTargetDec - rawPositions[1]);

                if (deltaDegree < gotoPrecision) { break; }
                if (SlewState == SlewType.SlewNone) { break; } //check for a stop

                object _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTargetDec); //move to target DEC

                // track movement until axis is stopped
                var stopwatch1 = Stopwatch.StartNew();
                while (stopwatch1.Elapsed.TotalMilliseconds < 2000)
                {
                    if (SlewState == SlewType.SlewNone) { break; }
                    var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                    var axis2stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusy).Result);
                    if (axis2stopped) { break; }
                    Thread.Sleep(100);
                }
                stopwatch1.Stop();

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Delta|{deltaDegree}|Seconds|{stopwatch1.Elapsed.TotalSeconds}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            return returncode;
        }

        /// <summary>
        /// Perform a final precision slew of the RA axis to target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trackingState"></param>
        /// <param name="stopwatch"></param>
        /// <returns></returns>
        private static int SkyPrecisionGoToRA(double[] target, bool trackingState, Stopwatch stopwatch)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|to|{target[0]}|tracking|{trackingState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returnCode = 0;
            if (!trackingState) return returnCode;
            //attempt precision moves to target
            var gotoPrecision = SkySettings.GotoPrecision;
            var rate = CurrentTrackingRate();
            var deltaTime = stopwatch.Elapsed.TotalSeconds;
            var maxtries = 0;

            while (true)
            {
                if (maxtries > 3) { break; }
                maxtries++;
                stopwatch.Reset();
                stopwatch.Start();

                //calculate new target position
                var deltaDegree = rate * deltaTime;

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Delta|{deltaDegree}|Rate|{rate}|Time|{deltaTime}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (deltaDegree < gotoPrecision) { break; }

                target[0] += deltaDegree;
                var deltaTarget = GetSyncedAxes(Axes.AxesAppToMount(target));
                if (SlewState == SlewType.SlewNone) { break; } //check for a stop

                object _ = new SkyAxisGoToTarget(0, AxisId.Axis1, deltaTarget[0]); //move to new target

                // track movement until axis is stopped
                var stopwatch1 = Stopwatch.StartNew();
                while (stopwatch1.Elapsed.TotalMilliseconds < 2000)
                {
                    if (SlewState == SlewType.SlewNone) { break; }
                    var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                    var axis1stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);
                    if (axis1stopped) { break; }
                    Thread.Sleep(100);
                }
                stopwatch1.Stop();

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|NewTarget|{target[0]}|Seconds|{stopwatch1.Elapsed.TotalSeconds}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                deltaTime = stopwatch.Elapsed.TotalSeconds; //take the time and move again
            }

            return returnCode;
        }

        /// <summary>
        /// Creates tasks that are put in the SkyQueue
        /// </summary>
        /// <param name="taskName"></param>
        public static void SkyTasks(MountTaskName taskName)
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{taskName}"
            };

            object _;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    break;
                case MountType.SkyWatcher:
                    switch (taskName)
                    {
                        case MountTaskName.AllowAdvancedCommandSet:
                            _ = new SkyAllowAdvancedCommandSet(0, SkySettings.AllowAdvancedCommandSet);
                            break;
                        case MountTaskName.AlternatingPpec:
                            _ = new SkySetAlternatingPPec(0, SkySettings.AlternatingPPec);
                            break;
                        case MountTaskName.DecPulseToGoTo:
                            _ = new SkySetDecPulseToGoTo(0, SkySettings.DecPulseToGoTo);
                            break;
                        case MountTaskName.CanAdvancedCmdSupport:
                            var SkyCanAdvanced = new SkyGetAdvancedCmdSupport(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(SkyCanAdvanced).Result), out bool pAdvancedResult);
                            CanAdvancedCmdSupport = pAdvancedResult;
                            break;
                        case MountTaskName.CanPpec:
                            var SkyMountCanPpec = new SkyCanPPec(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(SkyMountCanPpec).Result), out bool pPecResult);
                            CanPPec = pPecResult;
                            break;
                        case MountTaskName.CanPolarLed:
                            var SkyCanPolarLed = new SkyCanPolarLed(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(SkyCanPolarLed).Result), out bool polarLedResult);
                            CanPolarLed = polarLedResult;
                            break;
                        case MountTaskName.CanHomeSensor:
                            var canHomeSky = new SkyCanHomeSensors(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(canHomeSky).Result), out bool homeSensoResult);
                            CanHomeSensor = homeSensoResult;
                            break;
                        case MountTaskName.Capabilities:
                            var skyCap = new SkyGetCapabilities(SkyQueue.NewId);
                            Capabilities = (string)SkyQueue.GetCommandResult(skyCap).Result;
                            break;
                        case MountTaskName.Encoders:
                            _ = new SkySetEncoder(0, AxisId.Axis1, SkySettings.Encoders);
                            _ = new SkySetEncoder(0, AxisId.Axis2, SkySettings.Encoders);
                            break;
                        case MountTaskName.FullCurrent:
                            _ = new SkySetFullCurrent(0, AxisId.Axis1, SkySettings.FullCurrent);
                            _ = new SkySetFullCurrent(0, AxisId.Axis2, SkySettings.FullCurrent);
                            break;
                        case MountTaskName.GetFactorStep:
                            var skyFactor = new SkyGetFactorStepToRad(SkyQueue.NewId);
                            FactorStep = (double[])SkyQueue.GetCommandResult(skyFactor).Result;
                            break;
                        case MountTaskName.LoadDefaults:
                            _ = new SkyLoadDefaultMountSettings(0);
                            break;
                        case MountTaskName.InstantStopAxes:
                            _ = new SkyAxisStopInstant(0, AxisId.Axis1);
                            _ = new SkyAxisStopInstant(0, AxisId.Axis2);
                            break;
                        case MountTaskName.MinPulseRa:
                            _ = new SkySetMinPulseDuration(0, AxisId.Axis1, SkySettings.MinPulseRa);
                            break;
                        case MountTaskName.MinPulseDec:
                            _ = new SkySetMinPulseDuration(0, AxisId.Axis2, SkySettings.MinPulseDec);
                            break;
                        case MountTaskName.MonitorPulse:
                            _ = new SkySetMonitorPulse(0, MonitorPulse);
                            break;
                        case MountTaskName.PecTraining:
                            _ = new SkySetPPecTrain(0, AxisId.Axis1, PecTraining);
                            break;
                        case MountTaskName.Pec:
                            var ppecon = new SkySetPPec(SkyQueue.NewId, AxisId.Axis1, SkySettings.PPecOn);
                            var ppeconstr = (string)SkyQueue.GetCommandResult(ppecon).Result;
                            if (string.IsNullOrEmpty(ppeconstr))
                            {
                                SkySettings.PPecOn = false;
                                break;
                            }
                            if (ppeconstr.Contains("!")) { SkySettings.PPecOn = false; }
                            break;
                        case MountTaskName.PolarLedLevel:
                            if (SkySettings.PolarLedLevel < 0 || SkySettings.PolarLedLevel > 255) { return; }
                            _ = new SkySetPolarLedLevel(0, AxisId.Axis1, SkySettings.PolarLedLevel);
                            break;
                        case MountTaskName.StopAxes:
                            _ = new SkyAxisStop(0, AxisId.Axis1);
                            _ = new SkyAxisStop(0, AxisId.Axis2);
                            break;
                        case MountTaskName.SetSt4Guiderate:
                            _ = new SkySetSt4GuideRate(0, SkySettings.St4GuideRate);
                            break;
                        case MountTaskName.SetSouthernHemisphere:
                            _ = new SkySetSouthernHemisphere(SkyQueue.NewId, SouthernHemisphere);
                            break;
                        case MountTaskName.SetSnapPort1:
                            var sp1 = new SkySetSnapPort(SkyQueue.NewId, 1, SnapPort1);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(sp1).Result), out bool port1Result);
                            SnapPort1Result = port1Result;
                            break;
                        case MountTaskName.SetSnapPort2:
                            var sp2 = new SkySetSnapPort(SkyQueue.NewId, 2, SnapPort2);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(sp2).Result), out bool port2Result);
                            SnapPort2Result = port2Result;
                            break;
                        case MountTaskName.SyncAxes:
                            var sync = Axes.AxesAppToMount(new[] { _mountAxes.X, _mountAxes.Y });
                            _ = new SkySyncAxis(0, AxisId.Axis1, sync[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, sync[1]);
                            monitorItem.Message += $",{_mountAxes.X}|{_mountAxes.Y}|{sync[0]}|{sync[1]}";
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        case MountTaskName.SyncTarget:
                            var xy = Axes.RaDecToAxesXY(new[] { TargetRa, TargetDec });
                            var targ = Axes.AxesAppToMount(new[] { xy[0], xy[1] });
                            _ = new SkySyncAxis(0, AxisId.Axis1, targ[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, targ[1]);
                            monitorItem.Message += $",{_util.HoursToHMS(TargetRa, "h ", ":", "", 2)}|{_util.DegreesToDMS(TargetDec, "° ", ":", "", 2)}|{xy[0]}|{xy[1]}|{targ[0]}|{targ[1]}";
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] { _altAzSync.Y, _altAzSync.X });
                            var altaz = Axes.AxesAppToMount(new[] { yx[1], yx[0] });
                            _ = new SkySyncAxis(0, AxisId.Axis1, altaz[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, altaz[1]);
                            monitorItem.Message += $",{_altAzSync.X}|{_altAzSync.Y}|{yx[1]}|{yx[0]}|{altaz[0]}|{altaz[1]}";
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        case MountTaskName.GetAxisVersions:
                            var skyAxisVersions = new SkyGetAxisStringVersions(SkyQueue.NewId);
                            // Not used atm
                            _ = (long[])SkyQueue.GetCommandResult(skyAxisVersions).Result;
                            break;
                        case MountTaskName.GetAxisStrVersions:
                            var skyAxisStrVersions = new SkyGetAxisStringVersions(SkyQueue.NewId);
                            // Not used atm
                            _ = (string)SkyQueue.GetCommandResult(skyAxisStrVersions).Result;
                            break;
                        case MountTaskName.MountName:
                            var skyMountType = new SkyMountType(SkyQueue.NewId);
                            MountName = (string)SkyQueue.GetCommandResult(skyMountType).Result;
                            break;
                        case MountTaskName.MountVersion:
                            var skyMountVersion = new SkyMountVersion(SkyQueue.NewId);
                            MountVersion = (string)SkyQueue.GetCommandResult(skyMountVersion).Result;
                            break;
                        case MountTaskName.StepsPerRevolution:
                            var SkyMountRevolutions = new SkyGetStepsPerRevolution(SkyQueue.NewId);
                            StepsPerRevolution = (long[])SkyQueue.GetCommandResult(SkyMountRevolutions).Result;
                            break;
                        case MountTaskName.StepsWormPerRevolution:
                            var SkyWormRevolutions1 = new SkyGetPecPeriod(SkyQueue.NewId, AxisId.Axis1);
                            StepsWormPerRevolution[0] = (double)SkyQueue.GetCommandResult(SkyWormRevolutions1).Result;
                            var SkyWormRevolutions2 = new SkyGetPecPeriod(SkyQueue.NewId, AxisId.Axis2);
                            StepsWormPerRevolution[1] = (double)SkyQueue.GetCommandResult(SkyWormRevolutions2).Result;
                            break;
                        case MountTaskName.StepTimeFreq:
                            var SkyStepTimeFreq = new SkyGetStepTimeFreq(SkyQueue.NewId);
                            StepsTimeFreq = (long[])SkyQueue.GetCommandResult(SkyStepTimeFreq).Result;
                            break;
                        case MountTaskName.SetHomePositions:
                            _ = new SkySetAxisPosition(0, AxisId.Axis1, _homeAxes.X);
                            _ = new SkySetAxisPosition(0, AxisId.Axis2, _homeAxes.Y);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Handles MountControlException and SkyServerException
        /// </summary>
        /// <param name="ex"></param>
        public static void SkyErrorHandler(Exception ex)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Error,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ex.Message}|{ex.StackTrace}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            AlertState = true;
            var extype = ex.GetType().ToString().Trim();
            switch (extype)
            {
                case "GS.SkyWatcher.MountControlException":
                    var mounterr = (MountControlException)ex;
                    switch (mounterr.ErrorCode)
                    {
                        case SkyWatcher.ErrorCode.ErrInvalidId:
                        case SkyWatcher.ErrorCode.ErrAlreadyConnected:
                        case SkyWatcher.ErrorCode.ErrNotConnected:
                        case SkyWatcher.ErrorCode.ErrInvalidData:
                        case SkyWatcher.ErrorCode.ErrSerialPortBusy:
                        case SkyWatcher.ErrorCode.ErrMountNotFound:
                        case SkyWatcher.ErrorCode.ErrNoResponseAxis1:
                        case SkyWatcher.ErrorCode.ErrNoResponseAxis2:
                        case SkyWatcher.ErrorCode.ErrAxisBusy:
                        case SkyWatcher.ErrorCode.ErrMaxPitch:
                        case SkyWatcher.ErrorCode.ErrMinPitch:
                        case SkyWatcher.ErrorCode.ErrUserInterrupt:
                        case SkyWatcher.ErrorCode.ErrAlignFailed:
                        case SkyWatcher.ErrorCode.ErrUnimplemented:
                        case SkyWatcher.ErrorCode.ErrWrongAlignmentData:
                        case SkyWatcher.ErrorCode.ErrQueueFailed:
                        case SkyWatcher.ErrorCode.ErrTooManyRetries:
                            IsMountRunning = false;
                            MountError = mounterr;
                            break;
                        default:
                            IsMountRunning = false;
                            MountError = mounterr;
                            break;
                    }

                    break;
                case "GS.Server.SkyTelescope.SkyServerException":
                    var skyerr = (SkyServerException)ex;
                    switch (skyerr.ErrorCode)
                    {
                        case ErrorCode.ErrMount:
                        case ErrorCode.ErrExecutingCommand:
                        case ErrorCode.ErrUnableToDeqeue:
                        case ErrorCode.ErrSerialFailed:
                            IsMountRunning = false;
                            MountError = skyerr;
                            break;
                        default:
                            IsMountRunning = false;
                            MountError = skyerr;
                            break;
                    }

                    break;
                default:
                    MountError = ex;
                    IsMountRunning = false;
                    break;
            }
        }

        /// <summary>
        /// Checks command object for errors and unsuccessful execution
        /// </summary>
        /// <param name="command"></param>
        /// <returns>true for errors found and not successful</returns>
        private static bool CheckSkyErrors(ISkyCommand command)
        {
            if (command.Exception != null)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Warning,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{command.Successful},{command.Exception.Message},{command.Exception.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

            return !command.Successful || command.Exception != null;
        }

        #endregion

        #region Shared Mount Items

        /// <summary>
        /// Abort Slew in a normal motion
        /// </summary>
        public static void AbortSlew(bool speak)
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{SlewState}|{Tracking}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            //IsSlewing = false;
            _rateAxes = new Vector();
            _rateRaDec = new Vector();
            SlewState = SlewType.SlewNone;
            var tracking = Tracking;
            Tracking = false; //added back in for spec "Tracking is returned to its pre-slew state"

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            TrackingSpeak = false;
            Tracking = tracking;
            TrackingSpeak = true;

            if (speak) { Synthesizer.Speak(Application.Current.Resources["vceAbortSlew"].ToString()); }
        }

        /// <summary>
        /// Auto home, Slew home based on mount's home sensor
        /// </summary>
        public static async void AutoHomeAsync(int degreeLimit = 100, int offSetDec = 0)
        {
            try
            {
                if (!IsMountRunning) { return; }
                IsAutoHomeRunning = true;
                LastAutoHomeError = null;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Started"
                };
                MonitorLog.LogToMonitor(monitorItem);

                var returncode1 = 0;
                var returncode2 = 0;
                if (degreeLimit < 20) { degreeLimit = 100; }
                AutoHomeProgressBar = 0;
                var EncoderTemp = SkySettings.Encoders;
                if (Tracking) { Tracking = false; }
                Synthesizer.Speak(Application.Current.Resources["btnAutoHomeStart"].ToString());
                Synthesizer.VoicePause = true;

                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        var autosim = new AutoHomeSim();
                        returncode1 = await Task.Run(() => autosim.StartAutoHome(Axis.Axis1, degreeLimit));
                        AutoHomeProgressBar = 50;
                        returncode2 = await Task.Run(() => autosim.StartAutoHome(Axis.Axis2, degreeLimit, offSetDec));
                        break;
                    case MountType.SkyWatcher:
                        var autossky = new AutoHomeSky();
                        returncode1 = await Task.Run(() => autossky.StartAutoHome(AxisId.Axis1, degreeLimit));
                        AutoHomeProgressBar = 50;
                        returncode2 = await Task.Run(() => autossky.StartAutoHome(AxisId.Axis2, degreeLimit, offSetDec));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // put encoder setting back
                SkySettings.Encoders = EncoderTemp;
                string msgcode1 = null;
                switch (returncode1)
                {
                    case 0:
                        //good
                        break;
                    case -1:
                        msgcode1 = "RA failed home sensor reset";
                        break;
                    case -2:
                        msgcode1 = "RA home sensor not found";
                        break;
                    case -3:
                        //stop requested
                        break;
                    case -4:
                        msgcode1 = "RA too many restarts";
                        break;
                    case -5:
                        msgcode1 = "RA home capability check failed";
                        break;
                    default:
                        msgcode1 = "Ra code not found";
                        break;
                }

                string msgcode2 = null;
                switch (returncode2)
                {
                    case 0:
                        //good
                        break;
                    case -1:
                        msgcode2 = "Dec failed home sensor reset";
                        break;
                    case -2:
                        msgcode2 = "Dec home sensor not found";
                        break;
                    case -3:
                        //stop requested
                        break;
                    case -4:
                        msgcode2 = "Dec too many restarts";
                        break;
                    case -5:
                        msgcode2 = "Dec home capability check failed";
                        break;
                    default:
                        msgcode2 = "Dec code not found";
                        break;
                }

                StopAxes();

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Complete: {returncode1}|{returncode2}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (returncode1 == 0 && returncode2 == 0)
                {
                    // all is ok
                    //ResetHomePositions();
                    ReSyncAxes();
                    Synthesizer.VoicePause = false;
                    Thread.Sleep(1500);
                    Synthesizer.Speak(Application.Current.Resources["btnAutoHomeComplete"].ToString());

                }
                else
                {
                    //throw only if not a cancel request
                    if (returncode1 != -3 && returncode2 != -3)
                    {
                        var ex = new Exception($"Incomplete: {msgcode1} ({returncode1}), {msgcode2}({returncode2})");
                        LastAutoHomeError = ex;
                        throw ex;
                    }
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                LastAutoHomeError = ex;
                MountError = ex;
            }
            finally
            {
                AutoHomeProgressBar = 100;
                IsAutoHomeRunning = false;
                Synthesizer.VoicePause = false; //make sure pause is off
            }
        }

        /// <summary>
        /// Makes sure the axes are at full stop
        /// </summary>
        /// <returns></returns>
        private static bool AxesStopValidate()
        {
            if (!IsMountRunning) { return true; }
            Stopwatch stopwatch;
            bool axis2Stopped;
            bool axis1Stopped;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:

                    stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalMilliseconds <= 5000)
                    {
                        SimTasks(MountTaskName.StopAxes);
                        Thread.Sleep(100);
                        var statusx = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                        var axis1Status = (AxisStatus)MountQueue.GetCommandResult(statusx).Result;
                        axis1Stopped = axis1Status.Stopped;

                        var statusy = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                        var axis2Status = (AxisStatus)MountQueue.GetCommandResult(statusy).Result;
                        axis2Stopped = axis2Status.Stopped;

                        if (!axis1Stopped || !axis2Stopped) { continue; }
                        return true;
                    }
                    return false;
                case MountType.SkyWatcher:
                    stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalMilliseconds <= 5000)
                    {
                        SkyTasks(MountTaskName.StopAxes);
                        Thread.Sleep(100);
                        var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                        axis1Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);

                        var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                        axis2Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusy).Result);

                        if (!axis1Stopped || !axis2Stopped) { continue; }
                        return true;
                    }
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Reports to driver is axis can move
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool CanMoveAxis(TelescopeAxes axis)
        {
            var ax = 0;
            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    ax = 1;
                    break;
                case TelescopeAxes.axisSecondary:
                    ax = 2;
                    break;
                case TelescopeAxes.axisTertiary:
                    ax = 3;
                    break;
            }

            return ax != 0 && ax <= SkySettings.NumMoveAxis;
        }

        /// <summary>
        /// Checks the axis limits. AltAz and Polar mounts allow continuous movement,
        /// GEM mounts check the hour angle limit.
        /// </summary>
        private static void CheckAxisLimits()
        {
            var limitHit = false;
            var trackingLimit = false;
            //combine flip angle and tracking limit for a total limit passed meridian
            var totLimit = SkySettings.HourAngleLimit + SkySettings.AxisTrackingLimit;
            // check the ranges of the axes
            // primary axis must be in the range 0 to 360 for AltAz or Polar
            // and -hourAngleLimit to 180 + hourAngleLimit for german polar
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    // the primary axis must be in the range 0 to 360
                    //_mountAxes.X = Range.Range360(_mountAxes.X);
                    break;
                case AlignmentModes.algGermanPolar:
                    // the primary axis needs to be in the range -180 to +180 to correspond with hour angles of -12 to 12.
                    // check if we have hit the hour angle limit 
                    if (SouthernHemisphere)
                    {
                        if (_mountAxes.X >= SkySettings.HourAngleLimit ||
                            _mountAxes.X <= -SkySettings.HourAngleLimit - 180)
                        {
                            limitHit = true;
                        }
                        // Check tracking limit
                        if (_mountAxes.X >= totLimit || _mountAxes.X <= -totLimit - 180)
                        {
                            trackingLimit = true;
                        }
                    }
                    else
                    {
                        if (_mountAxes.X >= SkySettings.HourAngleLimit + 180 ||
                            _mountAxes.X <= -SkySettings.HourAngleLimit)
                        {
                            limitHit = true;
                        }
                        //Check Tracking Limit
                        if (_mountAxes.X >= totLimit + 180 || _mountAxes.X <= -totLimit)
                        {
                            trackingLimit = true;
                        }
                    }

                    break;
                case AlignmentModes.algPolar:
                    // the axis needs to be in the range -180 to +180 to correspond with hour angles
                    //_mountAxes.X = Range.Range180(_mountAxes.X);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // secondary must be in the range -90 to 0 to +90 for normal 
            // and +90 to 180 to 270 for through the pole.
            // rotation is continuous
            //_mountAxes.X = Range.Range270(_mountAxes.X);

            LimitAlarm = limitHit;
            if (!trackingLimit) return;

            if (Tracking && SkySettings.LimitTracking) { Tracking = false; } // turn off tracking

            if (SkySettings.LimitPark && SlewState != SlewType.SlewPark) // only hit this once while in limit
            {
                var found = SkySettings.ParkPositions.Find(x => x.Name == SkySettings.ParkLimitName);
                if (found == null)
                {
                    StopAxes();
                }
                else
                {
                    ParkSelected = found;
                    GoToPark();
                }
            }
        }

        /// <summary>
        /// Slew state based on axis status
        /// </summary>
        private static void CheckSlewState()
        {
            var slewing = false;
            switch (SlewState)
            {
                case SlewType.SlewNone:
                    slewing = false;
                    break;
                case SlewType.SlewSettle:
                    slewing = true;
                    break;
                case SlewType.SlewMoveAxis:
                    slewing = true;
                    break;
                case SlewType.SlewRaDec:
                    slewing = true;
                    break;
                case SlewType.SlewAltAz:
                    slewing = true;
                    break;
                case SlewType.SlewPark:
                    slewing = true;
                    // Tracking = false;  // Tracking reject already false issue
                    // AtPark = true;
                    break;
                case SlewType.SlewHome:
                    slewing = true;
                    //  Tracking = false; // Tracking reject already false issue
                    break;
                case SlewType.SlewHandpad:
                    slewing = true;
                    break;
                case SlewType.SlewComplete:
                    SlewState = SlewType.SlewNone;
                    break;
                default:
                    SlewState = SlewType.SlewNone;
                    break;
            }

            if (Math.Abs(RateAxisRa + RateAxisDec) > 0) { slewing = true; }
            IsSlewing = slewing;
        }

        /// <summary>
        /// Resets the spiral list if slew is out of limit
        /// </summary>
        private static void CheckSpiralLimit()
        {
            if (!SkySettings.SpiralLimits) return;
            if (SpiralCollection.Count == 0) return;
            var point = SpiralCollection[0];
            if (point == null) return;
            // calc distance between two coordinates
            var distance = Calculations.AngularDistance(RightAscensionXForm, DeclinationXForm, point.RaDec.X, point.RaDec.Y);
            if (distance <= SkySettings.SpiralDistance) return;
            SpiralCollection.Clear();
            SkySettings.SpiralDistance = 0;
            SpiralChanged = true;
        }

        /// <summary>
        /// Convert the move rate in hour angle to a change in altitude and azimuth
        /// </summary>
        /// <param name="haChange">The ha change.</param>
        /// <returns></returns>
        private static Vector ConvertRateToAltAz(double haChange)
        {
            var change = new Vector();

            var latRad = Principles.Units.Deg2Rad(SkySettings.Latitude);
            var azmRad = Principles.Units.Deg2Rad(Azimuth);
            var zenithAngle = Principles.Units.Deg2Rad((90 - Altitude)); // in radians

            // get the azimuth and elevation rates, as a ratio of the tracking rate
            var elevationRate = Math.Sin(azmRad) * Math.Cos(latRad);
            // fails at zenith so set a very large value, the limit check will trap this
            var azimuthRate =
                Math.Abs(Altitude - 90.0) > 0
                    ? (Math.Sin(latRad) * Math.Sin(zenithAngle) -
                       Math.Cos(latRad) * Math.Cos(zenithAngle) * Math.Cos(azmRad)) / Math.Sin(zenithAngle)
                    :
                    //_altAzm.Y != 90.0 ?(Math.Sin(latRad) * Math.Sin(zenithAngle) - Math.Cos(latRad) * Math.Cos(zenithAngle) * Math.Cos(azmRad)) / Math.Sin(zenithAngle) :
                    Azimuth >= 90 && Azimuth <= 270
                        ? 10000
                        : -10000;

            // get the changes in altitude and azimuth using the hour angle change and rates.
            change.Y = elevationRate * haChange;
            change.X = azimuthRate * haChange;
            // stop the secondary going past the vertical
            if (change.Y > 90 - Altitude) { change.Y = 0; }
            // limit the primary to the maximum slew rate
            if (change.X < -SlewSpeedEight) { change.X = -SlewSpeedEight; }
            if (change.X > SlewSpeedEight) { change.X = SlewSpeedEight; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"change:{change.X}|{change.Y}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return change;
        }

        /// <summary>
        /// Calculates the current tracking rate used in arc seconds per degree
        /// </summary>
        /// <returns></returns>
        public static double CurrentTrackingRate()
        {
            double rate;
            switch (SkySettings.TrackingRate)
            {
                case DriveRates.driveSidereal:
                    rate = SkySettings.SiderealRate;
                    break;
                case DriveRates.driveSolar:
                    rate = SkySettings.SolarRate;
                    break;
                case DriveRates.driveLunar:
                    rate = SkySettings.LunarRate;
                    break;
                case DriveRates.driveKing:
                    rate = SkySettings.KingRate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (rate < _siderealRate * 2 & rate != 0) //add any custom gearing offset
            {
                rate += TrackingOffsetRaRate;
            }

            //Implement Pec
            if (PecOn && Tracking && PecBinNow != null && !double.IsNaN(PecBinNow.Item2))
            {
                // safety check to make sure factor isn't too big
                if (Math.Abs(PecBinNow.Item2 - 1) < .04)
                {
                    rate *= PecBinNow.Item2;
                }
            }
            rate /= 3600;
            if (SkySettings.RaTrackingOffset <= 0) { return rate; }
            var offsetrate = rate * (Convert.ToDouble(SkySettings.RaTrackingOffset) / 100000);
            rate += offsetrate;
            return rate;
        }

        public static ParkPosition GetStoredParkPosition()
        {
            var p = new ParkPosition { Name = SkySettings.ParkName, X = SkySettings.ParkAxisX, Y = SkySettings.ParkAxisY };
            return p;
        }

        /// <summary>
        /// Used when the mount is first turned on and the instance is created
        /// </summary>
        private static double[] GetDefaultPositions()
        {
            double[] positions;
            // set default home position
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algGermanPolar:
                    _homeAxes.X = 90;
                    _homeAxes.Y = 90;
                    break;
                case AlignmentModes.algPolar:
                    _homeAxes.X = -90;
                    _homeAxes.Y = 0;
                    break;
                case AlignmentModes.algAltAz:
                    break;
                default:
                    _homeAxes.X = 90;
                    _homeAxes.Y = 90;
                    break;
            }

            // get home override from the settings
            if (Math.Abs(SkySettings.HomeAxisX) > 0 || Math.Abs(SkySettings.HomeAxisY) > 0)
            {
                _homeAxes.X = SkySettings.HomeAxisX;
                _homeAxes.Y = SkySettings.HomeAxisY;
            }

            MonitorEntry monitorItem;
            if (AtPark)
            {
                if (SkySettings.AutoTrack)
                {
                    AtPark = false;
                    Tracking = SkySettings.AutoTrack;
                }

                positions = Axes.AxesAppToMount(new[] { SkySettings.ParkAxisX, SkySettings.ParkAxisY });
                ParkSelected = GetStoredParkPosition();

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Parked,{SkySettings.ParkName}|{SkySettings.ParkAxisX}|{SkySettings.ParkAxisY}"
                };
                MonitorLog.LogToMonitor(monitorItem);

            }
            else
            {
                positions = new[] { _homeAxes.X, _homeAxes.Y };
            }

            monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Load:{positions[0]}|{positions[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return positions;
        }

        /// <summary>
        /// Gets current converted positions from the mount in degrees
        /// </summary>
        /// <returns></returns>
        private static double[] GetRawDegrees()
        {
            var actualDegrees = new[] { double.NaN, double.NaN };
            if (!IsMountRunning) { return actualDegrees; }
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    var simPositions = new CmdAxesDegrees(MountQueue.NewId);
                    actualDegrees = (double[])MountQueue.GetCommandResult(simPositions).Result;
                    break;
                case MountType.SkyWatcher:
                    var skyPositions = new SkyGetPositionsInDegrees(SkyQueue.NewId);
                    actualDegrees = (double[])SkyQueue.GetCommandResult(skyPositions).Result;
                    return CheckSkyErrors(skyPositions) ? null : actualDegrees;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return actualDegrees;
        }

        /// <summary>
        /// Convert steps to degrees
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="axis"></param>
        /// <returns>degrees</returns>
        private static double ConvertStepsToDegrees(double steps, int axis)
        {
            double degrees;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    degrees = steps / FactorStep[axis];
                    break;
                case MountType.SkyWatcher:
                    degrees = Principles.Units.Rad2Deg1(steps * FactorStep[axis]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return degrees;
        }

        /// <summary>
        /// Get steps from the mount
        /// </summary>
        /// <returns>double array</returns>
        private static double[] GetRawSteps()
        {
            var steps = new[] { double.NaN, double.NaN };
            if (!IsMountRunning) { return steps; }
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    var simPositions = new CmdAxesDegrees(MountQueue.NewId);
                    steps = (double[])MountQueue.GetCommandResult(simPositions).Result;
                    steps[0] *= FactorStep[0];
                    steps[1] *= FactorStep[1];
                    break;
                case MountType.SkyWatcher:
                    var skySteps = new SkyGetSteps(SkyQueue.NewId);
                    steps = (double[])SkyQueue.GetCommandResult(skySteps).Result;

                    return CheckSkyErrors(skySteps) ? new[] { double.NaN, double.NaN } : steps;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return steps;
        }

        /// <summary>
        /// Gets current positions from the mount in steps
        /// </summary>
        /// <returns></returns>
        private static double? GetRawSteps(int axis)
        {
            if (!IsMountRunning) { return null; }
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    var simPositions = new CmdAxisSteps(MountQueue.NewId);
                    var a = (int[])MountQueue.GetCommandResult(simPositions).Result;

                    switch (axis)
                    {
                        case 0:
                            return Convert.ToDouble(a[0]);
                        case 1:
                            return Convert.ToDouble(a[1]);
                        default:
                            return null;
                    }
                case MountType.SkyWatcher:
                    switch (axis)
                    {
                        case 0:
                            var b = new SkyGetAxisPositionCounter(SkyQueue.NewId, AxisId.Axis1);
                            return Convert.ToDouble(SkyQueue.GetCommandResult(b).Result);
                        case 1:
                            var c = new SkyGetAxisPositionCounter(SkyQueue.NewId, AxisId.Axis2);
                            return Convert.ToDouble(SkyQueue.GetCommandResult(c).Result);
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        public static Tuple<double?, DateTime> GetRawStepsDt(int axis)
        {
            if (!IsMountRunning) { return null; }
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    switch (axis)
                    {
                        case 0:
                            var a = new AxisStepsDt(MountQueue.NewId, Axis.Axis1);
                            return MountQueue.GetCommandResult(a).Result;
                        case 1:
                            var b = new AxisStepsDt(MountQueue.NewId, Axis.Axis2);
                            return MountQueue.GetCommandResult(b).Result;
                        default:
                            return null;
                    }
                case MountType.SkyWatcher:
                    switch (axis)
                    {
                        case 0:
                            var b = new SkyGetAxisPositionDate(SkyQueue.NewId, AxisId.Axis1);
                            return SkyQueue.GetCommandResult(b).Result;
                        case 1:
                            var c = new SkyGetAxisPositionCounter(SkyQueue.NewId, AxisId.Axis2);
                            return SkyQueue.GetCommandResult(c).Result;
                        default:
                            return null;
                    }
                default:
                    return null;
            }
        }

        ///// <summary>
        ///// gets HC speed in degrees
        ///// </summary>
        ///// <param name="speed"></param>
        ///// <returns></returns>
        //public static double GetSlewSpeed(SlewSpeed speed)
        //{
        //    switch (speed)
        //    {
        //        case SlewSpeed.One:
        //            return SlewSpeedOne;
        //        case SlewSpeed.Two:
        //            return SlewSpeedTwo;
        //        case SlewSpeed.Three:
        //            return SlewSpeedThree;
        //        case SlewSpeed.Four:
        //            return SlewSpeedFour;
        //        case SlewSpeed.Five:
        //            return SlewSpeedFive;
        //        case SlewSpeed.Six:
        //            return SlewSpeedSix;
        //        case SlewSpeed.Seven:
        //            return SlewSpeedSeven;
        //        case SlewSpeed.Eight:
        //            return SlewSpeedEight;
        //        default:
        //            return 0.0;
        //    }
        //}

        /// <summary>
        /// Runs the Goto in async so not to block the driver or UI threads.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slewState"></param>
        private static async void GoToAsync(double[] target, SlewType slewState)
        {
            if (!IsMountRunning) { return; }
            if (IsSlewing)
            {
                SlewState = SlewType.SlewNone;
                var stopped = AxesStopValidate();
                if (!stopped)
                {
                    AbortSlew(true);
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = "GoToAsync",
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Timeout stopping axes"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    return;
                }
            }

            SlewState = slewState;
            var startingState = slewState;
            var trackingState = Tracking;
            TrackingSpeak = false;
            Tracking = false;
            IsSlewing = true;

            var returncode = 1;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    returncode = await Task.Run(() => SimGoTo(target, trackingState));
                    break;
                case MountType.SkyWatcher:
                    returncode = await Task.Run(() => SkyGoTo(target, trackingState));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            TrackingSpeak = false;

            if (returncode == 0)
            {
                if (SlewState == SlewType.SlewNone)
                {
                    Tracking = trackingState;
                    TrackingSpeak = true;
                    return;
                }
                switch (startingState)
                {
                    case SlewType.SlewNone:
                        break;
                    case SlewType.SlewSettle:
                        break;
                    case SlewType.SlewMoveAxis:
                        break;
                    case SlewType.SlewRaDec:
                        break;
                    case SlewType.SlewAltAz:
                        break;
                    case SlewType.SlewPark:
                        AtPark = true;
                        break;
                    case SlewType.SlewHome:
                        break;
                    case SlewType.SlewHandpad:
                        break;
                    case SlewType.SlewComplete:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "GoToAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{SlewState} finished|code|{returncode}|{_util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|{_util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Actual|{ActualAxisX}|{ActualAxisY}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SlewState = SlewType.SlewNone;
                SpeakSlewEnd(startingState);
                Tracking = trackingState;
                TrackingSpeak = true;

                return;
            }
            Tracking = trackingState;
            AbortSlew(true);
            MountError = new Exception($"GoTo Async Error|{returncode}");
        }

        /// <summary>
        /// Goto home slew
        /// </summary>
        public static void GoToHome()
        {
            if (IsSlewing)
            {
                StopAxes();
                return;
            }

            Tracking = false;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = "Slew to Home"
            };
            MonitorLog.LogToMonitor(monitorItem);
            SlewMount(new Vector(_homeAxes.X, _homeAxes.Y), SlewType.SlewHome);
        }

        /// <summary>
        /// Goto park slew
        /// </summary>
        public static void GoToPark()
        {
            if (IsSlewing)
            {
                StopAxes();
                return;
            }

            // get position selected could be set from UI or AsCom
            var ps = ParkSelected;
            if (ps == null) { return; }
            if (double.IsNaN(ps.X)) { return; }
            if (double.IsNaN(ps.Y)) { return; }
            SetParkAxis(ps.Name, ps.X, ps.Y);

            // Store for startup default position
            SkySettings.ParkAxisX = ps.X;
            SkySettings.ParkAxisY = ps.Y;
            SkySettings.ParkName = ps.Name;

            Tracking = false;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ps.Name}|{ps.X}|{ps.Y}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            SlewMount(new Vector(ps.X, ps.Y), SlewType.SlewPark);
        }

        /// <summary>
        /// return the change in axis values as a result of any HC button presses
        /// </summary>
        /// <returns></returns>
        public static void HcMoves(SlewSpeed speed, SlewDirection direction, HCMode HcMode, bool HcAntiRa, bool HcAntiDec, int RaBacklash, int DecBacklash)
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{SkySettings.HcSpeed}|{HcMode}|{direction}|{ActualAxisX}|{ActualAxisY}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var change = new double[] { 0, 0 };
            double delta;
            switch (speed)
            {
                case SlewSpeed.One:
                    delta = SlewSpeedOne;
                    break;
                case SlewSpeed.Two:
                    delta = SlewSpeedTwo;
                    break;
                case SlewSpeed.Three:
                    delta = SlewSpeedThree;
                    break;
                case SlewSpeed.Four:
                    delta = SlewSpeedFour;
                    break;
                case SlewSpeed.Five:
                    delta = SlewSpeedFive;
                    break;
                case SlewSpeed.Six:
                    delta = SlewSpeedSix;
                    break;
                case SlewSpeed.Seven:
                    delta = SlewSpeedSeven;
                    break;
                case SlewSpeed.Eight:
                    delta = SlewSpeedEight;
                    break;
                default:
                    delta = 0;
                    break;
            }

            // Check hand control mode and direction
            switch (HcMode)
            {
                case HCMode.Axes:
                    switch (direction)
                    {
                        case SlewDirection.SlewNorth:
                        case SlewDirection.SlewUp:
                            change[1] = delta;
                            break;
                        case SlewDirection.SlewSouth:
                        case SlewDirection.SlewDown:
                            change[1] = -delta;
                            break;
                        case SlewDirection.SlewEast:
                        case SlewDirection.SlewLeft:
                            change[0] = SouthernHemisphere ? -delta : delta;
                            break;
                        case SlewDirection.SlewWest:
                        case SlewDirection.SlewRight:
                            change[0] = SouthernHemisphere ? delta : -delta;
                            break;
                        case SlewDirection.SlewNoneRa:
                            if (HcPrevMoveRa != null)
                            {
                                HcPrevMoveRa.StepEnd = GetRawSteps(0);
                                if (HcPrevMoveRa.StepEnd.HasValue && HcPrevMoveRa.StepStart.HasValue)
                                {
                                    HcPrevMoveRa.StepDiff = Math.Abs(HcPrevMoveRa.StepEnd.Value - HcPrevMoveRa.StepStart.Value);
                                }
                            }
                            break;
                        case SlewDirection.SlewNoneDec:
                            if (HcPrevMoveDec != null)
                            {
                                HcPrevMoveDec.StepEnd = GetRawSteps(1);
                                if (HcPrevMoveDec.StepEnd.HasValue && HcPrevMoveDec.StepStart.HasValue)
                                {
                                    HcPrevMoveDec.StepDiff = Math.Abs(HcPrevMoveDec.StepEnd.Value - HcPrevMoveDec.StepStart.Value);
                                    HcPrevMovesDec.Add(HcPrevMoveDec.StepDiff);
                                }
                            }
                            break;
                        default:
                            change[0] = 0;
                            change[1] = 0;
                            break;
                    }
                    break;
                case HCMode.Guiding:
                    switch (direction)
                    {
                        case SlewDirection.SlewNorth:
                        case SlewDirection.SlewUp:
                            switch (SkySettings.Mount)
                            {
                                case MountType.Simulator:
                                    change[1] = SideOfPier == PierSide.pierEast ? delta : -delta;
                                    break;
                                case MountType.SkyWatcher:
                                    change[1] = SideOfPier == PierSide.pierEast ? -delta : delta;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case SlewDirection.SlewSouth:
                        case SlewDirection.SlewDown:
                            switch (SkySettings.Mount)
                            {
                                case MountType.Simulator:
                                    change[1] = SideOfPier == PierSide.pierWest ? delta : -delta;
                                    break;
                                case MountType.SkyWatcher:
                                    change[1] = SideOfPier == PierSide.pierWest ? -delta : delta;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case SlewDirection.SlewEast:
                        case SlewDirection.SlewLeft:
                            change[0] = SouthernHemisphere ? delta : -delta;
                            break;
                        case SlewDirection.SlewWest:
                        case SlewDirection.SlewRight:
                            change[0] = SouthernHemisphere ? -delta : delta;
                            break;
                        case SlewDirection.SlewNoneRa:
                            if (HcPrevMoveRa != null)
                            {
                                HcPrevMoveRa.StepEnd = GetRawSteps(0);
                                if (HcPrevMoveRa.StepEnd.HasValue && HcPrevMoveRa.StepStart.HasValue)
                                {
                                    HcPrevMoveRa.StepDiff = Math.Abs(HcPrevMoveRa.StepEnd.Value - HcPrevMoveRa.StepStart.Value);
                                }
                            }
                            break;
                        case SlewDirection.SlewNoneDec:
                            if (HcPrevMoveDec != null)
                            {
                                HcPrevMoveDec.StepEnd = GetRawSteps(1);
                                if (HcPrevMoveDec.StepEnd.HasValue && HcPrevMoveDec.StepStart.HasValue)
                                {
                                    HcPrevMoveDec.StepDiff = Math.Abs(HcPrevMoveDec.StepEnd.Value - HcPrevMoveDec.StepStart.Value);
                                    HcPrevMovesDec.Add(HcPrevMoveDec.StepDiff);
                                }
                            }
                            break;
                        default:
                            change[0] = 0;
                            change[1] = 0;
                            break;
                    }
                    break;
                default:
                    change[0] = 0;
                    change[1] = 0;
                    break;
            }

            // Log data to Monitor
            if (Math.Abs(change[0]) > 0 || Math.Abs(change[1]) > 0)
            {
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{SkySettings.HcSpeed}|{direction}|{change[0]}|{change[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

            // Set appropriate slew state
            SlewState = Math.Abs(change[0]) + Math.Abs(change[1]) > 0 ? SlewType.SlewHandpad : SlewType.SlewNone;

            // Anti-backlash compensate
            long stepsNeededDec = 0;
            if (HcAntiDec && DecBacklash > 0 && HcPrevMoveDec != null)
            {
                switch (direction)
                {
                    case SlewDirection.SlewNorth:
                    case SlewDirection.SlewUp:
                    case SlewDirection.SlewSouth:
                    case SlewDirection.SlewDown:
                        if (Math.Abs(HcPrevMoveDec.Delta) > 0.000000 &&
                            Math.Sign(HcPrevMoveDec.Delta) != Math.Sign(change[1]))
                        {
                            stepsNeededDec = Convert.ToInt64(HcPrevMovesDec.Sum());
                            if (stepsNeededDec >= DecBacklash)
                            {
                                stepsNeededDec = DecBacklash;
                            }

                            if (change[1] < 0) stepsNeededDec = -stepsNeededDec;
                        }
                        break;
                }
            }
            long stepsNeededRa = 0;
            if (HcAntiRa && Tracking && RaBacklash > 0 && HcPrevMoveRa != null)
            {
                if (direction == SlewDirection.SlewNoneRa)
                {
                    if (HcPrevMoveRa.StepEnd.HasValue && HcPrevMoveRa.StepStart.HasValue)
                    {
                        if (SouthernHemisphere)
                        {
                            if (HcPrevMoveRa.StepEnd.Value > HcPrevMoveRa.StepStart.Value)
                            {
                                stepsNeededRa = Convert.ToInt64(HcPrevMoveRa.StepDiff);
                                if (stepsNeededRa >= RaBacklash) { stepsNeededRa = RaBacklash; }
                                stepsNeededRa = -Math.Abs(stepsNeededRa);
                            }
                        }
                        else
                        {
                            if (HcPrevMoveRa.StepEnd.Value < HcPrevMoveRa.StepStart.Value)
                            {
                                stepsNeededRa = Convert.ToInt64(HcPrevMoveRa.StepDiff);
                                if (stepsNeededRa >= RaBacklash) { stepsNeededRa = RaBacklash; }
                            }
                        }
                    }
                }
            }

            //  log anti-lash moves
            if (Math.Abs(stepsNeededDec) > 0 && HcPrevMoveDec != null)
            {
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{HcPrevMoveDec.Delta}|{HcPrevMovesDec.Sum()},Anti-Lash,{stepsNeededDec} of {DecBacklash}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            if (Math.Abs(stepsNeededRa) > 0 && HcPrevMoveRa != null)
            {
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{HcPrevMoveRa.Direction}|{HcPrevMoveRa.StepDiff},Anti-Lash,{stepsNeededRa} of {RaBacklash}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

            // Track previous direction moves
            switch (direction)
            {
                case SlewDirection.SlewNorth:
                case SlewDirection.SlewUp:
                case SlewDirection.SlewSouth:
                case SlewDirection.SlewDown:
                    HcPrevMoveDec = new HcPrevMove
                    {
                        Direction = direction,
                        StartDate = HiResDateTime.UtcNow,
                        Delta = change[1]
                    };
                    break;
                case SlewDirection.SlewEast:
                case SlewDirection.SlewLeft:
                case SlewDirection.SlewWest:
                case SlewDirection.SlewRight:
                    HcPrevMoveRa = new HcPrevMove
                    {
                        Direction = direction,
                        StartDate = HiResDateTime.UtcNow,
                        Delta = change[0],
                        StepStart = GetRawSteps(0),
                    };
                    break;
                case SlewDirection.SlewNoneRa:
                case SlewDirection.SlewNoneDec:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            // Send to mount
            object _;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    //RA Anti-backlash compensate
                    if (Math.Abs(stepsNeededDec) > 0)
                    {
                        HcPrevMovesDec.Clear();

                        var a = new CmdAxesDegrees(MountQueue.NewId);
                        var b = (double[])MountQueue.GetCommandResult(a).Result;
                        var arcsecs = Conversions.StepPerArcSec(StepsPerRevolution[1]);
                        var c = stepsNeededDec / arcsecs;
                        var d = Conversions.ArcSec2Deg(c);
                        _ = new CmdAxisGoToTarget(0, Axis.Axis2, b[1] + d);

                        // check for axis stopped
                        var stopwatch1 = Stopwatch.StartNew();
                        while (stopwatch1.Elapsed.TotalSeconds <= 2)
                        {
                            var deltax = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                            var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltax).Result;
                            if (!axis1Status.Slewing) break; // stopped doesn't report quick enough
                        }
                    }

                    // Correct position after lash correction
                    if (HcPrevMoveDec != null) HcPrevMoveDec.StepStart = GetRawSteps(1);

                    if (Math.Abs(stepsNeededRa) > 0)
                    {
                        var a = new CmdAxesDegrees(MountQueue.NewId);
                        var b = (double[])MountQueue.GetCommandResult(a).Result;
                        var arcsecs = Conversions.StepPerArcSec(StepsPerRevolution[0]);
                        var c = (stepsNeededRa) / arcsecs;
                        var d = Conversions.ArcSec2Deg(c);
                        _ = new CmdAxisGoToTarget(0, Axis.Axis1, b[0] + d);

                        // check for axis stopped
                        var stopwatch1 = Stopwatch.StartNew();
                        while (stopwatch1.Elapsed.TotalSeconds <= 2)
                        {
                            var deltax = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                            var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltax).Result;
                            if (!axis1Status.Slewing) break; // stopped doesn't report quick enough
                        }

                        GetRawDegrees();
                    }

                    _ = new CmdHcSlew(0, Axis.Axis1, change[0]);
                    _ = new CmdHcSlew(0, Axis.Axis2, change[1]);

                    break;
                case MountType.SkyWatcher:
                    // implement anti-backlash
                    if (Math.Abs(stepsNeededDec) > 0)
                    {
                        HcPrevMovesDec.Clear();

                        _ = new SkyAxisMoveSteps(0, AxisId.Axis2, stepsNeededDec);

                        var stopwatch = Stopwatch.StartNew();
                        while (stopwatch.Elapsed.TotalSeconds <= 2)
                        {
                            var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                            var axis2Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusy).Result);

                            if (!axis2Stopped) { continue; }
                            break;
                        }
                    }

                    // Correct position after lash correction
                    if (HcPrevMoveDec != null) HcPrevMoveDec.StepStart = GetRawSteps(1);

                    if (Math.Abs(stepsNeededRa) > 0)
                    {
                        _ = new SkyAxisMoveSteps(0, AxisId.Axis1, stepsNeededRa);
                        var stopwatch = Stopwatch.StartNew();
                        while (stopwatch.Elapsed.TotalSeconds <= 2)
                        {
                            var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                            var axis1Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);

                            if (!axis1Stopped) { continue; }
                            break;
                        }
                    }

                    SkyHCRate.X = change[0];
                    SkyHCRate.Y = change[1];
                    var rate = GetSlewRate();
                    _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                    _ = new SkyAxisSlew(0, AxisId.Axis2, rate.Y);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Resets the anti-backlash for the hand controller
        /// </summary>
        private static void HcResetPrevMove(MountAxis axis)
        {
            switch (axis)
            {
                case MountAxis.Dec:
                    HcPrevMoveDec = null;
                    break;
                case MountAxis.Ra:
                    HcPrevMoveRa = null;
                    break;
            }
        }

        /// <summary>
        /// Sets up defaults after an established connection
        /// </summary>
        private static bool MountConnect()
        {
            object _;
            if (!AsComOn) { AsComOn = true; }
            _targetRaDec = new Vector(double.NaN, double.NaN); // invalid target position
            var positions = GetDefaultPositions();
            double[] rawPositions = null;
            var counter = 0;
            int raWormTeeth;
            int decWormTeeth;
            bool positionsSet = false;
            MonitorEntry monitorItem;
            string msg;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    // defaults
                    SimTasks(MountTaskName.MountName);
                    SimTasks(MountTaskName.MountVersion);
                    SimTasks(MountTaskName.StepsPerRevolution);
                    SimTasks(MountTaskName.StepsWormPerRevolution);
                    SimTasks(MountTaskName.CanHomeSensor);
                    SimTasks(MountTaskName.GetFactorStep);
                    SimTasks(MountTaskName.Capabilities);

                    raWormTeeth = (int)(StepsPerRevolution[0] / StepsWormPerRevolution[0]);
                    decWormTeeth = (int)(StepsPerRevolution[1] / StepsWormPerRevolution[1]);
                    WormTeethCount = new[] { raWormTeeth, decWormTeeth };
                    PecBinSteps = StepsPerRevolution[0] / (WormTeethCount[0] * 1.0) / PecBinCount;

                    // checks if the mount is close enough to home position to set default position. If not use the positions from the mount
                    while (rawPositions == null)
                    {
                        if (counter > 5)
                        {
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, positions[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, positions[1]);
                            positionsSet = true;
                            monitorItem = new MonitorEntry
                            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Counter exceeded:{positions[0]}|{positions[1]}" };
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        }
                        counter++;

                        rawPositions = GetRawDegrees();
                        msg = rawPositions != null ? $"GetRawDegrees:{rawPositions[0]}|{rawPositions[1]}" : $"NULL";
                        monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                        MonitorLog.LogToMonitor(monitorItem);

                        if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1]))
                        {
                            rawPositions = null;
                            continue;
                        }

                        //is mount parked, if so set to the default position
                        if (AtPark)
                        {
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, positions[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, positions[1]);
                            positionsSet = true;
                            break;
                        }

                        if (!rawPositions[0].IsBetween(-.1, .1) || !rawPositions[1].IsBetween(-.1, .1)) { continue; }

                        _ = new CmdAxisToDegrees(0, Axis.Axis1, positions[0]);
                        _ = new CmdAxisToDegrees(0, Axis.Axis2, positions[1]);
                        positionsSet = true;

                    }
                    // Update AlignmentModel settings.
                    ConnectAlignmentModel();

                    break;
                case MountType.SkyWatcher:
                    SkyHCRate = new Vector(0, 0);
                    SkyTrackingRate = new Vector(0, 0);

                    // create a command and put in queue to test connection
                    var init = new SkyGetMotorCardVersion(SkyQueue.NewId, AxisId.Axis1);
                    _ = (string)SkyQueue.GetCommandResult(init).Result;
                    if (!init.Successful && init.Exception != null)
                    {
                        SkyErrorHandler(init.Exception);
                        return false;
                    }

                    // defaults
                    if (SkySettings.Mount == MountType.SkyWatcher)
                    {
                        SkyTasks(MountTaskName.AllowAdvancedCommandSet);
                    }
                    SkyTasks(MountTaskName.LoadDefaults);
                    SkyTasks(MountTaskName.StepsPerRevolution);
                    SkyTasks(MountTaskName.StepsWormPerRevolution);
                    SkyTasks(MountTaskName.StopAxes);
                    SkyTasks(MountTaskName.Encoders);
                    SkyTasks(MountTaskName.FullCurrent);
                    SkyTasks(MountTaskName.SetSt4Guiderate);
                    SkyTasks(MountTaskName.SetSouthernHemisphere);
                    SkyTasks(MountTaskName.MountName);
                    SkyTasks(MountTaskName.MountVersion);
                    SkyTasks(MountTaskName.StepTimeFreq);
                    SkyTasks(MountTaskName.CanPpec);
                    SkyTasks(MountTaskName.CanPolarLed);
                    SkyTasks(MountTaskName.PolarLedLevel);
                    SkyTasks(MountTaskName.CanHomeSensor);
                    SkyTasks(MountTaskName.DecPulseToGoTo);
                    SkyTasks(MountTaskName.AlternatingPpec);
                    SkyTasks(MountTaskName.MinPulseDec);
                    SkyTasks(MountTaskName.MinPulseRa);
                    SkyTasks(MountTaskName.GetFactorStep);
                    SkyTasks(MountTaskName.Capabilities);
                    SkyTasks(MountTaskName.CanAdvancedCmdSupport);
                    if (CanPPec) SkyTasks(MountTaskName.Pec);

                    raWormTeeth = (int)(StepsPerRevolution[0] / StepsWormPerRevolution[0]);
                    decWormTeeth = (int)(StepsPerRevolution[1] / StepsWormPerRevolution[1]);
                    WormTeethCount = new[] { raWormTeeth, decWormTeeth };
                    PecBinSteps = StepsPerRevolution[0] / (WormTeethCount[0] * 1.0) / PecBinCount;

                    CalcCustomTrackingOffset();  //generates rates for the custom gearing offsets

                    //log current positions
                    var steps = GetRawSteps();
                    monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"GetSteps:{steps[0]}|{steps[1]}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    // checks if the mount is close enough to home position to set default position. If not use the positions from the mount
                    while (rawPositions == null)
                    {
                        if (counter > 5)
                        {
                            _ = new SkySetAxisPosition(0, AxisId.Axis1, positions[0]);
                            _ = new SkySetAxisPosition(0, AxisId.Axis2, positions[1]);
                            positionsSet = true;
                            monitorItem = new MonitorEntry
                            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Counter exceeded:{positions[0]}|{positions[1]}" };
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        }
                        counter++;

                        //get positions and log them
                        rawPositions = GetRawDegrees();
                        msg = rawPositions != null ? $"GetDegrees|{rawPositions[0]}|{rawPositions[1]}" : $"NULL";
                        monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                        MonitorLog.LogToMonitor(monitorItem);

                        //if an error getting positions then stay in while loop and try again
                        if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1]))
                        {
                            rawPositions = null;
                            continue;
                        }

                        //is mount parked, if so set to the default position
                        if (AtPark)
                        {
                            _ = new SkySetAxisPosition(0, AxisId.Axis1, positions[0]);
                            _ = new SkySetAxisPosition(0, AxisId.Axis2, positions[1]);
                            positionsSet = true;
                            break;
                        }

                        //was mount powered and at 0,0  are both axes close to home?  if not then don't change current mount positions 
                        if (!rawPositions[0].IsBetween(-.1, .1) || !rawPositions[1].IsBetween(-.1, .1)) { continue; }

                        //Mount is close to home 0,0 so set the default position
                        _ = new SkySetAxisPosition(0, AxisId.Axis1, positions[0]);
                        _ = new SkySetAxisPosition(0, AxisId.Axis2, positions[1]);
                        positionsSet = true;

                    }

                    // Update AlignmentModel settings.
                    ConnectAlignmentModel();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            msg = positionsSet ? $"SetPositions|{positions[0]}|{positions[1]}" : $"PositionsNotSet";
            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
            MonitorLog.LogToMonitor(monitorItem);

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"MountAxes|{_mountAxes.X}|{_mountAxes.Y}|Actual|{ActualAxisX}|{ActualAxisY}" };
            MonitorLog.LogToMonitor(monitorItem);

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"StepsPerRevolution|{StepsPerRevolution[0]}|{StepsPerRevolution[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            //Load Pec Files
            var pecmsg = string.Empty;
            PecOn = SkySettings.PecOn;
            if (File.Exists(SkySettings.PecWormFile))
            {
                LoadPecFile(SkySettings.PecWormFile);
                pecmsg += SkySettings.PecWormFile;
            }

            if (File.Exists(SkySettings.Pec360File))
            {
                LoadPecFile(SkySettings.Pec360File);
                pecmsg += ", " + SkySettings.PecWormFile;
            }

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Pec: {pecmsg}" };
            MonitorLog.LogToMonitor(monitorItem);

            return true;
        }

        /// <summary>
        /// Start connection, queues, and events
        /// </summary>
        private static void MountStart()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.Mount}" };
            MonitorLog.LogToMonitor(monitorItem);

            // setup server defaults, connect serial port, start queues
            Defaults();
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    MountQueue.Start();
                    if (MountQueue.IsRunning) { ConnectAlignmentModel(); }
                    else
                    { throw new Exception("Failed to start simulator queue"); }

                    break;
                case MountType.SkyWatcher:
                    // open serial port
                    SkySystem.ConnectSerial = false;
                    SkySystem.ConnectSerial = true;
                    if (!SkySystem.ConnectSerial)
                    {
                        throw new SkyServerException(ErrorCode.ErrSerialFailed,
                            $"Serial Failed COM{SkySettings.ComPort}");
                    }
                    // Start up, pass custom mount gearing if needed
                    var Custom360Steps = new[] { 0, 0 };
                    var CustomWormSteps = new[] { 0.0, 0.0 };
                    if (SkySettings.CustomGearing)
                    {
                        Custom360Steps = new[] { SkySettings.CustomRa360Steps, SkySettings.CustomDec360Steps };
                        CustomWormSteps = new[] { (double)SkySettings.CustomRa360Steps / SkySettings.CustomRaWormTeeth, (double)SkySettings.CustomDec360Steps / SkySettings.CustomDecWormTeeth };
                    }

                    SkyQueue.Start(SkySystem.Serial, Custom360Steps, CustomWormSteps);
                    if (!SkyQueue.IsRunning)
                    {
                        throw new SkyServerException(ErrorCode.ErrMount, "Failed to start sky queue");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Run mount default commands and start the UI updates
            if (MountConnect())
            {
                // start with a stop
                AxesStopValidate();

                // Event to get mount positions and update UI
                _mediaTimer = new MediaTimer { Period = SkySettings.DisplayInterval, Resolution = 5 };
                _mediaTimer.Tick += UpdateServerEvent;
                _mediaTimer.Start();
            }
            else
            {
                MountStop();
            }
        }

        /// <summary>
        /// Stop queues and events
        /// </summary>
        private static void MountStop()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.Mount}" };
            MonitorLog.LogToMonitor(monitorItem);

            AxesStopValidate();
            if (_mediaTimer != null) { _mediaTimer.Tick -= UpdateServerEvent; }
            _mediaTimer?.Stop();
            _mediaTimer?.Dispose();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 1000) { } //change
            sw.Stop();

            if (MountQueue.IsRunning) { MountQueue.Stop(); }

            if (!SkyQueue.IsRunning) return;
            SkyQueue.Stop();
            SkySystem.ConnectSerial = false;
        }

        /// <summary>
        /// Pulse commands
        /// </summary>
        /// <param name="direction">GuideDirections</param>
        /// <param name="duration">in milliseconds</param>
        public static void PulseGuide(GuideDirections direction, int duration)
        {
            if (!IsMountRunning) { throw new Exception("Mount not running"); }

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{direction}|{duration}" };
            MonitorLog.LogToMonitor(monitorItem);

            dynamic _;
            switch (direction)
            {
                case GuideDirections.guideNorth:
                case GuideDirections.guideSouth:
                    if (duration == 0)
                    {
                        IsPulseGuidingDec = false;
                        return;
                    }
                    IsPulseGuidingDec = true;
                    HcResetPrevMove(MountAxis.Dec);
                    var decGuideRate = Math.Abs(GuideRateDec);
                    if (SideOfPier == PierSide.pierEast)
                    {
                        if (direction == GuideDirections.guideNorth) { decGuideRate = -decGuideRate; }
                    }
                    else
                    {
                        if (direction == GuideDirections.guideSouth) { decGuideRate = -decGuideRate; }
                    }

                    // Direction switched add backlash compensation
                    var decbacklashamount = 0;
                    if (direction != LastDecDirection) decbacklashamount = SkySettings.DecBacklash;
                    LastDecDirection = direction;

                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            decGuideRate = decGuideRate > 0 ? -Math.Abs(decGuideRate) : Math.Abs(decGuideRate);
                            _ = new CmdAxisPulse(0, Axis.Axis2, decGuideRate, duration);
                            break;
                        case MountType.SkyWatcher:
                            _ = new SkyAxisPulse(0, AxisId.Axis2, decGuideRate, duration, decbacklashamount);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case GuideDirections.guideEast:
                case GuideDirections.guideWest:
                    if (duration == 0)
                    {
                        IsPulseGuidingRa = false;
                        return;
                    }
                    IsPulseGuidingRa = true;
                    HcResetPrevMove(MountAxis.Ra);
                    var raGuideRate = Math.Abs(GuideRateRa);
                    if (SouthernHemisphere)
                    {
                        if (direction == GuideDirections.guideWest) { raGuideRate = -raGuideRate; }
                    }
                    else
                    {
                        if (direction == GuideDirections.guideEast) { raGuideRate = -raGuideRate; }
                    }

                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            _ = new CmdAxisPulse(0, Axis.Axis1, raGuideRate, duration);
                            break;
                        case MountType.SkyWatcher:
                            _ = new SkyAxisPulse(0, AxisId.Axis1, raGuideRate, duration);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>
        /// Reset positions for the axes.
        /// </summary>
        /// <param name="parkPosition">ParkPosition or Null for home</param>
        public static void ReSyncAxes(ParkPosition parkPosition = null)
        {
            if (!IsMountRunning) { return; }
            if (Tracking) { Tracking = false; }
            if (IsSlewing)
            {
                StopAxes();
                return;
            }

            //set to home position
            double[] position = { _homeAxes.X, _homeAxes.Y };
            var name = "home";

            //set to park position
            if (parkPosition != null)
            {
                position = Axes.AxesAppToMount(new[] { parkPosition.X, parkPosition.Y });
                name = parkPosition.Name;
            }

            //log
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{name}|{position[0]}|{position[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            switch (SkySettings.Mount) // mount type check
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    _ = new CmdAxisToDegrees(0, Axis.Axis1, position[0]);
                    _ = new CmdAxisToDegrees(0, Axis.Axis2, position[1]);
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    _ = new SkySetAxisPosition(0, AxisId.Axis1, position[0]);
                    _ = new SkySetAxisPosition(0, AxisId.Axis2, position[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //all good, go ahead and set dropdown to the park position and park
            if (parkPosition != null)
            {
                ParkSelected = parkPosition;
                GoToPark();
            }

            //reset any hc moves
            HcResetPrevMove(MountAxis.Ra);
            HcResetPrevMove(MountAxis.Dec);
        }

        /// <summary>
        /// Sets up offsets from the selected tracking rate
        /// </summary>
        internal static void SetGuideRates()
        {
            var rate = CurrentTrackingRate();
            _guideRate.X = rate * SkySettings.GuideRateOffsetX;
            _guideRate.Y = rate * SkySettings.GuideRateOffsetY;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_guideRate.X * 3600}|{_guideRate.Y * 3600}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Create new park position from internal position
        /// </summary>
        public static void SetParkAxis(string name)
        {
            if (string.IsNullOrEmpty(name)) { name = "Empty"; }

            // convert current position
            var park = Axes.MountAxis2Mount();
            if (park == null) { return; }

            var p = new ParkPosition { Name = name, X = park[0], Y = park[1] };
            ParkSelected = p;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{name}|{park[0]}|{park[1]}|{MountAxisX}|{MountAxisY}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            Synthesizer.Speak(Application.Current.Resources["vceParkSet"].ToString());
        }

        /// <summary>
        /// Create park position, expects MountAxis2Mount already done
        /// </summary>
        private static void SetParkAxis(string name, double x, double y)
        {
            if (string.IsNullOrEmpty(name)) name = "Empty";

            var p = new ParkPosition { Name = name, X = x, Y = y };
            ParkSelected = p;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{name}|{x}|{y}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Sets speeds for hand controller and slews in simulator
        /// </summary>
        internal static void SetSlewRates(double maxRate)
        {
            // Sky Speeds
            SlewSpeedOne = Math.Round(maxRate * 0.0034, 3);
            SlewSpeedTwo = Math.Round(maxRate * 0.0068, 3);
            SlewSpeedThree = Math.Round(maxRate * 0.047, 3);
            SlewSpeedFour = Math.Round(maxRate * 0.068, 3);
            SlewSpeedFive = Math.Round(maxRate * 0.2, 3);
            SlewSpeedSix = Math.Round(maxRate * 0.4, 3);
            SlewSpeedSeven = Math.Round(maxRate * 0.8, 3);
            SlewSpeedEight = Math.Round(maxRate * 1.0, 3);

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message =
                    $"{SlewSpeedOne}|{SlewSpeedTwo}|{SlewSpeedThree}|{SlewSpeedFour}|{SlewSpeedFive}|{SlewSpeedSix}|{SlewSpeedSeven}|{SlewSpeedEight}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Send tracking on or off to mount
        /// </summary>
        private static void SetTracking()
        {
            if (!IsMountRunning) { return; }

            double rateChange = 0;
            switch (_trackingMode)
            {
                case TrackingMode.Off:
                    break;
                case TrackingMode.AltAz:
                    rateChange = CurrentTrackingRate();
                    break;
                case TrackingMode.EqN:
                    rateChange = CurrentTrackingRate();
                    break;
                case TrackingMode.EqS:
                    rateChange = -CurrentTrackingRate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            dynamic _;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    _ = new CmdAxisTracking(0, Axis.Axis1, rateChange);
                    break;
                case MountType.SkyWatcher:
                    SkyTrackingRate.X = rateChange;
                    var rate = GetSlewRate();
                    _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // don't log if pec is on
            if (PecOn) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_trackingMode}|{rateChange * 3600}|{PecBinNow}|{SkyTrackingOffset[0]}|{SkyTrackingOffset[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Shuts down everything and exists
        /// </summary>
        public static void ShutdownServer()
        {
            IsMountRunning = false;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "MainWindow Closing" };
            MonitorLog.LogToMonitor(monitorItem);

            for (var intCounter = Application.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
            {
                if (Application.Current.Windows[intCounter] != null)
                {
                    Application.Current.Windows[intCounter].Close();
                }
            }

            // if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();

        }

        /// <summary>
        /// Gets the side of pier using the right ascension, assuming it depends on the
        /// hour angle only.  Used for Destination side of Pier, NOT to determine the mount
        /// pointing state
        /// </summary>
        /// <param name="rightAscension">The right ascension.</param>
        /// <returns></returns>
        public static PierSide SideOfPierRaDec(double rightAscension)
        {
            if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
            {
                return PierSide.pierUnknown;
            }

            var ha = Coordinate.Ra2Ha12(rightAscension, SiderealTime);
            PierSide sideOfPier;
            if (ha < 0.0 && ha >= -12.0) { sideOfPier = PierSide.pierWest; }
            else if (ha >= 0.0 && ha <= 12.0) { sideOfPier = PierSide.pierEast; }
            else { sideOfPier = PierSide.pierUnknown; }

            return sideOfPier;
        }

        /// <summary>
        /// Gets the side of pier using the right ascension, assuming it depends on the
        /// hour angle and GSS ha limit.  Used for Destination side of Pier, NOT to determine the mount
        /// pointing state
        /// </summary>
        /// <param name="rightAscension">The right ascension.</param>
        /// <returns></returns>
        public static PierSide SideOfPierRaDec1(double rightAscension)
        {
            if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
            {
                return PierSide.pierUnknown;
            }

            var limit = SkySettings.HourAngleLimit / 15;
            var ha = Coordinate.Ra2Ha12(rightAscension, SiderealTime);
            PierSide sideOfPier;

            if (ha < (0.0 + limit) && ha >= -12.0) { sideOfPier = PierSide.pierWest; }
            else if (ha >= (0.0 + limit) && ha <= 12.0) { sideOfPier = PierSide.pierEast; }
            else { sideOfPier = PierSide.pierUnknown; }

            return sideOfPier;
        }

        /// <summary>
        /// Determine actual side of pier for a ra/dec coordinate
        /// </summary>
        /// <remarks>ra/dec must already be converted using Transforms.CordTypeToInternal</remarks>
        /// <param name="RightAscension"></param>
        /// <param name="Declination"></param>
        /// <returns></returns>
        public static PierSide SideOfPierActual(double RightAscension, double Declination)
        {
            if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
            {
                return PierSide.pierUnknown;
            }

            var flipreq = Axes.IsFlipRequired(new[] { RightAscension, Declination });

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Ra:{RightAscension}|Dec:{Declination}|Flip:{flipreq}|SoP:{SideOfPier}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            switch (SideOfPier)
            {
                case PierSide.pierEast:
                    return flipreq ? PierSide.pierWest : PierSide.pierEast;
                case PierSide.pierWest:
                    return flipreq ? PierSide.pierEast : PierSide.pierWest;
                case PierSide.pierUnknown:
                    return PierSide.pierUnknown;
                default:
                    return PierSide.pierUnknown;
            }
        }

        /// <summary>
        /// Starts slew with ra/dec coordinates
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        public static void SlewRaDec(double rightAscension, double declination)
        {
            // convert RA/Dec to axis
            var a = Axes.RaDecToAxesXY(new[] { rightAscension, declination });

            _targetAxes = new Vector(a[0], a[1]);
            SlewMount(_targetAxes, SlewType.SlewRaDec);
        }

        /// <summary>
        /// Within the meridian limits will check for closest slew
        /// </summary>
        /// <param name="position"></param>
        /// <returns>axis position that is closest</returns>
        public static double[] CheckAlternatePosition(double[] position)
        {
            // See if the target is within limits
            if (!IsWithinMeridianLimits(position)) { return null; }
            var alt = Axes.GetAltAxisPosition(position);

            var cl = ChooseClosestPosition(ActualAxisX, position, alt);
            if (cl != "b") { return null; }
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    if (SouthernHemisphere) { alt[0] = 180 - alt[0]; }
                    break;
                case MountType.SkyWatcher:
                    if (SouthernHemisphere)
                    {
                        alt[0] = 180 - alt[0];
                    }
                    else
                    {
                        alt[1] = 180 - alt[1];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return alt;
        }

        /// <summary>
        /// Calculates which pair of axis positions is closer to a given position
        /// </summary>
        /// <param name="position">X axis position</param>
        /// <param name="a">First pair of positions</param>
        /// <param name="b">Seconds pair of positions</param>
        /// <returns>a or b as string</returns>
        public static string ChooseClosestPosition(double position, IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            var val1 = Math.Abs(a[0] - position);
            var val2 = Math.Abs(b[0] - position);
            if (!(Math.Abs(val1 - val2) > 0)) { return "a"; }
            return val1 < val2 ? "a" : "b";
        }

        /// <summary>
        /// Calculates if axis position is within the defined meridian limits
        /// </summary>
        /// <param name="position">X axis position of mount</param>
        /// <returns>True if within limits otherwise false</returns>
        public static bool IsWithinMeridianLimits(IReadOnlyList<double> position)
        {
            return position[0] > -SkySettings.HourAngleLimit && position[0] < SkySettings.HourAngleLimit ||
                   position[0] > 180 - SkySettings.HourAngleLimit && position[0] < 180 + SkySettings.HourAngleLimit;
        }

        /// <summary>
        /// Starts slew with alt/az coordinates
        /// </summary>
        /// <param name="altitude"></param>
        /// <param name="azimuth"></param>
        public static void SlewAltAz(double altitude, double azimuth)
        {
            SlewAltAz(new Vector(azimuth, altitude));
        }

        /// <summary>
        /// Starts slew with alt/az coordinates as vector type
        /// </summary>
        /// <param name="targetAltAzm"></param>
        private static void SlewAltAz(Vector targetAltAzm)
        {
            var yx = Axes.AltAzToAxesYX(new[] { targetAltAzm.Y, targetAltAzm.X });
            var target = new Vector(yx[1], yx[0]);

            if (target.LengthSquared > 0)
            {
                SlewMount(target, SlewType.SlewAltAz);
            }
        }

        /// <summary>
        /// Starts slew with primary/seconday internal coordinates, not mount positions
        /// </summary>
        /// <param name="primaryAxis"></param>
        /// <param name="secondaryAxis"></param>
        /// <param name="slewState"></param>
        public static void SlewAxes(double primaryAxis, double secondaryAxis, SlewType slewState)
        {
            SlewMount(new Vector(primaryAxis, secondaryAxis), slewState);
        }

        /// <summary>
        /// Start physical mount moves positions in internal degrees.
        /// </summary>
        /// <param name="targetPosition">The position.</param>
        /// <param name="slewState"></param>
        private static void SlewMount(Vector targetPosition, SlewType slewState)
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|{ActualAxisY}|to|{targetPosition.X}|{targetPosition.Y}|SlewType|{slewState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            //todo might be a good place to reject for axis limits

            HcResetPrevMove(MountAxis.Ra);
            HcResetPrevMove(MountAxis.Dec);

            _targetAxes = targetPosition;
            AtPark = false;
            SpeakSlewStart(slewState);
            GoToAsync(new[] { _targetAxes.X, _targetAxes.Y }, slewState);
        }

        /// <summary>
        /// Stop Axes in a normal motion
        /// </summary>
        public static void StopAxes()
        {
            if (!IsMountRunning) { return; }

            AutoHomeStop = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{SlewState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            _rateAxes = new Vector();
            _rateRaDec = new Vector();
            SlewState = SlewType.SlewNone;

            if (!AxesStopValidate())
            {
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        SimTasks(MountTaskName.StopAxes);
                        break;
                    case MountType.SkyWatcher:
                        SkyTasks(MountTaskName.StopAxes);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Tracking = false;
            Synthesizer.Speak(Application.Current.Resources["vceStop"].ToString());
        }

        /// <summary>
        /// Sync using az/alt
        /// </summary>
        /// <param name="targetAzimuth"></param>
        /// <param name="targetAltitude"></param>
        public static void SyncToAltAzm(double targetAzimuth, double targetAltitude)
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{targetAzimuth}|{targetAltitude}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var trackingstate = Tracking;

            _altAzSync = new Vector(targetAzimuth, targetAltitude);
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    SimTasks(MountTaskName.SyncAltAz);
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    SkyTasks(MountTaskName.SyncAltAz);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (trackingstate)
            {
                Tracking = false;
                Tracking = true;
            }

            Synthesizer.Speak(Application.Current.Resources["vceSyncAz"].ToString());
        }

        /// <summary>
        /// Sync using ra/dec
        /// </summary>
        public static void SyncToTargetRaDec()
        {
            if (!IsMountRunning) { return; }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $" {TargetRa}|{TargetDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var trackingstate = Tracking;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    if (AlignmentModel.IsAlignmentOn)
                    {
                        AddAlignmentPoint();
                    }
                    else
                    {
                        SimTasks(MountTaskName.SyncTarget);
                    }
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    if (AlignmentModel.IsAlignmentOn)
                    {
                        AddAlignmentPoint();
                    }
                    else
                    {
                        SkyTasks(MountTaskName.SyncTarget);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            if (trackingstate)
            {
                Tracking = false;
                Tracking = true;
            }

            Synthesizer.Speak(Application.Current.Resources["vceSyncCoords"].ToString());
        }

        /// <summary>
        /// Check if sync is too far from RaDec position
        /// </summary>
        /// <param name="ra"></param>
        /// <param name="dec"></param>
        /// <returns>False is out of limit</returns>
        public static bool CheckRaDecSyncLimit(double ra, double dec)
        {
            if (!SkySettings.SyncLimitOn) { return true; }
            if (SkySettings.NoSyncPastMeridian) { return false; } // add more checks later if needed

            //convert ra dec to mount positions
            var xy = Axes.RaDecToAxesXY(new[] { ra, dec });
            var target = GetSyncedAxes(Axes.AxesAppToMount(xy));

            //convert current position to mount position
            var current = Axes.AxesMountToApp(new[] { _mountAxisX, _mountAxisY });

            //compare ra dec to current position
            var a = Math.Abs(target[0]) - Math.Abs(current[0]);
            var b = Math.Abs(target[1]) - Math.Abs(current[1]);
            var ret = !(Math.Abs(a) > SkySettings.SyncLimit || Math.Abs(b) > SkySettings.SyncLimit);
            if (ret) return true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Warning,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{xy[0]}|{xy[1]}|{target[0]}|{target[1]}|{current[0]}|{current[1]}|{SkySettings.SyncLimit}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return false;

        }

        /// <summary>
        /// Check if sync is too far from AltAz position
        /// </summary>
        /// <param name="alt"></param>
        /// <param name="az"></param>
        /// <returns>False is out of limit</returns>
        public static bool CheckAltAzSyncLimit(double alt, double az)
        {
            if (!SkySettings.SyncLimitOn) { return true; }
            if (SkySettings.NoSyncPastMeridian) { return false; } // add more checks later if needed

            //convert ra dec to mount positions
            var yx = Axes.AltAzToAxesYX(new[] { alt, az });
            var target = GetSyncedAxes(Axes.AxesAppToMount(new[] { yx[1], yx[0] }));

            //convert current position to mount position
            var current = Axes.AxesMountToApp(new[] { _mountAxisX, _mountAxisY });

            //compare ra dec to current position
            var a = Math.Abs(target[0]) - Math.Abs(current[0]);
            var b = Math.Abs(target[1]) - Math.Abs(current[1]);

            var ret = !(Math.Abs(a) > SkySettings.SyncLimit || Math.Abs(b) > SkySettings.SyncLimit);

            if (ret) return true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Warning,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{yx[1]}|{yx[0]}|{target[0]}|{target[1]}|{current[0]}|{current[1]}|{SkySettings.SyncLimit}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return false;
        }

        #endregion

        #region Alignment

        private static void ConnectAlignmentModel()
        {
            AlignmentModel.Connect(_homeAxes.X, _homeAxes.Y, AlignmentSettings.ClearModelOnStartup);
        }

        private static bool _alignmentShow;
        /// <summary>
        /// sets up bool to load a test tab
        /// </summary>
        public static bool AlignmentShow
        {
            get => _alignmentShow;
            set
            {
                _alignmentShow = value;
                if (_alignmentShow && AlignmentSettings.IsAlignmentOn)
                {
                    AlignmentModel.IsAlignmentOn = true;
                }
                OnStaticPropertyChanged();
            }
        }

        private static void AlignmentModel_Notification(object sender, NotificationEventArgs e)
        {
            // Luckily the NotificationType enum and mimics MonitorType enum.
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Alignment,
                Type = (MonitorType)e.NotificationType,
                Method = e.Method,
                Thread = e.Thread,
                Message = e.Message
            };
            MonitorLog.LogToMonitor(monitorItem);
        }
        internal static double DegToRad(double degree) { return (degree / 180.0 * Math.PI); }
        internal static double RadToDeg(double rad) { return (rad / Math.PI * 180.0); }

        private static void AddAlignmentPoint()
        {
            // At this point:
            //      SkyServer.Steps contains the current encoder posititions.
            //      SkyServer.FactorStep contains the conversion from radians to steps
            // To get the target steps
            var xy = Axes.RaDecToAxesXY(new[] { TargetRa, TargetDec });
            var unsynced = Axes.AxesAppToMount(new[] { xy[0], xy[1] });
            var rawSteps = GetRawSteps();
            double[] synced = new double[] { ConvertStepsToDegrees(rawSteps[0], 0), ConvertStepsToDegrees(rawSteps[1], 1) };
            if (AlignmentModel.SyncToRaDec(
                unsynced,
                synced,
                DateTime.Now))
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Alignment,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Alignment point added: Unsynced axis = {unsynced[0]}/{unsynced[1]}, RA/Dec = {TargetRa}/{TargetDec}, Synched axis = {synced[0]}/{synced[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            else
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Alignment,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Alignment point added: Unsynced axis = {unsynced[0]}/{unsynced[1]}, RA/Dec = {TargetRa}/{TargetDec}, Synched axis = {synced[0]}/{synced[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double[] GetSyncedAxes(double[] unsynced)
        {
            if (AlignmentModel.IsAlignmentOn)
            {
                double[] synced = AlignmentModel.GetSyncedValue(unsynced);
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Alignment,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Mapped unsynced axis angles: {unsynced[0]}/{unsynced[1]} to {synced[0]}/{synced[1]}"
                };

                return synced;
            }
            else
            {
                return unsynced;
            }
        }


        private static double[] GetUnsyncedAxes(double[] synced)
        {
            if (AlignmentModel.IsAlignmentOn)
            {
                double[] unsynced = AlignmentModel.GetUnsyncedValue(synced);
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Alignment,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Mapped synced axis angles: {synced[0]}/{synced[1]} to {unsynced[0]}/{unsynced[1]}"
                };

                return unsynced;
            }
            else
            {
                return synced;
            }
        }
        #endregion

        #region Server Items
        private static void PropertyChangedSkySettings(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Latitude":
                    AlignmentModel.SiteLatitude = SkySettings.Latitude;
                    break;
                case "Longitude":
                    AlignmentModel.SiteLongitude = SkySettings.Longitude;
                    break;
                case "Elevation":
                    AlignmentModel.SiteElevation = SkySettings.Elevation;
                    break;
            }
        }
        private static void PropertyChangedAlignmentSettings(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsAlignmentOn":
                    AlignmentModel.IsAlignmentOn = AlignmentSettings.IsAlignmentOn;
                    break;
                case "ProximityLimit":
                    AlignmentModel.ProximityLimit = AlignmentSettings.ProximityLimit;
                    break;
                case "AlignmentBehaviour":
                    AlignmentModel.AlignmentBehaviour = AlignmentSettings.AlignmentBehaviour;
                    break;
                case "ActivePoints":
                    AlignmentModel.ActivePoints = AlignmentSettings.ActivePoints;
                    break;
                case "ThreePointAlgorithm":
                    AlignmentModel.ThreePointAlgorithm = AlignmentSettings.ThreePointAlgorithm;
                    break;
            }
        }
        private static void PropertyChangedSkyQueue(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsPulseGuidingRa":
                    IsPulseGuidingRa = SkyQueue.IsPulseGuidingRa;
                    break;
                case "IsPulseGuidingDec":
                    IsPulseGuidingDec = SkyQueue.IsPulseGuidingDec;
                    break;
            }
        }
        private static void PropertyChangedMountQueue(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsPulseGuidingRa":
                    IsPulseGuidingRa = MountQueue.IsPulseGuidingRa;
                    break;
                case "IsPulseGuidingDec":
                    IsPulseGuidingDec = MountQueue.IsPulseGuidingDec;
                    break;
            }
        }

        /// <summary>
        /// initial defaults
        /// </summary>
        private static void Defaults()
        {
            // default set for dec goto pulse
            LastDecDirection = GuideDirections.guideEast;

            // default snap port
            SnapPort1 = false;
            SnapPort2 = false;
            SnapPort1Result = false;
            SnapPort2Result = false;

            FactorStep = new[] { 0.0, 0.0 };
            StepsPerRevolution = new long[] { 12960000, 12960000 };
            WormTeethCount = new[] { 180, 180 };
            StepsWormPerRevolution = new[] { StepsPerRevolution[0] / (double)WormTeethCount[0], StepsPerRevolution[1] / (double)WormTeethCount[1] };
            SlewSettleTime = 0;

            //Pec
            PecBinCount = 100;
            Pec360Master = null;
            PecWormMaster = null;

            // reset any rates so slewing doesn't start
            _rateRaDec = new Vector(0, 0);
            _rateAxes = new Vector(0, 0);
            SlewState = SlewType.SlewNone;

            // invalid any target positions
            _targetRaDec = new Vector(double.NaN, double.NaN);

            //default hand control and slew rates
            SetSlewRates(SkySettings.MaxSlewRate);

            // Allows driver movements commands to process
            AsComOn = true;

            // default guide rates
            SetGuideRates();

            Tracking = false;
            TrackingSpeak = true;

            StepsTimeFreq = new long[2];

        }

        /// <summary>
        /// called from the setter property.  Used to update UI elements.  property name is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Time to open my mouth
        /// </summary>
        /// <param name="slewType"></param>
        private static void SpeakSlewStart(SlewType slewType)
        {
            switch (slewType)
            {
                case SlewType.SlewNone:
                    break;
                case SlewType.SlewSettle:
                    break;
                case SlewType.SlewMoveAxis:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewing"].ToString());
                    break;
                case SlewType.SlewRaDec:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewCoords"].ToString());
                    break;
                case SlewType.SlewAltAz:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewCoords"].ToString());
                    break;
                case SlewType.SlewPark:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewPark"].ToString());
                    break;
                case SlewType.SlewHome:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewHome"].ToString());
                    break;
                case SlewType.SlewHandpad:
                    break;
                case SlewType.SlewComplete:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slewType), slewType, null);
            }
        }

        /// <summary>
        /// Time to open my mouth
        /// </summary>
        /// <param name="slewType"></param>
        private static void SpeakSlewEnd(SlewType slewType)
        {
            switch (slewType)
            {
                case SlewType.SlewNone:
                    break;
                case SlewType.SlewSettle:
                    break;
                case SlewType.SlewMoveAxis:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                case SlewType.SlewRaDec:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                case SlewType.SlewAltAz:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                case SlewType.SlewPark:
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                case SlewType.SlewHome:
                    Synthesizer.Speak(Application.Current.Resources["vceHome"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                case SlewType.SlewHandpad:
                    break;
                case SlewType.SlewComplete:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    Synthesizer.Beep(BeepType.SlewComplete);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slewType), slewType, null);
            }
        }

        /// <summary>
        /// Update the Server and UI from the axis positions
        /// </summary>
        private static void UpdateServerEvent(object sender, EventArgs e)
        {
            var hasLock = false;
            try
            {
                // Stops the overrun of previous event not ended before next one starts
                Monitor.TryEnter(_timerLock, ref hasLock);
                if (!hasLock)
                {
                    TimerOverruns++;
                    return;
                }

                // the time is?
                SiderealTime = GetLocalSiderealTime();

                // Get raw positions, some are non-responses from mount and are returned as NaN
                var rawSteps = GetRawSteps(); //var rawPositions = GetRawDegrees();
                if (rawSteps == null) { return; }
                if (double.IsNaN(rawSteps[0]) || double.IsNaN(rawSteps[1])) { return; }


                Steps = rawSteps;

                //Implement Pec
                PecCheck();

                //Convert Positions to degrees
                // double[] rawPositions = { ConvertStepsToDegrees(rawSteps[0], 0), ConvertStepsToDegrees(rawSteps[1], 1) };
                double[] rawPositions = GetUnsyncedAxes(new double[] { ConvertStepsToDegrees(rawSteps[0], 0), ConvertStepsToDegrees(rawSteps[1], 1) });


                // UI diagnostics in degrees
                ActualAxisX = rawPositions[0];
                ActualAxisY = rawPositions[1];

                // convert positions to local
                var axes = Axes.AxesMountToApp(rawPositions);

                // local to track positions
                _mountAxes.X = axes[0];
                _mountAxes.Y = axes[1];

                // UI diagnostics
                MountAxisX = axes[0];
                MountAxisY = axes[1];

                // Calculate mount Alt/Az
                var altaz = Axes.AxesXYToAzAlt(axes);
                Azimuth = altaz[0];
                Altitude = altaz[1];

                // Calculate mount Ra/Dec
                var radec = Axes.AxesXYToRaDec(axes);
                RightAscension = radec[0];
                Declination = radec[1];

                Lha = Coordinate.Ra2Ha12(RightAscensionXForm, SiderealTime);

                // Track slewing state
                CheckSlewState();

                //used for warning light
                CheckAxisLimits();

                // reset spiral if moved too far
                CheckSpiralLimit();

                // Update UI 
                CheckPecTraining();
                IsHome = AtHome;
                IsSideOfPier = SideOfPier;

                // Event interval time set for UI performance
                _mediaTimer.Period = SkySettings.DisplayInterval;
            }
            catch (Exception ex)
            {
                SkyErrorHandler(ex);
            }
            finally
            {
                if (hasLock) { Monitor.Exit(_timerLock); }
            }
        }

        private static double GetLocalSiderealTime()
        {
            return GetLocalSiderealTime(HiResDateTime.UtcNow);
        }

        private static double GetLocalSiderealTime(DateTime utcNow)
        {
            var gsjd = JDate.Ole2Jd(utcNow);
            return Time.Lst(JDate.Epoch2000Days(), gsjd, false, SkySettings.Longitude);
        }
        #endregion

        #region PEC & PPEC

        private static bool _pecShow;
        /// <summary>
        /// sets up bool to load a test tab
        /// </summary>
        public static bool PecShow
        {
            get => _pecShow;
            set
            {
                _pecShow = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Can mount do PPec
        /// </summary>
        public static bool CanPPec
        {
            get => _canPPec;
            private set
            {
                if (_canPPec == value) return;
                _canPPec = value;
                OnStaticPropertyChanged();
            }
        }

        public static bool CanPolarLed
        {
            get => _canPolarLed;
            private set
            {
                if (_canPolarLed == value) { return; }
                _canPolarLed = value;
                OnStaticPropertyChanged();
            }
        }

        public static bool CanAdvancedCmdSupport
        {
            get => _canAdvancedCmdSupport;
            private set
            {
                if (_canAdvancedCmdSupport == value) { return; }
                _canAdvancedCmdSupport = value;
                OnStaticPropertyChanged();
            }
        }


        /// <summary>
        /// Turn on/off mount PPec
        /// </summary>
        public static bool PPecOn
        {
            get => SkySettings.PPecOn;
            set
            {
                SkySettings.PPecOn = value;
                SkyTasks(MountTaskName.Pec);
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// turn on/off mount training
        /// </summary>
        private static bool _pPecTraining;
        public static bool PecTraining
        {
            get => _pPecTraining;
            set
            {
                if (PecTraining == value) return;
                _pPecTraining = value;
                Synthesizer.Speak(value ? Application.Current.Resources["vcePeckTrainOn"].ToString() : Application.Current.Resources["vcePeckTrainOff"].ToString());

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyTasks(MountTaskName.PecTraining);
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Tracks training within mount
        /// </summary>
        private static bool _pPecTrainInProgress;
        public static bool PecTrainInProgress
        {
            get => _pPecTrainInProgress;
            private set
            {
                if (_pPecTrainInProgress == value) return;
                _pPecTrainInProgress = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Pec status
        /// </summary>
        public static bool PecOn
        {
            get => SkySettings.PecOn;
            set
            {
                SkySettings.PecOn = value;
                // set back to normal tracking
                if (!value && Tracking) { SetTracking(); }
                OnStaticPropertyChanged();
            }
        }

        private static Tuple<int, double, int> _pecBinNow;
        /// <summary>
        /// Pec Currently used bin for Pec
        /// </summary>
        public static Tuple<int, double, int> PecBinNow
        {
            get => _pecBinNow;
            private set
            {
                if (Equals(_pecBinNow, value)) { return; }
                _pecBinNow = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Pec Worm count bins
        /// </summary>
        public static int PecBinCount { get; private set; }

        /// <summary>
        /// Pec size by steps
        /// </summary>
        public static double PecBinSteps { get; private set; }

        /// <summary>
        /// Pec worm mode, list that holds all pec rate factors
        /// </summary>
        public static SortedList<int, Tuple<double, int>> PecWormMaster { get; private set; }

        /// <summary>
        /// Pec 360 mode, list that holds all pec rate factors
        /// </summary>
        public static SortedList<int, Tuple<double, int>> Pec360Master { get; private set; }

        /// <summary>
        /// Pec bin list that holds subset of the mater list, used as a cache
        /// </summary>
        private static SortedList<int, Tuple<double, int>> PecBinsSubs { get; set; }

        /// <summary>
        /// pPEC Monitors the mount doing pPEC training
        /// </summary>
        private static void CheckPecTraining()
        {
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    break;
                case MountType.SkyWatcher:
                    if (!PecTraining)
                    {
                        PecTrainInProgress = false;
                        return;
                    }

                    var ppectrain = new SkyIsPPecInTrainingOn(SkyQueue.NewId);
                    if (bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(ppectrain).Result), out bool bTrain))
                    {
                        PecTraining = bTrain;
                        PecTrainInProgress = bTrain;
                        if (!bTrain && PPecOn) //restart pec
                        {
                            PPecOn = false;
                            PPecOn = true;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Pec Implement
        /// </summary>
        private static void PecCheck()
        {
            try
            {
                if (!PecOn || !Tracking || PecBinCount < 0 || IsSlewing || !PecShow) return;

                // Get axis position and range it
                var position = (int)Range.RangeDouble(Steps[0], Convert.ToDouble(StepsPerRevolution[0]));

                // calc current bin number
                var newBinNo = (int)((position + SkySettings.PecOffSet) / PecBinSteps);

                // Holder for new bin
                Tuple<double, int> pecBin = null;

                switch (SkySettings.PecMode)
                {
                    case PecMode.PecWorm:
                        newBinNo %= 100;
                        // No bin change return
                        if (PecBinNow?.Item1 == newBinNo) return;
                        if (PecWormMaster == null || PecWormMaster?.Count == 0) { return; }
                        PecWormMaster?.TryGetValue(newBinNo, out pecBin);
                        break;
                    case PecMode.Pec360:
                        // No bin change return
                        if (PecBinNow?.Item1 == newBinNo) return;
                        if (Pec360Master == null || Pec360Master?.Count == 0) { return; }
                        if (PecBinsSubs == null) { PecBinsSubs = new SortedList<int, Tuple<double, int>>(); }
                        var count = 0;
                        // search subs for new bin
                        while (PecBinsSubs.TryGetValue(newBinNo, out pecBin) == false && count < 2)
                        {
                            // stay within limits
                            var binStart = newBinNo - 100 < 0 ? 0 : newBinNo - 100;
                            var binEnd = newBinNo + 100 > StepsPerRevolution[0] - 1  //adjust for going over max?
                                ? (int)StepsPerRevolution[0] - 1
                                : newBinNo + 100;

                            // create sub list
                            PecBinsSubs.Clear();
                            for (var i = binStart; i <= binEnd; i++)
                            {
                                var mi = Tuple.Create(0.0, 0);
                                var masterResult = Pec360Master != null && Pec360Master.TryGetValue(i, out mi);
                                if (masterResult) PecBinsSubs.Add(i, mi);
                            }

                            count++;
                        }
                        if (PecBinsSubs.Count == 0)
                        {
                            throw new Exception($"Pec sub not found");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // bin must exist or throw error
                if (pecBin == null) { throw new Exception($"Pec not found"); }

                var binNew = new Tuple<int, double, int>(newBinNo, pecBin.Item1, pecBin.Item2);

                // assign new bin info
                PecBinNow = binNew;

                // Send to mount
                SetTracking();
            }
            catch (Exception ex)
            {
                PecOn = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Mount,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                MountError = ex;
            }

        }

        /// <summary>
        /// Loads both types of pec files
        /// </summary>
        /// <param name="fileName"></param>
        public static void LoadPecFile(string fileName)
        {
            var def = new PecTrainingDefinition();
            var bins = new List<PecBinData>();

            // load file
            var lines = File.ReadAllLines(fileName);
            for (var i = 0; i < lines.Length; i += 1)
            {
                var line = lines[i];
                if (line.Length == 0) { continue; }

                switch (line[0])
                {
                    case '#':
                        var keys = line.Split('=');
                        if (keys.Length != 2) { break; }

                        switch (keys[0].Trim())
                        {
                            case "#StartTime":
                                if (DateTime.TryParseExact(keys[1].Trim(), "yyyy:MM:dd:HH:mm:ss.fff",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
                                {
                                    def.StartTime = startTime;
                                }
                                break;
                            case "#StartPosition":
                                if (double.TryParse(keys[1].Trim(), out var startPosition))
                                {
                                    def.StartPosition = startPosition;
                                }
                                break;
                            case "#EndTime":
                                if (DateTime.TryParseExact(keys[1].Trim(), "yyyy:MM:dd:HH:mm:ss.fff",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
                                {
                                    def.EndTime = endTime;
                                }
                                break;
                            case "#EndPosition":
                                if (double.TryParse(keys[1].Trim(), out var EndPosition))
                                {
                                    def.StartPosition = EndPosition;
                                }
                                break;
                            case "#Index":
                                if (int.TryParse(keys[1].Trim(), out var index))
                                {
                                    def.Index = index;
                                }
                                break;
                            case "#Cycles":
                                if (int.TryParse(keys[1].Trim(), out var cycles))
                                {
                                    def.Cycles = cycles;
                                }
                                break;
                            case "#WormPeriod":
                                if (double.TryParse(keys[1].Trim(), out var WormPeriod))
                                {
                                    def.WormPeriod = WormPeriod;
                                }
                                break;
                            case "#WormTeeth":
                                if (int.TryParse(keys[1].Trim(), out var WormTeeth))
                                {
                                    def.WormTeeth = WormTeeth;
                                }
                                break;
                            case "#WormSteps":
                                if (double.TryParse(keys[1].Trim(), out var WormSteps))
                                {
                                    def.WormSteps = WormSteps;
                                }
                                break;
                            case "#TrackingRate":
                                if (double.TryParse(keys[1].Trim(), out var TrackingRate1))
                                {
                                    def.TrackingRate = TrackingRate1;
                                }
                                break;
                            case "#PositionOffset":
                                if (double.TryParse(keys[1].Trim(), out var PositionOffset))
                                {
                                    def.PositionOffset = PositionOffset;
                                }
                                break;
                            case "#Ra":
                                if (double.TryParse(keys[1].Trim(), out var Ra))
                                {
                                    def.Ra = Ra;
                                }
                                break;
                            case "#Dec":
                                if (double.TryParse(keys[1].Trim(), out var Dec))
                                {
                                    def.Dec = Dec;
                                }
                                break;
                            case "#BinCount":
                                if (int.TryParse(keys[1].Trim(), out var BinCount))
                                {
                                    def.BinCount = BinCount;
                                }
                                break;
                            case "#BinSteps":
                                if (double.TryParse(keys[1].Trim(), out var BinSteps))
                                {
                                    def.BinSteps = BinSteps;
                                }
                                break;
                            case "#BinTime":
                                if (double.TryParse(keys[1].Trim(), out var BinTime))
                                {
                                    def.BinTime = BinTime;
                                }
                                break;
                            case "#StepsPerSec":
                                if (double.TryParse(keys[1].Trim(), out var StepsPerSec))
                                {
                                    def.StepsPerSec = StepsPerSec;
                                }
                                break;
                            case "#StepsPerRev":
                                if (double.TryParse(keys[1].Trim(), out var StepsPerRev))
                                {
                                    def.StepsPerRev = StepsPerRev;
                                }
                                break;
                            case "#InvertCapture":
                                if (bool.TryParse(keys[1].Trim(), out var InvertCapture))
                                {
                                    def.InvertCapture = InvertCapture;
                                }
                                break;
                            case "#FileName":
                                if (File.Exists(keys[1].Trim()))
                                {
                                    def.FileName = keys[1].Trim();
                                }
                                break;
                            case "#FileType":
                                if (Enum.TryParse<PecFileType>(keys[1].Trim(), true, out var FileType))
                                {
                                    def.FileType = FileType;
                                }
                                break;
                        }
                        break;
                    default:
                        var data = line.Split('|');
                        if (data.Length != 3) { break; }
                        var bin = new PecBinData();
                        if (int.TryParse(data[0].Trim(), out var binNumber))
                        {
                            bin.BinNumber = binNumber;
                        }
                        if (double.TryParse(data[1].Trim(), out var binFactor))
                        {
                            bin.BinFactor = binFactor;
                        }
                        if (int.TryParse(data[2].Trim(), out var binUpdates))
                        {
                            bin.BinUpdates = binUpdates;
                        }
                        if (binFactor > 0 && binFactor < 2) { bins.Add(bin); }
                        break;
                }
            }

            // validate
            var msg = string.Empty;
            var paramError = false;

            if (def.FileType != PecFileType.GSPecWorm && def.FileType != PecFileType.GSPec360)
            {
                paramError = true;
                msg = $"FileType {def.FileType}";
            }
            if (def.BinCount != PecBinCount)
            {
                paramError = true;
                msg = $"BinCount {def.BinCount}|{PecBinCount}";
            }
            if (Math.Abs(def.BinSteps - PecBinSteps) > 0.000000001)
            {
                paramError = true;
                msg = $"BinSteps {def.BinSteps}|{PecBinSteps}";
            }
            if (Math.Abs((long)def.StepsPerRev - StepsPerRevolution[0]) > 0.000000001)
            {
                paramError = true;
                msg = $"StepsPerRev{def.StepsPerRev}|{StepsPerRevolution[0]}";
            }
            if (def.WormTeeth != WormTeethCount[0])
            {
                paramError = true;
                msg = $"WormTeeth {def.WormTeeth}|{WormTeethCount[0]}";
            }
            switch (def.FileType)
            {
                case PecFileType.GSPecWorm:
                    if (def.BinCount == bins.Count) { break; }
                    paramError = true;
                    msg = $"BinCount {PecFileType.GSPecWorm}";
                    break;
                case PecFileType.GSPec360:
                    if (bins.Count == (int)(def.StepsPerRev / def.BinSteps)) { break; }
                    paramError = true;
                    msg = $"BinCount {PecFileType.GSPec360}";
                    break;
                case PecFileType.GSPecDebug:
                    paramError = true;
                    msg = $"BinCount {PecFileType.GSPecDebug}";
                    break;
                default:
                    paramError = true;
                    msg = $"FileType Error";
                    break;
            }

            if (paramError) { throw new Exception($"Error Loading Pec File ({msg})"); }

            bins = CleanUpBins(bins);

            // load to master
            switch (def.FileType)
            {
                case PecFileType.GSPecWorm:
                    var master = MakeWormMaster(bins);
                    UpdateWormMaster(master, PecMergeType.Replace);
                    SkySettings.PecWormFile = fileName;
                    break;
                case PecFileType.GSPec360:
                    SkySettings.Pec360File = fileName;
                    break;
                case PecFileType.GSPecDebug:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        /// <summary>
        /// Corrects missing bins
        /// </summary>
        /// <returns>new list of bins</returns>
        public static List<PecBinData> CleanUpBins(IReadOnlyCollection<PecBinData> bins)
        {
            if (bins == null) { return null; }

            // Correct for missing bins
            var sortedList = bins.OrderBy(o => o.BinNumber).ToList();
            var validBins = new List<PecBinData>();
            for (var i = sortedList[0].BinNumber; i <= sortedList[sortedList.Count - 1].BinNumber; i++)
            {
                var result = sortedList.Find(o => o.BinNumber == i);
                validBins.Add(result ?? new PecBinData { BinFactor = 1.0, BinNumber = i });
            }

            validBins = validBins.OrderBy(o => o.BinNumber).ToList();
            return validBins;
        }

        /// <summary>
        /// Creates a new master list using 100 bins
        /// </summary>
        /// <param name="bins"></param>
        /// <returns></returns>
        public static SortedList<int, Tuple<double, int>> MakeWormMaster(IReadOnlyList<PecBinData> bins)
        {
            // find the start of a worm period
            var index = 0;
            for (var i = 0; i < bins.Count; i++)
            {
                var binNo = bins[i].BinNumber * 1.0 / PecBinCount;
                var remainder = binNo % 1;
                if (remainder != 0) { continue; }
                index = i;
                break;
            }
            if (double.IsNaN(index)) { return null; }

            // create new bin set, zero based on worm start position
            var orderBins = new List<PecBinData>();
            for (var i = index; i < PecBinCount; i++)
            {
                orderBins.Add(bins[i]);
            }
            for (var i = 0; i < index; i++)
            {
                orderBins.Add(bins[i]);
            }

            // create master set of bins using train data
            var binsMaster = new SortedList<int, Tuple<double, int>>();
            for (var j = 0; j < PecBinCount; j++)
            {
                binsMaster.Add(j, new Tuple<double, int>(orderBins[j].BinFactor, 1));
            }
            return binsMaster;
        }

        /// <summary>
        /// Updates the server pec master list with applied bins
        /// </summary>
        /// <param name="mBins"></param>
        /// <param name="mergeType"></param>
        public static void UpdateWormMaster(SortedList<int, Tuple<double, int>> mBins, PecMergeType mergeType)
        {
            if (mBins == null) { return; }
            if (PecWormMaster == null) { mergeType = PecMergeType.Replace; }
            if (PecWormMaster?.Count != mBins.Count) { mergeType = PecMergeType.Replace; }

            switch (mergeType)
            {
                case PecMergeType.Replace:
                    PecWormMaster = mBins;
                    SkySettings.PecOffSet = 0; // reset offset
                    return;
                case PecMergeType.Merge:
                    var pecBins = PecWormMaster;
                    if (pecBins == null)
                    {
                        PecWormMaster = mBins;
                        SkySettings.PecOffSet = 0;
                        return;
                    }
                    for (var i = 0; i < mBins.Count; i++)
                    {
                        if (double.IsNaN(pecBins[i].Item1))
                        {
                            pecBins[i] = new Tuple<double, int>(mBins[i].Item1, 1);
                            continue;
                        }

                        var updateCount = pecBins[i].Item2;
                        if (updateCount < 1) { updateCount = 1; }
                        updateCount++;
                        var newFactor = (pecBins[i].Item1 * updateCount + mBins[i].Item1) / (updateCount + 1);
                        var newBin = new Tuple<double, int>(newFactor, updateCount);
                        pecBins[i] = newBin;

                    }
                    PecWormMaster = pecBins;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mergeType), mergeType, null);
            }
        }
        #endregion
    }
}

