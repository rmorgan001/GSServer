/* The MIT License
   
   Copyright(c) 2009 Remi Gillig <remigillig@gmail.com>
   
   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files(the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and /or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions :
   
   The above copyright notice and this permission notice shall be included in
   all copies or substantial portions of the Software.
   
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT.IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
   THE SOFTWARE.
   
    https://github.com/speps/XInputDotNet
*/

using System.Runtime.InteropServices;

namespace XInputDotNetPure
{
    class Imports
    {
        internal const string DllName = "XInputInterface";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint XInputGamePadGetState(uint playerIndex, out GamePadState.RawState state);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void XInputGamePadSetState(uint playerIndex, float leftMotor, float rightMotor);
    }

    public enum ButtonState
    {
        Pressed,
        Released
    }

    public struct GamePadButtons
    {
        private readonly ButtonState _start;
        private readonly ButtonState _back;
        private readonly ButtonState _leftStick;
        private readonly ButtonState _rightStick;
        private readonly ButtonState _leftShoulder;
        private readonly ButtonState _rightShoulder;
        private readonly ButtonState _guide;
        private readonly ButtonState _a;
        private readonly ButtonState _b;
        private readonly ButtonState _x;
        private readonly ButtonState _y;

        internal GamePadButtons(ButtonState start, ButtonState back, ButtonState leftStick, ButtonState rightStick,
                                ButtonState leftShoulder, ButtonState rightShoulder, ButtonState guide,
                                ButtonState a, ButtonState b, ButtonState x, ButtonState y)
        {
            this._start = start;
            this._back = back;
            this._leftStick = leftStick;
            this._rightStick = rightStick;
            this._leftShoulder = leftShoulder;
            this._rightShoulder = rightShoulder;
            this._guide = guide;
            this._a = a;
            this._b = b;
            this._x = x;
            this._y = y;
        }

        public ButtonState Start
        {
            get { return _start; }
        }

        public ButtonState Back
        {
            get { return _back; }
        }

        public ButtonState LeftStick
        {
            get { return _leftStick; }
        }

        public ButtonState RightStick
        {
            get { return _rightStick; }
        }

        public ButtonState LeftShoulder
        {
            get { return _leftShoulder; }
        }

        public ButtonState RightShoulder
        {
            get { return _rightShoulder; }
        }

        public ButtonState Guide
        {
            get { return _guide; }
        }

        public ButtonState A
        {
            get { return _a; }
        }

        public ButtonState B
        {
            get { return _b; }
        }

        public ButtonState X
        {
            get { return _x; }
        }

        public ButtonState Y
        {
            get { return _y; }
        }
    }

    public struct GamePadDPad
    {
        internal GamePadDPad(ButtonState up, ButtonState down, ButtonState left, ButtonState right)
        {
            this.Up = up;
            this.Down = down;
            this.Left = left;
            this.Right = right;
        }

        public ButtonState Up { get; }

        public ButtonState Down { get; }

        public ButtonState Left { get; }

        public ButtonState Right { get; }
    }

    public struct GamePadThumbSticks
    {
        public struct StickValue
        {
            private readonly float _x;
            private readonly float _y;

            internal StickValue(float x, float y)
            {
                this._x = x;
                this._y = y;
            }

            public float X
            {
                get { return _x; }
            }

            public float Y
            {
                get { return _y; }
            }
        }

        internal GamePadThumbSticks(StickValue left, StickValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public StickValue Left { get; }

        public StickValue Right { get; }
    }

    public struct GamePadTriggers
    {
        internal GamePadTriggers(float left, float right)
        {
            this.Left = left;
            this.Right = right;
        }

        public float Left { get; }

        public float Right { get; }
    }

    public struct GamePadState
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct RawState
        {
            public uint dwPacketNumber;
            public GamePad Gamepad;

            [StructLayout(LayoutKind.Sequential)]
            public struct GamePad
            {
                public ushort wButtons;
                public byte bLeftTrigger;
                public byte bRightTrigger;
                public short sThumbLX;
                public short sThumbLY;
                public short sThumbRX;
                public short sThumbRY;
            }
        }

