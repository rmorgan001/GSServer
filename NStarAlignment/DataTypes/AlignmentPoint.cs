using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NStarAlignment.DataTypes
{
    public class AlignmentPointCollection : ObservableCollection<AlignmentPoint>
    {
    }

    public class AlignmentPoint
    {
        public double[] RaDec { get; set; }
        
        /// <summary>
        /// Altitude and azimuth stored for UI purposes.
        /// </summary>
        public double[] AltAz { get; set; }

        public AxisPosition MountAxes { get; set; }
        public AxisPosition SkyAxes { get; set; }

        [JsonIgnore]
        public string Correction => $"{(SkyAxes[0] - MountAxes[0])*3600.0:F2}/{(SkyAxes[1] - MountAxes[1])*3600.0:F2}";

        [JsonIgnore]
        public string Synched => $"{SyncTime.UtcTime:G}";

        /// <summary>
        /// Local time of synchronization (UTC)
        /// </summary>
        [JsonProperty]
        public TimeRecord SyncTime { get; private set; }


        private AlignmentPoint()
        {
        }

        /// <summary>
        /// Groups sync details into a single struct
        /// </summary>
        /// <param name="raDec"></param>
        /// <param name="altAz"></param>
        /// <param name="mountAxes"></param>
        /// <param name="skyAxes"></param>
        /// <param name="syncTime"></param>
        public AlignmentPoint(double[] raDec, double[] altAz, AxisPosition mountAxes, AxisPosition skyAxes, TimeRecord syncTime)
        :this()
        {
            RaDec = raDec;
            AltAz = altAz;
            MountAxes = mountAxes;
            SkyAxes = skyAxes;
            SyncTime = syncTime;
        }


    }

}
