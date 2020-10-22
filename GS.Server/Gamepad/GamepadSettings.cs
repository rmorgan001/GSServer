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

using GS.Shared;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GS.Server.Gamepad
{
    public static class GamepadSettings
    {

        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        private static bool _startup;
        public static bool Startup
        {
            get => _startup;
            set
            {
                if (_startup == value) return;
                _startup = value;
                Properties.Gamepad.Default.Startup = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static int _delay;
        public static int Delay
        {
            get => _delay;
            set
            {
                if (_delay == value) return;
                _delay = value;
                Properties.Gamepad.Default.Delay = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// will upgrade if necessary
        /// </summary>
        public static void Load()
        {
            Upgrade();

            Startup = Properties.Gamepad.Default.Startup;
            Delay = Properties.Gamepad.Default.Delay;
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        private static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Gamepad.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Gamepad.Default.Upgrade();
            Properties.Gamepad.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Gamepad.Default.Save();
            Properties.Gamepad.Default.Reload();
        }

        /// <summary>
        /// store for the gamepad keys
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> LoadSettings()
        {
            var settingsDict = new Dictionary<string, string>();
            var settingsProperties = Properties.Gamepad.Default.Properties.OfType<SettingsProperty>().OrderBy(s => s.Name);
            foreach (var currentProperty in settingsProperties)
            {
                string key;
                string val = null;
                if (!string.IsNullOrEmpty(currentProperty.Name))
                {
                    key = currentProperty.Name.Trim().ToLower();
                }
                else
                {
                    continue;
                }
                var data = Properties.Gamepad.Default.GetType().GetProperty(currentProperty.Name)?.GetValue(Properties.Gamepad.Default, null);
                if (data != null) val = data.ToString();
                settingsDict.Add(key, val);
                LogSetting("Gamepad", $"{key} {val}");
            }

            return settingsDict;
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
        /// output gamepad keys to settings
        /// </summary>
        /// <param name="settingsDict"></param>
        public static void SaveSettings(Dictionary<string, string> settingsDict)
        {
            if (settingsDict == null) return;
            foreach (var setting in settingsDict)
            {
                switch (setting.Key)
                {
                    case "tracking":
                        Properties.Gamepad.Default.tracking = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"tracking {setting.Value}");
                        break;
                    case "stop":
                        Properties.Gamepad.Default.stop = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"stop {setting.Value}");
                        break;
                    case "park":
                        Properties.Gamepad.Default.park = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"park {setting.Value}");
                        break;
                    case "home":
                        Properties.Gamepad.Default.home = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"home {setting.Value}");
                        break;
                    case "speeddown":
                        Properties.Gamepad.Default.speeddown = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"speeddown {setting.Value}");
                        break;
                    case "speedup":
                        Properties.Gamepad.Default.speedup = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"speedup {setting.Value}");
                        break;
                    case "up":
                        Properties.Gamepad.Default.up = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"up {setting.Value}");
                        break;
                    case "down":
                        Properties.Gamepad.Default.down = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"down {setting.Value}");
                        break;
                    case "left":
                        Properties.Gamepad.Default.left = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"left {setting.Value}");
                        break;
                    case "right":
                        Properties.Gamepad.Default.right = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"right {setting.Value}");
                        break;
                    case "volumedown":
                        Properties.Gamepad.Default.volumedown = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"volumedown {setting.Value}");
                        break;
                    case "volumeup":
                        Properties.Gamepad.Default.volumeup = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"volumeup {setting.Value}");
                        break;
                    case "ratesidereal":
                        Properties.Gamepad.Default.ratesidereal = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"ratesidereal {setting.Value}");
                        break;
                    case "ratelunar":
                        Properties.Gamepad.Default.ratelunar = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"ratelunar {setting.Value}");
                        break;
                    case "ratesolar":
                        Properties.Gamepad.Default.ratesolar = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"ratesolar {setting.Value}");
                        break;
                    case "rateking":
                        Properties.Gamepad.Default.rateking = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"rateking {setting.Value}");
                        break;
                    case "abort":
                        Properties.Gamepad.Default.abort = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"abort {setting.Value}");
                        break;
                    case "spiralin":
                        Properties.Gamepad.Default.spiralin = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"spiralin {setting.Value}");
                        break;
                    case "spiralout":
                        Properties.Gamepad.Default.spiralout = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"spiralout {setting.Value}");
                        break;
                    case "newspiral":
                        Properties.Gamepad.Default.newspiral = setting.Value;
                        LogSetting(MethodBase.GetCurrentMethod().Name, $"newspiral {setting.Value}");
                        break;
                }
            }
            Save();
        }

        /// <summary>
        /// property event notification
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
