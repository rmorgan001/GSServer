using Microsoft.VisualStudio.TestTools.UnitTesting;
using NStarAlignment.DataTypes;
using NStarAlignment.Model;
using NStarAlignment.Utilities;
using System;

namespace NStarAlignment.Tests
{
    [TestClass]
    public class SphericalPolarTests
    {
        private const double latitude = 52.6666666666667;
        private const double longitude = -1.33333333333333;
        private const double elevation = 201.0;
        private readonly DateTime utcTime = new DateTime(2020, 1, 1, 00, 00, 00).ToUniversalTime();
        private TimeRecord timeRecord;
        private AlignmentModel model;
        private double tolerance = 0.002F;

        [TestInitialize]
        public void Initialise()
        {
            model = new AlignmentModel(latitude, longitude, elevation);
            model.ClearAlignmentPoints();
            timeRecord = new TimeRecord(this.utcTime, longitude);

        }


        [TestCleanup]
        public void Cleanup()
        {
            model = null;
        }

        [DataTestMethod]
        [DataRow(0.0, 45.0)]
        [DataRow(10.0, 45.0)]
        [DataRow(15.0, 45.0)]
        [DataRow(45.0, 45.0)]
        [DataRow(60.0, 45.0)]
        [DataRow(89.99, 45.0)]
        [DataRow(120.0, 45.0)]
        public void RoundTripPolarCartesTests(double alt, double az)
        {
            SphericalCoordinate inSpherical = new SphericalCoordinate()
            {
                X = az,
                Y = alt,
                R = 0
            };
            CarteseanCoordinate outCart = model.PolarToCartesean(inSpherical);
            SphericalCoordinate outSpherical = model.CarteseanToPolar(outCart);
            Assert.AreEqual(inSpherical, outSpherical);

        }

        [DataTestMethod]
        [DataRow(0.00, 0.00)]
        [DataRow(0.00, 30.00)]
        [DataRow(0.00, 60.00)]
        [DataRow(0.00, 90.00)]
        [DataRow(0.00, 120.00)]
        [DataRow(0.00, 150.00)]
        [DataRow(0.00, 180.00)]
        [DataRow(0.00, 210.00)]
        [DataRow(0.00, 240.00)]
        [DataRow(0.00, 270.00)]
        [DataRow(0.00, 300.00)]
        [DataRow(0.00, 330.00)]
        [DataRow(30.00, 0.00)]
        [DataRow(30.00, 30.00)]
        [DataRow(30.00, 60.00)]
        [DataRow(30.00, 90.00)]
        [DataRow(30.00, 120.00)]
        [DataRow(30.00, 150.00)]
        [DataRow(30.00, 180.00)]
        [DataRow(30.00, 210.00)]
        [DataRow(30.00, 240.00)]
        [DataRow(30.00, 270.00)]
        [DataRow(30.00, 300.00)]
        [DataRow(30.00, 330.00)]
        [DataRow(60.00, 0.00)]
        [DataRow(60.00, 30.00)]
        [DataRow(60.00, 60.00)]
        [DataRow(60.00, 90.00)]
        [DataRow(60.00, 120.00)]
        [DataRow(60.00, 150.00)]
        [DataRow(60.00, 180.00)]
        [DataRow(60.00, 210.00)]
        [DataRow(60.00, 240.00)]
        [DataRow(60.00, 270.00)]
        [DataRow(60.00, 300.00)]
        [DataRow(60.00, 330.00)]
        [DataRow(89.99, 0.00)]
        [DataRow(89.99, 30.00)]
        [DataRow(89.99, 60.00)]
        [DataRow(89.99, 90.00)]
        [DataRow(89.99, 120.00)]
        [DataRow(89.99, 150.00)]
        [DataRow(89.99, 180.00)]
        [DataRow(89.99, 210.00)]
        [DataRow(89.99, 240.00)]
        [DataRow(89.99, 270.00)]
        [DataRow(89.99, 300.00)]
        [DataRow(89.99, 330.00)]
        public void RoundTripSphericalPolarTests(double alt, double az)
        {
            AxisPosition axes = new AxisPosition(az, alt);
            SphericalCoordinate spherical1 = model.AxesToSpherical(new double[] {az, alt}, timeRecord);
            AxisPosition outAxes = model.SphericalToAxes(spherical1, timeRecord, spherical1.R);
            Assert.IsTrue(outAxes.Equals(axes, tolerance));

        }

