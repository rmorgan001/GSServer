using EqmodNStarAlignment.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace EqmodNStarAlignment.Tests
{
    [TestClass]
    public class AstroConvertTests
    {
        [DataRow(52.6683333333333, 0, 51.5)]
        [DataRow(52.6683333333333, 3.5, 51.5)]
        [DataRow(52.6683333333333, 9.5, 51.5)]
        [DataRow(52.6683333333333, 12.0, 51.5)]
        [DataRow(52.6683333333333, 15.5, 51.5)]
        [DataRow(52.6683333333333, 21.5, 51.5)]
        [DataRow(52.6683333333333, 0, -51.5)]
        [DataRow(52.6683333333333, 3.5, -51.5)]
        [DataRow(52.6683333333333, 9.5, -51.5)]
        [DataRow(52.6683333333333, 12.0, -51.5)]
        [DataRow(52.6683333333333, 15.5, -51.5)]
        [DataRow(52.6683333333333, 21.5, -51.5)]
        [DataTestMethod]
        public void CompareHaDec_aa2GetAltAz(double lat, double ha, double dec)
        {
            double haRad = AstroConvert.HrsToRad(Range.Range24(ha));
            double decRad = AstroConvert.DegToRad(Range.Range360(dec));
            double latRad = AstroConvert.DegToRad(lat);
            Debug.WriteLine($"HA/Dec (Radians) = {haRad}/{decRad}");


            double[] altAzRad1 = AstroConvert.GetAltAz(latRad, haRad, decRad);
            Debug.WriteLine($"Alt/Az 1 (Radians) = {altAzRad1[0]}/{altAzRad1[1]}");

            double altOut = 0d;
            double azOut = 0d;
            Legacy.hadec_aa(latRad, haRad, decRad, ref altOut, ref azOut);

            Debug.WriteLine($"Alt/Az 2 (Radians) = {altOut}/{azOut}");

            Assert.AreEqual(altOut, altAzRad1[0], 0.0001);
            Assert.AreEqual(azOut, altAzRad1[1], 0.0001);

        }

        [DataRow(52.6683333333333, 51.5, 35.0)]
        [DataRow(52.6683333333333, 51.5, 125.0)]
        [DataRow(52.6683333333333, 51.5, 180.0)]
        [DataRow(52.6683333333333, 51.5, 215.0)]
        [DataRow(52.6683333333333, 51.5, 305.0)]
        [DataRow(52.6683333333333, -51.5, 35.0)]
        [DataRow(52.6683333333333, -51.5, 125.0)]
        [DataRow(52.6683333333333, -51.5, 180.0)]
        [DataRow(52.6683333333333, -51.5, 215.0)]
        [DataRow(52.6683333333333, -51.5, 305.0)]
        [DataTestMethod]
        public void CompareAa_hadec2GetHaDec(double lat, double alt, double az)
        {
            double altRad = AstroConvert.DegToRad(Range.Range90(alt));
            double azRad = AstroConvert.DegToRad(Range.Range360(az));
            double latRad = AstroConvert.DegToRad(lat);
            Debug.WriteLine($"\nAlt/Az (Radians) = {altRad}/{azRad}");


            double[] haDecRad1 = AstroConvert.GetHaDec(latRad, altRad, azRad);
            Debug.WriteLine($"HA/Dec 1 (Radians) = {haDecRad1[0]}/{haDecRad1[1]}");

            double haOut = 0d;
            double decOut = 0d;
            Legacy.aa_hadec(latRad, altRad, azRad, ref haOut, ref decOut);

            Debug.WriteLine($"HA/Dec 2 (Radians) = {haOut}/{decOut}");

            Assert.AreEqual(haOut, haDecRad1[0], 0.0001);
            Assert.AreEqual(decOut, haDecRad1[1], 0.0001);

        }

        /*
        \\ hadec_aa( 0.919235827154167 ,  1.38444074819784 ,  5.38324899333645 , -0.585928494201069 ,  3.96459956111439 )
        hadec_aa [DataRow(-33.5712298065115 , 227.154822259407 , 5.28817411776769 , 308.437447738669 , 52.6683333333333 )] // (EQ_SphericalPolar)
        \\ aa_hadec( 0.919235827154167 , -0.586184018473869 ,  3.96475101882834 ,  1.38487663513127 , -0.9000246120113 )
        aa_hadec [DataRow( 79.3475863211831 ,-51.5676117143725 ,-33.5858703149488 , 227.163500458629 , 52.6683333333333 )]// (EQ_PolarSpherical)

        \\ hadec_aa( 0.919235827154167 ,  1.56557568277018 ,  4.71760386839224 , -0.919190774841039 ,  3.15019144835534 )
        hadec_aa [DataRow(-52.6657519537263 , 180.492674607753 , 5.98005860186418 , 270.298791382328 , 52.6683333333333 )] // (EQ_SphericalPolar)
        \\ aa_hadec( 0.919235827154167 , -0.919213641878334 ,  3.15024659248481 ,  1.5707963267949 , -1.56554822344537 )
        aa_hadec [DataRow( 89.9999999794503 ,-89.6993058071428 ,-52.6670622106594 , 180.495834381095 , 52.6683333333333 )]// (EQ_PolarSpherical)

         */

        [DataRow(0.919235827154167, 1.38444074819784, 5.38324899333645, -0.585928494201069, 3.96459956111439)]
        [DataRow(0.919235827154167, 1.56557568277018, 4.71760386839224, -0.919190774841039, 3.15019144835534)]
        [DataTestMethod]
        public void TestGetAltAz(double latRad, double haRad, double decRad, double altRad, double azRad)
        {
            double[] altAzRad = AstroConvert.GetAltAz(latRad, haRad, decRad);
            Assert.AreEqual(altRad, altAzRad[0], 0.001, "Alt is wrong value");
            Assert.AreEqual(azRad, altAzRad[1], 0.001, "Az is wrong value");
        }

        [DataRow(0.919235827154167, -0.586184018473869, 3.96475101882834, 1.38487663513127, -0.9000246120113)]
        [DataRow(0.919235827154167, -0.919213641878334, 3.15024659248481, 1.5707963267949, -1.56554822344537)]
        [DataTestMethod]
        public void TestGetHaDec(double latRad, double altRad, double azRad, double haRad, double decRad)
        {
            double[] haDecRad = AstroConvert.GetHaDec(latRad, altRad, azRad);
            Assert.AreEqual(haRad, haDecRad[0], 0.001, "HA is wrong value");
            Assert.AreEqual(decRad, haDecRad[1], 0.001, "Dec is wrong value");
        }

    }
}
