using NStarAlignment.Utilities;
using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    public struct TimeRecord
    {
        public DateTime UtcTime { get; set; }
        public double LocalSiderealTime { get; set; }

        public TimeRecord(DateTime utcTime, double longitude)
        {
            UtcTime = utcTime;
            LocalSiderealTime = AstroConvert.UtcToLocalApparentSiderealTime(longitude, utcTime);
        }
    }
}
