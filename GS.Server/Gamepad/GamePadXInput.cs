/*
MIT License

Copyright (c) 2021 Phil Crompton (phil@unitysoftware.co.uk)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

   Portions
   Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Reflection;
using System.Threading;
using GS.Principles;
//using GS.Server.Properties;
using GS.Shared;
using XInputDotNetPure;

namespace GS.Server.GamePad
{
    public sealed class GamePadXInput : GamePad
    {
        private bool _isAvailable;

        public override bool IsAvailable => _isAvailable;

        private GamePadState _gamePadState;
        private uint _lastPacketNumber;

        private GamePadDeadZone DeadZone { get; }


        // public bool LinkTriggersToVibration { get; set; }

        public GamePadXInput()
        {
            DeadZone = GamePadDeadZone.IndependentAxes;
            Find();
        }

        public override void Find()
        {
            try
            {
                _gamePadState = XInputDotNetPure.GamePad.GetState(PlayerIndex.One, DeadZone);
                _isAvailable = _gamePadState.IsConnected;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"|XInput|{_isAvailable}"
                };
                MonitorLog.LogToMonitor(monitorItem);

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

            }
        }


        public override void Poll(float vibrateLeft, float vibrateRight)
        {
            try
            {
                _gamePadState = XInputDotNetPure.GamePad.GetState(PlayerIndex.One, DeadZone);
                if (_gamePadState.IsConnected)
                {

                    bool changed = false;

                    if (_gamePadState.PacketNumber != _lastPacketNumber)
                    {
                        _lastPacketNumber = _gamePadState.PacketNumber;
                        changed = true;
                    }

                    XInputDotNetPure.GamePad.SetVibration(PlayerIndex.One, vibrateLeft, vibrateRight);
                    if (changed)
                    {
                        UpdateState();
                    }
                    _isAvailable = true;
                }
                else
                {
                    _isAvailable = false;
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
            }
        }

        private void UpdateState()
        {
            //  checkGuide.Checked = reporterState.LastActiveState.Buttons.Guide == XInputDotNetPure.ButtonState.Pressed;
            Buttons = new[]
            {
                _gamePadState.Buttons.A == ButtonState.Pressed,
                _gamePadState.Buttons.B == ButtonState.Pressed,
                _gamePadState.Buttons.X == ButtonState.Pressed,
                _gamePadState.Buttons.Y == ButtonState.Pressed,
                _gamePadState.Buttons.LeftShoulder == ButtonState.Pressed,
                _gamePadState.Buttons.RightShoulder == ButtonState.Pressed,
                _gamePadState.Buttons.Back == ButtonState.Pressed,
                _gamePadState.Buttons.Start == ButtonState.Pressed,
                _gamePadState.Buttons.LeftStick == ButtonState.Pressed,
                _gamePadState.Buttons.RightStick == ButtonState.Pressed
            };

            //checkDPadUp.Checked = reporterState.LastActiveState.DPad.Up == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadRight.Checked = reporterState.LastActiveState.DPad.Right == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadDown.Checked = reporterState.LastActiveState.DPad.Down == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadLeft.Checked = reporterState.LastActiveState.DPad.Left == XInputDotNetPure.ButtonState.Pressed;
            int povState = -1;
            if (_gamePadState.DPad.Up == ButtonState.Pressed
                && _gamePadState.DPad.Right != ButtonState.Pressed
                && _gamePadState.DPad.Left != ButtonState.Pressed)
            {
                povState = 0;
            }
            else if (_gamePadState.DPad.Up == ButtonState.Pressed
                     && _gamePadState.DPad.Right == ButtonState.Pressed)
            {
                povState = 4500;
            }
            else if (_gamePadState.DPad.Right == ButtonState.Pressed
                     && _gamePadState.DPad.Up != ButtonState.Pressed
                     && _gamePadState.DPad.Down != ButtonState.Pressed)
            {
                povState = 9000;
            }
            else if (_gamePadState.DPad.Right == ButtonState.Pressed
                     && _gamePadState.DPad.Down == ButtonState.Pressed)
            {
                povState = 13500;
            }

            else if (_gamePadState.DPad.Down == ButtonState.Pressed
                && _gamePadState.DPad.Right != ButtonState.Pressed
                && _gamePadState.DPad.Left != ButtonState.Pressed)
            {
                povState = 18000;
            }
            else if (_gamePadState.DPad.Down == ButtonState.Pressed
                     && _gamePadState.DPad.Left == ButtonState.Pressed)
            {
                povState = 22500;
            }
            else if (_gamePadState.DPad.Left == ButtonState.Pressed
                    && _gamePadState.DPad.Up != ButtonState.Pressed
                     && _gamePadState.DPad.Down != ButtonState.Pressed)
            {
                povState = 27000;
            }
            else if (_gamePadState.DPad.Left == ButtonState.Pressed
                     && _gamePadState.DPad.Up == ButtonState.Pressed)
            {
                povState = 31500;
            }
            POVs = new[] { povState, -1, -1, -1};

            //labelTriggerLeft.Text = FormatFloat(reporterState.LastActiveState.Triggers.Left);
            //labelTriggerRight.Text = FormatFloat(reporterState.LastActiveState.Triggers.Right);
            ZAxis = RangeForDirectX(-_gamePadState.Triggers.Left + _gamePadState.Triggers.Right);

            //labelStickLeftX.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Left.X);
            //labelStickLeftY.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Left.Y);

            XAxis = RangeForDirectX(_gamePadState.ThumbSticks.Left.X);    
            YAxis = RangeForDirectX(_gamePadState.ThumbSticks.Left.Y);

            //labelStickRightX.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Right.X);
            //labelStickRightY.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Right.Y);
            XRotation = RangeForDirectX(_gamePadState.ThumbSticks.Right.X);
            YRotation = RangeForDirectX(_gamePadState.ThumbSticks.Right.Y);
            ZRotation = null;
        }

        /// <summary>
        /// Convert an XInput axis with a range of -1 to 1 to match a DirectX axis with a range 0 to 65535
        /// </summary>
        /// <param name="x">XInput axis value</param>
        /// <returns></returns>
        private int RangeForDirectX(float x)
        {
            return (int) ((x + 1) * 32767.5);
        }

        public override void Dispose()
        {

        }
    }
}
