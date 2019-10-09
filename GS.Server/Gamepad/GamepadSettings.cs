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

using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
            }

            return settingsDict;
        }

        public static void SaveSettings( Dictionary<string, string> settingsDict)
        {
            if (settingsDict == null) return;
            foreach (var setting in settingsDict)
            {
                switch (setting.Key)
                {
                    case "tracking":
                        Properties.Gamepad.Default.tracking = setting.Value;
                        break;
                    case "stop":
                        Properties.Gamepad.Default.stop = setting.Value;
                        break;
                    case "park":
                        Properties.Gamepad.Default.park = setting.Value;
                        break;
                    case "home":
                        Properties.Gamepad.Default.home = setting.Value;
                        break;
                    case "speeddown":
                        Properties.Gamepad.Default.speeddown = setting.Value;
                        break;
                    case "speedup":
                        Properties.Gamepad.Default.speedup = setting.Value;
                        break;
                    case "up":
                        Properties.Gamepad.Default.up = setting.Value;
                        break;
                    case "down":
                        Properties.Gamepad.Default.down = setting.Value;
                        break;
                    case "left":
                        Properties.Gamepad.Default.left = setting.Value;
                        break;
                    case "right":
                        Properties.Gamepad.Default.right = setting.Value;
                        break;
                    case "volumedown":
                        Properties.Gamepad.Default.volumedown = setting.Value;
                        break;
                    case "volumeup":
                        Properties.Gamepad.Default.volumeup = setting.Value;
                        break;
                }
            }
            Save();
        }

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
