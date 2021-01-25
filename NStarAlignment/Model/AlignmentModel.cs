using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;

namespace NStarAlignment.Model
{
    public enum AlignmentMode
    {
        AltAz,
        GermanPolar,
        Polar
    }


    public class NotificationEventArgs : EventArgs
    {
        public string Message { get; set; }

        public NotificationEventArgs(string message) : base()
        {
            this.Message = message;
        }
    }

    public partial class AlignmentModel
    {
        public event EventHandler<NotificationEventArgs> Notification = delegate { };

        private void RaiseNotification(string message)
        {
            Volatile.Read(ref Notification).Invoke(this, new NotificationEventArgs(message));
        }

        /// <summary>
        /// Tolerance used when comparing angles to allow for rounding errors.
        /// Used when determining whether axis positions need flipping.
        /// </summary>
        private const double AXIS_ANGLE_COMPARISON_TOLERANCE = 0.001;


        public AxisPosition HomePosition { get; set; }

        public double[] OneStarAdjustment { get; } = { 0d, 0d };

        public AlignmentMode AlignmentMode { get; set; } = AlignmentMode.GermanPolar;

        /// <summary>
        /// Set to True when the model has at least 3 alignment points.
        /// </summary>
        public bool ThreeStarEnable { get; private set; }

        /// <summary>
        /// Determines whether or not an adjust position is returned.
        /// </summary>
        public bool IsAlignmentOn { get; set; }

        /// <summary>
        /// How close existing alignment points have to be to the new alignment point
        /// before they are removed and replaced with the new one (Degrees)
        /// </summary>
        public double ProximityLimit { get; set; } = 2.0;

        /// <summary>
        /// How many sample points should be used.
        /// </summary>
        public int SampleSize { get; set; } = 3;

        /// <summary>
        /// How close do points have to be to be considered for inclusion in the sample (Degrees)
        /// </summary>
        public double NearbyLimit { get; set; } = 90.0;


        public double SiteLongitude { get; set; }

        public double SiteLatitude { get; set; }

        public double SiteElevation { get; set; }


        public AlignmentPointCollection AlignmentPoints { get; } = new AlignmentPointCollection();


        private readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NStarAlignment\Points.config");

        public AlignmentModel(double siteLatitude, double siteLongitude, double siteElevation, bool clearPointsOnStartup = false)
        {
            SiteLatitude = siteLatitude;
            SiteLongitude = siteLongitude;
            SiteElevation = siteElevation;
            HomePosition = new AxisPosition(90.0, 90.0);

            if (!clearPointsOnStartup)
            {
                LoadAlignmentPoints();
            }
        }

        #region Alignment point management ...
        public void AddAlignmentPoint(double[] targetRaDec, double[] mountAxes, double[] skyAxes, TimeRecord time)
        {
            AddAlignmentPoint(targetRaDec,
                new AxisPosition(mountAxes[0], mountAxes[1]),
                new AxisPosition(skyAxes[0], skyAxes[1]),
                time);
        }

        public void AddAlignmentPoint(double[] targetRaDec, AxisPosition mountAxes, AxisPosition skyAxes,
            TimeRecord time)
        {
            // To protect against calculation errors near the pole reject positions that are too close
            if (Math.Abs(targetRaDec[1]) > 89.9)
            {
                throw new ArgumentOutOfRangeException(nameof(targetRaDec),
                    "The alignment point is too near the equatorial pole to be included in the model.");
            }


            if (AlignmentPoints.Count > 2 && ProximityLimit > 0.0)
            {
                // Remove any existing alignment points that are too close to the new one.
                var nearPoints = AlignmentPoints.Where(p => p.MountAxes.IncludedAngleTo(mountAxes) <= ProximityLimit).ToList();
                foreach (AlignmentPoint deletePt in nearPoints)
                {
                    AlignmentPoints.Remove(deletePt);
                }
            }

            var altAz = AstroConvert.RaDec2AltAz(targetRaDec[0], targetRaDec[1], time.LocalSiderealTime, SiteLatitude);
            CarteseanCoordinate mountXy = AxesToCartesean(mountAxes, time);
            CarteseanCoordinate skyXy = AxesToCartesean(skyAxes, time);
            AlignmentPoints.Add(new AlignmentPoint(targetRaDec, altAz, mountAxes, skyAxes,
                mountXy,
                skyXy,
                time));

            OneStarAdjustment[0] = skyAxes[0] - mountAxes[0];
            OneStarAdjustment[1] = skyAxes[1] - mountAxes[1];

            SaveAlignmentPoints();

            SendToMatrix();
        }

