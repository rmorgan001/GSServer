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
   Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GS.Shared;
using XInputDotNetPure;

namespace GS.Server.GamePad
{
    public sealed class GamePadXInput : GamePad
    {
        private bool _IsAvailable;

        public override bool IsAvailable
        {
            get => _IsAvailable;
        }

        //static PlayerIndex[] playerIndices = new PlayerIndex[] { PlayerIndex.One, PlayerIndex.Two, PlayerIndex.Three, PlayerIndex.Four };

        private GamePadState gamePadState;
        private uint lastPacketNumber;

        public GamePadDeadZone DeadZone { get; set; }


        // public bool LinkTriggersToVibration { get; set; }

        public GamePadXInput() : base()
        {
            DeadZone = GamePadDeadZone.IndependentAxes;
            Find();
        }

        public override void Find()
        {
            try
            {
                gamePadState = XInputDotNetPure.GamePad.GetState(PlayerIndex.One, DeadZone);
                _IsAvailable = gamePadState.IsConnected;
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

            }
        }


        public override void Poll(float vibrateLeft, float vibrateRight)
        {
            try
            {
                gamePadState = XInputDotNetPure.GamePad.GetState(PlayerIndex.One, DeadZone);
                if (gamePadState.IsConnected)
                {

                    bool changed = false;

                    if (gamePadState.PacketNumber != lastPacketNumber)
                    {
                        lastPacketNumber = gamePadState.PacketNumber;
                        changed = true;
                    }

                    XInputDotNetPure.GamePad.SetVibration(PlayerIndex.One, vibrateLeft, vibrateRight);
                    if (changed)
                    {
                        UpdateState();
                    }
                    _IsAvailable = true;
                }
                else
                {
                    _IsAvailable = false;
                }
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
            }
        }

        private void UpdateState()
        {
            //  checkGuide.Checked = reporterState.LastActiveState.Buttons.Guide == XInputDotNetPure.ButtonState.Pressed;
            Buttons = new bool[]
            {
                gamePadState.Buttons.A == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.B == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.X == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.Y == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.LeftShoulder == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.Back == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.Start == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.LeftStick == XInputDotNetPure.ButtonState.Pressed,
                gamePadState.Buttons.RightStick == XInputDotNetPure.ButtonState.Pressed
            };

            //checkDPadUp.Checked = reporterState.LastActiveState.DPad.Up == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadRight.Checked = reporterState.LastActiveState.DPad.Right == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadDown.Checked = reporterState.LastActiveState.DPad.Down == XInputDotNetPure.ButtonState.Pressed;
            //checkDPadLeft.Checked = reporterState.LastActiveState.DPad.Left == XInputDotNetPure.ButtonState.Pressed;
            int povState = -1;
            if (gamePadState.DPad.Up == XInputDotNetPure.ButtonState.Pressed
                && gamePadState.DPad.Right != XInputDotNetPure.ButtonState.Pressed
                && gamePadState.DPad.Left != XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 0;
            }
            else if (gamePadState.DPad.Up == XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Right == XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 4500;
            }
            else if (gamePadState.DPad.Right == XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Up != XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Down != XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 9000;
            }
            else if (gamePadState.DPad.Right == XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Down == XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 13500;
            }

            else if (gamePadState.DPad.Down == XInputDotNetPure.ButtonState.Pressed
                && gamePadState.DPad.Right != XInputDotNetPure.ButtonState.Pressed
                && gamePadState.DPad.Left != XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 18000;
            }
            else if (gamePadState.DPad.Down == XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Left == XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 22500;
            }
            else if (gamePadState.DPad.Left == XInputDotNetPure.ButtonState.Pressed
                    && gamePadState.DPad.Up != XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Down != XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 27000;
            }
            else if (gamePadState.DPad.Left == XInputDotNetPure.ButtonState.Pressed
                     && gamePadState.DPad.Up == XInputDotNetPure.ButtonState.Pressed)
            {
                povState = 31500;
            }
            POVs = new int[] { povState, -1, -1, -1};

            //labelTriggerLeft.Text = FormatFloat(reporterState.LastActiveState.Triggers.Left);
            //labelTriggerRight.Text = FormatFloat(reporterState.LastActiveState.Triggers.Right);
            ZAxis = RangeForDirectX(-gamePadState.Triggers.Left + gamePadState.Triggers.Right);

            //labelStickLeftX.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Left.X);
            //labelStickLeftY.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Left.Y);

            XAxis = RangeForDirectX(gamePadState.ThumbSticks.Left.X);    
            YAxis = RangeForDirectX(gamePadState.ThumbSticks.Left.Y);

            //labelStickRightX.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Right.X);
            //labelStickRightY.Text = FormatFloat(reporterState.LastActiveState.ThumbSticks.Right.Y);
            XRotation = RangeForDirectX(gamePadState.ThumbSticks.Right.X);
            YRotation = RangeForDirectX(gamePadState.ThumbSticks.Right.Y);

            //if (reporterState.LastActiveState.Buttons.Start == XInputDotNetPure.ButtonState.Pressed)
            //{
            //    timerStart.Start();
            //}
            //else
            //{
            //    timerStart.Stop();
            //}
            //if (reporterState.LastActiveState.Buttons.Back == XInputDotNetPure.ButtonState.Pressed)
            //{
            //    timerBack.Start();
            //}
            //else
            //{
            //    timerBack.Stop();
            //}

            //for (int i = 0; i < 4; i++)
            //{
            //    controllerControls[i].Visible = i == reporterState.LastActiveIndex && reporterState.LastActiveState.IsConnected;
            //}

            //PositionStickControl(stickControls[0], stickControlPositions[0], reporterState.LastActiveState.ThumbSticks.Left);
            //PositionStickControl(stickControls[1], stickControlPositions[1], reporterState.LastActiveState.ThumbSticks.Right);
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
