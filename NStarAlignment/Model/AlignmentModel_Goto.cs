using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;

namespace NStarAlignment.Model
{
    public partial class AlignmentModel
    {
        private readonly List<AlignmentPoint> _selectedPoints = new List<AlignmentPoint>();


        #region Going from calculated to synched axis
        /// <summary>
        /// Gets the observed axis positions for a given mount axis position.
        /// </summary>
        /// <param name="mountAxes">Mount Ra and Dec axis positions</param>
        /// <param name="pierSide">The pier side to use 0 = East, 1 = West, 2 = Unknown</param>
        /// <returns></returns>
        public double[] GetObservedAxes(double[] mountAxes, int pierSide)
        {
            if (!IsAlignmentOn || !AlignmentPoints.Any()) return mountAxes;   // Fast exit as alignment modeling is switched off or there are no points.

            lock (_accessLock)
            {
                ClearSelectedPoints();

                AxisPosition mAxes = new AxisPosition(mountAxes);
                if (mAxes.IncludedAngleTo(_homePosition) < ProximityLimit) return mountAxes;    // Fast exist if we are going home.
                RaiseNotification(NotificationType.Information, $"GetObservedAxes for {mountAxes[0]}/{mountAxes[1]}");
                PierSide pSide = (PierSide)pierSide;
                WriteLastAccessTime();

                Matrix offsets = Matrix.CreateInstance(1, 2);
                if (AlignmentPoints.Count == 1)
                {
                    offsets[0, 0] = AlignmentPoints[0].ObservedAxes[0] - AlignmentPoints[0].MountAxes[0];
                    offsets[0, 1] = AlignmentPoints[0].ObservedAxes[1] - AlignmentPoints[0].MountAxes[1];
                    AlignmentPoints[0].Selected = true;
                    _selectedPoints.Add(AlignmentPoints[0]);
                    RaiseNotification(NotificationType.Data, $"Single alignment point selected {AlignmentPoints[0].Id:D3}, Mount axes: {AlignmentPoints[0].MountAxes.RaAxis}/{AlignmentPoints[0].MountAxes.RaAxis}, Observed axes: {AlignmentPoints[0].ObservedAxes.RaAxis}/{AlignmentPoints[0].ObservedAxes.RaAxis}");
                }
                else
                {
                    IEnumerable<AlignmentPoint> nearestPoints = GetNearestMountPoints(mAxes, pSide, SampleSize);
                    var alignmentPoints = nearestPoints as AlignmentPoint[] ?? nearestPoints.ToArray();
                    int rows = alignmentPoints.Length;

                    if (rows > 2)
                    {
                        // Build features and values from registered points
                        Matrix features = Matrix.CreateInstance(rows, 3);
                        Matrix values = Matrix.CreateInstance(rows, 2);

                        _stringBuilder.Clear();
                        _stringBuilder.Append("Points chosen are");
                        for (int i = 0; i < rows; i++)
                        {
                            var pt = alignmentPoints[i];
                            _stringBuilder.Append($" ({pt.Id:D3})");
                            features[i, 0] = 1f;
                            features[i, 1] = pt.MountAxes[0] * pt.MountAxes[0];
                            features[i, 2] = pt.MountAxes[1] * pt.MountAxes[1];
                            values[i, 0] = Range.RangePlusOrMinus180(pt.ObservedAxes[0] - pt.MountAxes[0]);
                            values[i, 1] = Range.RangePlusOrMinus180(pt.ObservedAxes[1] - pt.MountAxes[1]);
                            pt.Selected = true;
                            _selectedPoints.Add(pt);
                        }

                        _stringBuilder.AppendLine(".");



                        // Solve the normal equation to get theta
                        Matrix theta = SolveNormalEquation(features, values);

                        // Calculate the difference for the incoming points
                        Matrix target = Matrix.CreateInstance(1, 3);
                        target[0, 0] = 1f;
                        target[0, 1] = mAxes[0] * mAxes[0];
                        target[0, 2] = mAxes[1] * mAxes[1];

                        offsets = target * theta;


                        _stringBuilder.AppendLine("Features");
                        _stringBuilder.AppendLine(features.ToString());
                        _stringBuilder.AppendLine("Values");
                        _stringBuilder.AppendLine(values.ToString());
                        _stringBuilder.AppendLine("Theta");
                        _stringBuilder.AppendLine(theta.ToString());
                        _stringBuilder.AppendLine("Target");
                        _stringBuilder.AppendLine(target.ToString());
                        _stringBuilder.AppendLine("Offsets");
                        _stringBuilder.AppendLine(offsets.ToString());
                        RaiseNotification(NotificationType.Data, _stringBuilder.ToString());
                        _stringBuilder.Clear();
                    }
                    else if (rows > 0)
                    {
                        // Just use the nearest point of the two.
                        offsets[0, 0] = alignmentPoints[0].ObservedAxes[0] - alignmentPoints[0].MountAxes[0];
                        offsets[0, 1] = alignmentPoints[0].ObservedAxes[1] - alignmentPoints[0].MountAxes[1];
                        alignmentPoints[0].Selected = true;
                        _selectedPoints.Add(alignmentPoints[0]);
                        RaiseNotification(NotificationType.Data, $"Using nearest point of two {alignmentPoints[0].Id:D3}, Mount axes: {alignmentPoints[0].MountAxes.RaAxis}/{alignmentPoints[0].MountAxes.RaAxis}, Observed axes: {alignmentPoints[0].ObservedAxes.RaAxis}/{alignmentPoints[0].ObservedAxes.RaAxis}");

                    }
                    // Otherwise default to using no correcting offset 

                }
                RaiseNotification(NotificationType.Data, $"Correction -> Observer = {offsets[0, 0]}/{offsets[0, 1]}");
                var observedAxes = new[]
                {
                    mountAxes[0] + offsets[0, 0],
                    mountAxes[1] + offsets[0, 1]
                };
                RaiseNotification(NotificationType.Information, $"Mount axes: {mountAxes[0]}/{mountAxes[1]} -> Observed axes: {observedAxes[0]}/{observedAxes[1]}");

                return observedAxes;
            }
        }


