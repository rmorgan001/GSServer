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
 // ReSharper disable RedundantAssignment
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.AutoHome;
using GS.Server.Helpers;
using GS.Shared;
using GS.Simulator;
using GS.SkyWatcher;
using AxisStatus = GS.Simulator.AxisStatus;


namespace GS.Server.SkyTelescope
{
    public static class SkyServer
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Fields

        private static readonly Util _util = new Util();
        private static readonly object _timerLock = new object();
        private static MediaTimer _mediatimer;

        // Slew and HC speeds
        private static double SlewSpeedOne;
        private static double SlewSpeedTwo;
        private static double SlewSpeedThree;
        private static double SlewSpeedFour;
        private static double SlewSpeedFive;
        private static double SlewSpeedSix;
        private static double SlewSpeedSeven;
        public static double SlewSpeedEight;

        private static Vector _homeAxes;
        private static Vector _mountAxes;
        private static Vector _targetAxes;
        private static Vector _altAzSync;

        #endregion Fields 

        static SkyServer()
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Loading SkyServer"
                };
                MonitorLog.LogToMonitor(monitorItem);

                // load default or user property settings
                SkySettings.Load();

                // load some things
                Defaults();

                // set local to NaN for contructor
                _mountAxes = new Vector(double.NaN, double.NaN);
            }
            catch (Exception ex)
            {
                // oops now what happened?
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                AlertState = true;
                throw;
            }
        }

        #region Property Settings 

        // UI display for actual poistions in degrees
        private static double _actualAxisX;

        public static double ActualAxisX
        {
            get => _actualAxisX;
            private set
            {
                if (Math.Abs(value - _actualAxisX) < 0.000000000000001) return;
                _actualAxisX = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _actualAxisY;

        public static double ActualAxisY
        {
            get => _actualAxisY;
            private set
            {
                if (Math.Abs(value - _actualAxisY) < 0.000000000000001) return;
                _actualAxisY = value;
                OnStaticPropertyChanged();
            }
        }

        // UI indicator for errors
        private static bool _alertState;

        public static bool AlertState
        {
            get => _alertState;
            set
            {
                if (value == AlertState) return;
                if (value) Synthesizer.Speak(Application.Current.Resources["vceError"].ToString());
                _alertState = value;
                OnStaticPropertyChanged();
            }
        }

        // Positions converted from mount
        private static Vector _altAzm;

        public static double Altitude
        {
            get => _altAzm.Y;
            private set
            {
                if (Math.Abs(value - _altAzm.Y) < 0.000000000000001) return;
                _altAzm.Y = value;
                OnStaticPropertyChanged();
            }
        }

        public static double Azimuth
        {
            get => _altAzm.X;
            private set
            {
                 if (Math.Abs(value - _altAzm.X) < 0.000000000000001) return;
                _altAzm.X = value;
                OnStaticPropertyChanged();
            }
        }

        // within degree range to trigger home
        public static bool AtHome
        {
            get
            {
                var h = new Vector(_homeAxes.X, _homeAxes.Y);
                var m = new Vector(Math.Abs(_mountAxes.X), _mountAxes.Y); // Abs is for S Hemi, hack for homepoistion
                var x = (m - h);
                var r = x.LengthSquared < 0.01;
                return r;
            }
        }

        // Is at park position
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
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        // UI progress bar for autohome 
        private static int _autoHomeProgressBar;
        public static int AutoHomeProgressBar
        {
            get => _autoHomeProgressBar;
            set
            {
                _autoHomeProgressBar = value;
                OnStaticPropertyChanged();
            }
        }

        // UI progress bar for autohome 
        private static bool _autoHomeStop;
        public static bool AutoHomeStop
        {
            get => _autoHomeStop;
            set
            {
                _autoHomeStop = value;
                OnStaticPropertyChanged();
            }
        }

        // Driver input for mount moves
        private static bool _ascomOn;

        // Allow ascom driver movements
        public static bool AscomOn
        {
            get => _ascomOn;
            set
            {
                _ascomOn = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        private static bool _canhomesensor;
        public static bool CanHomeSensor
        {
            get => _canhomesensor;
            private set
            {
                if (_canhomesensor == value) return;
                _canhomesensor = value;
                OnStaticPropertyChanged();
            }
        }

        public static bool Debug { private get; set; }

        // Positions converted from mount
        private static Vector _RaDec;
        public static double Declination
        {
            get => _RaDec.Y;
            private set
            {
                if (Math.Abs(value - _RaDec.Y) < 0.000000000000001) return;
                _RaDec.Y = value;
                DeclinationXform = value;
            }
        }
        public static double RightAscension
        {
            get => _RaDec.X;
            private set
            {
                if (Math.Abs(value - _RaDec.X) < 0.000000000000001) return;
                _RaDec.X = value;
                RightAscensionXform = value;
            }
        }

        // UI display for converted dec
        private static double _declinationXform;

        public static double DeclinationXform
        {
            get => _declinationXform;
            private set
            {
                var dec = Transforms.DecToCoordType(RightAscension, value);
                _declinationXform = dec;
                OnStaticPropertyChanged();
            }
        }

        //Move the telescope in one axis at the given rate
        private static Vector _rateAxes;

        public static double RateAxisDec
        {
            private get => _rateAxes.Y;
            set
            {
                _rateAxes.Y = Conversions.ArcSec2Deg(value);
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
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateAxes.Y}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static double RateAxisRa
        {
            private get => _rateAxes.X;
            set
            {
                _rateAxes.X = Conversions.ArcSec2Deg(value);
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
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateAxes.X}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        // Tracking rate offset from sidereal (arcseconds per second, default = 0.0) 
        private static Vector _rateRaDec;

        public static double RateDec
        {
            get => Conversions.Deg2ArcSec(_rateRaDec.Y);
            set
            {
                _rateRaDec.Y = Conversions.ArcSec2Deg(value);
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
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateRaDec.Y}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static double RateRa
        {
            get => Conversions.Deg2ArcSec(_rateRaDec.X);
            set
            {
                _rateRaDec.X = Conversions.ArcSec2Deg(value);
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
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateRaDec.X}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static Vector GuideRate;

        public static double GuideRateDec
        {
            get => GuideRate.Y;
            set => GuideRate.Y = value;
        }

        public static double GuideRateRa
        {
            get => GuideRate.X;
            set => GuideRate.X = value;
        }

        // UI Indicator
        private static bool _isHome;

        public static bool IsHome
        {
            get => _isHome;
            private set
            {
                if (value == _isHome) return;
                _isHome = value;
                OnStaticPropertyChanged();
            }
        }

        // Pulse reporting to driver
        private static bool IsPulseGuidingRa;
        private static bool IsPulseGuidingDec;

        public static bool IsPulseGuiding
        {
            get
            {
                if (IsPulseGuidingDec || IsPulseGuidingRa) return true;
                return false;
            }
        }

        /// <summary>
        /// Used to inform and show error on the UI thread
        /// </summary>
        private static Exception _mountError;

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
        /// Park position selected in UI
        /// </summary>
        public static ParkPosition ParkSelected { private get; set; }

        //private static DateTime SettleTime { get; set; } not sure if this is needed

        private static double _slewSettleTime;
        public static double SlewSettleTime
        {
            get => _slewSettleTime;
            set
            {
                if (Math.Abs(_slewSettleTime - value) <= 0) return;
                _slewSettleTime = value;
            }
        }

        // UI display for converted ra
        private static double _rightAscensionXform;
        public static double RightAscensionXform
        {
            get => _rightAscensionXform;
            private set
            {
                var ra = Transforms.RaToCoordType(value, Declination);
                _rightAscensionXform = ra;
                OnStaticPropertyChanged();
            }
        }

        // Moves mount to other side
        public static PierSide SideOfPier
        {
            get
            {
                if (SouthernHemisphere)
                {
                    //return _mountAxes.Y <= 90 && _mountAxes.Y >= -90 ? PierSide.pierWest : PierSide.pierEast;
                    // replaced with ...
                    if (_mountAxes.Y < 90.0000000001  && _mountAxes.Y > -90.0000000001 )
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
                var a = Axes.GetAltAxisPosition(new[] { ActualAxisX, ActualAxisY});
                var b = Axes.AxesAppToMount(a);

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value},{SkySettings.HourAngleLimit},{b[0]},{b[1]}"
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

        // UI indicator
        private static PierSide _isSideOfPier;
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
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value},{_mountAxes.Y},{_mountAxes.Y < 90 || _mountAxes.Y.IsEqualTo(90, 0.0000000001)},{_mountAxes.Y > -90 || _mountAxes.Y.IsEqualTo(-90, 0.0000000001)} "
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        // Local time
        private static double _siderealTime;
        public static double SiderealTime
        {
            get => _siderealTime;
            private set
            {
                _siderealTime = value;
                if (Debug) OnStaticPropertyChanged();
            }
        }

        // doing gotos
        private static bool _isSlewing;
        public static bool IsSlewing
        {
            get => _isSlewing;
            private set
            {
                if (_isSlewing == value) return;
                _isSlewing = value;
                OnStaticPropertyChanged();
            }
        }

        // applies backlash to pulse
        private static GuideDirections LastDecDirection { get; set; }

        // UI indicator for axes limits
        private static bool _limitAlarm;
        public static bool LimitAlarm
        {
            get => _limitAlarm;
            set
            {
                if (LimitAlarm == value) return;
                _limitAlarm = value;
                if (value) Synthesizer.Speak(Application.Current.Resources["vceLimit"].ToString());
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Starts/Stops current selected mount
        /// </summary>
        private static bool _mountrunning;
        public static bool IsMountRunning
        {
            get
            {
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _mountrunning = MountQueue.IsRunning;
                        break;
                    case MountType.SkyWatcher:
                        _mountrunning = SkyQueue.IsRunning;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return _mountrunning;
            }
            set
            {
                if (value == _mountrunning) return;
                _mountrunning = value;
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

        // Pulse monitoring for charts
        private static bool _monitorPulse;
        public static bool MonitorPulse
        {
            private get => _monitorPulse;
            set
            {
                if (_monitorPulse == value) return;
                _monitorPulse = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SimTasks(MountTaskName.MonitorPulse);
                SkyTasks(MountTaskName.MonitorPulse);
            }
        }

        // UI diagnostics
        private static double _mountAxisX;
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

        // UI diagnostics
        private static double _mountAxisY;
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

        // General Info
        public static string MountName { get; private set; }
        public static string MountVersion { get; private set; }
        public static long[] StepsPerRevolution { get; set; }

        // Opens and tracks settings screen
        private static bool _openSetupDialog;

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

        // Ensures settings only open once
        public static bool OpenSetupDialogFinished { get; set; }

        // Set for all types of gotos
        public static SlewType SlewState { get; private set; }

        // Camera Port
        private static bool SnapPort { get; set; }

        // Southern alignments
        public static bool SouthernHemisphere => SkySettings.Latitude < 0;

        // Goto targets
        private static Vector _targetRaDec;

        // Target for slewing
        public static double TargetDec
        {
            get => _targetRaDec.Y;
            set => _targetRaDec.Y = value;
        }

        // Target for slewing
        public static double TargetRa
        {
            get => _targetRaDec.X;
            set => _targetRaDec.X = value;
        }

        // Persistance of the rtf document while switching tabs
        public static string Notes { get; set; }

        // Counts any overlapping events that might occur
        // should always be 0 or event interval is too fast
        private static int TimerOverruns { get; set; }

        // Tracks tracking
        private static TrackingMode _trackingMode;
        private static bool _tracking;
        public static bool TrackingSpeak { private get; set; }
        public static bool Tracking
        {
            get => _trackingMode != TrackingMode.Off;
            set
            {
                if (value == _tracking) return;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (value)
                {
                    if (AtPark)
                    {
                        if(TrackingSpeak)Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
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
                }
                _tracking = value;

                SetTracking();
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Simulator Specific Items

        /// <summary>
        /// Sim GOTO slew
        /// </summary>
        /// <returns></returns>
        private static int SimGoTo(double[] target, bool trackingState)
        {
            var returncode = 1;
            //  stop slew after 60 seconds
            const int timer = 60;
            var stopwatch = Stopwatch.StartNew();

            SimTasks(MountTaskName.StopAxes);
            var simTarget = Axes.AxesAppToMount(target);

            #region First Slew

            // time could be off a bit may need to deal with each axis seperate
            object _ = new CmdAxisGoToTarget(0, Axis.Axis1, simTarget[0]);
            _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(100);
                if (SlewState == SlewType.SlewNone) break;
                var statusx = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                var axis1Status = (AxisStatus) MountQueue.GetCommandResult(statusx).Result;
                var axis1Stopped = axis1Status.Stopped;

                var statusy = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                var axis2Status = (AxisStatus) MountQueue.GetCommandResult(statusy).Result;
                var axis2Stopped = axis2Status.Stopped;

                if (!axis1Stopped || !axis2Stopped) continue;
                if (SlewSettleTime > 0)
                {
                    // post-slew settling time
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed.TotalSeconds < SlewSettleTime)
                    {
                    }

                    sw.Stop();
                }

                break;
            }

            AxesStopValidate();
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Initial:{_targetAxes},{stopwatch.Elapsed.TotalSeconds}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            #endregion

            #region Repeat Slews for Ra

            if (trackingState)
            {
                //attempt precision moves to target
                var gotoPrecision = SkySettings.GotoPrecision;
                var rate = CurrentTrackingRate();
                var deltaTime = stopwatch.Elapsed.TotalSeconds;
                var maxtries = 0;

                while (true)
                {
                    if (maxtries > 3) break;
                    maxtries++;
                    stopwatch.Reset();
                    stopwatch.Start();

                    //calculate new target position
                    var deltaDegree = rate * deltaTime;
                    if (deltaDegree < gotoPrecision) break;

                    target[0] += deltaDegree;
                    var deltaTarget = Axes.AxesAppToMount(target);

                    //check for a stop
                    if (SlewState == SlewType.SlewNone) break;

                    //move to new target
                    _ = new CmdAxisGoToTarget(0, Axis.Axis1, deltaTarget[0]);

                    // check for axis stopped
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed.TotalMilliseconds < 2000)
                    {
                        if (SlewState == SlewType.SlewNone) break;
                        var deltax = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                        var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltax).Result;
                        if (!axis1Status.Slewing) break; // stopped doesn't report quick enough
                    }
                    sw.Stop();

                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Percision:{target[0]},{deltaTime},{deltaDegree}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    //take the time and move again
                    deltaTime = stopwatch.Elapsed.TotalSeconds;
                }

                //make sure all axes are stopped
                SimTasks(MountTaskName.StopAxes);
            }

            stopwatch.Stop();

            //evertyhing seems okay to return
            returncode = 0;

            #endregion

            return returncode;
        }

        /// <summary>
        /// Creates tasks that are put in the MountQueue
        /// </summary>
        /// <param name="taskname"></param>
        private static void SimTasks(MountTaskName taskname)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{taskname}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            object _;

            switch (SkySettings.Mount)
            {
                case MountType.SkyWatcher:
                    break;
                case MountType.Simulator:
                    switch (taskname)
                    {
                        case MountTaskName.AlternatingPpec:
                            break;
                        case MountTaskName.CanPpec:
                            CanPec = false;
                            break;
                        case MountTaskName.CanHomeSensor:
                            var canHomeCmda = new CmdCapabilities(MountQueue.NewId);
                            var mountinfo = (MountInfo)MountQueue.GetCommandResult(canHomeCmda).Result;
                            CanHomeSensor = mountinfo.CanHomeSensors;
                            break;
                        case MountTaskName.DecPulseToGoTo:
                            break;
                        case MountTaskName.Encoders:
                            break;
                        case MountTaskName.FullCurrent:
                            break;
                        case MountTaskName.GetOneStepIndicators:
                            break;
                        case MountTaskName.LoadDefaults:
                            break;
                        case MountTaskName.StopAxes:
                            _ = new CmdAxisStop(0, Axis.Axis1);
                            _ = new CmdAxisStop(0, Axis.Axis2);
                            break;
                        case MountTaskName.InitialiseAxes:
                            break;
                        case MountTaskName.InstantStopAxes:
                            break;
                        case MountTaskName.SetSouthernHemisphere:
                            break;
                        case MountTaskName.SyncAxes:
                            var sync = Axes.AxesAppToMount(new[] {_mountAxes.X, _mountAxes.Y});
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, sync[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, sync[1]);
                            break;
                        case MountTaskName.SyncTarget:
                            var xy = Axes.RaDecToAxesXY(new[] {TargetRa, TargetDec});
                            var targ = Axes.AxesAppToMount(new[] {xy[0], xy[1]});
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, targ[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, targ[1]);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] {_altAzSync.Y, _altAzSync.X});
                            var altaz = Axes.AxesAppToMount(new[] {yx[1], yx[0]});
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
                            break;
                        case MountTaskName.SetSt4Guiderate:
                            break;
                        case MountTaskName.SkySetSnapPort:
                            break;
                        case MountTaskName.MountName:
                            var mountName = new CmdMountName(MountQueue.NewId);
                            MountName = (string) MountQueue.GetCommandResult(mountName).Result;
                            break;
                        case MountTaskName.GetAxisVersions:
                            break;
                        case MountTaskName.GetAxisStrVersions:
                            break;
                        case MountTaskName.MountVersion:
                            var mountVersion = new CmdMountVersion(MountQueue.NewId);
                            MountVersion = (string) MountQueue.GetCommandResult(mountVersion).Result;
                            break;
                        case MountTaskName.StepsPerRevolution:
                            var spr = new CmdSpr(MountQueue.NewId);
                            var sprnum = (long) MountQueue.GetCommandResult(spr).Result;
                            StepsPerRevolution = new[] {sprnum, sprnum};
                            break;
                        case MountTaskName.SetHomePositions:
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, _homeAxes.X);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, _homeAxes.Y);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(taskname), taskname, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Sky Specific Items

        // used to combine multiple sources for a single slew rate
        // include tracking, hand controller, etc..
        private static Vector SkyHCRate;
        private static Vector SkyTrackingRate;

        // PPEC info
        private static bool _canpec;
        public static bool CanPec
        {
            get => _canpec;
            private set
            {
                if (_canpec == value) return;
                _canpec = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _pec;
        public static bool Pec
        {
            get => _pec;
            set
            {
                if (Pec == value) return;
                _pec = value;
                Synthesizer.Speak(value ? Application.Current.Resources["vcePeckOn"].ToString() : Application.Current.Resources["vcePeckOff"].ToString());

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyTasks(MountTaskName.Pec);
                //SkyTasks(MountTaskName.Capabilities);
                OnStaticPropertyChanged();
            }
        }

        private static bool _pecTraining;
        public static bool PecTraining
        {
            get => _pecTraining;
            set
            {
                if (PecTraining == value) return;
                _pecTraining = value;
                Synthesizer.Speak(value ? Application.Current.Resources["vcePeckTrainOn"].ToString() : Application.Current.Resources["vcePeckTrainOff"].ToString());
                
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyTasks(MountTaskName.PecTraining);
                OnStaticPropertyChanged();
            }
        }

        private static bool _pecTrainInProgress;
        public static bool PecTrainInProgress
        {
            get => _pecTrainInProgress;
            private set
            {
                if (_pecTrainInProgress == value) return;
                _pecTrainInProgress = value;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server, Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Monitors the mount doing PPEC training
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

                    var ppectrain = new SkyIsPpecInTrainingOn(SkyQueue.NewId);
                    PecTraining = (bool) SkyQueue.GetCommandResult(ppectrain).Result;
                    PecTrainInProgress = PecTraining;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
            change += _rateRaDec; // tracking rate

            //todo might be a good place to reject for axis limits
            CheckAxisLimits();

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{change}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            return change;
        }

        /// <summary>
        /// Skywatcher GOTO slew
        /// </summary>
        /// <returns></returns>
        private static int SkyGoTo(double[] target, bool trackingState)
        {
            var returncode = 1;
            //  stop slew after 60 seconds
            const int timer = 60;
            var stopwatch = Stopwatch.StartNew();

            SkyTasks(MountTaskName.StopAxes);
            var skyTarget = Axes.AxesAppToMount(target);

            #region First Slew

            // time could be off a bit may need to deal with each axis seperate
            object _ = new SkyAxisGoToTarget(0, AxisId.Axis1, skyTarget[0]);
            _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(100);
                if (SlewState == SlewType.SlewNone) break;

                var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                var axis1Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);

                var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                var axis2Stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusy).Result);

                if (!axis1Stopped || !axis2Stopped) continue;
                if (SlewSettleTime > 0)
                {
                    // post-slew settling time
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed.TotalSeconds < SlewSettleTime)
                    {
                    }

                    sw.Stop();
                }

                break;
            }

            AxesStopValidate();
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Initial:{_targetAxes},{stopwatch.Elapsed.TotalSeconds},{skyTarget[0]},{skyTarget[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            
            #endregion

            #region Repeat Slews for Ra

            if (trackingState)
            {
                //attempt precision moves to target
                var gotoPrecision = SkySettings.GotoPrecision;
                var rate = CurrentTrackingRate();
                var deltaTime = stopwatch.Elapsed.TotalSeconds;
                var maxtries = 0;

                while (true)
                {
                    if (maxtries > 3) break;
                    maxtries++;
                    stopwatch.Reset();
                    stopwatch.Start();

                    //calculate new target position
                    var deltaDegree = rate * deltaTime;

                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Delta:{rate},{deltaTime},{deltaDegree}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    if (deltaDegree < gotoPrecision) break;

                    target[0] += deltaDegree;
                    var deltaTarget = Axes.AxesAppToMount(target);

                    //check for a stop
                    if (SlewState == SlewType.SlewNone) break;

                    //move to new target
                    _ = new SkyAxisGoToTarget(0, AxisId.Axis1, deltaTarget[0]);

                    // track movment until axis is stopped
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed.TotalMilliseconds < 2000)
                    {
                        if (SlewState == SlewType.SlewNone) break;
                        var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                        var axis1stopped = Convert.ToBoolean(SkyQueue.GetCommandResult(statusx).Result);
                        if (axis1stopped) break;
                    }
                    sw.Stop();

                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Precision:{target[0]},{deltaTime},{deltaDegree}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    //take the time and move again
                    deltaTime = stopwatch.Elapsed.TotalSeconds;
                }

                //make sure all axes are stopped
                SkyTasks(MountTaskName.StopAxes);
            }

            stopwatch.Stop();

            //evertyhing seems okay to return
            returncode = 0;

            #endregion

            return returncode;
        }

        /// <summary>
        /// Creates tasks that are put in the SkyQueue
        /// </summary>
        /// <param name="taskname"></param>
        internal static void SkyTasks(MountTaskName taskname)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{taskname}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            object _;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    break;
                case MountType.SkyWatcher:
                    switch (taskname)
                    {
                        case MountTaskName.AlternatingPpec:
                            _ = new SkySetAlternatingPpec(0, SkySettings.AlternatingPpec);
                            break;
                        case MountTaskName.DecPulseToGoTo:
                            _ = new SkySetDecPulseToGoTo(0, SkySettings.DecPulseToGoTo);
                            break;
                        case MountTaskName.CanPpec:
                            var SkyMountCanPpec = new SkyCanPpec(SkyQueue.NewId);
                            CanPec = (bool)SkyQueue.GetCommandResult(SkyMountCanPpec).Result;
                            break;
                        case MountTaskName.CanHomeSensor:
                            var canHomeSky = new SkyCanHomeSensors(SkyQueue.NewId);
                            CanHomeSensor = (bool)SkyQueue.GetCommandResult(canHomeSky).Result;
                            break;
                        case MountTaskName.Capabilities:
                            // populates driver with mount capabilities
                            _ = new SkyGetCapabilities(0);
                            break;
                        case MountTaskName.Encoders:
                            _ = new SkySetEncoder(0, AxisId.Axis1, SkySettings.Encoders);
                            _ = new SkySetEncoder(0, AxisId.Axis2, SkySettings.Encoders);
                            break;
                        case MountTaskName.FullCurrent:
                            _ = new SkySetFullCurrent(0, AxisId.Axis1, SkySettings.FullCurrent);
                            _ = new SkySetFullCurrent(0, AxisId.Axis2, SkySettings.FullCurrent);
                            break;
                        case MountTaskName.GetOneStepIndicators:
                            //tests if mount can one step
                            _ = new SkyGetOneStepIndicators(0);
                            break;
                        case MountTaskName.LoadDefaults:
                            _ = new SkyLoadDefaultMountSettings(0);
                            break;
                        case MountTaskName.InitialiseAxes:
                            _ = new SkyInitializeAxes(0);
                            break;
                        case MountTaskName.InstantStopAxes:
                            _ = new SkyAxisStopInstant(0, AxisId.Axis1);
                            _ = new SkyAxisStopInstant(0, AxisId.Axis2);
                            break;
                        case MountTaskName.MonitorPulse:
                            _ = new SkySetMonitorPulse(0, MonitorPulse);
                            break;
                        case MountTaskName.PecTraining:
                            _ = new SkySetPpecTrain(0, AxisId.Axis1, PecTraining);
                            break;
                        case MountTaskName.Pec:
                            _ = new SkySetPpec(0, AxisId.Axis1, Pec);
                            break;
                        case MountTaskName.StopAxes:
                            _ = new SkyAxisStop(0, AxisId.Axis1);
                            _ = new SkyAxisStop(0, AxisId.Axis2);
                            break;
                        case MountTaskName.SetSt4Guiderate:
                            _ = new SkySetSt4Guiderate(0, SkySettings.St4Guiderate);
                            break;
                        case MountTaskName.SetSouthernHemisphere:
                            _ = new SkySetSouthernHemisphere(0, SouthernHemisphere);
                            break;
                        case MountTaskName.SkySetSnapPort:
                            _ = new SkySetSnapPort(0, SnapPort);
                            break;
                        case MountTaskName.SyncAxes:
                            var sync = Axes.AxesAppToMount(new[] {_mountAxes.X, _mountAxes.Y});
                            _ = new SkySyncAxis(0, AxisId.Axis1, sync[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, sync[1]);
                            break;
                        case MountTaskName.SyncTarget:
                            var xy = Axes.RaDecToAxesXY(new[] {TargetRa, TargetDec});
                            var targ = Axes.AxesAppToMount(new[] {xy[0], xy[1]});
                            _ = new SkySyncAxis(0, AxisId.Axis1, targ[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, targ[1]);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] {_altAzSync.Y, _altAzSync.X});
                            var altaz = Axes.AxesAppToMount(new[] {yx[1], yx[0]});
                            _ = new SkySyncAxis(0, AxisId.Axis1, altaz[0]);
                            _ = new SkySyncAxis(0, AxisId.Axis2, altaz[1]);
                            break;
                        case MountTaskName.GetAxisVersions:
                            var skyAxisVersions = new SkyGetAxisStringVersions(SkyQueue.NewId);
                            // Not used atm
                            _ = (long[]) SkyQueue.GetCommandResult(skyAxisVersions).Result;
                            break;
                        case MountTaskName.GetAxisStrVersions:
                            var skyAxisStrVersions = new SkyGetAxisStringVersions(SkyQueue.NewId);
                            // Not used atm
                            _ = (string) SkyQueue.GetCommandResult(skyAxisStrVersions).Result;
                            break;
                        case MountTaskName.MountName:
                            var skyMountType = new SkyMountType(SkyQueue.NewId);
                            MountName = (string) SkyQueue.GetCommandResult(skyMountType).Result;
                            break;
                        case MountTaskName.MountVersion:
                            var skyMountVersion = new SkyMountVersion(SkyQueue.NewId);
                            MountVersion = (string) SkyQueue.GetCommandResult(skyMountVersion).Result;
                            break;
                        case MountTaskName.StepsPerRevolution:
                            var SkyMountRevolutions = new SkyGetStepsPerRevolution(SkyQueue.NewId);
                            StepsPerRevolution = (long[]) SkyQueue.GetCommandResult(SkyMountRevolutions).Result;
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
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.StackTrace}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            AlertState = true;
            var extype = ex.GetType().ToString().Trim();
            switch (extype)
            {
                case "GS.SkyWatcher.MountControlException":
                    var mounterr = (MountControlException) ex;
                    switch (mounterr.ErrorCode)
                    {
                        case SkyWatcher.ErrorCode.ErrInvalidId:
                        case SkyWatcher.ErrorCode.ErrAlreadyConnected:
                        case SkyWatcher.ErrorCode.ErrNotConnected:
                        case SkyWatcher.ErrorCode.ErrInvalidData:
                        case SkyWatcher.ErrorCode.ErrSerialPortBusy:
                        case SkyWatcher.ErrorCode.ErrMountNotFound:
                        case SkyWatcher.ErrorCode.ErrNoresponseAxis1:
                        case SkyWatcher.ErrorCode.ErrNoresponseAxis2:
                        case SkyWatcher.ErrorCode.ErrAxisBusy:
                        case SkyWatcher.ErrorCode.ErrMaxPitch:
                        case SkyWatcher.ErrorCode.ErrMinPitch:
                        case SkyWatcher.ErrorCode.ErrUserInterrupt:
                        case SkyWatcher.ErrorCode.ErrAlignFailed:
                        case SkyWatcher.ErrorCode.ErrUnimplement:
                        case SkyWatcher.ErrorCode.ErrWrongAlignmentData:
                        case SkyWatcher.ErrorCode.ErrQueueFailed:
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
                    var skyerr = (SkyServerException) ex;
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
        /// Checks command object for errors and unsucessful execution
        /// </summary>
        /// <param name="command"></param>
        /// <returns>true for errors found and not sucessful</returns>
        private static bool CheckSkyErrors(ISkyCommand command)
        {
            return !command.Successful && command.Exception != null;
        }

        #endregion

        #region Shared Mount Commands

        /// <summary>
        /// Abort Slew in a normal motion
        /// </summary>
        public static void AbortSlew()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SlewState},{Tracking}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            //IsSlewing = false;
            _rateAxes = new Vector();
            _rateRaDec = new Vector();
            SlewState = SlewType.SlewNone;
            var tracking = Tracking;
            //Tracking = false;

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

            Synthesizer.Speak(Application.Current.Resources["vceAbortSlew"].ToString());
        }

        /// <summary>
        /// Autohome, Slew home based on mount's home sensor
        /// </summary>
        public static async void AutoHomeAsync(int degreelimit = 100, int offsetdec = 0)
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Started"
                };
                MonitorLog.LogToMonitor(monitorItem);

                var returncode1 = 0;
                var returncode2 = 0;
                if (degreelimit < 20) degreelimit = 100;
                AutoHomeProgressBar = 0;
                var EncoderTemp = SkySettings.Encoders;
                if (Tracking) Tracking = false;
                Synthesizer.Speak(Application.Current.Resources["msgAutoHomeStart"].ToString());
                Synthesizer.VoicePause = true;

                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        var autosim = new AutohomeSim();
                        returncode1 = await Task.Run(() => autosim.StartAutoHome(Axis.Axis1, degreelimit));
                        AutoHomeProgressBar = 50;
                        returncode2 = await Task.Run(() => autosim.StartAutoHome(Axis.Axis2, degreelimit, offsetdec));
                        break;
                    case MountType.SkyWatcher:
                        var autossky = new AutohomeSky();
                        returncode1 = await Task.Run(() => autossky.StartAutoHome(AxisId.Axis1, degreelimit));
                        AutoHomeProgressBar = 50;
                        returncode2 = await Task.Run(() => autossky.StartAutoHome(AxisId.Axis2, degreelimit, offsetdec));
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
                        msgcode1 = "RA too many restarts";
                        break;
                    default:
                        msgcode2 = "Dec code not found";
                        break;
                }

                StopAxes();

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Complete: {returncode1},{returncode2}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (returncode1 == 0 && returncode2 == 0)
                {
                    // all is ok
                    ResetHomePositions();
                    Synthesizer.VoicePause = false;
                    Thread.Sleep(1500);
                    Synthesizer.Speak(Application.Current.Resources["msgAutoHomeComplete"].ToString());
                    
                }
                else
                {
                    //throw only if not a cancel request
                    if (returncode1 != -3 && returncode2 != -3)
                    {
                        throw new Exception($"Incomplete: {msgcode1} ({returncode1}), {msgcode2}({returncode2})");
                    }
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = "AutoHomeAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                MountError = ex;
            }
            finally
            {
                AutoHomeProgressBar = 100;
                Synthesizer.VoicePause = false; //make sure pause is off
            }
        }

        /// <summary>
        /// Makes sure the axes are at full stop
        /// </summary>
        /// <returns></returns>
        private static bool AxesStopValidate()
        {
            if (!IsMountRunning) return true;
            Stopwatch stopwatch;
            var axis1Stopped = false;
            var axis2Stopped = false;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:

                    stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalMilliseconds <= 5000)
                    {
                        SimTasks(MountTaskName.StopAxes);
                        Thread.Sleep(100);
                        var statusx = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                        var axis1Status = (AxisStatus) MountQueue.GetCommandResult(statusx).Result;
                        axis1Stopped = axis1Status.Stopped;

                        var statusy = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                        var axis2Status = (AxisStatus) MountQueue.GetCommandResult(statusy).Result;
                        axis2Stopped = axis2Status.Stopped;

                        if (!axis1Stopped || !axis2Stopped) continue;
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

                        if (!axis1Stopped || !axis2Stopped) continue;
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
                    }
                    else
                    {
                        if (_mountAxes.X >= SkySettings.HourAngleLimit + 180 ||
                            _mountAxes.X <= -SkySettings.HourAngleLimit)
                        {
                            limitHit = true;
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

            //if (limitHit) Tracking = false; if limit hit turn off tracking
            LimitAlarm = limitHit;
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

            if (Math.Abs(RateAxisRa + RateAxisDec) > 0) slewing = true;
            IsSlewing = slewing;
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
            if (change.Y > 90 - Altitude)
                change.Y = 0;
            // limit the primary to the maximum slew rate
            if (change.X < -SlewSpeedEight)
                change.X = -SlewSpeedEight;
            if (change.X > SlewSpeedEight)
                change.X = SlewSpeedEight;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"change:{change.X},{change.Y}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return change;
        }

        /// <summary>
        /// Calculates the current tracking rate used in arcseconds per degree
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

            rate = rate / 3600;
            if (SkySettings.RaTrackingOffset <= 0) return rate;
            var offsetrate = rate * (Convert.ToDouble(SkySettings.RaTrackingOffset) / 100000);
            rate = rate + offsetrate;
            return rate;
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

            if (AtPark)
            {
                if (SkySettings.AutoTrack)
                {
                    AtPark = false;
                    Tracking = SkySettings.AutoTrack;
                }

                positions = Axes.AxesAppToMount(new[] { SkySettings.ParkAxisX, SkySettings.ParkAxisY });
            }
            else
            { 
                positions = new[] {_homeAxes.X, _homeAxes.Y};
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{positions[0]},{positions[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return positions;
        }

        /// <summary>
        /// Gets current converted positions from the mount
        /// </summary>
        /// <returns></returns>
        private static double[] GetRawPositions()
        {
            var actualDegrees = new[] { double.NaN, double.NaN };
            if (!IsMountRunning) return actualDegrees;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    var simPositions = new CmdAxesDegrees(MountQueue.NewId);
                    actualDegrees = (double[]) MountQueue.GetCommandResult(simPositions).Result;
                    break;
                case MountType.SkyWatcher:
                    var skyPositions = new SkyGetPositionsInDegrees(SkyQueue.NewId);
                    actualDegrees = (double[]) SkyQueue.GetCommandResult(skyPositions).Result;
                    return CheckSkyErrors(skyPositions) ? null : actualDegrees;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return actualDegrees;
        }

        /// <summary>
        /// Runs the Goto in async so not to block the driver or UI threads.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slewState"></param>
        private static async void GoToAsync(double[] target, SlewType slewState)
        {
            if (IsSlewing)
            {
                SlewState = SlewType.SlewNone;
                var stopped = AxesStopValidate();
                if (!stopped)
                {
                    AbortSlew();
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
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

            var returncode = 0;
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
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = "GoToAsync",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Goto finished:{returncode},{SlewState}, {_util.HoursToHMS(RightAscension, "h ", ":", "", 2)}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SlewState = SlewType.SlewNone;
                SpeakSlewEnd(startingState);
                Tracking = trackingState;
                TrackingSpeak = true;

                return;
            }
            Tracking = trackingState;
            AbortSlew();
            throw new Exception($"GoTo Async Error: {returncode}");
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
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = "Slew to Home"
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

            // get position selected in ui
            var ps = ParkSelected;
            if (ps == null) return;
            SetParkAxis(ps.Name, ps.X, ps.Y);

            Tracking = false;
            var x = SkySettings.ParkAxisX;
            var y = SkySettings.ParkAxisY;
            var name = SkySettings.ParkName;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{name},{x},{y}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            SlewMount(new Vector(x, y), SlewType.SlewPark);
        }

        /// <summary>
        /// return the change in axis values as a result of any HC button presses
        /// </summary>
        /// <returns></returns>
        public static void HcMoves(SlewSpeed speed, SlewDirection direction)
        {
            var change = new double[] {0, 0};
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

            // check the button states and implement mode
            switch (SkySettings.HcMode)
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
                        case SlewDirection.SlewNone:
                            break;
                        default:
                            change[0] = 0;
                            change[1] = 0;
                            break;
                    }
                    break;
                //case HCMode.Compass:
                //    switch (direction)
                //    {
                //        case SlewDirection.SlewNorth:
                //        case SlewDirection.SlewUp:
                //            switch (SkySystem.Mount)
                //            {
                //                case MountType.Simulator:
                //                    change[1] = MountAxisY >= 90 ? -delta : delta;
                //                    break;
                //                case MountType.SkyWatcher:
                //                    if (SouthernHemisphere)
                //                    {
                //                        change[1] = MountAxisY >= 90 ? -delta : delta;
                //                    }
                //                    else
                //                    {
                //                        change[1] = MountAxisY >= 90 ? delta : -delta;
                //                    }

                //                    break;
                //                default:
                //                    throw new ArgumentOutOfRangeException();
                //            }
                //            break;
                //        case SlewDirection.SlewSouth:
                //        case SlewDirection.SlewDown:
                //            switch (SkySystem.Mount)
                //            {
                //                case MountType.Simulator:
                //                    change[1] = MountAxisY < 90 ? -delta : delta;
                //                    break;
                //                case MountType.SkyWatcher:
                //                    if (SouthernHemisphere)
                //                    {
                //                        change[1] = MountAxisY < 90 ? -delta : delta;
                //                    }
                //                    else
                //                    {
                //                       change[1] = MountAxisY < 90 ? delta : -delta; 
                //                    }
                                    
                //                    break;
                //                default:
                //                    throw new ArgumentOutOfRangeException();
                //            }
                //            break;
                //        case SlewDirection.SlewEast:
                //        case SlewDirection.SlewLeft:
                //            change[0] = SouthernHemisphere ? delta : -delta;
                //            break;
                //        case SlewDirection.SlewWest:
                //        case SlewDirection.SlewRight:
                //            change[0] = SouthernHemisphere ? -delta : delta;
                //            break;
                //        case SlewDirection.SlewNone:
                //            break;
                //        default:
                //            change[0] = 0;
                //            change[1] = 0;
                //            break;
                //    }
                //    break;
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
                        case SlewDirection.SlewNone:
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
            
            if (Math.Abs(change[0]) > 0 || Math.Abs(change[1]) > 0)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{SkySettings.HcSpeed},{direction},{change[0]},{change[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

            SlewState = Math.Abs(change[0]) + Math.Abs(change[1]) > 0 ? SlewType.SlewHandpad : SlewType.SlewNone;

            object _;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    _ = new CmdHcSlew(0, Axis.Axis1, change[0]);
                    _ = new CmdHcSlew(0, Axis.Axis2, change[1]);
                    break;
                case MountType.SkyWatcher:
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
        /// Sets up defaults after an established connection
        /// </summary>
        private static bool MountConnect()
        {
            object _;
            if(!AscomOn)AscomOn = true;
            _targetRaDec = new Vector(double.NaN, double.NaN); // invalid target position
            var positions = GetDefaultPositions();
            double[] rawPositions = null;
            var counter = 0;

            MonitorEntry monitorItem;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    // defaults
                    SimTasks(MountTaskName.MountName);
                    SimTasks(MountTaskName.MountVersion);
                    SimTasks(MountTaskName.StepsPerRevolution);
                    SimTasks(MountTaskName.CanHomeSensor);

                    // checks if the mount is close enough to home position to set default position. If not use the positions from the mount
                    while ( rawPositions == null)
                    {
                        if (counter > 5)
                        {
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, positions[0]);
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, positions[1]);
                            break;
                        }
                        counter++;
                        rawPositions = GetRawPositions();
                        if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1]))
                        {
                            rawPositions = null;
                            continue;
                        }
                        if (!rawPositions[0].IsBetween(-.1, .1) || !rawPositions[1].IsBetween(-.1, .1)) continue;
                        _ = new CmdAxisToDegrees(0, Axis.Axis1, positions[0]);
                        _ = new CmdAxisToDegrees(0, Axis.Axis2, positions[1]);
                    }
                    
                    break;
                case MountType.SkyWatcher:
                    SkyHCRate = new Vector(0, 0);
                    SkyTrackingRate = new Vector(0, 0);

                    // create a command and put in queue to test connection
                    var init = new SkyGetMotorCardVersion(SkyQueue.NewId, AxisId.Axis1);
                    _ = (string) SkyQueue.GetCommandResult(init).Result;
                    if (!init.Successful && init.Exception != null)
                    {
                        SkyErrorHandler(init.Exception);
                        return false;
                    }

                    // defaults
                    SkyTasks(MountTaskName.StopAxes);
                    SkyTasks(MountTaskName.LoadDefaults);
                    SkyTasks(MountTaskName.Encoders);
                    SkyTasks(MountTaskName.FullCurrent);
                    SkyTasks(MountTaskName.SetSt4Guiderate);
                    SkyTasks(MountTaskName.SetSouthernHemisphere);
                    SkyTasks(MountTaskName.MountName);
                    SkyTasks(MountTaskName.MountVersion);
                    SkyTasks(MountTaskName.StepsPerRevolution);
                    SkyTasks(MountTaskName.InitialiseAxes);
                    SkyTasks(MountTaskName.GetOneStepIndicators);
                    SkyTasks(MountTaskName.CanPpec);
                    SkyTasks(MountTaskName.CanHomeSensor);

                    // checks if the mount is close enough to home position to set default position. If not use the positions from the mount
                    while (rawPositions == null)
                    {
                        if (counter > 5)
                        {
                            _ = new SkySetAxisPosition(0, AxisId.Axis1, positions[0]);
                            _ = new SkySetAxisPosition(0, AxisId.Axis2, positions[1]);
                            monitorItem = new MonitorEntry
                                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Counter exceded:{positions[0]},{positions[1]}" };
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        }
                        counter++;
                        rawPositions = GetRawPositions();
                        if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1]))
                        {
                            rawPositions = null;
                            continue;
                        }
                        
                        if (!rawPositions[0].IsBetween(-.1, .1) || !rawPositions[1].IsBetween(-.1, .1)) continue;
                        _ = new SkySetAxisPosition(0, AxisId.Axis1, positions[0]);
                        _ = new SkySetAxisPosition(0, AxisId.Axis2, positions[1]);

                        monitorItem = new MonitorEntry
                            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"SetPosition:{positions[0]},{positions[1]}" };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"MountAxes:{_mountAxes.X},{_mountAxes.Y} Actual:{ActualAxisX},{ActualAxisY}" };
            MonitorLog.LogToMonitor(monitorItem);

            var x = GetRawPositions();

            monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Get Positions:{x[0]},{x[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return true;
        }

        /// <summary>
        /// Start connection, queues, and events
        /// </summary>
        private static void MountStart()
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.Mount}"};
            MonitorLog.LogToMonitor(monitorItem);

            Defaults();
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    MountQueue.Start();
                    if (!MountQueue.IsRunning) throw new Exception("Failed to start simulator queue");
                    break;
                case MountType.SkyWatcher:
                    // open serial port
                    SkySystem.ConnectSerial = false;
                    SkySystem.ConnectSerial = true;
                    if (!SkySystem.ConnectSerial)
                        throw new SkyServerException(ErrorCode.ErrSerialFailed, $"Serial Failed COM{SkySettings.ComPort}");

                    // Start up
                    SkyQueue.Start(SkySystem.Serial);
                    if (!SkyQueue.IsRunning)
                        throw new SkyServerException(ErrorCode.ErrMount, "Failed to start sky queue");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // connect and set defaults
            if (MountConnect())
            {
                 // start with a stop
                AxesStopValidate();

                // Event to get mount data and update UI
                _mediatimer = new MediaTimer {Period = SkySettings.DisplayInterval, Resolution = 5};
                _mediatimer.Tick += UpdateServerEvent;
                _mediatimer.Start();
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
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.Mount}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (MountQueue.IsRunning)
            {
                AxesStopValidate();
                if (_mediatimer != null) _mediatimer.Tick -= UpdateServerEvent;
                _mediatimer?.Stop();
                _mediatimer?.Dispose();
                MountQueue.Stop();
            }

            if (!SkyQueue.IsRunning) return;
            AxesStopValidate();
            if(_mediatimer != null) _mediatimer.Tick -= UpdateServerEvent;
            _mediatimer?.Stop();
            _mediatimer?.Dispose();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 1000)
            {
            }

            sw.Stop();
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
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{direction},{duration}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            if (duration == 0)
            {
                // stops the current guide command
                switch (direction)
                {
                    case GuideDirections.guideNorth:
                    case GuideDirections.guideSouth:
                        IsPulseGuidingDec = false;
                        break;
                    case GuideDirections.guideEast:
                    case GuideDirections.guideWest:
                        IsPulseGuidingRa = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                return;
            }

            dynamic _;
            switch (direction)
            {
                case GuideDirections.guideNorth:
                case GuideDirections.guideSouth:
                    IsPulseGuidingDec = true;
                    break;
                case GuideDirections.guideEast:
                case GuideDirections.guideWest:
                    IsPulseGuidingRa = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            if (IsPulseGuidingRa)
            {
                var raGuideRate = Math.Abs(GuideRateRa);
                if (SouthernHemisphere)
                {
                    if (direction == GuideDirections.guideWest) raGuideRate = -raGuideRate;
                }
                else
                {
                    if (direction == GuideDirections.guideEast) raGuideRate = -raGuideRate;
                }

                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdAxisPulse(0, Axis.Axis1, raGuideRate, duration, SkySettings.RaBacklash, Declination);
                        PulseWait(duration, (int) Axis.Axis1);
                        break;
                    case MountType.SkyWatcher:
                        _ = new SkyAxisPulse(0, AxisId.Axis1, raGuideRate, duration, SkySettings.RaBacklash,
                            Declination);
                        PulseWait(duration, (int) Axis.Axis1);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (IsPulseGuidingDec)
            {
                var decGuideRate = Math.Abs(GuideRateDec);
                if (SideOfPier == PierSide.pierEast)
                {
                    if (direction == GuideDirections.guideNorth) decGuideRate = -decGuideRate;
                }
                else
                {
                    if (direction == GuideDirections.guideSouth) decGuideRate = -decGuideRate;
                }

                // Direction switched add backlash compensation
                var backlashswitch = 0;
                if (direction != LastDecDirection) backlashswitch = SkySettings.DecBacklash;
                LastDecDirection = direction;

                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        decGuideRate = decGuideRate > 0 ? -Math.Abs(decGuideRate) : Math.Abs(decGuideRate);
                        _ = new CmdAxisPulse(0, Axis.Axis2, decGuideRate, duration, backlashswitch, Declination);
                        PulseWait(duration, (int) Axis.Axis2);
                        break;
                    case MountType.SkyWatcher:
                        _ = new SkyAxisPulse(0, AxisId.Axis2, decGuideRate, duration, backlashswitch, Declination);
                        PulseWait(duration, (int) Axis.Axis2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

        /// <summary>
        /// Waits out the pulse duration time so it can report the pulse is finished.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="axis"></param>
        private static async void PulseWait(int duration, int axis)
        {
            await Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalMilliseconds < duration)
                {
                    //do something while waiting
                }

                sw.Stop();
                switch (axis)
                {
                    case 0:
                        IsPulseGuidingRa = false;
                        break;
                    case 1:
                        IsPulseGuidingDec = false;
                        break;
                }
            });

        }

        /// <summary>
        /// Sets the mount to 0,90 for a home position sync.
        /// </summary>
        public static void ResetHomePositions()
        {
            if (!IsMountRunning) return;
            if (Tracking)Tracking = false;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    SimTasks(MountTaskName.SetHomePositions);
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    SkyTasks(MountTaskName.SetHomePositions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_homeAxes.X}, {_homeAxes.Y}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Sets up offsets from the selected tracking rate
        /// </summary>
        internal static void SetGuideRates()
        {
            var rate = CurrentTrackingRate();
            GuideRate.X = rate * SkySettings.GuideRateOffsetX;
            GuideRate.Y = rate * SkySettings.GuideRateOffsetY;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{GuideRate.X},{GuideRate.Y}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Sets park positions from internal current positions
        /// </summary>
        public static void SetParkAxis(string name)
        {
            // convert current position
            var park = Axes.MountAxis2Mount();
            if (park == null) return;

            SkySettings.ParkAxisY = park[1];
            SkySettings.ParkAxisX = park[0];
            SkySettings.ParkName = name;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{park[1]},{park[0]}, {MountAxisX},{MountAxisY}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            Synthesizer.Speak(Application.Current.Resources["vceParkSet"].ToString());
        }

        /// <summary>
        /// Sets park positions, expects MountAxis2ParkCoords is already done
        /// </summary>
        private static void SetParkAxis(string name, double x, double y)
        {
            if (string.IsNullOrEmpty(name)) name = "Empty";

            SkySettings.ParkAxisY = y;
            SkySettings.ParkAxisX = x;
            SkySettings.ParkName = name;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{name},{x},{y}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Sets speeds for hand controller and slews in simulator
        /// </summary>
        internal static void SetSlewRates(double maxrate)
        {
            // Sky Speeds
            SlewSpeedOne = Math.Round(maxrate * 0.0034, 3);
            SlewSpeedTwo = Math.Round(maxrate * 0.0068, 3);
            SlewSpeedThree = Math.Round(maxrate * 0.047, 3);
            SlewSpeedFour = Math.Round(maxrate * 0.068, 3);
            SlewSpeedFive = Math.Round(maxrate * 0.2, 3);
            SlewSpeedSix = Math.Round(maxrate * 0.4, 3);
            SlewSpeedSeven = Math.Round(maxrate * 0.8, 3);
            SlewSpeedEight = Math.Round(maxrate * 1.0, 3);

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message =
                    $"{SlewSpeedOne},{SlewSpeedTwo},{SlewSpeedThree},{SlewSpeedFour},{SlewSpeedFive},{SlewSpeedSix},{SlewSpeedSeven},{SlewSpeedEight}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Send tracking on or off to mount
        /// </summary>
        private static void SetTracking()
        {
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

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_trackingMode},{rateChange}"
            };
            MonitorLog.LogToMonitor(monitorItem);
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

            var ha = Coordinate.Ra2Ha(rightAscension, SiderealTime);
            PierSide sideOfPier;
            if (ha < 0.0 && ha >= -12.0) sideOfPier = PierSide.pierWest;
            else if (ha >= 0.0 && ha <= 12.0) sideOfPier = PierSide.pierEast;
            else sideOfPier = PierSide.pierUnknown;

            return sideOfPier;
        }

        /// <summary>
        /// Starts slew with ra/dec coordinates
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        public static void SlewRaDec(double rightAscension, double declination)
        {
            // convert RA/Dec to axis
            var a = Axes.RaDecToAxesXY(new[] {rightAscension, declination});
            _targetAxes = new Vector(a[0], a[1]);
            SlewMount(_targetAxes, SlewType.SlewRaDec);
        }

        /// <summary>
        /// Wihin the meridian limits will check for closest slew
        /// </summary>
        /// <param name="position"></param>
        /// <returns>axis position that is closest</returns>
        public static double[] CheckAlternatePosition(double[] position)
        {
            // See if the target is within limits
            if (!IsWithinMeridianLimits(position)) return null;
            var alt = Axes.GetAltAxisPosition(position);

            var cl = ChooseClosestPosition(ActualAxisX, position, alt);
            if (cl != "b") return null;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    if (SouthernHemisphere) alt[0] = 180 - alt[0];
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
        private static string ChooseClosestPosition(double position, IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            var val1 = Math.Abs(a[0] - position);
            var val2 = Math.Abs(b[0] - position);
            if (!(Math.Abs(val1 - val2) > 0)) return "a";
            return val1 < val2 ? "a" : "b";
        }

        /// <summary>
        /// Calculates if axis position is within the defined meridian limits
        /// </summary>
        /// <param name="position">X axis poistion of mount</param>
        /// <returns>True if within limits otherwize false</returns>
        private static bool IsWithinMeridianLimits(IReadOnlyList<double> position)
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
            var yx = Axes.AltAzToAxesYX(new[] {targetAltAzm.Y, targetAltAzm.X});
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
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{targetPosition.X},{targetPosition.Y},{slewState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            //todo might be a good place to reject for axis limits

            _targetAxes = targetPosition;
            AtPark = false;
            SpeakSlewStart(slewState);
            GoToAsync(new[] {_targetAxes.X, _targetAxes.Y}, slewState);
        }

        /// <summary>
        /// Stop Axes in a normal motion
        /// </summary>
        public static void StopAxes()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{SlewState}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            //IsSlewing = false;
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
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{targetAzimuth},{targetAltitude}"
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
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {TargetRa},{TargetDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var trackingstate = Tracking;

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    SimTasks(MountTaskName.StopAxes);
                    SimTasks(MountTaskName.SyncTarget);
                    break;
                case MountType.SkyWatcher:
                    SkyTasks(MountTaskName.StopAxes);
                    SkyTasks(MountTaskName.SyncTarget);
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

        #endregion

        #region Server Items

        /// <summary>
        /// inital defaults
        /// </summary>
        private static void Defaults()
        {
            // default set for dec goto pulse
            LastDecDirection = GuideDirections.guideEast;

            // set default snap port but don't run task
            SnapPort = false;
            
            StepsPerRevolution = new long[] { 1296000, 1296000 };
            SlewSettleTime = 0;

            // reset any rates so slewing doesn't start
            _rateRaDec = new Vector(0, 0);
            _rateAxes = new Vector(0, 0);
            SlewState = SlewType.SlewNone;

            // invalid any target positions
            _targetRaDec = new Vector(double.NaN, double.NaN); 

            //default handcontrol and slew rates
            SetSlewRates(SkySettings.MaxSlewRate);

            // Allows driver movements commands to process
            AscomOn = true;

            // default guide rates
            SetGuideRates();

            Tracking = false;
            TrackingSpeak = true;
        }

        /// <summary>
        /// called from the setter property.  Used to update UI elements.  propertyname is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Time to open my mouth
        /// </summary>
        /// <param name="slewtype"></param>
        private static void SpeakSlewStart(SlewType slewtype)
        {
            switch (slewtype)
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
                    throw new ArgumentOutOfRangeException(nameof(slewtype), slewtype, null);
            }
        }

        /// <summary>
        /// Time to open my mouth
        /// </summary>
        /// <param name="slewtype"></param>
        private static void SpeakSlewEnd(SlewType slewtype)
        {
            switch (slewtype)
            {
                case SlewType.SlewNone:
                    break;
                case SlewType.SlewSettle:
                    break;
                case SlewType.SlewMoveAxis:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    break;
                case SlewType.SlewRaDec:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    break;
                case SlewType.SlewAltAz:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    break;
                case SlewType.SlewPark:
                    Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString());
                    break;
                case SlewType.SlewHome:
                    Synthesizer.Speak(Application.Current.Resources["vceHome"].ToString());
                    break;
                case SlewType.SlewHandpad:
                    break;
                case SlewType.SlewComplete:
                    Synthesizer.Speak(Application.Current.Resources["vceSlewComplete"].ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slewtype), slewtype, null);
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
                SiderealTime = Time.Lst(JDate.Epoch2000Days(), _util.JulianDate, false, SkySettings.Longitude);

                // Get raw positions, some are non-responses from mount and are returned as NaN
                var rawPositions = GetRawPositions();
                if (rawPositions == null) return;
                if (double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1])) return;

                // UI diagnostics
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

                // Track slewing state
                CheckSlewState();

                //used for warning light
                CheckAxisLimits();

                // Update UI 
                CheckPecTraining();
                IsHome = AtHome;
                IsSideOfPier = SideOfPier;

                // Event interval time set for UI performance
                _mediatimer.Period = SkySettings.DisplayInterval;
            }
            finally
            {
                if (hasLock) Monitor.Exit(_timerLock);
            }
        }
        
        #endregion
    }
}

