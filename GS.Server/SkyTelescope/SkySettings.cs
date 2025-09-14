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
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Pulses;
using GS.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;

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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        //private static bool _canDoesRefraction;  moved to Refraction property
        //public static bool CanDoesRefraction
        //{
        //    get => _canDoesRefraction;
        //    set
        //    {
        //        if (_canDoesRefraction == value) return;
        //        _canDoesRefraction = value;
        //        Properties.SkyTelescope.Default.CanDoesRefraction = value;
        //        Properties.SkyTelescope.Default.Refraction = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
        //        OnStaticPropertyChanged();
        //    }
        //}

        private static bool _canEquatorial;
        public static bool CanEquatorial
        {
            get => _canEquatorial;
            private set
            {
                if (_canEquatorial == value) return;
                _canEquatorial = value;
                Properties.SkyTelescope.Default.CanEquatorial = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _canSetPark;
        public static bool CanSetPark
        {
            get => _canSetPark;
            set
            {
                if (_canSetPark == value) return;
                _canSetPark = value;
                Properties.SkyTelescope.Default.CanSetPark = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _canUnPark;
        public static bool CanUnPark
        {
            get => _canUnPark;
            private set
            {
                if (_canUnPark == value) return;
                _canUnPark = value;
                Properties.SkyTelescope.Default.CanUnpark = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static Vector3 _axisModelOffsets;

        public static Vector3 AxisModelOffsets
        {
            get => _axisModelOffsets;

            private set
            {
                _axisModelOffsets = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _allowAdvancedCommandSet;
        public static bool AllowAdvancedCommandSet
        {
            get => _allowAdvancedCommandSet;
            set
            {
                if (_allowAdvancedCommandSet == value) return;
                _allowAdvancedCommandSet = value;
                Properties.SkyTelescope.Default.AllowAdvancedCommandSet = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static int _customDec360Steps;
        public static int CustomDec360Steps
        {
            get => _customDec360Steps;
            set
            {
                if (_customDec360Steps == value) return;
                _customDec360Steps = value;
                Properties.SkyTelescope.Default.CustomDec360Steps = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _customDecTrackingOffset;
        public static int CustomDecTrackingOffset
        {
            get => _customDecTrackingOffset;
            set
            {
                if (_customDecTrackingOffset == value) return;
                _customDecTrackingOffset = value;
                Properties.SkyTelescope.Default.CustomDecTrackingOffset = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _customDecWormTeeth;
        public static int CustomDecWormTeeth
        {
            get => _customDecWormTeeth;
            set
            {
                if (_customDecWormTeeth == value) return;
                _customDecWormTeeth = value;
                Properties.SkyTelescope.Default.CustomDecWormTeeth = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _customRa360Steps;
        public static int CustomRa360Steps
        {
            get => _customRa360Steps;
            set
            {
                if (_customRa360Steps == value) return;
                _customRa360Steps = value;
                Properties.SkyTelescope.Default.CustomRa360Steps = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _customRaTrackingOffset;
        public static int CustomRaTrackingOffset
        {
            get => _customRaTrackingOffset;
            set
            {
                if (_customRaTrackingOffset == value) return;
                _customRaTrackingOffset = value;
                Properties.SkyTelescope.Default.CustomRaTrackingOffset = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _customRaWormTeeth;
        public static int CustomRaWormTeeth
        {
            get => _customRaWormTeeth;
            set
            {
                if (_customRaWormTeeth == value) return;
                _customRaWormTeeth = value;
                Properties.SkyTelescope.Default.CustomRaWormTeeth = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _customGearing;
        public static bool CustomGearing
        {
            get => _customGearing;
            set
            {
                if (_customGearing == value) return;
                _customGearing = value;
                Properties.SkyTelescope.Default.CustomGearing = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static string _gpsComPort;
        public static string GpsComPort
        {
            get => _gpsComPort;
            set
            {
                if (_gpsComPort == value) return;
                _gpsComPort = value;
                var i = Strings.GetNumberFromString(value);
                var vi = i ?? 0;
                Properties.SkyTelescope.Default.GpsPort = vi;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{vi}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                if (value != DriveRates.driveSidereal)
                {
                    SkyServer.RateDecOrg = 0;
                    SkyServer.RateDec = 0;
                    SkyServer.RateRaOrg = 0;
                    SkyServer.RateRa = 0;
                }
                Properties.SkyTelescope.Default.TrackingRate = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static FrontGraphic _frontGraphic;
        public static FrontGraphic FrontGraphic
        {
            get => _frontGraphic;
            set
            {
                if (_frontGraphic == value) return;
                _frontGraphic = value;
                Properties.SkyTelescope.Default.FrontGraphic = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
            }
        }

        private static HcMode _hcMode;
        public static HcMode HcMode
        {
            get => _hcMode;
            set
            {
                if (_hcMode == value) return;
                _hcMode = value;
                Properties.SkyTelescope.Default.HCMode = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static MountType _mount;
        public static MountType Mount
        {
            get => _mount;
            set
            {
                if (_mount == value) return;
                _mount = value;
                Properties.SkyTelescope.Default.Mount = $"{value}";
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                SkyServer.IsMountRunning = false;
                OnStaticPropertyChanged();
            }
        }

        private static PecMode _pecMode;
        public static PecMode PecMode
        {
            get => _pecMode;
            set
            {
                if (_pecMode == value) return;
                _pecMode = value;
                Properties.SkyTelescope.Default.PecMode = $"{value}";
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static bool _alternatingPPec;
        public static bool AlternatingPPec
        {
            get => _alternatingPPec;
            set
            {
                if (_alternatingPPec == value) return;
                _alternatingPPec = value;
                Properties.SkyTelescope.Default.AlternatingPPEC = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _atPark;
        public static bool AtPark
        {
            get => _atPark;
            set
            {
                if (_atPark == value) return;
                _atPark = value;
                Properties.SkyTelescope.Default.AtPark = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _axisTrackingLimit;
        public static double AxisTrackingLimit
        {
            get => _axisTrackingLimit;
            set
            {
                if (Math.Abs(_axisTrackingLimit - value) < 0.0000000000001) return;
                _axisTrackingLimit = value;
                Properties.SkyTelescope.Default.AxisTrackingLimit = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _axisHzTrackingLimit;
        public static double AxisHzTrackingLimit
        {
            get => _axisHzTrackingLimit;
            set
            {
                if (Math.Abs(_axisHzTrackingLimit - value) < 0.0000000000001) return;
                _axisHzTrackingLimit = value;
                Properties.SkyTelescope.Default.AxisHzTrackingLimit = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _cameraHeight;
        public static double CameraHeight
        {
            get => _cameraHeight;
            set
            {
                if (Math.Abs(_cameraHeight - value) < 0.0) return;
                _cameraHeight = value;
                Properties.SkyTelescope.Default.CameraHeight = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _cameraWidth;
        public static double CameraWidth
        {
            get => _cameraWidth;
            set
            {
                if (Math.Abs(_cameraWidth - value) < 0.0) return;
                _cameraWidth = value;
                Properties.SkyTelescope.Default.CameraWidth = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _port;
        public static string Port
        {
            get => _port;
            set
            {
                if (_port == value){return;}
                _port = value;
                Properties.SkyTelescope.Default.Port = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value);
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _decBacklash;
        public static int DecBacklash
        {
            get => _decBacklash;
            set
            {
                if (_decBacklash == value) return;
                _decBacklash = value;
                Properties.SkyTelescope.Default.DecBacklash = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.DecPulseToGoTo);
            }
        }

        private static bool _disableKeysOnGoTo;
        public static bool DisableKeysOnGoTo
        {
            get => _disableKeysOnGoTo;
            set
            {
                if (_disableKeysOnGoTo == value) return;
                _disableKeysOnGoTo = value;
                Properties.SkyTelescope.Default.DisableKeysOnGoTo = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _elevation = double.NaN;
        public static double Elevation
        {
            get => _elevation;
            set
            {
                if (Math.Abs(_elevation - value) <= 0) return;
                _elevation = value;
                Properties.SkyTelescope.Default.Elevation = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                SkyServer.SkyTasks(MountTaskName.Encoders);
            }
        }

        private static double _eyepieceFs;
        public static double EyepieceFs
        {
            get => _eyepieceFs;
            set
            {
                if (Math.Abs(_eyepieceFs - value) < 0.0) return;
                _eyepieceFs = value;
                Properties.SkyTelescope.Default.EyepieceFS = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                SkyServer.SkyTasks(MountTaskName.FullCurrent);
            }
        }

        private static double _focalLength;
        public static double FocalLength
        {
            get => _focalLength;
            set
            {
                if (Math.Abs(_focalLength - value) <= 0) return;
                _focalLength = value;
                Properties.SkyTelescope.Default.FocalLength = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _globalStopOn;
        public static bool GlobalStopOn
        {
            get => _globalStopOn;
            set
            {
                if (_globalStopOn == value) return;
                _globalStopOn = value;
                Properties.SkyTelescope.Default.GlobalStopOn = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                if (Math.Abs(_guideRateOffsetY - value) < 0.0000000000001) return;
                _guideRateOffsetY = value;
                Properties.SkyTelescope.Default.GuideRateOffsetY = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SetGuideRates();
            }
        }

        private static bool _hcAntiDec;
        public static bool HcAntiDec
        {
            get => _hcAntiDec;
            set
            {
                if (_hcAntiDec == value) return;
                _hcAntiDec = value;
                Properties.SkyTelescope.Default.HcAntiDec = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _hcAntiRa;
        public static bool HcAntiRa
        {
            get => _hcAntiRa;
            set
            {
                if (_hcAntiRa == value) return;
                _hcAntiRa = value;
                Properties.SkyTelescope.Default.HcAntiRa = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _hcFlipEw;
        public static bool HcFlipEw
        {
            get => _hcFlipEw;
            set
            {
                if (_hcFlipEw == value) return;
                _hcFlipEw = value;
                Properties.SkyTelescope.Default.HcFlipEW = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _hcFlipNs;
        public static bool HcFlipNs
        {
            get => _hcFlipNs;
            set
            {
                if (_hcFlipNs == value) return;
                _hcFlipNs = value;
                Properties.SkyTelescope.Default.HcFlipNS = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static List<HcPulseGuide> _hcPulseGuides;
        public static List<HcPulseGuide> HcPulseGuides
        {
            get => _hcPulseGuides;
            set
            {
                // if (_hcPulseGuides == value) return;
                _hcPulseGuides = value.OrderBy(hcPulseGuide => hcPulseGuide.Speed).ToList();
                var output = JsonConvert.SerializeObject(_hcPulseGuides);
                Properties.SkyTelescope.Default.HcPulseGuides = output;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{output}");
                OnStaticPropertyChanged();
            }
        }

        private static double _homeAxisX = double.NaN;
        /// <summary>
        /// Home position in user.config are app axes values
        /// Home position for polar is stored as Alt / Az in user.config, adjusted for southern hemisphere
        /// Getter returns the X axis as mount axis value
        /// </summary>
        public static double HomeAxisX
        {
            get
            {
                if (AlignmentMode != AlignmentModes.algPolar)
                {
                    return Axes.AxesAppToMount(new [] {_homeAxisX, _homeAxisY })[0];
                }
                else
                {
                    var angleOffset = Latitude < 0 ? 180.0 : 0.0;
                    var home = new[]
                    {
                        Properties.SkyTelescope.Default.HomeAxisX - angleOffset,
                        Properties.SkyTelescope.Default.HomeAxisY
                    };
                    home = Axes.AzAltToAxesXy(home);
                    return home[0]; // This is the X axis in the mount axes, which is the Ra axis
                }
            }
            private set
            {
                if (Math.Abs(_homeAxisX - value) <= 0.0000000000001) return;
                _homeAxisX = value;
                Properties.SkyTelescope.Default.HomeAxisX = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _homeAxisY = double.NaN;
        /// <summary>
        /// Home position in user.config are app axes values
        /// Home position for polar is stored as Alt / Az in user.config, adjusted for southern hemisphere
        /// Getter returns the Y axis as mount axis value
        /// </summary>
        public static double HomeAxisY
        {
            get
            {
                if (AlignmentMode != AlignmentModes.algPolar)
                {
                    return Axes.AxesAppToMount(new[] { _homeAxisX, _homeAxisY })[1];
                }
                else
                {
                    var angleOffset = Latitude < 0 ? 180.0 : 0.0;
                    var home = new[]
                    {
                        Properties.SkyTelescope.Default.HomeAxisX - angleOffset,
                        Properties.SkyTelescope.Default.HomeAxisY
                    };
                    home = Axes.AzAltToAxesXy(home);
                    return home[1]; // This is the Y axis in the mount axes, which is the Dec axis
                }
            }

            private set
            {
                if (Math.Abs(_homeAxisY - value) <= 0.0000000000001) return;
                _homeAxisY = value;
                Properties.SkyTelescope.Default.HomeAxisY = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _hourAngleLimit = double.NaN;
        public static double HourAngleLimit
        {
            get => _hourAngleLimit;
            set
            {
                if (Math.Abs(_hourAngleLimit - value) < 0.0000000000001) return;
                _hourAngleLimit = value;
                Properties.SkyTelescope.Default.HourAngleLimit = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _homeDialog;
        public static bool HomeDialog
        {
            get => _homeDialog;
            set
            {
                if (_homeDialog == value) return;
                _homeDialog = value;
                Properties.SkyTelescope.Default.HomeDialog = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }
        
        private static bool _hzLimitTracking;
        public static bool HzLimitTracking
        {
            get => _hzLimitTracking;
            set
            {
                if (_hzLimitTracking == value) return;
                _hzLimitTracking = value;
                Properties.SkyTelescope.Default.HzLimitTracking = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _hzLimitPark;
        public static bool HzLimitPark
        {
            get => _hzLimitPark;
            set
            {
                if (_hzLimitPark == value) return;
                _hzLimitPark = value;
                Properties.SkyTelescope.Default.HzLimitPark = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _kingRate;
        public static double KingRate
        {
            get => _kingRate;
            set
            {
                if (Math.Abs(_kingRate - value) < 0.0000000000001) return;
                _kingRate = value;
                Properties.SkyTelescope.Default.KingRate = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _latitude;
        public static double Latitude
        {
            get => _latitude;
            set
            {
                if (Math.Abs(_latitude - value) < 0.0000000000001) return;
                _latitude = value;
                Properties.SkyTelescope.Default.Latitude = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _limitPark;
        public static bool LimitPark
        {
            get => _limitPark;
            set
            {
                if (_limitPark == value) return;
                _limitPark = value;
                Properties.SkyTelescope.Default.LimitPark = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _longitude;
        public static double Longitude
        {
            get => _longitude;
            set
            {
                if (Math.Abs(_longitude - value) < 0.0000000000001) return;
                _longitude = value;
                Properties.SkyTelescope.Default.Longitude = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _lunarRate;
        public static double LunarRate
        {
            get => _lunarRate;
            set
            {
                if (Math.Abs(_lunarRate - value) < 0.0000000000001) return;
                _lunarRate = value;
                Properties.SkyTelescope.Default.LunarRate = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _maxSlewRate;
        public static double MaxSlewRate
        {
            get => _maxSlewRate;
            set
            {
                if (Math.Abs(_maxSlewRate - value) < 0.0000000000001) return;
                _maxSlewRate = value;
                Properties.SkyTelescope.Default.MaximumSlewRate = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
                SkyServer.SkyTasks(MountTaskName.MinPulseRa);
            }
        }

        private static double[] _parkAxes = {double.NaN, double.NaN};
        /// <summary>
        /// Park axes position in mount axes values
        /// </summary>
        public static double[] ParkAxes
        {
            get => _parkAxes;
            set
            {
                if (Math.Abs(_parkAxes[0] - value[0]) <= 0.0000000000001 && Math.Abs(_parkAxes[1] - value[1]) <= 0.0000000000001)
                    return;
                _parkAxes = value;
                value[0] = Math.Round(value[0], 6);
                value[1] = Math.Round(value[1], 6);
                Properties.SkyTelescope.Default.ParkAxes = JsonConvert.SerializeObject(value);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _parkDialog;
        public static bool ParkDialog
        {
            get => _parkDialog;
            set
            {
                if (_parkDialog == value) return;
                _parkDialog = value;
                Properties.SkyTelescope.Default.ParkDialog = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _pecOn;
        public static bool PecOn
        {
            get => _pecOn;
            set
            {
                if (_pecOn == value) return;
                _pecOn = value;
                Properties.SkyTelescope.Default.PecOn = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _pecOffSet;
        public static int PecOffSet
        {
            get => _pecOffSet;
            set
            {
                if (_pecOffSet == value) return;
                _pecOffSet = value;
                Properties.SkyTelescope.Default.PecOffSet = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _pecWormFile;
        public static string PecWormFile
        {
            get => _pecWormFile;
            set
            {
                if (_pecWormFile == value) return;
                _pecWormFile = value;
                Properties.SkyTelescope.Default.PecWormFile = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _pec360File;
        public static string Pec360File
        {
            get => _pec360File;
            set
            {
                if (_pec360File == value) return;
                _pec360File = value;
                Properties.SkyTelescope.Default.Pec360File = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _pPecOn;
        public static bool PPecOn
        {
            get => _pPecOn;
            set
            {
                if (_pPecOn == value) return;
                _pPecOn = value;
                Properties.SkyTelescope.Default.PpecOn = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _polarLedLevel;
        public static int PolarLedLevel
        {
            get => _polarLedLevel;
            set
            {
                if (_polarLedLevel == value) return;
                _polarLedLevel = value;
                Properties.SkyTelescope.Default.PolarLedLevel = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static PolarMode _polarMode;
        public static PolarMode PolarMode
        {
            get => _polarMode;
            set
            {
                if (_polarMode == value) return;
                _polarMode = value;
                Properties.SkyTelescope.Default.PolarMode = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        //private static bool _polarModeEast;
        //public static bool PolarModeEast
        //{
        //    get => _polarModeEast;
        //    set
        //    {
        //        if (_polarModeEast == value) return;
        //        _polarModeEast = value;
        //        Properties.SkyTelescope.Default.PolarModeEast = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
        //        OnStaticPropertyChanged();
        //    }
        //}

        private static int _raBacklash;
        public static int RaBacklash
        {
            get => _raBacklash;
            set
            {
                if (_raBacklash == value) return;
                _raBacklash = value;
                Properties.SkyTelescope.Default.RaBacklash = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _raGaugeFlip;
        public static bool RaGaugeFlip
        {
            get => _raGaugeFlip;
            set
            {
                if (_raGaugeFlip == value) return;
                _raGaugeFlip = value;
                Properties.SkyTelescope.Default.RaGaugeFlip = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _axisLimitX;
        public static double AxisLimitX
        {
            get => _axisLimitX;
            set
            {
                if (Math.Abs(_axisLimitX - value) < 0.0000000000001) return;
                _axisLimitX = value;
                Properties.SkyTelescope.Default.AxisLimitX = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        //private static int _spiralFov;
        //public static int SpiralFov
        //{
        //    get => _spiralFov;
        //    set
        //    {
        //        if (_spiralFov == value) return;
        //        _spiralFov = value;
        //        Properties.SkyTelescope.Default.SpiralFov = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
        //        OnStaticPropertyChanged();
        //    }
        //}

        //private static int _spiralPause;
        //public static int SpiralPause
        //{
        //    get => _spiralPause;
        //    set
        //    {
        //        if (_spiralPause  ==  value) return;
        //        _spiralPause = value;
        //        Properties.SkyTelescope.Default.SpiralPause = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
        //        OnStaticPropertyChanged();
        //    }
        //}

        //private static int _spiralSpeed;
        //public static int SpiralSpeed
        //{
        //    get => _spiralSpeed;
        //    set
        //    {
        //        if (_spiralSpeed == value) return;
        //        _spiralSpeed = value;
        //        Properties.SkyTelescope.Default.SpiralSpeed = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
        //        OnStaticPropertyChanged();
        //    }
        //}

        private static bool _spiralLimits;
        public static bool SpiralLimits
        {
            get => _spiralLimits;
            set
            {
                if (_spiralLimits == value) return;
                _spiralLimits = value;
                Properties.SkyTelescope.Default.SpiralLimits = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _spiralHeight;
        public static int SpiralHeight
        {
            get => _spiralHeight;
            set
            {
                if (_spiralHeight == value) return;
                _spiralHeight = value;
                Properties.SkyTelescope.Default.SpiralHeight = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _spiralDistance;
        public static double SpiralDistance
        {
            get => _spiralDistance;
            set
            {
                if (Math.Abs(_spiralDistance - value) < 0.0) return;
                _spiralDistance = value;
                Properties.SkyTelescope.Default.SpiralDistance = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _spiralWidth;
        public static int SpiralWidth
        {
            get => _spiralWidth;
            set
            {
                if (_spiralWidth == value) return;
                _spiralWidth = value;
                Properties.SkyTelescope.Default.SpiralWidth = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _st4GuideRate;
        public static int St4GuideRate
        {
            get => _st4GuideRate;
            set
            {
                if (_st4GuideRate == value) return;
                _st4GuideRate = value;
                Properties.SkyTelescope.Default.St4Guiderate = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                if (Math.Abs(_solarRate - value) < 0.0000000000001) return;
                _solarRate = value;
                Properties.SkyTelescope.Default.SolarRate = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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

        private static bool _syncLimitOn;
        public static bool SyncLimitOn
        {
            get => _syncLimitOn;
            set
            {
                if (_syncLimitOn == value) return;
                _syncLimitOn = value;
                Properties.SkyTelescope.Default.SyncLimitOn = value;
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        //private static TimeSpan _uTCDateOffset;
        //public static TimeSpan UTCDateOffset
        // {}
        //    get => _uTCDateOffset;
        //    set
        //    {}
        //        if (_uTCDateOffset == value) return;
        //        _uTCDateOffset = value;
        //        Properties.SkyTelescope.Default.UTCOffset = value;
        //        LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
        //        OnStaticPropertyChanged();
        //    }
        // }

        private static List<ParkPosition> _parkPositions;
        /// <summary>
        /// Park positions in mount axes values
        /// Polar values are stored as Az / Alt in user.config
        /// </summary>
        public static List<ParkPosition> ParkPositions
        {
            get => _parkPositions;
            set
            {
                {
                    _parkPositions = value.OrderBy(parkPosition => parkPosition.Name).ToList();
                    var output = JsonConvert.SerializeObject(_parkPositions);
                    Properties.SkyTelescope.Default.ParkPositions = output;
                    LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{output}");
                    OnStaticPropertyChanged();
                }
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _parkLimitName;
        public static string ParkLimitName
        {
            get => _parkLimitName;
            set
            {
                if (_parkLimitName == value) return;
                _parkLimitName = value;
                Properties.SkyTelescope.Default.ParkLimitName = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _parkHzLimitName;
        public static string ParkHzLimitName
        {
            get => _parkHzLimitName;
            set
            {
                if (_parkHzLimitName == value) return;
                _parkHzLimitName = value;
                Properties.SkyTelescope.Default.ParkHzLimitName = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _altAzTrackingUpdateInterval;
        public static int AltAzTrackingUpdateInterval
        {
            get => _altAzTrackingUpdateInterval;
            set
            {
                if (_altAzTrackingUpdateInterval == value) return;
                _altAzTrackingUpdateInterval = value;
                Properties.SkyTelescope.Default.AltAzTrackingUpdateInterval = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _axisLowerLimitY;
        public static double AxisLowerLimitY
        {
            get => _axisLowerLimitY;
            set
            {
                if (Math.Abs(_axisLowerLimitY - value) < 0.000001) return;
                _axisLowerLimitY = value;
                Properties.SkyTelescope.Default.AxisLowerLimitY = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _axisUpperLimitY;
        public static double AxisUpperLimitY
        {
            get => _axisUpperLimitY + (AlignmentMode == AlignmentModes.algPolar ? Math.Abs(Latitude) : 0);
            set
            {
                if (Math.Abs(_axisUpperLimitY - value) < 0.000001) return;
                _axisUpperLimitY = value;
                Properties.SkyTelescope.Default.AxisUpperLimitY = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the application settings and initializes various configuration properties.
        /// </summary>
        /// <remarks>This method retrieves settings from the user configuration file and applies them to
        /// initialize  the application's state. It sets various capabilities, server configurations, and mount
        /// properties  based on the loaded settings. This method also handles deserialization of complex settings and 
        /// ensures that all required properties are properly initialized.</remarks>
        public static void Load()
        {
            // Load settings from user.config handles create new, update or migrate settings as needed 
            LoadConfigSettings();

            //capabilities
            CanAlignMode = Properties.SkyTelescope.Default.CanAlignMode;
            CanAltAz = Properties.SkyTelescope.Default.CanAltAz;
            //CanDoesRefraction = Properties.SkyTelescope.Default.CanDoesRefraction;
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
            CanUnPark = Properties.SkyTelescope.Default.CanUnpark;
            NoSyncPastMeridian = Properties.SkyTelescope.Default.NoSyncPastMeridian;
            NumMoveAxis = Properties.SkyTelescope.Default.NumMoveAxis;
            VersionOne = Properties.SkyTelescope.Default.VersionOne;

            //Server

            Enum.TryParse<EquatorialCoordinateType>(Properties.SkyTelescope.Default.EquatorialCoordinateType, true, out var eParse);
            EquatorialCoordinateType = eParse;
            Enum.TryParse<AlignmentModes>(Properties.SkyTelescope.Default.AlignmentMode, true, out var aParse);
            AlignmentMode = aParse;
            Enum.TryParse<FrontGraphic>(Properties.SkyTelescope.Default.FrontGraphic, true, out var fParse);
            FrontGraphic = fParse;
            Enum.TryParse<DriveRates>(Properties.SkyTelescope.Default.TrackingRate, true, out var dParse);
            TrackingRate = dParse;
            Enum.TryParse<SlewSpeed>(Properties.SkyTelescope.Default.HcSpeed, true, out var hParse);
            HcSpeed = hParse;
            var hcModeBol = Enum.TryParse<HcMode>(Properties.SkyTelescope.Default.HCMode, true, out var hcParse);
            if (!hcModeBol) hcParse = HcMode.Guiding;// getting rid of compass mode
            HcMode = hcParse;
            Enum.TryParse<MountType>(Properties.SkyTelescope.Default.Mount, true, out var mountParse);
            Mount = mountParse;
            Enum.TryParse<Handshake>(Properties.SkyTelescope.Default.HandShake, true, out var hsParse);
            HandShake = hsParse;
            Enum.TryParse<SerialSpeed>(Properties.SkyTelescope.Default.BaudRate, true, out var bRateParse);
            BaudRate = bRateParse;
            Enum.TryParse<SerialSpeed>(Properties.SkyTelescope.Default.GpsBaudRate, true, out var grateParse);
            GpsBaudRate = grateParse;
            Enum.TryParse<PecMode>(Properties.SkyTelescope.Default.PecMode, true, out var pecParse);
            PecMode = pecParse;

            AllowAdvancedCommandSet = Properties.SkyTelescope.Default.AllowAdvancedCommandSet;
            AlternatingPPec = Properties.SkyTelescope.Default.AlternatingPPEC;
            ApertureArea = Properties.SkyTelescope.Default.ApertureArea;
            ApertureDiameter = Properties.SkyTelescope.Default.ApertureDiameter;
            AtPark = Properties.SkyTelescope.Default.AtPark;
            AutoTrack = Properties.SkyTelescope.Default.AutoTrack;
            AxisLowerLimitY = Properties.SkyTelescope.Default.AxisLowerLimitY;
            AxisModelOffsets = JsonConvert.DeserializeObject<Vector3>(Properties.SkyTelescope.Default.AxisModelOffsets);
            AxisUpperLimitY = Properties.SkyTelescope.Default.AxisUpperLimitY;
            AxisLimitX = Properties.SkyTelescope.Default.AxisLimitX;
            AxisTrackingLimit = Properties.SkyTelescope.Default.AxisTrackingLimit;
            AxisHzTrackingLimit = Properties.SkyTelescope.Default.AxisHzTrackingLimit;
            CameraHeight = Properties.SkyTelescope.Default.CameraHeight;
            CameraWidth = Properties.SkyTelescope.Default.CameraWidth;
            Port = Properties.SkyTelescope.Default.Port;
            CustomDec360Steps = Properties.SkyTelescope.Default.CustomDec360Steps;
            CustomDecTrackingOffset = Properties.SkyTelescope.Default.CustomDecTrackingOffset;
            CustomDecWormTeeth = Properties.SkyTelescope.Default.CustomDecWormTeeth;
            CustomRa360Steps = Properties.SkyTelescope.Default.CustomRa360Steps;
            CustomRaTrackingOffset = Properties.SkyTelescope.Default.CustomRaTrackingOffset;
            CustomRaWormTeeth = Properties.SkyTelescope.Default.CustomRaWormTeeth;
            CustomGearing = Properties.SkyTelescope.Default.CustomGearing;
            DataBits = Properties.SkyTelescope.Default.DataBits;
            DecBacklash = Properties.SkyTelescope.Default.DecBacklash;
            DecPulseToGoTo = Properties.SkyTelescope.Default.DecPulseToGoTo;
            DisplayInterval = Properties.SkyTelescope.Default.DisplayInterval;
            DisableKeysOnGoTo = Properties.SkyTelescope.Default.DisableKeysOnGoTo;
            DtrEnable = Properties.SkyTelescope.Default.DTREnable;
            Elevation = Properties.SkyTelescope.Default.Elevation;
            Encoders = Properties.SkyTelescope.Default.EncodersOn;
            EyepieceFs = Properties.SkyTelescope.Default.EyepieceFS;
            FocalLength = Properties.SkyTelescope.Default.FocalLength;
            FullCurrent = Properties.SkyTelescope.Default.FullCurrent;
            HcAntiDec = Properties.SkyTelescope.Default.HcAntiDec;
            HcAntiRa = Properties.SkyTelescope.Default.HcAntiRa;
            HcFlipEw = Properties.SkyTelescope.Default.HcFlipEW;
            HcFlipNs = Properties.SkyTelescope.Default.HcFlipNS;
            GlobalStopOn = Properties.SkyTelescope.Default.GlobalStopOn;
            GotoPrecision = Properties.SkyTelescope.Default.GotoPrecision;
            GpsComPort = "COM" + Properties.SkyTelescope.Default.GpsPort;
            GuideRateOffsetY = Properties.SkyTelescope.Default.GuideRateOffsetY;
            GuideRateOffsetX = Properties.SkyTelescope.Default.GuideRateOffsetX;
            HcPulseGuides = JsonConvert.DeserializeObject<List<HcPulseGuide>>(Properties.SkyTelescope.Default.HcPulseGuides);
            HourAngleLimit = Properties.SkyTelescope.Default.HourAngleLimit;
            HomeWarning = Properties.SkyTelescope.Default.HomeWarning;
            HomeDialog = Properties.SkyTelescope.Default.HomeDialog;
            HzLimitTracking = Properties.SkyTelescope.Default.HzLimitTracking;
            HzLimitPark = Properties.SkyTelescope.Default.HzLimitPark;
            InstrumentDescription = Properties.SkyTelescope.Default.InstrumentDescription;
            InstrumentName = Properties.SkyTelescope.Default.InstrumentName;
            KingRate = Properties.SkyTelescope.Default.KingRate;
            Latitude = Properties.SkyTelescope.Default.Latitude;
            LimitTracking = Properties.SkyTelescope.Default.LimitTracking;
            LimitPark = Properties.SkyTelescope.Default.LimitPark;
            Longitude = Properties.SkyTelescope.Default.Longitude;
            LunarRate = Properties.SkyTelescope.Default.LunarRate;
            MaxSlewRate = Properties.SkyTelescope.Default.MaximumSlewRate;
            MinPulseDec = Properties.SkyTelescope.Default.MinPulseDec;
            MinPulseRa = Properties.SkyTelescope.Default.MinPulseRa;
            ParkAxes = JsonConvert.DeserializeObject<double[]>(Properties.SkyTelescope.Default.ParkAxes);
            ParkDialog = Properties.SkyTelescope.Default.ParkDialog;
            ParkName = Properties.SkyTelescope.Default.ParkName;
            ParkLimitName = Properties.SkyTelescope.Default.ParkLimitName;
            ParkHzLimitName = Properties.SkyTelescope.Default.ParkHzLimitName;
            PecOn = Properties.SkyTelescope.Default.PecOn;
            PecOffSet = Properties.SkyTelescope.Default.PecOffSet;
            PPecOn = Properties.SkyTelescope.Default.PpecOn;
            PecWormFile = Properties.SkyTelescope.Default.PecWormFile;
            Pec360File = Properties.SkyTelescope.Default.Pec360File;
            PolarLedLevel = Properties.SkyTelescope.Default.PolarLedLevel;
            Enum.TryParse<PolarMode>(Properties.SkyTelescope.Default.PolarMode, true, out var pParse);
            PolarMode = pParse;
            RaBacklash = Properties.SkyTelescope.Default.RaBacklash;
            RaGaugeFlip = Properties.SkyTelescope.Default.RaGaugeFlip;
            ReadTimeout = Properties.SkyTelescope.Default.ReadTimeout;
            Refraction = Properties.SkyTelescope.Default.Refraction;
            RaTrackingOffset = Properties.SkyTelescope.Default.RATrackingOffset;
            RtsEnable = Properties.SkyTelescope.Default.RTSEnable;
            SiderealRate = Properties.SkyTelescope.Default.SiderealRate;
            SpiralDistance = Properties.SkyTelescope.Default.SpiralDistance;
            SpiralLimits = Properties.SkyTelescope.Default.SpiralLimits;
            SpiralHeight = Properties.SkyTelescope.Default.SpiralHeight;
            SpiralWidth = Properties.SkyTelescope.Default.SpiralWidth;
            SolarRate = Properties.SkyTelescope.Default.SolarRate;
            St4GuideRate = Properties.SkyTelescope.Default.St4Guiderate;
            SyncLimit = Properties.SkyTelescope.Default.SyncLimit;
            SyncLimitOn = Properties.SkyTelescope.Default.SyncLimitOn;
            Temperature = Properties.SkyTelescope.Default.Temperature;
            AltAzTrackingUpdateInterval = Properties.SkyTelescope.Default.AltAzTrackingUpdateInterval;
            Enum.TryParse<Model3DType>(Properties.SkyTelescope.Default.ModelType, true, out var mtParse);
            Settings.Settings.ModelType = mtParse;

            // Set home axis amd park position information once all mount properties are loaded
            HomeAxisX = Properties.SkyTelescope.Default.HomeAxisX;
            HomeAxisY = Properties.SkyTelescope.Default.HomeAxisY;
            ParkPositions = JsonConvert.DeserializeObject<List<ParkPosition>>(Properties.SkyTelescope.Default.ParkPositions);
            //UTCDateOffset = Properties.SkyTelescope.Default.UTCOffset;
        }

        /// <summary>
        /// Loads and initializes configuration settings for the application, handling scenarios such as version
        /// upgrades, profile-based settings, and default configuration initialization.
        /// </summary>
        /// <remarks>This method manages the application's configuration settings by checking for the
        /// existence of current and previous configuration files, handling version mismatches, and ensuring that
        /// settings are properly loaded or initialized. It supports the following scenarios: <list type="bullet">
        /// <item> <description>If a current configuration file exists, it reloads the settings.</description> </item>
        /// <item> <description>If a previous version's profile-based settings exist but no current configuration file,
        /// it upgrades and migrates the settings to the current version.</description> </item> <item> <description>If
        /// no previous or current configuration files exist, it initializes new settings using default values from the
        /// application's configuration.</description> </item> </list> This method ensures that the application's
        /// settings are consistent and up-to-date across different versions and profiles.</remarks>
        private static void LoadConfigSettings()
        {
            var zeroVersion = new Version("0.0.0.0");
            // Get version information for assembly and current config
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Properties.SkyTelescope.Default.SettingsKey = Properties.Profile.Default.Current;
            var configExists = Properties.SkyTelescope.Default.Version != "0";
            // Get version information for earlier user config if it exists
            var prevVersionObj = Properties.Server.Default.GetPreviousVersion("Version");
            var earlierVersion = prevVersionObj != null ? new Version(prevVersionObj.ToString()) : zeroVersion;
            var earlierConfigExists = earlierVersion != zeroVersion;
            var isConfigProfile = earlierConfigExists && earlierVersion >= new Version("1.2.0.7");
            // Alpha profile handling will be removed for beta and later releases
            var isAlphaProfile = earlierConfigExists && 
                                 new Version("1.2.0.0") <= earlierVersion && earlierVersion <= new Version("1.2.0.6");
            // Drop through to Scenario 1: user.config exists, profile settings available, settings loaded at end of function
            if (!configExists)
            {
                // Scenario 2, 3 or 4
                if (isConfigProfile || isAlphaProfile)
                {
                    // Scenario 2:. user.config does not exist, previous version profile based settings available
                    // Copy settings from previous version profile settings
                    // Set version and force write of profile settings to user.config
                    Properties.Profile.Default.Upgrade();
                    Properties.Profile.Default.Version = currentVersion;
                    Properties.Profile.Default.PropertyValues["Current"].IsDirty = true;
                    Properties.Profile.Default.Save();
                    foreach (var mountType in Properties.Profile.Default.List.Split(','))
                    {
                        // Copy settings from previous version into dictionary - deleted settings are not copied 
                        Properties.SkyTelescope.Default.SettingsKey = mountType;
                        Dictionary<string, object> previousSettings = new Dictionary<string, object>(256);
                        foreach (SettingsPropertyValue propertyValue in Properties.SkyTelescope.Default.PropertyValues)
                        {
                            if (Properties.SkyTelescope.Default.GetPreviousVersion(propertyValue.Name)?.GetType() !=
                                null)
                            {
                                var name = propertyValue.Name;
                                Properties.SkyTelescope.Default[name] =
                                    Properties.SkyTelescope.Default.GetPreviousVersion(name);
                                Properties.SkyTelescope.Default.PropertyValues[name].IsDirty = true;
                            }
                        }
                        Properties.SkyTelescope.Default.Version = currentVersion;
                        Properties.SkyTelescope.Default.PropertyValues["Version"].IsDirty = true;
                        if (isAlphaProfile && mountType == "algPolar")
                        {
                            // Fix up alpha profile settings: ParkPositions, AtPark, ParkName
                            ResetParkPositions();
                        }
                        Properties.SkyTelescope.Default.Save();
                    }
                    Properties.SkyTelescope.Default.SettingsKey = Properties.Profile.Default.Current;
                }
                else // Scenarios 3 or 4: user.config does not exist, no previous version profile based settings available
                {
                    // Setup new profile settings
                    Properties.Profile.Default.Version = currentVersion;
                    Properties.Profile.Default.PropertyValues["Current"].IsDirty = true;
                    Properties.Profile.Default.Save();

                    // Create new settings file from app.config defaults for cases 3 and 4
                    Properties.SkyTelescope.Default.Reset();
                    Properties.SkyTelescope.Default.Version = currentVersion;
                    foreach (var mountType in Properties.Profile.Default.List.Split(','))
                    {
                        InitSettingsFile(mountType, currentVersion);
                    }

                    Properties.SkyTelescope.Default.SettingsKey = Properties.Profile.Default.Current;
                    Properties.SkyTelescope.Default.Save();
                    Properties.SkyTelescope.Default.Reload();
                    if (earlierConfigExists) // Case 3: previous version config file exists so copy applicable settings
                    {
                        Dictionary<string, object> previousSettings = new Dictionary<string, object>(256);
                        Properties.SkyTelescope.Default.SettingsKey = "";
                        _ = Properties.SkyTelescope.Default["Version"]; // Force read of properties.SkyTelescope.Default
                        // Get the settings from the previous version
                        foreach (SettingsPropertyValue propertyValue in Properties.SkyTelescope.Default.PropertyValues)
                        {
                            if (Properties.SkyTelescope.Default.GetPreviousVersion(propertyValue.Name)?.GetType() !=
                                null)
                            {
                                previousSettings[propertyValue.Name] =
                                    Properties.SkyTelescope.Default.GetPreviousVersion(propertyValue.Name);
                            }
                        }
                        // Get model type from previous version Server properties
                        previousSettings["ModelType"] = Properties.Server.Default.GetPreviousVersion("ModelType");
                        // Get the mount type from the previous version
                        var alignMentMode = previousSettings["AlignmentMode"]?.ToString() ?? "algGermanPolar";
                        // Remove settings that are always mount type specific
                        foreach (var name in new [] { "AlignmentMode", "AxisModelOffsets", "HomeAxisX", "HomeAxisY", "AtPark", "ParkName", "ParkPositions", "PolarMode", "Version"}) previousSettings.Remove(name);
                        // Copy settings from previous version into all AltAz and German Polar mount types
                        CopyEarlierConfigSettings(previousSettings, "algAltAz");
                        CopyEarlierConfigSettings(previousSettings, "algGermanPolar");
                        
                        // Remove settings that are Polar mount type specific
                        foreach (var name in new[] { "AxisLimitX", "AxisLowerLimitY", "AxisUpperLimitY", "HourAngleLimit", "ModelType" }) 
                            previousSettings.Remove(name);
                        // Copy settings from previous version into all Polar mount types
                        CopyEarlierConfigSettings(previousSettings, "algPolar");

                        // Get the park and limit settings from previous version
                        previousSettings.Clear();
                        foreach (var propertyName in new[] {
                                     "ParkPositionsAltAz", "ParkPositionsEQ", "ParkPositions",
                                     "ParkAxisX", "ParkAxisY",
                                     "ParkAxisAz", "ParkAxisAlt",
                                     "AtPark", 
                                     // "AzSlewLimit", "AltAxisLowerLimit", "AltAxisUpperLimit",
                                     // "AltAzAxesLimitOn", "AltAxisLimitOn",
                                     "AlignmentMode", "Mount" }
                                ) AddPropertyValue(previousSettings, propertyName, Properties.SkyTelescope.Default);
                        // Migrate park and limit settings for German Polar and AltAz mounts. No action for Polar mounts
                        if (String.IsNullOrEmpty((string)previousSettings["ParkPositions"]))
                        {
                            RestoreParkSettings(previousSettings, "algGermanPolar", "ParkPositionsEQ", "ParkAxisX", "ParkAxisY");
                            RestoreParkSettings(previousSettings, "algAltAz", "ParkPositionsAltAz", "ParkAxisAz", "ParkAxisAlt");
                        }
                        else
                        {
                            RestoreParkSettings(previousSettings, "algGermanPolar", "ParkPositions", "ParkAxisX", "ParkAxisY");
                        }
                        Properties.SkyTelescope.Default.SettingsKey = Properties.Profile.Default.Current;
                        Properties.SkyTelescope.Default.AtPark = (bool)previousSettings["AtPark"];
                    }
                    // Scenario 4: user.config does not exist, no previous user.config
                    // Must initialise Polar ParkPositions based on latitude
                    // Set profile to Polar and get list of polar ParkPositions from app.config with default = (0, 0) and home = (0, 5)
                    Properties.SkyTelescope.Default.SettingsKey = "algPolar";
                    ResetParkPositions();
                    Save();
                    // 
                    // No migration needed, user.config has been created just set the current settings key
                    Properties.SkyTelescope.Default.SettingsKey = Properties.Profile.Default.Current;
                }
            }
            Properties.SkyTelescope.Default.Reload();
        }

        /// <summary>
        /// Initializes the settings file with configuration values specific to the provided settings key and version.
        /// </summary>
        /// <remarks>This method loads the configuration values for the specified settings key from the
        /// application configuration file and applies them to the user-specific settings file. It also marks all
        /// settings as dirty to ensure they are saved during the next save operation. If a property from the
        /// application configuration is no longer present in the current version, it is logged as an informational
        /// message.</remarks>
        /// <param name="settingsKey">The key identifying the configuration profile to load (e.g., "algAltAz", "algGermanPolar", or "algPolar").</param>
        /// <param name="version">The version string to associate with the settings file.</param>
        private static void InitSettingsFile(string settingsKey, string version)
        {
            // Load SettingsKey to access config section
            Properties.SkyTelescope.Default.SettingsKey = settingsKey;
            Properties.SkyTelescope.Default.Reload();
            // Set version to current assembly
            Properties.SkyTelescope.Default.Version = version;
            // Initialise user.config section with mount type specific values from app.config
            SettingsPropertyCollection profileProperties = null;
            switch (settingsKey)
            {
                case "algAltAz":
                    profileProperties = Properties.Profiles.AltAz.Default.Properties;
                    break;
                case "algGermanPolar":
                    profileProperties = Properties.Profiles.GermanPolar.Default.Properties;
                    break;
                case "algPolar":
                    profileProperties = Properties.Profiles.Polar.Default.Properties;
                    break;
            }
            // Copy the mount type specific app.config defaults to user.config
            foreach (SettingsProperty profileProperty in profileProperties)
            {
                try
                {
                    Properties.SkyTelescope.Default.PropertyValues[profileProperty.Name].PropertyValue =
                        Convert.ChangeType(profileProperty.DefaultValue, profileProperty.PropertyType);
                }
                catch (Exception e) //Log unknown properties removed from current version
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"{profileProperty.Name} removed from current settings version: {e.Message}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                }
            }
            // Iterate over all settings and mark as dirty to force write on save
            foreach (SettingsPropertyValue propertyValue in Properties.SkyTelescope.Default.PropertyValues)
            {
                propertyValue.IsDirty = true;
            }
            Properties.SkyTelescope.Default.Save();
        }

        static void  CopyEarlierConfigSettings(Dictionary<string, object> previousSettings, string mountType)
        {
            // Copy settings from previous version into dictionary - deleted settings are not copied 
            Properties.SkyTelescope.Default.SettingsKey = mountType;
            _ = Properties.SkyTelescope.Default["Version"]; // Force read of properties.SkyTelescope.Default
            foreach (var setting in previousSettings)
            {
                var name = setting.Key;
                Properties.SkyTelescope.Default[name] = setting.Value;
                Properties.SkyTelescope.Default.PropertyValues[name].IsDirty = true;
            }
            Properties.SkyTelescope.Default.Save();
        }

        /// <summary>
        /// Sets the park settings from previous version of user.config
        /// </summary>
        /// <param name="previousSettings"></param>
        /// <param name="settingsKey"></param>
        /// <param name="parkPositions"></param>
        /// <param name="axisX"></param>
        /// <param name="axisY"></param>
        private static void RestoreParkSettings(Dictionary<string, object> previousSettings, string settingsKey, string parkPositions, string axisX, string axisY)
        {
            Properties.SkyTelescope.Default.SettingsKey = settingsKey;
            Properties.SkyTelescope.Default.Reload();
            if (previousSettings.TryGetValue(parkPositions, out var value))
            {
                Properties.SkyTelescope.Default["ParkPositions"] = value;
            }

            if ((previousSettings.TryGetValue(axisX, out var valueX)) &&
                (previousSettings.TryGetValue(axisY, out var valueY)))
            {
                try
                {
                    var parkAxes = new[] { Double.Parse((string)valueX), Double.Parse((string)valueY) };
                    Properties.SkyTelescope.Default.ParkAxes = JsonConvert.SerializeObject(parkAxes);

                }
                catch (Exception e) when (e is ArgumentException || e is FormatException)
                {
                }
            }
            Save();
        }

        /// <summary>
        /// Resets the park positions to their default values based on the current latitude setting.
        /// </summary>
        /// <remarks>This method adjusts the Y-coordinates of the park positions to account for the
        /// absolute value of the latitude  and sets the park state to its default configuration. The updated park
        /// positions are serialized and saved  to the application settings.</remarks>
        public static void ResetParkPositions()
        {
            var parkPositions = JsonConvert.DeserializeObject<List<ParkPosition>>(Properties.Profiles.Polar.Default.ParkPositions);
            parkPositions[0].Y = parkPositions[0].Y + Math.Abs(Properties.SkyTelescope.Default.Latitude) - 90.0;
            parkPositions[0].Y = Math.Round(parkPositions[0].Y, 6);
            parkPositions[1].Y = parkPositions[1].Y + Math.Abs(Properties.SkyTelescope.Default.Latitude) - 90.0;
            parkPositions[1].Y = Math.Round(parkPositions[1].Y, 6);
            Properties.SkyTelescope.Default.ParkPositions = JsonConvert.SerializeObject(parkPositions);
            Properties.SkyTelescope.Default.AtPark = false;
            Properties.SkyTelescope.Default.ParkName = "Default";
            ParkPositions = parkPositions;
        }

        /// <summary>
        /// save and reload using current SettingsKey
        /// </summary>
        public static void Save()
        {
            Properties.SkyTelescope.Default.Save();
            Properties.SkyTelescope.Default.Reload();
        }

        /// <summary>
        /// Add property value from previous version of settings file
        /// GetPreviousVersion assumes setting does not exist in the current settings class
        /// so we have to wrap GetPreviousVersion with a setting create and delete
        /// </summary>
        /// <param name="settingName">The name of the setting to get previous version</param>
        /// <param name="properties">Properties instance containing setting as property</param>
        private static void GetPropertyValue(string settingName, ApplicationSettingsBase properties)
        {
            try
            {
                // Create setting property ready for get
                SettingsAttributeDictionary settingsAttributeDictionary = new SettingsAttributeDictionary();
                settingsAttributeDictionary.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
                SettingsProperty p = new SettingsProperty(settingName, typeof(string),
                    properties.Providers["LocalFileSettingsProvider"],
                    false, String.Empty,
                    SettingsSerializeAs.String, settingsAttributeDictionary,
                    false, false);
                properties.Properties.Add(p);
                var settingValue = properties.GetPreviousVersion(settingName);
                // Remove setting
                properties.Properties.Remove(settingName);
            }
            catch (ArgumentException)
            {
                // Setting already exists - just get the value
            }
        }

        /// <summary>
        /// Add property value from previous version of settings file
        /// GetPreviousVersion assumes setting does not exist in the current settings class
        /// so we have to wrap GetPreviousVersion with a setting create and delete
        /// </summary>
        /// <param name="previousSettings">Dictionary to hold settings keyed by setting name</param>
        /// <param name="settingName">The name of the setting to get previous version</param>
        /// <param name="properties">Properties instance containing setting as property</param>
        private static void AddPropertyValue(Dictionary<string, object> previousSettings, string settingName, ApplicationSettingsBase properties)
        {
            try
            {
                // No profile
                properties.SettingsKey = "";
                // Create setting property ready for get
                SettingsAttributeDictionary settingsAttributeDictionary = new SettingsAttributeDictionary();
                settingsAttributeDictionary.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
                SettingsProperty p = new SettingsProperty(settingName, typeof(string),
                    properties.Providers["LocalFileSettingsProvider"],
                    false, String.Empty,
                    SettingsSerializeAs.String, settingsAttributeDictionary,
                    false, false);
                properties.Properties.Add(p);
                var settingValue = properties.GetPreviousVersion(settingName);
                if (settingValue != null) previousSettings.Add(settingName, settingValue);
                // Remove setting
                properties.Properties.Remove(settingName);
            }
            catch (ArgumentException)
            {
                // Setting already exists - just get the value
                previousSettings.Add(settingName, properties.GetPreviousVersion(settingName));
            }
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
