using Microsoft.VisualStudio.TestTools.UnitTesting;
using NStarAlignment.Utilities;
using System;

namespace NStarAlignment.Tests
{
    [TestClass]
    public class AscomToolsTests
    {
        private const double Latitude = 52.6666666666667;
        private const double Longitude = -1.33333333333333;
        private const double Elevation = 201.0;
        private const double Lst = 4.6;
        private const double Temperature = 10.0;
        private readonly DateTime LocalUTCTime = new DateTime(2020, 1, 1, 00, 00, 00).ToUniversalTime();

        private AscomTools _Tools;


        [TestInitialize]
        public void Initialise()
        {
            _Tools = new AscomTools();
            _Tools.Transform.SiteLatitude = Latitude;
            _Tools.Transform.SiteLongitude = Longitude;
            _Tools.Transform.SiteElevation = Elevation;
            _Tools.Transform.SiteTemperature = Temperature;

        }

        [TestCleanup]
        public void Cleanup()
        {
            _Tools.Dispose();
            _Tools = null;
        }

        [DataTestMethod]
        [DataRow(16.6, 0.0)]
        public void GetRaDecGetAltAzTests(double ra, double dec)
        {
            double[] raDecIn = new double[] { ra, dec };
            double[] altAz = _Tools.GetAltAz(raDecIn[0], raDecIn[1], LocalUTCTime);
            double[] raDecOut = _Tools.GetRaDec(altAz[0], altAz[1], LocalUTCTime);
            Assert.AreEqual(raDecIn[0], raDecOut[0], 0.001);
            Assert.AreEqual(raDecIn[1], raDecOut[1], 0.001);
        }


    }
}
