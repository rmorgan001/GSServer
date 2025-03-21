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

//using System;

namespace GS.Principles
{
    public static class Conversions
    {
        ///// <summary>
        ///// Milliseconds to seconds per arcSeconds
        ///// </summary>
        ///// <param name="millSeconds"></param>
        ///// <param name="rate">Degrees per second</param>
        ///// <param name="prate">Percentage of rate</param>
        ///// <returns></returns>
        //public static double Ms2ArcSec(double millSeconds, double rate, double prate)
        //{
        //    if (Math.Abs(prate) < 0.01) prate = 1;
        //     var a = millSeconds / 1000.0;
        //    var b = GuideRate(rate, prate);
        //    var c = a * b * 3600;
        //    return c;
        //}

        ///// <summary>
        ///// Calculate guideRate from rate in arcSeconds per second
        ///// </summary>
        ///// <param name="rate">Degrees per second</param>
        ///// <param name="prate">Percentage of rate 0-1.0</param>
        ///// <returns></returns>
        //public static double GuideRate(double rate, double prate)
        //{
        //    if (Math.Abs(prate) < 0.01) prate = 1;
        //    var a = ArcSec2Deg(rate);
        //    var b = a * prate;
        //    return b;
        //}

        ///// <summary>
        ///// Steps in arcSeconds per second
        ///// </summary>
        ///// <param name="prate">Percentage of rate 0-1.0</param>
        ///// <param name="totalSteps"></param>
        ///// <param name="milliseconds"></param>
        ///// <param name="rate">in arcSeconds</param>
        ///// <returns></returns>
        //public static double Rate2Steps(double milliseconds, double rate, double prate, double totalSteps)
        //{
        //    var a = StepPerArcSec(totalSteps);
        //    var b = a * Ms2ArcSec(milliseconds, rate, prate);
        //    return b;
        //}

        /// <summary>
        /// Calculates steps per arcSecond
        /// </summary>
        /// <param name="totalSteps"></param>
        /// <returns></returns>
        public static double StepPerArcSec(double totalSteps)
        {
            var a = totalSteps / 360 / 3600.0;
            return a;
        }

        /// <summary>
        /// ArcSeconds to degrees
        /// </summary>
        /// <param name="arcSec">ArcSecond per second</param>
        /// <returns></returns>
        public static double ArcSec2Deg(double arcSec)
        {
            return arcSec / 3600.0;
        }

        /// <summary>
        /// Degrees to ArcSeconds
        /// </summary>
        /// <param name="degrees">Degrees per second</param>
        /// <returns>ArcSeconds</returns>
        public static double Deg2ArcSec(double degrees)
        {
            return degrees * 3600.0;
        }

        ///// <summary>
        ///// Seconds to Degrees
        ///// </summary>
        ///// <param name="degrees">Degrees per second</param>
        ///// <returns></returns>
        //public static double Sec2Deg(double degrees)
        //{
        //    return degrees / 360.0;
        //}

        ///// <summary>
        ///// Seconds to ArcSeconds
        ///// </summary>
        ///// <param name="seconds">seconds in time</param>
        ///// <returns>ArcSeconds</returns>
        //public static double Sec2ArcSec(double seconds)
        //{
        //    return seconds * 15;
        //}

        ///// <summary>
        ///// ArcSeconds to Seconds
        ///// </summary>
        ///// <param name="arcSecs">seconds in arc</param>
        ///// <returns>seconds</returns>
        //public static double ArcSec2Sec(double arcSecs)
        //{
        //    return arcSecs / 15;
        //}

        /// <summary>
        /// Seconds per sidereal second to Arc seconds per second
        /// </summary>
        /// <param name="seconds">seconds per sidereal second</param>
        public static double SideSec2ArcSec(double seconds)
        {
            return seconds * 1.0027304323 * 15;
        }


        ///// <summary>
        ///// Arc seconds per second to Seconds per sidereal second
        ///// </summary>
        ///// <param name="seconds">arc seconds per second</param>
        //public static double ArcSec2SideSec(double seconds)
        //{
        //    return seconds * 0.9972695677;
        //}
    }
}
