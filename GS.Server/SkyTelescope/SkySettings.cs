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
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _canEquatorial;
        public static bool CanEquatorial
        {
            get => _canEquatorial;
            private set
            {
                if (_canEquatorial == value) return;
                _canEquatorial = value;
                Properties.SkyTelescope.Default.CanEquatorial = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetPierSide;
        public static bool CanSetPierSide
        {
            get => _canSetPierSide;
            private set
            {
                if (_canSetPierSide == value) return;
                _canSetPierSide = value;
                Properties.SkyTelescope.Default.CanSetPierSide = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static SerialSpeed _baudRate;
        public static SerialSpeed BaudRate
        {
            get => _baudRate;
            set
            {
                if (_baudRate == value) return;
                _baudRate = value;
                Properties.SkyTelescope.Default.BaudRate = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
            }
        }

        private static int _gpsComPort;
        public static int GpsComPort
        {
            get => _gpsComPort;
            set
            {
                if (_gpsComPort == value) return;
                _gpsComPort = value;
                Properties.SkyTelescope.Default.GpsPort = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static SerialSpeed _gpsBaudRate;
        public static SerialSpeed GpsBaudRate
        {
            get => _gpsBaudRate;
            set
            {
                if (_gpsBaudRate == value) return;
                _gpsBaudRate = value;
                Properties.SkyTelescope.Default.GpsBaudRate = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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
                Properties.SkyTelescope.Default.HcSpeed = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
            }
        }

        private static Handshake _handShake;
        public static Handshake HandShake
        {
            get => _handShake;
            private set
            {
                if (_handShake == value) return;
                _handShake = value;
                Properties.SkyTelescope.Default.HandShake = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
            }
        }

        private static HCMode _hcMode;
        public static HCMode HcMode
        {
            get => _hcMode;
            set
            {
                if (_hcMode == value) return;
                _hcMode = value;
                Properties.SkyTelescope.Default.HCMode = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
            }
        }

        private static MountType _mount;
        public static MountType Mount
        {
            get => _mount;
            set
            {
                if (Mount == value) return;
                _mount = value;
                Properties.SkyTelescope.Default.Mount = $"{value}";
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                SkyServer.IsMountRunning = false;
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.AlternatingPpec);
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _comPort;
        public static int ComPort
        {
            get => _comPort;
            set
            {
                if (_comPort == value) return;
                _comPort = value;
                Properties.SkyTelescope.Default.ComPort = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _dataBits;
        public static int DataBits
        {
            get => _dataBits;
            private set
            {
                if (_dataBits == value) return;
                _dataBits = value;
                Properties.SkyTelescope.Default.DataBits = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _decPulseToGoTo;
        public static bool DecPulseToGoTo
        {
            get => _decPulseToGoTo;
            set
            {
                if (_decPulseToGoTo == value) return;
                _decPulseToGoTo = value;
                Properties.SkyTelescope.Default.DecPulseToGoTo = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.DecPulseToGoTo);
            }
        }

        private static bool _dtrEnable;
        public static bool DtrEnable
        {
            get => _dtrEnable;
            private set
            {
                if (_dtrEnable == value) return;
                _dtrEnable = value;
                Properties.SkyTelescope.Default.DTREnable = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                SkyServer.SkyTasks(MountTaskName.Encoders);
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                SkyServer.SkyTasks(MountTaskName.FullCurrent);
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _guideRateOffsetX;
        public static double GuideRateOffsetX
        {
            get => _guideRateOffsetX;
            set
            {
                if (Math.Abs(_guideRateOffsetX - value) < 0.0000000000001) return;
                _guideRateOffsetX = value;
                Properties.SkyTelescope.Default.GuideRateOffsetX = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                if (Math.Abs(GuideRateOffsetY - value) < 0.0000000000001) return;
                _guideRateOffsetY = value;
                Properties.SkyTelescope.Default.GuideRateOffsetY = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                if (Math.Abs(_homeAxisX - value) <= 0.0000000000001) return;
                _homeAxisX = value;
                Properties.SkyTelescope.Default.HomeAxisX = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _homeAxisY;
        public static double HomeAxisY
        {
            get => _homeAxisY;
            private set
            {
                if (Math.Abs(_homeAxisY - value) <= 0.0000000000001) return;
                _homeAxisY = value;
                Properties.SkyTelescope.Default.HomeAxisY = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _hourAngleLimit;
        public static double HourAngleLimit
        {
            get => _hourAngleLimit;
            set
            {
                if (Math.Abs(HourAngleLimit - value) < 0.0000000000001) return;
                _hourAngleLimit = value;
                Properties.SkyTelescope.Default.HourAngleLimit = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _kingRate;
        public static double KingRate
        {
            get => _kingRate;
            set
            {
                if (Math.Abs(KingRate - value) < 0.0000000000001) return;
                _kingRate = value;
                Properties.SkyTelescope.Default.KingRate = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _latitude;
        public static double Latitude
        {
            get => _latitude;
            set
            {
                if (Math.Abs(Latitude - value) < 0.0000000000001) return;
                _latitude = value;
                Properties.SkyTelescope.Default.Latitude = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                SkyServer.SkyTasks(MountTaskName.SetSouthernHemisphere);
                OnStaticPropertyChanged();
            }
        }

        private static bool _limitTracking;
        public static bool LimitTracking
        {
            get => _limitTracking;
            set
            {
                if (_limitTracking == value) return;
                _limitTracking = value;
                Properties.SkyTelescope.Default.LimitTracking = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _longitude;
        public static double Longitude
        {
            get => _longitude;
            set
            {
                if (Math.Abs(Longitude - value) < 0.0000000000001) return;
                _longitude = value;
                Properties.SkyTelescope.Default.Longitude = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _lunarRate;
        public static double LunarRate
        {
            get => _lunarRate;
            set
            {
                if (Math.Abs(LunarRate - value) < 0.0000000000001) return;
                _lunarRate = value;
                Properties.SkyTelescope.Default.LunarRate = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _maxSlewRate;
        public static double MaxSlewRate
        {
            get => _maxSlewRate;
            set
            {
                if (Math.Abs(MaxSlewRate - value) < 0.0000000000001) return;
                _maxSlewRate = value;
                Properties.SkyTelescope.Default.MaximumSlewRate = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SetSlewRates(value);
            }
        }

        private static int _minPulseDec;
        public static int MinPulseDec
        {
            get => _minPulseDec;
            set
            {
                if (_minPulseDec == value) return;
                _minPulseDec = value;
                Properties.SkyTelescope.Default.MinPulseDec = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.MinPulseDec);
            }
        }

        private static int _minPulseRa;
        public static int MinPulseRa
        {
            get => _minPulseRa;
            set
            {
                if (_minPulseRa == value) return;
                _minPulseRa = value;
                Properties.SkyTelescope.Default.MinPulseRa = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.MinPulseRa);
            }
        }

        private static bool _modelOn;
        public static bool ModelOn
        {
            get => _modelOn;
            set
            {
                if (_modelOn == value) return;
                _modelOn = value;
                Properties.SkyTelescope.Default.ModelOn = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _parkAxisX;
        public static double ParkAxisX
        {
            get => _parkAxisX;
            set
            {
                if (Math.Abs(_parkAxisX - value) <= 0.0000000000001) return;
                _parkAxisX = value;
                Properties.SkyTelescope.Default.ParkAxisX = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _parkAxisY;
        public static double ParkAxisY
        {
            get => _parkAxisY;
            set
            {
                if (Math.Abs(_parkAxisY - value) <= 0.0000000000001) return;
                _parkAxisY = value;
                Properties.SkyTelescope.Default.ParkAxisY = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _readTimeout;
        public static int ReadTimeout
        {
            get => _readTimeout;
            private set
            {
                if (_readTimeout == value) return;
                _readTimeout = value;
                Properties.SkyTelescope.Default.ReadTimeout = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _rtsEnable;
        public static bool RtsEnable
        {
            get => _rtsEnable;
            private set
            {
                if (_rtsEnable == value) return;
                _rtsEnable = value;
                Properties.SkyTelescope.Default.RTSEnable = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _siderealRate;
        public static double SiderealRate
        {
            get => _siderealRate;
            set
            {
                if (Math.Abs(_siderealRate - value) < 0.0000000000001) return;
                _siderealRate = value;
                Properties.SkyTelescope.Default.SiderealRate = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _displayInterval;
        public static int DisplayInterval
        {
            get => _displayInterval;
            set
            {
                if (_displayInterval == value) return;
                _displayInterval = value;
                Properties.SkyTelescope.Default.DisplayInterval = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.SetSt4Guiderate);
            }
        }

        private static double _solarRate;
        public static double SolarRate
        {
            get => _solarRate;
            set
            {
                if (Math.Abs(SolarRate - value) < 0.0000000000001) return;
                _solarRate = value;
                Properties.SkyTelescope.Default.SolarRate = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _syncLimit;
        public static int SyncLimit
        {
            get => _syncLimit;
            private set
            {
                if (_syncLimit == value) return;
                _syncLimit = value;
                Properties.SkyTelescope.Default.SyncLimit = value;
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static TimeSpan _uTCDateOffset;
        public static TimeSpan UTCDateOffset
        {
            get => _uTCDateOffset;
            set
            {
                if (_uTCDateOffset == value) return;
                _uTCDateOffset = value;
                Properties.SkyTelescope.Default.UTCOffset = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static List<ParkPosition> _parkPositions;
        public static List<ParkPosition> ParkPositions
        {
            get => _parkPositions;
            set
            {
                // if (_parkPositions == value) return;
                _parkPositions = value.OrderBy(ParkPosition => ParkPosition.Name).ToList();
                var output = JsonConvert.SerializeObject(_parkPositions);
                Properties.SkyTelescope.Default.ParkPositions = output;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{output}");
                OnStaticPropertyChanged();
            }
        }

        private static string _parkName;
        public static string ParkName
        {
            get => _parkName;
            set
            {
                if (_parkName == value) return;
                _parkName = value;
                Properties.SkyTelescope.Default.ParkName = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// will upgrade if necessary
        /// </summary>
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
            var hcmodebol = Enum.TryParse<HCMode>(Properties.SkyTelescope.Default.HCMode, true, out var hcparse);
            if (!hcmodebol) hcparse = HCMode.Guiding;// getting rid of compass mode
            HcMode = hcparse;
            Enum.TryParse<MountType>(Properties.SkyTelescope.Default.Mount, true, out var mountparse);
            Mount = mountparse;
            Enum.TryParse<Handshake>(Properties.SkyTelescope.Default.HandShake, true, out var hsparse);
            HandShake = hsparse;
            Enum.TryParse<SerialSpeed>(Properties.SkyTelescope.Default.BaudRate, true, out var brateparse);
            BaudRate = brateparse;
            Enum.TryParse<SerialSpeed>(Properties.SkyTelescope.Default.GpsBaudRate, true, out var grateparse);
            GpsBaudRate = grateparse;

            AlternatingPpec = Properties.SkyTelescope.Default.AlternatingPPEC;
            ApertureArea = Properties.SkyTelescope.Default.ApertureArea;
            ApertureDiameter = Properties.SkyTelescope.Default.ApertureDiameter;
            AtPark = Properties.SkyTelescope.Default.AtPark;
            AutoTrack = Properties.SkyTelescope.Default.AutoTrack;
            ComPort = Properties.SkyTelescope.Default.ComPort;
            DataBits = Properties.SkyTelescope.Default.DataBits;
            DecBacklash = Properties.SkyTelescope.Default.DecBacklash;
            DecPulseToGoTo = Properties.SkyTelescope.Default.DecPulseToGoTo;
            DtrEnable = Properties.SkyTelescope.Default.DTREnable;
            Elevation = Properties.SkyTelescope.Default.Elevation;
            Encoders = Properties.SkyTelescope.Default.EncodersOn;
            FocalLength = Properties.SkyTelescope.Default.FocalLength;
            FullCurrent = Properties.SkyTelescope.Default.FullCurrent;
            GotoPrecision = Properties.SkyTelescope.Default.GotoPrecision;
            GpsComPort = Properties.SkyTelescope.Default.GpsPort;
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
            LimitTracking = Properties.SkyTelescope.Default.LimitTracking;
            Longitude = Properties.SkyTelescope.Default.Longitude;
            LunarRate = Properties.SkyTelescope.Default.LunarRate;
            MaxSlewRate = Properties.SkyTelescope.Default.MaximumSlewRate;
            MinPulseDec = Properties.SkyTelescope.Default.MinPulseDec;
            MinPulseRa = Properties.SkyTelescope.Default.MinPulseRa;
            ModelOn = Properties.SkyTelescope.Default.ModelOn;
            ParkAxisX = Properties.SkyTelescope.Default.ParkAxisX;
            ParkAxisY = Properties.SkyTelescope.Default.ParkAxisY;
            ParkName = Properties.SkyTelescope.Default.ParkName;
            RaBacklash = Properties.SkyTelescope.Default.RaBacklash;
            ReadTimeout = Properties.SkyTelescope.Default.ReadTimeout;
            Refraction = Properties.SkyTelescope.Default.Refraction;
            RaTrackingOffset = Properties.SkyTelescope.Default.RATrackingOffset;
            RtsEnable = Properties.SkyTelescope.Default.RTSEnable;
            SiderealRate = Properties.SkyTelescope.Default.SiderealRate;
            DisplayInterval = Properties.SkyTelescope.Default.DisplayInterval;
            SolarRate = Properties.SkyTelescope.Default.SolarRate;
            St4Guiderate = Properties.SkyTelescope.Default.St4Guiderate;
            SyncLimit = Properties.SkyTelescope.Default.SyncLimit;
            Temperature = Properties.SkyTelescope.Default.Temperature;
            UTCDateOffset = Properties.SkyTelescope.Default.UTCOffset;

            //first time load from old park positions
            var pp = Properties.SkyTelescope.Default.ParkPositions;
            if (string.IsNullOrEmpty(pp))
            {
                var pp1 = new ParkPosition { Name = "Default", X = ParkAxisX, Y = ParkAxisY };
                var pp2 = new ParkPosition { Name = "Home", X = 90, Y = 90 };
                var pps = new List<ParkPosition> { pp1, pp2 };
                pp = JsonConvert.SerializeObject(pps);
                Properties.SkyTelescope.Default.ParkPositions = pp;
            }

            // Json items
            ParkPositions = JsonConvert.DeserializeObject<List<ParkPosition>>(pp);
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        public static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.SkyTelescope.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.SkyTelescope.Default.Upgrade();
            Properties.SkyTelescope.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.SkyTelescope.Default.Save();
            Properties.SkyTelescope.Default.Reload();
        }

        /// <summary>
        /// output to session log
        /// </summary>
        /// <param name="method"></param>
        /// <param name="value"></param>
        private static void LogSetting(string method, string value)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = $"{method}", Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// property event notification
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
