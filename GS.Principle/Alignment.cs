using System;
using System.Collections.Generic;

namespace GS.Principles
{
    public static class Alignment
    {
        public static void Test2Star()
        {
           var t = AlignmentError(23, 48, .2, .53, 42.66666667);
           var _ = AlignmentAdjustment(23.2, 48.53, t[0], t[1], 42.66666667);
        }

        /// <summary>
        /// Calculate Error in Degrees in Alt/Az between star center and the delta from star.
        /// </summary>
        /// <remarks>Julius Scheiner (b1858-d1913)</remarks>
        /// <param name="ha">Decimal Hour Angle of Star Center</param>
        /// <param name="dec">Decimal Declination of Star Center</param>
        /// <param name="dha">Decimal Hour Angle Difference from Star to Delta</param>
        /// <param name="ddec">Decimal Declination Difference from Star to Delta</param>
        /// <param name="lat">Decimal Latitude</param>
        /// <returns>Amount of Error in Degrees in Alt and Az</returns>
        public static double[] AlignmentError(double ha, double dec, double dha, double ddec, double lat)
        {
            var ret = new[] {0.0, 0.0};
            var a = Units.Deg2Rad(ha * 15.0);
            var b = Units.Deg2Rad(dha * 15.0);
            var c = Units.Deg2Rad(dec);
            var d = Units.Deg2Rad(ddec);
            var e = Units.Deg2Rad(lat);

            var i = Math.Sin(a);
            var j = Math.Cos(a);
            var k = Math.Tan(c);
            var l = Math.Sin(e);
            var m = Math.Cos(e); 

            var t = i * k;
            var u = l - j * m * k;
            var v = m * i;
            var det = t * v - u * j; 

            if (det <= 0) return ret;

            var x = (v * b - u * d) / det;
            var y = (-j * b + t * d) / det;

            ret[0] = x; //error in altitude
            ret[1] = y; //error in azimuth
            return ret;
        }

        /// <summary>
        /// Calculate adjustment needed for Ha and Dec from errors found in AlignStarError
        /// </summary>
        /// <remarks>Julius Scheiner (b1858-d1913)</remarks>
        /// <param name="ha">Decimal Hour Angle of Target Star</param>
        /// <param name="dec">Decimal Declination of Target Star</param>
        /// <param name="alterr">Decimal Altitude Error to be Applied</param>
        /// <param name="azerr">Decimal Azimuth Error to be applied</param>
        /// <param name="lat">Decimal Latitude</param>
        /// <returns>Adjustments in Degrees for Ha and Dec</returns>
        public static IEnumerable<double> AlignmentAdjustment(double ha, double dec, double alterr, double azerr, double lat)
        {
            var ret = new[] { 0.0, 0.0 };
            var a = Units.Deg2Rad(ha * 15.0);
            var b = Units.Deg2Rad(dec);
            var c = Units.Deg2Rad(lat);

            var i = Math.Sin(a);
            var j = Math.Cos(a);
            var k = Math.Tan(b);
            var l = Math.Sin(c);
            var m = Math.Cos(c);

            var t = i * k;
            var u = l - j * m * k;
            var v = m * i;

            var x = (t * alterr + u * azerr);
            var y = (j * alterr + v * azerr);

            ret[0] = Units.Rad2Deg(x) / 15;
            ret[1] = Units.Rad2Deg(y);

            return ret;
        }

    }
}
