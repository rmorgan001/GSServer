using EqmodNStarAlignment.DataTypes;
using EqmodNStarAlignment.Model;
using EqmodNStarAlignment.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace EqmodNStarAlignment.Tests
{
    //TODO: Set methods back to private and invoke using reflection.
    [TestClass]
    public class EqmodVectorTests
    {
        const double siteLatitude = 52.6683333333333;
        const double siteLongitude = -1.33888888888889;
        const double siteElevation = 200d;
        const double siteTemperatire = 15d;

        const long decHomePos = 9003008;
        const long raHomePos = 8388608;
        const double doubleDelta = 0.00000001;

        const HemisphereEnum hemisphere = HemisphereEnum.Northern;

        private EncoderPosition stepsPerRev = new EncoderPosition(2457601, 2457601);

        private AlignmentModel AlignmentModel;

        [TestInitialize]
        public void Initialize()
        {
            AlignmentModel = new AlignmentModel(siteLatitude, siteLongitude, siteElevation);
            AlignmentModel.StepsPerRev = stepsPerRev;
            AlignmentModel.SetHomePosition(raHomePos, decHomePos);
            AlignmentModel.PolarEnable = true;

        }

        [TestMethod]
        public void TestEQ_sp2Cs_1()
        {
            EncoderPosition encoder = new EncoderPosition(8619721, 8850776);
            Coord expected = new Coord()
            {
                x = 322188.53833622788,
                y = -258342.03199652123,
                z = 1
            };
            Coord result = AlignmentModel.EQ_sp2Cs(encoder);
            Assert.AreEqual(expected.x, result.x, 1.0);
            Assert.AreEqual(expected.y, result.y, 1.0);
            Assert.AreEqual(expected.z, result.z);
        }

        [TestMethod]
        public void TestEQ_sp2Cs_2()
        {
            EncoderPosition encoder = new EncoderPosition(8487504, 8913867);
            Coord expected = new Coord()
            {
                x = 456444.627886648,
                y = -182935.356983078,
                z = 1
            };
            Coord result = AlignmentModel.EQ_sp2Cs(encoder);
            Assert.AreEqual(expected.x, result.x, 1.0);
            Assert.AreEqual(expected.y, result.y, 1.0);
            Assert.AreEqual(expected.z, result.z);
        }

    }
}
