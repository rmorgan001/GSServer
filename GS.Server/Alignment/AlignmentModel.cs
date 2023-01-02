using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;
using GS.Shared.Domain;

namespace GS.Server.Alignment
{
    public enum NotificationType
    {
        Information,
        Data,
        Warning,
        Error
    }

    [TypeConverter(typeof(EnumTypeConverter))]
    public enum PierSideEnum
    {
        [Description("Unknown")]
        Unknown = -1,
        [Description("East")]
        EastLookingWest,
        [Description("West")]
        WestLookingEast,
    }

    [TypeConverter(typeof(EnumTypeConverter))]
    public enum ActivePointsEnum
    {
        [Description("All")]
        All,
        [Description("Pierside Only")]
        PierSide,
        [Description("Local Quadrant")]
        LocalQuadrant
    }

    [TypeConverter(typeof(EnumTypeConverter))]
    public enum ThreePointAlgorithmEnum
    {
        [Description("Best Centre")]
        BestCentre,
        [Description("Closest Points")]
        ClosestPoints
    }

    [TypeConverter(typeof(EnumTypeConverter))]
    public enum AlignmentBehaviourEnum
    {
        [Description("N-Star + Nearest")]
        NStarPlusNearest,
        [Description("Nearest")]
        Nearest
    }


    public enum HemisphereEnum
    {
        Northern,
        Southern
    }

    public class NotificationEventArgs : EventArgs
    {
        public NotificationType NotificationType { get; set; }
        public string Method { get; set; }

        public int Thread { get; set; }
        public string Message { get; set; }


        public NotificationEventArgs(NotificationType notificationType, string method, string message)
        {
            this.NotificationType = notificationType;
            this.Method = method;
            this.Message = message;
            this.Thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }
    }

    public partial class AlignmentModel
    {
        #region Events ...

        public event EventHandler<NotificationEventArgs> Notification = delegate { };

        private void RaiseNotification(NotificationType notificationType, string method, string message)
        {
            Volatile.Read(ref Notification).Invoke(this, new NotificationEventArgs(notificationType, method, message));
        }

        #endregion

        #region variables ...
        private readonly List<string> _exceptionMessages = new List<string>();

        private bool _threeStarEnabled = false;
        #endregion

        #region Properties ...
        public bool IsAlignmentOn { get; set; }

        private double _proximityLimit = 0.5;
        /// <summary>
        /// How close existing alignment points have to be to the new alignment point
        /// before they are removed and replaced with the new one (degrees)
        /// </summary>
        public double ProximityLimit
        {
            get => _proximityLimit;
            set
            {
                if (_proximityLimit == value) return;
                _proximityLimit = value;
            }
        }


        private double _siteLongitude;
        public double SiteLongitude
        {
            get => _siteLongitude;
            set
            {
                if (_siteLongitude == value) return;
                _siteLongitude = value;
                SendToMatrix();
            }
        }

        private double _siteLatitude;
        public double SiteLatitude
        {
            get => _siteLatitude;
            set
            {
                if (_siteLatitude == value) return;
                _siteLatitude = value;
                Hemisphere = (_siteLatitude >= 0 ? HemisphereEnum.Northern : HemisphereEnum.Southern);
                SendToMatrix();     // Refresh the matrices as these are affected by the site latitude
            }
        }

        public HemisphereEnum Hemisphere { get; private set; } = HemisphereEnum.Northern;

        private double _siteElevation;
        public double SiteElevation
        {
            get => _siteElevation;
            set
            {
                if (_siteElevation == value) return;
                _siteElevation = value;
            }
        }

        public bool CheckLocalPier { get; set; }

        private AlignmentBehaviourEnum _alignmentBehaviour = AlignmentBehaviourEnum.NStarPlusNearest;
        public AlignmentBehaviourEnum AlignmentBehaviour
        {
            get => _alignmentBehaviour;
            set
            {
                if (_alignmentBehaviour == value) return;
                _alignmentBehaviour = value;
            }
        }
        private ThreePointAlgorithmEnum _threePointAlgorithm = ThreePointAlgorithmEnum.BestCentre;
        public ThreePointAlgorithmEnum ThreePointAlgorithm
        {
            get => _threePointAlgorithm;
            set
            {
                if (_threePointAlgorithm == value) return;
                _threePointAlgorithm = value;
            }
        }

        public AxisPosition Home { get; private set; }

        //private double[] _reportedHomePosition = new double[]{ 0, 0 };

        public void SetHomePosition(double ra, double dec)
        {
            Home = new AxisPosition(ra, dec);
            SendToMatrix();
        }


        private ActivePointsEnum _activePoints;
        public ActivePointsEnum ActivePoints
        {
            get => _activePoints;
            set
            {
                if (_activePoints == value) return;
                _activePoints = value;
            }
        }

        public AlignmentPointCollection AlignmentPoints { get; } = new AlignmentPointCollection();

        public AlignmentPoint SelectedAlignmentPoint { get; private set; }

        public DateTime? LastAccessDateTime { get; private set; }

        public int NStarMaxCombinationCount { get; set; } = 50;

        // String builder for building more detailed messages.
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly object _accessLock = new object();

