using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using GS.Shared;

namespace GS.Server.Charting
{
    public static class ChartSettings
    {

        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        private static string _thirdColor;
        public static string ThirdColor
        {
            get => _thirdColor;
            set
            {
                if (_thirdColor == value) return;
                _thirdColor = value;
                Properties.Chart.Default.ThirdColor = value;
                OnStaticPropertyChanged();

            }
        }

        private static string _fourthColor;
        public static string FourthColor
        {
            get => _fourthColor;
            set
            {
                if (_fourthColor == value) return;
                _fourthColor = value;
                Properties.Chart.Default.FourthColor = value;
                OnStaticPropertyChanged();
            }
        }

        private static string _raColor;
        public static string RaColor
        {
            get => _raColor;
            set
            {
                if (_raColor == value) return;
                _raColor = value;
                Properties.Chart.Default.RaColor = value;
                OnStaticPropertyChanged();
            }
        }

        private static string _decColor;
        public static string DecColor
        {
            get => _decColor;
            set
            {
                if (_decColor == value) return;
                _decColor = value;
                Properties.Chart.Default.DecColor = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _invertRa;
        public static bool InvertRa
        {
            get => _invertRa;
            set
            {
                if (_invertRa == value) return;
                _invertRa = value;
                Properties.Chart.Default.InvertRa = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _invertDec;
        public static bool InvertDec
        {
            get => _invertDec;
            set
            {
                if (_invertDec == value) return;
                _invertDec = value;
                Properties.Chart.Default.InvertDec = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _invertThird;
        public static bool InvertThird
        {
            get => _invertThird;
            set
            {
                if (_invertThird == value) return;
                _invertThird = value;
                Properties.Chart.Default.InvertThird = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _invertFourth;
        public static bool InvertFourth
        {
            get => _invertFourth;
            set
            {
                if (_invertFourth == value) return;
                _invertFourth = value;
                Properties.Chart.Default.InvertFourth = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _raLine;
        public static bool RaLine
        {
            get => _raLine;
            set
            {
                if (_raLine == value) return;
                _raLine = value;
                Properties.Chart.Default.RaLine = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _raBar;
        public static bool RaBar
        {
            get => _raBar;
            set
            {
                if (_raBar == value) return;
                _raBar = value;
                Properties.Chart.Default.RaBar = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _raStep;
        public static bool RaStep
        {
            get => _raStep;
            set
            {
                if (_raStep == value) return;
                _raStep = value;
                Properties.Chart.Default.RaStep = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _decLine;
        public static bool DecLine
        {
            get => _decLine;
            set
            {
                if (_decLine == value) return;
                _decLine = value;
                Properties.Chart.Default.DecLine = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _decBar;
        public static bool DecBar
        {
            get => _decBar;
            set
            {
                if (_decBar == value) return;
                _decBar = value;
                Properties.Chart.Default.DecBar = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _decStep;
        public static bool DecStep
        {
            get => _decStep;
            set
            {
                if (_decStep == value) return;
                _decStep = value;
                Properties.Chart.Default.DecStep = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _thirdLine;
        public static bool ThirdLine
        {
            get => _thirdLine;
            set
            {
                if (_thirdLine == value) return;
                _thirdLine = value;
                Properties.Chart.Default.ThirdLine = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _thirdBar;
        public static bool ThirdBar
        {
            get => _thirdBar;
            set
            {
                if (_thirdBar == value) return;
                _thirdBar = value;
                Properties.Chart.Default.ThirdBar = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _thirdStep;
        public static bool ThirdStep
        {
            get => _thirdStep;
            set
            {
                if (_thirdStep == value) return;
                _thirdStep = value;
                Properties.Chart.Default.ThirdStep = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _fourthLine;
        public static bool FourthLine
        {
            get => _fourthLine;
            set
            {
                if (_fourthLine == value) return;
                _fourthLine = value;
                Properties.Chart.Default.FourthLine = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _fourthBar;
        public static bool FourthBar
        {
            get => _fourthBar;
            set
            {
                if (_fourthBar == value) return;
                _fourthBar = value;
                Properties.Chart.Default.FourthBar = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _fourthStep;
        public static bool FourthStep
        {
            get => _fourthStep;
            set
            {
                if (_fourthStep == value) return;
                _fourthStep = value;
                Properties.Chart.Default.FourthStep = value;
                OnStaticPropertyChanged();
            }
        }

        private static string _phdHostText;
        public static string PhdHostText
        {
            get => _phdHostText;
            set
            {
                if (_phdHostText == value) return;
                _phdHostText = value;
                Properties.Chart.Default.PhdHostText = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _showInArcseconds;
        public static bool ShowInArcseconds
        {
            get => _showInArcseconds;
            set
            {
                if (_showInArcseconds == value) return;
                _showInArcseconds = value;
                Properties.Chart.Default.ShowInArcseconds = value;
                //SetChartName();
                OnStaticPropertyChanged();
            }
        }

        private static bool _baseIndexPosition;
        public static bool BaseIndexPosition
        {
            get => _baseIndexPosition;
            set
            {
                if (_baseIndexPosition == value) return;
                _baseIndexPosition = value;
                Properties.Chart.Default.BaseIndexPosition = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _disableAnimations;
        public static bool DisableAnimations
        {
            get => _disableAnimations;
            set
            {
                if (_disableAnimations == value) return;
                _disableAnimations = value;
                Properties.Chart.Default.DisableAnimations = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _axisMinSeconds;
        public static int AxisMinSeconds
        {
            get => _axisMinSeconds;
            set
            {
                if (_axisMinSeconds == value) return;
                _axisMinSeconds = value;
                Properties.Chart.Default.AxisMinSeconds = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _animationTime;
        public static int AnimationTime
        {
            get => _animationTime;
            set
            {
                if (_animationTime == value) return;
                _animationTime = value;
                Properties.Chart.Default.AnimationTime = value;
               // AnimationsSpeed = TimeSpan.FromMilliseconds(value * 100);
                OnStaticPropertyChanged();
            }
        }

        private static int _lineSmoothness;
        public static int LineSmoothness
        {
            get => _lineSmoothness;
            set
            {
                if (_lineSmoothness == value) return;
                _lineSmoothness = value;
                Properties.Chart.Default.LineSmoothness = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// will upgrade if necessary
        /// </summary>
        public static void Load()
        {
            Upgrade();

            ThirdColor = Properties.Chart.Default.ThirdColor;
            FourthColor = Properties.Chart.Default.FourthColor;
            RaColor = Properties.Chart.Default.RaColor;
            DecColor = Properties.Chart.Default.DecColor;
            InvertRa = Properties.Chart.Default.InvertRa;
            InvertDec = Properties.Chart.Default.InvertDec;
            InvertThird = Properties.Chart.Default.InvertThird;
            InvertFourth = Properties.Chart.Default.InvertFourth;
            RaLine = Properties.Chart.Default.RaLine;
            RaBar = Properties.Chart.Default.RaBar;
            RaStep = Properties.Chart.Default.RaStep;
            DecLine = Properties.Chart.Default.DecLine;
            DecBar = Properties.Chart.Default.DecBar;
            DecStep = Properties.Chart.Default.DecStep;
            ThirdLine = Properties.Chart.Default.ThirdLine;
            ThirdBar = Properties.Chart.Default.ThirdBar;
            ThirdStep = Properties.Chart.Default.ThirdStep;
            FourthLine = Properties.Chart.Default.FourthLine;
            FourthBar = Properties.Chart.Default.FourthBar;
            FourthStep = Properties.Chart.Default.FourthStep;
            PhdHostText = Properties.Chart.Default.PhdHostText;
            ShowInArcseconds = Properties.Chart.Default.ShowInArcseconds;
            BaseIndexPosition = Properties.Chart.Default.BaseIndexPosition;
            DisableAnimations = Properties.Chart.Default.DisableAnimations;
            AxisMinSeconds = Properties.Chart.Default.AxisMinSeconds;
            AnimationTime = Properties.Chart.Default.AnimationTime;
            LineSmoothness = Properties.Chart.Default.LineSmoothness;
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        private static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Chart.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Chart.Default.Upgrade();
            Properties.Chart.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Chart.Default.Save();
            Properties.Chart.Default.Reload();
        }

        /// <summary>
        /// output setting to the monitor for logging
        /// </summary>
        public static void LogSettings()
        {
            var settingsProperties = Properties.Chart.Default.Properties.OfType<SettingsProperty>().OrderBy(s => s.Name);
            const int itemsinrow = 4;
            var count = 0;
            var msg = string.Empty;
            foreach (var currentProperty in settingsProperties)
            {
                msg += $"{currentProperty.Name}={Properties.Chart.Default[currentProperty.Name]},";
                count++;
                if (count < itemsinrow) continue;

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);

                count = 0;
                msg = string.Empty;
            }

            if (msg.Length <= 0) return;
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{msg}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
