using System;
using System.Collections.Generic;
using System.Linq;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;

namespace NStarAlignment.Model
{
    public partial class AlignmentModel
    {
        #region Going from calculated to synched axis
        public double[] GetSkyAxes(double[] mountAxes, TimeRecord time)
        {
            if (!IsAlignmentOn) return mountAxes;   // Fast exist as alignment modeling is switched off.

            AxisPosition mAxes = new AxisPosition(mountAxes);
            //System.Diagnostics.Debug.WriteLine($"Mount axes: {mountAxes[0]}/{mountAxes[1]} -> {mAxes[0]}/{mAxes[1]}");

            IEnumerable<AlignmentPoint> nearestPoints = GetNearestMountPoints(mAxes, SampleSize);
            var alignmentPoints = nearestPoints as AlignmentPoint[] ?? nearestPoints.ToArray();
            int rows = alignmentPoints.Count();

            // Build features and values from registered points
            Matrix features = Matrix.CreateInstance(rows, 3);
            Matrix values = Matrix.CreateInstance(rows, 2);
            // System.Diagnostics.Debug.Write("Points chosen are");
            for(int i = 0; i < rows; i++)
            {
                var pt = alignmentPoints[i];
                // System.Diagnostics.Debug.Write($" ({pt.Id:D3})");
                features[i, 0] = 1f;
                features[i, 1] = pt.MountAxes[0] * pt.MountAxes[0];
                features[i, 2] = pt.MountAxes[1] * pt.MountAxes[1];
                values[i, 0] = Range.RangePlusOrMinus180(pt.SkyAxes[0] - pt.MountAxes[0]);
                values[i, 1] = Range.RangePlusOrMinus180(pt.SkyAxes[1] - pt.MountAxes[1]);
            }
            // System.Diagnostics.Debug.WriteLine(".");



            // Solve the normal equation to get theta
            Matrix theta = SolveNormalEquation(features, values);

            // Calculate the difference for the incoming points
            Matrix target = Matrix.CreateInstance(1, 3);
            target[0, 0] = 1f;
            target[0, 1] = mAxes[0] * mAxes[0];
            target[0, 2] = mAxes[1] * mAxes[1];

            Matrix offsets = target * theta;


            //System.Diagnostics.Debug.WriteLine("Features");
            //System.Diagnostics.Debug.WriteLine(features.ToString());
            //System.Diagnostics.Debug.WriteLine("Values");
            //System.Diagnostics.Debug.WriteLine(values.ToString());
            //System.Diagnostics.Debug.WriteLine("Theta");
            //System.Diagnostics.Debug.WriteLine(theta.ToString());
            //System.Diagnostics.Debug.WriteLine("Target");
            //System.Diagnostics.Debug.WriteLine(target.ToString());
            //System.Diagnostics.Debug.WriteLine("Offsets");
            //System.Diagnostics.Debug.WriteLine(offsets.ToString());

            var skyAxes = new[]
            {
                mountAxes[0] + offsets[0, 0],
                mountAxes[1] + offsets[0, 1]
            };
            return skyAxes;
        }


