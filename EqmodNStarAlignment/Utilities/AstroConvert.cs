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
   Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EqmodNStarAlignment.Utilities
{

    public static class AstroConvert
    {
        /// <summary>
        /// Radians per degree
        /// </summary>
        private const double DegRad = 0.0174532925;           // Radians per degree
        /// <summary>
        /// Degrees per radian
        /// </summary>
        private const double RadDeg = 57.2957795;             // Degrees per Radian

        /// <summary>
        /// Radians per hour
        /// </summary>
        private const double HrsRad = 0.2617993881;           // Radians per hour

        #region Unit convertsions ...

        public static double DegToRad(double degrees) { return (degrees * DegRad); }
        public static double HrsToRad(double hours) { return (hours * HrsRad); }

        public static double RadToHrs(double radians) { return (radians / HrsRad); }

        public static double RadToDeg(double rad) { return (rad * RadDeg); }

        #endregion

        #region Astro32.dll functions ...
        /*
         * The methods in this region are created using the code that I believe was originally used
         * to create the astro32.dll that was shipped with EQMOD.
         * Source: http://mmto.org/~dclark/Reports/MountDoxygen/html/aa__hadec_8c_source.html
         */

        static double lastLatitide;
        static double sinLatitude = 0.0;
        static double cosLatitude = 0.0;


        /* given geographical latitude (n+, radians), lt, altitude (up+, radians),
           * alt, and azimuth (angle round to the east from north+, radians),
           * return hour angle (radians), ha, and declination (radians), dec.
           * Originally called aa_hadec
           */

        public static double[] GetHaDec(double lt, double alt, double az)
        {
            double ha = 0d, dec = 0d;
            aaha_aux(lt, az, alt, ref ha, ref dec);
            if (ha > Math.PI)
                ha -= 2 * Math.PI;
            return new double[] { ha, dec };
        }

        /* given geographical (n+, radians), lt, hour angle (radians), ha, and
         * declination (radians), dec, return altitude (up+, radians), alt, and
         * azimuth (angle round to the east from north+, radians),
         * Originally caled hadec_aa
         */
        public static double[] GetAltAz(double lt, double ha, double dec)
        {
            double alt = 0d, az = 0d;
            aaha_aux(lt, ha, dec, ref az, ref alt);
            return new double[] { alt, az };
        }

        static void aaha_aux(double latitude, double x, double y, ref double p, ref double q)
        {
            lastLatitide = double.MinValue;
            double cap = 0.0;
            double B = 0.0;

            if (latitude != lastLatitide)
            {
                sinLatitude = Math.Sin(latitude);
                cosLatitude = Math.Cos(latitude);
                lastLatitide = latitude;
            }

            solve_sphere(-x, Math.PI / 2 - y, sinLatitude, cosLatitude, ref cap, ref B);
            p = B;
            q = Math.PI / 2 - Math.Acos(cap);
        }

        /* solve a spherical triangle:
         *           A
         *          /  \
         *         /    \
         *      c /      \ b
         *       /        \
         *      /          \
         *    B ____________ C
         *           a
         *
         * given A, b, c find B and a in range 0..B..2PI and 0..a..PI, respectively..
         * cap and Bp may be NULL if not interested in either one.
         * N.B. we pass in cos(c) and sin(c) because in many problems one of the sides
         *   remains constant for many values of A and b.
         */
        static void solve_sphere(double A, double b, double cc, double sc, ref double cap, ref double Bp)
        {
            double cb = Math.Cos(b), sb = Math.Sin(b);
            double sA, cA = Math.Cos(A);
            double x, y;
            double ca;
            double B;

            ca = cb * cc + sb * sc * cA;
            if (ca > 1.0)
            {
                ca = 1.0;
            }
            if (ca < -1.0)
            {
                ca = -1.0;
            }
            cap = ca;

            if (sc < 1e-7)
            {
                B = cc < 0 ? A : Math.PI - A;
            }
            else
            {
                sA = Math.Sin(A);
                y = sA * sb * sc;
                x = cb - ca * cc;
                B = Math.Atan2(y, x);
            }

            Bp = Range.ZeroToValue(B, Math.PI * 2);
        }
        #endregion

    }
}
