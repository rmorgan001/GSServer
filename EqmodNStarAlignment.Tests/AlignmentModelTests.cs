using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        const double siteTemperatire = 15d;
        const long decHomePos = 9003008;
        const long raHomePos = 8388608;
        const double doubleDelta = 0.0001;
        const double deltaEncoder = 10; // 1/16 arcsecond
        AlignmentPoint[] points = new AlignmentPoint[] {
                new AlignmentPoint(new EncoderPosition(8987817, 8919464), new double[] { 23.6715774536133, 77.7643051147461 }, new EncoderPosition(8987821, 8919479), new DateTime(2022, 11, 28, 19, 06, 06)),
                new AlignmentPoint(new EncoderPosition(7985357, 9135000), new double[] { 21.481803894043, 70.6648559570313 }, new EncoderPosition(7985268, 9135003), new DateTime(2022, 11, 28, 19, 07, 17)),
                new AlignmentPoint(new EncoderPosition(7847708, 9164640), new double[] { 22.8413619995117, 66.3250198364258 }, new EncoderPosition(7847528, 9164630), new DateTime(2022, 11, 28, 19, 08, 09)),
                new AlignmentPoint(new EncoderPosition(8200354, 9412632), new double[] { 21.2521438598633, 29.9979152679443 }, new EncoderPosition(8200185, 9412623), new DateTime(2022, 11, 30, 20, 51, 15)),
                new AlignmentPoint(new EncoderPosition(8380206, 9263912), new double[] { 19.5041027069092, 51.7807502746582 }, new EncoderPosition(8380039, 9263918), new DateTime(2022, 11, 30, 20, 51, 45)),
                new AlignmentPoint(new EncoderPosition(8421824, 9522552), new double[] { 19.107213973999, 13.8985624313354 }, new EncoderPosition(8421625, 9522528), new DateTime(2022, 11, 30, 20, 52, 18)),
                new AlignmentPoint(new EncoderPosition(7887790, 9676808), new double[] { 0.343226462602615, -8.69821166992188 }, new EncoderPosition(7887531, 9676788), new DateTime(2022, 11, 30, 20, 53, 31)),
                new AlignmentPoint(new EncoderPosition(7907761, 9417944), new double[] { 0.159508779644966, 29.2189121246338 }, new EncoderPosition(7907519, 9417941), new DateTime(2022, 11, 30, 20, 54, 12)),
                new AlignmentPoint(new EncoderPosition(8155121, 9549296), new double[] { 21.7548809051514, 9.98066520690918 }, new EncoderPosition(8154849, 9549274), new DateTime(2022, 11, 30, 20, 54, 51)),
                new AlignmentPoint(new EncoderPosition(8964653, 8318792), new double[] { 1.87664878368378, -10.2228670120239 }, new EncoderPosition(8964648, 8318820), new DateTime(2022, 11, 30, 20, 56, 38)),
                new AlignmentPoint(new EncoderPosition(8549669, 8439192), new double[] { 5.94036722183228, 7.41184711456299 }, new EncoderPosition(8549590, 8439206), new DateTime(2022, 11, 30, 20, 57, 16)),
                new AlignmentPoint(new EncoderPosition(8836220, 8668816), new double[] { 3.16128063201904, 41.0447845458984 }, new EncoderPosition(8836045, 8668807), new DateTime(2022, 11, 30, 20, 58, 21)),
                new AlignmentPoint(new EncoderPosition(8810409, 8729584), new double[] { 3.43303275108337, 49.9433364868164 }, new EncoderPosition(8810306, 8729555), new DateTime(2022, 11, 30, 20, 59, 35)),
                new AlignmentPoint(new EncoderPosition(8028636, 8809304), new double[] { 11.0852670669556, 61.624095916748 }, new EncoderPosition(8028459, 8809295), new DateTime(2022, 11, 30, 21, 00, 36)),
                new AlignmentPoint(new EncoderPosition(8378814, 8423856), new double[] { 7.6750659942627, 5.16721248626709 }, new EncoderPosition(8378754, 8423883), new DateTime(2022, 11, 30, 21, 01, 14)),
                new AlignmentPoint(new EncoderPosition(8387315, 8605928), new double[] { 7.60103368759155, 31.8366107940674 }, new EncoderPosition(8387210, 8605946), new DateTime(2022, 11, 30, 21, 01, 45)),
                new AlignmentPoint(new EncoderPosition(8206767, 8622712), new double[] { 9.37405490875244, 34.2941551208496 }, new EncoderPosition(8206583, 8622723), new DateTime(2022, 11, 30, 21, 02, 17)),
                new AlignmentPoint(new EncoderPosition(8661869, 8841768), new double[] { 4.93953990936279, 66.3796844482422 }, new EncoderPosition(8661823, 8841760), new DateTime(2022, 11, 30, 21, 02, 58)),
                new AlignmentPoint(new EncoderPosition(8625168, 8702760), new double[] { 5.30665588378906, 46.0202331542969 }, new EncoderPosition(8624972, 8702773), new DateTime(2022, 11, 30, 21, 03, 24)),
                new AlignmentPoint(new EncoderPosition(8696329, 8501640), new double[] { 4.62079238891602, 16.5555591583252 }, new EncoderPosition(8696186, 8501627), new DateTime(2022, 11, 30, 21, 03, 58)),
                new AlignmentPoint(new EncoderPosition(8959119, 9178696), new double[] { 14.0829401016235, 64.2641220092773 }, new EncoderPosition(8959093, 9178699), new DateTime(2022, 11, 30, 21, 05, 44)),
                new AlignmentPoint(new EncoderPosition(8912307, 9356584), new double[] { 14.549503326416, 38.2075653076172 }, new EncoderPosition(8912190, 9356578), new DateTime(2022, 11, 30, 21, 06, 15)),
                new AlignmentPoint(new EncoderPosition(8412924, 9595864), new double[] { 19.4437313079834, 3.1609582901001 }, new EncoderPosition(8412645, 9595830), new DateTime(2022, 11, 30, 21, 07, 12))
            };


        private EncoderPosition stepsPerRev = new EncoderPosition(2457601, 2457601);

        private AlignmentModel _alignmentModel;

        [TestInitialize]
        public void Initialize()
        {
            _alignmentModel = new AlignmentModel(siteLatitude, siteLongitude, siteElevation);
            _alignmentModel.StepsPerRev = stepsPerRev;
            _alignmentModel.SetHomePosition(raHomePos, decHomePos);
            string inputFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AlignmentPointsIn.json");
            _alignmentModel.LoadAlignmentPoints(inputFile);

        }


        [TestMethod]
        public void SavePointsTest()
        {
            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "AlignmentPointsOut.json");
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            foreach (AlignmentPoint pt in points)
            {
                _alignmentModel.EQ_NPointAppend(pt);
            }
            _alignmentModel.SaveAlignmentPoints(outputFile);

            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void LoadPointsTest()
        {
            string inputFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AlignmentPointsIn.json");
            _alignmentModel.ClearAlignmentPoints();

            _alignmentModel.LoadAlignmentPoints(inputFile);

            Assert.IsTrue(_alignmentModel.AlignmentPoints.Count == 23);

        }

        [TestMethod]
        public void TestMatrices()
        {

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

            // Clear points and reload first three
            _alignmentModel.ClearAlignmentPoints();

            for (int i = 0; i < 3; i++)
            {
                _alignmentModel.EQ_NPointAppend(points[i]);
            }

            Assert.IsTrue(_alignmentModel.AlignmentPoints.Count == 3);

            Assert.AreEqual(ExpectedTaki.Element[0, 0], AlignmentModel.EQMT.Element[0, 0], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[0, 1], AlignmentModel.EQMT.Element[0, 1], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[0, 2], AlignmentModel.EQMT.Element[0, 2], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[1, 0], AlignmentModel.EQMT.Element[1, 0], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[1, 1], AlignmentModel.EQMT.Element[1, 1], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[1, 2], AlignmentModel.EQMT.Element[1, 2], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[2, 0], AlignmentModel.EQMT.Element[2, 0], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[2, 1], AlignmentModel.EQMT.Element[2, 1], doubleDelta);
            Assert.AreEqual(ExpectedTaki.Element[2, 2], AlignmentModel.EQMT.Element[2, 2], doubleDelta);

            Assert.AreEqual(ExpectedAffine.Element[0, 0], AlignmentModel.EQMM.Element[0, 0], doubleDelta);
            Assert.AreEqual(ExpectedAffine.Element[0, 1], AlignmentModel.EQMM.Element[0, 1], doubleDelta);
            Assert.AreEqual(ExpectedAffine.Element[1, 0], AlignmentModel.EQMM.Element[1, 0], doubleDelta);
            Assert.AreEqual(ExpectedAffine.Element[1, 1], AlignmentModel.EQMM.Element[1, 1], doubleDelta);

        }

        [DataRow(8230673, 8846876, 8230828, 8846880)]
        [DataRow(8224190, 8840364, 8224348, 8840368)]
        [DataRow(8218188, 8834403, 8218348, 8834408)]
        [DataRow(8212194, 8828474, 8212404, 8828464)]
        [DataRow(8206462, 8822595, 8206588, 8822600)]
        [DataRow(8200335, 8816551, 8200468, 8816552)]
        [DataRow(8194520, 8810698, 8194660, 8810696)]
        [DataRow(8188449, 8804653, 8188596, 8804648)]
        [DataRow(8182274, 8798512, 8182428, 8798504)]
        [DataRow(8176361, 8792548, 8176548, 8792552)]
        [DataRow(8170243, 8786429, 8170364, 8786408)]
        [DataRow(8164092, 8780291, 8164220, 8780272)]
        [DataRow(8157949, 8774145, 8158084, 8774128)]
        [DataRow(8152079, 8768280, 8152220, 8768264)]
        [DataRow(8145665, 8761853, 8145812, 8761840)]
        [DataRow(8139500, 8755707, 8139652, 8755696)]
        [DataRow(8133279, 8749473, 8133436, 8749464)]
        [DataRow(8126858, 8743046, 8127020, 8743040)]
        [DataRow(8120654, 8736900, 8120820, 8736896)]
        [DataRow(8052366, 8698287, 8052543, 8698296)]
        [DataRow(8052373, 8698287, 8052550, 8698296)]
        [DataRow(8052380, 8698287, 8052557, 8698296)]
        [DataRow(8052387, 8698287, 8052564, 8698296)]
        [DataRow(8114850, 8731041, 8115020, 8731040)]
        [DataRow(8108318, 8724510, 8108492, 8724512)]
        [DataRow(8102251, 8718452, 8102428, 8718456)]
        [DataTestMethod]
        public void TestDelta_Matrix_Reverse_Map(long expectedRa, long expectedDec, long testRA, long testDec)
        {
            _alignmentModel.ThreePointAlgorithm = ThreePointAlgorithmEnum.ClosestPoints;
            _alignmentModel.ActivePoints = ActivePointsEnum.All;

            EncoderPosition test = new EncoderPosition(testRA, testDec);
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("Delta_Matrix_Reverse_Map", BindingFlags.NonPublic | BindingFlags.Instance);
            MapResult result = (MapResult)methodInfo.Invoke(_alignmentModel, new object[] { test });
            // var result = AlignmentModel.Delta_Matrix_Reverse_Map(new EncoderPosition(testRA, testDec));
            Assert.AreEqual(expectedRa, result.EncoderPosition.RA, 120, "RA result is incorrect");
            Assert.AreEqual(expectedDec, result.EncoderPosition.Dec, 120, "Dec result is incorrect");
        }

        /*
        Get_EncoderHours [DataRow( 6.00557616960605 , 8388608 , 8388037 , 2457601 )] // (EQ_SphericalPolar)
        Get_EncoderDegrees [DataRow( 298.381592455407 , 9003008.25 , 9196760 , 2457601 )] // (EQ_SphericalPolar)
        Range360 [DataRow( 298.381592455407 )] // (EQ_SphericalPolar)
        hadec_aa [DataRow( 0.919235827154167 , 1.57225616639081 , 5.20774120974001 ,-0.775376033155811 , 3.86988561836677 )] // (EQ_SphericalPolar)
        [DataRow( 8388037 , 9196760 , 8673472.03453868 , 9625248.59910099 , 1 )] // (EQ_SphericalPolar)

                 * 
        Get_EncoderHours [DataRow( 7.3038666569553 , 8388608 , 8255092 , 2457601 )] // (EQ_SphericalPolar)
        Get_EncoderDegrees [DataRow( 596.661305069456 , 9003008.25 , 8775416 , 2457601 )] // (EQ_SphericalPolar)
        Range360 [DataRow( 236.661305069456 )] // (EQ_SphericalPolar)
        hadec_aa [DataRow( 0.919235827154167 , 1.91214782155489 , 4.13051898080895 ,-0.585620724448854 , 2.47103051434048 )] // (EQ_SphericalPolar)
        [DataRow( 8255092 , 8775416 , 8126324.76231087 , 9773690.13425487 , 1 )] // (EQ_SphericalPolar)

        Get_EncoderHours [DataRow( 2.10939611434078 , 8388608 , 8787006 , 2457601 )] // (EQ_SphericalPolar)
        Get_EncoderDegrees [DataRow( 544.180076424122 , 9003008.25 , 8417144 , 2457601 )] // (EQ_SphericalPolar)
        Range360 [DataRow( 184.180076424122 )] // (EQ_SphericalPolar)
        hadec_aa [DataRow( 0.919235827154167 , 0.552238611994933 , 3.21454874650256 ,-0.610002127045004 , 0.692316929152782 )] // (EQ_SphericalPolar)
        [DataRow( 8787006 , 8417144 , 7430599.89173006 , 9754617.08246391 , 1 )] // (EQ_SphericalPolar)
        
        Get_EncoderHours [DataRow( 9.09690303674193 , 8388608 , 8071485 , 2457601 )] // (EQ_SphericalPolar)
        Get_EncoderDegrees [DataRow( 360.214379795581 , 9003008.25 , 9618872 , 2457601 )] // (EQ_SphericalPolar)
        Range360 [DataRow( 0.214379795581124 )] // (EQ_SphericalPolar)
        hadec_aa [DataRow( 0.919235827154167 , 2.38156364862407 , 3.74163327836756E-03 ,-0.45178319762006 , 5.41095216511199 )] // (EQ_SphericalPolar)
        [DataRow( 8071485 , 9618872 , 9276243.79763711 , 9878388.38000644 , 1 )] // (EQ_SphericalPolar)         
         */

        [DataRow(8388037, 9196760, 8673472.03453868, 9625248.59910099, 1)]
        [DataRow(8255092, 8775416, 8126324.76231087, 9773690.13425487, 1)] // (EQ_SphericalPolar)
        [DataRow(8787006, 8417144, 7430599.89173006, 9754617.08246391, 1)] // (EQ_SphericalPolar)
        [DataRow(8071485, 9618872, 9276243.79763711, 9878388.38000644, 1)] // (EQ_SphericalPolar)    
        [DataTestMethod]
        public void Test_EQ_SphericalPolar(long ra, long dec, double expectedX, double expectedY, double expectedR)

        {
            _alignmentModel.ThreePointAlgorithm = ThreePointAlgorithmEnum.ClosestPoints;
            _alignmentModel.ActivePoints = ActivePointsEnum.All;

            EncoderPosition test = new EncoderPosition(ra, dec);
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("EQ_SphericalPolar", BindingFlags.NonPublic | BindingFlags.Instance);
            SphericalCoord result = (SphericalCoord)methodInfo.Invoke(_alignmentModel, new object[] { test });
            Assert.AreEqual(expectedX, result.x, 1d, "x result is incorrect");
            Assert.AreEqual(expectedY, result.y, 1d, "y result is incorrect");
            Assert.AreEqual(expectedR, result.r, 1d, "r result is incorrect");
        }

        [DataRow(509702.147272839, -0.545762684044915, 8388607.58399624, 9512710.14727313)] // 2457601  8388608  9003008.25 (EQ_plAffine)
        [DataTestMethod]
        public void Test_EQ_Polar2Cartes(double expectedRa, double expectedDec, double testX, double testY)

        {
            _alignmentModel.ThreePointAlgorithm = ThreePointAlgorithmEnum.ClosestPoints;
            _alignmentModel.ActivePoints = ActivePointsEnum.All;

            SphericalCoord test = new SphericalCoord(testX, testY, 0);
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("EQ_Polar2Cartes", BindingFlags.NonPublic | BindingFlags.Instance);
            CartesCoord result = (CartesCoord)methodInfo.Invoke(_alignmentModel, new object[] { test });
            Assert.AreEqual(expectedRa, result.x, doubleDelta, "x result is incorrect");
            Assert.AreEqual(expectedDec, result.y, doubleDelta, "y result is incorrect");
        }




        //    EQ_Transform_Affine[DataRow(509688.110229567, 43.3535014036799, 509701.89727284, -0.545762684044915, 1)] // (EQ_plAffine)


        [DataRow(8388641.26987594, 9512696.11207337, 0, 509688.110229567, 43.3535014036799, 1, 0)] // 2457601  8388608  9003008.25 (EQ_plAffine)
        [DataTestMethod]
        public void Test_EQ_Cartes2Polar(double expectedX, double expectedY, double expectedR, double testX, double testY, double testR,double testRa)
        {
            // EQ_Cartes2Polar(tmpobj3.X, tmpobj3.Y, tmpobj1.r, tmpobj1.RA, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos)
            _alignmentModel.ThreePointAlgorithm = ThreePointAlgorithmEnum.ClosestPoints;
            _alignmentModel.ActivePoints = ActivePointsEnum.All;

            CartesCoord test = new CartesCoord(testX, testY, 0d, 0d, 0d);
            CartesCoord rads = new CartesCoord(0d, 0d, 0d, testR, testRa);
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("EQ_Cartes2Polar", BindingFlags.NonPublic | BindingFlags.Instance);
            SphericalCoord result = (SphericalCoord)methodInfo.Invoke(_alignmentModel, new object[] { test, rads });
            Assert.AreEqual(expectedX, result.x, doubleDelta, "x result is incorrect");
            Assert.AreEqual(expectedY, result.y, doubleDelta, "y result is incorrect");
        }

        /*
        i / j  =  45.6245611530248 /-35.8465499073082 
        aa_hadec [DataRow( 0.919235827154167 ,-0.625640320648098 , 0.796298810987879 ,-2.51826869952598 ,-0.122171513161253 )]// (EQ_PolarSpherical)
        X/Y =  2.38092213307473 / 186.999912079268 
        [DataRow( 7471271.29753954 , 9742383.65611805 , 1 , 8759202 , 8436394 )] // (EQ_PolarSpherical)
        
        i / j  =  293.182350156892 /-35.2779827906899 
        aa_hadec [DataRow( 0.919235827154167 ,-0.615716952455876 , 5.11699731312566 , 2.24992160190372 ,-0.267520981472236 )]// (EQ_PolarSpherical)
        X/Y =  8.59406745883108 / 344.672176833943 
        [DataRow( 9161266.49146647 , 9750146.49564232 , 1 , 8122975 , 9512771 )] // (EQ_PolarSpherical)

        i / j  =  225.266546243132 /-15.7592732434377 
        aa_hadec [DataRow( 0.919235827154167 ,-0.275051205505143 , 3.93164292204616 , 1.07035991069787 ,-0.677340930938018 )]// (EQ_PolarSpherical)
        X/Y =  4.08847369169778 / 321.191223374651 
        [DataRow( 8697627.74809352 , 10016642.0517647 , 1 , 8584348 , 9352474 )] // (EQ_PolarSpherical)

        i / j  =  135.229150634789 /-31.6026716503086 
        aa_hadec [DataRow( 0.919235827154167 ,-0.551570672094294 , 2.36019392055553 ,-1.30546762821246 ,-0.900034929862018 )]// (EQ_PolarSpherical)
        X/Y =  7.01348097965497 / 231.568202883672 
        [DataRow( 8082972.21063669 , 9800326.76416405 , 1 , 8284828 , 8740647 )] // (EQ_PolarSpherical)

         */

        [DataRow(7471271.29753954, 9742383.65611805, 1, 8759202, 8436394)] // (EQ_PolarSpherical)
        [DataRow(9161266.49146647, 9750146.49564232, 1, 8122975, 9512771)] // (EQ_PolarSpherical)
        [DataRow(8697627.74809352, 10016642.0517647, 1, 8584348, 9352474)] // (EQ_PolarSpherical)
        [DataRow(8082972.21063669, 9800326.76416405, 1, 8284828, 8740647)] // (EQ_PolarSpherical)
        [DataTestMethod]
        public void Test_EQ_PolarSpherical(double ra, double dec, long range, double testX, double testY)

        {
            _alignmentModel.ThreePointAlgorithm = ThreePointAlgorithmEnum.ClosestPoints;
            _alignmentModel.ActivePoints = ActivePointsEnum.All;

            SphericalCoord test = new SphericalCoord(ra, dec, 0d);
            SphericalCoord rads = new SphericalCoord(0d, 0d, range);
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("EQ_PolarSpherical", BindingFlags.NonPublic | BindingFlags.Instance);
            EncoderPosition result = (EncoderPosition)methodInfo.Invoke(_alignmentModel, new object[] { test, rads });
            Assert.AreEqual(testX, result.RA, 1d, "x result is incorrect");
            Assert.AreEqual(testY, result.Dec, 1d, "y result is incorrect");
        }



        [DataRow(7.17370069429496, 8388608, 8268421, 2457601)] // (EQ_SphericalPolar)
        [DataRow(6.00557616960605, 8388608, 8388037, 2457601)]
        [DataRow(7.3038666569553, 8388608, 8255092, 2457601)]
        [DataRow(9.09690303674193, 8388608, 8071485, 2457601)]
        [DataRow(2.10939611434078, 8388608, 8787006, 2457601)]
        [DataTestMethod]
        public void Test_Get_EncoderHours(double expectedHrs, long raCentre, long encoderRa, long stepsPerRev)
        {
            // Get_EncoderHours(RACENTER, RA, TOT, 0)
            Assert.AreEqual(_alignmentModel.HomeEncoder.RA, raCentre, "RA Home position is incorrect.");
            Assert.AreEqual(_alignmentModel.StepsPerRev.RA, stepsPerRev, "RA Steps Per 360 is incorrect");
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("Get_EncoderHours", BindingFlags.NonPublic | BindingFlags.Instance);
            double result = (double)methodInfo.Invoke(_alignmentModel, new object[] { encoderRa });
            Assert.AreEqual(expectedHrs, result, doubleDelta, "Resultant hours are incorrect");
        }

        [DataRow(246.372629242908, 9003008, 8841712, 2457601)] // (EQ_SphericalPolar)
        [DataRow(298.381592455407, 9003008, 9196760, 2457601)]
        [DataRow(236.661305069456, 9003008, 8775416, 2457601)]
        [DataRow(184.180076424122, 9003008, 8417144, 2457601)]
        [DataRow(0.214379795581124, 9003008, 9618872, 2457601)]
        [DataTestMethod]
        public void Test_Get_EncoderDegrees(double expectedDegrees, long decCentre, long encoderDec, long stepsPerRev)
        {
            // j = Get_EncoderDegrees(DECCENTER, DEC, TOT, 0) + 270
            Assert.AreEqual(_alignmentModel.HomeEncoder.Dec, decCentre, "Dec Home position is incorrect.");
            Assert.AreEqual(_alignmentModel.StepsPerRev.Dec, stepsPerRev, "Dec Steps Per 360 is incorrect");
            Type type = _alignmentModel.GetType();
            MethodInfo methodInfo = type.GetMethod("Get_EncoderDegrees", BindingFlags.NonPublic | BindingFlags.Instance);
            double result = (double)methodInfo.Invoke(_alignmentModel, new object[] { encoderDec });
            result = Range.Range360(result + 270d);
            Assert.AreEqual(expectedDegrees, result, doubleDelta, "Resultant degrees are incorrect");
        }



    }
}



