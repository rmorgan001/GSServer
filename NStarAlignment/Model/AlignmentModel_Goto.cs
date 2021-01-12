using System;
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

            var skyAxes = new[] { 0.0, 0.0 };
            CarteseanCoordinate result;
            System.Diagnostics.Debug.WriteLine($"Alignment model Sidereal time: {time.LocalSiderealTime}");
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
                case AlignmentMode.AltAz:
                    var tempRaDec = AstroConvert.AltAz2RaDec(axes[1], axes[0], SiteLatitude, localSiderealTime);
                    raDec[0] = AstroConvert.Ra2Ha12(tempRaDec[0], localSiderealTime) * 15.0; // ha in degrees
                    raDec[1] = tempRaDec[1];
                    break;
                case AlignmentMode.GermanPolar:
                case AlignmentMode.Polar:
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

                    if (axes[0] > HomePosition.RaAxis + 90 && axes[0] < HomePosition.RaAxis - 90)
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

        /// <summary>
        /// Conversion of mount axis positions in degrees to Alt and Az
        /// </summary>
        /// <param name="axes"></param>
        /// <returns>AzAlt</returns>
        internal double[] AxesXYToAzAlt(double[] axes)
        {
            var a = AxesYXToAltAz(new[] { axes[1], axes[0] });
            var b = new[] { a[1], a[0] };
            return b;
        }



        /// <summary>
        /// Conversion of mount axis positions in degrees to Alt and Az
        /// </summary>
        /// <param name="axes"></param>
        /// <returns>AltAz</returns>
        internal double[] AxesYXToAltAz(double[] axes)
        {
            var altAz = new[] { axes[0], axes[1] };
            switch (AlignmentMode)
            {
                case AlignmentMode.AltAz:
                    break;
                case AlignmentMode.GermanPolar:
                    if (altAz[0] > 90)
                    {
                        altAz[1] += 180.0;
                        altAz[0] = 180 - altAz[0];
                        altAz = Range.RangeAltAz(altAz);
                    }

                    //southern hemisphere
                    if (SiteLatitude < 0) altAz[0] = -altAz[0];

                    //axis degrees to ha
                    var ha = altAz[1] / 15.0;
                    altAz = AstroConvert.HaDec2AltAz(ha, altAz[0], SiteLatitude);
                    break;
                case AlignmentMode.Polar:
                    //axis degrees to ha
                    ha = altAz[1] / 15.0;
                    if (SiteLatitude < 0) altAz[0] = -altAz[0];
                    altAz = AstroConvert.HaDec2AltAz(ha, altAz[0], SiteLatitude);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            altAz = Range.RangeAltAz(altAz);
            return altAz;
        }


        /// <summary>
        /// convert a decimal Alt/Az positions to an axes positions at a given time
        /// </summary>
        /// <param name="altAz"></param>
        /// <param name="lst">Local Sidereal Time</param>
        /// <returns></returns>
        internal double[] AltAzToAxesYX(double[] altAz, TimeRecord time)
        {
            var axes = new[] {altAz[0], altAz[1]};
            switch (AlignmentMode)
            {
                case AlignmentMode.AltAz:
                    break;
                case AlignmentMode.GermanPolar:
                    axes = AstroConvert.AltAz2RaDec(axes[0], axes[1], SiteLatitude, time.LocalSiderealTime);

                    axes[0] = AstroConvert.Ra2Ha12(axes[0], time.LocalSiderealTime) * 15.0; // ha in degrees

                    if (SiteLatitude < 0) axes[1] = -axes[1];

                    axes = Range.RangeAzAlt(axes);

                    if (axes[0] > 180.0 || axes[0] < 0)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    break;
                case AlignmentMode.Polar:
                    axes = AstroConvert.AltAz2RaDec(axes[0], axes[1], SiteLatitude, time.LocalSiderealTime);

                    axes[0] = AstroConvert.Ra2Ha12(axes[0], time.LocalSiderealTime) * 15.0; // ha in degrees

                    if (SiteLatitude < 0) axes[1] = -axes[1];

                    axes = Range.RangeAzAlt(axes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            axes = Range.RangeAxesXY(axes);
            return new[] { axes[1], axes[0] };
        }

        #endregion


        #region Going from sky position to mount position ...
        public double[] GetMountAxes(double[] skyAxes, TimeRecord time)
        {
            if (!IsAlignmentOn) return skyAxes; // Fast exit as alignment modeling is switched off.

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
