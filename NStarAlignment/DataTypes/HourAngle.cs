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

using System;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace NStarAlignment.DataTypes
{
    public struct HourAngle
       : IComparable
    {
        private const double DEFAULT_HOURS_DELTA = 0.00000000001;    /* In decimal hours */


        private double _value;
        private int _hours;
        private int _minutes;
        private double _seconds;


        public HourAngle(double hour, bool radians = false)
        {
            _value = Range24(radians ? RadiansToHours(hour) : hour);
            /* All members must be set before calling out of the constructor so set dummy values
               so compiler is happy.
            */
            _hours = 0;
            _minutes = 0;
            _seconds = 0.0;
            SetHmsFromHours(hour);
        }


        public HourAngle(int hours, int minutes, double seconds)
        {
            /* All members must be set before calling out of the constructor so set dummy values
               so compiler is happy.
            */
            _value = 0.0;

            _hours = hours;
            _minutes = minutes;
            _seconds = seconds;
            _value = SetHoursFromHms();
        }

        /// <summary>
        /// Gets or sets the value of the <see cref="Angle"/> in double hours.
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                SetHmsFromHours(_value);

            }
        }

        [JsonIgnore]
        public int Hours
        {
            get => _hours;
            set
            {
                _hours = value;
                MatchHmsSigns(_hours);
                _value = HmsToHours(_hours, _minutes, _seconds);
            }
        }

        [JsonIgnore]
        public int Minutes
        {
            get => _minutes;
            set
            {
                if (value < -60
                    || value > 60)
                {
                    throw new ArgumentException("Minutes must be between -60 and 60", nameof(value));
                }
                _minutes = value;
                MatchHmsSigns(_minutes);
                _value = HmsToHours(_hours, _minutes, _seconds);
            }
        }

        [JsonIgnore]
        public double Seconds
        {
            get => _seconds;
            set
            {
                if (value < -60.0
                    || value > 60.0)
                {
                    throw new ArgumentException("Seconds must be between -60.0 and 60.0", nameof(value));
                }
                _seconds = value;
                MatchHmsSigns(_value);
                _value = HmsToHours(_hours, _minutes, _seconds);
            }
        }

        /// <summary>
        /// Gets the absolute value of the <see cref="Angle"/>.
        /// </summary>

        [JsonIgnore]
        public HourAngle Abs =>
            /* Use the individual elements to avoid rounding errors */
            new HourAngle(Math.Abs(Hours), Math.Abs(Minutes), Math.Abs(Seconds));


        /// <summary>
        /// Gets a value indicating whether the value has been set.
        /// </summary>
        /// <value>
        /// <b>True</b> if no value has been set; otherwise, <b>False</b>.
        /// </value>

        public static implicit operator HourAngle(double hour)
        {
            return new HourAngle(hour);
        }

        public static implicit operator double(HourAngle hour)
        {
            return hour.Value;
        }


        public static implicit operator string(HourAngle hour) => hour.Value.ToString(CultureInfo.CurrentCulture);

        /// <summary>
        /// Compares the two specified hours, using the default delta value.
        /// </summary>
        public static bool operator ==(HourAngle hour1, HourAngle hour2)
        {
            /* Just in case of rounding errors... */
            return (Math.Abs(hour1.Value - hour2.Value) <= DEFAULT_HOURS_DELTA);
        }

        public static bool operator !=(HourAngle hour1, HourAngle hour2)
        {
            return !(hour1 == hour2);
        }


        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is HourAngle angle
                    && this == angle);
        }

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);


        private static double HmsToHours(int hours, int minutes, double seconds)
        {
            Debug.Assert((hours >= 0 && minutes >= 0 && seconds >= 0.0)
                         || (hours <= 0 && minutes <= 0 && seconds <= 0.0),
                         "Hours/minutes/seconds don't have consistent signs.");

            return hours + minutes / 60.0 + (seconds / 3600.0);
        }

        public static double RadiansToHours(double radians)
        {
            return radians * (12.0 / Math.PI);
        }


        private void SetHmsFromHours(double hour)
        {
            _hours = (int)Truncate(hour);
            hour = (hour - Hours) * 60.0;
            _minutes = (int)Truncate(hour);
            _seconds = (hour - Minutes) * 60.0;
        }

        private double SetHoursFromHms()
        {
            if (Hours < 0 || Minutes < 0 || Seconds < 0.0)
            {
                SetHmsToNegative();
            }

            return Hours + Minutes / 60.0 + (Seconds / 3600.0);
        }

        private void MatchHmsSigns(double value)
        {
            /* If the value is zero, no sign can be inferred */
            if (value > 0.0)
            {
                SetHmsToPositive();
            }
            else if (value < 0.0)
            {
                SetHmsToNegative();
            }
        }

        private void SetHmsToPositive()
        {
            if (Hours < 0)
            {
                _hours *= -1;
            }

            if (Minutes < 0)
            {
                _minutes *= -1;
            }

            if (Seconds < 0.0)
            {
                _seconds *= -1.0;
            }
        }

        private void SetHmsToNegative()
        {
            if (Hours > 0)
            {
                _hours *= -1;
            }

            if (Minutes > 0)
            {
                _minutes *= -1;
            }

            if (Seconds > 0.0)
            {
                _seconds *= -1.0;
            }
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {
            int result;

            if (!(obj is HourAngle))
            {
                result = 1;
            }
            else
            {
                var that = (HourAngle)obj;
                result = Value.CompareTo(that.Value);
            }

            return result;
        }

        #endregion


        public static double Truncate(double value)
        {
            return Math.Truncate(value);
        }


        public static double Range24(double hours)
        {
            hours = hours % 24.0;
            while (hours < 0.0)
            {
                hours = hours + 24.0;
            }
            return hours;
        }
    }
}
