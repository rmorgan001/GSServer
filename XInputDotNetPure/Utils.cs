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
using System;

namespace XInputDotNetPure
{
    static class Utils
    {
        public const uint Success = 0x000;
        public const uint NotConnected = 0x000;

        private const int LeftStickDeadZone = 7849;
        private const int RightStickDeadZone = 8689;
        private const int TriggerDeadZone = 30;

        public static float ApplyTriggerDeadZone(byte value, GamePadDeadZone deadZoneMode)
        {
            return deadZoneMode == GamePadDeadZone.None ? ApplyDeadZone(value, byte.MaxValue, 0.0f) : ApplyDeadZone(value, byte.MaxValue, TriggerDeadZone);
        }

        public static GamePadThumbSticks.StickValue ApplyLeftStickDeadZone(short valueX, short valueY, GamePadDeadZone deadZoneMode)
        {
            return ApplyStickDeadZone(valueX, valueY, deadZoneMode, LeftStickDeadZone);
        }

        public static GamePadThumbSticks.StickValue ApplyRightStickDeadZone(short valueX, short valueY, GamePadDeadZone deadZoneMode)
        {
            return ApplyStickDeadZone(valueX, valueY, deadZoneMode, RightStickDeadZone);
        }

        private static GamePadThumbSticks.StickValue ApplyStickDeadZone(short valueX, short valueY, GamePadDeadZone deadZoneMode, int deadZoneSize)
        {
            if (deadZoneMode == GamePadDeadZone.Circular)
            {
                // Cast to long to avoid int overflow if valueX and valueY are both 32768, which would result in a negative number and Sqrt returns NaN
                var distanceFromCenter = (float)Math.Sqrt(valueX * (long)valueX + valueY * (long)valueY);
                var coefficient = ApplyDeadZone(distanceFromCenter, short.MaxValue, deadZoneSize);
                coefficient = coefficient > 0.0f ? coefficient / distanceFromCenter : 0.0f;
                return new GamePadThumbSticks.StickValue(
                    Clamp(valueX * coefficient),
                    Clamp(valueY * coefficient)
                );
            }
            else if (deadZoneMode == GamePadDeadZone.IndependentAxes)
            {
                return new GamePadThumbSticks.StickValue(
                    ApplyDeadZone(valueX, short.MaxValue, deadZoneSize),
                    ApplyDeadZone(valueY, short.MaxValue, deadZoneSize)
                );
            }
            else
            {
                return new GamePadThumbSticks.StickValue(
                    ApplyDeadZone(valueX, short.MaxValue, 0.0f),
                    ApplyDeadZone(valueY, short.MaxValue, 0.0f)
                );
            }
        }

        private static float Clamp(float value)
        {
            return value < -1.0f ? -1.0f : (value > 1.0f ? 1.0f : value);
        }

        private static float ApplyDeadZone(float value, float maxValue, float deadZoneSize)
        {
            if (value < -deadZoneSize)
            {
                value += deadZoneSize;
            }
            else if (value > deadZoneSize)
            {
                value -= deadZoneSize;
            }
            else
            {
                return 0.0f;
            }

            value /= maxValue - deadZoneSize;

            return Clamp(value);
        }
    }
}
