using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NStarAlignment.DataTypes;
using NStarAlignment.Model;

namespace NStarAlignment.Tests
{
    [TestClass]
    public class MatrixTests
    {
        [TestMethod]
        public void TestInvert()
        {
            double[][] values = new[]
            {
                new[] {1.00000, 18894.75675, 14226.78803},
                new[] {1.00000, 12831.49749, 16721.15369},
                new[] {1.00000, 9661.92668, 13113.96006}
            };
            double[][] expectedValues = new[]
            {
                new[] {0.225445485, -3.705052597, 4.479607112},
                new[] {0.000121139, -0.000037372, -0.000083767},
                new[] {-0.000106442, 0.000310061, -0.000203619}
            };


            Matrix initial = Matrix.CreateInstance(values);
            System.Diagnostics.Debug.WriteLine(initial.ToString());
            Matrix expected = Matrix.CreateInstance(expectedValues);
            System.Diagnostics.Debug.WriteLine(expected.ToString());

            Matrix inverse = initial.Invert();
            System.Diagnostics.Debug.WriteLine(inverse.ToString());
            Assert.IsTrue(inverse.IsEqualTo(expected, 0.0001));
        }


        [TestMethod]
        public void TestSolveNormalEquation()
        {
            double[][] featuresData = new[]
            {
                new[] {1.00000, 18894.75675, 14226.78803},
                new[] {1.00000, 12831.49749, 16721.15369},
                new[] {1.00000, 9661.92668, 13113.96006}
            };

            double[][] valuesdata = new[]
            {
                new[] {2.1449, 2.1487},
                new[] {3.1759, 1.5399},
                new[] {4.9241, 1.6036}
            };

            double[][] expectedData = new[]
            {
                new[] {10.77471486, 1.962502184},
                new []{-0.00027134, 0.000068413},
                new []{-0.00024623, -0.000077773 }
            };
            Matrix samples = Matrix.CreateInstance(featuresData);
            Matrix values = Matrix.CreateInstance(valuesdata);
            Matrix expected = Matrix.CreateInstance(expectedData);
            Matrix result = AlignmentModel.SolveNormalEquation(samples, values);
            System.Diagnostics.Debug.WriteLine(samples.ToString());
            System.Diagnostics.Debug.WriteLine(values.ToString());
            System.Diagnostics.Debug.WriteLine(expected.ToString());
            System.Diagnostics.Debug.WriteLine(result.ToString());
            Assert.IsTrue(result.IsEqualTo(expected, 0.0001));


        }
    }
}




