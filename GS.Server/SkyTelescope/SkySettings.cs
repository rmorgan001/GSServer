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
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ASCOM.DeviceInterface;
using GS.Server.Helpers;
using GS.Shared;

namespace GS.Server.SkyTelescope
{
    public static class SkySettings
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Capabilities

        private static bool _canAlignMode;
        public static bool CanAlignMode
        {
            get => _canAlignMode;
            private set
            {
                if (_canAlignMode == value) return;
                _canAlignMode = value;
                Properties.SkyTelescope.Default.CanAlignMode = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canAltAz;
        public static bool CanAltAz
        {
            get => _canAltAz;
            private set
            {
                if (_canAltAz == value) return;
                _canAltAz = value;
                Properties.SkyTelescope.Default.CanAltAz = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canDoesRefraction;
        public static bool CanDoesRefraction
        {
            get => _canDoesRefraction;
            set
            {
                if (_canDoesRefraction == value) return;
                _canDoesRefraction = value;
                Properties.SkyTelescope.Default.CanDoesRefraction = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canDualAxisPulseGuide;
        public static bool CanDualAxisPulseGuide
        {
            get => _canDualAxisPulseGuide;
            private set
            {
                if (_canDualAxisPulseGuide == value) return;
                _canDualAxisPulseGuide = value;
                Properties.SkyTelescope.Default.CanDualAxisPulseGuide = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canEquatorial;
        public static bool CanEquatorial
        {
            get => _canEquatorial;
            set
            {
                if (_canEquatorial == value) return;
                _canEquatorial = value;
                Properties.SkyTelescope.Default.CanEquatorial = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canFindHome;
        public static bool CanFindHome
        {
            get => _canFindHome;
            private set
            {
                if (_canFindHome == value) return;
                _canFindHome = value;
                Properties.SkyTelescope.Default.CanFindHome = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canLatLongElev;
        public static bool CanLatLongElev
        {
            get => _canLatLongElev;
            private set
            {
                if (_canLatLongElev == value) return;
                _canLatLongElev = value;
                Properties.SkyTelescope.Default.CanLatLongElev = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canOptics;
        public static bool CanOptics
        {
            get => _canOptics;
            private set
            {
                if (_canOptics == value) return;
                _canOptics = value;
                Properties.SkyTelescope.Default.CanOptics = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canPark;
        public static bool CanPark
        {
            get => _canPark;
            private set
            {
                if (_canPark == value) return;
                _canPark = value;
                Properties.SkyTelescope.Default.CanPark = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canPulseGuide;
        public static bool CanPulseGuide
        {
            get => _canPulseGuide;
            private set
            {
                if (_canPulseGuide == value) return;
                _canPulseGuide = value;
                Properties.SkyTelescope.Default.CanPulseGuide = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetEquRates;
        public static bool CanSetEquRates
        {
            get => _canSetEquRates;
            private set
            {
                if (_canSetEquRates == value) return;
                _canSetEquRates = value;
                Properties.SkyTelescope.Default.CanSetEquRates = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetDeclinationRate;
        public static bool CanSetDeclinationRate
        {
            get => _canSetDeclinationRate;
            private set
            {
                if (_canSetDeclinationRate == value) return;
                _canSetDeclinationRate = value;
                Properties.SkyTelescope.Default.CanSetDeclinationRate = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetGuideRates;
        public static bool CanSetGuideRates
        {
            get => _canSetGuideRates;
            private set
            {
                if (_canSetGuideRates == value) return;
                _canSetGuideRates = value;
                Properties.SkyTelescope.Default.CanSetGuideRates = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetPark;
        public static bool CanSetPark
        {
            get => _canSetPark;
            private set
            {
                if (_canSetPark == value) return;
                _canSetPark = value;
                Properties.SkyTelescope.Default.CanSetPark = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetPierSide;
        public static bool CanSetPierSide
        {
            get => _canSetPierSide;
            set
            {
                if (_canSetPierSide == value) return;
                _canSetPierSide = value;
                Properties.SkyTelescope.Default.CanSetPierSide = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetRightAscensionRate;
        public static bool CanSetRightAscensionRate
        {
            get => _canSetRightAscensionRate;
            private set
            {
                if (_canSetRightAscensionRate == value) return;
                _canSetRightAscensionRate = value;
                Properties.SkyTelescope.Default.CanSetRightAscensionRate = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetTracking;
        public static bool CanSetTracking
        {
            get => _canSetTracking;
            private set
            {
                if (_canSetTracking == value) return;
                _canSetTracking = value;
                Properties.SkyTelescope.Default.CanSetTracking = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSiderealTime;
        public static bool CanSiderealTime
        {
            get => _canSiderealTime;
            private set
            {
                if (_canSiderealTime == value) return;
                _canSiderealTime = value;
                Properties.SkyTelescope.Default.CanSiderealTime = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSlew;
        public static bool CanSlew
        {
            get => _canSlew;
            private set
            {
                if (_canSlew == value) return;
                _canSlew = value;
                Properties.SkyTelescope.Default.CanSlew = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSlewAltAz;
        public static bool CanSlewAltAz
        {
            get => _canSlewAltAz;
            private set
            {
                if (_canSlewAltAz == value) return;
                _canSlewAltAz = value;
                Properties.SkyTelescope.Default.CanSlewAltAz = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSlewAltAzAsync;
        public static bool CanSlewAltAzAsync
        {
            get => _canSlewAltAzAsync;
            private set
            {
                if (_canSlewAltAzAsync == value) return;
                _canSlewAltAzAsync = value;
                Properties.SkyTelescope.Default.CanSlewAltAzAsync = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSlewAsync;
        public static bool CanSlewAsync
        {
            get => _canSlewAsync;
            private set
            {
                if (_canSlewAsync == value) return;
                _canSlewAsync = value;
                Properties.SkyTelescope.Default.CanSlewAsync = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSync;
        public static bool CanSync
        {
            get => _canSync;
            private set
            {
                if (_canSync == value) return;
                _canSync = value;
                Properties.SkyTelescope.Default.CanSync = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSyncAltAz;
        public static bool CanSyncAltAz
        {
            get => _canSyncAltAz;
            private set
            {
                if (_canSyncAltAz == value) return;
                _canSyncAltAz = value;
                Properties.SkyTelescope.Default.CanSyncAltAz = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canTrackingRates;
        public static bool CanTrackingRates
        {
            get => _canTrackingRates;
            private set
            {
                if (_canTrackingRates == value) return;
                _canTrackingRates = value;
                Properties.SkyTelescope.Default.CanTrackingRates = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _canUnpark;
        public static bool CanUnpark
        {
            get => _canUnpark;
            private set
            {
                if (_canUnpark == value) return;
                _canUnpark = value;
                Properties.SkyTelescope.Default.CanUnpark = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _noSyncPastMeridian;
        public static bool NoSyncPastMeridian
        {
            get => _noSyncPastMeridian;
            private set
            {
                if (_noSyncPastMeridian == value) return;
                _noSyncPastMeridian = value;
                Properties.SkyTelescope.Default.NoSyncPastMeridian = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _numMoveAxis;
        public static int NumMoveAxis
        {
            get => _numMoveAxis;
            private set
            {
                if (_numMoveAxis == value) return;
                _numMoveAxis = value;
                Properties.SkyTelescope.Default.NumMoveAxis = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _versionOne;
        public static bool VersionOne
        {
            get => _versionOne;
            private set
            {
                if (_versionOne == value) return;
                _versionOne = value;
                Properties.SkyTelescope.Default.VersionOne = value;
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Server Settings

        private static AlignmentModes _alignmentMode;
        public static AlignmentModes AlignmentMode
        {
            get => _alignmentMode;
            set
            {
                if (_alignmentMode == value) return;
                _alignmentMode = value;
                Properties.SkyTelescope.Default.AlignmentMode = value.ToString();
                OnStaticPropertyChanged();
            }
        }

        private static EquatorialCoordinateType _equatorialCoordinateType;
        public static EquatorialCoordinateType EquatorialCoordinateType
        {
            get => _equatorialCoordinateType;
            set
            {
                if (_equatorialCoordinateType == value) return;
                _equatorialCoordinateType = value;
                Properties.SkyTelescope.Default.EquatorialCoordinateType = value.ToString();
                OnStaticPropertyChanged();
            }
        }

        private static DriveRates _trackingRate;
        public static DriveRates TrackingRate
        {
            get => _trackingRate;
            set
            {
                if (_trackingRate == value) return;
                _trackingRate = value;
                Properties.SkyTelescope.Default.TrackingRate = value.ToString();
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static SlewSpeed _hcSpeed;
        public static SlewSpeed HcSpeed
        {
            get => _hcSpeed;
            set
            {
                if (_hcSpeed == value) return;
                _hcSpeed = value;
                Synthesizer.Speak(value.ToString());
                Properties.SkyTelescope.Default.HcSpeed = value.ToString();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool _alternatingPpec;
        public static bool AlternatingPpec
        {
            get => _alternatingPpec;
            set
            {
                if (_alternatingPpec == value) return;
                _alternatingPpec = value;
                Properties.SkyTelescope.Default.AlternatingPPEC = value;
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.AlternatingPpec);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _apertureArea;
        public static double ApertureArea
        {
            get => _apertureArea;
            private set
            {
                if (Math.Abs(_apertureArea - value) < 0.0) return;
                _apertureArea = value;
                Properties.SkyTelescope.Default.ApertureArea = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _apertureDiameter;
        public static double ApertureDiameter
        {
            get => _apertureDiameter;
            private set
            {
                if (Math.Abs(_apertureDiameter - value) < 0.0) return;
                _apertureDiameter = value;
                Properties.SkyTelescope.Default.ApertureDiameter = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _atPark;
        public static bool AtPark
        {
            get => _atPark;
            set
            {
                if (AtPark == value) return;
                _atPark = value;
                Properties.SkyTelescope.Default.AtPark = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem); 
            }
        }

        private static bool _autoTrack;
        public static bool AutoTrack
        {
            get => _autoTrack;
            private set
            {
                if (_autoTrack == value) return;
                _autoTrack = value;
                Properties.SkyTelescope.Default.AutoTrack = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _decBacklash;
        public static int DecBacklash
        {
            get => _decBacklash;
            set
            {
                if (DecBacklash == value) return;
                _decBacklash = value;
                Properties.SkyTelescope.Default.DecBacklash = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _elevation;
        public static double Elevation
        {
            get => _elevation;
            set
            {
                if (Math.Abs(_elevation - value) <= 0) return;
                _elevation = value;
                Properties.SkyTelescope.Default.Elevation = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool _encoders;
        public static bool Encoders
        {
            get => _encoders;
            set
            {
                if (_encoders == value) return;
                _encoders = value;
                Properties.SkyTelescope.Default.EncodersOn = value;
                SkyServer.SkyTasks(MountTaskName.Encoders);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool _fullCurrent;
        public static bool FullCurrent
        {
            get => _fullCurrent;
            set
            {
                if (_fullCurrent == value) return;
                _fullCurrent = value;
                Properties.SkyTelescope.Default.FullCurrent = value;
                SkyServer.SkyTasks(MountTaskName.FullCurrent);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _focalLength;
        public static double FocalLength
        {
            get => _focalLength;
            private set
            {
                if (Math.Abs(_focalLength - value) <= 0) return;
                _focalLength = value;
                Properties.SkyTelescope.Default.FocalLength = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _gotoPrecision;
        public static double GotoPrecision
        {
            get => _gotoPrecision;
            private set
            {
                _gotoPrecision = value;
                Properties.SkyTelescope.Default.GotoPrecision = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _guideRateOffsetX;
        public static double GuideRateOffsetX
        {
            get => _guideRateOffsetX;
            set
            {
                if (Math.Abs(_guideRateOffsetX - value) < 0.0) return;
                _guideRateOffsetX = value;
                Properties.SkyTelescope.Default.GuideRateOffsetX = value;
                OnStaticPropertyChanged();
                SkyServer.SetGuideRates();
            }
        }

        private static double _guideRateOffsetY;
        public static double GuideRateOffsetY
        {
            get => _guideRateOffsetY;
            set
            {
                if (Math.Abs(GuideRateOffsetY - value) < 0.00001) return;
                _guideRateOffsetY = value;
                Properties.SkyTelescope.Default.GuideRateOffsetY = value;
                OnStaticPropertyChanged();
                SkyServer.SetGuideRates();
            }
        }

        private static double _homeAxisX;
        public static double HomeAxisX
        {
            get => _homeAxisX;
            private set
            {
                if (Math.Abs(_homeAxisX - value) <= 0) return;
                _homeAxisX = value;
                Properties.SkyTelescope.Default.HomeAxisX = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _homeAxisY;
        public static double HomeAxisY
        {
            get => _homeAxisY;
            private set
            {
                if (Math.Abs(_homeAxisY - value) <= 0) return;
                _homeAxisY = value;
                Properties.SkyTelescope.Default.HomeAxisY = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _hourAngleLimit;
        public static double HourAngleLimit
        {
            get => _hourAngleLimit;
            set
            {
                if (Math.Abs(HourAngleLimit - value) < 0.00001) return;
                _hourAngleLimit = value;
                Properties.SkyTelescope.Default.HourAngleLimit = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool _homeWarning;
        public static bool HomeWarning
        {
            get => _homeWarning;
            set
            {
                if (_homeWarning == value) return;
                _homeWarning = value;
                Properties.SkyTelescope.Default.HomeWarning = value;
                OnStaticPropertyChanged();
            }
        }

        private static string _instrumentDescription;
        public static string InstrumentDescription
        {
            get => _instrumentDescription;
            private set
            {
                if (_instrumentDescription == value) return;
                _instrumentDescription = value;
                Properties.SkyTelescope.Default.InstrumentDescription = value;
                OnStaticPropertyChanged();
            }
        }

        private static string _instrumentName;
        public static string InstrumentName
        {
            get => _instrumentName;
            private set
            {
                if (_instrumentName == value) return;
                _instrumentName = value;
                Properties.SkyTelescope.Default.InstrumentName = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _kingRate;
        public static double KingRate
        {
            get => _kingRate;
            set
            {
                if (Math.Abs(KingRate - value) < 0.0000000001) return;
                _kingRate = value;
                Properties.SkyTelescope.Default.KingRate = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
        }
        
        private static double _latitude;
        public static double Latitude
        {
            get => _latitude;
            set
            {
                if (Math.Abs(Latitude - value) < 0.00001) return;
                _latitude = value;
                Properties.SkyTelescope.Default.Latitude = value;

                SkyServer.SkyTasks(MountTaskName.SetSouthernHemisphere);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        private static double _longitude;
        public static double Longitude
        {
            get => _longitude;
            set
            {
                if (Math.Abs(Longitude - value) < 0.00001) return;
                _longitude = value;
                Properties.SkyTelescope.Default.Longitude = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                OnStaticPropertyChanged();
            }
        }

        private static double _lunarRate;
        public static double LunarRate
        {
            get => _lunarRate;
            set
            {
                if (Math.Abs(LunarRate - value) < 0.0000000001) return;
                _lunarRate = value;
                Properties.SkyTelescope.Default.LunarRate = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _maxSlewRate;
        public static double MaxSlewRate
        {
            get => _maxSlewRate;
            set
            {
                if (Math.Abs(MaxSlewRate - value) < 0.00001) return;
                _maxSlewRate = value;
                Properties.SkyTelescope.Default.MaximumSlewRate = value;
                OnStaticPropertyChanged();
                SkyServer.SetSlewRates(value);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _parkAxisX;
        public static double ParkAxisX
        {
            get => _parkAxisX;
            set
            {
                if (Math.Abs(_parkAxisX - value) <= 0) return;
                _parkAxisX = value;
                Properties.SkyTelescope.Default.ParkAxisX = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _parkAxisY;
        public static double ParkAxisY
        {
            get => _parkAxisY;
            set
            {
                if (Math.Abs(_parkAxisY - value) <= 0) return;
                _parkAxisY = value;
                Properties.SkyTelescope.Default.ParkAxisY = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _raBacklash;
        public static int RaBacklash
        {
            get => _raBacklash;
            private set
            {
                if (RaBacklash == value) return;
                _raBacklash = value;
                Properties.SkyTelescope.Default.RaBacklash = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool _refraction;
        public static bool Refraction
        {
            get => _refraction;
            set
            {
                if (_refraction == value) return;
                _refraction = value;
                Properties.SkyTelescope.Default.Refraction = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static int _raTrackingOffset;
        public static int RaTrackingOffset
        {
            get => _raTrackingOffset;
            private set
            {
                if (_raTrackingOffset.Equals(value)) return;
                _raTrackingOffset = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _siderealRate;
        public static double SiderealRate
        {
            get => _siderealRate;
            set
            {
                if (Math.Abs(_siderealRate - value) < 0.0000000001) return;
                _siderealRate = value;
                Properties.SkyTelescope.Default.SiderealRate = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static int _simInterval;
        public static int SimInterval
        {
            get => _simInterval;
            set
            {
                if (_simInterval == value) return;
                _simInterval = value;
                Properties.SkyTelescope.Default.SimulatorInterval = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _skyInterval;
        public static int SkyInterval
        {
            get => _skyInterval;
            set
            {
                if (SkyInterval == value) return;
                _skyInterval = value;
                Properties.SkyTelescope.Default.SkyInterval = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _st4Guiderate;
        public static int St4Guiderate
        {
            get => _st4Guiderate;
            set
            {
                if (St4Guiderate == value) return;
                _st4Guiderate = value;
                Properties.SkyTelescope.Default.St4Guiderate = value;
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.SetSt4Guiderate);

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _solarRate;
        public static double SolarRate
        {
            get => _solarRate;
            set
            {
                if (Math.Abs(SolarRate - value) < 0.0000000001) return;
                _solarRate = value;
                Properties.SkyTelescope.Default.SolarRate = value;
                OnStaticPropertyChanged();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static double _temperature;
        public static double Temperature
        {
            get => _temperature;
            set
            {
                if (Math.Abs(_temperature - value) < 0) return;
                _temperature = value;
                Properties.SkyTelescope.Default.Temperature = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _uTCDateOffset;
        public static int UTCDateOffset
        {
            get => _uTCDateOffset;
            set
            {
                if (_uTCDateOffset == value) return;
                _uTCDateOffset = value;
                Properties.SkyTelescope.Default.UTCDateOffset = value;
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public static void Load()
        {
            Upgrade();

            //capabilities
            CanAlignMode = Properties.SkyTelescope.Default.CanAlignMode;
            CanAltAz = Properties.SkyTelescope.Default.CanAltAz;
            CanDoesRefraction = Properties.SkyTelescope.Default.CanDoesRefraction;
            CanDualAxisPulseGuide = Properties.SkyTelescope.Default.CanDualAxisPulseGuide;
            CanEquatorial = Properties.SkyTelescope.Default.CanEquatorial;
            CanFindHome = Properties.SkyTelescope.Default.CanFindHome;
            CanLatLongElev = Properties.SkyTelescope.Default.CanLatLongElev;
            CanOptics = Properties.SkyTelescope.Default.CanOptics;
            CanPark = Properties.SkyTelescope.Default.CanPark;
            CanPulseGuide = Properties.SkyTelescope.Default.CanPulseGuide;
            CanSetEquRates = Properties.SkyTelescope.Default.CanSetEquRates;
            CanSetDeclinationRate = Properties.SkyTelescope.Default.CanSetDeclinationRate;
            CanSetGuideRates = Properties.SkyTelescope.Default.CanSetGuideRates;
            CanSetPark = Properties.SkyTelescope.Default.CanSetPark;
            CanSetPierSide = Properties.SkyTelescope.Default.CanSetPierSide;
            CanSetRightAscensionRate = Properties.SkyTelescope.Default.CanSetRightAscensionRate;
            CanSetTracking = Properties.SkyTelescope.Default.CanSetTracking;
            CanSiderealTime = Properties.SkyTelescope.Default.CanSiderealTime;
            CanSlew = Properties.SkyTelescope.Default.CanSlew;
            CanSlewAltAz = Properties.SkyTelescope.Default.CanSlewAltAz;
            CanSlewAltAzAsync = Properties.SkyTelescope.Default.CanSlewAltAzAsync;
            CanSlewAsync = Properties.SkyTelescope.Default.CanSlewAsync;
            CanSync = Properties.SkyTelescope.Default.CanSync;
            CanSyncAltAz = Properties.SkyTelescope.Default.CanSyncAltAz;
            CanTrackingRates = Properties.SkyTelescope.Default.CanTrackingRates;
            CanUnpark = Properties.SkyTelescope.Default.CanUnpark;
            NoSyncPastMeridian = Properties.SkyTelescope.Default.NoSyncPastMeridian;
            NumMoveAxis = Properties.SkyTelescope.Default.NumMoveAxis;
            VersionOne = Properties.SkyTelescope.Default.VersionOne;

            //Server

            Enum.TryParse<EquatorialCoordinateType>(Properties.SkyTelescope.Default.EquatorialCoordinateType, true, out var eparse);
            EquatorialCoordinateType = eparse;
            Enum.TryParse<AlignmentModes>(Properties.SkyTelescope.Default.AlignmentMode, true, out var aparse);
            AlignmentMode = aparse;
            Enum.TryParse<DriveRates>(Properties.SkyTelescope.Default.TrackingRate, true, out var dparse);
            TrackingRate = dparse;
            Enum.TryParse<SlewSpeed>(Properties.SkyTelescope.Default.HcSpeed, true, out var hparse);
            HcSpeed = hparse;

            AlternatingPpec = Properties.SkyTelescope.Default.AlternatingPPEC;
            ApertureArea = Properties.SkyTelescope.Default.ApertureArea;
            ApertureDiameter = Properties.SkyTelescope.Default.ApertureDiameter;
            AtPark = Properties.SkyTelescope.Default.AtPark;
            AutoTrack = Properties.SkyTelescope.Default.AutoTrack;
            DecBacklash = Properties.SkyTelescope.Default.DecBacklash;
            Elevation = Properties.SkyTelescope.Default.Elevation;
            Encoders = Properties.SkyTelescope.Default.EncodersOn;
            FocalLength = Properties.SkyTelescope.Default.FocalLength;
            FullCurrent = Properties.SkyTelescope.Default.FullCurrent;
            GotoPrecision = Properties.SkyTelescope.Default.GotoPrecision;
            GuideRateOffsetY = Properties.SkyTelescope.Default.GuideRateOffsetY;
            GuideRateOffsetX = Properties.SkyTelescope.Default.GuideRateOffsetX;
            HomeAxisX = Properties.SkyTelescope.Default.HomeAxisX;
            HomeAxisY = Properties.SkyTelescope.Default.HomeAxisY;
            HourAngleLimit = Properties.SkyTelescope.Default.HourAngleLimit;
            HomeWarning = Properties.SkyTelescope.Default.HomeWarning;
            InstrumentDescription = Properties.SkyTelescope.Default.InstrumentDescription;
            InstrumentName = Properties.SkyTelescope.Default.InstrumentName;
            KingRate = Properties.SkyTelescope.Default.KingRate;
            Latitude = Properties.SkyTelescope.Default.Latitude;
            Longitude = Properties.SkyTelescope.Default.Longitude;
            LunarRate = Properties.SkyTelescope.Default.LunarRate;
            MaxSlewRate = Properties.SkyTelescope.Default.MaximumSlewRate;
            ParkAxisX = Properties.SkyTelescope.Default.ParkAxisX;
            ParkAxisY = Properties.SkyTelescope.Default.ParkAxisY;
            RaBacklash = Properties.SkyTelescope.Default.RaBacklash;
            Refraction = Properties.SkyTelescope.Default.Refraction;
            RaTrackingOffset = Properties.SkyTelescope.Default.RATrackingOffset;
            SiderealRate = Properties.SkyTelescope.Default.SiderealRate;
            SimInterval = Properties.SkyTelescope.Default.SimulatorInterval;
            SkyInterval = Properties.SkyTelescope.Default.SkyInterval;
            SolarRate = Properties.SkyTelescope.Default.SolarRate;
            St4Guiderate = Properties.SkyTelescope.Default.St4Guiderate;
            Temperature = Properties.SkyTelescope.Default.Temperature;
            UTCDateOffset = Properties.SkyTelescope.Default.UTCDateOffset;
        }

        public static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.SkyTelescope.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.SkyTelescope.Default.Upgrade();
            Properties.SkyTelescope.Default.Version = assembly.ToString();
            Save();
        }

        public static void Save()
        {
            Properties.SkyTelescope.Default.Save();
            Properties.SkyTelescope.Default.Reload();
        }

        public static void LogSettings()
        {
           var settingsProperties = Properties.SkyTelescope.Default.Properties.OfType<SettingsProperty>().OrderBy(s => s.Name);
           const int itemsinrow = 4;
            var count = 0;
            var msg = string.Empty;
            foreach (var currentProperty in settingsProperties)
            {
                msg += $"{currentProperty.Name}={Properties.SkyTelescope.Default[currentProperty.Name]},";
                count ++;
                if (count < itemsinrow) continue;

                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{msg}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                count = 0;
                msg = string.Empty;
            }

            if (msg.Length <= 0) return;
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{msg}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static Dictionary<string,string> SettingsList()
        {
            var settingsProperties = Properties.SkyTelescope.Default.Properties.OfType<SettingsProperty>()
                .OrderBy(s => s.Name);

            return settingsProperties.ToDictionary(currentProperty => currentProperty.Name, currentProperty => Properties.SkyTelescope.Default[currentProperty.Name].ToString());
        }

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
