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
using ASCOM.Utilities;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GS.Principles
{
    public class Time : IDisposable
    {
        private static readonly Util util = new Util();

        /// <summary>
        /// Decimal hours to Timespan
        /// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        /// </summary>
        /// <param name="dh">18.524166667</param>
        /// <returns>0, 18, 31, 27, 0</returns>
        public static TimeSpan Dhs2HMS(double dh)
        {
            var a = Math.Abs(dh);
            var b = a * 3600;
            var c = Math.Round((b % 60), 2);
            var d = Math.Abs(c - 60.0) < 0.0 ? 0 : c; //seconds
            var e = TimeSpan.FromSeconds(d); //milliseconds
            var f = e.Milliseconds;
            var g = Math.Abs(c - 60.0) < 0.0 ? b + 60 : b;
            var h = Math.Floor(g / 60) % 60; //minutes
            var i = Math.Floor(g / 3600);
            var j = dh < 0 ? -1 * i : i; //hours
            var Dhs2HMS = new TimeSpan(0, (int)j, (int)h, e.Seconds, f);
            return Dhs2HMS;
        }

        /// <summary>
        /// Timespan to Decimal Hours
        /// Microsoft convertion of timespan
        /// </summary>
        /// <param name="ts">0, 18, 31, 27, 0</param>
        /// <returns>18.524166666666666</returns>
        public static double Ts2Dhrs(TimeSpan ts)
        {
            var Ts2Dhrs = Convert.ToDouble(ts.TotalHours);
            return Ts2Dhrs;
        }

        /// <summary>
        /// Sexagesimal hours to Decimal Hours 
        /// Microsoft convertion of timespan
        /// </summary>
        /// <param name="s">"18:31:27"</param>
        /// <returns>18.524166666666666</returns>
        public static double Sx2Dhrs(string s)
        {
            var Sx2Dhrs = util.HMSToHours(s);
            return Sx2Dhrs;
        }

        /// <summary>
        /// Hours, Minutes, Seconds, and Milliseconds to Decimal Hours
        /// </summary>
        /// <param name="hours">18</param>
        /// <param name="minutes">31</param>
        /// <param name="seconds">27</param>
        /// <param name="milliseconds">0</param>
        /// <returns>18.524230555555555</returns>
        public static double HMS2Dhrs(int hours, int minutes, int seconds, int milliseconds)
        {
            var a = (Math.Abs(seconds * 1000 + milliseconds) / 1000.0 / 60);
            var b = (Math.Abs(minutes) + a) / 60;
            var c = Math.Abs(hours) + b;
            var HMS2Dhrs = hours < 0 || minutes < 0 || seconds < 0 || milliseconds < 0 ? -c : c;
            return HMS2Dhrs;
        }

        /// <summary>
        /// Hours from Decimal Hours
        /// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator 
        /// </summary>
        /// <param name="dh">18.524166667</param>
        /// <returns>18</returns>
        public static int Dhrs2Hrs(double dh)
        {
            var a = Math.Abs(dh);
            var b = a * 3600;
            var c = Math.Round(b - 60 * Math.Floor(b / 60), 2);
            var d = Math.Abs(c - 60) < 0.0 ? b + 60 : b;
            var Dhrs2Hrs = dh < 0 ? -Math.Floor(d / 3600) : Math.Floor(d / 3600);
            return Convert.ToInt32(Dhrs2Hrs);
        }

        /// <summary>
        /// Minutes from Decimal Hours
        /// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator 
        /// </summary>
        /// <param name="dh">18.524166667</param>
        /// <returns>31</returns>
        public static int Dhrs2Mins(double dh)
        {
            var a = Math.Abs(dh);
            var b = a * 3600;
            var c = Math.Round(b - 60 * Math.Floor(b / 60), 2);
            var d = Math.Abs(c - 60) < 0.0 ? b + 60 : b;
            var Dhrs2Mins = Math.Floor(d / 60) % 60;
            return Convert.ToInt32(Dhrs2Mins);
        }

        /// <summary>
        /// Seconds from Decimal Hours
        /// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator 
        /// </summary>
        /// <param name="dh">18.52420139</param>
        /// <returns>27.125004</returns>
        public static double Dhrs2Secs(double dh)
        {
            var a = Math.Abs(dh);
            var b = a * 3600;
            var c = Math.Round(b - 60 * Math.Floor(b / 60), 6);
            var Dhrs2Secs = Math.Abs(c - 60) < 0.0 ? 0 : c;
            return Dhrs2Secs;
        }

        /// <summary>
        /// calculating the percentage of X from Y using timespans
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static double Divide(TimeSpan dividend, TimeSpan divisor)
        {
            return dividend.Ticks / (double)divisor.Ticks;
        }

        /// <summary>
        /// Greenwich Mean Time to Local Sidreal Time
        /// </summary>
        /// <param name="ts">4, 40, 5, 230</param>
        /// <param name="longitude">-64</param>
        /// <returns>0.40145277777777721</returns>
        public static double Gmt2Lst(TimeSpan ts, double longitude)
        {
            var a = Ts2Dhrs(ts);
            var b = longitude / 15.0;
            var c = a + b;
            var Gmt2Lst = c - 24.0 * Math.Floor(c / 24);
            return Gmt2Lst;
        }

        /// <summary>
        /// Local Sidereal Time
        /// Adopted from the ASCOM .net telescope simulator
        /// </summary>
        /// <param name="ejd">2000, 1, 1, 12, 0, 0</param>
        /// <param name="jd">2009, 6, 19, 4, 40, 5, 230</param>
        /// <param name="nutation">true</param>
        /// <param name="longitude">81</param>
        /// <returns>3.9042962940932857</returns>
        public static double Lst(double ejd, double jd, bool nutation, double longitude)
        {
            var a = jd - ejd;                               // Days since epoch
            var b = a / 36525;                              // Century to days for the epoch
            var c = 280.46061837 + 360.98564736629 * a;     // Greenwich Mean Sidereal Time (GMST)
            var d = c + longitude;                          // Local Mean Sidereal Time (LMST)
            if (d < 0.0)
            {
                while (d < 0.0)
                {
                    d += 360.0;
                }
            }
            else
            {
                while (d > 360.0) d -= 360.0;
            }
            if (nutation)
            {
                //calculate OM the longitude when the Moon passes through the plane of the ecliptic
                var e = 125.04452 - 1934.136261 * b;
                if (e < 0.0)
                {
                    while (e < 0.0) e += 360.0;
                }
                else
                {
                    while (e > 360.0) e -= 360.0;
                }
                //calculat L mean longitude of the Sun
                var f = 280.4665 + 36000.7698 * b;
                if (f < 0.0)
                {
                    while (f < 0.0) f += 360.0;
                }
                else
                {
                    while (f > 360.0) f -= 360.0;
                }
                //calculate L1 mean longitude of the Moon
                var g = 218.3165 + 481267.8813 * b;
                if (g < 0.0)
                {
                    while (g < 0) g += 360.0;
                }
                else
                {
                    while (g > 360.0) g -= 360.0;
                }
                //calculate e Obliquity of the Ecliptic
                var h = 23.439 - 0.0000004 * b;
                if (h < 0.0)
                {
                    while (h < 0.0) h += 360.0;
                }
                else
                {
                    while (h > 360.0) h -= 360.0;
                }
                var i = (-17.2 * Math.Sin(e)) - (1.32 * Math.Sin(2 * f)) - (0.23 * Math.Sin(2 * g)) + (0.21 * Math.Sin(2 * e));
                var j = (i * Math.Cos(h)) / 3600;               // Nutation correction for true values
                d += j;                                      // True Local Sidereal Time (LST)
            }
            var m = d * 24.0 / 360.0;
            var Lst = Range.Range24(m);
            return Lst;
        }

        /// <summary>
        /// Local Sidereal Time
        /// Adapted from Jean Meeus' "Astronomical Algorithms"
        /// Formula can be simplified by leaving out the terms with b in them. The error is less than 0.1 seconds this century (i.e. 2100)
        /// </summary>
        /// <param name="ejd">2000, 1, 1, 12, 0, 0</param>
        /// <param name="jd">2009, 6, 19, 4, 40, 5, 230</param>
        /// <param name="longitude">81</param>
        /// <returns>3.9042832246341277</returns>
        public static double Lst1(double ejd, double jd, double longitude)
        {
            var a = jd - ejd;                                           // Days since epoch
            var b = a / 36525;                                          // Century to days for the epoch
            var c = 280.46061837 + 360.98564736629 * a;                 // Greenwich Mean Sidereal Time (GMST)
            var d = c + 0.000387933 * b * b - b * b * b / 38710000.0;   // Can be left out  
            var e = d + longitude;                                      // Local Mean Sidereal Time (LMST)
            var f = e * 24.0 / 360.0;
            var Lst1 = Range.Range24(f);
            return Lst1;
        }

        /// <summary>
        /// Local Sidereal Time
        /// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        /// </summary>
        /// <param name="ejd">2000, 1, 1, 12, 0, 0</param>
        /// <param name="utc">2009, 6, 19, 4, 40, 5, 230</param>
        /// <param name="longitude">81</param>
        /// <returns>3.9042831390200021</returns>
        public static double Lst2(double ejd, DateTime utc, double longitude)
        {
            var a = util.DateUTCToJulian(new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, 0));
            var b = a - ejd;                                                       // Days since epoch
            var c = b / 36525;                                                      // Century to days for the epoch
            var d = 6.697374558 + (2400.051336 * c) + (0.000025862 * c * c);
            var e = d - (24 * (int)(d / 24));
            var f = Ts2Dhrs(new TimeSpan(0, utc.Hour, utc.Minute, utc.Second, utc.Millisecond));
            var g = f * 1.002737909;
            var h = e + g;
            var i = h - (24 * (int)(h / 24));                                       // Greenwich Sidereal Time (GST)

            var j = longitude / 15;
            var k = i + j;
            var l = k - (24 * (int)(k / 24));                                       // Local Sidereal Time
            var Lst2 = Range.Range24(l);
            return Lst2;
        }

        /// <summary>
        /// Convert unix timestamp to datetime
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Creates a delayed timer using the mediatimer
        /// </summary>
        /// <param name="ms">Milliseconds</param>
        public static void TimerDelay(int ms)
        {
            var i = 0;
            var mediaTimer = new MediaTimer { Mode = TimerMode.OneShot, Period = ms };
            mediaTimer.Tick += (s, args) => i = 1;
            mediaTimer.Start();
            while (i == 0) { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)util)?.Dispose();
            }
            // free native resources if there are any.
        }

        /// <summary>
        /// Change System Time,  expects time in UTC
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string SetSystemUTCTime(DateTime dt)
        {
            var systime = new SYSTEMTIME(dt);
            var ret = NativeMethods.SetLocalTime(ref systime);
            if (ret) return string.Empty;
            var error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            return error;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public readonly ushort Year;
            public readonly ushort Month;
            public readonly ushort DayOfWeek;
            public readonly ushort Day;
            public readonly ushort Hour;
            public readonly ushort Minute;
            public readonly ushort Second;
            public readonly ushort Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                Year = (ushort)dt.Year;
                Month = (ushort)dt.Month;
                DayOfWeek = (ushort)dt.DayOfWeek;
                Day = (ushort)dt.Day;
                Hour = (ushort)dt.Hour;
                Minute = (ushort)dt.Minute;
                Second = (ushort)dt.Second;
                Milliseconds = (ushort)dt.Millisecond;
            }
        }
    }
}
