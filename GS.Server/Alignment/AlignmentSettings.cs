/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com),
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

        private static bool _isAlertOn;
        public static bool IsAlertOn
        {
            get => _isAlertOn;
            set
            {   
                if (_isAlertOn == value) return;
                _isAlertOn = value;
                if (_isAlertOn) 
                {
                    IsAlignmentOn = false;
                }
                AlertBadge = (_isAlertOn ? "!" : "");
                OnStaticPropertyChanged();
            }

        }

        private static string _alertBadge;
        /// <summary>
        /// Trigger the display of a badge on the AlignmentTab
        /// </summary>
        public static string AlertBadge
        {
            get => _alertBadge;
            set
            {
                if (_alertBadge == value) return;
                _alertBadge = value;
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


        private static AlignmentBehaviourEnum _alignmentBehaviour;
        public static AlignmentBehaviourEnum AlignmentBehaviour
        {
            get => _alignmentBehaviour;
            set
            {
                if (_alignmentBehaviour == value) return;
                _alignmentBehaviour = value;
                Properties.Alignment.Default.AlignmentBehaviour = (int)value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }

        }

        private static ActivePointsEnum _activePoints;
        public static ActivePointsEnum ActivePoints
        {
            get => _activePoints;
            set
            {
                if (_activePoints == value) return;
                _activePoints = value;
                Properties.Alignment.Default.ActivePoints = (int)value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }

        }

        private static ThreePointAlgorithmEnum _threePointAlgorithm;
        public static ThreePointAlgorithmEnum ThreePointAlgorithm
        {
            get => _threePointAlgorithm;
            set
            {
                if (_threePointAlgorithm == value) return;
                _threePointAlgorithm = value;
                Properties.Alignment.Default.ThreePointAlgorithm = (int)value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, $"{value}");
                OnStaticPropertyChanged();
            }

        }

        private static int _alignmentWarningThreshold;
        public static int AlignmentWarningThreshold
        {
            get => _alignmentWarningThreshold;
            set
            {
                if (_alignmentWarningThreshold == value) return;
                _alignmentWarningThreshold = value;
                Properties.Alignment.Default.AlignmentWarningThreshold = (int)value;
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
            AlignmentBehaviour = (AlignmentBehaviourEnum)Properties.Alignment.Default.AlignmentBehaviour;
            ActivePoints = (ActivePointsEnum)Properties.Alignment.Default.ActivePoints;
            ThreePointAlgorithm = (ThreePointAlgorithmEnum)Properties.Alignment.Default.ThreePointAlgorithm;
            AlignmentWarningThreshold = Properties.Alignment.Default.AlignmentWarningThreshold;
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
