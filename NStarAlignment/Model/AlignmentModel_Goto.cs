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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// List of selected alignment points for GetMountAxis.
        /// </summary>
        private readonly List<AlignmentPoint> _selectedPoints = new List<AlignmentPoint>();

        /// <summary>
        /// List of selected alignment points for GetObservedAxis.
        /// </summary>
        private readonly List<AlignmentPoint> _selectedGotoPoints = new List<AlignmentPoint>();

        private readonly List<string> _exceptionMessages = new List<string>();

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
                try
                {
                    ClearSelectedGotoPoints();
                    AxisPosition mAxes = new AxisPosition(mountAxes);
                    if (mAxes.IncludedAngleTo(_homePosition) < ProximityLimit)
                        return mountAxes; // Fast exist if we are going home.
                    RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                        $"GetObservedAxes for {mountAxes[0]}/{mountAxes[1]}");
                    PierSide pSide = (PierSide)pierSide;
                    WriteLastAccessTime();
                    Matrix offsets = Matrix.CreateInstance(1, 2);
                    if (AlignmentPoints.Count == 1)
                    {
                        if (AlignmentPoints[0].PierSide == pSide)
                        {
                            offsets[0, 0] = AlignmentPoints[0].ObservedAxes[0] - AlignmentPoints[0].MountAxes[0];
                            offsets[0, 1] = AlignmentPoints[0].ObservedAxes[1] - AlignmentPoints[0].MountAxes[1];
                            AlignmentPoints[0].SelectedForGoto = true;
                            _selectedGotoPoints.Add(AlignmentPoints[0]);
                            RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                $"Single alignment point selected {AlignmentPoints[0].Id:D3}, Mount axes: {AlignmentPoints[0].MountAxes.RaAxis}/{AlignmentPoints[0].MountAxes.RaAxis}, Observed axes: {AlignmentPoints[0].ObservedAxes.RaAxis}/{AlignmentPoints[0].ObservedAxes.RaAxis}");
                        }
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
                                pt.SelectedForGoto = true;
                                _selectedGotoPoints.Add(pt);
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
                            RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name, _stringBuilder.ToString());
                            _stringBuilder.Clear();
                        }
                        else if (rows > 0)
                        {
                            // Just use the nearest point of the two.
                            offsets[0, 0] = alignmentPoints[0].ObservedAxes[0] - alignmentPoints[0].MountAxes[0];
                            offsets[0, 1] = alignmentPoints[0].ObservedAxes[1] - alignmentPoints[0].MountAxes[1];
                            AlignmentPoints[0].SelectedForGoto = true;
                            _selectedGotoPoints.Add(AlignmentPoints[0]);
                            RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                $"Using nearest point of two {alignmentPoints[0].Id:D3}, Mount axes: {alignmentPoints[0].MountAxes.RaAxis}/{alignmentPoints[0].MountAxes.RaAxis}, Observed axes: {alignmentPoints[0].ObservedAxes.RaAxis}/{alignmentPoints[0].ObservedAxes.RaAxis}");

                        }
                        else
                        {
                            // Otherwise default to just using the nearest point with the same pier side
                            AlignmentPoint alignmentPoint = GetNearestMountPoint(mAxes, pSide);
                            if (alignmentPoint != null)
                            {
                                offsets[0, 0] = alignmentPoint.ObservedAxes[0] - alignmentPoint.MountAxes[0];
                                offsets[0, 1] = alignmentPoint.ObservedAxes[1] - alignmentPoint.MountAxes[1];
                                alignmentPoint.SelectedForGoto = true;
                                _selectedGotoPoints.Add(alignmentPoint);
                                RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                    $"Using nearest point in whole sky {alignmentPoint.Id:D3}, Mount axes: {alignmentPoint.MountAxes.RaAxis}/{alignmentPoint.MountAxes.RaAxis}, Observed axes: {alignmentPoint.ObservedAxes.RaAxis}/{alignmentPoint.ObservedAxes.RaAxis}");
                            }
                            else
                            {
                                RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                    $"No alignment points selected, Observed axes: {mountAxes[0]}/{mountAxes[1]}, pier side: {pierSide}");

                            }
                        }
                    }

                    var observedAxes = new[]
                    {
                        mountAxes[0] + offsets[0, 0],
                        mountAxes[1] + offsets[0, 1]
                    };
                    RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                        $"Correction -> Observer = {offsets[0, 0]}/{offsets[0, 1]}");
                    RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                        $"Mount axes: {mountAxes[0]}/{mountAxes[1]} -> Observed axes: {observedAxes[0]}/{observedAxes[1]}");

                    return observedAxes;
                }
                catch (Exception ex)
                {
                    LogException(ex, true);
                    return mountAxes;
                }
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
                try
                {
                    bool postLogMessages = false;
                    AxisPosition sAxes = new AxisPosition(observedAxes);
                    if (sAxes.IncludedAngleTo(_homePosition) < ProximityLimit)
                        return observedAxes; // Fast exit if we are going home.

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
                            if (AlignmentPoints[0].PierSide == pSide)
                            {
                                RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                                    $"GetMountAxes for {observedAxes[0]}/{observedAxes[1]}");
                                ClearSelectedPoints();
                                offsets[0, 0] = AlignmentPoints[0].MountAxes[0] - AlignmentPoints[0].ObservedAxes[0];
                                offsets[0, 1] = AlignmentPoints[0].MountAxes[1] - AlignmentPoints[0].ObservedAxes[1];
                                AlignmentPoints[0].Selected = true;
                                _selectedPoints.Add(AlignmentPoints[0]);
                                // Cache the offsets and checksum
                                _lastOffsets = offsets;
                                _currentChecksum = checksum;
                                RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                    $"Single alignment point selected {AlignmentPoints[0].Id:D3}, Mount axes: {AlignmentPoints[0].MountAxes.RaAxis}/{AlignmentPoints[0].MountAxes.RaAxis}, Observed axes: {AlignmentPoints[0].ObservedAxes.RaAxis}/{AlignmentPoints[0].ObservedAxes.RaAxis}");
                                postLogMessages = true;
                            }
                        }
                    }
                    else
                    {
                        // Get the nearest points and their corresponding checksum value
                        AlignmentPoint[] alignmentPoints =
                            GetNearestObservedPoints(sAxes, pSide, SampleSize, out checksum);
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
                                RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                                    $"GetMountAxes for {observedAxes[0]}/{observedAxes[1]}");
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
                                RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name, _stringBuilder.ToString());
                                _stringBuilder.Clear();

                                // Cache the offsets and the checksum
                                _lastOffsets = offsets;
                                _currentChecksum = checksum;
                                postLogMessages = true;
                            }
                            else if (rows > 0)
                            {
                                checksum = GetChecksum(alignmentPoints[0].Id);
                                if (checksum == _currentChecksum)
                                {
                                    // Checksum hasn't changed so use the last offsets
                                    offsets = _lastOffsets;
                                }
                                else
                                {
                                    RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                                        $"GetMountAxes for {observedAxes[0]}/{observedAxes[1]}");

                                    ClearSelectedPoints();
                                    // Use the nearest point of the two.
                                    offsets[0, 0] =
                                        alignmentPoints[0].MountAxes[0] - alignmentPoints[0].ObservedAxes[0];
                                    offsets[0, 1] =
                                        alignmentPoints[0].MountAxes[1] - alignmentPoints[0].ObservedAxes[1];
                                    alignmentPoints[0].Selected = true;
                                    _selectedPoints.Add(alignmentPoints[0]);
                                    // Cache the offsets and checksum
                                    _lastOffsets = offsets;
                                    _currentChecksum = checksum;
                                    postLogMessages = true;
                                    RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                        $"Using nearest point of two {alignmentPoints[0].Id:D3}, Mount axes: {alignmentPoints[0].MountAxes.RaAxis}/{alignmentPoints[0].MountAxes.RaAxis}, Observed axes: {alignmentPoints[0].ObservedAxes.RaAxis}/{alignmentPoints[0].ObservedAxes.RaAxis}");
                                }
                            }
                            else
                            {
                                // Otherwise default to just using the nearest point in the whole
                                ClearSelectedPoints();
                                AlignmentPoint alignmentPoint = GetNearestObservedPoint(sAxes, pSide, out checksum);
                                if (alignmentPoint != null)
                                {
                                    offsets[0, 0] = alignmentPoint.MountAxes[0] - alignmentPoint.ObservedAxes[0];
                                    offsets[0, 1] = alignmentPoint.MountAxes[1] - alignmentPoint.ObservedAxes[1];
                                    alignmentPoint.Selected = true;
                                    _selectedPoints.Add(alignmentPoint);
                                    // Cache the offsets and checksum
                                    _lastOffsets = offsets;
                                    _currentChecksum = checksum;
                                    postLogMessages = true;
                                    RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                            $"Using nearest point in whole sky {alignmentPoint.Id:D3}, Mount axes: {alignmentPoint.MountAxes.RaAxis}/{alignmentPoint.MountAxes.RaAxis}, Observed axes: {alignmentPoint.ObservedAxes.RaAxis}/{alignmentPoint.ObservedAxes.RaAxis}");
                                }
                                else
                                {
                                    if (_currentChecksum != int.MinValue)
                                    {
                                        _currentChecksum = int.MinValue;
                                        RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                                            $"No alignment points selected, Observed axes: {observedAxes[0]}/{observedAxes[1]}, pier side: {pierSide}");
                                    }
                                }
                            }
                        }

                        // Otherwise default to using zero offset
                    }

                    var mountAxes = new[]
                    {
                        observedAxes[0] + offsets[0, 0],
                        observedAxes[1] + offsets[0, 1]
                    };
                    if (postLogMessages)
                    {
                        RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name,
                            $"Correction -> Mount = {offsets[0, 0]}/{offsets[0, 1]}");
                        RaiseNotification(NotificationType.Information, MethodBase.GetCurrentMethod()?.Name,
                            $"Observed axes: {observedAxes[0]}/{observedAxes[1]} -> Mount axes: {mountAxes[0]}/{mountAxes[1]}");
                    }

                    return mountAxes;
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    return observedAxes;
                }
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

        private void ClearSelectedGotoPoints()
        {
            foreach (AlignmentPoint pt in _selectedGotoPoints)
            {
                pt.SelectedForGoto = false;
            }
            _selectedGotoPoints.Clear();
        }

        private AlignmentPoint[] GetNearestMountPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.MountAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.MountAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints).ToArray();
        }

        private AlignmentPoint GetNearestMountPoint(AxisPosition axisPosition, PierSide pierSide)
        {
            return AlignmentPoints
                .Where(p => p.PierSide == pierSide)
                .OrderBy(d => d.MountAxes.IncludedAngleTo(axisPosition)).FirstOrDefault();
        }

        private AlignmentPoint[] GetNearestObservedPoints(AxisPosition axisPosition, PierSide pierSide, int numberOfPoints, out int checkSum)
        {
            AlignmentPoint[] points = AlignmentPoints
                .Where(p => p.PierSide == pierSide && p.ObservedAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.ObservedAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints).ToArray();
            checkSum = GetChecksum(points.Select(p => p.Id).ToArray());
            return points;
        }
        private AlignmentPoint GetNearestObservedPoint(AxisPosition axisPosition, PierSide pierSide, out int checkSum)
        {
            AlignmentPoint alignmentPoint = AlignmentPoints
                .Where(p => p.PierSide == pierSide)
                .OrderBy(d => d.ObservedAxes.IncludedAngleTo(axisPosition)).FirstOrDefault();
            checkSum = alignmentPoint != null ? GetChecksum(alignmentPoint.Id) : int.MinValue;
            return alignmentPoint;
        }

        private static Matrix SolveNormalEquation(Matrix inputFeatures, Matrix outputValue)
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
