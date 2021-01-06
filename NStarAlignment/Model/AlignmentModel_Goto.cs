using System;
using ASCOM.DeviceInterface;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;

namespace NStarAlignment.Model
{
    public partial class AlignmentModel
    {
        #region Going from calculated to synched axis
        public double[] GetSkyAxes(double[] mountAxes, DateTime utcTime)
        {
            if (!IsAlignmentOn) return mountAxes;   // Fast exist as alignment modeling is switched off.

            var time = new TimeRecord(utcTime, SiteLongitude);
            var skyAxes = new[] { 0.0, 0.0 };
            CarteseanCoordinate result;

            if (!ThreeStarEnable)
            {
                result = DeltaMap(mountAxes);
                skyAxes[0] = result[0];
                skyAxes[1] = result[1];
            }
            else
            {
                // Transform target using model
                switch (AlignmentAlgorithm)
                {
                    case AlignmentAlgorithm.Nearest:
                        // Nearest 
                        result = DeltaSyncReverseMatrixMap(mountAxes, time);
                        break;
                    case AlignmentAlgorithm.NStar:
                        // n-star 
                        result = DeltaMatrixMap(mountAxes, time);
                        break;
                    case AlignmentAlgorithm.NStarPlusNearest:
                        // n-Star or failing that nearest 
                        result = DeltaMatrixMap(mountAxes, time);

                        if (!result.Flag)
                        {
                            result = DeltaSyncReverseMatrixMap(mountAxes, time);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unrecognized value for property AlignmentAlgorithm");
                }
                skyAxes[0] = result[0];
                skyAxes[1] = result[1];
            }
            if (Math.Abs(skyAxes[0] - mountAxes[0]) > 90)
            {
                // Switch axis positions
                skyAxes = GetAltAxisPosition(skyAxes);
            }
            return skyAxes;
        }

        /// <summary>
        /// GEMs have two possible axes positions, given an axis position this returns the other 
        /// </summary>
        /// <param name="alt">position</param>
        /// <returns>other axis position</returns>
        private double[] GetAltAxisPosition(double[] alt)
        {
            var d = new[] { 0.0, 0.0 };
            if (alt[0] > 90)
            {
                d[0] = alt[0] - 180;
                d[1] = 180 - alt[1];
            }
            else
            {
                d[0] = alt[0] + 180;
                d[1] = 180 - alt[1];
            }
            return d;
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Ra and Dec
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public double[] AxesToRaDec(double[] axes, TimeRecord time)
        {
            var raDec = new[] { axes[0], axes[1] };
            var localSiderealTime = time.LocalSiderealTime;

            switch (AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    var tempRaDec = AstroConvert.AltAz2RaDec(axes[1], axes[0], SiteLatitude, localSiderealTime);
                    raDec[0] = AstroConvert.Ra2Ha12(tempRaDec[0], localSiderealTime) * 15.0; // ha in degrees
                    raDec[1] = tempRaDec[1];
                    break;
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    if (raDec[1] > HomePosition.DecAxis)
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
        public double[] RaDecToAxes(double[] raDec, TimeRecord time)
        {
            var axes = new[] { raDec[0], raDec[1] };
            var localSiderealTime = time.LocalSiderealTime;
            switch (AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    axes = Range.RangeAzAlt(axes);
                    axes = AstroConvert.RaDec2AltAz(axes[0], axes[1], localSiderealTime, SiteLatitude);
                    return axes;
                case AlignmentModes.algGermanPolar:
                    axes[0] = (localSiderealTime - axes[0]) * 15.0;
                    if (SiteLatitude < 0) axes[1] = -axes[1];
                    axes[0] = Range.Range360(axes[0]);

                    if (axes[0] > HomePosition.RaAxis + 90 && axes[0] < HomePosition.RaAxis - 90)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    axes = Range.RangeAxes(axes);

                    ////check for alternative position within meridian limits
                    //var b = AxesAppToMount(axes);

                    //var alt = SkyServer.CheckAlternatePosition(b);
                    //if (alt != null) axes = alt;

                    return axes;
                case AlignmentModes.algPolar:
                    axes[0] = (localSiderealTime - axes[0]) * 15.0;
                    axes[1] = (SiteLatitude < 0) ? -axes[1] : axes[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            axes = Range.RangeAxes(axes);
            return axes;
        }

        #endregion


        #region Going from sky position to mount position ...
        public double[] GetMountAxes(double[] skyAxes, DateTime utcTime)
        {
            if (!IsAlignmentOn) return skyAxes; // Fast exit as alignment modeling is switched off.

            var time = new TimeRecord(utcTime, SiteLongitude);
            var mountAxes = new[] { 0.0, 0.0 };
            CarteseanCoordinate result;

            if (!ThreeStarEnable)
            {
                result = DeltaReverseMap(skyAxes);
                mountAxes[0] = result[0];
                mountAxes[1] = result[1];
            }
            else
            {
                // Transform target using model
                switch (AlignmentAlgorithm)
                {
                    case AlignmentAlgorithm.Nearest:
                        //  use nearest point (not very accurate)
                        result = DeltaSyncMatrixMap(skyAxes, time);
                        break;
                    case AlignmentAlgorithm.NStar:
                        // n-star 
                        result = DeltaMatrixReverseMap(skyAxes, time);
                        break;
                    case AlignmentAlgorithm.NStarPlusNearest:
                        // n-star, failing that nearest
                        result = DeltaMatrixReverseMap(skyAxes, time);

                        if (!result.Flag)
                        {
                            result = DeltaSyncMatrixMap(skyAxes, time);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unrecognized value for property AlignmentAlgorithm");

                }
                mountAxes[0] = result[0];
                mountAxes[1] = result[1];
            }
            if (Math.Abs(mountAxes[0] - skyAxes[0]) > 90)
            {
                // Switch axis positions
                mountAxes = GetAltAxisPosition(mountAxes);
            }
            return mountAxes;
        }

        #endregion
    }
}