        #endregion


        #region Going from observed position to mount position ...
        /// <summary>
        /// Gets the mount axis positions for a given observed axis position.
        /// </summary>
        /// <param name="observedAxes">Observed Ra and Dec axis positions</param>
        /// <param name="pierSide">The pier side to use 0 = East, 1 = West, 2 = Unknown</param>
        /// <returns></returns>
        public double[] GetMountAxes(double[] observedAxes, int pierSide)
        {
            if (!IsAlignmentOn || !AlignmentPoints.Any()) return observedAxes; // Fast exit as alignment modeling is switched off or there are no points.

            lock (_accessLock)
            {
                ClearSelectedPoints();
                AxisPosition sAxes = new AxisPosition(observedAxes);
                if (sAxes.IncludedAngleTo(_homePosition) < ProximityLimit) return observedAxes;    // Fast exit if we are going home.
                RaiseNotification(NotificationType.Information, $"GetMountAxes for {observedAxes[0]}/{observedAxes[1]}");

                PierSide pSide = (PierSide)pierSide;
                WriteLastAccessTime();
                Matrix offsets = Matrix.CreateInstance(1, 2);
                if (AlignmentPoints.Count == 1)
                {
                    offsets[0, 0] = AlignmentPoints[0].MountAxes[0] - AlignmentPoints[0].ObservedAxes[0];
                    offsets[0, 1] = AlignmentPoints[0].MountAxes[1] - AlignmentPoints[0].ObservedAxes[1];
                    AlignmentPoints[0].Selected = true;
                    _selectedPoints.Add(AlignmentPoints[0]);
                    RaiseNotification(NotificationType.Data, $"Single alignment point selected {AlignmentPoints[0].Id:D3}, Mount axes: {AlignmentPoints[0].MountAxes.RaAxis}/{AlignmentPoints[0].MountAxes.RaAxis}, Observed axes: {AlignmentPoints[0].ObservedAxes.RaAxis}/{AlignmentPoints[0].ObservedAxes.RaAxis}");
                }
                else
                {
                    IEnumerable<AlignmentPoint> nearestPoints = GetNearestObservedPoints(sAxes, pSide, SampleSize);
                    var alignmentPoints = nearestPoints as AlignmentPoint[] ?? nearestPoints.ToArray();
                    int rows = alignmentPoints.Length;

                    if (rows > 2)
                    {
                        // Build features and values from registered points
                        Matrix features = Matrix.CreateInstance(rows, 3);
                        Matrix values = Matrix.CreateInstance(rows, 2);
                        _stringBuilder.Clear();
                        _stringBuilder.Append("Points chosen are");
                        for (int i = 0; i < rows; i++)
                        {
                            var pt = alignmentPoints[i];
                            _stringBuilder.Append($" ({pt.Id:D3})");
                            features[i, 0] = 1f;
                            features[i, 1] = pt.ObservedAxes[0] * pt.ObservedAxes[0];
                            features[i, 2] = pt.ObservedAxes[1] * pt.ObservedAxes[1];
                            values[i, 0] = Range.RangePlusOrMinus180(pt.MountAxes[0] - pt.ObservedAxes[0]);
                            values[i, 1] = Range.RangePlusOrMinus180(pt.MountAxes[1] - pt.ObservedAxes[1]);
                            pt.Selected = true;
                            _selectedPoints.Add(pt);
                        }
                        _stringBuilder.AppendLine(".");



                        // Solve the normal equation to get theta
                        Matrix theta = SolveNormalEquation(features, values);

                        // Calculate the difference for the incoming points
                        Matrix target = Matrix.CreateInstance(1, 3);
                        target[0, 0] = 1f;
                        target[0, 1] = sAxes[0] * sAxes[0];
                        target[0, 2] = sAxes[1] * sAxes[1];

                        offsets = target * theta;


                        _stringBuilder.AppendLine("Features");
                        _stringBuilder.AppendLine(features.ToString());
                        _stringBuilder.AppendLine("Values");
                        _stringBuilder.AppendLine(values.ToString());
                        _stringBuilder.AppendLine("Theta");
                        _stringBuilder.AppendLine(theta.ToString());
                        _stringBuilder.AppendLine("Target");
                        _stringBuilder.AppendLine(target.ToString());
                        _stringBuilder.AppendLine("Offsets");
                        _stringBuilder.AppendLine(offsets.ToString());
                        RaiseNotification(NotificationType.Data, _stringBuilder.ToString());
                        _stringBuilder.Clear();
                    }
                    else if (rows > 0)
                    {
                        // Use the nearest point of the two.
                        offsets[0, 0] = alignmentPoints[0].MountAxes[0] - alignmentPoints[0].ObservedAxes[0];
                        offsets[0, 1] = alignmentPoints[0].MountAxes[1] - alignmentPoints[0].ObservedAxes[1];
                        alignmentPoints[0].Selected = true;
                        _selectedPoints.Add(alignmentPoints[0]);
                        RaiseNotification(NotificationType.Data, $"Using nearest point of two {alignmentPoints[0].Id:D3}, Mount axes: {alignmentPoints[0].MountAxes.RaAxis}/{alignmentPoints[0].MountAxes.RaAxis}, Observed axes: {alignmentPoints[0].ObservedAxes.RaAxis}/{alignmentPoints[0].ObservedAxes.RaAxis}");

                    }
                    // Otherwise default to using zero offset
                }
                RaiseNotification(NotificationType.Data, $"Correction -> Mount = {offsets[0, 0]}/{offsets[0, 1]}");

                var mountAxes = new[]
                {
                    observedAxes[0] + offsets[0, 0],
                    observedAxes[1] + offsets[0, 1]
                };
                RaiseNotification(NotificationType.Information, $"Observed axes: {observedAxes[0]}/{observedAxes[1]} -> Mount axes: {mountAxes[0]}/{mountAxes[1]}");
                return mountAxes;
            }
        }

        #endregion


        #region Support methods ...

        private void ClearSelectedPoints()
        {
            foreach (AlignmentPoint pt in _selectedPoints)
            {
                pt.Selected = false;
            }
            _selectedPoints.Clear();
        }

        private IEnumerable<AlignmentPoint> GetNearestMountPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.MountAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.MountAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints);
        }

        private IEnumerable<AlignmentPoint> GetNearestObservedPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.ObservedAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.ObservedAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints);
        }

        public static Matrix SolveNormalEquation(Matrix inputFeatures, Matrix outputValue)
        {
            Matrix inputFeaturesT = inputFeatures.Transpose();
            Matrix xx = inputFeaturesT * inputFeatures;
            Matrix xxi = xx.Invert();

            Matrix xy = inputFeaturesT * outputValue;

            Matrix result = xxi * xy;
            return result;
        }
        #endregion

    }
}
