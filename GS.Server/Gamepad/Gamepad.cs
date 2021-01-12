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
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace GS.Server.GamePad
{
    public sealed class GamePad : IDisposable
    {
        private Joystick joystick;
        private Guid joystickGuid;
        private Dictionary<string, string> _settingsDict;
        private readonly DirectInput directInput;
        private readonly IntPtr hWnd;

        private JoystickState State { get; set; }
        public bool IsAvailable { get; private set; }
        public bool[] Buttons { get; private set; }
        public int[] POVs { get; private set; }
        public int YAxis { get; private set; }
        public int XAxis { get; private set; }
        public int ZAxis { get; private set; }
        // public IList<EffectInfo> AllEffects { get; private set; }
        // public IList<DeviceObjectInstance> DeviceObjects { get; set; }
        // public DeviceFlags DeviceFlags { get; private set; }
        // public DeviceInstance DeviceInfo { get; private set; }
        // public int PovCount { get; private set; }
        // public int AxisCount { get; private set; }
        // public JoystickUpdate[] Data { get; private set; }  // buffered data

        /// <summary>
        /// Constructor sets up and find a joystick
        /// </summary>
        /// <param name="window_handle"></param>
        public GamePad(IntPtr window_handle)
        {
            hWnd = window_handle;
            LoadSettings();
            directInput = new DirectInput();
            joystickGuid = Guid.Empty;
            Find();
        }

        /// <summary>
        /// Finds a valid joystick or game pad that is attached
        /// </summary>
        public void Find()
        {
            try
            {
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly))
                    joystickGuid = deviceInstance.InstanceGuid;

                // If Game pad not found, look for a Joystick
                if (joystickGuid == Guid.Empty)
                    foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
                        joystickGuid = deviceInstance.InstanceGuid;

                // If Joystick not found, throws an error
                if (joystickGuid == Guid.Empty)
                {
                    IsAvailable = false;
                    return;
                }

                // Instantiate the joystick
                joystick = new Joystick(directInput, joystickGuid);
                joystick.SetCooperativeLevel(hWnd, CooperativeLevel.Background | CooperativeLevel.Exclusive);

                // Query supported info
                // AllEffects = joystick.GetEffects();
                // DeviceObjects = joystick.GetObjects();
                // DeviceInfo = joystick.Information;
                // var cps = joystick.Capabilities;
                // AxisCount = cps.AxeCount;
                // PovCount = cps.PovCount;
                // DeviceFlags = cps.Flags;

                // Set BufferSize in order to use buffered data.
                joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                joystick.Acquire();

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                switch (ex.HResult)
                {
                    case unchecked((int)0x8007001E):
                    case unchecked((int)0x80040154):
                        return;
                    default:
                        throw;
                }
            }

        }

        /// <summary>
        /// Query the joystick for all items
        /// </summary>
        public void Poll()
        {
            try
            {
                if (!IsAvailable) return;
                // joystick.Acquire();
                // joystick.Poll();
                State = null;
                State = joystick.GetCurrentState();
                Buttons = State.Buttons;
                POVs = State.PointOfViewControllers;
                XAxis = State.X;
                YAxis = State.Y;
                ZAxis = State.Z;
                // Data = joystick.GetBufferedData();
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                switch (ex.HResult)
                {
                    case unchecked((int)0x8007001E):
                    case unchecked((int)0x80040154):
                        return;
                    default:
                        throw;
                }
            }

        }

        /// <summary>
        /// Releases the use of the joystick object
        /// </summary>
        private void Release()
        {
            joystick?.Unacquire();
        }

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

        /// <summary>
        /// PoV commands to int conversions
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static string PovDirection(int degrees)
        {
            switch (degrees)
            {
                case 0:
                    return "up";
                case 9000:
                    return "right";
                case 18000:
                    return "down";
                case 27000:
                    return "left";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Axis values to commands
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string AxisDirection(int number)
        {
            if (number >= 0 && number < 2000) return "low";
            if (number >= 2000 && number <= 64000) return "normal";
            if (number > 64000 && number <= 66000) return "high";
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

        /// <inheritdoc />
        /// <summary>
        /// clean up
        /// </summary>
        public void Dispose()
        {
            Release();
            _settingsDict = null;
            joystick.Dispose();
            directInput.Dispose();
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
