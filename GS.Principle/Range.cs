/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Principles
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
        /// Returns double in the range -180 to 180
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double Range180(double d)
        {
            while (d <= -180.0 || d > 180.0)
            {
                if (d <= -180.0) d += 360;
                if (d > 180) d -= 360;
            }
            return d;
        }

        /// <summary>
        /// Returns double in the range -90 to 0 to 90 to 180 to 270.
        /// </summary>
        /// <param name="d">290.169444444444451</param>
        /// <returns>-69.830555555555577</returns>
        public static double Range270(double d)
        {
            while ((d >= 270) || (d < -90))
            {
                if (d < -90) d += 360.0;
                if (d >= 270) d -= 360.0;
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
        /// Force range for Altitude and Azimuth
        /// </summary>
        ///  <remarks>Attention to the order given and received</remarks>
        /// <param name="altAz"></param>
        /// <returns></returns>
        public static double[] RangeAltAz(double[] altAz)
        {
            double[] a = { Range90(altAz[0]), Range360(altAz[1]) };
            return a;
        }

        /// <summary>
        /// Force range for Azimuth an Altitude
        /// </summary>
        /// <remarks>Attention to the order given and received</remarks>
        /// <param name="azAlt"></param>
        /// <returns></returns>
        public static double[] RangeAzAlt(double[] azAlt)
        {
            double[] a = { Range360(azAlt[0]), Range90(azAlt[1]) };
            return a;
        }

        /// <summary>
        /// Force range for Right Ascension and Declination
        /// </summary>
        /// <param name="raDec"></param>
        /// <returns></returns>
        public static double[] RangeRaDec(double[] raDec)
        {
            double[] a = { Range24(raDec[0]), Range90(raDec[1]) };
            return a;
        }

        /// <summary>
        /// Force range for primary and secondary axes
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static double[] RangeAxesXY(double[] axes)
        {
            double[] xy = { Range360(axes[0]), Range270(axes[1]) };
            return xy;
        }

        /// <summary>
        /// Force range for secondary and primary axes
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static double[] RangeAxesYX(double[] axes)
        {
            double[] xy = { Range270(axes[1]), Range360(axes[0]) };
            return xy;
        }

        /// <summary>
        /// Returns double in the range of another double
        /// </summary>
        /// <param name="d">Number to range</param>
        /// <param name="a">Maximum range</param>
        /// <returns></returns>
        public static double RangeDouble(double d, double a)
        {
            while (d >= a || d < 0.0)
            {
                if (d < 0.0) d += a;
                if (d >= a) d -= a;
            }
            return d;
        }
    }
}