        private readonly bool _isConnected;
        private readonly uint _packetNumber;
        private readonly GamePadButtons _buttons;
        private readonly GamePadDPad _dPad;
        private readonly GamePadThumbSticks _thumbSticks;
        private readonly GamePadTriggers _triggers;

        enum ButtonsConstants
        {
            DPadUp = 0x00000001,
            DPadDown = 0x00000002,
            DPadLeft = 0x00000004,
            DPadRight = 0x00000008,
            Start = 0x00000010,
            Back = 0x00000020,
            LeftThumb = 0x00000040,
            RightThumb = 0x00000080,
            LeftShoulder = 0x0100,
            RightShoulder = 0x0200,
            Guide = 0x0400,
            A = 0x1000,
            B = 0x2000,
            X = 0x4000,
            Y = 0x8000
        }

        internal GamePadState(bool isConnected, RawState rawState, GamePadDeadZone deadZone)
        {
            this._isConnected = isConnected;

            if (!isConnected)
            {
                rawState.dwPacketNumber = 0;
                rawState.Gamepad.wButtons = 0;
                rawState.Gamepad.bLeftTrigger = 0;
                rawState.Gamepad.bRightTrigger = 0;
                rawState.Gamepad.sThumbLX = 0;
                rawState.Gamepad.sThumbLY = 0;
                rawState.Gamepad.sThumbRX = 0;
                rawState.Gamepad.sThumbRY = 0;
            }

            _packetNumber = rawState.dwPacketNumber;
            _buttons = new GamePadButtons(
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.Start) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.Back) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.LeftThumb) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.RightThumb) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.LeftShoulder) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.RightShoulder) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.Guide) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.A) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.B) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.X) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.Y) != 0 ? ButtonState.Pressed : ButtonState.Released
            );
            _dPad = new GamePadDPad(
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.DPadUp) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.DPadDown) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.DPadLeft) != 0 ? ButtonState.Pressed : ButtonState.Released,
                (rawState.Gamepad.wButtons & (uint)ButtonsConstants.DPadRight) != 0 ? ButtonState.Pressed : ButtonState.Released
            );

            _thumbSticks = new GamePadThumbSticks(
                Utils.ApplyLeftStickDeadZone(rawState.Gamepad.sThumbLX, rawState.Gamepad.sThumbLY, deadZone),
                Utils.ApplyRightStickDeadZone(rawState.Gamepad.sThumbRX, rawState.Gamepad.sThumbRY, deadZone)
            );
            _triggers = new GamePadTriggers(
                Utils.ApplyTriggerDeadZone(rawState.Gamepad.bLeftTrigger, deadZone),
                Utils.ApplyTriggerDeadZone(rawState.Gamepad.bRightTrigger, deadZone)
            );
        }

        public uint PacketNumber
        {
            get { return _packetNumber; }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public GamePadButtons Buttons
        {
            get { return _buttons; }
        }

        public GamePadDPad DPad
        {
            get { return _dPad; }
        }

        public GamePadTriggers Triggers
        {
            get { return _triggers; }
        }

        public GamePadThumbSticks ThumbSticks
        {
            get { return _thumbSticks; }
        }
    }

    public enum PlayerIndex
    {
        One = 0,
        Two,
        Three,
        Four
    }

    public enum GamePadDeadZone
    {
        Circular,
        IndependentAxes,
        None
    }

    public class GamePad
    {
        public static GamePadState GetState(PlayerIndex playerIndex)
        {
            return GetState(playerIndex, GamePadDeadZone.IndependentAxes);
        }

        public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZone)
        {
            GamePadState.RawState state;
            uint result = Imports.XInputGamePadGetState((uint)playerIndex, out state);
            return new GamePadState(result == Utils.Success, state, deadZone);
        }

        public static void SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
        {
            Imports.XInputGamePadSetState((uint)playerIndex, leftMotor, rightMotor);
        }
    }
}
