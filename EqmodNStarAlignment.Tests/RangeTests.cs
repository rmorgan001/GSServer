using EqmodNStarAlignment.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace EqmodNStarAlignment.Tests
{
    [TestClass]
    public class RangeTests
    {
        [DataRow(3d, 3d, 6d)]
        [DataRow(3d, 9d, 6d)]
        [DataRow(0d, 0d, 6d)]
        [DataRow(3d, -3d, 6d)]
        [DataRow(3d, -9d, 6d)]
        [DataTestMethod]
        public void TestRangeValue(double expectedValue, double testValue, double range)
        {
            Assert.AreEqual(expectedValue, Range.ZeroToValue(testValue, range));
        }
    }
}
