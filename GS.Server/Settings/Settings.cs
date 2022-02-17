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

using GS.Shared;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;

namespace GS.Server.Settings
{
    public static class Settings
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Settings

        private static bool _charting;
        public static bool Charting
        {
            get => _charting;
            set
            {
                if (_charting == value) return;
                _charting = value;
                Properties.Server.Default.Charting = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _disableHardwareAcceleration;
        public static bool DisableHardwareAcceleration
        {
            get => _disableHardwareAcceleration;
            set
            {
                if (_disableHardwareAcceleration == value) return;
                _disableHardwareAcceleration = value;
                Properties.Server.Default.DisableHardwareAcceleration = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }
           
        private static bool _focuser;
        public static bool Focuser
        {
            get => _focuser;
            set
            {
                if (_focuser == value) return;
                _focuser = value;
                Properties.Server.Default.Focuser = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _gamePad;
        public static bool GamePad
        {
            get => _gamePad;
            set
            {
                if (_gamePad == value) return;
                _gamePad = value;
                Properties.Server.Default.Gamepad = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _notes;
        public static bool Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value;
                Properties.Server.Default.Notes = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _skyWatcher;
        public static bool SkyWatcher
        {
            get => _skyWatcher;
            set
            {
                if (_skyWatcher == value) return;
                _skyWatcher = value;
                Properties.Server.Default.SkyWatcher = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static Model3DType _modelType;
        public static Model3DType ModelType
        {
            get => _modelType;
            set
            {
                if (_modelType == value) return;
                _modelType = value;
                Properties.Server.Default.ModelType = value.ToString();
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _model3d;
        public static bool Model3D
        {
            get => _model3d;
            set
            {
                if (_model3d == value) return;
                _model3d = value;
                Properties.Server.Default.Model3D = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static Vector3D _modelLookDirection1;
        public static Vector3D ModelLookDirection1
        {
            get => _modelLookDirection1;
            set
            {
                _modelLookDirection1 = value;
                Properties.Server.Default.ModelLookDirection1 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static Point3D _modelPosition1;
        public static Point3D ModelPosition1
        {
            get => _modelPosition1;
            set
            {
                _modelPosition1 = value;
                Properties.Server.Default.ModelPosition1 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static Vector3D _modelUpDirection1;
        public static Vector3D ModelUpDirection1
        {
            get => _modelUpDirection1;
            set
            {
                _modelUpDirection1 = value;
                Properties.Server.Default.ModelUpDirection1 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static Vector3D _modelLookDirection2;
        public static Vector3D ModelLookDirection2
        {
            get => _modelLookDirection2;
            set
            {
                _modelLookDirection2 = value;
                Properties.Server.Default.ModelLookDirection2 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static Point3D _modelPosition2;
        public static Point3D ModelPosition2
        {
            get => _modelPosition2;
            set
            {
                _modelPosition2 = value;
                Properties.Server.Default.ModelPosition2 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static Vector3D _modelUpDirection2;
        public static Vector3D ModelUpDirection2
        {
            get => _modelUpDirection2;
            set
            {
                _modelUpDirection2 = value;
                Properties.Server.Default.ModelUpDirection2 = value.ToString(CultureInfo.InvariantCulture);
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
            }
        }

        private static bool _pec;
        public static bool Pec
        {
            get => _pec;
            set
            {
                if (_pec == value) return;
                _pec = value;
                Properties.Server.Default.Pec = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _plot;
        public static bool Plot
        {
            get => _plot;
            set
            {
                if (_plot == value) return;
                _plot = value;
                Properties.Server.Default.Plot = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _poleLocator;
        public static bool PoleLocator
        {
            get => _poleLocator;
            set
            {
                if (_poleLocator == value) return;
                _poleLocator = value;
                Properties.Server.Default.PoleLocator = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _pulses;
        public static bool Pulses
        {
            get => _pulses;
            set
            {
                if (_pulses == value) return;
                _pulses = value;
                Properties.Server.Default.Pulses = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _sleepMode;
        public static bool SleepMode
        {
            get => _sleepMode;
            set
            {
                if (_sleepMode == value) return;
                _sleepMode = value;
                Properties.Server.Default.SleepMode = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _snap;
        public static bool Snap
        {
            get => _snap;
            set
            {
                if (_snap == value) return;
                _snap = value;
                Properties.Server.Default.Snap = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _alignmentTabVisible;
        public static bool AlignmentTabVisible
        {
            get => _alignmentTabVisible;
            set
            {
                if (_alignmentTabVisible == value) return;
                _alignmentTabVisible = value;
                Properties.Server.Default.AlignmentTabVisible = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _startMinimized;
        public static bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized == value) return;
                _startMinimized = value;
                Properties.Server.Default.StartMinimized = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _startOnTop;
        public static bool StartOnTop
        {
            get => _startOnTop;
            set
            {
                if (_startOnTop == value) return;
                _startOnTop = value;
                Properties.Server.Default.StartOnTop = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _voiceActive;
        public static bool VoiceActive
        {
            get => _voiceActive;
            set
            {
                if (_voiceActive == value) return;
                _voiceActive = value;
                Properties.Server.Default.VoiceActive = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _voiceName;
        public static string VoiceName
        {
            get => _voiceName;
            set
            {
                if (_voiceName == value) return;
                _voiceName = value;
                Properties.Server.Default.VoiceName = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _voiceVolume;
        public static int VoiceVolume
        {
            get => _voiceVolume;
            set
            {
                if (_voiceVolume == value) return;
                _voiceVolume = value;
                Properties.Server.Default.VoiceVolume = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _darkSkyKey;
        public static string DarkSkyKey
        {
            get => _darkSkyKey;
            set
            {
                if (_darkSkyKey == value) return;
                _darkSkyKey = value;
                Properties.Server.Default.DarkSkyKey = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _darkTheme;
        public static bool DarkTheme
        {
            get => _darkTheme;
            set
            {
                if (_darkTheme == value) return;
                _darkTheme = value;
                Properties.Server.Default.DarkTheme = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _primaryColor;
        public static string PrimaryColor
        {
            get => _primaryColor;
            set
            {
                if (_primaryColor == value) return;
                _primaryColor = value;
                Properties.Server.Default.PrimaryColor = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _accentColor;
        public static string AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor == value) return;
                _accentColor = value;
                Properties.Server.Default.AccentColor = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static WindowState _windowState;
        public static WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                Properties.Server.Default.WindowState = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }
        }

        private static double _windowHeight;
        public static double WindowHeight
        {
            get => _windowHeight;
            set
            {
                if (Math.Abs(_windowHeight - value) < 0.1) return;
                _windowHeight = value;
                Properties.Server.Default.WindowHeight = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _windowWidth;
        public static double WindowWidth
        {
            get => _windowWidth;
            set
            {
                if (Math.Abs(_windowWidth - value) < 0.1) return;
                _windowWidth = value;
                Properties.Server.Default.WindowWidth = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _windowLeft;
        public static double WindowLeft
        {
            get => _windowLeft;
            set
            {
                if (Math.Abs(_windowLeft - value) < 0.1) return;
                _windowLeft = value;
                Properties.Server.Default.WindowLeft = value;
                //  LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static double _windowTop;
        public static double WindowTop
        {
            get => _windowTop;
            set
            {
                if (Math.Abs(_windowTop - value) < 0.1) return;
                _windowTop = value;
                Properties.Server.Default.WindowTop = value;
                // LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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

            Charting = Properties.Server.Default.Charting;
            Focuser = Properties.Server.Default.Focuser;
            DisableHardwareAcceleration = Properties.Server.Default.DisableHardwareAcceleration;
            GamePad = Properties.Server.Default.Gamepad;
            Notes = Properties.Server.Default.Notes;
            SkyWatcher = Properties.Server.Default.SkyWatcher;
            Model3D = Properties.Server.Default.Model3D;
            ModelLookDirection1 = Vector3D.Parse(Properties.Server.Default.ModelLookDirection1);
            ModelPosition1 = Point3D.Parse(Properties.Server.Default.ModelPosition1);
            ModelUpDirection1 = Vector3D.Parse(Properties.Server.Default.ModelUpDirection1);
            ModelLookDirection2 = Vector3D.Parse(Properties.Server.Default.ModelLookDirection2);
            ModelPosition2 = Point3D.Parse(Properties.Server.Default.ModelPosition2);
            ModelUpDirection2 = Vector3D.Parse(Properties.Server.Default.ModelPosition2);
            Pec = Properties.Server.Default.Pec;
            Plot = Properties.Server.Default.Plot;
            PoleLocator = Properties.Server.Default.PoleLocator;
            Pulses = Properties.Server.Default.Pulses;
            SleepMode = Properties.Server.Default.SleepMode;
            Snap = Properties.Server.Default.Snap;
            WindowState = Properties.Server.Default.WindowState;   //Load WindowState before StartMinimized or Height and Width
            StartMinimized = Properties.Server.Default.StartMinimized;
            StartOnTop = Properties.Server.Default.StartOnTop;
            VoiceActive = Properties.Server.Default.VoiceActive;
            VoiceName = Properties.Server.Default.VoiceName;
            VoiceVolume = Properties.Server.Default.VoiceVolume;
            DarkSkyKey = Properties.Server.Default.DarkSkyKey;
            DarkTheme = Properties.Server.Default.DarkTheme;
            PrimaryColor = Properties.Server.Default.PrimaryColor;
            AccentColor = Properties.Server.Default.AccentColor;
            WindowHeight = Properties.Server.Default.WindowHeight;
            WindowWidth = Properties.Server.Default.WindowWidth;
            WindowLeft = Properties.Server.Default.WindowLeft;
            WindowTop = Properties.Server.Default.WindowTop;
            AlignmentTabVisible = Properties.Server.Default.AlignmentTabVisible;

            Enum.TryParse<Model3DType>(Properties.Server.Default.ModelType, true, out var aparse);
            ModelType = aparse;

        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        public static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Server.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Server.Default.Upgrade();
            Properties.Server.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Server.Default.Save();
            Properties.Server.Default.Reload();
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
