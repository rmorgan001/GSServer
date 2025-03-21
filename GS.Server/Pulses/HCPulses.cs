/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Threading;
using GS.Principles;
using GS.Server.SkyTelescope;
using GS.Shared;

namespace GS.Server.Pulses
{
    public class HcPulseGuide
    {
        //Speeds 1-8 are used for the HC
        public int Speed { get; set; }
        //Pulse length in milliseconds
        public int Duration { get; set; }
        //Time in milliseconds between pulses when mouse is down
        public int Interval { get; set; }
        //Pulse rate in deg/sec
        public double Rate { get; set; }
    }

    public class HcDefaultPulseGuides
    {
        //default list of pulses for the HC
        public List<HcPulseGuide> DefaultPulseGuides =>
            new List<HcPulseGuide>
            {
                new HcPulseGuide { Speed = 1, Duration = 1000, Interval = 1000, Rate = 0.012 },
                new HcPulseGuide { Speed = 2, Duration = 1000, Interval = 1000, Rate = 0.024 },
                new HcPulseGuide { Speed = 3, Duration = 1000, Interval = 1000, Rate = 0.165 },
                new HcPulseGuide { Speed = 4, Duration = 1000, Interval = 1000, Rate = 0.238 },
                new HcPulseGuide { Speed = 5, Duration = 1000, Interval = 1000, Rate = 0.7 },
                new HcPulseGuide { Speed = 6, Duration = 1000, Interval = 1000, Rate = 1.4 },
                new HcPulseGuide { Speed = 7, Duration = 1000, Interval = 1000, Rate = 2.8 },
                new HcPulseGuide { Speed = 8, Duration = 1000, Interval = 1000, Rate = 3.5}
            };

        /// <summary>
        /// Gets a HcPulseGuide by speed number from the default list
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public HcPulseGuide GetDefaultHcPulseGuide(int speed)
        {
            return !DefaultPulseGuides.Exists(x => x.Speed == speed) ? null : DefaultPulseGuides.Find(x => x.Speed.Equals(speed));
        }
    }

    public class HcPulse
    {

        public int StartHcPulses( int direction, int interval, double rate)
        {
            try
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{direction}|{interval}|{rate}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return 0;
        }

    }
}


