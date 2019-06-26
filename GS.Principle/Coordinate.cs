/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;

namespace GS.Principles
{
    public static class Coordinate
    {
        /// <summary>
        /// Right Ascension to Local Hour Angles
        /// </summary>
        /// <remarks>
        /// The hour angle (HA) of an object is equal to the difference between
        /// the current local sidereal time (LST) and the right ascension of that object.
        /// Adoped from ASCOM
        /// </remarks>
        /// <param name="rightAscension">In decimal hours</param>
        /// <param name="localsiderealtime">In decimal hours</param>
        /// <returns>Local Hour Angles in decimal hours</returns>
        public static double Ra2Ha(double rightAscension, double localsiderealtime)
        {
            var a = localsiderealtime - rightAscension;
            var Ra2Ha = Range.Range12(a);
            return Ra2Ha;
        }

        /// <summary>
        /// Right Ascension and Declination to Altitude and Azimuth
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="rightAscension">In decimal hours</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="localsiderealtime">In decimal hours</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Array of Azmith, Altitude in decimal degrees</returns>
        public static double[] RaDec2AltAz(double rightAscension, double declination, double localsiderealtime, double latitude)
        {
            var a = Ra2Ha(rightAscension, localsiderealtime);
            var RaDec2AltAz = HaDec2AltAz(a, declination, latitude);
            return RaDec2AltAz;
        }

        /// <summary>
        /// Hour Angles and Declination to Altitude and Azimuth
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="hourangle">Local Hour angle</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Array of Azmith, Altitude in decimal degrees</returns>
        public static double[] HaDec2AltAz(double hourangle, double declination, double latitude)
        {
            var a = Units.Hrs2Rad(hourangle);
            var b = Units.Deg2Rad(declination);
            var c = Units.Deg2Rad(latitude);
            var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = f * i - e * g * h;
            var k = -(d * g);
            var l = e * g * i + f * h;
            var m = Math.Sqrt(j * j + k * k);
            var n = Units.Rad2Deg(Math.Atan2(k, j));
            var o = Units.Rad2Deg(Math.Atan2(l, m));
            var p = Range.Range360(n);
            var q = Range.Range90(o);
            var HaDec2AltAz = new[]{q,p};
            return HaDec2AltAz;
        }

        /// <summary>
        /// Hour Angles and Declination to Azimuth
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="hourangle">In Decimal Hours</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Azmith in decimal degrees</returns>
        public static double HaDec2Azm(double hourangle, double declination, double latitude)
        {
            var a = Units.Hrs2Rad(hourangle);
            var b = Units.Deg2Rad(declination);
            var c = Units.Deg2Rad(latitude);
            var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = f * i - e * g * h;
            var k = -(d * g);
            var n = Units.Rad2Deg(Math.Atan2(k, j));
            var HaDec2Azm = Range.Range360(n);
            return HaDec2Azm;
        }

        /// <summary>
        /// Hour Angles and Declination to Altitude
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="hourangle">In Decimal Hours</param>
        /// <param name="declination">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Altitude in decimal degrees</returns>
        public static double HaDec2Alt(double hourangle, double declination, double latitude)
        {
            var a = Units.Hrs2Rad(hourangle);
            var b = Units.Deg2Rad(declination);
            var c = Units.Deg2Rad(latitude);
            //var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = f * h + g * i * e;
            var HaDec2Alt = Units.Rad2Deg(Math.Asin(j));
            return HaDec2Alt;
        }

        /// <summary>
        /// Azimuth and Altitude to Right Ascension and Declination
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <param name="lst">In decimal hours</param>
        /// <returns>Ra in decimal hours, Dec in decimal degrees</returns>
        public static double[] AltAz2RaDec( double altitude, double azimuth, double latitude, double lst)
        {
            var a = AltAz2Ra(altitude, azimuth, latitude, lst);
            var b = AltAz2Dec(altitude, azimuth, latitude);
            var AltAz2RaDec = new[] { a, b };
            return AltAz2RaDec;
        }

        /// <summary>
        /// Azimuth and Altitude to Declination
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <returns>Declination in decimal degrees</returns>
        public static double AltAz2Dec( double altitude, double azimuth, double latitude)
        {
            var a = Units.Deg2Rad(azimuth);
            var b = Units.Deg2Rad(altitude);
            var c = Units.Deg2Rad(latitude);
            //var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = e * i * g + h * f;
            var k = Units.Rad2Deg2(Math.Asin(j));
            var AltAz2Dec = Range.Range90(k);
            return AltAz2Dec;
        }

        /// <summary>
        /// Azimuth and Altitude to Right Ascension
        /// Adoped from ASCOM
        /// </summary>
        /// <param name="altitude">In decimal degrees</param>
        /// <param name="azimuth">In decimal degrees</param>
        /// <param name="latitude">In decimal degrees</param>
        /// <param name="lst">In decimal hours</param>
        /// <returns>Right Ascension in decimal hours</returns>
        public static double AltAz2Ra( double altitude, double azimuth, double latitude, double lst)
        {
            var a = Units.Deg2Rad(azimuth);
            var b = Units.Deg2Rad(altitude);
            var c = Units.Deg2Rad(latitude);
            var d = Math.Sin(a);
            var e = Math.Cos(a);
            var f = Math.Sin(b);
            var g = Math.Cos(b);
            var h = Math.Sin(c);
            var i = Math.Cos(c);
            var j = -d * g;
            var k = -e * h * g + f * i;
            var l = Units.Rad2Hrs(Math.Atan2(j, k));
            var AltAz2Ra = Range.Range24(lst - l);
            return AltAz2Ra;
        }
    }
}
