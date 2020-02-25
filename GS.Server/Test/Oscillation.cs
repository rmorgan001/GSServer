/* Copyright(C) 2020  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ASCOM.DeviceInterface;
using GS.Principles;
using GS.Shared;

namespace GS.Server.Test
{
    public class Oscillation
    {
        private readonly List<OscillationPair> Oscillations;
        public Oscillation()
        {
            Oscillations = new List<OscillationPair>();
        }
        public void AddOscillation(OscillationPair pair)
        {
            if (double.IsNaN(pair.Data)) return;
            if (Math.Abs(pair.Data) < 0.0) return;
            pair.Data = Math.Abs(pair.Data);
            Oscillations.Add(pair);
        }

        public DateTime GetStartTime()
        {
            return Oscillations.Min(a => a.TimeStamp);
        }

        public DateTime GetEndTime()
        {
            return Oscillations.Max(a => a.TimeStamp);
        }
        public void ClearOscillations()
        {
            Oscillations.Clear();
        }

        public void RemoveOscillation(OscillationPair pair)
        {
            Oscillations.Remove(pair);
        }

        public OscillationResult GetResult(GuideDirections direction)
        {
            return GetResult(direction, GetStartTime(), GetEndTime());
        }

        public OscillationResult GetResult(GuideDirections direction, DateTime start, DateTime end)
        {
            try
            {
                //make a new list based on time
                var list = Oscillations.Where(x => x.TimeStamp >= start && x.TimeStamp <= end).OrderBy(x => x.TimeStamp).ToList();
                if (!list.Any()) return null; 
                var tempdata = new List<OscillationData>();

                GuideDirections lastdirection;
                var s = new OscillationData();
                var n = new OscillationData();
                var e = new OscillationData();
                var w = new OscillationData();

                switch (direction)
                {
                    case GuideDirections.guideNorth:
                    case GuideDirections.guideSouth:
                        lastdirection = GuideDirections.guideEast;
                        foreach (var pair in list)
                        {
                            switch (pair.Direction)
                            {
                                case GuideDirections.guideNorth:
                                    if (lastdirection == GuideDirections.guideSouth)
                                    {
                                        if (s.Total > 0) { tempdata.Add(s); }
                                        s = new OscillationData();
                                    }

                                    n.Direction = GuideDirections.guideNorth;
                                    n.Strength++;
                                    if (n.Max > pair.Data){n.Max = pair.Data;}
                                    n.Total += pair.Data;

                                    lastdirection = GuideDirections.guideNorth;
                                    break;
                                case GuideDirections.guideSouth:
                                    if (lastdirection == GuideDirections.guideNorth)
                                    {
                                        if (n.Total > 0){tempdata.Add(n);}
                                        n = new OscillationData();
                                    }

                                    s.Direction = GuideDirections.guideSouth;
                                    s.Strength++;
                                    if (s.Max > pair.Data) { s.Max = pair.Data; }
                                    s.Total += pair.Data;

                                    lastdirection = GuideDirections.guideSouth;
                                    break;
                            }
                        }

                        tempdata.Add(s);
                        tempdata.Add(n);
                        break;
                    case GuideDirections.guideEast:
                    case GuideDirections.guideWest:
                        lastdirection = GuideDirections.guideNorth;
                        foreach (var pair in list)
                        {
                            switch (pair.Direction)
                            {
                                case GuideDirections.guideEast:
                                    if (lastdirection == GuideDirections.guideWest)
                                    {
                                        if (w.Total > 0) { tempdata.Add(w); }
                                        w = new OscillationData();
                                    }

                                    e.Direction = GuideDirections.guideEast;
                                    e.Strength++;
                                    if (e.Max > pair.Data) { e.Max = pair.Data; }
                                    e.Total += pair.Data;

                                    lastdirection = GuideDirections.guideEast;
                                    break;
                                case GuideDirections.guideWest:
                                    if (lastdirection == GuideDirections.guideEast)
                                    {
                                        if (e.Total > 0) { tempdata.Add(e); }
                                        e = new OscillationData();
                                    }

                                    w.Direction = GuideDirections.guideWest;
                                    e.Strength++;
                                    if (w.Max > pair.Data) { w.Max = pair.Data; }
                                    w.Total += pair.Data;

                                    lastdirection = GuideDirections.guideWest;
                                    break;
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                // create result
                int total = tempdata.Count;

                if (total == 0){return null;}

                var tmpAvgStrength = Math.Round(tempdata.Average(x => x.Strength),2);

                var list1 = tempdata.Where(x => x.Direction == GuideDirections.guideNorth).ToList();
                var tmpAvgStrength1 = 0.0;
                var tmpPercent1 = 0.0;
                if (list1.Any())
                {
                    tmpAvgStrength1 = Math.Round(list1.Average(item => item.Strength),2);
                    tmpPercent1 = Math.Round(list1.Count / (total * 1.0) * 100, 1);
                }

                var list2 = tempdata.Where(x => x.Direction == GuideDirections.guideSouth).ToList();
                var tmpAvgStrength2 = 0.0;
                var tmpPercent2 = 0.0;
                if (list2.Any())
                {
                    tmpAvgStrength2 = Math.Round(list2.Average(item => item.Strength), 2);
                    tmpPercent2 = Math.Round(list2.Count / (total * 1.0) * 100, 1);
                }

                var result = new OscillationResult
                {
                    //Data = tempdata, 
                    StartTime = start,
                    EndTime = end,
                    Direction1 = GuideDirections.guideNorth,
                    Direction2 = GuideDirections.guideSouth,
                    AvgStrength = tmpAvgStrength,
                    AvgStrength1 = tmpAvgStrength1,
                    AvgStrength2 = tmpAvgStrength2,
                    Percent1 = tmpPercent1,
                    Percent2 = tmpPercent2
                };

                return result;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                return null;
            }
        }
    }

    public class OscillationPair
    {
        public double Data { get; set; }
        public DateTime TimeStamp { get; set; }
        public GuideDirections Direction { get; set; }
    }

    public class OscillationResult
    {
        public double AvgStrength { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GuideDirections Direction1 { get; set; }
        public double AvgStrength1 { get; set; }
        public double Percent1 { get; set; }
        public GuideDirections Direction2 { get; set; }
        public double AvgStrength2 { get; set; }
        public double Percent2 { get; set; }
        //public List<OscillationData> Data { get; set; }
    }

    public class OscillationData
    {
        public double Strength { get; set; }
        public double Total { get; set; }
        public double Max { get; set; }
        public GuideDirections Direction { get; set; }
    }
}
