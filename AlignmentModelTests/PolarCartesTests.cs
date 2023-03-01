using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using GS.Server.SkyTelescope;

namespace AlignmentModelTests
{
    [TestClass]
    public class PolarCartesTest
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
            _alignmentModel = new AlignmentModel(SiteLatitude, SiteLongitude, SiteElevation);
            _alignmentModel.Connect(RaHome, DecHome, SkyServer.StepsPerRevolution);
        }

        /// <summary>
        /// Test the polar=>cartesian=>polar round tip
        /// </summary>
        /// <param name="testX"></param>
        /// <param name="testY"></param>
        [DataRow(14501588.4734624, 17527728.0326762)]
        [DataRow(4024181.81564379, 17524500.3350093)]
        [DataRow(4126168.61599776, 18046139.9964502)]
        [DataRow(4344203.44721392, 17199887.2258348)]
        [DataRow(4366381.30549882, 19245147.6203932)]
        [DataRow(3998703.12753902, 19015186.7410018)]
        [DataRow(14137677.3873764, 17652660.2094915)]
        [DataRow(14511919.9276025, 17447495.0667899)]
        [DataRow(7436925.01308934, 19006524.4961878)]
        [DataRow(7432815.42282481, 18714847.0472273)]
        [DataRow(6852847.11142045, 18359868.1724321)]
        [DataRow(9981959.94393325, 20776066.4402011)]
        [DataRow(12253584.6011247, 19611013.1941131)]
        [DataRow(11170002.8731509, 19030684.0924693)]
        [DataRow(11360902.2465664, 19916367.8300746)]
        [DataTestMethod]
        public void Test_EQ_Polar2Cartes(double testX, double testY)
        {
            SphericalCoord test = new SphericalCoord(testX, testY);
            Type type = _alignmentModel.GetType();
            MethodInfo p2cMethodInfo = type.GetMethod("EQ_Polar2Cartes", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo c2pMethodInfo = type.GetMethod("EQ_Cartes2Polar", BindingFlags.NonPublic | BindingFlags.Instance);
            if (p2cMethodInfo == null) return;
            CartesCoord cResult = (CartesCoord)p2cMethodInfo.Invoke(_alignmentModel, new object[] { test });
            SphericalCoord sResult = (SphericalCoord)c2pMethodInfo.Invoke(_alignmentModel, new object[] { cResult, cResult });

            Assert.AreEqual(test.x, sResult.x, DoubleDelta, "Xs differ.");
            Assert.AreEqual(test.y, sResult.y, DoubleDelta, "Ys differ.");

        }






    }
}
