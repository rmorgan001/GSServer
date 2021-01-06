using Microsoft.VisualStudio.TestTools.UnitTesting;
using NStarAlignment.Model;
using System;

namespace NStarAlignment.Tests
{
    [TestClass]
    public class NStarTriangleCentre
    {
        private const double latitude = 52.6666666666667;
        private const double longitude = -1.33333333333333;
        private const double elevation = 201.0;
        private const double temperature = 10.0;
        private const double tolerance = 0.05;

        private AlignmentModel model;

        [TestInitialize]
        public void Initialise()
        {
            model = new AlignmentModel(latitude, longitude, elevation, temperature)
            {
                AlignmentAlgorithm = AlignmentAlgorithm.NStar,
                ThreePointMode = ThreePointMode.TriangleWithNearestCentre
            };
            model.ClearAlignmentPoints();


            // NW
            model.AddAlignmentPoint(new double[] { 1.45234227180481, 60.3347473144531 }, new double[] { 144.945062700193, 60.3347473144531 }, new double[] { 149.723756536623, 59.1249432487836 }, new DateTime(2020, 06, 28, 16, 43, 18));
            model.AddAlignmentPoint(new double[] { 2.86930561065674, 55.9740562438965 }, new double[] { 123.819627432618, 55.9740562438965 }, new double[] { 128.404096999038, 55.283421763509 }, new DateTime(2020, 06, 28, 16, 43, 48));
            model.AddAlignmentPoint(new double[] { 2.08612251281738, 72.5127334594727 }, new double[] { 135.650906194933, 72.5127334594727 }, new double[] { 143.329091832532, 71.5666812294945 }, new DateTime(2020, 06, 28, 16, 44, 08));
            //model.AddAlignmentPoint(new double[]{1.93080902099609, 63.763801574707}, new double[] { 138.060159151908, 63.763801574707 }, new double[] { 143.482011096669, 62.7368181631223 }, new DateTime(2020, 06, 28, 16, 44, 27));
            //// SW
            model.AddAlignmentPoint(new double[] { 7.5975866317749 , 31.8432216644287 }, new double[] { 53.2216111482121, 31.8432216644287 }, new double[] { 56.0429603483716, 31.4201609110561 }, new DateTime(2020, 06, 28, 16, 45, 06));
            model.AddAlignmentPoint(new double[] { 5.93735456466675, 7.40935134887695}, new double[] { 78.2768689906225, 7.40935134887695 }, new double[] { 81.0690819058243, 7.01751946987766 }, new DateTime(2020, 06, 28, 16, 45, 43));
            model.AddAlignmentPoint(new double[] { 7.67223644256592, 5.17215204238892}, new double[] { 52.3623042968102, 5.17215204238892 }, new double[] { 54.9750315780859, 4.46534644056118 }, new DateTime(2020, 06, 28, 16, 46, 09));
            // model.AddAlignmentPoint(new double[]{7.32045793533325, 16.5029945373535}, new double[] { 57.7312421342358, 16.5029945373535 }, new double[] { 60.4098738774835, 15.9579469987893 }, new DateTime(2020, 06, 28, 16, 46, 31));
            ////SE
            model.AddAlignmentPoint(new double[] { 13.0530576705933, 10.8525428771973 }, new double[] { 152.075877884869, 169.147457122803 }, new double[] { 152.72162571714, 171.563163746142 }, new DateTime(2020, 06, 28, 16, 47, 51));
            model.AddAlignmentPoint(new double[] { 12.3489561080933, -0.778456687927246 }, new double[] { 162.71580699319, 180.778456687927 }, new double[] { 163.926678775838, 183.082432087453 }, new DateTime(2020, 06, 28, 16, 48, 09));
            model.AddAlignmentPoint(new double[] { 13.5954742431641, -0.697973549365997 }, new double[] { 144.097740196623, 180.697973549366 }, new double[] { 144.925319826158, 183.236913318592 }, new DateTime(2020, 06, 28, 16, 48, 28));
            // model.AddAlignmentPoint(new double[]{12.9437046051025, 3.28892135620117}, new double[] { 153.945157337934, 176.711078643799 }, new double[] { 154.854211281482, 179.129744501857 }, new DateTime(2020, 06, 28, 16, 48, 45));
            ////NE
            model.AddAlignmentPoint(new double[] { 16.405143737793, 61.4716148376465 }, new double[] { 102.750476080924, 118.528385162354 }, new double[] { 100.26112572701, 120.586627285499 }, new DateTime(2020, 06, 28, 16, 51, 39));
            model.AddAlignmentPoint(new double[] { 15.4235200881958, 58.8995513916016 }, new double[] { 117.558254360687, 121.100448608398 }, new double[] { 114.734222729235, 122.861582942981 }, new DateTime(2020, 06, 28, 16, 52, 00));
            model.AddAlignmentPoint(new double[] { 15.9716958999634, 54.6963729858398 }, new double[] { 109.430551501457, 125.30362701416 }, new double[] { 107.541470813369, 127.418045303378 }, new DateTime(2020, 06, 28, 16, 52, 22));
            // model.AddAlignmentPoint(new double[]{16.0384006500244, 58.5149154663086}, new double[] { 108.51725186361, 121.485084533691 }, new double[] { 106.174079618103, 123.499884590653 }, new DateTime(2020, 06, 28, 16, 52, 43));
        }

