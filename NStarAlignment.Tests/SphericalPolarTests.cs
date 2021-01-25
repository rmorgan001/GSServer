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
        public void RoundTripAxesToSpherical(double x, double y)
        {
            SphericalCoordinate spherical = model.AxesToSpherical(new double[] { x, y }, timeRecord);
            double[] axes = model.SphericalToAxes(spherical, timeRecord, spherical.WeightsDown);
            double cosx = Math.Cos(Angle.DegreesToRadians(x));
            double cosy = Math.Cos(Angle.DegreesToRadians(y));
            double cosa0 = Math.Cos(Angle.DegreesToRadians(axes[0]));
            double cosa1 = Math.Cos(Angle.DegreesToRadians(axes[1]));

            Assert.AreEqual(cosx, cosa0, tolerance);
            Assert.AreEqual(cosy, cosa1, tolerance);
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
                WeightsDown = false
            };
            CarteseanCoordinate outCart = model.SphericalToCartesean(inSpherical);
            SphericalCoordinate outSpherical = model.CarteseanToSpherical(outCart);
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
            AxisPosition outAxes = model.SphericalToAxes(spherical1, timeRecord, spherical1.WeightsDown);
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



        [DataTestMethod]
        #region Test Data ...
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
        [DataRow(120.00, 0.00)]
        [DataRow(120.00, 30.00)]
        [DataRow(120.00, 60.00)]
        [DataRow(120.00, 90.00)]
        [DataRow(120.00, 120.00)]
        [DataRow(120.00, 150.00)]
        [DataRow(120.00, 180.00)]
        [DataRow(120.00, 210.00)]
        [DataRow(120.00, 240.00)]
        [DataRow(120.00, 270.00)]
        [DataRow(120.00, 300.00)]
        [DataRow(120.00, 330.00)]
        [DataRow(150.00, 0.00)]
        [DataRow(150.00, 30.00)]
        [DataRow(150.00, 60.00)]
        [DataRow(150.00, 90.00)]
        [DataRow(150.00, 120.00)]
        [DataRow(150.00, 150.00)]
        [DataRow(150.00, 180.00)]
        [DataRow(150.00, 210.00)]
        [DataRow(150.00, 240.00)]
        [DataRow(150.00, 270.00)]
        [DataRow(150.00, 300.00)]
        [DataRow(150.00, 330.00)]
        [DataRow(180.00, 0.00)]
        [DataRow(180.00, 30.00)]
        [DataRow(180.00, 60.00)]
        [DataRow(180.00, 90.00)]
        [DataRow(180.00, 120.00)]
        [DataRow(180.00, 150.00)]
        [DataRow(180.00, 180.00)]
        [DataRow(180.00, 210.00)]
        [DataRow(180.00, 240.00)]
        [DataRow(180.00, 270.00)]
        [DataRow(180.00, 300.00)]
        [DataRow(180.00, 330.00)]
        [DataRow(210.00, 0.00)]
        [DataRow(210.00, 30.00)]
        [DataRow(210.00, 60.00)]
        [DataRow(210.00, 90.00)]
        [DataRow(210.00, 120.00)]
        [DataRow(210.00, 150.00)]
        [DataRow(210.00, 180.00)]
        [DataRow(210.00, 210.00)]
        [DataRow(210.00, 240.00)]
        [DataRow(210.00, 270.00)]
        [DataRow(210.00, 300.00)]
        [DataRow(210.00, 330.00)]
        [DataRow(240.00, 0.00)]
        [DataRow(240.00, 30.00)]
        [DataRow(240.00, 60.00)]
        [DataRow(240.00, 90.00)]
        [DataRow(240.00, 120.00)]
        [DataRow(240.00, 150.00)]
        [DataRow(240.00, 180.00)]
        [DataRow(240.00, 210.00)]
        [DataRow(240.00, 240.00)]
        [DataRow(240.00, 270.00)]
        [DataRow(240.00, 300.00)]
        [DataRow(240.00, 330.00)]
        [DataRow(270.00, 0.00)]
        [DataRow(270.00, 30.00)]
        [DataRow(270.00, 60.00)]
        [DataRow(270.00, 90.00)]
        [DataRow(270.00, 120.00)]
        [DataRow(270.00, 150.00)]
        [DataRow(270.00, 180.00)]
        [DataRow(270.00, 210.00)]
        [DataRow(270.00, 240.00)]
        [DataRow(270.00, 270.00)]
        [DataRow(270.00, 300.00)]
        [DataRow(270.00, 330.00)]
        [DataRow(300.00, 0.00)]
        [DataRow(300.00, 30.00)]
        [DataRow(300.00, 60.00)]
        [DataRow(300.00, 90.00)]
        [DataRow(300.00, 120.00)]
        [DataRow(300.00, 150.00)]
        [DataRow(300.00, 180.00)]
        [DataRow(300.00, 210.00)]
        [DataRow(300.00, 240.00)]
        [DataRow(300.00, 270.00)]
        [DataRow(300.00, 300.00)]
        [DataRow(300.00, 330.00)]
        [DataRow(330.00, 0.00)]
        [DataRow(330.00, 30.00)]
        [DataRow(330.00, 60.00)]
        [DataRow(330.00, 90.00)]
        [DataRow(330.00, 120.00)]
        [DataRow(330.00, 150.00)]
        [DataRow(330.00, 180.00)]
        [DataRow(330.00, 210.00)]
        [DataRow(330.00, 240.00)]
        [DataRow(330.00, 270.00)]
        [DataRow(330.00, 300.00)]
        [DataRow(330.00, 330.00)]
        #endregion
        public void AxesXYToRaDecTest(double x, double y)
        {
            double[] raDec = model.AxesXYToRaDec(new double[]{x, y}, timeRecord);
            double[] axes = model.RaDecToAxesXY(raDec, timeRecord);
            System.Diagnostics.Debug.WriteLine($"Original Axes = {x}/{y}, raDec = {raDec[0]}/{raDec[1]}, axes = {axes[0]}/{axes[1]}");
            Assert.IsTrue(Angle.AreSameDegrees(x, axes[0], tolerance));
            Assert.IsTrue(Angle.AreSameDegrees(y, axes[1], tolerance));
        }


        [DataTestMethod]
        [DataRow(0, 30, 12, 0, 30, 12)]
        public void GenerateAngleData(double xStart, double xIncrement, int xSteps, double yStart, double yIncrement, int ySteps)
        {
            System.Diagnostics.Debug.WriteLine("#region Test Data ...");
            double x = xStart;
            double y = yStart;
            for (int xs = 0; xs < xSteps; xs++)
            {
                for (int ys = 0; ys < ySteps; ys++)
                {
                    System.Diagnostics.Debug.WriteLine($"[DataRow({x:F2}, {y:F2})]");
                    y += yIncrement;
                }

                y = yStart;
                x += xIncrement;
            }
            System.Diagnostics.Debug.WriteLine("#endregion");
            Assert.IsTrue(true);
        }

    }
}
