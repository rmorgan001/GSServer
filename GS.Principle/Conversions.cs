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
    public static class Conversions
    {
        /// <summary>
        /// Milliseconds to seconds per arcseconds
        /// </summary>
        /// <param name="millseconds"></param>
        /// <param name="rate">Degrees per second</param>
        /// <param name="prate">Perentage of rate</param>
        /// <returns></returns>
        public static double Ms2Arcsec(double millseconds, double rate, double prate)
        {
            if (Math.Abs(prate) < 0.01) prate = 1;
             var a = millseconds / 1000.0;
            var b = GuideRate(rate, prate);
            var c = a * b * 3600;
            return c;
        }

        /// <summary>
        /// Calculate guiderate from rate in arcseconds per second
        /// </summary>
        /// <param name="rate">Degrees per second</param>
        /// <param name="prate">Perentage of rate 0-1.0</param>
        /// <returns></returns>
        public static double GuideRate(double rate, double prate)
        {
            if (Math.Abs(prate) < 0.01) prate = 1;
            var a = ArcSec2Deg(rate);
            var b = a * prate;
            return b;
        }

        /// <summary>
        /// Steps in arcseconds per second
        /// </summary>
        /// <param name="prate">Perentage of rate 0-1.0</param>
        /// <param name="totalsteps"></param>
        /// <param name="milliseconds"></param>
        /// <param name="rate">in arcseconds</param>
        /// <returns></returns>
        public static double Rate2Steps(double milliseconds, double rate, double prate, double totalsteps)
        {
            var a = StepPerArcsec(totalsteps);
            var b = a * Ms2Arcsec(milliseconds, rate, prate);
            return b;
        }

        /// <summary>
        /// Calculates steps per arcsecond
        /// </summary>
        /// <param name="totalsteps"></param>
        /// <returns></returns>
        public static double StepPerArcsec(double totalsteps)
        {
            var a = totalsteps / 360 / 3600.0;
            return a;
        }

        /// <summary>
        /// Arcseconds to degrees
        /// </summary>
        /// <param name="arcsec">ArcSecond per second</param>
        /// <returns></returns>
        public static double ArcSec2Deg(double arcsec)
        {
            return arcsec / 3600.0;
        }

        /// <summary>
        /// Degrees to Arcseconds
        /// </summary>
        /// <param name="degrees">Degrees per second</param>
        /// <returns></returns>
        public static double Deg2ArcSec(double degrees)
        {
            return degrees * 3600.0;
        }
    }
}
