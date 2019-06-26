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
            while ((d >= 12.0) || (d <= -12.0))
            {
                if (d <= -12.0) d += 24.0;
                if (d >= 12.0) d -= 24.0;
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
        /// <param name="altaz"></param>
        /// <returns></returns>
        public static double[] RangeAltAz(double[] altaz)
        {
           double[] altAz = { Range90(altaz[0]), Range360(altaz[1]) };
           return altAz;
        }

        /// <summary>
        /// Force range for Azimuth an Altitude
        /// </summary>
        /// <remarks>Attention to the order given and received</remarks>
        /// <param name="azalt"></param>
        /// <returns></returns>
        public static double[] RangeAzAlt(double[] azalt)
        {
            double[] azAlt = { Range360(azalt[0]), Range90(azalt[1]) };
            return azAlt;
        }

        /// <summary>
        /// Force range for Right Ascension and Declination
        /// </summary>
        /// <param name="radec"></param>
        /// <returns></returns>
        public static double[] RangeRaDec(double[] radec)
        {
            double[] raDec = { Range24(radec[0]), Range90(radec[1]) };
            return raDec;
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
        /// Force range for secondar and primary axes
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        public static double[] RangeAxesYX(double[] axes)
        {
            double[] xy = { Range270(axes[1]), Range360(axes[0]) };
            return xy;
        }
    }
}