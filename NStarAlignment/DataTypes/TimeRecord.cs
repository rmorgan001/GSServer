using NStarAlignment.Utilities;
using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    public struct TimeRecord
    {
        public DateTime UtcTime { get; private set; }
        public double LocalSiderealTime { get; private set; }

        public TimeRecord(DateTime utcTime, double localSiderealTime)
        {
            UtcTime = utcTime;
            LocalSiderealTime = localSiderealTime;
        }
    }

}
