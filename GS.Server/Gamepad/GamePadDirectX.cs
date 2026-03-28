/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using SharpDX.DirectInput;
using GS.Shared;
using System.Reflection;
using System.Threading;
using System.Linq;
using GS.Principles;

namespace GS.Server.GamePad
{
    public sealed class GamePadDirectX: GamePad
    {
        private bool _isAvailable;
        public override bool IsAvailable => _isAvailable;

        private Joystick _joystick;
        private readonly DirectInput _directInput;
        private JoystickState State { get; set; }
        private Guid _joystickGuid;
        private readonly bool[] _axisAvailable = new bool[]
        {
            false,      // X-axis
            false,      // Y-axis
            false,      // Z-axis
            false,      // X-rotation
            false,      // Y-rotation
            false       // Z-rotation
        };

        private bool _previousAvailability;
        private readonly IntPtr _hWnd;

        /// <summary>
        /// Constructor sets up and find a joystick
        /// </summary>
        /// <param name="windowHandle"></param>
        public GamePadDirectX(IntPtr windowHandle)
        {
            _hWnd = windowHandle;
            _directInput = new DirectInput();
            _joystickGuid = Guid.Empty;
            Find();
        }


        /// <summary>
        /// Finds a valid joystick or game pad that is attached
        /// </summary>
        public override void Find()
        {
            try
            {
                foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly))
                    _joystickGuid = deviceInstance.InstanceGuid;

                // If Game pad not found, look for a Joystick
                if (_joystickGuid == Guid.Empty)
                    foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
                        _joystickGuid = deviceInstance.InstanceGuid;

                // If Joystick not found, throws an error
                if (_joystickGuid == Guid.Empty)
                {
                    _isAvailable = false;

                    // Only log if state changed (was available, now not)
                    if (_previousAvailability != _isAvailable)
                    {
                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Ui,
                            Category = MonitorCategory.Server,
                            Type = MonitorType.Data,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"|DirectX|{_isAvailable}|Not Found"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                        _previousAvailability = _isAvailable;
                    }
                    return;
                }

                // Instantiate the joystick
                _joystick = new Joystick(_directInput, _joystickGuid);
                _joystick.SetCooperativeLevel(_hWnd, CooperativeLevel.Background | CooperativeLevel.Exclusive);

                // Query supported info
                //var allEffects = joystick.GetEffects();
                var deviceObjects = _joystick.GetObjects();
                _axisAvailable[0] = deviceObjects.Any(o => o.Name == "X Axis");
                _axisAvailable[1] = deviceObjects.Any(o => o.Name == "Y Axis");
                _axisAvailable[2] = deviceObjects.Any(o => o.Name == "Z Axis");
                _axisAvailable[3] = deviceObjects.Any(o => o.Name == "X Rotation");
                _axisAvailable[4] = deviceObjects.Any(o => o.Name == "Y Rotation");
                _axisAvailable[5] = deviceObjects.Any(o => o.Name == "Z Rotation");

                //var deviceInfo = joystick.Information;
                //var cps = joystick.Capabilities;
                //var axisCount = cps.AxeCount;
                //var povCount = cps.PovCount;
                //var deviceFlags = cps.Flags;

                // Set BufferSize in order to use buffered data.
                _joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                _joystick.Acquire();

                _isAvailable = true;

                // Log if state changed (was not available, now is)
                if (_previousAvailability != _isAvailable)
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Ui,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Data,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"|DirectX|{_isAvailable}|Device Found"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    _previousAvailability = _isAvailable;
                }
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
        public override void Poll(float vibrateLeft, float vibrateRight)
        {
            try
            {
                if (!IsAvailable) return;
                // joystick.Acquire();
                // joystick.Poll();
                State = null;
                State = _joystick.GetCurrentState();
                Buttons = State.Buttons;
                POVs = State.PointOfViewControllers;
                XAxis = _axisAvailable[0] ? (int?)State.X: null;
                YAxis = _axisAvailable[1] ? (int?)State.Y : null;
                ZAxis = _axisAvailable[2] ? (int?)State.Z : null;
                XRotation = _axisAvailable[3] ? (int?)State.RotationX : null;
                YRotation = _axisAvailable[4] ? (int?)State.RotationY : null;
                ZRotation = _axisAvailable[5] ? (int?)State.RotationZ : null;
                // Data = joystick.GetBufferedData();
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
            _joystick?.Unacquire();
        }

        /// <inheritdoc />
        /// <summary>
        /// clean up
        /// </summary>
        public override void Dispose()
        {
            Release();
            _joystick.Dispose();
            _directInput.Dispose();
        }

    }
}
