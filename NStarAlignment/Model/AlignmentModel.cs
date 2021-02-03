using Newtonsoft.Json;
using NStarAlignment.DataTypes;
using NStarAlignment.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NStarAlignment.Model
{

    public enum NotificationType
    {
        Information,
        Data,
        Warning,
        Error
    }


    public class NotificationEventArgs : EventArgs
    {
        public NotificationType NotificationType { get; set; }
        public string Message { get; set; }

        public NotificationEventArgs(NotificationType notificationType, string message)
        {
            this.NotificationType = notificationType;
            this.Message = message;
        }
    }

    public partial class AlignmentModel
    {
        public event EventHandler<NotificationEventArgs> Notification = delegate { };

        private void RaiseNotification(NotificationType notificationType, string message)
        {
            Volatile.Read(ref Notification).Invoke(this, new NotificationEventArgs(notificationType, message));
        }

        /// <summary>
        /// Tolerance used when comparing angles to allow for rounding errors.
        /// Used when determining whether axis positions need flipping.
        /// </summary>
        private const double AxisAngleComparisonTolerance = 0.001;


        private AxisPosition _homePosition = new AxisPosition(90.0, 90.0);

        public void SetHomePosition(double raAxis, double decAxis)
        {
            _homePosition = new AxisPosition(raAxis, decAxis);
        }

        public double[] OneStarAdjustment { get; } = { 0d, 0d };

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

        public DateTime? LastAccessDateTime { get; private set; }

        // String builder for building more detailed messages.
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        
        private readonly object _accessLock = new object();

        private readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NStarAlignment\Points.config");

        private readonly string _timeStampFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NStarAlignment\TimeStamp.config");

        public AlignmentModel(double siteLatitude, double siteLongitude, double siteElevation, bool clearPointsOnStartup = false)
        {
            SiteLatitude = siteLatitude;
            SiteLongitude = siteLongitude;
            SiteElevation = siteElevation;
            _homePosition = new AxisPosition(90.0, 90.0);
            // Load the last access time property.
            ReadLastAccessTime();
            // Re-load alignment points unless clear points on start up is specified
            // In case of lost connections or restarts the points are only cleared if the last time the model was accessed is more than an hour ago.
            if (!clearPointsOnStartup || (LastAccessDateTime != null && (LastAccessDateTime.Value > DateTime.Now - new TimeSpan(1, 0, 0))))
            {
                LoadAlignmentPoints();
            }
        }

        #region Alignment point management ...
        public void AddAlignmentPoint(double[] mountAltAz, double[] observedAltAz, double[] mountAxes, double[] observedAxes, int pierSide, DateTime syncTime)
        {
            AddAlignmentPoint(mountAltAz,
                observedAltAz,
                new AxisPosition(mountAxes[0], mountAxes[1]),
                new AxisPosition(observedAxes[0], observedAxes[1]),
                (PierSide)pierSide,
                syncTime);
        }

        public void AddAlignmentPoint(double[] mountAltAz, double[] observedAltAz, AxisPosition mountAxes, AxisPosition observedAxes, PierSide pierSide, DateTime syncTime)
        {
            lock (_accessLock)
            {
                if (AlignmentPoints.Count > 2 && ProximityLimit > 0.0)
                {
                    // Remove any existing alignment points that are too close to the new one.
                    var nearPoints = AlignmentPoints
                        .Where(p => p.MountAxes.IncludedAngleTo(mountAxes) <= ProximityLimit).ToList();
                    foreach (AlignmentPoint deletePt in nearPoints)
                    {
                        AlignmentPoints.Remove(deletePt);
                    }
                }

                CarteseanCoordinate mountXy = AltAzToCartesean(mountAltAz);
                CarteseanCoordinate observedXy = AltAzToCartesean(observedAltAz);
                AlignmentPoints.Add(new AlignmentPoint(observedAltAz, mountAxes, observedAxes, mountXy, observedXy, pierSide, syncTime));

                OneStarAdjustment[0] = observedAxes[0] - mountAxes[0];
                OneStarAdjustment[1] = observedAxes[1] - mountAxes[1];
                SaveAlignmentPoints();
            }
        }

        public bool RemoveAlignmentPoint(AlignmentPoint pointToDelete)
        {
            bool result = AlignmentPoints.Remove(pointToDelete);
            if (result)
            {
                SaveAlignmentPoints();
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
            ReportAlignmentPoints();
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
                    if (loaded != null)
                    {
                        foreach (var alignmentPoint in loaded)
                        {
                            AlignmentPoints.Add(alignmentPoint);
                        }
                    }
                }
            }
            ReportAlignmentPoints();
        }

        public void ClearAlignmentPoints()
        {
            AlignmentPoints.Clear();
            SaveAlignmentPoints();
        }

        #endregion

        #region Access time related ...
        private void WriteLastAccessTime()
        {
            LastAccessDateTime = DateTime.Now;
            var dir = Path.GetDirectoryName(_timeStampFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(_timeStampFile, JsonConvert.SerializeObject(LastAccessDateTime, Formatting.Indented));
        }

        private void ReadLastAccessTime()
        {
            var dir = Path.GetDirectoryName(_timeStampFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(_timeStampFile))
            {
                using (var file = File.OpenText(_timeStampFile))
                {
                    var serializer = new JsonSerializer();
                    DateTime? loaded = (DateTime?)serializer.Deserialize(file, typeof(DateTime?));
                    LastAccessDateTime = loaded;
                }
            }
        }
        #endregion

        private CarteseanCoordinate AltAzToCartesean(double[] altAz)
        {
            // The next line replaces AxesToSpherical,
            var result = new CarteseanCoordinate();
            double radius = 90f - altAz[0];

            // Avoid division 0 errors
            if (Math.Abs(radius) < 0.0000001)
            {
                radius = 1;
            }
            // Get the cartesian coordinates
            double azRadians = AstroConvert.DegToRad(altAz[1]);
            var x = Math.Sin(azRadians);
            var y = -Math.Cos(azRadians);
            // Get unit vector
            result[0] =  x * radius;
            result[1] =  y * radius;

            return result;
        }

        private void ReportAlignmentPoints()
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine("=============== Alignment points ===============");
            _stringBuilder.AppendLine("ID \tAltAz         \tMount axes         \tObserved axes");
            foreach (var pt in AlignmentPoints)
            {
                _stringBuilder.AppendLine(
                    $"{pt.Id:D3}\t{pt.AltAz[0]}/{pt.AltAz[1]}\t{pt.MountAxes.RaAxis}/{pt.MountAxes.DecAxis}\t{pt.ObservedAxes.RaAxis}/{pt.ObservedAxes.DecAxis}");
            }
            RaiseNotification(NotificationType.Data, _stringBuilder.ToString());
            _stringBuilder.Clear();
        }

    }
}
