/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

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
using GS.Shared;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static double _proximityLimit;
        public static double ProximityLimit
        {
            get => _proximityLimit;
            set
            {
                if (Math.Abs(_proximityLimit - value) < 0.0000000000001) return;
                _proximityLimit = value;
                Properties.Alignment.Default.ProximityLimit = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }

        }

        private static double _nearbyLimit;
        public static double NearbyLimit
        {
            get => _nearbyLimit;
            set
            {
                if (Math.Abs(_nearbyLimit - value) < 0.0000000000001) return;
                _nearbyLimit = value;
                Properties.Alignment.Default.NearbyLimit = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                Properties.Alignment.Default.SampleSize = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
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
