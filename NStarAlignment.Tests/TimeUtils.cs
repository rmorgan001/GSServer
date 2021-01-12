using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NStarAlignment.Tests
{
    public class TimeUtils
    {
        public static double GetLocalSiderealTime(DateTime utcTime, TimeSpan utcDateOffset, double longitude)
        {
            double jd = Ole2Jd(utcTime.Add(utcDateOffset));
            return Lst(Epoch2000Days(), jd, false, longitude);

        }
        /// <summary>
        /// OLE Automation Date to Julian Date
        /// For modern dates only
        /// </summary>
        /// <param name="date">DateTime</param>
        /// <returns>2455002.25</returns>
        private static double Ole2Jd(DateTime date)
        {
            var ole2Jd = date.ToOADate() + 2415018.5;
            return ole2Jd;
        }

        /// <summary>
        /// Days since epoch 2000
        /// </summary>
        /// <returns></returns>
        private static double Epoch2000Days()
        {
            return 2451545.0;              //J2000 1 Jan 2000, 12h 0m 0s
        }

        /// <summary>
        /// Local Sidereal Time
        /// Adopted from the ASCOM .net telescope simulator
        /// </summary>
        /// <param name="ejd">2000, 1, 1, 12, 0, 0</param>
        /// <param name="jd">2009, 6, 19, 4, 40, 5, 230</param>
        /// <param name="nutation">true</param>
        /// <param name="longitude">81</param>
        /// <returns>3.9042962940932857</returns>
        private static double Lst(double ejd, double jd, bool nutation, double longitude)
        {
            var a = jd - ejd;                               // Days since epoch
            var b = a / 36525;                              // Century to days for the epoch
            var c = 280.46061837 + 360.98564736629 * a;     // Greenwich Mean Sidereal Time (GMST)
            var d = c + longitude;                          // Local Mean Sidereal Time (LMST)
            if (d < 0.0)
            {
                while (d < 0.0)
                {
                    d += 360.0;
                }
            }
            else
            {
                while (d > 360.0) d -= 360.0;
            }
            if (nutation)
            {
                //calculate OM the longitude when the Moon passes through the plane of the ecliptic
                var e = 125.04452 - 1934.136261 * b;
                if (e < 0.0)
                {
                    while (e < 0.0) e += 360.0;
                }
                else
                {
                    while (e > 360.0) e -= 360.0;
                }
                //calculat L mean longitude of the Sun
                var f = 280.4665 + 36000.7698 * b;
                if (f < 0.0)
                {
                    while (f < 0.0) f += 360.0;
                }
                else
                {
                    while (f > 360.0) f -= 360.0;
                }
                //calculate L1 mean longitude of the Moon
                var g = 218.3165 + 481267.8813 * b;
                if (g < 0.0)
                {
                    while (g < 0) g += 360.0;
                }
                else
                {
                    while (g > 360.0) g -= 360.0;
                }
                //calculate e Obliquity of the Ecliptic
                var h = 23.439 - 0.0000004 * b;
                if (h < 0.0)
                {
                    while (h < 0.0) h += 360.0;
                }
                else
                {
                    while (h > 360.0) h -= 360.0;
                }
                var i = (-17.2 * Math.Sin(e)) - (1.32 * Math.Sin(2 * f)) - (0.23 * Math.Sin(2 * g)) + (0.21 * Math.Sin(2 * e));
                var j = (i * Math.Cos(h)) / 3600;               // Nutation correction for true values
                d += j;                                      // True Local Sidereal Time (LST)
            }
            var m = d * 24.0 / 360.0;
            var lst = Range24(m);
            return lst;
        }

        private static double Range24(double d)
        {
            while ((d >= 24.0) || (d < 0.0))
            {
                if (d < 0.0) d += 24.0;
                if (d >= 24.0) d -= 24.0;
            }
            return d;
        }
    }
}
