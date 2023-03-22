using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Reflection;
using GS.Server.SkyTelescope;

namespace AlignmentModelTests
{
    [TestClass]
    public class SphericalPolarTest
    {
        private const double SiteLatitude = 56d;
        private const double SiteLongitude = -5d;
        private const double SiteElevation = 10d;
        private const double DecHome = 90d;
        private const double RaHome = 90d;
        private const double DoubleDelta = 0.0001;

        private AlignmentModel _alignmentModel;

        [TestInitialize]
        public void Initialize()
        {
            _alignmentModel = SkyServer.AlignmentModel;
            _alignmentModel.Connect(RaHome, DecHome, SkyServer.StepsPerRevolution);
        }

        //[DataRow(134.307342530302, 55.7319439806278)]
        //[DataRow(63.7072912837969, 130.765779672163)]
        //[DataRow(73.7963078628305, 126.429440743229)]
        //[DataRow(69.5094535018456, 138.971945042359)]
        //[DataRow(103.280927760896, 119.356817705725)]
        //[DataRow(90.0485961716324, 115.457326067169)]
        //[DataRow(119.721777456959, 52.8103789020268)]
        //[DataRow(135.566546400331, 54.8419698722453)]
        //[DataRow(150.881763483797, 158.317946374478)]
        //[DataRow(149.075523365393, 161.99654546719)]
        //[DataRow(135.566546400331, 161.015757545441)]
        //[DataRow(9.98312045419137, 41.5784877238403)]
        //[DataRow(49.6139950035978, 44.8784207024449)]
        //[DataRow(39.5056752076461, 27.1335995026076)]
        //[DataRow(33.7936498434583, 38.8532364127109)]
        [DataRow(4.06386877968907, 3.33107709884644)]
        [DataTestMethod]
        public void Test_EQ_SphericalPolar(double testRA, double testDec)
        {
            Type type = _alignmentModel.GetType();

            MethodInfo sPmethodInfo = type.GetMethod("EQ_SphericalPolar", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo pSmethodInfo = type.GetMethod("EQ_PolarSpherical", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sPmethodInfo == null || pSmethodInfo == null) return;

            AxisPosition spherical = new AxisPosition(testRA, testDec);
            SphericalCoord pResult = (SphericalCoord)sPmethodInfo.Invoke(_alignmentModel, new object[] { spherical });
            Debug.Write($"[DataRow({pResult.x}, {pResult.y})]");
            AxisPosition sResult =
                (AxisPosition)pSmethodInfo.Invoke(_alignmentModel, new object[] { pResult });

            Assert.AreEqual(testRA, sResult.RA, DoubleDelta, "RA is correct");
            Assert.AreEqual(testDec, sResult.Dec, DoubleDelta, "Dec is correct");


        }


        public void Test_FullRoundTrip(int id, double unsyncedRA, double unsyncedDec, double unsynchedX,
            double unsyncedY, double syncedRA, double syncedDec, double synchedX, double syncedY, string syncTime)
        {

        }

    }
}
