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

    public enum AlignmentAlgorithm
    {
        [Description("Nearest star")]
        Nearest,
        [Description("N-star")]
        NStar,
        [Description("N-Star + Nearest")]
        NStarPlusNearest
    }

    public enum PointFilterMode
    {
        [Description("All points")]
        AllPoints,
        [Description("Same side of meridian")]
        Meridian,
        [Description("Local quadrant")]
        LocalQuadrant
    }

    public enum ThreePointMode
    {
        [Description("Nearest enclosing triangle")]
        NearestTriangle,
        [Description("Enclosing triangle with nearest centre")]
        TriangleWithNearestCentre,
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
        public event EventHandler<NotificationEventArgs> Notification = delegate {};

        private void RaiseNotification(string message)
        {
            Volatile.Read(ref Notification).Invoke(this, new NotificationEventArgs(message));
        }

        /// <summary>
        /// Tolerance used when comparing angles to allow for rounding errors.
        /// Used when determining whether axis positions need flipping.
        /// </summary>
        private const double AXIS_ANGLE_COMPARISON_TOLERANCE = 0.001;

        #region Properties that could be configurable or not.

        /// <summary>
        /// The maximum number of points to consider when looking for enclosing triangles.
        /// </summary>
        public int MaximumCombinationCount { get; set; } = 50;

        #endregion

        public AxisPosition HomePosition { get; }

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
        /// Sets the alignment algorithm to use
        /// </summary>
        public AlignmentAlgorithm AlignmentAlgorithm { get; set; } = AlignmentAlgorithm.Nearest;

        public PointFilterMode PointFilterMode { get; set; } = PointFilterMode.AllPoints;

        public ThreePointMode ThreePointMode { get; set; } = ThreePointMode.TriangleWithNearestCentre;

        /// <summary>
        /// How close existing alignment points have to be to the new alignment point
        /// before they are removed and replaced with the new one.
        /// </summary>
        public double ProximityLimit { get; set; } = 2;

        /// <summary>
        /// If true point distances are calculated using polar distances
        /// If false cartesean distances are used.
        /// </summary>
        public bool LocalToPier { get; set; } = false;

        public double SiteLongitude { get; set; }

        public double SiteLatitude { get; set; }

        public double SiteElevation { get; set; }


        public AlignmentPointCollection AlignmentPoints { get; } = new AlignmentPointCollection();

        // private AscomTools _ascomTools;

        private readonly List<AxisPosition> _mountAxes = new List<AxisPosition>();
        private readonly List<CarteseanCoordinate> _mountAxisCoordinates = new List<CarteseanCoordinate>();
        private readonly List<AxisPosition> _skyAxes = new List<AxisPosition>();
        private readonly List<CarteseanCoordinate> _skyAxisCoordinates = new List<CarteseanCoordinate>();

        private Matrix _mappingMatrix = Matrix.CreateInstance();
        private CarteseanCoordinate _offsetVector;




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

        #region Testing code ...

        #endregion

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
                var nearPoints = AlignmentPoints.Where(p => (Math.Abs(p.SkyAxes[0] - skyAxes[0]) <= ProximityLimit
                                                             && Math.Abs(p.SkyAxes[1] - skyAxes[1]) <= ProximityLimit))
                    .ToList();
                foreach (AlignmentPoint deletePt in nearPoints)
                {
                    AlignmentPoints.Remove(deletePt);
                }
            }

            var altAz = AstroConvert.RaDec2AltAz(targetRaDec[0], targetRaDec[1], time.LocalSiderealTime, SiteLatitude);
            AlignmentPoints.Add(new AlignmentPoint(targetRaDec, altAz, mountAxes, skyAxes, time));

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
            _mountAxes.Clear();
            _mountAxisCoordinates.Clear();
            _skyAxes.Clear();
            _skyAxisCoordinates.Clear();
            foreach (var p in AlignmentPoints)
            {
                _mountAxes.Add(p.MountAxes);
                _mountAxisCoordinates.Add(AxesToCartesean(p.MountAxes, p.SyncTime));
                _skyAxes.Add(p.SkyAxes);
                _skyAxisCoordinates.Add(AxesToCartesean(p.SkyAxes, p.SyncTime));
            }

            //Activate Matrix here
            ActivateMatrix();

        }

        private void ActivateMatrix()
        {

            // assume false - will set true later if 3 stars active
            ThreeStarEnable = false;
            if (AlignmentPoints.Count >= 3)
            {
                AssembleMatrix(_skyAxisCoordinates[0], _skyAxisCoordinates[1], _skyAxisCoordinates[2], _mountAxisCoordinates[0], _mountAxisCoordinates[1], _mountAxisCoordinates[2]);
                ThreeStarEnable = true;
            }
        }


        /// <summary>
        /// Subroutine to draw the Transformation Matrix (Affine Mapping Method)
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="a3"></param>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <param name="m3"></param>
        /// <returns></returns>
        private void AssembleMatrix(CarteseanCoordinate a1, CarteseanCoordinate a2, CarteseanCoordinate a3,
            CarteseanCoordinate m1, CarteseanCoordinate m2, CarteseanCoordinate m3)
        {
            AssembleMatrix(new CarteseanCoordinate(0.0, 0.0), a1, a2, a3, m1, m2, m3);
        }

        /// <summary>
        /// Subroutine to draw the Transformation Matrix (Affine Mapping Method)
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="a3"></param>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <param name="m3"></param>
        /// <returns></returns>
        private bool AssembleMatrix(CarteseanCoordinate targetPosition, CarteseanCoordinate a1, CarteseanCoordinate a2, CarteseanCoordinate a3, CarteseanCoordinate m1, CarteseanCoordinate m2, CarteseanCoordinate m3)
        {
            // Get the P Matrix
            Matrix pMatrix = GetOffsetMatrix(a1, a2, a3);

            // Get the Q Matrix
            Matrix qMatrix = GetOffsetMatrix(m1, m2, m3);

            // Get the Inverse of P
            Matrix inversePMatrix = pMatrix.Inverse2X2();

            // Get the mapping Matrix by Multiplying inversePMatrix and qMatrix
            _mappingMatrix[0, 0] = (inversePMatrix[0, 0] * qMatrix[0, 0]) + (inversePMatrix[0, 1] * qMatrix[1, 0]);
            _mappingMatrix[0, 1] = (inversePMatrix[0, 0] * qMatrix[0, 1]) + (inversePMatrix[0, 1] * qMatrix[1, 1]);
            _mappingMatrix[1, 0] = (inversePMatrix[1, 0] * qMatrix[0, 0]) + (inversePMatrix[1, 1] * qMatrix[1, 0]);
            _mappingMatrix[1, 1] = (inversePMatrix[1, 0] * qMatrix[0, 1]) + (inversePMatrix[1, 1] * qMatrix[1, 1]);

            // Get the Coordinate Offset Vector
            _offsetVector.X = m1.X - ((a1.X * _mappingMatrix[0, 0]) + (a1.Y * _mappingMatrix[1, 0]));
            _offsetVector.Y = m1.Y - ((a1.X * _mappingMatrix[0, 1]) + (a1.Y * _mappingMatrix[1, 1]));

            if ((targetPosition.X + targetPosition.Y) == 0)
            {
                return false;
            }

            return IsPointInTriangle(targetPosition, m1, m2, m3);
        }

        /// <summary>
        /// Function to transform the Coordinates using the mapping matrix and offset vector
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
        private CarteseanCoordinate ApplyMatrixTransformation(CarteseanCoordinate ob)
        {
            var result = new CarteseanCoordinate
            {
                X = _offsetVector.X + ((ob.X * _mappingMatrix[0, 0]) + (ob.Y * _mappingMatrix[1, 0])),
                Y = _offsetVector.Y + ((ob.X * _mappingMatrix[0, 1]) + (ob.Y * _mappingMatrix[1, 1])),
                R = ob.R
            };
            return result;
        }



        private bool UpdateMatrixForMount(double[] targetAxes, TimeRecord time)
        {
            return UpdateMatrix(targetAxes, time, false);

        }

        private bool UpdateMatrixForSky(double[] targetAxes, TimeRecord time)
        {
            return UpdateMatrix(targetAxes, time, true);
        }


        private bool UpdateMatrix(double[] targetAxes, TimeRecord time, bool toSky)
        {
            int[] tr;

            if (AlignmentPoints.Count < 3)
            {
                return false;
            }

            switch (ThreePointMode)
            {
                case ThreePointMode.NearestTriangle:
                    // find the 50 nearest points - then find the nearest enclosing triangle 
                    tr = GetNearest3Points(targetAxes, time);
                    break;
                case ThreePointMode.TriangleWithNearestCentre:    // Triangle with centre the nearest to the target
                                                                  // find the 50 nearest points - then find the enclosing triangle with the nearest centre point 
                    tr = GetNearestCenteredTriangle(targetAxes, time);
                    break;
                default:
                    return false;
            }

            if (tr[0] == -1 || tr[1] == -1 || tr[2] == -1)
            {
                return false;
            }

            CarteseanCoordinate cartesean = AxesToCartesean(new AxisPosition(targetAxes), time);
            var result = toSky ? AssembleMatrix(cartesean, _mountAxisCoordinates[tr[0]], _mountAxisCoordinates[tr[1]], _mountAxisCoordinates[tr[2]], _skyAxisCoordinates[tr[0]], _skyAxisCoordinates[tr[1]], _skyAxisCoordinates[tr[2]])
                : AssembleMatrix(cartesean, _skyAxisCoordinates[tr[0]], _skyAxisCoordinates[tr[1]], _skyAxisCoordinates[tr[2]], _mountAxisCoordinates[tr[0]], _mountAxisCoordinates[tr[1]], _mountAxisCoordinates[tr[2]]);

            return result;

        }

        /// <summary>
        /// Implement a AFFINE transformation on a Polar coordinate system
        /// This is done by converting the Polar Data to Cartesian, Apply AFFINE transformation
        /// Then restore the transformed Cartesian Coordinates back to polar
        /// </summary>
        /// <param name="targetAxes"></param>
        /// <param name="timeRecord"></param>
        /// <returns></returns>
        private CarteseanCoordinate TransformCoordinate(double[] targetAxes, TimeRecord timeRecord)
        {
            CarteseanCoordinate result = new CarteseanCoordinate();

            SphericalCoordinate polarIn = AxesToSpherical(targetAxes, timeRecord);
            CarteseanCoordinate cartIn = PolarToCartesean(polarIn);
            cartIn[2] = 1;

            CarteseanCoordinate transformed = ApplyMatrixTransformation(cartIn);

            SphericalCoordinate polarOut = CarteseanToPolar(transformed);

            var adjustedAxis = SphericalToAxes(polarOut, timeRecord, polarIn.R);
            result[0] = adjustedAxis[0];
            result[1] = adjustedAxis[1];

            return result;
        }

        /// <summary>
        /// Function to put coordinate values into a P/Q Affine matrix array
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private static Matrix GetOffsetMatrix(CarteseanCoordinate p1, CarteseanCoordinate p2, CarteseanCoordinate p3)
        {

            var temp = Matrix.CreateInstance();

            temp[0, 0] = p2.X - p1.X;
            temp[1, 0] = p3.X - p1.X;
            temp[0, 1] = p2.Y - p1.Y;
            temp[1, 1] = p3.Y - p1.Y;

            return temp;

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
            CarteseanCoordinate result = PolarToCartesean(spherical);
            result[2] = 1;
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

        /// <summary>
        /// Function to check if a point is inside the triangle. Computed based sum of areas method
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private bool IsPointInTriangle(CarteseanCoordinate pt, CarteseanCoordinate p1, CarteseanCoordinate p2, CarteseanCoordinate p3)
        {
            var ta = GetTriangleArea(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
            var t1 = GetTriangleArea(pt.X, pt.Y, p2.X, p2.Y, p3.X, p3.Y);
            var t2 = GetTriangleArea(p1.X, p1.Y, pt.X, pt.Y, p3.X, p3.Y);
            var t3 = GetTriangleArea(p1.X, p1.Y, p2.X, p2.Y, pt.X, pt.Y);
            return (Math.Abs(ta - t1 - t2 - t3) < 2);
        }

        private CarteseanCoordinate GetCenterPoint(CarteseanCoordinate p1, CarteseanCoordinate p2, CarteseanCoordinate p3)
        {

            var result = new CarteseanCoordinate();
            double p2X;
            double p2Y;
            double p4X;
            double p4Y;





            // Get the two line 4 point data

            var p1X = p1.X;
            var p1Y = p1.Y;


            if (p3.X > p2.X)
            {
                p2X = ((p3.X - p2.X) / 2d) + p2.X;
            }
            else
            {
                p2X = ((p2.X - p3.X) / 2d) + p3.X;
            }

            if (p3.Y > p2.Y)
            {
                p2Y = ((p3.Y - p2.Y) / 2d) + p2.Y;
            }
            else
            {
                p2Y = ((p2.Y - p3.Y) / 2d) + p3.Y;
            }

            var p3X = p2.X;
            var p3Y = p2.Y;


            if (p1.X > p3.X)
            {
                p4X = ((p1.X - p3.X) / 2d) + p3.X;
            }
            else
            {
                p4X = ((p3.X - p1.X) / 2d) + p1.X;
            }

            if (p1.Y > p3.Y)
            {
                p4Y = ((p1.Y - p3.Y) / 2d) + p3.Y;
            }
            else
            {
                p4Y = ((p3.Y - p1.Y) / 2d) + p1.Y;
            }


            var xd1 = p2X - p1X;
            var xd2 = p4X - p3X;
            var yd1 = p2Y - p1Y;
            var yd2 = p4Y - p3Y;
            var xd3 = p1X - p3X;
            var yd3 = p1Y - p3Y;


            var dv = (yd2 * xd1) - (xd2 * yd1);

            if (Math.Abs(dv) < 0.00000001d)
            {
                dv = 0.00000001d;
            } //avoid div 0 errors


            var ua = ((xd2 * yd3) - (yd2 * xd3)) / dv;
            var ub = ((xd1 * yd3) - (yd1 * xd3)) / dv;

            result.X = p1X + (ua * xd1);
            result.Y = p1Y + (ub * yd1);

            return result;
        }


        public SphericalCoordinate AxesToSpherical(double[] axisPositions, TimeRecord time)
        {
            var result = new SphericalCoordinate();
            var raDec = AxesToRaDec(axisPositions, time);
            var altAz = AstroConvert.RaDec2AltAz(raDec[0], raDec[1], time.LocalSiderealTime, SiteLatitude);

            // System.Diagnostics.Debug.WriteLine($"AxesToSpherical - Axes: {axisPositions[0]}/{axisPositions[1]}, RA/Dec: {raDec[0]}/{raDec[1]}, AltAz: {altAz[0]}/{altAz[1]}");
            result.X = (altAz[1] - 180) + HomePosition.RaAxis;
            result.Y = ((altAz[0] + 90) * 2.0) + HomePosition.DecAxis;
            // Calculate difference between Home Ra and Ra Axis
            var deltaRa = Math.Abs(HomePosition.RaAxis - axisPositions[0]);
            deltaRa = Math.Abs((deltaRa + 180) % 360 - 180);

            // Check if RA value is within allowed visible range
            if (deltaRa <= 90d && ((90d - deltaRa) <= AXIS_ANGLE_COMPARISON_TOLERANCE))
            {
                // Weights are down
                result.R = 1;
            }
            else
            {
                // Weights are up
                result.R = 0;
            }

            return result;
        }


        public double[] SphericalToAxes(SphericalCoordinate raDec, TimeRecord time, double range)
        {
            var altAz = new double[] { ((raDec.Y - HomePosition.DecAxis) * 0.5) - 90.0, (raDec.X - HomePosition.RaAxis) + 180.0 };
            // Use ASCOM Transform
            var tmpRaDec = AstroConvert.AltAz2RaDec(altAz[0], altAz[1], SiteLatitude, time.LocalSiderealTime);
            var axes = new AxisPosition(RaDecToAxesXY(tmpRaDec, time));
            // Calculate difference between Home Ra and Ra Axis
            var deltaRa = Math.Abs(HomePosition.RaAxis - axes[0]);
            deltaRa = Math.Abs((deltaRa + 180) % 360 - 180);
            if (range == 1)
            {
                // Weights need to be down .(include 1000th degree tolerance to allow for rounding errors)
                if (deltaRa > 90d && ((deltaRa - 90d) > AXIS_ANGLE_COMPARISON_TOLERANCE))
                {
                    // Flip the axes
                    axes = axes.Flip();
                }
            }
            else
            {
                // Weights need to be up. (include 1000th degree tolerance to allow for rounding errors)
                if (deltaRa <= 90d && ((90d - deltaRa) > AXIS_ANGLE_COMPARISON_TOLERANCE))
                {
                    // Flip the axes
                    axes.Flip();
                }
            }
            return axes;

        }



        public CarteseanCoordinate PolarToCartesean(SphericalCoordinate polar)
        {
            var result = new CarteseanCoordinate();
            // make angle stays within the 360 bound
            double tempRa;
            if (polar.X > HomePosition.RaAxis)
            {
                // adjustedRa = new Angle((polar.X - HomePosition.RAAxis));
                tempRa = polar.X - HomePosition.RaAxis;
            }
            else
            {
                // adjustedRa = new Angle(Angle.Range360(360d - ()));
                tempRa = 360 - (HomePosition.RaAxis - polar.X);
            }
            var adjustedRa = new Angle(tempRa);

            double radius = polar.Y - HomePosition.DecAxis;

            // Avoid division 0 errors
            if (Math.Abs(radius) < 0.0000001)
            {
                radius = 1;
            }

            // Get the cartesian coordinates

            result[0] = adjustedRa.Cos * radius;
            result[1] = adjustedRa.Sin * radius;
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


        public SphericalCoordinate CarteseanToPolar(CarteseanCoordinate carts)
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
            if (carts.X == 0)
            {
                angle = carts.Y > 0 ? new Angle(Math.PI / 2d, true) : new Angle(-1 * (Math.PI / 2d), true);
            }

            // Convert angle to degrees

            if (angle < 0)
            {
                angle = 360 + angle;
            }

            if (carts.R < 0)
            {
                angle = new Angle((angle + 180).Range360());
            }

            if (angle >= 180)
            {
                result.X = HomePosition.RaAxis - (360 - angle);
            }
            else
            {
                result.X = angle + HomePosition.RaAxis;
            }

            //treat y as the polar coordinate radius (ra var not used - always 0)

            result.Y = radius + HomePosition.DecAxis;

            return result;
        }


        private int[] GetNearestCenteredTriangle(double[] targetAxis, TimeRecord time)
        {
            var result = new[] { -1, -1, -1 };

            var filteredPoints = new List<TriangleDataHolder>();


            // Adjust only if there are three alignment stars

            if (AlignmentPoints.Count <= 3)
            {
                for (var i = 0; i < AlignmentPoints.Count; i++)
                {
                    result[i] = i;
                }
                RaiseNotification($"Nearest centered triangle ({targetAxis[0]}, {targetAxis[1]}) by points {result[0]}, {result[1]} and {result[2]}");
                return result;
            }

            var cartesean = AxesToCartesean(new AxisPosition(targetAxis), time);

            // first find out the distances to the alignment stars
            for (var i = 0; i < AlignmentPoints.Count; i++)
            {
                var dataHolder = new TriangleDataHolder
                {
                    Coordinate = _skyAxisCoordinates[i]
                };

                switch (PointFilterMode)
                {
                    case PointFilterMode.AllPoints:
                        // all points 
                        break;
                    case PointFilterMode.Meridian:
                        // only consider points on this side of the meridian 
                        if (dataHolder.Coordinate.Y * cartesean.Y < 0)
                        {
                            continue;
                        }

                        break;
                    case PointFilterMode.LocalQuadrant:
                        // local quadrant 
                        if (cartesean.Quadrant != dataHolder.Coordinate.Quadrant)
                        {
                            continue;
                        }

                        break;
                }

                if (LocalToPier)
                {
                    // calculate polar distance
                    dataHolder.Distance = Math.Pow(_skyAxes[i].RaAxis - targetAxis[0], 2) + Math.Pow(_skyAxes[i].DecAxis - targetAxis[1], 2);
                }
                else
                {
                    // calculate cartesian distance
                    dataHolder.Distance = Math.Pow(dataHolder.Coordinate.X - cartesean.X, 2) + Math.Pow(dataHolder.Coordinate.Y - cartesean.Y, 2);
                }

                // Also save the reference star id for this particular reference star
                dataHolder.AlignmentPointIndex = i;
                filteredPoints.Add(dataHolder);
            }

            if (filteredPoints.Count < 3)
            {
                // not enough points to do 3-point
                return result;  // All set to -1.
            }

            // now sort the distances so the closest stars are at the top
            var sortedPoints = filteredPoints.OrderBy(p => p.Distance).Take(MaximumCombinationCount).ToList();


            var sortedPointCount = sortedPoints.Count;
            // iterate through all the triangles possible using the nearest alignment points
            var l = 1;
            var m = 2;
            var n = 3;
            var success = false;
            double lastDistance = 0;
            for (var i = 0; i < sortedPointCount - 2; i++)
            {
                CarteseanCoordinate p1 = sortedPoints[i].Coordinate;
                for (var j = i + 1; j < sortedPointCount - 1; j++)
                {
                    CarteseanCoordinate p2 = sortedPoints[j].Coordinate;
                    for (var k = j + 1; k < sortedPointCount; k++)
                    {
                        CarteseanCoordinate p3 = sortedPoints[k].Coordinate;

                        if (IsPointInTriangle(cartesean, p1, p2, p3))
                        {
                            // Compute for the center point
                            CarteseanCoordinate pc = GetCenterPoint(p1, p2, p3);
                            // don't need full pythagoras - sum of squares is good enough
                            var newDistance = Math.Pow(pc.X - cartesean.X, 2) + Math.Pow(pc.Y - cartesean.Y, 2);

                            if (!success)
                            {
                                // first time through
                                lastDistance = newDistance;
                                success = true;
                                l = i;
                                m = j;
                                n = k;
                            }
                            else
                            {
                                if (newDistance < lastDistance)
                                {
                                    l = i;
                                    m = j;
                                    n = k;
                                    lastDistance = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            if (success)
            {
                result[0] = sortedPoints[l].AlignmentPointIndex;
                result[1] = sortedPoints[m].AlignmentPointIndex;
                result[2] = sortedPoints[n].AlignmentPointIndex;
                RaiseNotification($"Nearest centered triangle to defined ({targetAxis[0]}, {targetAxis[1]}) by points {result[0]}, {result[1]} and {result[2]}");
            }
            else
            {
                RaiseNotification($"Nearest centered triangle could not be determined for axes ({targetAxis[0]}, {targetAxis[1]}).");
            }

            return result;
        }

        private int[] GetNearest3Points(double[] targetAxis, TimeRecord time)
        {
            var result = new[] { -1, -1, -1 };

            var filteredPoints = new List<TriangleDataHolder>();


            // Adjust only if there are three alignment stars

            if (AlignmentPoints.Count <= 3)
            {
                for (var i = 0; i < AlignmentPoints.Count; i++)
                {
                    result[i] = i;
                }
                RaiseNotification($"Nearest three points to ({targetAxis[0]}, {targetAxis[1]}) by points {result[0]}, {result[1]} and {result[2]}");
                return result;
            }

            var cartesean = AxesToCartesean(new AxisPosition(targetAxis), time);

            // first find out the distances to the alignment stars
            for (var i = 0; i < AlignmentPoints.Count; i++)
            {
                var dataHolder = new TriangleDataHolder
                {
                    Coordinate = _skyAxisCoordinates[i]
                };

                switch (PointFilterMode)
                {
                    case PointFilterMode.AllPoints:
                        // all points 

                        break;
                    case PointFilterMode.Meridian:
                        // only consider points on this side of the meridian 
                        if (dataHolder.Coordinate.Y * cartesean.Y < 0)
                        {
                            continue;
                        }

                        break;
                    case PointFilterMode.LocalQuadrant:
                        // local quadrant 
                        if (cartesean.Quadrant != dataHolder.Coordinate.Quadrant)
                        {
                            continue;
                        }

                        break;
                }

                if (LocalToPier)
                {
                    // calculate polar distance
                    dataHolder.Distance = Math.Pow(_skyAxes[i].RaAxis - targetAxis[0], 2) + Math.Pow(_skyAxes[i].DecAxis - targetAxis[1], 2);
                }
                else
                {
                    // calculate cartesian distance
                    dataHolder.Distance = Math.Pow(dataHolder.Coordinate.X - cartesean.X, 2) + Math.Pow(dataHolder.Coordinate.Y - cartesean.Y, 2);
                }

                // Also save the reference star id for this particular reference star
                dataHolder.AlignmentPointIndex = i;
                filteredPoints.Add(dataHolder);
            }

            if (filteredPoints.Count < 3)
            {
                // not enough points to do 3-point
                return result;  // All set to -1.
            }

            // now sort the distances so the closest stars are at the top
            var sortedPoints = filteredPoints.OrderBy(p => p.Distance).Take(MaximumCombinationCount).ToList();

            var sortedPointCount = sortedPoints.Count;
            // iterate through all the triangles possible using the nearest alignment points
            for (var i = 0; i < sortedPointCount - 2; i++)
            {
                CarteseanCoordinate p1 = sortedPoints[i].Coordinate;
                for (var j = i + 1; j < sortedPointCount - 1; j++)
                {
                    CarteseanCoordinate p2 = sortedPoints[j].Coordinate;
                    for (var k = j + 1; k < sortedPointCount; k++)
                    {
                        CarteseanCoordinate p3 = sortedPoints[k].Coordinate;

                        if (IsPointInTriangle(cartesean, p1, p2, p3))
                        {
                            result[0] = sortedPoints[i].AlignmentPointIndex;
                            result[1] = sortedPoints[j].AlignmentPointIndex;
                            result[2] = sortedPoints[k].AlignmentPointIndex;
                            RaiseNotification($"Nearest three points to ({targetAxis[0]}, {targetAxis[1]}) are: {result[0]}, {result[1]} and {result[2]}");

                            return result;
                        }
                    }
                }
            }

            // If it gets to here nothing was found and result still has all -1s
            RaiseNotification($"Nearest three points to ({targetAxis[0]}, {targetAxis[1]}) could not be determined.");
            return result;
        }

        #endregion

    }
}
