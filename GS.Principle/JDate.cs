/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Principles
{
    public static class JDate
    {
        private static readonly Util Util = new Util();

        ///// <summary>
        ///// Julian Day to Greenwich Calendar Date (UTC) Day
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="jd">2456474.4423611108</param>
        ///// <returns>30.942361110821366</returns>
        //public static double Jd2CDay(double jd)
        //{
        //    var a = Math.Floor(jd + 0.5);
        //    var b = jd + 0.5 - a;
        //    var c = Math.Floor((a - 1867216.25) / 36524.25);
        //    var d = a > 2299160 ? a + 1 + c - Math.Floor(c / 4) : a;
        //    var e = d + 1524;
        //    var f = Math.Floor((e - 122.1) / 365.25);
        //    var g = Math.Floor(365.25 * f);
        //    var h = Math.Floor((e - g) / 30.6001);
        //    var jd2CDay = e - g + b - Math.Floor(30.6001 * h);
        //    return jd2CDay;
        //}

        ///// <summary>
        ///// Julian Month to Greenwich Calendar Month (UTC)
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="jd">2456474.4423611108</param>
        ///// <returns>6</returns>
        //public static int Jd2CMonth(double jd)
        //{
        //    var i = Math.Floor(jd + 0.5);
        //    var a = Math.Floor((i - 1867216.25) / 36524.25);
        //    var b = i > 2299160 ? i + 1 + a - Math.Floor(a / 4) : i;
        //    var c = b + 1524;
        //    var d = Math.Floor((c - 122.1) / 365.25);
        //    var e = Math.Floor(365.25 * d);
        //    var g = Math.Floor((c - e) / 30.6001);
        //    var jd2CMonth = g < 13.5 ? g - 1 : g - 13;
        //    return Convert.ToInt32(jd2CMonth);
        //}

        ///// <summary>
        ///// Julian Year to Greenwich Calendar Year (UTC)
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="jd">2456474.4423611108</param>
        ///// <returns>2013</returns>
        //public static int Jd2CYear(double jd)
        //{
        //    var a = Math.Floor(jd + 0.5);
        //    var b = Math.Floor((a - 1867216.25) / 36524.25);
        //    var c = a > 2299160 ? a + 1 + b - Math.Floor(b / 4) : a;
        //    var d = c + 1524;
        //    var e = Math.Floor((d - 122.1) / 365.25);
        //    var f = Math.Floor(365.25 * e);
        //    var g = Math.Floor((d - f) / 30.6001);
        //    var h = g < 13.5 ? g - 1 : g - 13;
        //    var jd2CYear = h > 2.5 ? e - 4716 : e - 4715;
        //    return Convert.ToInt32(jd2CYear);
        //}

        ///// <summary>
        ///// Greenwich Calendar Date (UTC) to Julian Date
        ///// Count of days and fractions since noon Universal Time on 1 January 4713 BC (on the Julian calendar)
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="day">19.75</param>
        ///// <param name="month">6</param>
        ///// <param name="year">2009</param>
        ///// <returns>2455002.25</returns>
        //public static double Utc2Jd(double day, int month, int year)
        //{
        //    double y;
        //    double m;
        //    if (month < 3)
        //    {
        //        y = year - 1;
        //        m = month + 12;
        //    }
        //    else
        //    {
        //        y = year;
        //        m = month;
        //    }
        //    double //a;
        //    double b;
        //    if (year > 1582)
        //    {
        //        a = Math.Floor(y / 100);
        //        b = 2 - a + Math.Floor(a / 4);
        //    }
        //    else
        //    {
        //        if (Math.Abs(year - 1582) < 0.0 && month > 10)
        //        {
        //            a = Math.Floor(y / 100);
        //            b = 2 - a + Math.Floor(a / 4);
        //        }
        //        else
        //        {
        //            if (Math.Abs(year - 1582) < 0.0 && Math.Abs(month - 10) < 0.0 && day >= 15)
        //            {
        //                a = Math.Floor(y / 100);
        //                b = 2 - a + Math.Floor(a / 4);
        //            }
        //            else
        //            {
        //                b = 0;
        //            }
        //        }
        //    }
        //    var c = y < 0 ? Math.Floor((365.25 * y) - 0.75) : Math.Floor(365.25 * y);
        //    var d = Math.Floor(30.6001 * (m + 1));
        //    var utc2Jd = b + c + d + day + 1720994.5;
        //    return utc2Jd;
        //}

        /// <summary>
        /// OLE Automation Date to Julian Date
        /// For modern dates only
        /// </summary>
        /// <param name="date">DateTime</param>
        /// <returns>2455002.25</returns>
        public static double Ole2Jd(DateTime date)
        {
            var ole2Jd = date.ToOADate() + 2415018.5;
            return ole2Jd;
        }

        ///// <summary>
        ///// Individual Units to Julian Date
        ///// Adapted from Jean Meeus' "Astronomical Algorithms"
        ///// </summary>
        ///// <param name="year">2009</param>
        ///// <param name="month">5</param>
        ///// <param name="day">19</param>
        ///// <param name="hour">18</param>
        ///// <param name="minute">0</param>
        ///// <param name="second">0</param>
        ///// <param name="millisecond">0</param>
        ///// <returns>2455002.25</returns>
        //public static double Utc2Jd1(int year, int month, int day, int hour, int minute, int second, int millisecond)
        //{
        //    var a = IsJd(year, month, day);
        //    var b = month > 2 ? month : month + 12;
        //    var c = month > 2 ? year : year - 1;
        //    var d = day + hour / 24.0 + minute / 1440.0 + (second + millisecond / 1000.0) / 86400.0;
        //    var e = a ? 0 : 2 - c / 100 + c / 100 / 4;
        //    var utc2Jd1 = (int)(365.25 * (c + 4716)) + (int)(30.6001 * (b + 1)) + d + e - 1524.5;
        //    return utc2Jd1;
        //}

        /// <summary>
        /// Julian Date conversion with AsCom Utilities
        /// </summary>
        /// <param name="date">2009, 6, 19, 18, 0, 0</param>
        /// <returns>2455002.25</returns>
        public static double Utc2Jd2(DateTime date)
        {
            var utc2Jd2 = Util.DateUTCToJulian(date);
            return utc2Jd2;
        }

        ///// <summary>
        ///// Validate a Julian Date
        ///// </summary>
        ///// <param name="year">2009</param>
        ///// <param name="month">6</param>
        ///// <param name="day">19</param>
        ///// <returns>True</returns>
        //public static bool IsJd(int year, int month, int day)
        //{
        //    // All dates prior to 1582 are in the Julian calendar
        //    if (year < 1582) return true;
        //    // All dates after 1582 are in the Gregorian calendar
        //    if (year > 1582) return false;
        //    // If 1582, check before October 4 (Julian) or after October 15 (Gregorian)
        //    if (month < 10) return true;
        //    if (month > 10) return false;
        //    if (day < 5) return true;
        //    if (day > 14) return false;
        //    throw new ArgumentOutOfRangeException();
        //}

        ///// <summary>
        ///// Julian date to Greenwich Calendar Date (UTC)
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="jd">2455002.25</param>
        ///// <returns>2456306</returns>
        //public static double Jd2Utc(double jd)
        //{
        //    var a = jd;
        //    var b = Math.Floor(a + 0.5);
        //    //var c = a + 0.5 - b; //uncomment for hours
        //    var d = Math.Floor((b - 1867216.25) / 36524.25);
        //    var e = b > 2299160 ? b + 1 + d - Math.Floor(d / 4) : b;
        //    var f = e + 1524;
        //    var g = Math.Floor((f - 122.1) / 365.25);
        //    var jd2Utc = Math.Floor(365.25 * g);
        //    //var i = Math.Floor((f - h) / 30.6001); //uncomment for month
        //    //var j = f - h + c - Math.Floor(30.6001 * i); //uncomment for day
        //    //var k = i < 13.5 ? i - 1 : i - 13; //uncomment for month
        //    //var l = k > 2.5 ? g - 4716 : g - 4715; //uncomment for year
        //    return jd2Utc;
        //}

        ///// <summary>
        ///// Days since the epoch (UTC)
        ///// Adapted from Peter Duffett-Smith, Practical Astronomy with your Calculator
        ///// </summary>
        ///// <param name="eDate">2010, 6, 19, 18, 0, 0</param>
        ///// <param name="date">1999, 6, 19, 18, 0, 0</param>
        ///// <returns>-4018</returns>
        //public static double EpochDays(DateTime eDate, DateTime date)
        //{
        //    var a = Utc2Jd2(eDate);
        //    var b = Utc2Jd2(date);
        //    var epochDays = b - a;
        //    return epochDays;
        //}

        /// <summary>
        /// Days since epoch 2000
        /// </summary>
        /// <returns></returns>
        public static double Epoch2000Days()
        {
            return 2451545.0;              //J2000 1 Jan 2000, 12h 0m 0s
        }
    }
}
