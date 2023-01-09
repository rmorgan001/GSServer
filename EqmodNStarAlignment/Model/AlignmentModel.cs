﻿using EqmodNStarAlignment.Utilities;
using EqmodNStarAlignment.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;

namespace EqmodNStarAlignment.Model
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
    public enum AlignmentModeEnum
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
                RefreshProximitySteps();
            }
        }

        private void RefreshProximitySteps()
        {
            _proximityStepsRa = (long)this.ProximityLimit * (this.StepsPerRev.RA / 360);
            _proximityStepsDec = (long)this.ProximityLimit * (this.StepsPerRev.Dec / 360);
        }

        private long _proximityStepsRa = 12533;   /// Approimately 0.5 degrees (if steps per rev = 9024000)
        private long _proximityStepsDec = 12533;   /// Approimately 0.5 degrees (if steps per rev = 9024000)

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

        public double SiteElevation { get; set; }

        public bool CheckLocalPier { get; set; }

        public AlignmentModeEnum AlignmentMode { get; set; } = AlignmentModeEnum.NStarPlusNearest;

        public ThreePointAlgorithmEnum ThreePointAlgorithm { get; set; } = ThreePointAlgorithmEnum.BestCentre;

        public EncoderPosition HomeEncoder { get; private set; }

        private long[] _reportedHomePosition = new long[]{ 0, 0 };

        public void SetHomePosition(long RAEncoder, long decEncoder)
        {
            _reportedHomePosition[0] = RAEncoder;
            _reportedHomePosition[1] = decEncoder;
            // EQMod expects the RA Home position to have a step value of 0x800000 (0 hours)
            // and the Dec Home position to be 0xA26C80 (90 degrees)

            // Set the home position to the internal zero position for RA and internal zero  + 90 degrees for Dec 
            HomeEncoder = new EncoderPosition(_homeZeroPosition, _homeZeroPosition + this.StepsPerRev.Dec / 4);  // Set the dec at 90 degrees.

            // Calculate the correction for mapping mount encoder positions to internal encoder positions
            EncoderMappingOffset = new EncoderPosition(HomeEncoder.RA - RAEncoder, HomeEncoder.Dec - decEncoder);
            // To convert: Mount = Internal - EncoderMappingOffset
            //             Internal = Mount + EncoderMappingOffset
            SendToMatrix();
        }

        private EncoderPosition _stepsPerRev;
        public EncoderPosition StepsPerRev
        {
            get => _stepsPerRev;
            set
            {
                if (_stepsPerRev == value)
                {
                    return;
                }
                _stepsPerRev = value;
                SetHomePosition(_reportedHomePosition[0], _reportedHomePosition[1]);
            }
        }

        public ActivePointsEnum ActivePoints { get; set; }

        public AlignmentPointCollection AlignmentPoints { get; } = new AlignmentPointCollection();

        public AlignmentPoint SelectedAlignmentPoint { get; private set; }

        public DateTime? LastAccessDateTime { get; private set; }

        public int NStarMaxCombinationCount { get; set; } = 50;

        // String builder for building more detailed messages.
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly object _accessLock = new object();

        private readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EqmodNStarAlignment\Points.config");

        private readonly string _timeStampFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"EqmodNStarAlignment\TimeStamp.config");

        private const long _homeZeroPosition = 0x800000;    // As per EQMOD

        public EncoderPosition EncoderMappingOffset { get; private set; } // Mapping from Mount encoder positions to internal encoder positions

        /// <summary>
        /// RA/Dec encoder adjustments for when there is only one star/point logged.
        /// </summary>
        private EncoderPosition _oneStarAdjustment = new EncoderPosition(0, 0);

        #endregion

        #region Constructor ...
        public AlignmentModel(double siteLatitude, double siteLongitude, double siteElevation)
        {
            SiteLatitude = siteLatitude;
            SiteLongitude = siteLongitude;
            SiteElevation = siteElevation;
            RefreshProximitySteps();

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
        public bool SyncToRaDec(long[] encoder, double[] origRaDec, long[] target, DateTime syncTime)
        {
            try
            {
                lock (_accessLock)
                {

                    // AlignmentPoints.Add(new AlignmentPoint(encoder, origRaDec, target, syncTime));
                    if (EQ_NPointAppend(new AlignmentPoint(encoder + this.EncoderMappingOffset, origRaDec, target + this.EncoderMappingOffset, syncTime)))
                    {

                    }
                    //_currentChecksum = int.MinValue;    // Reset checksum so that offsets are recalculated

                    //OneStarAdjustment[0] = observedAxes[0] - mountAxes[0];
                    //OneStarAdjustment[1] = observedAxes[1] - mountAxes[1];
                    SaveAlignmentPoints();

                    return true;
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
            _stringBuilder.AppendLine("ID \tEncoder Ra/Dec         \tOrig Ra/Dec        \tTarget RaDec   \tObserved time");
            foreach (var pt in AlignmentPoints)
            {
                _stringBuilder.AppendLine(
                    $"{pt.Id:D3}\t{pt.Encoder.RA}/{pt.Encoder.Dec}\t{pt.OrigRaDec.RA}/{pt.OrigRaDec.Dec}\t{pt.Target.RA}/{pt.Target.Dec}\t{pt.AlignTime}");
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
