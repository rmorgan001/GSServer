using GS.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    public static class AlignmentSettings
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion


        #region Properties 
        private static bool _isAlignmentOn;
        public static bool IsAlignmentOn
        {
            get => _isAlignmentOn;
            set
            {
                if (_isAlignmentOn == value) return;
                _isAlignmentOn = value;
                Properties.Alignment.Default.IsAlignmentOn = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static double _proximityLimit;
        public static double ProximityLimit
        {
            get => _proximityLimit;
            set
            {
                if (_proximityLimit == value) return;
                _proximityLimit = value;
                Properties.Alignment.Default.ProximityLimit = (double)value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static double _nearbyLimit;
        public static double NearbyLimit
        {
            get => _nearbyLimit;
            set
            {
                if (_nearbyLimit == value) return;
                _nearbyLimit = value;
                Properties.Alignment.Default.NearbyLimit = (double)value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static int _sampleSize;
        public static int SampleSize
        {
            get => _sampleSize;
            set
            {
                if (_sampleSize == value) return;
                _sampleSize = value;
                Properties.Alignment.Default.SampleSize = (int)value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static bool _clearModelOnStartup;
        public static bool ClearModelOnStartup
        {
            get => _clearModelOnStartup;
            set
            {
                if (_clearModelOnStartup == value) return;
                _clearModelOnStartup = value;
                Properties.Alignment.Default.ClearModelOnStartup = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, value.ToString());
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

            // properties
            IsAlignmentOn = Properties.Alignment.Default.IsAlignmentOn;
            ClearModelOnStartup = Properties.Alignment.Default.ClearModelOnStartup;
            ProximityLimit = Properties.Alignment.Default.ProximityLimit;
            NearbyLimit = Properties.Alignment.Default.NearbyLimit;
            SampleSize = Properties.Alignment.Default.SampleSize;
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        public static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Alignment.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Alignment.Default.Upgrade();
            Properties.Alignment.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Alignment.Default.Save();
            Properties.Alignment.Default.Reload();
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
