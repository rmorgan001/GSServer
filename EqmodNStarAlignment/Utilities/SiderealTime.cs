using GS.Principles;
using System;
using System.Collections.Generic;
using System.Text;

namespace EqmodNStarAlignment.Utilities
{
    public static class SiderealTime
    {
        public static double GetLocalSiderealTime(double longitude)
        {
            return GetLocalSiderealTime(HiResDateTime.UtcNow, longitude);
        }

        public static double GetLocalSiderealTime(DateTime utcNow, double longitude)
        {
            var gsjd = JDate.Ole2Jd(utcNow);
            return Time.Lst(JDate.Epoch2000Days(), gsjd, false, longitude);
        }

    }
}
