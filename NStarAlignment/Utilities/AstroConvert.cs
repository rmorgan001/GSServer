/*
MIT License

Copyright (c) 2017 Phil Crompton

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

namespace NStarAlignment.Utilities
{

    public class AstroConvert
    {
        /// <summary>
        /// Radians per degree
        /// </summary>
        private const double DEG_RAD = 0.0174532925;           // Radians per degree
        /// <summary>
        /// Degrees per radian
        /// </summary>
        private const double RAD_DEG = 57.2957795;             // Degrees per Radian

        /// <summary>
        /// Radians per hour
        /// </summary>
        private const double HRS_RAD = 0.2617993881;           // Radians per hour

        #region Unit convertsions ...

        public static double DegToRad(double degrees) { return (degrees * DEG_RAD); }
        public static double HrsToRad(double hours) { return (hours * HRS_RAD); }

        public static double RadToHrs(double radians) { return (radians / HRS_RAD); }

        public static double RadToDeg(double rad) { return (rad * RAD_DEG); }

        #endregion

        /// <summary>
        /// Right Ascension to Local 12 Hour Angles
        /// </summary>
        /// <remarks>
        /// The hour angle (HA) of an object is equal to the difference between
        /// the current local sidereal time (LST) and the right ascension of that object.
        /// Adopted from ASCOM
        /// </remarks>
        /// <param name="rightAscension">In decimal hours</param>
        /// <param name="localSiderealTime">In decimal hours</param>
        /// <returns>Local Hour Angles in decimal hours</returns>
        public static double Ra2Ha12(double rightAscension, double localSiderealTime)
        {
            var a = localSiderealTime - rightAscension;
            var ra2Ha = Range.Range12(a);
            return ra2Ha;
        }


        /// <summary>
        /// Right Ascension and Declination to Altitude and Azimuth
        /// Adopted from ASCOM
        /// </summary>
        /// <param name="rightAscension">In decimal hours</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="localSiderealTime">In decimal hours</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Array of Azimuth, Altitude in decimal degrees</returns>
        public static double[] RaDec2AltAz(double rightAscension, double declination, double localSiderealTime, double latitude)
        {
            var a = Ra2Ha12(rightAscension, localSiderealTime);
            var raDec2AltAz = HaDec2AltAz(a, declination, latitude);
            return raDec2AltAz;
        }

        /// <summary>
        /// Hour Angles and Declination to Altitude and Azimuth
        /// Adopted from ASCOM
        /// </summary>
        /// <param name="hourAngle">Local Hour angle</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Array of Azimuth, Altitude in decimal degrees</returns>
        public static double[] HaDec2AltAz(double hourAngle, double declination, double latitude)
        {
            double a = HrsToRad(hourAngle);
            double b = DegToRad(declination);
            double c = DegToRad(latitude);
            double d = Math.Sin(a);
            double e = Math.Cos(a);
            double f = Math.Sin(b);
            double g = Math.Cos(b);
            double h = Math.Sin(c);
            double i = Math.Cos(c);
            double j = f * i - e * g * h;
            double k = -(d * g);
            double l = e * g * i + f * h;
            double m = Math.Sqrt(j * j + k * k);
            double n = RadToDeg(Math.Atan2(k, j));
            double o = RadToDeg(Math.Atan2(l, m));
            double p = Range.Range360(n);
            double q = Range.Range90(o);
            double[] altAz = { q, p };
            return altAz;
        }

        /// <summary>
        /// Azimuth and Altitude to Right Ascension and Declination
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <param name="lst">In decimal hours</param>
        /// <returns>Ra in decimal hours, Dec in decimal degrees</returns>
        public static double[] AltAz2RaDec(double altitude, double azimuth, double latitude, double lst)
        {
            var a = AltAz2Ra(altitude, azimuth, latitude, lst);
            var b = AltAz2Dec(altitude, azimuth, latitude);
            var altAz2RaDec = new[] { a, b };
            return altAz2RaDec;
        }

        /// <summary>
        /// Azimuth and Altitude to Declination
        /// Adopted from ASCOM
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Declination in decimal degrees</returns>
        public static double AltAz2Dec(double altitude, double azimuth, double latitude)
        {
            var a = DegToRad(azimuth);
            var b = DegToRad(altitude);
            var c = DegToRad(latitude);
            //var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = e * i * g + h * f;
            var k = RadToDeg(Math.Asin(j));
            var altAz2Dec = Range.Range90(k);
            return altAz2Dec;
        }

        /// <summary>
        /// Azimuth and Altitude to Right Ascension
        /// Adopted from ASCOM
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <param name="lst">In decimal hours</param>
        /// <returns>Right Ascension in decimal hours</returns>
        public static double AltAz2Ra(double altitude, double azimuth, double latitude, double lst)
        {
            var a = DegToRad(azimuth);
            var b = DegToRad(altitude);
            var c = DegToRad(latitude);
            var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = -d * g;
            var k = -e * h * g + f * i;
            var l = RadToHrs(Math.Atan2(j, k));
            var altAz2Ra = Range.Range24(lst - l);
            return altAz2Ra;
        }

    }
}
