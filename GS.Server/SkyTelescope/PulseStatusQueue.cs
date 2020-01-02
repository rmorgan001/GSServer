/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GS.Principles;
using GS.Shared;
using GS.Simulator;
using GS.SkyWatcher;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Monitors status of pulses sent to the mount
    /// </summary>
    public static class PulseStatusQueue
    {
        private static readonly BlockingCollection<PulseStatusEntry> _pulseBlockingCollection;

        static PulseStatusQueue()
        {
            try
            {
                _pulseBlockingCollection = new BlockingCollection<PulseStatusEntry>();
                Task.Factory.StartNew(() =>
                {
                    foreach (var PulseWaitEntry in _pulseBlockingCollection.GetConsumingEnumerable())
                    {
                        ProcessPulseStatusQueue(PulseWaitEntry);
                    }
                });
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount,
                    Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception ex)
            {
                ResetGuiding(0, false);
                ResetGuiding(1, false);

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server,
                    Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// adds a pulse wait item to a blocking queue for processing
        /// </summary>
        /// <param name="entry"></param>
        public static void AddPulseStatusEntry(PulseStatusEntry entry)
        {
            var add = _pulseBlockingCollection.TryAdd(entry);
            if (add) return;
            ResetGuiding(entry.Axis, false);
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Add Failed: {entry.Axis},{entry.Duration}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Process queue items monitor the pulse
        /// </summary>
        /// <param name="entry"></param>
        private static void ProcessPulseStatusQueue(PulseStatusEntry entry)
        {
            try
            {
                if (!SkyServer.Tracking || SkyServer.IsSlewing)
                {
                    ResetGuiding(entry);
                    return;
                }
                entry.ProcessDateTime = HiResDateTime.UtcNow;

                bool pulseRunning;
                switch (SkySettings.Mount)
                {
                    case MountType.Simulator:
                        if (entry.Axis == 0)
                        {
                            var statuscmd = new CmdPulseRaRunning(MountQueue.NewId);
                            pulseRunning = Convert.ToBoolean(MountQueue.GetCommandResult(statuscmd).Result);
                        }
                        else
                        {
                            var statuscmd = new CmdPulseDecRunning(MountQueue.NewId);
                            pulseRunning = Convert.ToBoolean(MountQueue.GetCommandResult(statuscmd).Result);
                        }
                        break;
                    case MountType.SkyWatcher:
                        if (entry.Axis == 0)
                        {
                            var statussky = new SkyPulseRaRunning(SkyQueue.NewId);
                            pulseRunning = Convert.ToBoolean(SkyQueue.GetCommandResult(statussky).Result);
                        }
                        else
                        {
                            var statussky = new SkyPulseDecRunning(SkyQueue.NewId);
                            pulseRunning = Convert.ToBoolean(SkyQueue.GetCommandResult(statussky).Result);
                        }
                        break;
                    default:
                        pulseRunning = false;
                        break;
                }
                ResetGuiding(entry.Axis, pulseRunning);

                var curtime = HiResDateTime.UtcNow;
                var processtime =  (int)(curtime - entry.ProcessDateTime).TotalMilliseconds;
                var alltime = (int)(curtime - entry.CreateDateTime).TotalMilliseconds;
                var monitorItem = new MonitorEntry
                { Datetime = curtime, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Complete,{alltime},{processtime}-{entry.Duration}={processtime - entry.Duration}"};
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception ex)
            {
                ResetGuiding(entry.Axis, false);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }
       
        /// <summary>
        /// Set pulseguiding to false for an entry
        /// </summary>
        /// <param name="entry"></param>
        private static void ResetGuiding(PulseStatusEntry entry)
        {
            ResetGuiding(entry.Axis, false);
        }

        /// <summary>
        /// Set a pulse guide to false for an axis
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="pulserunning"></param>
        private static void ResetGuiding(int axis, bool pulserunning)
        {
            switch (axis)
            {
                case 0:
                    SkyServer._isPulseGuidingRa = pulserunning;
                    break;
                case 1:
                    SkyServer._isPulseGuidingDec = pulserunning;
                    break;
                default:
                    SkyServer._isPulseGuidingDec = pulserunning;
                    SkyServer._isPulseGuidingRa = pulserunning;
                    break;
            }
        }
    }

    /// <summary>
    /// Definition for a pulse status entry
    /// </summary>
    public class PulseStatusEntry
    {
        public int Axis { get; set; }
        public int Duration { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime ProcessDateTime { get; set; }
    }
}
