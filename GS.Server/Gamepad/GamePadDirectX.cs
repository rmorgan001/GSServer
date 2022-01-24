/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Server.GamePad
{
    public sealed class GamePadDirectX: GamePad
    {
        private bool _IsAvailable;
        public override bool IsAvailable { get => _IsAvailable ; }

        private Joystick joystick;
        private readonly DirectInput directInput;
        private JoystickState State { get; set; }
        private Guid joystickGuid;
        private bool[] axisAvailable = new bool[]
        {
            false,      // X-axis
            false,      // Y-axis
            false,      // Z-axis
            false,      // X-rotation
            false,      // Y-rotation
            false       // Z-rotation
        };

        private readonly IntPtr hWnd;

        /// <summary>
        /// Constructor sets up and find a joystick
        /// </summary>
        /// <param name="window_handle"></param>
        public GamePadDirectX(IntPtr window_handle)
        {
            hWnd = window_handle;
            directInput = new DirectInput();
            joystickGuid = Guid.Empty;
            Find();
        }


        /// <summary>
        /// Finds a valid joystick or game pad that is attached
        /// </summary>
        public override void Find()
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
                    _IsAvailable = false;
                    return;
                }

                // Instantiate the joystick
                joystick = new Joystick(directInput, joystickGuid);
                joystick.SetCooperativeLevel(hWnd, CooperativeLevel.Background | CooperativeLevel.Exclusive);

                // Query supported info
                //var allEffects = joystick.GetEffects();
                var deviceObjects = joystick.GetObjects();
                axisAvailable[0] = deviceObjects.Any(o => o.Name == "X Axis");
                axisAvailable[1] = deviceObjects.Any(o => o.Name == "Y Axis");
                axisAvailable[2] = deviceObjects.Any(o => o.Name == "Z Axis");
                axisAvailable[3] = deviceObjects.Any(o => o.Name == "X Rotation");
                axisAvailable[4] = deviceObjects.Any(o => o.Name == "Y Rotation");
                axisAvailable[5] = deviceObjects.Any(o => o.Name == "Z Rotation");

                //var deviceInfo = joystick.Information;
                //var cps = joystick.Capabilities;
                //var axisCount = cps.AxeCount;
                //var povCount = cps.PovCount;
                //var deviceFlags = cps.Flags;

                // Set BufferSize in order to use buffered data.
                joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                joystick.Acquire();

                _IsAvailable = true;
            }
            catch (Exception ex)
            {
                _IsAvailable = false;
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
        public override void Poll(float vibrateLeft, float vibrateRight)
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
                XAxis = axisAvailable[0] ? (int?)State.X: null;
                YAxis = axisAvailable[1] ? (int?)State.Y : null;
                ZAxis = axisAvailable[2] ? (int?)State.Z : null;
                XRotation = axisAvailable[3] ? (int?)State.RotationX : null;
                YRotation = axisAvailable[4] ? (int?)State.RotationY : null;
                ZRotation = axisAvailable[5] ? (int?)State.RotationZ : null;
                // Data = joystick.GetBufferedData();
            }
            catch (Exception ex)
            {
                _IsAvailable = false;
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

        /// <inheritdoc />
        /// <summary>
        /// clean up
        /// </summary>
        public override void Dispose()
        {
            Release();
            joystick.Dispose();
            directInput.Dispose();
        }

    }
}
