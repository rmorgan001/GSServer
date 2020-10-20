/* Copyright(C) 2020  Rob Morgan (robert.morgan.e@gmail.com)

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
    public static class Calculations
    {
        /// <summary>
        /// Compute the angular distance between two coordinates
        /// </summary>
        /// <param name="ra1">hours decimal</param>
        /// <param name="dec1">degrees decimal</param>
        /// <param name="ra2">hours decimal</param>
        /// <param name="dec2">degrees decimal</param>
        /// <returns>degrees</returns>
        public static double AngularDistance(double ra1, double dec1, double ra2, double dec2)
        {
            var a = Math.Sin(Units.Deg2Rad1(dec1));
            var b = Math.Sin(Units.Deg2Rad1(dec2));
            var c = Math.Cos(Units.Deg2Rad1(dec1));
            var d = Math.Cos(Units.Deg2Rad1(dec2));
            var e = (ra1 - ra2) * 15;
            var f = Math.Cos(Units.Deg2Rad1(e));
            var g = Math.Acos(a * b +  c * d * f);
            var h = Units.Rad2Deg1(g);
            return h;
        }

    }
}