        public bool RemoveAlignmentPoint(AlignmentPoint pointToDelete)
        {
            bool result = AlignmentPoints.Remove(pointToDelete);
            if (result)
            {
                SaveAlignmentPoints();
                SendToMatrix();
            }
            return result;
        }

        private void SaveAlignmentPoints()
        {
            var dir = Path.GetDirectoryName(_configFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(_configFile, JsonConvert.SerializeObject(AlignmentPoints, Formatting.Indented));
        }

        private void LoadAlignmentPoints()
        {
            var dir = Path.GetDirectoryName(_configFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(_configFile))
            {
                AlignmentPoints.Clear();
                using (var file = File.OpenText(_configFile))
                {
                    var serializer = new JsonSerializer();
                    var loaded = (AlignmentPointCollection)serializer.Deserialize(file, typeof(AlignmentPointCollection));
                    foreach (var alignmentPoint in loaded)
                    {
                        AlignmentPoints.Add(alignmentPoint);
                    }
                }
                SendToMatrix();
            }
        }

        public void ClearAlignmentPoints()
        {
            AlignmentPoints.Clear();
            SaveAlignmentPoints();
            SendToMatrix();
        }

        #endregion

        #region Matrix related ...

        private void SendToMatrix()
        {
            //Activate Matrix here
            ActivateMatrices();

        }

        private void ActivateMatrices()
        {

            // assume false - will set true later if 3 stars active
            ThreeStarEnable = (AlignmentPoints.Count >= 3);
            //if (AlignmentPoints.Count >= 3)
            //{
            //    //AssembleTakiMatrix(_mountAxisCoordinates[0], _mountAxisCoordinates[1], _mountAxisCoordinates[2], _skyAxisCoordinates[0], _skyAxisCoordinates[1], _skyAxisCoordinates[2]);
            //    //AssembleAffineMatrix(_skyAxisCoordinates[0], _skyAxisCoordinates[1], _skyAxisCoordinates[2], _mountAxisCoordinates[0], _mountAxisCoordinates[1], _mountAxisCoordinates[2]);
            //    ThreeStarEnable = true;
            //}
        }





        #endregion

        #region Conversions ...
        /// <summary>
        /// Replace the original AxesToSpherical which used to calculate the AltAz from the RA and Dec encoder positions
        /// since. The Ra and Dec (hours and degrees) are already known the new method simply takes these 
        /// as well as the axis positions  in case PolarEnable is switched off.

        /// </summary>
        /// <param name="axisPositions"></param>
        /// <param name="timeRecord"></param>
        /// <returns></returns>
        private CarteseanCoordinate AxesToCartesean(double[] axisPositions, TimeRecord timeRecord)
        {
            // The next line replaces AxesToSpherical,
            SphericalCoordinate spherical = AxesToSpherical(axisPositions, timeRecord);

            // Now convert the Alt-Az to cartesean coordinates.
            CarteseanCoordinate result = SphericalToCartesean(spherical);
            result[2] = 1;
            //System.Diagnostics.Debug.WriteLine($"{this.AlignmentPoints.Count+1} - {axisPositions[0]}/{axisPositions[1]}, {spherical.X}/{spherical.Y}/{spherical.WeightsDown}, {result.X}/{result.Y}/{result.R}/{result.Quadrant}");
            return result;
        }

        /// <summary>
        /// Calculate the area of triangle
        /// </summary>
        /// <param name="px1"></param>
        /// <param name="py1"></param>
        /// <param name="px2"></param>
        /// <param name="py2"></param>
        /// <param name="px3"></param>
        /// <param name="py3"></param>
        /// <returns></returns>
        private double GetTriangleArea(double px1, double py1, double px2, double py2, double px3, double py3)
        {


            //True formula is this
            //    GetTriangleArea = Abs(((px2 * py1) - (px1 * py2)) + ((px3 * py2) - (px2 * py3)) + ((px1 * py3) - (px3 * py1))) / 2

            // Make LARGE  numerical value safe for Windows by adding a scaling factor

            var ta = (((px2 * py1) - (px1 * py2)) / 10000d) + (((px3 * py2) - (px2 * py3)) / 10000d) + (((px1 * py3) - (px3 * py1)) / 10000d);

            return Math.Abs(ta) / 2d;

        }


        public SphericalCoordinate AxesToSpherical(double[] axisPositions, TimeRecord time)
        {
            var result = new SphericalCoordinate();
            var raDec = AxesXYToRaDec(axisPositions, time);
            var altAz = AstroConvert.RaDec2AltAz(raDec[0], raDec[1], time.LocalSiderealTime, SiteLatitude);
            // Convert Alt/Az to spherical where X = angle from X axis in XY plane and Y = Angle from Z axis to XY plane
            result.X = new Angle(altAz[1] + 90f);
            result.Y = new Angle(90f - altAz[0]);

            // Check if RA value is within allowed visible range
            var deltaRa = Range.Range360(Math.Abs(HomePosition.RaAxis - axisPositions[0]));
            if (deltaRa <= 90d && ((90d - deltaRa) <= AXIS_ANGLE_COMPARISON_TOLERANCE))
            {
                // Weights are down
                result.WeightsDown = true;
            }
            else
            {
                // Weights are up
                result.WeightsDown = false;
            }
            return result;
        }


        public double[] SphericalToAxes(SphericalCoordinate spherical, TimeRecord time, bool weightsDown)
        {
            var altAz = new double[] { 90f - spherical.Y, spherical.X - 90f };
            // Use ASCOM Transform
            var tmpRaDec = AstroConvert.AltAz2RaDec(altAz[0], altAz[1], SiteLatitude, time.LocalSiderealTime);
            var axes = new AxisPosition(RaDecToAxesXY(tmpRaDec, time));
            // Calculate difference between Home Ra and Ra Axis
            var deltaRa = Range.Range360(Math.Abs(HomePosition.RaAxis - axes[0]));
            if (weightsDown)
            {
                // Weights need to be down .(include 1000th degree tolerance to allow for rounding errors)
                if (deltaRa > 90d || ((deltaRa - 90d) > AXIS_ANGLE_COMPARISON_TOLERANCE))
                {
                    // Flip the axes
                    axes = axes.Flip();
                }
            }
            else
            {
                // Weights need to be up. (include 1000th degree tolerance to allow for rounding errors)
                if (deltaRa <= 90d && ((90d - deltaRa) <= AXIS_ANGLE_COMPARISON_TOLERANCE))
                {
                    // Flip the axes
                    axes.Flip();
                }
            }
            return axes;
        }



        public CarteseanCoordinate SphericalToCartesean(SphericalCoordinate polar)
        {
            var result = new CarteseanCoordinate();
            double radius = polar.Y - 90f;

            // Avoid division 0 errors
            if (Math.Abs(radius) < 0.0000001)
            {
                radius = 1;
            }
            // Get the cartesian coordinates
            result[0] = polar.X.Cos * radius;
            result[1] = polar.X.Sin * radius;
            result.Ra = 0d;

            // if radius is a negative number, pass this info on the next conversion routine

            if (radius > 0)
            {
                result.R = 1;
            }
            else
            {
                result.R = -1;
            }

            return result;
        }



        public SphericalCoordinate CarteseanToSpherical(CarteseanCoordinate carts)
        {

            SphericalCoordinate result = new SphericalCoordinate();

            // Ah the famous radius formula

            var radius = Math.Sqrt((carts.X * carts.X) + (carts.Y * carts.Y)) * carts.R;


            // And the nasty angle compute routine (any simpler way to implement this ?)

            Angle angle = new Angle();
            if (carts.X > 0)
            {
                angle = new Angle(Math.Atan(carts.Y / carts.X), true);
            }
            if (carts.X < 0)
            {
                angle = carts.Y >= 0 ? new Angle(Math.Atan(carts.Y / carts.X) + Math.PI, true) : new Angle(Math.Atan(carts.Y / carts.X) - Math.PI, true);
            }
            if (Math.Abs(carts.X) < AXIS_ANGLE_COMPARISON_TOLERANCE)
            {
                angle = carts.Y > 0 ? new Angle(Math.PI / 2d, true) : new Angle(-1 * (Math.PI / 2d), true);
            }

            // Convert angle to degrees

            //if (angle < 0)
            //{
            //    angle = 360f + angle;
            //}

            //if (carts.R < 0f)
            //{
            //    angle = new Angle((angle + 180f).Range360());
            //}

            //if (angle >= 180f)
            //{
            //    result.X = 90f - (360f - angle);
            //}
            //else
            //{
            //    result.X = angle + 90f;
            //}

            //treat y as the polar coordinate radius (ra var not used - always 0)
            result.X = angle;
            result.Y = radius + 90f;

            return result;
        }

        private IEnumerable<AlignmentPoint> GetNearestMountPoints(AxisPosition axisPosition, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.MountAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.MountAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints);
        }

        private IEnumerable<AlignmentPoint> GetNearestSkyPoints(AxisPosition axisPosition, int numberOfPoints)
        {
            return AlignmentPoints
                .Where(p => p.SkyAxes.IncludedAngleTo(axisPosition) <= NearbyLimit)
                .OrderBy(d => d.SkyAxes.IncludedAngleTo(axisPosition)).Take(numberOfPoints);
        }
        #endregion

    }
}