        [DataTestMethod]
        [DataRow(0.00, 0.00)]
        [DataRow(0.00, 30.00)]
        [DataRow(0.00, 60.00)]
        [DataRow(0.00, 90.00)]
        [DataRow(0.00, 120.00)]
        [DataRow(0.00, 150.00)]
        [DataRow(0.00, 180.00)]
        [DataRow(0.00, 210.00)]
        [DataRow(0.00, 240.00)]
        [DataRow(0.00, 270.00)]
        [DataRow(0.00, 300.00)]
        [DataRow(0.00, 330.00)]
        [DataRow(30.00, 0.00)]
        [DataRow(30.00, 30.00)]
        [DataRow(30.00, 60.00)]
        [DataRow(30.00, 90.00)]
        [DataRow(30.00, 120.00)]
        [DataRow(30.00, 150.00)]
        [DataRow(30.00, 180.00)]
        [DataRow(30.00, 210.00)]
        [DataRow(30.00, 240.00)]
        [DataRow(30.00, 270.00)]
        [DataRow(30.00, 300.00)]
        [DataRow(30.00, 330.00)]
        [DataRow(60.00, 0.00)]
        [DataRow(60.00, 30.00)]
        [DataRow(60.00, 60.00)]
        [DataRow(60.00, 90.00)]
        [DataRow(60.00, 120.00)]
        [DataRow(60.00, 150.00)]
        [DataRow(60.00, 180.00)]
        [DataRow(60.00, 210.00)]
        [DataRow(60.00, 240.00)]
        [DataRow(60.00, 270.00)]
        [DataRow(60.00, 300.00)]
        [DataRow(60.00, 330.00)]
        [DataRow(90.00, 0.00)]
        [DataRow(90.00, 30.00)]
        [DataRow(90.00, 60.00)]
        [DataRow(90.00, 90.00)]
        [DataRow(90.00, 120.00)]
        [DataRow(90.00, 150.00)]
        [DataRow(90.00, 180.00)]
        [DataRow(90.00, 210.00)]
        [DataRow(90.00, 240.00)]
        [DataRow(90.00, 270.00)]
        [DataRow(90.00, 300.00)]
        [DataRow(90.00, 330.00)]
        public void AxisToRaDecTests(double dec, double ra)
        {
            double[] raDec = model.AxesXYToRaDec(new double[] { ra, dec }, timeRecord);
            double[] axes = model.RaDecToAxesXY(raDec, timeRecord);
            Assert.AreEqual(ra, axes[0], tolerance);
            Assert.AreEqual(dec, axes[1], tolerance);
        }


        [DataTestMethod]
        [DataRow(6.58561821949163, 0)]
        [DataRow(4.58561821949163, 0)]
        [DataRow(2.58561821949163, 0)]
        [DataRow(0.585618219491629, 0)]
        [DataRow(22.5856182194916, 0)]
        [DataRow(20.5856182194916, 0)]
        [DataRow(18.5856182194916, 0)]
        [DataRow(16.5856182194916, 0)]
        [DataRow(14.5856182194916, 0)]
        [DataRow(12.5856182194916, 0)]
        [DataRow(10.5856182194916, 0)]
        [DataRow(8.58561821949163, 0)]
        [DataRow(6.58561821949163, 30)]
        [DataRow(4.58561821949163, 30)]
        [DataRow(2.58561821949163, 30)]
        [DataRow(0.585618219491629, 30)]
        [DataRow(22.5856182194916, 30)]
        [DataRow(20.5856182194916, 30)]
        [DataRow(18.5856182194916, 30)]
        [DataRow(16.5856182194916, 30)]
        [DataRow(14.5856182194916, 30)]
        [DataRow(12.5856182194916, 30)]
        [DataRow(10.5856182194916, 30)]
        [DataRow(8.58561821949163, 30)]
        [DataRow(6.58561821949163, 60)]
        [DataRow(4.58561821949163, 60)]
        [DataRow(2.58561821949163, 60)]
        [DataRow(0.585618219491629, 60)]
        [DataRow(22.5856182194916, 60)]
        [DataRow(20.5856182194916, 60)]
        [DataRow(18.5856182194916, 60)]
        [DataRow(16.5856182194916, 60)]
        [DataRow(14.5856182194916, 60)]
        [DataRow(12.5856182194916, 60)]
        [DataRow(10.5856182194916, 60)]
        [DataRow(8.58561821949163, 60)]
        [DataRow(6.58561821949163, 89)]
        [DataRow(4.58561821949163, 89)]
        [DataRow(2.58561821949163, 89)]
        [DataRow(0.585618219491629, 89)]
        [DataRow(22.5856182194916, 89)]
        [DataRow(20.5856182194916, 89)]
        [DataRow(18.5856182194916, 89)]
        [DataRow(16.5856182194916, 89)]
        [DataRow(14.5856182194916, 89)]
        [DataRow(12.5856182194916, 89)]
        [DataRow(10.5856182194916, 89)]
        [DataRow(8.58561821949163, 89)]
        public void RADecAltAxRADecTest(double ra, double dec)
        {
            double lst = TimeUtils.GetLocalSiderealTime(timeRecord.UtcTime, new TimeSpan(), longitude);
            double[] altAz = AstroConvert.RaDec2AltAz(ra, dec, lst, latitude);
            double[] raDec = AstroConvert.AltAz2RaDec(altAz[0], altAz[1], latitude, lst);
            Assert.AreEqual(ra, raDec[0], tolerance);
            Assert.AreEqual(dec, raDec[1], tolerance);
        }
    }
}
