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
                XAxis = State.X;
                YAxis = State.Y;
                ZAxis = State.Z;
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