        /// <summary>
        /// Conversion of mount axis positions in degrees to Ra and Dec
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double[] AxesXYToRaDec(double[] axes, TimeRecord time)
        {
            var raDec = new[] { axes[0], axes[1] };
            var localSiderealTime = time.LocalSiderealTime;

            switch (AlignmentMode)
            {
                case AlignmentMode.AltAz:
                    var tempRaDec = AstroConvert.AltAz2RaDec(axes[1], axes[0], SiteLatitude, localSiderealTime);
                    raDec[0] = AstroConvert.Ra2Ha12(tempRaDec[0], localSiderealTime) * 15.0; // ha in degrees
                    raDec[1] = tempRaDec[1];
                    break;
                case AlignmentMode.GermanPolar:
                case AlignmentMode.Polar:
                    if (raDec[1] > 90)
                    {
                        raDec[0] += 180.0;
                        raDec[1] = 180 - raDec[1];
                        raDec = Range.RangeAzAlt(raDec);
                    }

                    raDec[0] = localSiderealTime - raDec[0] / 15.0;
                    //southern hemisphere
                    if (SiteLatitude < 0) raDec[1] = -raDec[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            raDec = Range.RangeRaDec(raDec);
            return raDec;
        }

        /// <summary>
        /// Convert a RaDec position to an axes positions.
        /// </summary>
        /// <param name="raDec"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double[] RaDecToAxesXY(double[] raDec, TimeRecord time)
        {
            var axes = new[] { raDec[0], raDec[1] };
            var localSiderealTime = time.LocalSiderealTime;
            switch (AlignmentMode)
            {
                case AlignmentMode.AltAz:
                    axes = Range.RangeAzAlt(axes);
                    axes = AstroConvert.RaDec2AltAz(axes[0], axes[1], localSiderealTime, SiteLatitude);
                    return axes;
                case AlignmentMode.GermanPolar:
                    axes[0] = (localSiderealTime - axes[0]) * 15.0;
                    if (SiteLatitude < 0) axes[1] = -axes[1];
                    axes[0] = Range.Range360(axes[0]);

                    if (axes[0] > 180 || axes[0] < 0)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    axes = Range.RangeAxesXY(axes);

                    ////check for alternative position within meridian limits
                    //var b = AxesAppToMount(axes);

                    //var alt = SkyServer.CheckAlternatePosition(b);
                    //if (alt != null) axes = alt;

                    return axes;
                case AlignmentMode.Polar:
                    axes[0] = (localSiderealTime - axes[0]) * 15.0;
                    axes[1] = (SiteLatitude < 0) ? -axes[1] : axes[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            axes = Range.RangeAxesXY(axes);
            return axes;
        }


        #endregion


        #region Going from sky position to mount position ...
        public double[] GetMountAxes(double[] skyAxes, TimeRecord time)
        {
            if (!IsAlignmentOn) return skyAxes; // Fast exit as alignment modeling is switched off.
            AxisPosition sAxes = new AxisPosition(skyAxes);
            //System.Diagnostics.Debug.WriteLine($"Sky axes: {skyAxes[0]}/{skyAxes[1]} -> {sAxes[0]}/{sAxes[1]}");
            IEnumerable<AlignmentPoint> nearestPoints = GetNearestSkyPoints(sAxes, SampleSize);
            var alignmentPoints = nearestPoints as AlignmentPoint[] ?? nearestPoints.ToArray();
            int rows = alignmentPoints.Count();

            // Build features and values from registered points
            Matrix features = Matrix.CreateInstance(rows, 3);
            Matrix values = Matrix.CreateInstance(rows, 2);
            // System.Diagnostics.Debug.Write("Points chosen are");
            for (int i = 0; i < rows; i++)
            {
                var pt = alignmentPoints[i];
                // System.Diagnostics.Debug.Write($" ({pt.Id:D3})");
                features[i, 0] = 1f;
                features[i, 1] = pt.SkyAxes[0] * pt.SkyAxes[0];
                features[i, 2] = pt.SkyAxes[1] * pt.SkyAxes[1];
                values[i, 0] = Range.RangePlusOrMinus180(pt.MountAxes[0] - pt.SkyAxes[0]);
                values[i, 1] = Range.RangePlusOrMinus180(pt.MountAxes[1] - pt.SkyAxes[1]);
            }
            // System.Diagnostics.Debug.WriteLine(".");



            // Solve the normal equation to get theta
            Matrix theta = SolveNormalEquation(features, values);

            // Calculate the difference for the incoming points
            Matrix target = Matrix.CreateInstance(1, 3);
            target[0, 0] = 1f;
            target[0, 1] = sAxes[0] * sAxes[0];
            target[0, 2] = sAxes[1] * sAxes[1];

            Matrix offsets = target * theta;


            //System.Diagnostics.Debug.WriteLine("Features");
            //System.Diagnostics.Debug.WriteLine(features.ToString());
            //System.Diagnostics.Debug.WriteLine("Values");
            //System.Diagnostics.Debug.WriteLine(values.ToString());
            //System.Diagnostics.Debug.WriteLine("Theta");
            //System.Diagnostics.Debug.WriteLine(theta.ToString());
            //System.Diagnostics.Debug.WriteLine("Target");
            //System.Diagnostics.Debug.WriteLine(target.ToString());
            //System.Diagnostics.Debug.WriteLine("Offsets");
            //System.Diagnostics.Debug.WriteLine(offsets.ToString());

            var mountAxes = new[]
            {
                skyAxes[0] + offsets[0, 0],
                skyAxes[1] + offsets[0, 1]
            };
            return mountAxes;
        }

        #endregion

        public static Matrix SolveNormalEquation(Matrix inputFeatures, Matrix outputValue)
        {
            Matrix inputFeaturesT = inputFeatures.Transpose();
            Matrix xx = inputFeaturesT * inputFeatures;
            Matrix xxi = xx.Invert();

            Matrix xy = inputFeaturesT * outputValue;

            Matrix result = xxi * xy;
            return result;
        }

    }
}
