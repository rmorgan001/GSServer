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
using System;
using System.Collections.Generic;

namespace GS.Server.GamePad
{
    public abstract class GamePad : IGamePad
    {
        private Dictionary<string, string> _settingsDict;

        public abstract bool IsAvailable { get; }
        public bool[] Buttons { get; protected set; }
        public int[] POVs { get; protected set; }
        public int YAxis { get; protected set; }
        public int XAxis { get; protected set; }
        public int ZAxis { get; protected set; }
        public int YRotation { get; protected set; }
        public int XRotation { get; protected set; }

        /// <summary>
        /// Constructor sets up and find a joystick
        /// </summary>
        /// <param name="window_handle"></param>
        protected GamePad()
        {
            LoadSettings();
        }

        public abstract void Find();

        public abstract void Poll(float vibrateLeft, float vibrateRight);

        public abstract void Dispose();

        /// <summary>
        /// Load a setting into the local dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void LoadSetting(string key, string value)
        {
            if (key == null) return;
            key = key.Trim().ToLower();
            value = value.ToLower().Trim();
            if (!value.Contains("button") && !key.Contains("pov") && !key.Contains("x") && !key.Contains("y") && !key.Contains("z")) return;
            _settingsDict.Add(key, value);
        }

        /// <summary>
        /// returns all the current settings
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetSettings()
        { return _settingsDict; }

        /// <summary>
        /// update or add an item in the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Update_Setting(string key, string value)
        {
            key = key.Trim().ToLower();
            if (!string.IsNullOrEmpty(value)) value = value.Trim().ToLower();
            ClearValues(value);
            if (_settingsDict.ContainsKey(key))
            {
                _settingsDict[key] = value;
            }
            else
            {
                LoadSetting(key, value);
            }
        }

        /// <summary>
        /// Clears commands from all items found.
        /// If a button is set it clears the command from all other buttons
        /// </summary>
        /// <param name="value"></param>
        private void ClearValues(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            value = value.Trim().ToLower();
            var copy = new Dictionary<string, string>(_settingsDict);
            foreach (var item in copy)
            {
                if (item.Value?.ToLower().Trim() == value)
                {
                    _settingsDict[item.Key] = null;
                }
            }
        }

        /// <summary>
        /// Gets a dictionary item by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get_ValueByKey(string key)
        {
            key = key.Trim().ToLower();
            _settingsDict.TryGetValue(key, out var result);
            return result;
        }

        /// <summary>
        /// Gets a dictionary item by value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Get_KeyByValue(string value)
        {
            value = value.Trim().ToLower();
            foreach (var item in _settingsDict)
            {
                if (item.Value == value) return item.Key;
            }
            return null;
        }


        private void LoadSettings()
        {
            GamePadSettings.Load();
            _settingsDict = GamePadSettings.LoadSettings();
        }

        public void SaveSettings()
        {
            if (_settingsDict == null) return;
            GamePadSettings.SaveSettings(_settingsDict);
        }

    }

    /// <summary>
    /// Used to store PoV commands
    /// </summary>
    internal readonly struct PovPair
    {
        public readonly int Key;
        public readonly int Value;

        public PovPair(int key, int value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Used to store X Y Z commands
    /// </summary>
    internal readonly struct AxisPair
    {
        public readonly int Key;
        public readonly string Value;
        public AxisPair(int key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
