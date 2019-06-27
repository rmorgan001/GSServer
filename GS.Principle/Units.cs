/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Principles
{
    public static class Units
    {
        /// <summary>
        /// DEC coordinate to double
        /// </summary>
        /// <param name="degrees">90</param>
        /// <param name="minutes">10</param>
        /// <param name="seconds">10</param>
        /// <returns>90.169444444444451</returns>
        public static double Deg2Dou(double degrees, double minutes, double seconds)
        {
            var a = (degrees < 0 ? -1 : 1);
            var b = Math.Abs(degrees);
            var c = b + (minutes / 60) + (seconds / 3600);
            var d = c * a;
            return d;
        }

        /// <summary>
        /// RA coordinate to double
        /// </summary>
        /// <param name="hour">90</param>
        /// <param name="minutes">10</param>
        /// <param name="seconds">10</param>
        /// <returns>90.169444444444451</returns>
        public static double Ra2Dou(double hour, double minutes, double seconds)
        {
            var a = (hour / 1) + (minutes / 60.0) + (seconds / 3600);
            return a;
        }

        /// <summary>
        /// Double to Dec coordinate string
        /// </summary>
        /// <param name="coord">90.169444444444451</param>
        /// <returns>"90:10:10"</returns>
        public static string Dou2Deg(double coord)
        {
            var a = (int)Math.Round(coord * 3600);
            var b = a / 3600;
            a = Math.Abs(a % 3600);
            var c = a / 60;
            a %= 60;
            var d = $"{b}:{c}:{a}";
            return d;
        }

        /// <summary>
        /// Radians to Degrees
        /// </summary>
        /// <param name="radians">90.169444444444451</param>
        /// <returns>5166.3286077060147</returns>
        public static double Rad2Deg(double radians)
        {
            const double a = 360.0 / (2 * Math.PI);
            var b = radians * a;
            return b;
        }

        /// <summary>
        /// Radians to Degrees
        /// </summary>
        /// <param name="radians">90.169444444444451</param>
        /// <returns>5166.3286077060147</returns>
        public static double Rad2Deg1(double radians)
        {
            const double a = 180.0 / Math.PI;
            var b = radians * a;
            return b;
        }

        /// <summary>
        /// Radians to Degrees
        /// </summary>
        /// <param name="radians">90.169444444444451</param>
        /// <returns>5166.3286077060147</returns>
        public static double Rad2Deg2(double radians)
        {
            const double a = 57.295779513082320876798154814105;
            var b = radians * a;
            return b;
        }

        /// <summary>
        /// Degrees to radians
        /// </summary>
        /// <param name="degrees">90.169444444444451</param>
        /// <returns>1.5737536902496649</returns>
        public static double Deg2Rad(double degrees)
        {
            const double a = 2 * Math.PI / 360.0;
            var b = degrees * a;
            return b;
        }

        /// <summary>
        /// Degrees to radians
        /// </summary>
        /// <param name="degrees">90.169444444444451</param>
        /// <returns>1.5737536902496649</returns>
        public static double Deg2Rad1(double degrees)
        {
            const double a = Math.PI / 180.0;
            var b = degrees * a;
            return b;
        }

        /// <summary>
        /// Degrees to radians
        /// </summary>
        /// <param name="degrees">90.169444444444451</param>
        /// <returns>1.5737536902496649</returns>
        public static double Deg2Rad2(double degrees)
        {
            var b = degrees * 0.01745329251994329576923690768489;
            return b;
        }

        /// <summary>
        /// Datetime total hours to degrees
        /// </summary>
        /// <param name="dateTime">{1/1/2000 12:00:00 AM}</param>
        /// <returns>0</returns>
        public static double Date2Deg(DateTime dateTime)
        {
            var a = dateTime.TimeOfDay.TotalHours * 360 / 24;
            return a;
        }

        /// <summary>
        /// Hour angles to degrees
        /// </summary>
        /// <param name="hours">23</param>
        /// <returns>345</returns>
        public static double Hrs2Deg(double hours)
        {
            var a = hours * 15;
            return a;
        }

        /// <summary>
        /// Degrees to Hours
        /// </summary>
        /// <param name="degrees">345</param>
        /// <returns>23</returns>
        public static double Deg2Hrs(double degrees)
        {
            var a = degrees / 15;
            return a;
        }

        /// <summary>
        /// Hour angles to radians
        /// </summary>
        /// <param name="hours">23</param>
        /// <returns>6.0213859193804362</returns>
        public static double Hrs2Rad(double hours)
        {
            var a = hours * 0.2617993877991494;
            return a;
        }

        /// <summary>
        /// Radians to hour angles
        /// </summary>
        /// <param name="radians">6.021385919380436</param>
        /// <returns>23</returns>
        public static double Rad2Hrs(double radians)
        {
            var a = radians * 3.8197186342054885;
            return a;
        }
    }
}
