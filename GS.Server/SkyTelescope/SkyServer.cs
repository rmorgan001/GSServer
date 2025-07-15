/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Server.Alignment;
using GS.Server.AutoHome;
using GS.Server.Helpers;
using GS.Server.Pec;
using GS.Server.Pulses;
using GS.Server.Windows;
using GS.Shared;
using GS.Simulator;
using GS.SkyWatcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static System.Math;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
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

        private const double SiderealRate = 15.0410671786691;

        private static readonly Util Util = new Util();
        private static readonly object TimerLock = new object();
        private static MediaTimer _mediaTimer;
        private static MediaTimer _altAzTrackingTimer;
        private static Int32 _altAzTrackingLock;

        // Slew and HC speeds
        private static double _slewSpeedOne;
        private static double _slewSpeedTwo;
        private static double _slewSpeedThree;
        private static double _slewSpeedFour;
        private static double _slewSpeedFive;
        private static double _slewSpeedSix;
        private static double _slewSpeedSeven;
        public static double SlewSpeedEight;

        // HC Anti-Backlash
        private static HcPrevMove _hcPrevMoveRa;
        private static HcPrevMove _hcPrevMoveDec;
        private static readonly IList<double> HcPrevMovesDec = new List<double>();

        private static Vector _homeAxes;
        private static Vector _mountAxes;
       //private static Vector _targetAxes;
        private static Vector _altAzSync;

        public static readonly List<SpiralPoint> SpiralCollection;

        // AlignmentModel
        public static readonly AlignmentModel AlignmentModel;

        // Cancellation token sources for go to and pulse guide async operations
        private static CancellationTokenSource _ctsGoTo;
        private static CancellationTokenSource _ctsPulseGuideRa;
        private static CancellationTokenSource _ctsPulseGuideDec;
        private static CancellationTokenSource _ctsHcPulseGuide;
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
                    IsAlignmentOn = AlignmentSettings.IsAlignmentOn,
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
        private static bool _isPulseGuidingDec;
        private static bool _isPulseGuidingRa;
        private static PierSide _isSideOfPier;
        private static bool _isSlewing;
        private static Exception _lastAutoHomeError;
        private static double _lha;
        private static bool _limitAlarm;
        private static bool _lowVoltageEventState;
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
        private static Vector _rateMoveAxes;
        private static bool _moveAxisActive;
        private static Vector _rateRaDec;
        private static double _rightAscensionXForm;
        private static bool _rotate3DModel;
        private static double _slewSettleTime;
        private static double _siderealTime;
        private static bool _spiralChanged;
        private static Vector _targetRaDec;
        private static TrackingMode _trackingMode;
        //private static TrackingMode _prevTrackingMode;
        private static bool _tracking; //off
        private static bool _snapPort1Result;
        private static bool _snapPort2Result;
        private static double[] _steps;
        private static bool _flipOnNextGoto;
        private static bool _mountPositionUpdated;
        private static readonly object MountPositionUpdatedLock = new object();
        private static AzSlewMotionType _azSlewMotion;
        private static bool _canFlipAzimuthSide;
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
                double dX = Abs(m.X - h.X);
                dX = Min(dX, 360.0 - dX);   // Az Alt can have home (0, 0) so wrap at 360
                double dY = Abs(m.Y - h.Y);
                var d = new Vector(dX, dY);
                var r = d.LengthSquared < 0.01;
                // only report AtHome when slewing has finished
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
        /// Current azimuth axis side for slewing 
        /// </summary>
        public static AzSlewMotionType AzSlewMotion
        {
            get => _azSlewMotion;
            private set
            {
                if (_azSlewMotion == value) { return; }
                _azSlewMotion = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Can flip azimuth side state
        /// </summary>
        public static bool CanFlipAzimuthSide
        {
            get => _canFlipAzimuthSide;
            set
            {
                if (_canFlipAzimuthSide != value)
                {
                    _canFlipAzimuthSide = value;
                    OnStaticPropertyChanged();
                }
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
            set
            {
                if (_capabilities == value) { return; }
                _capabilities = value;
                OnStaticPropertyChanged();
            }
        }

        public static double ControllerVoltage
        {
            get
            {
                try
                {
                    var status = new SkyGetControllerVoltage(SkyQueue.NewId, AxisId.Axis1);
                    return SkyQueue.GetCommandResult(status).Result;
                }
                catch (Exception)
                {
                    return Double.NaN;
                }
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
                _declinationXForm = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Factor to covert steps, Sky Watcher in rad
        /// </summary>
        private static double[] FactorStep { get; set; }

        /// <summary>
        /// UI Checkbox option to flip on the next goto
        /// </summary>
        public static bool FlipOnNextGoto
        {
            get => _flipOnNextGoto;
            set
            {
                _flipOnNextGoto = value;

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
        /// Current Tab being viewed by the user
        /// </summary>
        public static Main.IPageVM SelectedTab { get; set; }

        private static bool _hcPulseDone;
        public static bool HcPulseDone
        {
            get => _hcPulseDone;
            set
            {
                _hcPulseDone = value;
                OnStaticPropertyChanged();
            }
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
                LoopCounter = 0;
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
        /// Alt Az uses both axes so always synchronous pulse guiding on one of Ra or Dec
        /// </summary>
        public static bool IsPulseGuiding => (IsPulseGuidingDec || IsPulseGuidingRa); 

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
        /// Count number of times server loop is executed
        /// </summary>
        public static ulong LoopCounter { get; private set; }

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
        /// Low voltage event from mount status
        /// </summary>
        public static bool LowVoltageEventState
        {
            get => _lowVoltageEventState;
            private set
            {
                _lowVoltageEventState = value;
                OnStaticPropertyChanged();
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
        /// Status of primary axis move
        /// </summary>
        public static bool MovePrimaryAxisActive => _rateMoveAxes.X != 0.0;

        /// <summary>
        /// Status of secondary axis move
        /// </summary>
        public static bool MoveSecondaryAxisActive => _rateMoveAxes.Y != 0.0;

        public static bool MoveAxisActive
        {
            get => _moveAxisActive;
            set
            {
                _moveAxisActive = value;
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

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{value.Name}|{value.X}|{value.Y}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Is Dec pulse guiding
        /// </summary>
        public static bool IsPulseGuidingDec
        {
            get => _isPulseGuidingDec;
            set
            {
                if (_isPulseGuidingDec != value)
                {
                    _isPulseGuidingDec = value;
                    // reset Dec pulse guiding cancellation token source
                    if (!_isPulseGuidingDec && _ctsPulseGuideDec != null)
                    {
                        _ctsPulseGuideDec?.Dispose();
                        _ctsPulseGuideDec = null;
                    }
                }
            }
        }

        /// <summary>
        /// Is Ra pulse guiding
        /// </summary>
        public static bool IsPulseGuidingRa
        {
            get => _isPulseGuidingRa;
            set
            {
                if (_isPulseGuidingRa != value)
                {
                    _isPulseGuidingRa = value;
                    // reset Ra pulse guiding cancellation token source
                    if (!_isPulseGuidingRa && _ctsPulseGuideRa != null)
                    {
                        _ctsPulseGuideRa?.Dispose();
                        _ctsPulseGuideRa = null;
                    }
                }
            }
        }

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
            }
        }

        /// <summary>
        /// Move Secondary axis at the given rate in degrees, MoveAxis
        /// Tracking if enabled:
        /// - is restored for the Secondary axis when MoveAxis is called with rate = 0
        /// - continues for the Primary axis unless it is also executing a MoveAxis command
        /// </summary>
        public static double RateMoveSecondaryAxis
        {
            private get => _rateMoveAxes.Y;
            set
            {
                if (Math.Abs(_rateMoveAxes.Y - value) < .0000000001) return;
                _rateMoveAxes.Y = value;
                CancelAllAsync();
                // Set slewing state
                SetRateMoveSlewState();
                // Move axis at requested rate
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdMoveAxisRate(0, Axis.Axis2, -_rateMoveAxes.Y);
                        break;
                    case MountType.SkyWatcher:
                        _ = new SkyAxisSlew(0, AxisId.Axis2, _rateMoveAxes.Y);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // Update tracking if required
                if (Tracking) SetTracking();

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateMoveAxes.Y}|{SkyTrackingOffset[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Move Primary axis at the given rate in degrees, MoveAxis
        /// Tracking if enabled:
        /// - is restored for the Primary axis when MoveAxis is called with rate = 0
        /// - continues for the Secondary axis unless it is also executing a MoveAxis command
        /// </summary>
        public static double RateMovePrimaryAxis
        {
            private get => _rateMoveAxes.X;
            set
            {
                if (Math.Abs(_rateMoveAxes.X - value) < 0.0000000001) return;
                _rateMoveAxes.X = value;
                CancelAllAsync();
                // Set slewing state
                SetRateMoveSlewState();
                // Move axis at requested rate
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        _ = new CmdMoveAxisRate(0, Axis.Axis1, _rateMoveAxes.X);
                        break;
                    case MountType.SkyWatcher:
                        _ = new SkyAxisSlew(0, AxisId.Axis1, _rateMoveAxes.X);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // Update tracking if required
                if (Tracking) SetTracking();
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateMoveAxes.X}|{SkyTrackingOffset[0]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Set/ reset tracking and slewing state whilst MoveAxis is active
        /// </summary>
        private static void SetRateMoveSlewState()
        {
            if (MovePrimaryAxisActive || MoveSecondaryAxisActive)
            {
                MoveAxisActive = true;
                IsSlewing = true;
                SlewState = SlewType.SlewMoveAxis;
            }
            if (!MovePrimaryAxisActive && !MoveSecondaryAxisActive)
            {
                MoveAxisActive = false;
                IsSlewing = false;
                SlewState = SlewType.SlewNone;
                if (Tracking) SkyPredictor.Set(RightAscensionXForm, DeclinationXForm);
            }
        }

        /// <summary>
        /// The declination tracking rate in degrees, DeclinationRate
        /// corrected direction applied
        /// </summary>
        public static double RateDec
        {
            get => _rateRaDec.Y;
            set
            {
                _rateRaDec.Y = value;
                ActionRateRaDec(); // Update the mount tracking rate

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Data,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_rateRaDec.Y}|{SkyTrackingOffset[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Store the original DeclinationRate to maintain direction
        /// </summary>
        public static double RateDecOrg { get; set; }

        /// <summary>
        /// The right ascension tracking in degrees, RightAscensionRate
        /// corrected direction applied
        /// </summary>
        public static double RateRa
        {
            get => _rateRaDec.X;
            set
            {
                _rateRaDec.X = value;
                ActionRateRaDec(); // Update the mount tracking rate

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
        /// Store the original RightAscensionRate and maintain direction
        /// Previous conversions were not exact
        /// </summary>
        public static double RateRaOrg { get; set; }

        /// <summary>
        /// Action Ra and Dec tracking rate offsets
        /// </summary>
        private static void ActionRateRaDec()
        {
            // If tracking is on then change the mount tracking rate
            if (Tracking)
            {
                if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                {
                    // get tracking target at time now
                    var raDec = SkyPredictor.GetRaDecAtTime(HiResDateTime.UtcNow);
                    // set predictor parameters ready for tracking
                    SkyPredictor.Set(raDec[0], raDec[1], _rateRaDec.X, _rateRaDec.Y);
                }
                SetTracking();
            }
            else
            {
                if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                {
                    // no tracking target so set to current position 
                    SkyPredictor.Set(RightAscensionXForm, DeclinationXForm, _rateRaDec.X, _rateRaDec.Y);
                }
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
                _rightAscensionXForm = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Trigger to rotate the 3D model
        /// </summary>
        public static bool Rotate3DModel
        {
            get => _rotate3DModel;
            set
            {
                _rotate3DModel = value;
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
                        $"SideOfPier ({value}) is outside the range of set Limits: {SkySettings.HourAngleLimit}");
                }
                MonitorLog.LogToMonitor(monitorItem);
                SlewAxes(b[0], b[1], SlewType.SlewMoveAxis);
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

        /// <summary>
        /// current micro steps, used to update SkyServer and UI
        /// </summary>
        private static double[] Steps
        {
            get => _steps;
                // return SkyPredictor.GetStepsAtTime();
            set
            {
                _steps = value;

                // Update steps interpolation data
                //SkyPredictor.AddStepsRecord(_steps, HiResDateTime.UtcNow);
                //var stepsAtTime = SkyPredictor.GetStepsAtTime();
                //if (!double.IsNaN(stepsAtTime[0])) _steps = stepsAtTime;

                //Implement Pec
                PecCheck();

                //Convert Positions to degrees
                // double[] rawPositions = { ConvertStepsToDegrees(rawSteps[0], 0), ConvertStepsToDegrees(rawSteps[1], 1) };
                var rawPositions = GetUnsyncedAxes(new[] { ConvertStepsToDegrees(_steps[0], 0), ConvertStepsToDegrees(_steps[1], 1) });


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
                var altAz = Axes.AxesXYToAzAlt(axes);
                Azimuth = altAz[0];
                Altitude = altAz[1];

                // Calculate top-o-centric Ra/Dec
                var raDec = Axes.AxesXYToRaDec(axes);
                RightAscension = raDec[0];
                Declination = raDec[1];

                // Calculate EquatorialSystem Property Ra/Dec for UI
                var xy = Transforms.InternalToCoordType(raDec[0], raDec[1]);
                RightAscensionXForm = xy.X;
                DeclinationXForm = xy.Y;

                OnStaticPropertyChanged();
            }
        }

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
        /// Dec target for slewing, epoch is same as EquatorialSystem Property
        /// convert to top-o-centric for any internal calculations
        /// </summary>
        public static double TargetDec
        {
            get => _targetRaDec.Y;
            set => _targetRaDec.Y = value;
        }

        /// <summary>
        /// Ra target for slewing, epoch is same as EquatorialSystem Property
        /// convert to top-o-centric for any internal calculations
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
        /// AltAzPredictor set / reset for Tracking true / false
        /// </summary>
        public static bool Tracking
        {
            get => _trackingMode != TrackingMode.Off;
            set
            {
                if (value == _tracking)
                {
                    OnStaticPropertyChanged();
                    return;
                } //off

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

                SkyPredictor.Reset();
                if (value)
                {
                    // Tracking on
                    if (AtPark)
                    {
                        if (TrackingSpeak) { Synthesizer.Speak(Application.Current.Resources["vceParked"].ToString()); }
                        throw new ASCOM.ParkedException(Application.Current.Resources["exParked"].ToString());
                    }
                    // Set tracking mode based on AlignmentMode and hemisphere
                    SetTrackingMode();
                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            AltAzTrackingMode = AltAzTrackingType.Predictor;
                            // Must have a tracking target for Alt Az otherwise just set the reference time to now
                            if (!SkyPredictor.RaDecSet)
                            {
                                SkyPredictor.Set(RightAscensionXForm, DeclinationXForm, 0, 0);
                            }
                            else
                            {
                                SkyPredictor.ReferenceTime = DateTime.Now;
                            }
                            if (TrackingSpeak) Synthesizer.Speak(Application.Current.Resources["vceTrackingOn"].ToString());
                            break;
                        case AlignmentModes.algGermanPolar:
                        case AlignmentModes.algPolar:
                            if (TrackingSpeak) Synthesizer.Speak(Application.Current.Resources["vceTrackingOn"].ToString());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // Tracking off
                    IsPulseGuidingDec = false; // Ensure pulses are off
                    IsPulseGuidingRa = false;
                    if (TrackingSpeak && _trackingMode != TrackingMode.Off) { Synthesizer.Speak(Application.Current.Resources["vceTrackingOff"].ToString()); }
                    _trackingMode = TrackingMode.Off;
                }
                _tracking = value; //off

                SetTracking();
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Has mount position been updated 
        /// </summary>
        public static bool MountPositionUpdated
        {
            get
            {
                lock (MountPositionUpdatedLock)
                {
                    return _mountPositionUpdated;
                }
            }
            set
            {
                lock (MountPositionUpdatedLock)
                {
                    _mountPositionUpdated = value;
                }
            }
        }

        /// <summary>
        /// Current Alt/Az tracking mode - RA/Dec predictor or calculated tracking rate
        /// </summary>
        public static AltAzTrackingType AltAzTrackingMode { get; set; }

        #endregion

        #region Simulator Items

        /// <summary>
        /// Sim GOTO slew
        /// </summary>
        /// <returns></returns>
        private static int SimGoTo(double[] target, bool trackingState, SlewType slewType, CancellationToken token)
        {
            const int returnCode = 0;
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

            token.ThrowIfCancellationRequested(); // check for a stop
            double[] simTarget;
            // convert target to axis for Ra / Dec slew
            if (slewType == SlewType.SlewRaDec)
            {
                var a = Axes.RaDecToAxesXY(target);
                simTarget = GetSyncedAxes(Axes.AxesAppToMount(a));
            }
            else
            {
                simTarget = GetSyncedAxes(Axes.AxesAppToMount(target));
            }
            const int timer = 120; //  stop slew after seconds
            var stopwatch = Stopwatch.StartNew();

            SimTasks(MountTaskName.StopAxes);

            #region First Slew
            token.ThrowIfCancellationRequested(); // check for a stop
            // time could be off a bit may need to deal with each axis separate
            // Convert az to plus / minus slew - AltAzAlignment mode only
            _ = new CmdAxisGoToTarget(0, Axis.Axis1, ConvertToAzEastWest(simTarget[0]));
            _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(50);
                token.ThrowIfCancellationRequested(); // check for a stop

                var statusx = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                var axis1Status = (AxisStatus)MountQueue.GetCommandResult(statusx).Result;
                var axis1Stopped = axis1Status.Stopped;

                Thread.Sleep(50);
                token.ThrowIfCancellationRequested(); // check for a stop

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
            token.ThrowIfCancellationRequested(); // check for a stop
            if (stopwatch.Elapsed.TotalSeconds <= timer)
                Task.Run(() => SimPrecisionGoto(target, slewType, token), token).Wait(token);
            #endregion

            SimTasks(MountTaskName.StopAxes);//make sure all axes are stopped
            return returnCode;
        }

        /// <summary>
        /// Performs a final precision slew of the axes to target if necessary.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slewType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static int SimPrecisionGoto(double[] target, SlewType slewType, CancellationToken token)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|({ActualAxisX},{ActualAxisY})|to|({target[0]},{target[1]})"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returnCode = 0;
            // var gotoPrecision = SkySettings.GotoPrecision;
            var maxtries = 0;
            double[] deltaDegree = { 0.0, 0.0 };
            double[] gotoPrecision = { ConvertStepsToDegrees(2, 0), ConvertStepsToDegrees(2, 1) };
            const double milliSeconds = 0.001;
            var deltaTime = 75 * milliSeconds; // 75mS for simulator slew

            while (true)
            {
                token.ThrowIfCancellationRequested(); // check for a stop
                if (maxtries > 5) { break; }
                maxtries++;
                double[] simTarget;

                // convert target to axis for Ra / Dec slew and calculate tracking rates
                if (slewType == SlewType.SlewRaDec)
                {
                    if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                    {
                        var nextTime = HiResDateTime.UtcNow.AddMilliseconds(deltaTime);
                        // get predicted RA and Dec at update time
                        var predictorRaDec = SkyPredictor.GetRaDecAtTime(nextTime);
                        // convert to internal Ra and Dec
                        var internalRaDec = Transforms.CoordTypeToInternal(predictorRaDec[0], predictorRaDec[1]);
                        // get alt az target
                        simTarget = Axes.RaDecToAxesXY(new[] { internalRaDec.X, internalRaDec.Y }, GetLocalSiderealTime(nextTime));
                        simTarget = GetSyncedAxes(Axes.AxesAppToMount(simTarget));
                    }
                    else
                    {
                        simTarget = Axes.RaDecToAxesXY(target);
                        simTarget = GetSyncedAxes(Axes.AxesAppToMount(simTarget));
                    }
                }
                else
                {
                    simTarget = GetSyncedAxes(Axes.AxesAppToMount(target));
                }

                // Calculate error
                var rawPositions = GetRawDegrees();
                if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1])) { break; }
                deltaDegree[0] = Range.Range180(simTarget[0] - rawPositions[0]);
                deltaDegree[1] = Range.Range180(simTarget[1] - rawPositions[1]);

                var axis1AtTarget = Math.Abs(deltaDegree[0]) < gotoPrecision[0];
                var axis2AtTarget = Math.Abs(deltaDegree[1]) < gotoPrecision[1];
                if (axis1AtTarget && axis2AtTarget) { break; }

                token.ThrowIfCancellationRequested(); // check for a stop
                if (!axis1AtTarget)
                {
                    // Convert az to plus / minus slew - AltAzAlignment mode only
                    simTarget[0] = ConvertToAzEastWest(simTarget[0]);
                    object _ = new CmdAxisGoToTarget(0, Axis.Axis1, simTarget[0]); //move to target RA / Az
                }
                token.ThrowIfCancellationRequested(); // check for a stop
                if (!axis2AtTarget)
                {
                    object _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTarget[1]); //move to target Dec / Alt
                }

                // track movement until axes are stopped
                var stopwatch1 = Stopwatch.StartNew();

                var axis1Stopped = false;
                var axis2Stopped = false;

                while (stopwatch1.Elapsed.TotalMilliseconds < 3000)
                {
                    Thread.Sleep(20);
                    token.ThrowIfCancellationRequested(); // check for a stop
                    if (!axis1Stopped)
                    {
                        var status1 = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                        var axis1Status = (AxisStatus)MountQueue.GetCommandResult(status1).Result;
                        axis1Stopped = axis1Status.Stopped;
                    }
                    Thread.Sleep(20);
                    token.ThrowIfCancellationRequested(); // check for a stop
                    if (!axis2Stopped)
                    {
                        var status2 = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                        var axis2Status = (AxisStatus)MountQueue.GetCommandResult(status2).Result;
                        axis2Stopped = axis2Status.Stopped;
                    }
                    if (axis1Stopped && axis2Stopped) { break; }
                }
                stopwatch1.Stop();
                deltaTime = stopwatch1.Elapsed.Milliseconds * milliSeconds;

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{Util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Delta|({deltaDegree[0]}, {deltaDegree[1]})|Seconds|{stopwatch1.Elapsed.TotalSeconds}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            return returnCode;
        }

        /// <summary>
        /// Performs a precision slew of axes to pulse target defined by RaDec predictor
        /// </summary>
        /// <param name="token"></param>
        private static void SimPulseGoto(CancellationToken token)
        {
            var maxTries = 0;
            double[] deltaDegree = { 0.0, 0.0 };
            var axis1AtTarget = false;
            var axis2AtTarget = false;
            double[] gotoPrecision = { ConvertStepsToDegrees(2, 0), ConvertStepsToDegrees(2, 1) };
            long loopTime = 75; // 75mS for simulator slew
            try
            {
                while (true)
                {
                    if (maxTries > 5) { break; }
                    maxTries++;
                    double[] simTarget = {0.0, 0.0};

                    // convert target to axis for Ra / Dec slew and calculate tracking rates
                    if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                    {
                        var nextTime = HiResDateTime.UtcNow.AddMilliseconds(loopTime);
                        // get predicted RA and Dec at update time
                        var predictorRaDec = SkyPredictor.GetRaDecAtTime(nextTime);
                        // convert to internal Ra and Dec
                        var internalRaDec = Transforms.CoordTypeToInternal(predictorRaDec[0], predictorRaDec[1]);
                        // get alt az target
                        simTarget = Axes.RaDecToAxesXY(new[] { internalRaDec.X, internalRaDec.Y }, GetLocalSiderealTime(nextTime));
                        simTarget = GetSyncedAxes(Axes.AxesAppToMount(simTarget));
                    }

                    // Calculate error
                    var rawPositions = GetRawDegrees();
                    if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1])) { break; }
                    deltaDegree[0] = Range.Range180(simTarget[0] - rawPositions[0]);
                    deltaDegree[1] = Range.Range180(simTarget[1] - rawPositions[1]);

                    axis1AtTarget = Math.Abs(deltaDegree[0]) < gotoPrecision[0] || axis1AtTarget;
                    axis2AtTarget = Math.Abs(deltaDegree[1]) < gotoPrecision[1] || axis2AtTarget;
                    if (axis1AtTarget && axis2AtTarget) { break; }
                    if (!axis1AtTarget)
                    {
                        simTarget[0] = ConvertToAzEastWest(simTarget[0]);
                        token.ThrowIfCancellationRequested();
                        object _ = new CmdAxisGoToTarget(0, Axis.Axis1, simTarget[0]); //move to target RA / Az
                    }
                    if (!axis2AtTarget)
                    {
                        token.ThrowIfCancellationRequested();
                        object _ = new CmdAxisGoToTarget(0, Axis.Axis2, simTarget[1]); //move to target Dec / Alt
                    }

                    // track movement until axes are stopped
                    var stopwatch1 = Stopwatch.StartNew();

                var axis1Stopped = false;
                var axis2Stopped = false;

                while (stopwatch1.Elapsed.TotalMilliseconds < 500)
                    {
                        token.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                        if (!axis1Stopped)
                        {
                            var status1 = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                            var axis1Status = (AxisStatus)MountQueue.GetCommandResult(status1).Result;
                            axis1Stopped = axis1Status.Stopped;
                        }
                        Thread.Sleep(100);
                        if (!axis2Stopped)
                        {
                            var status2 = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                            var axis2Status = (AxisStatus)MountQueue.GetCommandResult(status2).Result;
                            axis2Stopped = axis2Status.Stopped;
                        }
                        if (axis1Stopped && axis2Stopped) { break; }
                    }
                    stopwatch1.Stop();
                }
            }
            catch (OperationCanceledException)
            {
            }
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
                            var canHomeCmdA = new GetHomeSensorCapability(MountQueue.NewId);
                            bool.TryParse(Convert.ToString(MountQueue.GetCommandResult(canHomeCmdA).Result), out bool hasHome);
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
                            var a = Transforms.CoordTypeToInternal(TargetRa, TargetDec);
                            var xy = Axes.RaDecToAxesXY(new[] { a.X, a.Y });
                            var tarG = (SkySettings.AlignmentMode != AlignmentModes.algAltAz) ?
                                Axes.AxesAppToMount(new[] { xy[0], xy[1] }) : new[] { xy[0], xy[1] };
                            // Convert az to plus / minus slew - AltAzAlignment mode only
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, ConvertToAzEastWest(tarG[0]));
                            _ = new CmdAxisToDegrees(0, Axis.Axis2, tarG[1]);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] { _altAzSync.X, _altAzSync.Y });
                            var altaz = (SkySettings.AlignmentMode != AlignmentModes.algAltAz) ?
                                Axes.AxesAppToMount(new[] { yx[1], yx[0] }) : yx;
                            // Convert az to plus / minus slew - AltAzAlignment mode only
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, ConvertToAzEastWest(altaz[0]));
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
                            // Convert az to plus / minus slew - AltAzAlignment mode only
                            _ = new CmdAxisToDegrees(0, Axis.Axis1, ConvertToAzEastWest(_homeAxes.X));
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
            var siderealI = ratioFactor / SiderealRate;
            siderealI += SkySettings.CustomRaTrackingOffset;  //calc :I and add offset
            var newRate = ratioFactor / siderealI; //calc new rate from offset
            TrackingOffsetRaRate = SiderealRate - newRate;

            ratioFactor = (double)StepsTimeFreq[1] / StepsPerRevolution[1] * 1296000.0;  //generic factor for calc
            siderealI = ratioFactor / SiderealRate;
            siderealI += SkySettings.CustomDecTrackingOffset;  //calc :I and add offset
            newRate = ratioFactor / siderealI; //calc new rate from offset
            TrackingOffsetDecRate = SiderealRate - newRate;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{TrackingOffsetRaRate}|{TrackingOffsetDecRate}" };
            MonitorLog.LogToMonitor(monitorItem);

        }

        // used to combine multiple sources for a single slew rate
        // include tracking, hand controller, etc..
        private static Vector _skyHcRate;
        private static Vector _skyTrackingRate;
        private static readonly int[] SkyTrackingOffset = { 0, 0 }; // Store for custom mount :I offset

        /// <summary>
        /// combines multiple Ra and Dec rates for a single slew rate
        /// </summary>
        /// <returns></returns>
        private static Vector SkyGetRate()
        {
            var change = new Vector();

            change += _skyTrackingRate; // Tracking
            change += _skyHcRate; // Hand controller
            // Primary axis
            change.X += RateMovePrimaryAxis;
            change.X += SkySettings.AlignmentMode != AlignmentModes.algAltAz ? GetRaRateDirection(RateRa) : 0;
            // Secondary axis
            change.Y += RateMoveSecondaryAxis;
            change.Y += SkySettings.AlignmentMode != AlignmentModes.algAltAz ? GetDecRateDirection(RateDec) : 0;

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
        private static int SkyGoTo(double[] target, bool trackingState, SlewType slewType, CancellationToken token)
        {
            const int returnCode = 0;
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|{ActualAxisX}|{ActualAxisY}|to|{target[0]}|{target[1]}|tracking|{trackingState}|slewing|{slewType}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            token.ThrowIfCancellationRequested(); // check for a stop

            double[] skyTarget;
            // convert target to axis for Ra / Dec slew
            if (slewType == SlewType.SlewRaDec)
            {
                var a = Axes.RaDecToAxesXY(target);
                skyTarget = GetSyncedAxes(Axes.AxesAppToMount(a));
            }
            else
            {
                skyTarget = GetSyncedAxes(Axes.AxesAppToMount(target));
            }
            const int timer = 240; // stop goto after timer
            var stopwatch = Stopwatch.StartNew();

            SkyTasks(MountTaskName.StopAxes);

            #region First Slew
            token.ThrowIfCancellationRequested(); // check for a stop
            // time could be off a bit may need to deal with each axis separate
            // Convert az to plus / minus slew - AltAzAlignment mode only
            _ = new SkyAxisGoToTarget(0, AxisId.Axis1, ConvertToAzEastWest(skyTarget[0]));
            _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTarget[1]);

            while (stopwatch.Elapsed.TotalSeconds <= timer)
            {
                Thread.Sleep(50);
                token.ThrowIfCancellationRequested(); // check for a stop

                var statusx = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                var x = SkyQueue.GetCommandResult(statusx);
                var axis1Stopped = Convert.ToBoolean(x.Result);

                Thread.Sleep(50);
                token.ThrowIfCancellationRequested(); // check for a stop

                var statusy = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                var y = SkyQueue.GetCommandResult(statusy);
                var axis2Stopped = Convert.ToBoolean(y.Result);

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
                Message = $"Current|{Util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|{Util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Seconds|{stopwatch.Elapsed.TotalSeconds}|Target|{target[0]}|{target[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            #endregion

            #region Final precision slew
            token.ThrowIfCancellationRequested(); // check for a stop
            if (stopwatch.Elapsed.TotalSeconds <= timer)
                Task.Run(() => SkyPrecisionGoto(target, slewType, token)).Wait();
            #endregion

            SkyTasks(MountTaskName.StopAxes); //make sure all axes are stopped
            return returnCode;
        }

        /// <summary>
        /// Performs a final precision slew of the axes to target if necessary.
        /// On entry both axes are stopped from SkyGoTo
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slewType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static int SkyPrecisionGoto(double[] target, SlewType slewType, CancellationToken token)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"from|({ActualAxisX},{ActualAxisY})|to|({target[0]},{target[1]})"
            };
            MonitorLog.LogToMonitor(monitorItem);

            const int returnCode = 0;
            var maxtries = 0;
            double[] deltaDegree = { 0.0, 0.0 };
            var axis1AtTarget = false;
            var axis2AtTarget = false;

            // double[] gotoPrecision = { ConvertStepsToDegrees(2, 0), ConvertStepsToDegrees(2, 1) };
            double[] gotoPrecision = { SkySettings.GotoPrecision, SkySettings.GotoPrecision };
            long loopTime = 800;
            while (true)
            {
                token.ThrowIfCancellationRequested(); // check for a stop
                // start loop timer
                var loopTimer = Stopwatch.StartNew();
                // Update mount position
                MountPositionUpdated = false;
                UpdateSteps();
                while (!MountPositionUpdated) Thread.Sleep(10);
                // Check for maxtries or no change and exit
                if (maxtries >= 5) { break; }
                maxtries++;
                // convert target to axis for Ra / Dec slew and calculate tracking rates
                double[] skyTarget;
                if (slewType == SlewType.SlewRaDec)
                {
                    if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                    {
                        var nextTime = HiResDateTime.UtcNow.AddMilliseconds(loopTime);
                        // get predicted RA and Dec at update time 
                        var predictorRaDec = SkyPredictor.GetRaDecAtTime(nextTime);
                        // get required target position in topo coordinates
                        var internalRaDec = Transforms.CoordTypeToInternal(predictorRaDec[0], predictorRaDec[1]);
                        skyTarget = Axes.RaDecToAxesXY(new[] { internalRaDec.X, internalRaDec.Y }, GetLocalSiderealTime(nextTime));
                        skyTarget = GetSyncedAxes(skyTarget);
                    }
                    else
                    {
                        skyTarget = Axes.RaDecToAxesXY(target);
                        skyTarget = GetSyncedAxes(Axes.AxesAppToMount(skyTarget));
                    }
                }
                else
                {
                    skyTarget = GetSyncedAxes(Axes.AxesAppToMount(target));
                }
                // Calculate error
                var rawPositions = GetRawDegrees();
                deltaDegree[0] = Range.Range180(ConvertToAzEastWest(skyTarget[0]) - rawPositions[0]);
                deltaDegree[1] = Range.Range180(skyTarget[1] - rawPositions[1]);

                axis1AtTarget = Math.Abs(deltaDegree[0]) < gotoPrecision[0] || axis1AtTarget;
                axis2AtTarget = Math.Abs(deltaDegree[1]) < gotoPrecision[1] || axis2AtTarget;
                if (axis1AtTarget && axis2AtTarget) { break; }

                token.ThrowIfCancellationRequested(); // check for a stop
                if (!axis1AtTarget)
                {
                    skyTarget[0] += 0.25 * deltaDegree[0];
                    // Convert az to plus / minus slew - AltAzAlignment mode only
                    skyTarget[0] = ConvertToAzEastWest(skyTarget[0]);
                    object _ = new SkyAxisGoToTarget(0, AxisId.Axis1, skyTarget[0]); //move to target RA / Az
                }
                var axis1Done = axis1AtTarget;
                while (loopTimer.Elapsed.TotalMilliseconds < 3000)
                {
                    Thread.Sleep(30);
                    if (token.IsCancellationRequested) { break; } // check for a stop
                    if (!axis1Done)
                    {
                        var status1 = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                        axis1Done = Convert.ToBoolean(SkyQueue.GetCommandResult(status1).Result);
                    }
                    if (axis1Done) { break; }
                }

                if (!axis2AtTarget)
                {
                    skyTarget[1] += 0.1 * deltaDegree[1];
                    token.ThrowIfCancellationRequested(); // check for a stop
                    object _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTarget[1]); //move to target Dec / Alt
                }
                var axis2Done = axis2AtTarget;
                while (loopTimer.Elapsed.TotalMilliseconds < 3000)
                {
                    Thread.Sleep(30);
                    token.ThrowIfCancellationRequested(); // check for a stop
                    if (!axis2Done)
                    {
                        var status2 = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                        axis2Done = Convert.ToBoolean(SkyQueue.GetCommandResult(status2).Result);
                    }
                    if (axis2Done) { break; }
                }
                loopTimer.Stop();
                loopTime = loopTimer.ElapsedMilliseconds;

                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{Util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|" +
                        $"{Util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}" +
                        $"|Delta|{deltaDegree[0]}|{deltaDegree[1]}" +
                        $"|Seconds|{loopTimer.Elapsed.TotalSeconds}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            return returnCode;
        }

        /// <summary>
        /// Performs a precision slew of axes to pulse target defined by RaDec predictor
        /// </summary>
        /// <param name="token"></param>
        private static void SkyPulseGoto(CancellationToken token)
        {
            var maxTries = 0;
            double[] deltaDegree = { 0.0, 0.0 };
            var axis1AtTarget = false;
            var axis2AtTarget = false;

            // double[] gotoPrecision = { ConvertStepsToDegrees(2, 0), ConvertStepsToDegrees(2, 1) };
            double[] gotoPrecision = { SkySettings.GotoPrecision, SkySettings.GotoPrecision };
            long loopTime = 400;
            try
            {
                while (true)
                {
                    // start loop timer
                    var loopTimer = Stopwatch.StartNew();
                    // Update mount position
                    MountPositionUpdated = false;
                    UpdateSteps();
                    while (!MountPositionUpdated) Thread.Sleep(10);
                    // Check for maxtries or no change and exit
                    if (maxTries >= 5) { break; }
                    maxTries++;
                    double[] skyTarget = {0.0, 0.0};

                    // convert target to axis for Ra / Dec slew and calculate tracking rates
                    if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                    {
                        var nextTime = HiResDateTime.UtcNow.AddMilliseconds(loopTime);
                        // get predicted RA and Dec at update time
                        var predictorRaDec = SkyPredictor.GetRaDecAtTime(nextTime);
                        // convert to internal Ra and Dec
                        var internalRaDec = Transforms.CoordTypeToInternal(predictorRaDec[0], predictorRaDec[1]);
                        // get alt az target
                        skyTarget = Axes.RaDecToAxesXY(new[] { internalRaDec.X, internalRaDec.Y }, GetLocalSiderealTime(nextTime));
                        skyTarget = GetSyncedAxes(skyTarget);
                    }
                    // Calculate error
                    var rawPositions = GetRawDegrees();
                    if (rawPositions == null || double.IsNaN(rawPositions[0]) || double.IsNaN(rawPositions[1])) { break; }
                    deltaDegree[0] = skyTarget[0] - rawPositions[0];
                    deltaDegree[1] = skyTarget[1] - rawPositions[1];

                    axis1AtTarget = Math.Abs(deltaDegree[0]) < gotoPrecision[0] || axis1AtTarget;
                    axis2AtTarget = Math.Abs(deltaDegree[1]) < gotoPrecision[1] || axis2AtTarget;
                    if (axis1AtTarget && axis2AtTarget) { break; }

                    if (!axis1AtTarget)
                    {
                        // skyTarget[0] += trackingRate.X * loopTime;
                        // Convert az to plus / minus slew - AltAzAlignment mode only
                        skyTarget[0] = ConvertToAzEastWest(skyTarget[0]);// no 0.1*deltaDegree[0]
                        token.ThrowIfCancellationRequested();
                        object _ = new SkyAxisGoToTarget(0, AxisId.Axis1, skyTarget[0]); //move to target RA / Az
                    }
                    var axis1Done = axis1AtTarget;
                    while (loopTimer.Elapsed.TotalMilliseconds < 3000)
                    {
                        if (SlewState == SlewType.SlewNone) { break; }

                        Thread.Sleep(30);
                        token.ThrowIfCancellationRequested();

                        if (!axis1Done)
                        {
                            var status1 = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis1);
                            axis1Done = Convert.ToBoolean(SkyQueue.GetCommandResult(status1).Result);
                        }
                        if (axis1Done) { break; }
                    }

                    if (!axis2AtTarget)
                    {
                        token.ThrowIfCancellationRequested();
                        object _ = new SkyAxisGoToTarget(0, AxisId.Axis2, skyTarget[1]); //move to target Dec / Alt // no 0.1*deltaDegree[0]
                    }
                    var axis2Done = axis2AtTarget;
                    while (loopTimer.Elapsed.TotalMilliseconds < 3000)
                    {
                        if (SlewState == SlewType.SlewNone) { break; }

                        Thread.Sleep(30);
                        token.ThrowIfCancellationRequested();

                        if (!axis2Done)
                        {
                            var status2 = new SkyIsAxisFullStop(SkyQueue.NewId, AxisId.Axis2);
                            axis2Done = Convert.ToBoolean(SkyQueue.GetCommandResult(status2).Result);
                        }
                        if (axis2Done) { break; }
                    }
                    loopTimer.Stop();
                    loopTime = loopTimer.ElapsedMilliseconds;
                }
            }
            catch (OperationCanceledException)
            {
            }
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
                            var skyCanAdvanced = new SkyGetAdvancedCmdSupport(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(skyCanAdvanced).Result), out bool pAdvancedResult);
                            CanAdvancedCmdSupport = pAdvancedResult;
                            break;
                        case MountTaskName.CanPpec:
                            var skyMountCanPPec = new SkyCanPPec(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(skyMountCanPPec).Result), out bool pPecResult);
                            CanPPec = pPecResult;
                            break;
                        case MountTaskName.CanPolarLed:
                            var skyCanPolarLed = new SkyCanPolarLed(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(skyCanPolarLed).Result), out bool polarLedResult);
                            CanPolarLed = polarLedResult;
                            break;
                        case MountTaskName.CanHomeSensor:
                            var canHomeSky = new SkyCanHomeSensors(SkyQueue.NewId);
                            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(canHomeSky).Result), out bool homeSensorResult);
                            CanHomeSensor = homeSensorResult;
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
                            var ppeOcn = new SkySetPPec(SkyQueue.NewId, AxisId.Axis1, SkySettings.PPecOn);
                            var pPecOnStr = (string)SkyQueue.GetCommandResult(ppeOcn).Result;
                            if (string.IsNullOrEmpty(pPecOnStr))
                            {
                                SkySettings.PPecOn = false;
                                break;
                            }
                            if (pPecOnStr.Contains("!")) { SkySettings.PPecOn = false; }
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
                            var a = Transforms.CoordTypeToInternal(TargetRa, TargetDec);
                            var xy = Axes.RaDecToAxesXY(new[] { a.X, a.Y });
                            var targ = (SkySettings.AlignmentMode != AlignmentModes.algAltAz) ?
                                Axes.AxesAppToMount(new[] { xy[0], xy[1] }) : new[] { xy[0], xy[1] };
                            _ = new SkySyncAxis(0, AxisId.Axis1, ConvertToAzEastWest(targ[0]));
                            _ = new SkySyncAxis(0, AxisId.Axis2, targ[1]);
                            monitorItem.Message += $",{Util.HoursToHMS(a.X, "h ", ":", "", 2)}|{Util.DegreesToDMS(a.Y, "° ", ":", "", 2)}|{xy[0]}|{xy[1]}|{targ[0]}|{targ[1]}";
                            MonitorLog.LogToMonitor(monitorItem);
                            break;
                        case MountTaskName.SyncAltAz:
                            var yx = Axes.AltAzToAxesYX(new[] { _altAzSync.X, _altAzSync.Y });
                            var altaz = (SkySettings.AlignmentMode != AlignmentModes.algAltAz) ?
                                Axes.AxesAppToMount(new[] { yx[1], yx[0] }) : yx;
                            _ = new SkySyncAxis(0, AxisId.Axis1, ConvertToAzEastWest(altaz[0]));
                            _ = new SkySyncAxis(0, AxisId.Axis2, altaz[1]);
                            monitorItem.Message += $",{_altAzSync.Y}|{_altAzSync.X}|{yx[1]}|{yx[0]}|{altaz[0]}|{altaz[1]}";
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
                            var skyMountRevolutions = new SkyGetStepsPerRevolution(SkyQueue.NewId);
                            StepsPerRevolution = (long[])SkyQueue.GetCommandResult(skyMountRevolutions).Result;
                            break;
                        case MountTaskName.StepsWormPerRevolution:
                            var skyWormRevolutions1 = new SkyGetPecPeriod(SkyQueue.NewId, AxisId.Axis1);
                            StepsWormPerRevolution[0] = (double)SkyQueue.GetCommandResult(skyWormRevolutions1).Result;
                            var skyWormRevolutions2 = new SkyGetPecPeriod(SkyQueue.NewId, AxisId.Axis2);
                            StepsWormPerRevolution[1] = (double)SkyQueue.GetCommandResult(skyWormRevolutions2).Result;
                            break;
                        case MountTaskName.StepTimeFreq:
                            var skyStepTimeFreq = new SkyGetStepTimeFreq(SkyQueue.NewId);
                            StepsTimeFreq = (long[])SkyQueue.GetCommandResult(skyStepTimeFreq).Result;
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
                    Message = $"{command.Successful}|{command.Exception.Message}|{command.Exception.StackTrace}"
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
            var tracking = Tracking || SlewState == SlewType.SlewRaDec || MoveAxisActive;
            Tracking = false; //added back in for spec "Tracking is returned to its pre-slew state"
            CancelAllAsync();
            // Stop all MoveAxis commands
            MoveAxisActive = false;
            RateMovePrimaryAxis = 0.0;
            RateMoveSecondaryAxis = 0.0;
            _rateRaDec = new Vector(0, 0);
            SlewState = SlewType.SlewNone;

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
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                AxesStopValidate();
                // wait for the move to stop - physical overrun
                //var trackingRate = SkyGetRate();
                AxesRateOfChange.Reset();
                do
                {
                    // Update mount velocity
                    MountPositionUpdated = false;
                    UpdateSteps();
                    while (!MountPositionUpdated) Thread.Sleep(50);
                    AxesRateOfChange.Update(_actualAxisX, _actualAxisY, HiResDateTime.UtcNow);
                } while (AxesRateOfChange.AxisVelocity.Length > 0);
                SkyPredictor.Set(RightAscensionXForm, DeclinationXForm);
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
                    Method = MonitorLog.GetCurrentMethod(),
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Started"
                };
                MonitorLog.LogToMonitor(monitorItem);

                var returnCode1 = 0;
                var returnCode2 = 0;
                if (degreeLimit < 20) { degreeLimit = 100; }
                AutoHomeProgressBar = 0;
                var encoderTemp = SkySettings.Encoders;
                if (Tracking) { Tracking = false; }
                Synthesizer.Speak(Application.Current.Resources["btnAutoHomeStart"].ToString());
                Synthesizer.VoicePause = true;

                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        var autoSim = new AutoHomeSim();
                        returnCode1 = await Task.Run(() => autoSim.StartAutoHome(Axis.Axis1, degreeLimit));
                        AutoHomeProgressBar = 50;
                        returnCode2 = await Task.Run(() => autoSim.StartAutoHome(Axis.Axis2, degreeLimit, offSetDec));
                        break;
                    case MountType.SkyWatcher:
                        var autosSky = new AutoHomeSky();
                        returnCode1 = await Task.Run(() => autosSky.StartAutoHome(AxisId.Axis1, degreeLimit));
                        AutoHomeProgressBar = 50;
                        returnCode2 = await Task.Run(() => autosSky.StartAutoHome(AxisId.Axis2, degreeLimit, offSetDec));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // put encoder setting back
                SkySettings.Encoders = encoderTemp;
                string msgCode1 = null;
                switch (returnCode1)
                {
                    case 0:
                        //good
                        break;
                    case -1:
                        msgCode1 = "RA failed home sensor reset";
                        break;
                    case -2:
                        msgCode1 = "RA home sensor not found";
                        break;
                    case -3:
                        //stop requested
                        break;
                    case -4:
                        msgCode1 = "RA too many restarts";
                        break;
                    case -5:
                        msgCode1 = "RA home capability check failed";
                        break;
                    default:
                        msgCode1 = "Ra code not found";
                        break;
                }

                string msgcode2 = null;
                switch (returnCode2)
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
                    Method = MonitorLog.GetCurrentMethod(),
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Complete: {returnCode1}|{returnCode2}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (returnCode1 == 0 && returnCode2 == 0)
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
                    if (returnCode1 == -3 || returnCode2 == -3) return;
                    var ex = new Exception($"Incomplete: {msgCode1} ({returnCode1}), {msgcode2}({returnCode2})");
                    LastAutoHomeError = ex;
                    throw ex;
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
                    Method = MonitorLog.GetCurrentMethod(),
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
                        var statusX = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                        var axis1Status = (AxisStatus)MountQueue.GetCommandResult(statusX).Result;
                        axis1Stopped = axis1Status.Stopped;

                        var statusY = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                        var axis2Status = (AxisStatus)MountQueue.GetCommandResult(statusY).Result;
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
        /// Checks the axis limits. Polar mounts allow continuous movement,
        /// AltAz checks for elevation limits and azimuth slewing limits through 180
        /// GEM mounts check the hour angle limit.
        /// </summary>
        private static void CheckAxisLimits()
        {
            var limitHit = false;
            var meridianLimit = false;
            var horizonLimit = false;
            //var msg = string.Empty;
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Warning,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = string.Empty
            };

            //Meridian Limit Test,  combine flip angle and tracking limit for a total limit passed meridian
            var totLimit = SkySettings.HourAngleLimit + SkySettings.AxisTrackingLimit;

            // check the ranges of the axes primary axis must be in the range 0 to 360 for AltAz or Polar
            // and -hourAngleLimit to 180 + hourAngleLimit for german polar
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    // the primary (azimuth) axis must be in the range 0 to 360
                    // _mountAxes.X = Range.Range360(_mountAxes.X);
                    // the secondary (altitude) axis must always be in the range -90 to +90
                    // _mountAxes.Y = Range.Range90(_mountAxes.Y);
                    // the secondary (altitude) axis range can be limited
                    if (SkySettings.AltAzAxesLimitOn)
                    {
                        // Check altitude
                        if ((_mountAxes.Y < SkySettings.AltAxisLowerLimit - 1.0) ||
                            (_mountAxes.Y > SkySettings.AltAxisUpperLimit + 1.0))
                        {
                            limitHit = true;
                            meridianLimit = true;
                        }

                        limitHit = limitHit || AzEastWestSlewAtLimit(_mountAxes.X);
                        meridianLimit = meridianLimit || AzEastWestTrackAtLimit(_mountAxes.X);
                    }

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
                            meridianLimit = true;
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
                            meridianLimit = true;
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

            // Horizon Limit Test
            if (SkySettings.HzLimitPark || SkySettings.HzLimitTracking) // Skip all if set to do nothing
            {
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                        break;
                    case AlignmentModes.algGermanPolar:
                        if (SideOfPier == PierSide.pierEast && Altitude <= SkySettings.AxisHzTrackingLimit && Tracking)
                        {
                            limitHit = true;
                            horizonLimit = true;
                        }

                        break;
                    case AlignmentModes.algPolar:
                        break;
                }
            }

            // Set the warning indicator light
            LimitAlarm = limitHit;

            // Meridian Triggers
            if (meridianLimit)
            {
                monitorItem.Message =
                    $"Meridian Limit Alarm: Park: {SkySettings.LimitPark} | Position: {SkySettings.ParkLimitName} | Stop Tracking: {SkySettings.LimitTracking}";
                MonitorLog.LogToMonitor(monitorItem);

                if (Tracking && SkySettings.LimitTracking)
                {
                    Tracking = false;
                } // turn off tracking

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
            // Horizon Triggers
            if (horizonLimit)
            {
                monitorItem.Message =
                    $"Horizon Limit Alarm: Park: {SkySettings.HzLimitPark} | Position:{SkySettings.ParkHzLimitName} | Stop Tracking:{SkySettings.HzLimitTracking}";
                MonitorLog.LogToMonitor(monitorItem);

                if (Tracking && SkySettings.HzLimitTracking)
                {
                    Tracking = false;
                } // turn off tracking

                if (SkySettings.HzLimitPark && SlewState != SlewType.SlewPark) // only hit this once while in limit
                {
                    var found = SkySettings.ParkPositions.Find(x => x.Name == SkySettings.ParkHzLimitName);
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

            if ((Math.Abs(RateMovePrimaryAxis) + Math.Abs(RateMoveSecondaryAxis)) > 0) { slewing = true; }
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

        ///// <summary>
        ///// Convert the move rate in hour angle and declination to a move rate in altitude and azimuth
        ///// </summary>
        ///// <param name="haRate">The ha rate.</param>
        ///// <param name="decRate">The dec rate </param>
        ///// <returns></returns>
        //private static Vector ConvertRateToAltAz(double haRate, double decRate)
        //{
        //    return ConvertRateToAltAz(haRate, decRate, TargetDec);
        //}

        /// <summary>
        /// Convert the move rate in hour angle and declination to a move rate in altitude and azimuth
        /// </summary>
        /// <param name="haRate">The ha rate.</param>
        /// <param name="decRate">The dec rate </param>
        /// <param name="targetDec"></param>
        /// <returns></returns>
        private static Vector ConvertRateToAltAz(double haRate, double decRate, double targetDec)
        {
            var change = new Vector(0,0);
            if (double.IsNaN(targetDec)) { return change; }

            var azimuthRate = new Vector(); // [X,Y] = [ha, dec]
            var altitudeRate = new Vector(); // [X,Y] = [ha, dec]

            var latRad = Principles.Units.Deg2Rad(SkySettings.Latitude);
            var azmRad = Principles.Units.Deg2Rad(Azimuth);
            var haRad = Principles.Units.Hrs2Rad(Lha);
            var decRad = Principles.Units.Deg2Rad(targetDec);
            var zenithAngle = Principles.Units.Deg2Rad((90 - Altitude)); // in radians

            // get the azimuth and altitude geometry factors for changing ha
            altitudeRate.X = Sin(azmRad) * Cos(latRad);
            // fails at zenith so set a very large value, the limit check will trap this
            azimuthRate.X =
                Abs(Altitude - 90.0) > 0
                    ? (Sin(latRad) -
                       Cos(latRad) * Cos(azmRad) / Tan(zenithAngle))
                    :
                    //Abs(Altitude - 90.0) > 0
                    //    ? (Sin(latRad) * Sin(zenithAngle) -
                    //       Cos(latRad) * Cos(zenithAngle) * Cos(azmRad)) / Sin(zenithAngle)
                    //_altAzm.Y != 90.0 ?(Math.Sin(latRad) * Math.Sin(zenithAngle) - Math.Cos(latRad) * Math.Cos(zenithAngle) * Math.Cos(azmRad)) / Math.Sin(zenithAngle) :
                    Azimuth >= 90 && Azimuth <= 270
                        ? 10000
                        : -10000;

            // get the azimuth and altitude geometry factors for changing dec
            // fails at zenith so set a very large value, the limit check will trap this
            altitudeRate.Y =
                Abs(Altitude - 90.0) > 0
                ? (Sin(decRad) * Sin(latRad) -
                   Sin(decRad) * Cos(haRad) * Cos(latRad)) / Sin(zenithAngle)
                :
                Azimuth >= 90 && Azimuth <= 270
                    ? 10000
                    : -10000;
            // fails at zenith so set a very large value, the limit check will trap this
            azimuthRate.Y =
                Abs(Altitude - 90.0) > 0
                ? (Sin(zenithAngle) * Sin(haRad) * Cos(decRad) +
                   Sin(decRad) * Cos(haRad) * Cos(latRad) -
                   Cos(decRad) * Sin(latRad)) /
                  ((Sin(decRad) * Cos(latRad) -
                   Cos(decRad) * Cos(haRad) * Sin(latRad)) * Sin(zenithAngle))
                :
                Azimuth >= 90 && Azimuth <= 270
                    ? 10000
                    : -10000;

            // calculate the rate of change in altitude and azimuth using the hour angle and dec change rate and geometry factors.
            change.Y = altitudeRate.X * haRate + altitudeRate.Y * decRate;
            change.X = azimuthRate.X * haRate + azimuthRate.Y * decRate;
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
        /// Cycles tracking
        /// </summary>
        /// <param name="silence">turns off voice</param>
        /// <remarks>planetarium programs fix which doesn't turn on tracking before a goto</remarks>
        public static void CycleOnTracking(bool silence)
        {
            if (silence) { TrackingSpeak = false; }

            // Tracking = false;
            Tracking = true;

            if (silence) { TrackingSpeak = true; }
        }

        /// <summary>
        /// Event handler for timed update AltAz tracking
        /// </summary>
        private static void AltAzTrackingTimerEvent(object sender, EventArgs e)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MonitorLog.GetCurrentMethod(),
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"TimerID|{_altAzTrackingTimer?.TimerID}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            // timer must be running to update tracking
            if (_altAzTrackingTimer?.IsRunning == true)
            {
                // handle timer race condition triggering handler
                if (Interlocked.CompareExchange(ref _altAzTrackingLock, -1, 0) == 0)
                {
                    SetTracking();
                    // Release the lock
                    _altAzTrackingLock = 0;
                }
            }
        }

        /// <summary>
        /// Stop Alt Az tracking timer
        /// </summary>
        private static void StopAltAzTrackingTimer()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MonitorLog.GetCurrentMethod(),
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"TimerID|{_altAzTrackingTimer?.TimerID}|Running|{_altAzTrackingTimer?.IsRunning}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            if (_altAzTrackingTimer != null)
            {
                _altAzTrackingTimer.Tick -= AltAzTrackingTimerEvent;
                if (_altAzTrackingTimer.IsRunning)
                {
                    _altAzTrackingTimer.Stop();
                }
                _altAzTrackingTimer.Dispose();
                _altAzTrackingTimer = null;
            }
        }

        /// <summary>
        /// Start Alt Az tracking timer
        /// </summary>
        private static void StartAltAzTrackingTimer()
        {
            var timerId = _altAzTrackingTimer?.TimerID;
            _altAzTrackingTimer = new MediaTimer
            {
                Period = SkySettings.AltAzTrackingUpdateInterval
            };
            _altAzTrackingTimer.Tick += AltAzTrackingTimerEvent;
            _altAzTrackingTimer.Start();
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MonitorLog.GetCurrentMethod(),
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"TimerID on entry|{timerId}|TimerID|{_altAzTrackingTimer?.TimerID}|Running|{_altAzTrackingTimer?.IsRunning}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Alt Az timer is running property
        /// </summary>
        private static bool AltAzTimerIsRunning => _altAzTrackingTimer?.IsRunning == true;

        //private static double[] _lastAltAzTarget = new[] { double.NaN, double.NaN, };

        /// <summary>
        /// Update AltAz tracking rates including delta for tracking error
        /// </summary>
        private static void SetAltAzTrackingRates(AltAzTrackingType altAzTrackingType)
        {
            switch (altAzTrackingType)
            {
                case AltAzTrackingType.Predictor:
                    double[] delta = { 0.0, 0.0 };
                    if (SkyPredictor.RaDecSet)
                    {
                        // Update mount position
                        MountPositionUpdated = false;
                        UpdateSteps();
                        while (!MountPositionUpdated) Thread.Sleep(10);
                        var steps = Steps;
                        DateTime nextTime = HiResDateTime.UtcNow.AddMilliseconds(SkySettings.AltAzTrackingUpdateInterval);
                        var raDec = SkyPredictor.GetRaDecAtTime(nextTime);
                        // get required target position in topo coordinates
                        var internalRaDec = Transforms.CoordTypeToInternal(raDec[0], raDec[1]);
                        var skyTarget = Axes.RaDecToAxesXY(new[] { internalRaDec.X, internalRaDec.Y }, GetLocalSiderealTime(nextTime));
                        skyTarget = GetSyncedAxes(skyTarget);
                        var rawPositions = new[] { ConvertStepsToDegrees(steps[0], 0), ConvertStepsToDegrees(steps[1], 1) };
                        delta[0] = Range.Range180((skyTarget[0] - rawPositions[0]));
                        delta[1] = Range.Range180((skyTarget[1] - rawPositions[1]));
                        const double milliSecond = 0.001;
                        _skyTrackingRate.X = delta[0] / (SkySettings.AltAzTrackingUpdateInterval * milliSecond);
                        _skyTrackingRate.Y = delta[1] / (SkySettings.AltAzTrackingUpdateInterval * milliSecond);
                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Server,
                            Category = MonitorCategory.Server,
                        Type = MonitorType.Data,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Ra:{internalRaDec.X}|Dec:{internalRaDec.Y}|Azimuth delta:{delta[0]}|Altitude delta:{delta[1]}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                    break;
                case AltAzTrackingType.Rate:
                    _skyTrackingRate = ConvertRateToAltAz(CurrentTrackingRate(), 0.0, DeclinationXForm);
                    break;
            }
        }

        /// <summary>
        /// Cancel all currently executing async operations
        /// </summary>
        public static void CancelAllAsync()
        {
            if (_ctsGoTo != null || _ctsPulseGuideDec != null || _ctsPulseGuideRa != null || _ctsHcPulseGuide != null)
            {
                _ctsGoTo?.Cancel();
                _ctsPulseGuideDec?.Cancel();
                _ctsPulseGuideRa?.Cancel();
                _ctsHcPulseGuide?.Cancel();
                var sw = Stopwatch.StartNew();
                while (_ctsGoTo != null &&_ctsPulseGuideDec != null && _ctsPulseGuideRa != null && _ctsHcPulseGuide != null && sw.ElapsedMilliseconds< 2000)
                    Thread.Sleep(200); // wait for any pending pulse guide operations to wake up and cancel
            }
        }

        /// <summary>
/// Calculates the current RA tracking rate used in arc seconds per second
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

            if (rate < SiderealRate * 2 & rate != 0) //add any custom gearing offset
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

        /// <summary>
        /// Determine which SideOfPier for a given Ra/Dec coordinate
        /// </summary>
        /// <remarks>Ra/Dec must already be converted using Transforms.CordTypeToInternal.</remarks>
        /// <remarks>Checks if a flip it required for coordinates</remarks>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static PierSide DetermineSideOfPier(double rightAscension, double declination)
        {
            var sop = SideOfPier;
            if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
            {
                return PierSide.pierUnknown;
            }

            var flipReq = Axes.IsFlipRequired(new[] { rightAscension, declination });

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Ra:{rightAscension}|Dec:{declination}|Flip:{flipReq}|SoP:{sop}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            switch (sop)
            {
                case PierSide.pierEast:
                    return flipReq ? PierSide.pierWest : PierSide.pierEast;
                case PierSide.pierWest:
                    return flipReq ? PierSide.pierEast : PierSide.pierWest;
                case PierSide.pierUnknown:
                    return PierSide.pierUnknown;
                default:
                    return PierSide.pierUnknown;
            }
        }

        ///// <summary>
        ///// Gets the side of pier using the right ascension, assuming it depends on the
        ///// hour angle only.  Used for Destination side of Pier, NOT to determine the mount
        ///// pointing state
        ///// </summary>
        ///// <param name="rightAscension">The right ascension.</param>
        ///// <returns></returns>
        //public static PierSide SideOfPierRaDec(double rightAscension)
        //{
        //    if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
        //    {
        //        return PierSide.pierUnknown;
        //    }
        //    var ha = Coordinate.Ra2Ha12(rightAscension, SiderealTime);
        //    PierSide sideOfPier;
        //    if (ha < 0.0 && ha >= -12.0) { sideOfPier = PierSide.pierWest; }
        //    else if (ha >= 0.0 && ha <= 12.0) { sideOfPier = PierSide.pierEast; }
        //    else { sideOfPier = PierSide.pierUnknown; }
        //    return sideOfPier;
        //}
        ///// <summary>
        ///// Gets pierSide based on the Right Ascension (HA) using only the meridian or by adding in the Angle limit
        ///// Used for Destination side of Pier.  
        ///// </summary>
        ///// <param name="rightAscension">The right ascension.</param>
        ///// <returns>PierSide</returns>
        //public static PierSide SideOfPierRaDec1(double rightAscension)
        //{
        //    if (SkySettings.AlignmentMode != AlignmentModes.algGermanPolar)
        //    {
        //        return PierSide.pierUnknown;
        //    }
        //    var limit = 0.0;
        //    if (UseLimitInDSoP) // new user setting
        //    {
        //        limit = SkySettings.HourAngleLimit / 15;
        //    }
        //    var ha = Coordinate.Ra2Ha12(rightAscension, SiderealTime);
        //    PierSide sideOfPier;
        //    if (ha < (0.0 + limit) && ha >= -12.0) { sideOfPier = PierSide.pierWest; }
        //    else if (ha >= (0.0 + limit) && ha <= 12.0) { sideOfPier = PierSide.pierEast; }
        //    else { sideOfPier = PierSide.pierUnknown; }
        //    return sideOfPier;
        //}

        /// <summary>
        /// Evaluate and return slew motion state used by Alt Az slewing
        /// </summary>
        /// <returns></returns>
        private static AzSlewMotionType GetAzSlewMotion()
        {
            AzSlewMotionType azSlewMotion = AzSlewMotion;
            if (_actualAxisX >= 0.0)
            {
                azSlewMotion = AzSlewMotionType.East;
            }
            if (_actualAxisX < 0.0)
            {
                azSlewMotion = AzSlewMotionType.West;
            }
            return azSlewMotion;
        }

        /// <summary>
        /// Set mechanical direction for dec rate
        /// Positive direction mean go mechanical north
        /// </summary>
        /// <returns></returns>
        private static double GetDecRateDirection(double rate)
        {
            var north = rate > 0;
            rate = Math.Abs(rate);
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    switch (SideOfPier)
                    {
                        case PierSide.pierEast:
                            if (SouthernHemisphere)
                            {
                                if (north) { rate = -rate; }
                            }
                            else
                            {
                                if (!north) { rate = -rate; }
                            }
                            break;
                        case PierSide.pierUnknown:
                            break;
                        case PierSide.pierWest:
                            if (SouthernHemisphere)
                            {
                                if (!north) { rate = -rate; }
                            }
                            else
                            {
                                if (north) { rate = -rate; }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case MountType.SkyWatcher:
                    switch (SideOfPier)
                    {
                        case PierSide.pierEast:
                            if (SouthernHemisphere)
                            {
                                if (north) { rate = -rate; }
                            }
                            else
                            {
                                if (north) { rate = -rate; }
                            }
                            break;
                        case PierSide.pierUnknown:
                            break;
                        case PierSide.pierWest:
                            if (SouthernHemisphere)
                            {
                                if (!north) { rate = -rate; }
                            }
                            else
                            {
                                if (!north) { rate = -rate; }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rate;
        }

        /// <summary>
        /// Set mechanical direction for ra rate
        /// Positive direction mean go mechanical east
        /// </summary>
        /// <returns></returns>
        private static double GetRaRateDirection(double rate)
        {
            var east = rate > 0;
            rate = Math.Abs(rate);

            if (SouthernHemisphere)
            {
                if (!east) { rate = -rate; }
            }
            else
            {
                if (east) { rate = -rate; }
            }

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
                    _homeAxes.X = 90;
                    _homeAxes.Y = 90;
                    break;
                case AlignmentModes.algAltAz:
                    _homeAxes.X = 0;
                    _homeAxes.Y = 0;
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
        /// Main get for the Steps
        /// </summary>
        /// <returns></returns>
        public static void UpdateSteps()
        {
            if (!IsMountRunning) { return; }

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    _ = new CmdAxesSteps(MountQueue.NewId);
                    break;
                case MountType.SkyWatcher:
                    _ = new SkyUpdateSteps(SkyQueue.NewId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
        /// <param name="tracking"></param>
        private static void GoToAsync(double[] target, SlewType slewState, bool tracking = false)
        {
            MonitorEntry monitorItem;
            if (!IsMountRunning)
            {
                return;
            }

            CancelAllAsync();

            while (_ctsGoTo != null) Thread.Sleep(10);

            if (IsSlewing)
            {
                SlewState = SlewType.SlewNone;
                var stopped = AxesStopValidate();
                if (!stopped)
                {
                    AbortSlew(true);
                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = MonitorLog.GetCurrentMethod(),
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Timeout stopping axes"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    return;
                }
            }

            SlewState = slewState;
            var startingState = slewState;
            // Planetarium fix to set Tracking for non-ASCOM compliant programs - set true by GoToCoordinatesAsync()
            var trackingState = tracking || Tracking;
            TrackingSpeak = false;
            Tracking = false;
            if (slewState == SlewType.SlewRaDec)
            {
                SkyPredictor.Set(TargetRa, TargetDec, RateRa, RateDec); // 
            }
            IsSlewing = true;

            // Assume fail
            try
            {
                _ctsGoTo = new CancellationTokenSource();
                var returnCode = 1;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        returnCode = SimGoTo(target, trackingState, slewState, _ctsGoTo.Token);
                        break;
                    case MountType.SkyWatcher:
                        returnCode = SkyGoTo(target, trackingState, slewState, _ctsGoTo.Token);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }

                TrackingSpeak = false;

                if (returnCode == 0)
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
                        case SlewType.SlewSettle:
                        case SlewType.SlewMoveAxis:
                            break;
                        case SlewType.SlewRaDec:
                            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                            {
                                // update TargetRa and TargetDec after slewing with offset rates as per ASCOM spec
                                if (SkyPredictor.RatesSet)
                                {
                                    var targetRaDec = SkyPredictor.GetRaDecAtTime(HiResDateTime.UtcNow);
                                    TargetRa = targetRaDec[0];
                                    TargetDec = targetRaDec[1];
                                }

                                // use tracking to complete slew for Alt Az mounts
                                SkyPredictor.Set(TargetRa, TargetDec);
                                _tracking = true;
                                _trackingMode = TrackingMode.AltAz;
                                SetTracking();
                                var sw = Stopwatch.StartNew();
                                // wait before completing async slew
                                while (sw.ElapsedMilliseconds < 2 * SkySettings.AltAzTrackingUpdateInterval)
                                {
                                    if (_ctsGoTo?.IsCancellationRequested == true)
                                    {
                                        // Stop current Alt Az tracking timed action
                                        StopAltAzTrackingTimer();
                                        // Prevent re-enabling by this thread
                                        trackingState = false;
                                        // Stop tracking motion 
                                        StopAxes();
                                        break;
                                    }
                                    else
                                        Thread.Sleep(100);
                                }
                            }

                            break;
                        case SlewType.SlewAltAz:
                            break;
                        case SlewType.SlewPark:
                            trackingState = false;
                            AtPark = true;
                            SkyPredictor.Reset();
                            break;
                        case SlewType.SlewHome:
                            trackingState = false;
                            SkyPredictor.Reset();
                            break;
                        case SlewType.SlewHandpad:
                            // ensure tracking if enabled has the correct target
                            SkyPredictor.Set(RightAscensionXForm, DeclinationXForm);
                            break;
                        case SlewType.SlewComplete:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MonitorLog.GetCurrentMethod(),
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message =
                            $"{SlewState} finished|code|{returnCode}|{Util.HoursToHMS(RightAscensionXForm, "h ", ":", "", 2)}|{Util.DegreesToDMS(DeclinationXForm, "° ", ":", "", 2)}|Actual|{ActualAxisX}|{ActualAxisY}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    SlewState = SlewType.SlewNone;
                    SpeakSlewEnd(startingState);
                    Tracking = trackingState;
                    TrackingSpeak = true;
                }
                else
                {
                    // Handle can't slew    
                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "GoTo coordinates outside axes limits"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    SlewState = SlewType.SlewNone;
                    SpeakSlewEnd(startingState);
                    Tracking = false;
                    TrackingSpeak = true;
                }
            }
            catch (Exception ex)
            {
                // OperationCanceledException thrown by SimGoTo or SkyGoTo
                // AggregateException with base OperationCanceledException thrown by PrecisionGoTo
                var cancelled = ex is OperationCanceledException || ex.GetBaseException() is OperationCanceledException;
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Warning,
                    Method = MonitorLog.GetCurrentMethod(),
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = cancelled ? "GoTo cancelled by command" : "GoTo failed, axes stopped"
                };
                MonitorLog.LogToMonitor(monitorItem);
                // Reset rates and axis movement
                _rateMoveAxes = new Vector(0, 0);
                MoveAxisActive = false;
                _rateRaDec = new Vector(0, 0);
                // Stop axes
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

                SlewState = SlewType.SlewNone;
                Tracking = trackingState;
                // Some unknown exception
                if (!cancelled)
                    MountError = new Exception($"GoTo Async Error|{ex.Message}");
            }
            finally
            {
                _ctsGoTo?.Dispose();
                _ctsGoTo = null;
            }
        }

        /// <summary>
        /// Goto home slew
        /// </summary>
        public static void GoToHome()
        {
            if (AtHome || SlewState == SlewType.SlewHome) return;

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
            Tracking = false;

            var ps = ParkSelected; // get position selected could be set from UI or AsCom
            if (ps == null) { return; }
            if (double.IsNaN(ps.X)) { return; }
            if (double.IsNaN(ps.Y)) { return; }
            SetParkAxis(ps.Name, ps.X, ps.Y);

            SkySettings.ParkAxisX = ps.X; // Store for startup default position
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
        public static void HcMoves(SlewSpeed speed, SlewDirection direction, HcMode hcMode, bool hcAntiRa, bool hcAntiDec, int raBacklash, int decBacklash)
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
                Message = $"{SkySettings.HcSpeed}|{hcMode}|{direction}|{ActualAxisX}|{ActualAxisY}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            var altAzModeSet = (SkySettings.AlignmentMode == AlignmentModes.algAltAz);

            var change = new double[] { 0, 0 };
            double delta;
            switch (speed)
            {
                case SlewSpeed.One:
                    delta = _slewSpeedOne;
                    break;
                case SlewSpeed.Two:
                    delta = _slewSpeedTwo;
                    break;
                case SlewSpeed.Three:
                    delta = _slewSpeedThree;
                    break;
                case SlewSpeed.Four:
                    delta = _slewSpeedFour;
                    break;
                case SlewSpeed.Five:
                    delta = _slewSpeedFive;
                    break;
                case SlewSpeed.Six:
                    delta = _slewSpeedSix;
                    break;
                case SlewSpeed.Seven:
                    delta = _slewSpeedSeven;
                    break;
                case SlewSpeed.Eight:
                    delta = SlewSpeedEight;
                    break;
                default:
                    delta = 0;
                    break;
            }

            // Check hand control mode and direction
            switch (hcMode)
            {
                case HcMode.Axes:
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
                            change[0] = SouthernHemisphere && !altAzModeSet ? -delta : delta;
                            break;
                        case SlewDirection.SlewWest:
                        case SlewDirection.SlewRight:
                            change[0] = SouthernHemisphere && !altAzModeSet ? delta : -delta;
                            break;
                        case SlewDirection.SlewNoneRa:
                            if (_hcPrevMoveRa != null)
                            {
                                _hcPrevMoveRa.StepEnd = GetRawSteps(0);
                                if (_hcPrevMoveRa.StepEnd.HasValue && _hcPrevMoveRa.StepStart.HasValue)
                                {
                                    _hcPrevMoveRa.StepDiff = Math.Abs(_hcPrevMoveRa.StepEnd.Value - _hcPrevMoveRa.StepStart.Value);
                                }
                            }
                            break;
                        case SlewDirection.SlewNoneDec:
                            if (_hcPrevMoveDec != null)
                            {
                                _hcPrevMoveDec.StepEnd = GetRawSteps(1);
                                if (_hcPrevMoveDec.StepEnd.HasValue && _hcPrevMoveDec.StepStart.HasValue)
                                {
                                    _hcPrevMoveDec.StepDiff = Math.Abs(_hcPrevMoveDec.StepEnd.Value - _hcPrevMoveDec.StepStart.Value);
                                    HcPrevMovesDec.Add(_hcPrevMoveDec.StepDiff);
                                }
                            }
                            break;
                        default:
                            change[0] = 0;
                            change[1] = 0;
                            break;
                    }
                    break;
                case HcMode.Guiding:
                    switch (direction)
                    {
                        case SlewDirection.SlewNorth:
                        case SlewDirection.SlewUp:
                            if (!altAzModeSet)
                            {
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
                            }
                            else
                            {
                                change[1] = delta;
                            }
                            break;
                        case SlewDirection.SlewSouth:
                        case SlewDirection.SlewDown:
                            if (!altAzModeSet)
                            {
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
                            }
                            else
                            {
                                change[1] = -delta;
                            }
                            break;
                        case SlewDirection.SlewEast:
                        case SlewDirection.SlewLeft:
                            if (!altAzModeSet)
                            {
                                change[0] = SouthernHemisphere ? delta : -delta;
                            }
                            else
                            {
                                change[0] = delta;
                            }
                            break;
                        case SlewDirection.SlewWest:
                        case SlewDirection.SlewRight:
                            if (!altAzModeSet)
                            {
                                change[0] = SouthernHemisphere ? -delta : delta;
                            }
                            else
                            {
                                change[0] = -delta;
                            }
                            break;
                        case SlewDirection.SlewNoneRa:
                            if (_hcPrevMoveRa != null)
                            {
                                _hcPrevMoveRa.StepEnd = GetRawSteps(0);
                                if (_hcPrevMoveRa.StepEnd.HasValue && _hcPrevMoveRa.StepStart.HasValue)
                                {
                                    _hcPrevMoveRa.StepDiff = Math.Abs(_hcPrevMoveRa.StepEnd.Value - _hcPrevMoveRa.StepStart.Value);
                                }
                            }
                            break;
                        case SlewDirection.SlewNoneDec:
                            if (_hcPrevMoveDec != null)
                            {
                                _hcPrevMoveDec.StepEnd = GetRawSteps(1);
                                if (_hcPrevMoveDec.StepEnd.HasValue && _hcPrevMoveDec.StepStart.HasValue)
                                {
                                    _hcPrevMoveDec.StepDiff = Math.Abs(_hcPrevMoveDec.StepEnd.Value - _hcPrevMoveDec.StepStart.Value);
                                    HcPrevMovesDec.Add(_hcPrevMoveDec.StepDiff);
                                }
                            }
                            break;
                        default:
                            change[0] = 0;
                            change[1] = 0;
                            break;
                    }
                    break;
                case HcMode.Pulse:
                    HcPulseMoveAsync(speed,direction);
                    return;
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
            if (hcAntiDec && decBacklash > 0 && _hcPrevMoveDec != null)
            {
                switch (direction)
                {
                    case SlewDirection.SlewNorth:
                    case SlewDirection.SlewUp:
                    case SlewDirection.SlewSouth:
                    case SlewDirection.SlewDown:
                        if (Math.Abs(_hcPrevMoveDec.Delta) > 0.000000 &&
                            Math.Sign(_hcPrevMoveDec.Delta) != Math.Sign(change[1]))
                        {
                            stepsNeededDec = Convert.ToInt64(HcPrevMovesDec.Sum());
                            if (stepsNeededDec >= decBacklash)
                            {
                                stepsNeededDec = decBacklash;
                            }

                            if (change[1] < 0) stepsNeededDec = -stepsNeededDec;
                        }
                        break;
                }
            }
            long stepsNeededRa = 0;
            if (hcAntiRa && Tracking && raBacklash > 0 && _hcPrevMoveRa != null)
            {
                if (direction == SlewDirection.SlewNoneRa)
                {
                    if (_hcPrevMoveRa.StepEnd.HasValue && _hcPrevMoveRa.StepStart.HasValue)
                    {
                        if (SouthernHemisphere)
                        {
                            if (_hcPrevMoveRa.StepEnd.Value > _hcPrevMoveRa.StepStart.Value)
                            {
                                stepsNeededRa = Convert.ToInt64(_hcPrevMoveRa.StepDiff);
                                if (stepsNeededRa >= raBacklash) { stepsNeededRa = raBacklash; }
                                stepsNeededRa = -Math.Abs(stepsNeededRa);
                            }
                        }
                        else
                        {
                            if (_hcPrevMoveRa.StepEnd.Value < _hcPrevMoveRa.StepStart.Value)
                            {
                                stepsNeededRa = Convert.ToInt64(_hcPrevMoveRa.StepDiff);
                                if (stepsNeededRa >= raBacklash) { stepsNeededRa = raBacklash; }
                            }
                        }
                    }
                }
            }

            //  log anti-lash moves
            if (Math.Abs(stepsNeededDec) > 0 && _hcPrevMoveDec != null)
            {
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_hcPrevMoveDec.Delta}|{HcPrevMovesDec.Sum()},Anti-Lash,{stepsNeededDec} of {decBacklash}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            if (Math.Abs(stepsNeededRa) > 0 && _hcPrevMoveRa != null)
            {
                monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_hcPrevMoveRa.Direction}|{_hcPrevMoveRa.StepDiff},Anti-Lash,{stepsNeededRa} of {raBacklash}"
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
                    _hcPrevMoveDec = new HcPrevMove
                    {
                        Direction = direction,
                        StartDate = HiResDateTime.UtcNow,
                        Delta = change[1],
                        StepStart = GetRawSteps(1),
                    };
                    break;
                case SlewDirection.SlewEast:
                case SlewDirection.SlewLeft:
                case SlewDirection.SlewWest:
                case SlewDirection.SlewRight:
                    _hcPrevMoveRa = new HcPrevMove
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

            // For Alt / Az mode swap to alt / az rate tracking whilst slewing
            if ((altAzModeSet) && (change[0] != 0.0 || change[1] != 0.0) && Tracking)
            {
                // Acquire the AltAz tracking lock
                while (Interlocked.CompareExchange(ref _altAzTrackingLock, -1, 0) != 0) Thread.Sleep(10);
                if (AltAzTimerIsRunning) _altAzTrackingTimer.Stop();
                SetAltAzTrackingRates(AltAzTrackingType.Rate);
            }

            // Send to mount
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    //RA Anti-backlash compensate
                    if (Math.Abs(stepsNeededDec) > 0)
                    {
                        HcPrevMovesDec.Clear();

                        var a = new CmdAxesDegrees(MountQueue.NewId);
                        var b = (double[])MountQueue.GetCommandResult(a).Result;
                        var arcSecs = Conversions.StepPerArcSec(StepsPerRevolution[1]);
                        var c = stepsNeededDec / arcSecs;
                        var d = Conversions.ArcSec2Deg(c);
                        _ = new CmdAxisGoToTarget(0, Axis.Axis2, b[1] + d);

                        // check for axis stopped
                        var stopwatch1 = Stopwatch.StartNew();
                        while (stopwatch1.Elapsed.TotalSeconds <= 2)
                        {
                            var deltaX = new CmdAxisStatus(MountQueue.NewId, Axis.Axis2);
                            var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltaX).Result;
                            if (!axis1Status.Slewing) break; // stopped doesn't report quick enough
                        }
                    }

                    // Correct position after lash correction
                    if (_hcPrevMoveDec != null) _hcPrevMoveDec.StepStart = GetRawSteps(1);

                    if (Math.Abs(stepsNeededRa) > 0)
                    {
                        var a = new CmdAxesDegrees(MountQueue.NewId);
                        var b = (double[])MountQueue.GetCommandResult(a).Result;
                        var arcSecs = Conversions.StepPerArcSec(StepsPerRevolution[0]);
                        var c = (stepsNeededRa) / arcSecs;
                        var d = Conversions.ArcSec2Deg(c);
                        _ = new CmdAxisGoToTarget(0, Axis.Axis1, b[0] + d);

                        // check for axis stopped
                        var stopwatch1 = Stopwatch.StartNew();
                        while (stopwatch1.Elapsed.TotalSeconds <= 2)
                        {
                            var deltaX = new CmdAxisStatus(MountQueue.NewId, Axis.Axis1);
                            var axis1Status = (AxisStatus)MountQueue.GetCommandResult(deltaX).Result;
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
                    if (_hcPrevMoveDec != null) _hcPrevMoveDec.StepStart = GetRawSteps(1);

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

                    _skyHcRate.X = change[0];
                    _skyHcRate.Y = change[1];
                    var rate = SkyGetRate();
                    _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                    _ = new SkyAxisSlew(0, AxisId.Axis2, rate.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // start alt / az predictive tracking again if on before hc move
            if (altAzModeSet && (change[0] == 0.0) && (change[1] == 0.0) && Tracking)
                // execute on new thread to allow responsive UI updates and ASCOM transactions
                Task.Run(() =>
                    {
                        // wait for the movment to stop
                        var trackingRate = SkyGetRate();
                        AxesRateOfChange.Reset();
                        do
                        {
                            MountPositionUpdated = false;
                            // Update mount velocity
                            UpdateSteps();
                            while (!MountPositionUpdated) Thread.Sleep(50);
                            AxesRateOfChange.Update(_actualAxisX, _actualAxisY, HiResDateTime.UtcNow);
                        } while ((AxesRateOfChange.AxisVelocity - trackingRate).Length > 1.1 * CurrentTrackingRate());
                        // resume tracking
                        SkyPredictor.Set(RightAscensionXForm, DeclinationXForm, 0, 0);
                        SetTracking();
                        // release the AltAz tracking timer lock
                        _altAzTrackingLock = 0;
                        monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Server,
                            Category = MonitorCategory.Server,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"|RaDec SlewNone tracking|{RightAscensionXForm}|{DeclinationXForm}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                );
        }

        /// <summary>
        /// Starts async process from the hand controller which will continue to send pulses to mount until they are canceled
        /// </summary>
        /// <param name="speed">HC speed 1 to 8</param>
        /// <param name="direction">direction of pulse</param>
        public static async void HcPulseMoveAsync(SlewSpeed speed, SlewDirection direction)
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MonitorLog.GetCurrentMethod(),
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message =
                        $"{speed}|{direction}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                switch (direction)
                {
                    case SlewDirection.SlewNoneRa:
                    case SlewDirection.SlewNoneDec:
                        _ctsHcPulseGuide?.Cancel();
                        return;
                    case SlewDirection.SlewNorth:
                    case SlewDirection.SlewSouth:
                    case SlewDirection.SlewEast:
                    case SlewDirection.SlewWest:
                    case SlewDirection.SlewUp:
                    case SlewDirection.SlewDown:
                    case SlewDirection.SlewLeft:
                    case SlewDirection.SlewRight:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                var hpGs = SkySettings.HcPulseGuides;
                if (hpGs == null) { return; }

                var hcSpeed = (int)speed;  // selected HC speed
                var exist = hpGs.Any(x => x.Speed == hcSpeed); 
                if (!exist) { return; }  // could do a default here
            
                var hcPulseGuide = hpGs.Find(x => x.Speed == hcSpeed);
                GuideDirections pulseDirection;
                switch (direction)
                {
                    case SlewDirection.SlewNorth:
                    case SlewDirection.SlewUp:
                        pulseDirection = GuideDirections.guideNorth;
                        break;
                    case SlewDirection.SlewSouth:
                    case SlewDirection.SlewDown:
                        pulseDirection = GuideDirections.guideSouth;
                        break;
                    case SlewDirection.SlewEast:
                    case SlewDirection.SlewLeft:
                        pulseDirection = GuideDirections.guideEast;
                        break;
                    case SlewDirection.SlewWest:
                    case SlewDirection.SlewRight:
                        pulseDirection = GuideDirections.guideWest;
                        break;
                    //case SlewDirection.SlewNoneRa:
                    //    _ctsHcPulseGuide?.Cancel();
                    //    return;
                    //case SlewDirection.SlewNoneDec:
                    //    _ctsHcPulseGuide?.Cancel();
                    //    return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                int returnCode;
                //string msg;
                switch (pulseDirection)
                {
                    case GuideDirections.guideSouth:
                    case GuideDirections.guideNorth:
                    case GuideDirections.guideWest:
                    case GuideDirections.guideEast:
                        _ctsHcPulseGuide = new CancellationTokenSource();
                        returnCode = await Task.Run(() => HcPulseMove(hcPulseGuide, pulseDirection, _ctsHcPulseGuide.Token));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
                if (returnCode > 0)
                {
                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = MonitorLog.GetCurrentMethod(),
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message =
                            $"ReturnCode:{returnCode}|{hcPulseGuide.Speed}|{hcPulseGuide.Duration}|{hcPulseGuide.Interval}|{hcPulseGuide.Rate}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                }
            }
            catch (Exception ex)
            {
                // OperationCanceledException thrown by HcPulseMove
                var cancelled = ex is OperationCanceledException || ex.GetBaseException() is OperationCanceledException;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Warning,
                    Method = MonitorLog.GetCurrentMethod(),
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = cancelled ? "HcPulseGuide cancelled by command" : "HcPulseGuides failed"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

        }

        /// <summary>
        /// Inf loop to send pulse until token is canceled
        /// </summary>
        /// <param name="hcPulseGuide">PulseGuide object</param>
        /// <param name="pulseDirection">direction button from the HC</param>
        /// <param name="token">CancellationToken</param>
        /// <returns></returns>
        public static int HcPulseMove(HcPulseGuide hcPulseGuide,GuideDirections pulseDirection,CancellationToken token)
        {
            try
            {
                var direction = pulseDirection;
                var duration = hcPulseGuide.Duration;
                var interval = hcPulseGuide.Interval;
                if (duration <= 0){return 2;}
                if (interval < 0){return 2;}

                while (true)
                {
                    if (token.IsCancellationRequested){break;}
                    PulseGuide(direction, duration, hcPulseGuide.Rate);
                    if (token.IsCancellationRequested){break;}
                    Thread.Sleep(duration);
                    HcPulseDone = true;
                    Thread.Sleep(interval);
                    HcPulseDone = false;
                } 
                return 0;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                HcPulseDone = false;
                _ctsPulseGuideDec?.Cancel();
                _ctsPulseGuideRa?.Cancel();
                return 3;
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
                    _hcPrevMoveDec = null;
                    break;
                case MountAxis.Ra:
                    _hcPrevMoveRa = null;
                    break;
            }
        }

        /// <summary>
        /// Sets up defaults after an established connection
        /// </summary>
        private static bool MountConnect()
        {
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
                    _skyHcRate = new Vector(0, 0);
                    _skyTrackingRate = new Vector(0, 0);

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

                    //CanHomeSensor = true; //test auto home

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

            try
            {
                // Get the app's configuration
                var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                // User config file copy from path
                var userConfigFilepath = userConfig.FilePath;
                // User config file copy to directory path
                var logDirectoryPath = GsFile.GetLogPath();
                // Copy the user config file to the log directory
                File.Copy(userConfigFilepath, Path.Combine(logDirectoryPath, "user.config"), true);

                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Copied user.config to {logDirectoryPath}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception e) when (e is ConfigurationErrorsException || e is ArgumentException) // All other exceptions mean app cannot function
            {
                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Cannot copy user.config. {e.Message} " };
                MonitorLog.LogToMonitor(monitorItem);
            }

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

            // setup server defaults, stop auto-discovery, connect serial port, start queues
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
                            $"Connection Failed: {SkySystem.Error}");
                    }
                    // Start up, pass custom mount gearing if needed
                    var custom360Steps = new[] { 0, 0 };
                    var customWormSteps = new[] { 0.0, 0.0 };
                    if (SkySettings.CustomGearing)
                    {
                        custom360Steps = new[] { SkySettings.CustomRa360Steps, SkySettings.CustomDec360Steps };
                        customWormSteps = new[] { (double)SkySettings.CustomRa360Steps / SkySettings.CustomRaWormTeeth, (double)SkySettings.CustomDec360Steps / SkySettings.CustomDecWormTeeth };
                    }

                    SkyQueue.Start(SkySystem.Serial, custom360Steps, customWormSteps, LowVoltageEventSet);
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

                // Event to update AltAz tracking rate
                _altAzTrackingTimer = new MediaTimer { Period = SkySettings.AltAzTrackingUpdateInterval, Resolution = 5 };
                _altAzTrackingTimer.Tick += AltAzTrackingTimerEvent;
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

            // Stop all asynchronous operations
            CancelAllAsync();
            AxesStopValidate();
            if (_mediaTimer != null) { _mediaTimer.Tick -= UpdateServerEvent; }
            _mediaTimer?.Stop();
            _mediaTimer?.Dispose();
            if (_altAzTrackingTimer != null) { _altAzTrackingTimer.Tick -= AltAzTrackingTimerEvent; }
            _altAzTrackingTimer?.Stop();
            _altAzTrackingTimer?.Dispose();
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 1000) { } //change
            sw.Stop();

            if (MountQueue.IsRunning) { MountQueue.Stop(); }

            if (!SkyQueue.IsRunning) return;
            SkyQueue.Stop();
            SkySystem.ConnectSerial = false;
        }

        /// <summary>
        /// Execute single axis pulse guide for AltAz using predictor
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="guideRate"></param>
        /// <param name="duration"></param>
        /// <param name="pulseGoTo"></param>
        /// <param name="token"></param>
        private static void PulseGuideAltAz(int axis, double guideRate, int duration, Action<CancellationToken> pulseGoTo, CancellationToken token)
        {
            Task.Run(() =>
            {
                var pulseStartTime = HiResDateTime.UtcNow;
                StopAltAzTrackingTimer();
                // set predictor Ra and Dec ready for pulse go to action
                switch (axis)
                {
                    case 0:
                        SkyPredictor.Set(SkyPredictor.Ra - duration * 0.001 * guideRate / SiderealRate, SkyPredictor.Dec);
                        break;
                    case 1:
                        SkyPredictor.Set(SkyPredictor.Ra, SkyPredictor.Dec + duration * guideRate * 0.001);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                }
                // setup to log and graph the pulse
                var pulseEntry = new PulseEntry();
                if (MonitorPulse)
                {
                    pulseEntry.Axis = axis;
                    pulseEntry.Duration = duration;
                    pulseEntry.Rate = guideRate;
                    pulseEntry.StartTime = pulseStartTime;
                }
                // execute pulse
                pulseGoTo(token);
                // pulse movement finished or cancelled so resume tracking
                SetTracking();
                // wait for pulse duration so completion variable IsPulseGuiding remains true 
                var waitTime = (int)(pulseStartTime.AddMilliseconds(duration) - HiResDateTime.UtcNow).TotalMilliseconds;
                if (waitTime > 0)
                {
                    var stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalMilliseconds < waitTime && !token.IsCancellationRequested)
                    {
                        if (stopwatch.ElapsedMilliseconds % 200 == 0)
                        {
                            UpdateSteps();
                        } // Process positions while waiting
                    }
                }
                // log and graph pulse
                if (MonitorPulse)
                {
                    MonitorLog.LogToMonitor(pulseEntry);
                }
                if (token.IsCancellationRequested)
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = MonitorLog.GetCurrentMethod(),
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Axis|{axis}|Async operation cancelled"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                // set pulse guiding status
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
        /// Pulse commands
        /// </summary>
        /// <param name="direction">GuideDirections</param>
        /// <param name="duration">in milliseconds</param>
        /// /// <param name="altRate">alternate rate to replace the guide rate</param>
        public static void PulseGuide(GuideDirections direction, int duration, double altRate)
        {
            if (!IsMountRunning) { throw new Exception("Mount not running"); }

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{direction}|{duration}" };
            MonitorLog.LogToMonitor(monitorItem);

            var useAltRate = Math.Abs(altRate) > 0;
            
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
                    var decGuideRate = useAltRate ? altRate : Math.Abs(GuideRateDec);
                    if (SkySettings.AlignmentMode != AlignmentModes.algAltAz)
                    {
                        if (SideOfPier == PierSide.pierEast)
                        {
                            if (direction == GuideDirections.guideNorth) { decGuideRate = -decGuideRate; }
                        }
                        else
                        {
                            if (direction == GuideDirections.guideSouth) { decGuideRate = -decGuideRate; }
                        }
                    }
                    else
                    {
                        if (direction == GuideDirections.guideSouth) { decGuideRate = -decGuideRate; }
                    }

                    // Direction switched add backlash compensation
                    var decBacklashAmount = 0;
                    if (direction != LastDecDirection) decBacklashAmount = SkySettings.DecBacklash;
                    LastDecDirection = direction;
                    _ctsPulseGuideDec = new CancellationTokenSource();

                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                            {
                                PulseGuideAltAz((int)Axis.Axis2, decGuideRate, duration, SimPulseGoto, _ctsPulseGuideDec.Token);
                            }
                            else
                            {
                                if (!SouthernHemisphere)
                                {
                                    decGuideRate = decGuideRate > 0 ? -Math.Abs(decGuideRate) : Math.Abs(decGuideRate);
                                }
                                _ = new CmdAxisPulse(0, Axis.Axis2, decGuideRate, duration, _ctsPulseGuideDec.Token);
                            }
                            break;
                        case MountType.SkyWatcher:
                            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                            {
                                PulseGuideAltAz((int)AxisId.Axis2, decGuideRate, duration, SkyPulseGoto, _ctsPulseGuideDec.Token);
                            }
                            else
                            {
                                _ = new SkyAxisPulse(0, AxisId.Axis2, decGuideRate, duration, decBacklashAmount, _ctsPulseGuideDec.Token);
                            }
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
                    var raGuideRate = useAltRate ? altRate : Math.Abs(GuideRateRa);
                    if (SkySettings.AlignmentMode != AlignmentModes.algAltAz)
                    {
                        if (SouthernHemisphere)
                        {
                            if (direction == GuideDirections.guideWest) { raGuideRate = -raGuideRate; }
                        }
                        else
                        {
                            if (direction == GuideDirections.guideEast) { raGuideRate = -raGuideRate; }
                        }
                    }
                    else
                    {
                        if (direction == GuideDirections.guideEast) { raGuideRate = -raGuideRate; }
                    }

                    _ctsPulseGuideRa = new CancellationTokenSource();
                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                            {
                                PulseGuideAltAz((int)Axis.Axis1, raGuideRate, duration, SimPulseGoto, _ctsPulseGuideRa.Token);
                            }
                            else
                            {
                                _ = new CmdAxisPulse(0, Axis.Axis1, raGuideRate, duration, _ctsPulseGuideRa.Token);
                            }

                            break;
                        case MountType.SkyWatcher:
                            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                            {
                                PulseGuideAltAz((int)AxisId.Axis1, raGuideRate, duration, SkyPulseGoto, _ctsPulseGuideRa.Token);
                            }
                            else
                            {
                                _ = new SkyAxisPulse(0, AxisId.Axis1, raGuideRate, duration, 0, _ctsPulseGuideRa.Token);
                            }
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
            Tracking = false;
            StopAxes();

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
            _slewSpeedOne = Math.Round(maxRate * 0.0034, 3);
            _slewSpeedTwo = Math.Round(maxRate * 0.0068, 3);
            _slewSpeedThree = Math.Round(maxRate * 0.047, 3);
            _slewSpeedFour = Math.Round(maxRate * 0.068, 3);
            _slewSpeedFive = Math.Round(maxRate * 0.2, 3);
            _slewSpeedSix = Math.Round(maxRate * 0.4, 3);
            _slewSpeedSeven = Math.Round(maxRate * 0.8, 3);
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
                    $"{_slewSpeedOne}|{_slewSpeedTwo}|{_slewSpeedThree}|{_slewSpeedFour}|{_slewSpeedFive}|{_slewSpeedSix}|{_slewSpeedSeven}|{SlewSpeedEight}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Set tracking on or off
        /// </summary>
        private static void SetTracking()
        {
            if (!IsMountRunning) { return; }

            double rateChange = 0;
            Vector rate;
            // Set rate change for tracking mode
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

            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            if (rateChange != 0)
                            {
                                SetAltAzTrackingRates(AltAzTrackingType.Predictor);
                                if (!AltAzTimerIsRunning) StartAltAzTrackingTimer();
                            }
                            else
                            {
                                if (AltAzTimerIsRunning) StopAltAzTrackingTimer();
                                _skyTrackingRate.X = 0.0;
                                _skyTrackingRate.Y = 0.0;
                            }
                            rate = SkyGetRate();
                            // Tracking applied unless MoveAxis is active
                            if (!MovePrimaryAxisActive)
                                _ = new CmdAxisTracking(0, Axis.Axis1, rate.X);
                            if (!MoveSecondaryAxisActive)
                                _ = new CmdAxisTracking(0, Axis.Axis2, rate.Y);
                            break;
                        case AlignmentModes.algPolar:
                        case AlignmentModes.algGermanPolar:
                            if (!MovePrimaryAxisActive) // Set current tracking rate and RA tracking rate offset (0 if not sidereal)
                            {
                                _ = new CmdAxisTracking(0, Axis.Axis1, rateChange);
                            }
                            _ = new CmdRaDecRate(0, Axis.Axis1, GetRaRateDirection(_rateRaDec.X));
                            if (!MoveSecondaryAxisActive) // Set Dec tracking rate offset (0 if not sidereal)
                            {
                                _ = new CmdRaDecRate(0, Axis.Axis2, GetDecRateDirection(_rateRaDec.Y));
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case MountType.SkyWatcher:
                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            if (rateChange != 0)
                            {
                                SetAltAzTrackingRates(AltAzTrackingType.Predictor);
                                if (!AltAzTimerIsRunning) StartAltAzTrackingTimer();
                            }
                            else
                            {
                                if (AltAzTimerIsRunning) StopAltAzTrackingTimer();
                                _skyTrackingRate.X = 0.0;
                                _skyTrackingRate.Y = 0.0;
                            }

                            // Get current tracking  including RA and Dec offsets
                            // Tracking applied unless MoveAxis is active
                            break;
                        case AlignmentModes.algPolar:
                        case AlignmentModes.algGermanPolar:
                            _skyTrackingRate.X = rateChange;
                            _skyTrackingRate.Y = 0.0;
                            // Get current tracking including RA and Dec offsets
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    rate = SkyGetRate(); // Get current tracking  including RA and Dec offsets
                    if (!MovePrimaryAxisActive)
                        _ = new SkyAxisSlew(0, AxisId.Axis1, rate.X);
                    if (!MoveSecondaryAxisActive)
                        _ = new SkyAxisSlew(0, AxisId.Axis2, rate.Y);
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
        /// Set tracking mode based on alignment mode
        /// </summary>
        public static void SetTrackingMode()
        {
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    _trackingMode = TrackingMode.AltAz;
                    break;
                case AlignmentModes.algPolar:
                case AlignmentModes.algGermanPolar:
                    _trackingMode = SouthernHemisphere ? TrackingMode.EqS : TrackingMode.EqN;
                    break;
            }
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
                Application.Current.Windows[intCounter]?.Close();
            }
        }

        /// <summary>
        /// Starts slew with ra/dec internal coordinates
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <param name="tracking"></param>
        public static void SlewRaDec(double rightAscension, double declination, bool tracking = false)
        {
            //// convert RA/Dec to polar or GEM axis
            //var a = Axes.RaDecToAxesXY(new[] { rightAscension, declination });
            //_targetAxes = (SkySettings.AlignmentMode == AlignmentModes.algAltAz) ? new Vector(rightAscension, declination) : new Vector(a[0], a[1]);
            SlewMount(new Vector(rightAscension, declination), SlewType.SlewRaDec, tracking);
        }

        /// <summary>
        /// Within the meridian limits will check for closest slew
        /// </summary>
        /// <param name="position"></param>
        /// <returns>axis position that is closest</returns>
        public static double[] CheckAlternatePosition(double[] position)
        {
            // Check Forced flip for a goto
            var flipGoto = FlipOnNextGoto;
            FlipOnNextGoto = false;

            // See if the target is within flip angle limits
            if (!IsWithinFlipLimits(position)) { return null; }
            var alt = Axes.GetAltAxisPosition(position);

            var cl = ChooseClosestPosition(ActualAxisX, position, alt);  //choose the closest angle to slew 
            if (flipGoto) // implement the forced flip for a goto
            {
                cl = cl == "a" ? "b" : "a"; //choose the farthest angle to slew which will flip
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"flip|{cl}|{ActualAxisX}|{position[0]}|{position[1]}|{alt[0]}|{alt[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }

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
        private static string ChooseClosestPosition(double position, IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            var val1 = Math.Abs(a[0] - position);
            var val2 = Math.Abs(b[0] - position);
            if (!(Math.Abs(val1 - val2) > 0)) { return "a"; }
            return val1 < val2 ? "a" : "b";
        }

        /// <summary>
        /// Calculates if axis position is within the defined flip angle
        /// </summary>
        /// <param name="position">X axis position of mount</param>
        /// <returns>True if within limits otherwise false</returns>
        public static bool IsWithinFlipLimits(IReadOnlyList<double> position)
        {
            return position[0] > -SkySettings.HourAngleLimit && position[0] < SkySettings.HourAngleLimit ||
                   position[0] > 180 - SkySettings.HourAngleLimit && position[0] < 180 + SkySettings.HourAngleLimit;
        }

        /// <summary>
        /// Execute azimuth flip about South direction with tracking state maintained
        /// </summary>
        public static void FlipAzimuthPosition()
        {
            var tracking = Tracking;
            var azimuth = Azimuth;
            Tracking = false;
            switch (AzSlewMotion)
            {
                case AzSlewMotionType.East:
                    azimuth -= 360.0;
                    break;
                case AzSlewMotionType.West:
                    azimuth += 360.0;
                    break;
            }
            Task.Run( () => 
            {
                SlewAltAz(Altitude, azimuth);
                while (IsSlewing)
                {
                    Thread.Sleep(SkySettings.DisplayInterval);
                }
                Tracking = tracking;
            });
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
        /// Checks if current azimuth position can be reached from opposite direction
        /// within slew limit
        /// </summary>
        /// <returns></returns>
        private static bool CheckFlipAzimuth()
        {
            var result = false;
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                result = (Range.Range360(ActualAxisX) < 180 + SkySettings.AzSlewLimit)
                         && (Range.Range360(ActualAxisX) > 180 - SkySettings.AzSlewLimit);
            }
            return result;
        }

        /// <summary>
        /// Starts slew with alt/az coordinates as vector type
        /// </summary>
        /// <param name="targetAltAzm"></param>
        private static void SlewAltAz(Vector targetAltAzm)
        {
            var yx = Axes.AltAzToAxesYX(new[] { targetAltAzm.Y, targetAltAzm.X });
            var target = (SkySettings.AlignmentMode != AlignmentModes.algAltAz) ? new Vector(yx[1], yx[0]) : targetAltAzm;

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
        /// <param name="tracking"></param>
        // ReSharper disable once AsyncVoidMethod
        private static async void SlewMount(Vector targetPosition, SlewType slewState, bool tracking = false)
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

            HcResetPrevMove(MountAxis.Ra);
            HcResetPrevMove(MountAxis.Dec);

            //_targetAxes = targetPosition;
            AtPark = false;
            SpeakSlewStart(slewState);
            //GoToAsync(new[] { _targetAxes.X, _targetAxes.Y }, slewState, tracking);
            await Task.Run(() => GoToAsync(new[] { targetPosition.X, targetPosition.Y }, slewState, tracking));
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

            CancelAllAsync();
            // Stop all MoveAxis and do not restore tracking
            MoveAxisActive = false;
            RateMovePrimaryAxis = 0.0;
            RateMoveSecondaryAxis = 0.0;
            _rateRaDec = new Vector(0, 0);

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

            SlewState = SlewType.SlewNone;
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
            if (trackingstate)
            {
                TrackingSpeak = false;
                Tracking = false;
            }

            _altAzSync = new Vector(targetAltitude, targetAzimuth);
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

            MountPositionUpdated = false;
            while (!MountPositionUpdated)
            {
                Thread.Sleep(50);
            }

            if (trackingstate)
            {
                Tracking = true;
                TrackingSpeak = true;
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
                Message = $" {TargetRa}|{TargetDec}|{Tracking}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var trackingstate = Tracking;
            if (trackingstate)
            {
                TrackingSpeak = false;
                Tracking = false;
            }

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
                        if (SkySettings.AlignmentMode == AlignmentModes.algAltAz) SkyPredictor.Set(TargetRa, TargetDec);
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
                        if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                        {
                            SkyPredictor.Set(TargetRa, TargetDec);
                            SetTracking();
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            MountPositionUpdated = false;
            while (!MountPositionUpdated)
            {
                Thread.Sleep(50);
            }

            if (trackingstate)
            {
                Tracking = true;
                TrackingSpeak = true;
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
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                current[0] = Range.Range360(current[0]);

            //compare ra dec / az alt to current mount position
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

            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                target[0] = az;
                target[1] = alt;
                current[0] = Range.Range360(_mountAxisX);
                current[1] = _mountAxisY;
            }

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

        /// <summary>
        /// Convert az in range 0 to 360 to plus / minus range centred on 0 degrees 
        /// </summary>
        /// <param name="az"></param>
        /// <returns></returns>
        public static double ConvertToAzEastWest(double az)
        {
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                switch (AzSlewMotion)
                {
                    case AzSlewMotionType.East:
                        if (az > 180 + SkySettings.AzSlewLimit)
                        {
                            az -= 360.0;
                        }
                        break;
                    case AzSlewMotionType.West:
                        if (az > 180 - SkySettings.AzSlewLimit)
                        {
                            az -= 360.0;
                        }
                        break;
                }
            }
            return az;
        }

        /// <summary>
        /// Checks for az in plus / minus range centred on 0 degrees for slewing
        /// Eastwards and Westwards flip at 0 degrees and are always in limit
        /// </summary>
        /// <param name="az"></param>
        /// <returns></returns>
        public static bool AzEastWestSlewAtLimit(double az)
        {
            bool atLimit = false;
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                switch (GetAzSlewMotion())
                {
                    case AzSlewMotionType.East:
                        atLimit = (az > 180.0 + SkySettings.AzSlewLimit);
                        break;
                    case AzSlewMotionType.West:
                        atLimit = az < (-180.0 - SkySettings.AzSlewLimit);
                        break;
                }
            }
            return atLimit;
        }

        /// <summary>
        /// Checks for az in plus / minus range centred on 0 degrees for slewing
        /// Eastwards and Westwards flip at 0 degrees and are always in limit
        /// </summary>
        /// <param name="az"></param>
        /// <returns></returns>
        public static bool AzEastWestTrackAtLimit(double az)
        {
            bool atLimit = false;
            if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
            {
                switch (GetAzSlewMotion())
                {
                    case AzSlewMotionType.East:
                        atLimit = (az > 180.0 + SkySettings.AzSlewLimit + SkySettings.AxisTrackingLimit);
                        break;
                    case AzSlewMotionType.West:
                        atLimit = (az < -180.0 - SkySettings.AzSlewLimit - SkySettings.AxisTrackingLimit);
                        break;
                }
            }
            return atLimit;
        }

        #endregion

        #region Alignment

        private static void ConnectAlignmentModel()
        {
            AlignmentModel.Connect(_homeAxes.X, _homeAxes.Y, StepsPerRevolution, AlignmentSettings.ClearModelOnStartup);
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
            //      SkyServer.Steps contains the current encoder positions.
            //      SkyServer.FactorStep contains the conversion from radians to steps
            // To get the target steps
            var a = Transforms.CoordTypeToInternal(TargetRa, TargetDec);
            var xy = Axes.RaDecToAxesXY(new[] { a.X, a.Y });
            var unSynced = Axes.AxesAppToMount(new[] { xy[0], xy[1] });
            var rawSteps = GetRawSteps();
            var synced = new[] { ConvertStepsToDegrees(rawSteps[0], 0), ConvertStepsToDegrees(rawSteps[1], 1) };
            if (AlignmentModel.SyncToRaDec(
                unSynced,
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
                    Message = $"Alignment point added: Un-synced axis = {unSynced[0]}/{unSynced[1]}, RA/Dec = {a.X}/{a.Y}, Synched axis = {synced[0]}/{synced[1]}"
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
                    Message = $"Alignment point added: Un-synced axis = {unSynced[0]}/{unSynced[1]}, RA/Dec = {a.X}/{a.Y}, Synched axis = {synced[0]}/{synced[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Gets the alignment model corrected target (physical) axis positions for a given calculated axis position.
        /// </summary>
        /// <param name="unsynced"></param>
        /// <returns></returns>
        private static double[] GetSyncedAxes(double[] unsynced)
        {
            if (AlignmentModel.IsAlignmentOn && SkyServer.SlewState == SlewType.SlewRaDec && !SkyServer.IsHome && !SkyServer.AtPark)
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
                    Message = $"Mapped un-synced axis angles: {unsynced[0]}/{unsynced[1]} to {synced[0]}/{synced[1]}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                // For safety, check the difference is within the max unsynced/synched difference found in the alignment model.
                var a = Math.Abs(unsynced[0] - synced[0]);
                var b = Math.Abs(unsynced[1] - synced[1]);
                double[] maxDelta = AlignmentModel.MaxDelta;
                if (Math.Abs(a) > maxDelta[0] * AlignmentModel.AlignmentWarningThreshold || Math.Abs(b) > maxDelta[1] * AlignmentModel.AlignmentWarningThreshold)
                {
                    // Log a warning message, switch off the alignment model and return the original calculated position.
                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Alignment,
                        Type = MonitorType.Warning,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Large delta: {unsynced[0]}|{unsynced[1]}|{synced[0]}|{synced[1]}|{maxDelta[0]}|{maxDelta[1]}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    AlignmentSettings.IsAlertOn = true;
                    return unsynced;
                }
                else
                {
                    return synced;
                }
            }
            else
            {
                return unsynced;
            }
        }

        /// <summary>
        /// Get the axis positions to report for a given physical axis position.
        /// </summary>
        /// <param name="synced"></param>
        /// <returns></returns>
        private static double[] GetUnsyncedAxes(double[] synced)
        {
            double[] unsynced = AlignmentModel.GetUnsyncedValue(synced);

            if (AlignmentModel.IsAlignmentOn && SkyServer.SlewState != SlewType.SlewPark && SkyServer.SlewState != SlewType.SlewHome
                && !SkyServer.IsHome && !SkyServer.AtPark)
            {
                //var monitorItem = new MonitorEntry
                //{
                //    Datetime = HiResDateTime.UtcNow,
                //    Device = MonitorDevice.Server,
                //    Category = MonitorCategory.Alignment,
                //    Type = MonitorType.Data,
                //    Method = MethodBase.GetCurrentMethod()?.Name,
                //    Thread = Thread.CurrentThread.ManagedThreadId,
                //    Message = $"Mapped synced axis angles: {synced[0]}/{synced[1]} to {unsynced[0]}/{unsynced[1]}"
                //};
                //MonitorLog.LogToMonitor(monitorItem);
                return unsynced;
            }

            return synced;
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
                case "AlignmentMode":
                    Tracking = false;
                    SkyPredictor.Reset();
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
                case "AlignmentWarningThreshold":
                    AlignmentModel.AlignmentWarningThreshold = AlignmentSettings.AlignmentWarningThreshold;
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
                case "Steps":
                    Steps = SkyQueue.Steps;
                    MountPositionUpdated = true;
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
                case "Steps":
                    Steps = MountQueue.Steps;
                    MountPositionUpdated = true;
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
            _rateMoveAxes = new Vector(0, 0);
            MoveAxisActive = false;
            SlewState = SlewType.SlewNone;

            // invalidate target positions
            _targetRaDec = new Vector(double.NaN, double.NaN);
           SkyPredictor.Reset();

            //default hand control and slew rates
            SetSlewRates(SkySettings.MaxSlewRate);

            // Allows driver movements commands to process
            AsComOn = true;

            //Next goto will flip
            FlipOnNextGoto = false;

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
                Monitor.TryEnter(TimerLock, ref hasLock);
                if (!hasLock)
                {
                    TimerOverruns++;
                    return;
                }

                LoopCounter++; // increment counter

                SiderealTime = GetLocalSiderealTime(); // the time is?

                UpdateSteps(); // get step from the mount

                Lha = Coordinate.Ra2Ha12(RightAscensionXForm, SiderealTime);

                CheckSlewState(); // Track slewing state

                CheckAxisLimits(); //used for warning light

                CheckSpiralLimit(); // reset spiral if moved too far

                // Update UI 
                CheckPecTraining();
                IsHome = AtHome;
                IsSideOfPier = SideOfPier;
                // Update Azimuth slewing information
                if (SkySettings.AlignmentMode == AlignmentModes.algAltAz)
                {
                    AzSlewMotion = GetAzSlewMotion();
                    CanFlipAzimuthSide = CheckFlipAzimuth();
                }
                var t = SkySettings.DisplayInterval; // Event interval time set for UI performance 
                _mediaTimer.Period = t;

                if (LoopCounter % (ulong) Settings.Settings.ModelIntFactor == 0)
                {
                    Rotate3DModel = true;
                }
            }
            catch (Exception ex)
            {
                SkyErrorHandler(ex);
            }
            finally
            {
                if (hasLock) { Monitor.Exit(TimerLock); }
            }
        }

        /// <summary>
        /// Handles the event triggered when a low voltage condition is detected.
        /// </summary>
        /// <remarks>This method sets the <see cref="LowVoltageEventState"/> to <see langword="true"/> to
        /// indicate that a low voltage condition has occurred.</remarks>
        /// <param name="sender">The source of the event. Typically, this is the object that raised the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
        private static void LowVoltageEventSet(object sender, EventArgs e)
        {
            LowVoltageEventState = true;
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Mount,
                Type = MonitorType.Error,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"Mount detected low voltage: check power supply and wiring"
            };
            MonitorLog.LogToMonitor(monitorItem);
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
                                if (double.TryParse(keys[1].Trim(), out var endPosition))
                                {
                                    def.StartPosition = endPosition;
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
                                if (double.TryParse(keys[1].Trim(), out var wormPeriod))
                                {
                                    def.WormPeriod = wormPeriod;
                                }
                                break;
                            case "#WormTeeth":
                                if (int.TryParse(keys[1].Trim(), out var wormTeeth))
                                {
                                    def.WormTeeth = wormTeeth;
                                }
                                break;
                            case "#WormSteps":
                                if (double.TryParse(keys[1].Trim(), out var wormSteps))
                                {
                                    def.WormSteps = wormSteps;
                                }
                                break;
                            case "#TrackingRate":
                                if (double.TryParse(keys[1].Trim(), out var trackingRate1))
                                {
                                    def.TrackingRate = trackingRate1;
                                }
                                break;
                            case "#PositionOffset":
                                if (double.TryParse(keys[1].Trim(), out var positionOffset))
                                {
                                    def.PositionOffset = positionOffset;
                                }
                                break;
                            case "#Ra":
                                if (double.TryParse(keys[1].Trim(), out var ra))
                                {
                                    def.Ra = ra;
                                }
                                break;
                            case "#Dec":
                                if (double.TryParse(keys[1].Trim(), out var dec))
                                {
                                    def.Dec = dec;
                                }
                                break;
                            case "#BinCount":
                                if (int.TryParse(keys[1].Trim(), out var binCount))
                                {
                                    def.BinCount = binCount;
                                }
                                break;
                            case "#BinSteps":
                                if (double.TryParse(keys[1].Trim(), out var binSteps))
                                {
                                    def.BinSteps = binSteps;
                                }
                                break;
                            case "#BinTime":
                                if (double.TryParse(keys[1].Trim(), out var binTime))
                                {
                                    def.BinTime = binTime;
                                }
                                break;
                            case "#StepsPerSec":
                                if (double.TryParse(keys[1].Trim(), out var stepsPerSec))
                                {
                                    def.StepsPerSec = stepsPerSec;
                                }
                                break;
                            case "#StepsPerRev":
                                if (double.TryParse(keys[1].Trim(), out var stepsPerRev))
                                {
                                    def.StepsPerRev = stepsPerRev;
                                }
                                break;
                            case "#InvertCapture":
                                if (bool.TryParse(keys[1].Trim(), out var invertCapture))
                                {
                                    def.InvertCapture = invertCapture;
                                }
                                break;
                            case "#FileName":
                                if (File.Exists(keys[1].Trim()))
                                {
                                    def.FileName = keys[1].Trim();
                                }
                                break;
                            case "#FileType":
                                if (Enum.TryParse<PecFileType>(keys[1].Trim(), true, out var fileType))
                                {
                                    def.FileType = fileType;
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

            if (def.FileType != PecFileType.GsPecWorm && def.FileType != PecFileType.GsPec360)
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
                case PecFileType.GsPecWorm:
                    if (def.BinCount == bins.Count) { break; }
                    paramError = true;
                    msg = $"BinCount {PecFileType.GsPecWorm}";
                    break;
                case PecFileType.GsPec360:
                    if (bins.Count == (int)(def.StepsPerRev / def.BinSteps)) { break; }
                    paramError = true;
                    msg = $"BinCount {PecFileType.GsPec360}";
                    break;
                case PecFileType.GsPecDebug:
                    paramError = true;
                    msg = $"BinCount {PecFileType.GsPecDebug}";
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
                case PecFileType.GsPecWorm:
                    var master = MakeWormMaster(bins);
                    UpdateWormMaster(master, PecMergeType.Replace);
                    SkySettings.PecWormFile = fileName;
                    break;
                case PecFileType.GsPec360:
                    SkySettings.Pec360File = fileName;
                    break;
                case PecFileType.GsPecDebug:
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