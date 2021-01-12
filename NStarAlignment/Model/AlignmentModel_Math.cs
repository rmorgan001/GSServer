using NStarAlignment.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NStarAlignment.Model

{
    public partial class AlignmentModel
    {

        private CarteseanCoordinate DeltaMap(double[] currentAxis)
        {
            return new CarteseanCoordinate(currentAxis[0] + OneStarAdjustment[0], currentAxis[1] + OneStarAdjustment[1]);
        }
        private CarteseanCoordinate DeltaReverseMap(double[] currentAxis)
        {
            return new CarteseanCoordinate(currentAxis[0] - OneStarAdjustment[0], currentAxis[1] - OneStarAdjustment[1]);
        }


        private CarteseanCoordinate DeltaSyncMatrixMap(double[] targetAxis, TimeRecord time)
        {
            var result = new CarteseanCoordinate(targetAxis[0], targetAxis[1]);


            if (AlignmentPoints.Count > 0)
            {
                var i = GetNearest(targetAxis, time);

                if (i != -1)
                {
                    result[0] = targetAxis[0] + (_mountAxes[i].RaAxis - _skyAxes[i].RaAxis);
                    result[1] = targetAxis[1] + (_mountAxes[i].DecAxis - _skyAxes[i].DecAxis);
                }
            }

            return result;
        }

        private CarteseanCoordinate DeltaSyncReverseMatrixMap(double[] targetAxis, TimeRecord time)
        {
            var result = new CarteseanCoordinate(targetAxis[0], targetAxis[1]);


            if (AlignmentPoints.Count > 0)
            {
                var i = GetNearest(targetAxis, time);

                if (i != -1)
                {
                    result[0] = targetAxis[0] - (_mountAxes[i].RaAxis - _skyAxes[i].RaAxis);
                    result[1] = targetAxis[1] - (_mountAxes[i].DecAxis - _skyAxes[i].DecAxis);
                }
            }

            return result;
        }


      private CarteseanCoordinate DeltaMatrixMap(double[] targetAxes, TimeRecord time)
      {
         // re transform based on the nearest 3 stars
         var updateSuccess = UpdateMatrixForSky(targetAxes, time);

         var result = TransformCoordinate(targetAxes, time);

         result.Z = 1;
         result.Flag = updateSuccess;

         return result;
      }

      private CarteseanCoordinate DeltaMatrixReverseMap(double[] targetAxes, TimeRecord time)
        {

            // re transform based on the nearest 3 stars
            var updateSuccess = UpdateMatrixForMount(targetAxes, time);

            var result = TransformCoordinate(targetAxes, time);

            result.Z = 1;
            result.Flag = updateSuccess;

            return result;
        }



        #region Utility methods
        private int GetNearest(double[] targetAxis, TimeRecord time)
        {
            var choices = new List<KeyValuePair<int, double>>();

            var cartesean  = AxesToCartesean(new AxisPosition(targetAxis), time);
            for (var i = 0; i < AlignmentPoints.Count; i++)
            {
                switch (PointFilterMode)
                {
                    case PointFilterMode.AllPoints:
                        // all points 

                        break;
                    case PointFilterMode.Meridian:
                        // only consider points on this side of the meridian 
                        if (_skyAxisCoordinates[i].Y * cartesean.Y < 0)
                        {
                            continue;
                        }

                        break;
                    case PointFilterMode.LocalQuadrant:
                        // local quadrant 
                        if (cartesean.Quadrant != _skyAxisCoordinates[i].Quadrant)
                        {
                            continue;
                        }

                        break;
                }

                choices.Add(LocalToPier
                    ? new KeyValuePair<int, double>(i,
                        Math.Pow(_skyAxes[i].RaAxis - targetAxis[0], 2) +
                        Math.Pow(_skyAxes[i].DecAxis - targetAxis[1], 2))
                    : new KeyValuePair<int, double>(i,
                        Math.Pow(_skyAxisCoordinates[i].X - cartesean.X, 2) + Math.Pow(_skyAxisCoordinates[i].Y - cartesean.Y, 2)));
            }

            if (choices.Count == 0)
            {
                // No suitable points found.
                return -1;
            }
            else
            {
                // Replace original call to EQ_FindLowest
                return choices.OrderBy(c => c.Value).Select(c => c.Key).FirstOrDefault();
            }
        }

        #endregion

    }
}
