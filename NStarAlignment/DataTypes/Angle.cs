/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

/*
 * This code was originally developed by Aeronautical Software Developments Limited as
 * part of PDToolkit. It is included here with their permission.
 * http://www.aerosoftdev.com/
 * 
 */
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace NStarAlignment.DataTypes
{
    /// <summary>
    /// Represents a compass angle in degrees.
    /// </summary>
    public struct Angle
       : IComparable
    {
        private double _value;        /* Always held in double degrees */
        private int _degrees;
        private int _minutes;
        private double _seconds;
        private const double DEFAULT_DEGREES_DELTA = 0.000001D;    /* In decimal degrees */

        public Angle(double angle, bool radians = false)
        {
            _value = radians ? Angle.RadiansToDegrees(angle) : angle;

            /* All members must be set before calling out of the constructor so set dummy values
               so compiler is happy.
            */
            _degrees = 0;
            _minutes = 0;
            _seconds = 0.0D;
            SetDmsFromDegrees(angle);
        }


        public Angle(int degrees, int minutes, double seconds)
        {
            /* All members must be set before calling out of the constructor so set dummy values
               so compiler is happy.
            */
            _value = 0.0D;

            _degrees = degrees;
            _minutes = minutes;
            _seconds = seconds;
            _value = SetDegreesFromDms();
        }


        /// <summary>
        /// Gets or sets the value of the <see cref="Angle"/> in double degrees.
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                SetDmsFromDegrees(_value);
            }
        }

        /// <summary>
        /// Gets or sets the value of the <see cref="Angle"/> in radians.
        /// </summary>
        [JsonIgnore]
        public double Radians
        {
            get => Angle.DegreesToRadians(_value);
            set
            {
                _value = Angle.RadiansToDegrees(value);
                SetDmsFromDegrees(_value);
            }
        }


        [JsonIgnore]
        public double Cos => Math.Cos(this.Radians);

        [JsonIgnore]
        public double Sin => Math.Sin(this.Radians);

        [JsonIgnore]
        public double Tan => Math.Tan(this.Radians);

        #region Operator overloads ...
        /// <summary>
        /// Gets a value indicating whether the value has been set.
        /// </summary>
        /// <value>
        /// <b>True</b> if no value has been set; otherwise, <b>False</b>.
        /// </value>
        [Browsable(false)]

        public static implicit operator Angle(double angle)
        {
            return new Angle(angle);
        }

        public static implicit operator double(Angle angle)
        {
            return angle._value;
        }

        public static implicit operator string(Angle angle) => angle.Value.ToString(CultureInfo.CurrentCulture);

        /// <summary>
        /// Compares the two specified angles, using the default delta value.
        /// </summary>
        public static bool operator ==(Angle angle1, Angle angle2)
        {
            /* Just in case of rounding errors... */
            return (Math.Abs(angle1._value - angle2._value) <= Angle.DEFAULT_DEGREES_DELTA
                    || (angle1._degrees == angle2._degrees
                        && angle1._minutes == angle2._minutes
                        && Math.Abs(angle1._seconds - angle2._seconds) <= (Angle.DEFAULT_DEGREES_DELTA * 3600D)));
        }

        public static bool operator !=(Angle angle1, Angle angle2)
        {
            return !(angle1 == angle2);
        }

        public static bool operator <(Angle angle1, Angle angle2)
        {
            return (angle1._value < angle2._value);
        }

        public static bool operator <=(Angle angle1, Angle angle2)
        {
            return (angle1._value <= angle2._value);
        }

        public static bool operator >(Angle angle1, Angle angle2)
        {
            return (angle1._value > angle2._value);
        }

        public static bool operator >=(Angle angle1, Angle angle2)
        {
            return (angle1._value >= angle2._value);
        }

        public static Angle operator +(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Value + angle2.Value);
        }

        public static Angle operator -(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Value - angle2.Value);
        }

        public static Angle operator *(Angle angle, double factor)
        {
            return new Angle(angle.Value * factor);
        }

        #endregion

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is Angle angle
                    && this == angle);
        }


        public override string ToString()
        {
            return Value.ToString(CultureInfo.CurrentCulture);
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        public static double DegreesToRadians(double degrees)
        {
            return degrees / (180.0 / Math.PI);
        }

        public static bool AreSameDegrees(double a1, double a2, double tolerance = 0)
        {
            double cosa1 = Math.Cos(Angle.DegreesToRadians(a1));
            double cosa2 = Math.Cos(Angle.DegreesToRadians(a2));
            return Math.Abs(cosa2 - cosa1) <= tolerance;
        }

        private void SetDmsFromDegrees(double angle)
        {
            _degrees = (int)Truncate(angle);
            angle = (angle - _degrees) * 60.0D;
            _minutes = (int)Truncate(angle);
            _seconds = (angle - _minutes) * 60.0D;
        }

        private double SetDegreesFromDms()
        {
            if (_degrees < 0 || _minutes < 0 || _seconds < 0.0D)
            {
                SetDmsToNegative();
            }

            return _degrees + (_minutes / 60.0D) + (_seconds / 3600.0D);
        }

        private void SetDmsToNegative()
        {
            if (_degrees > 0)
            {
                _degrees *= -1;
            }

            if (_minutes > 0)
            {
                _minutes *= -1;
            }

            if (_seconds > 0.0D)
            {
                _seconds *= -1.0D;
            }
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            int result;

            if (!(obj is Angle))
            {
                result = 1;
            }
            else
            {
                var that = (Angle)obj;
                result = _value.CompareTo(that._value);
            }

            return result;
        }

        #endregion

        public static double Truncate(double value)
        {
            return Math.Truncate(value);
        }

        /// <summary>
        /// Converts an angle to the range 0.0 to 360.0 degrees
        /// </summary>
        public static double Range360(double ang)
        {
            ang = ang % 360.0;
            while (ang < 0.0)
            {
                ang = ang + 360.0;
            }
            return ang;
        }
    }
}

