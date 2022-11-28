using System;
using System.IO;
using EqmodNStarAlignment.DataTypes;
using EqmodNStarAlignment.Model;
using EqmodNStarAlignment.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EqmodNStarAlignment.Tests
{
    [TestClass]
    public class AlignmentModelTests
    {
        const double siteLatitude = 52.6683333333333;
        const double siteLongitude = -1.33888888888889;
        const double siteElevation = 200d;
        const long decHomePos = 9003008;
        const long raHomePos = 8388608;
        const double doubleDelta = 0.00000001;

        const HemisphereEnum hemisphere = HemisphereEnum.Northern;

        private EncoderPosition stepsPerRev = new EncoderPosition(2457601, 2457601);

        private AlignmentModel AlignmentModel;

        [TestInitialize]
        public void Initialize()
        {
            AlignmentModel = new AlignmentModel(siteLatitude, siteLongitude, siteElevation, stepsPerRev);
            AlignmentModel.SetHomePosition(raHomePos, decHomePos);
            AlignmentModel.PolarEnable = true;
        }


        [TestMethod]
        public void SavePointsTest()
        {
            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "AlignmentPointsOut.json");
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            AlignmentPoint pt1 = new AlignmentPoint(new EncoderPosition(8987817, 8919464), new double[] { 23.6715774536133, 77.7643051147461 }, new EncoderPosition(8987821, 8919479), new DateTime(2022, 11, 28, 19, 06, 06));
            AlignmentPoint pt2 = new AlignmentPoint(new EncoderPosition(7985357, 9135000), new double[] { 21.481803894043, 70.6648559570313 }, new EncoderPosition(7985268, 9135003), new DateTime(2022, 11, 28, 19, 07, 17));
            AlignmentPoint pt3 = new AlignmentPoint(new EncoderPosition(7847708, 9164640), new double[] { 22.8413619995117, 66.3250198364258 }, new EncoderPosition(7847528, 9164630), new DateTime(2022, 11, 28, 19, 08, 09));

            AlignmentModel.EQ_NPointAppend(pt1);
            AlignmentModel.EQ_NPointAppend(pt2);
            AlignmentModel.EQ_NPointAppend(pt3);

            AlignmentModel.SaveAlignmentPoints(outputFile);

            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void LoadPointsTest()
        {
            string inputFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AlignmentPointsIn.json");

            Matrix ExpectedTaki = Matrix.CreateInstance();
            ExpectedTaki.Element[0, 0] = 0.999962437601123;
            ExpectedTaki.Element[0, 1] = -1.03237679560398E-03;
            ExpectedTaki.Element[0, 2] = 0;
            ExpectedTaki.Element[1, 0] = 4.1772366345733E-04;
            ExpectedTaki.Element[1, 1] = 0.999904533164291;
            ExpectedTaki.Element[1, 2] = 0;
            ExpectedTaki.Element[2, 0] = 0;
            ExpectedTaki.Element[2, 1] = 0;
            ExpectedTaki.Element[2, 2] = 1;

            Matrix ExpectedAffine = Matrix.CreateInstance();
            ExpectedAffine.Element[0, 0] = 1.00003713248826;
            ExpectedAffine.Element[0, 1] = 1.03251370113913E-03;
            ExpectedAffine.Element[1, 0] = -4.17779058621084E-04;
            ExpectedAffine.Element[1, 1] = 1.00009504460391;

            AlignmentModel.ClearAlignmentPoints();

            AlignmentModel.LoadAlignmentPoints(inputFile);

            Assert.IsTrue(AlignmentModel.AlignmentPoints.Count == 3);

            Assert.AreEqual(ExpectedTaki.Element[0, 0], AlignmentModel.EQMT.Element[0, 0], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[0, 1], AlignmentModel.EQMT.Element[0, 1], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[0, 2], AlignmentModel.EQMT.Element[0, 2], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[1, 0], AlignmentModel.EQMT.Element[1, 0], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[1, 1], AlignmentModel.EQMT.Element[1, 1], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[1, 2], AlignmentModel.EQMT.Element[1, 2], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[2, 0], AlignmentModel.EQMT.Element[2, 0], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[2, 1], AlignmentModel.EQMT.Element[2, 1], 0.00000001);
            Assert.AreEqual(ExpectedTaki.Element[2, 2], AlignmentModel.EQMT.Element[2, 2], 0.00000001);

            Assert.AreEqual(ExpectedAffine.Element[0, 0], AlignmentModel.EQMM.Element[0, 0], 0.00000001);
            Assert.AreEqual(ExpectedAffine.Element[0, 1], AlignmentModel.EQMM.Element[0, 1], 0.00000001);
            Assert.AreEqual(ExpectedAffine.Element[1, 0], AlignmentModel.EQMM.Element[1, 0], 0.00000001);
            Assert.AreEqual(ExpectedAffine.Element[1, 1], AlignmentModel.EQMM.Element[1, 1], 0.00000001);

        }
    }
}
