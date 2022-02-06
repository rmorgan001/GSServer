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


namespace GS.Server.Focuser
{
    public static class FocuserSettings
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion


        #region Properties 
        private static string _deviceId;
        public static string DeviceId
        {
            get => _deviceId;
            set
            {
                if (_deviceId == value) return;
                _deviceId = value;
                Properties.Focuser.Default.DeviceId = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }

        private static int _stepSize;
        public static int StepSize
        {
            get => _stepSize;
            set
            {
                if (_stepSize == value) return;
                _stepSize = value;
                Properties.Focuser.Default.StepSize = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
                OnStaticPropertyChanged();
            }

        }


        private static bool _reverseDirection;
        public static bool ReverseDirection
        {
            get => _reverseDirection;
            set
            {
                if (_reverseDirection == value) return;
                _reverseDirection = value;
                Properties.Focuser.Default.ReverseDirection = value;
                LogSetting(MethodBase.GetCurrentMethod()?.Name, value.ToString());
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
            DeviceId = Properties.Focuser.Default.DeviceId;
            StepSize = Properties.Focuser.Default.StepSize;
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        public static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Alignment.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Focuser.Default.Upgrade();
            Properties.Focuser.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Focuser.Default.Save();
            Properties.Focuser.Default.Reload();
        }

        /// <summary>
        /// output to session log
        /// </summary>
        /// <param name="method"></param>
        /// <param name="value"></param>
        private static void LogSetting(string method, string value)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = $"{method}", Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
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