        [TestMethod]
        public void TestSyncedNW()
        {
            // model.AddAlignmentPoint(1.93080902099609, 63.763801574707, new double[] { 138.060159151908, 63.763801574707 }, new double[] { 143.482011096669, 62.7368181631223 }, new DateTime(2020, 06, 28, 16, 44, 27));
            double[] adjustedAxis = model.GetSkyAxes(new double[] { 138.060159151908, 63.763801574707 }, new DateTime(2020, 06, 28, 16, 44, 27));
            double[] expectedAxis = new double[] { 143.482011096669, 62.7368181631223 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestSyncedSW()
        {
            // model.AddAlignmentPoint(7.32045793533325, 16.5029945373535, new double[] { 57.7312421342358, 16.5029945373535 }, new double[] { 60.4098738774835, 15.9579469987893 }, new DateTime(2020, 06, 28, 16, 46, 31));
            double[] adjustedAxis = model.GetSkyAxes(new double[] { 57.7312421342358, 16.5029945373535 }, new DateTime(2020, 06, 28, 16, 46, 31));
            double[] expectedAxis = new double[] { 60.4098738774835, 15.9579469987893 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestSynchedSE()
        {
            //  model.AddAlignmentPoint(12.9437046051025, 3.28892135620117, new double[] { 153.945157337934, 176.711078643799 }, new double[] { 154.854211281482, 179.129744501857 }, new DateTime(2020, 06, 28, 16, 48, 45));
            double[] adjustedAxis = model.GetSkyAxes(new double[] { 153.945157337934, 176.711078643799 }, new DateTime(2020, 06, 28, 16, 48, 45));
            double[] expectedAxis = new double[] { 154.854211281482, 179.129744501857 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestSynchedNE()
        {
            //  model.AddAlignmentPoint(16.0384006500244, 58.5149154663086, new double[] { 108.51725186361, 121.485084533691 }, new double[] { 106.174079618103, 123.499884590653 }, new DateTime(2020, 06, 28, 16, 52, 43));
            double[] adjustedAxis = model.GetSkyAxes(new double[] { 108.51725186361, 121.485084533691 }, new DateTime(2020, 06, 28, 16, 52, 43));
            double[] expectedAxis = new double[] { 106.174079618103, 123.499884590653 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestTheoreticalNW()
        {
            // model.AddAlignmentPoint(1.93080902099609, 63.763801574707, new double[] { 138.060159151908, 63.763801574707 }, new double[] { 143.482011096669, 62.7368181631223 }, new DateTime(2020, 06, 28, 16, 44, 27));
            double[] adjustedAxis = model.GetMountAxes(new double[] { 143.482011096669, 62.7368181631223 }, new DateTime(2020, 06, 28, 16, 44, 27));
            double[] expectedAxis = new double[] { 138.060159151908, 63.763801574707 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestTheoreticalSW()
        {
            // model.AddAlignmentPoint(7.32045793533325, 16.5029945373535, new double[] { 57.7312421342358, 16.5029945373535 }, new double[] { 60.4098738774835, 15.9579469987893 }, new DateTime(2020, 06, 28, 16, 46, 31));
            double[] adjustedAxis = model.GetMountAxes(new double[] { 60.4098738774835, 15.9579469987893 }, new DateTime(2020, 06, 28, 16, 46, 31));
            double[] expectedAxis = new double[] { 57.7312421342358, 16.5029945373535 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestTheoreticalSE()
        {
            //  model.AddAlignmentPoint(12.9437046051025, 3.28892135620117, new double[] { 153.945157337934, 176.711078643799 }, new double[] { 154.854211281482, 179.129744501857 }, new DateTime(2020, 06, 28, 16, 48, 45));
            double[] adjustedAxis = model.GetMountAxes(new double[] { 154.854211281482, 179.129744501857 }, new DateTime(2020, 06, 28, 16, 48, 45));
            double[] expectedAxis = new double[] { 153.945157337934, 176.711078643799 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

        [TestMethod]
        public void TestTheoreticalNE()
        {
            //  model.AddAlignmentPoint(16.0384006500244, 58.5149154663086, new double[] { 108.51725186361, 121.485084533691 }, new double[] { 106.174079618103, 123.499884590653 }, new DateTime(2020, 06, 28, 16, 52, 43));
            double[] adjustedAxis = model.GetMountAxes(new double[] { 106.174079618103, 123.499884590653 }, new DateTime(2020, 06, 28, 16, 52, 43));
            double[] expectedAxis = new double[] { 108.51725186361, 121.485084533691 };
            Assert.AreEqual(expectedAxis[0], adjustedAxis[0], tolerance);
            Assert.AreEqual(expectedAxis[1], adjustedAxis[1], tolerance);
        }

    }
}