        private readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EqmodNStarAlignment\Points.config");

        private readonly string _timeStampFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EqmodNStarAlignment\TimeStamp.config");

        private const double _homeZeroPosition = 0x800000;    // As per EQMOD

        public AxisPosition EncoderMappingOffset { get; private set; } // Mapping from Mount unsynced positions to internal unsynced positions

        /// <summary>
        /// RA/Dec unsynced adjustments for when there is only one star/point logged.
        /// </summary>
        private AxisPosition _oneStarAdjustment = new AxisPosition(0, 0);

        #endregion

        #region Constructor ...
        public AlignmentModel(double siteLatitude, double siteLongitude, double siteElevation)
        {
            SiteLatitude = siteLatitude;
            SiteLongitude = siteLongitude;
            SiteElevation = siteElevation;
        }
        #endregion

        public void Connect(bool clearPointsOnStartup = false)
        {
            try
            {
                // Load the last access time property.
                ReadLastAccessTime();
                // Re-load alignment points unless clear points on start up is specified
                // In case of lost connections or restarts the points are only cleared if the last time the model was accessed is more than an hour ago.
                if (!clearPointsOnStartup || (LastAccessDateTime != null &&
                                              (LastAccessDateTime.Value > DateTime.Now - new TimeSpan(1, 0, 0))))
                {
                    LoadAlignmentPoints();
                }
            }
            catch (Exception ex)
            {
                LogException(ex, true);
            }
        }

        #region Alignment point management ...
        public bool SyncToRaDec(double[] unsynced, double[] origRaDec, double[] synced, DateTime syncTime)
        {
            try
            {
                lock (_accessLock)
                {

                    bool result = EQ_NPointAppend(new AlignmentPoint(unsynced, origRaDec, synced, syncTime));
                    SaveAlignmentPoints();

                    return result;
                }
            }
            catch (Exception ex)
            {
                LogException(ex, true);
            }
            return false;
        }


        public bool RemoveAlignmentPoint(AlignmentPoint pointToDelete)
        {
            try
            {
                bool result = AlignmentPoints.Remove(pointToDelete);
                if (result)
                {
                    int ptCt = AlignmentPoints.Count();
                    if (ptCt == 0)
                    {
                        _oneStarAdjustment = new AxisPosition(0d, 0d);
                    }
                    else
                    {
                        _oneStarAdjustment = AlignmentPoints[ptCt - 1].Delta; // Use the last point's delta
                    }
                    if (ptCt < 3)
                    {
                        _threeStarEnabled = false;
                    }
                    SaveAlignmentPoints();
                }

                return result;
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return false;
            }
        }


        public void SaveAlignmentPoints(string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(AlignmentPoints, Formatting.Indented));
        }

        private void SaveAlignmentPoints()
        {
            var dir = Path.GetDirectoryName(_configFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            SaveAlignmentPoints(_configFile);
            ReportAlignmentPoints();
        }


        public void LoadAlignmentPoints(string filename)
        {
            AlignmentPoints.Clear();
            using (var file = File.OpenText(filename))
            {
                var serializer = new JsonSerializer();
                try
                {
                    var loaded =
                        (AlignmentPointCollection)serializer.Deserialize(file, typeof(AlignmentPointCollection));
                    if (loaded != null)
                    {
                        foreach (var alignmentPoint in loaded)
                        {
                            AlignmentPoints.Add(alignmentPoint);
                            _oneStarAdjustment = alignmentPoint.Delta;
                        }
                        SendToMatrix(); // Updates the cartesean values.
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

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
                LoadAlignmentPoints(_configFile);
            }
            ReportAlignmentPoints();
        }

        public void ClearAlignmentPoints()
        {
            try
            {
                AlignmentPoints.Clear();
                _oneStarAdjustment = new AxisPosition(0d, 0d);
                _threeStarEnabled = false;
                SaveAlignmentPoints();
            }
            catch (Exception ex)
            {
                LogException(ex, true);
            }
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

        #region Helper methods ...
        private void ReportAlignmentPoints()
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine("=============== Alignment points ===============");
            _stringBuilder.AppendLine("ID \tUnsynced Ra/Dec         \tOrig Ra/Dec        \tSynced RaDec   \tObserved time");
            foreach (var pt in AlignmentPoints)
            {
                _stringBuilder.AppendLine(
                    $"{pt.Id:D3}\t{pt.Unsynced.RA}/{pt.Unsynced.Dec}\t{pt.OrigRaDec.RA}/{pt.OrigRaDec.Dec}\t{pt.Synced.RA}/{pt.Synced.Dec}\t{pt.AlignTime}");
            }
            RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name, _stringBuilder.ToString());
            _stringBuilder.Clear();
        }

        private void LogException(Exception ex, bool allowDuplicates = false)
        {
            if (allowDuplicates || !_exceptionMessages.Contains(ex.Message))
            {
                _exceptionMessages.Add(ex.Message);
                string message = $"{ex.Message}|{ex.StackTrace}";
                RaiseNotification(NotificationType.Error, MethodBase.GetCurrentMethod()?.Name, message);
            }
        }

        #endregion
    }
}
