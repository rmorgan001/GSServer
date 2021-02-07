/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;

namespace NStarAlignment.Model
{
    public partial class AlignmentModel
    {
        /// <summary>
        /// Checksum used as a quick to see if the seleted points have changed
        /// </summary>
        private int _currentChecksum = int.MinValue;

        /// <summary>
        /// The cached offsets linked with the current checksum 
        /// </summary>
        private Matrix _lastOffsets = Matrix.CreateInstance(1, 2);

        /// <summary>
        /// List of selected alignment points.
        /// </summary>
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
                    AlignmentPoint[] alignmentPoints = GetNearestMountPoints(mAxes, pSide, SampleSize);
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
                AxisPosition sAxes = new AxisPosition(observedAxes);
                if (sAxes.IncludedAngleTo(_homePosition) < ProximityLimit) return observedAxes;    // Fast exit if we are going home.
                RaiseNotification(NotificationType.Information, $"GetMountAxes for {observedAxes[0]}/{observedAxes[1]}");

                PierSide pSide = (PierSide)pierSide;
                WriteLastAccessTime();
                Matrix offsets = Matrix.CreateInstance(1, 2);
                int checksum;

                if (AlignmentPoints.Count == 1)
                {
                    checksum = GetChecksum(AlignmentPoints[0].Id);
                    if (checksum == _currentChecksum)
                    {
                        // Checksum hasn't changed so use the last offsets
                        offsets = _lastOffsets;
                    }
                    else
                    {
                        ClearSelectedPoints();
                        offsets[0, 0] = AlignmentPoints[0].MountAxes[0] - AlignmentPoints[0].ObservedAxes[0];
                        offsets[0, 1] = AlignmentPoints[0].MountAxes[1] - AlignmentPoints[0].ObservedAxes[1];
                        AlignmentPoints[0].Selected = true;
                        _selectedPoints.Add(AlignmentPoints[0]);
                        // Cache the offsets and checksum
                        _lastOffsets = offsets;
                        _currentChecksum = checksum;
                        RaiseNotification(NotificationType.Data,
                            $"Single alignment point selected {AlignmentPoints[0].Id:D3}, Mount axes: {AlignmentPoints[0].MountAxes.RaAxis}/{AlignmentPoints[0].MountAxes.RaAxis}, Observed axes: {AlignmentPoints[0].ObservedAxes.RaAxis}/{AlignmentPoints[0].ObservedAxes.RaAxis}");
                    }
                }
                else
                {
                    // Get the nearest points and their corresponding checksum value
                    AlignmentPoint[] alignmentPoints = GetNearestObservedPoints(sAxes, pSide, SampleSize, out checksum);
                    if (checksum == _currentChecksum)
                    {
                        // Checksum hasn't changed to use the last offsets
                        offsets = _lastOffsets;
                    }
                    else
                    {
                        int rows = alignmentPoints.Length;
                        if (rows > 2)
                        {
                            ClearSelectedPoints();
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

                            // Cache the offsets and the checksum
                            _lastOffsets = offsets;
                            _currentChecksum = checksum;
                        }
                        else if (rows > 0)
                        {
                            checksum = GetChecksum(AlignmentPoints[0].Id);
                            if (checksum == _currentChecksum)
                            {
                                // Checksum hasn't changed so use the last offsets
                                offsets = _lastOffsets;
                            }
                            else
                            {
                                ClearSelectedPoints();
                                // Use the nearest point of the two.
                                offsets[0, 0] = alignmentPoints[0].MountAxes[0] - alignmentPoints[0].ObservedAxes[0];
                                offsets[0, 1] = alignmentPoints[0].MountAxes[1] - alignmentPoints[0].ObservedAxes[1];
                                alignmentPoints[0].Selected = true;
                                _selectedPoints.Add(alignmentPoints[0]);
                                // Cache the offsets and checksum
                                _lastOffsets = offsets;
                                _currentChecksum = checksum;
                                RaiseNotification(NotificationType.Data,
                                    $"Using nearest point of two {alignmentPoints[0].Id:D3}, Mount axes: {alignmentPoints[0].MountAxes.RaAxis}/{alignmentPoints[0].MountAxes.RaAxis}, Observed axes: {alignmentPoints[0].ObservedAxes.RaAxis}/{alignmentPoints[0].ObservedAxes.RaAxis}");
                            }
                        }
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

        private AlignmentPoint[] GetNearestMountPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.MountAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.MountAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints).ToArray();
        }

        private AlignmentPoint[] GetNearestObservedPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints, out int checkSum)
        {
            AlignmentPoint[] points = AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.ObservedAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.ObservedAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints).ToArray();
            checkSum = GetChecksum(points.Select(p => p.Id).ToArray());
            return points;
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

        #region Adler checksum
        private const int Mod = 65521;
        private static int GetChecksum(int[] array)
        {
            int i;
            var a = 1;
            var b = 0;
            var size = array.Length;
            for (i = 0; i < size; i++)
            {
                a = (a + array[i]) % Mod;
                b = (b + a) % Mod;
            }
            return (b << 16) | a;
        }

        private static int GetChecksum(int singleValue)
        {
            var a = (1 + singleValue) % Mod;
            var b = a % Mod;
            return (b << 16) | a;
        }
        #endregion

        #endregion

    }
}
