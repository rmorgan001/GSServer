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
namespace NStarAlignment.Utilities
{
    /// <summary>
    /// Forces parameters to be within a certain range 
    /// </summary>
    /// <remarks>Attention to the order of parameters (AltAz vs AzAlt) in the method names</remarks>
    public static class Range
    {
        /// <summary>
        /// Returns double in the range -12 to +12
        /// </summary>
        /// <param name="d">90.169444444444451</param>
        /// <returns>-5.8305555555555486</returns>
        public static double Range12(double d)
        {
            while ((d > 12.0) || (d <= -12.0))
            {
                if (d <= -12.0) d += 24.0;
                if (d > 12.0) d -= 24.0;
            }
            return d;
        }

        /// <summary>
        /// Returns double in the range 0 to 24.0
        /// </summary>
        /// <param name="d">90.169444444444451</param>
        /// <returns>18.169444444444451</returns>
        public static double Range24(double d)
        {
            while ((d >= 24.0) || (d < 0.0))
            {
                if (d < 0.0) d += 24.0;
                if (d >= 24.0) d -= 24.0;
            }
            return d;
        }

        /// <summary>
        /// Returns double in the range -90 to 90
        /// </summary>
        /// <param name="d">90.169444444444451</param>
        /// <returns>89.830555555555549</returns>
        public static double Range90(double d)
        {
            while ((d > 90.0) || (d < -90.0))
            {
                if (d < -90.0) d += 180.0;
                if (d > 90.0) d = 180.0 - d;
            }
            return d;
        }

        /// <summary>
        /// Returns double in the range 0 to 360
        /// </summary>
        /// <param name="d">590.169444444444451</param>
        /// <returns>230.16944444444448</returns>
        public static double Range360(double d)
        {
            while ((d >= 360.0) || (d < 0.0))
            {
                if (d < 0.0) d += 360.0;
                if (d >= 360.0) d -= 360.0;
            }
            return d;
        }

        /// <summary>
        /// Returns double in the range -180 to 180
        /// </summary>
        /// <param name="d">590.169444444444451</param>
        /// <returns>230.16944444444448</returns>
        public static double RangePlusOrMinus180(double d)
        {
            while (d <= 180.0)
            {
                d = d + 360.0;
            }

            while (d > 180)
            {
                d = d - 360.0;
            }
            return d;
        }

    }
}
