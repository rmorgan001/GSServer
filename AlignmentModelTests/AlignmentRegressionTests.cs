using System;
using System.Collections.Generic;
using System.Reflection;
using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlignmentModelTests
{
    [TestClass]
    [DoNotParallelize]
    public class AlignmentRegressionTests
    {
        private AlignmentModel model;

        [TestInitialize]
        public void Initialize()
        {
            model = new AlignmentModel(0, 0, 0) { ActivePoints = ActivePointsEnum.All, ThreePointAlgorithm = ThreePointAlgorithmEnum.BestCentre };
        }

        private object Call(string name, params object[] args) =>
            typeof(AlignmentModel).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(model, args);

        private void Add(double x, double y, double sx, double sy)
        {
            model.AlignmentPoints.Add(new AlignmentPoint
            {
                UnsyncedCartesian = new Coord { x = x, y = y },
                SyncedCartesian = new Coord { x = sx, y = sy }
            });
        }

        [TestMethod]
        public void NearestUsesTheInputCoordinateSpace()
        {
            Add(0, 0, 100, 0);
            Add(100, 0, 200, 0);
            var unsynced = (AlignmentPoint)Call("GetNearest", new CartesCoord(100, 0), false);
            var synced = (AlignmentPoint)Call("GetNearest", new CartesCoord(100, 0), true);
            Assert.AreSame(model.AlignmentPoints[1], unsynced);
            Assert.AreSame(model.AlignmentPoints[0], synced);
        }

        [TestMethod]
        public void NearestCorrectionRoundTripsAStoredAlignmentPoint()
        {
            Add(10, 20, 15, 23);
            var forward = (MapResult)Call("DeltaReverse_Map", new CartesCoord(10, 20));
            Assert.AreEqual(15.0, forward.Position.x, 1e-8);
            Assert.AreEqual(23.0, forward.Position.y, 1e-8);
            var reverse = (MapResult)Call("Delta_Map", forward.Position);
            Assert.AreEqual(10.0, reverse.Position.x, 1e-8);
            Assert.AreEqual(20.0, reverse.Position.y, 1e-8);
        }

        [TestMethod]
        public void DegenerateTrianglesDoNotProduceAMatrixCorrection()
        {
            Add(0, 0, 100, 100);
            Add(10, 0, 110, 100);
            Add(20, 0, 120, 100);
            var mapped = (MapResult)Call("Delta_Matrix_Map", new CartesCoord(5, 0));
            Assert.IsFalse(mapped.InTriangle);
        }

        [TestMethod]
        public void ReverseTriangleUsesSyncedCoordinatesAndSkipsDegeneratePoints()
        {
            Add(0, 0, 100, 100);
            Add(10, 0, 110, 100);
            Add(20, 0, 120, 100);
            Add(0, 10, 100, 110);
            var triangle = (List<AlignmentPoint>)Call("EQ_Choose_3Points", new CartesCoord(102, 102), true);
            Assert.AreEqual(3, triangle.Count);
            var mapped = (MapResult)Call("Delta_Matrix_Reverse_Map", new CartesCoord(102, 102));
            Assert.IsTrue(mapped.InTriangle);
            Assert.AreEqual(2.0, mapped.Position.x, 1e-8);
            Assert.AreEqual(2.0, mapped.Position.y, 1e-8);
            var forward = (MapResult)Call("Delta_Matrix_Map", mapped.Position);
            Assert.IsTrue(forward.InTriangle);
            Assert.AreEqual(102.0, forward.Position.x, 1e-8);
            Assert.AreEqual(102.0, forward.Position.y, 1e-8);
        }

        [TestMethod]
        public void ThreePointStateTracksCollectionSizeWithoutInvertingFirstTriangle()
        {
            Add(0, 0, 0, 0); Add(1, 0, 1, 0); Add(2, 0, 2, 0);
            model.SendToMatrix();
            var field = typeof(AlignmentModel).GetField("_threeStarEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue((bool)field.GetValue(model));
            model.AlignmentPoints.RemoveAt(0);
            model.SendToMatrix();
            Assert.IsFalse((bool)field.GetValue(model));
        }

        [TestMethod]
        public void PoleCorrectionIsNotDiscarded()
        {
            typeof(AlignmentModel).GetProperty("StepsPerRev").SetValue(model, new long[] { 3600, 3600 });
            long[] center = model.ScaleCenter;
            var original = (CartesCoord)Call("EQ_Polar2Cartes", new SphericalCoord(center[0], center[1]));
            var unchanged = (SphericalCoord)Call("EQ_Cartes2Polar", original, original);
            var corrected = (SphericalCoord)Call("EQ_Cartes2Polar", new CartesCoord(original.x + 10, original.y), original);
            Assert.AreEqual((double)center[1], unchanged.y, 1e-8);
            Assert.AreEqual(center[1] + 10.0, corrected.y, 1e-8);
        }
    }
}
