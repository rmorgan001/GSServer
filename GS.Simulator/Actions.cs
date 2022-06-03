/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Shared;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace GS.Simulator
{
    /// <summary>
    /// Actions or commands send to the mount  
    /// </summary>
    public class Actions
    {
        #region Fields

        private readonly IOSerial _ioSerial;
        private readonly double[] _stepsPerSec = { 0.0, 0.0 };
        private readonly int[] _revSteps = { 0, 0 };

        #endregion

        #region Properties
        internal static bool IsConnected => IOSerial.IsConnected;
        internal MountInfo MountInfo { get; private set; }
        internal bool MonitorPulse { private get; set; }
        #endregion

        internal Actions()
        {
            _ioSerial = new IOSerial();
        }

        #region Action Commands

        /// <summary>
        /// Angle to step
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        internal long AngleToStep(Axis axis, double angle)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    return (long)(angle * 36000);
                case Axis.Axis2:
                    return (long)(angle * 36000);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Get axes in degrees
        /// </summary>
        /// <returns></returns>
        internal double[] AxesDegrees()
        {
            var x = Convert.ToDouble(_ioSerial.Send($"degrees|{Axis.Axis1}"));

            //put in for capture tracking in charts
            var stepsx = _ioSerial.Send($"steps|{Axis.Axis1}");
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"steps1|{null}|{stepsx}" };
            MonitorLog.LogToMonitor(monitorItem);

            var y = Convert.ToDouble(_ioSerial.Send($"degrees|{Axis.Axis2}"));

            //put in for capture tracking in charts
            var stepsy = _ioSerial.Send($"steps|{Axis.Axis2}");
            monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"steps2|{null}|{stepsy}" };
            MonitorLog.LogToMonitor(monitorItem);


            var d = new[] { x, y };
            return d;
        }

        /// <summary>
        /// Slew to target
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="target"></param>
        internal void AxisGoToTarget(Axis axis, double target)
        {
            _ioSerial.Send($"gototarget|{axis}|{target}");
        }

        /// <summary>
        /// Set an axis in degrees
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void AxisToDegrees(Axis axis, double degrees)
        {
            _ioSerial.Send($"setdegrees|{axis}|{degrees}");
        }

        /// <summary>
        /// Send pulse
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="guideRate"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        internal bool AxisPulse(Axis axis, double guideRate, int duration)
        {
            if (axis == Axis.Axis1)
            {
                MountQueue.IsPulseGuidingRa = true;
                MountQueue.IsPulseGuidingDec = false;
            }
            else
            {
                MountQueue.IsPulseGuidingDec = true;
                MountQueue.IsPulseGuidingRa = false;
            }

            var arcsecs = duration / 1000.0 * Conversions.Deg2ArcSec(Math.Abs(guideRate));
            if (arcsecs < .0002)
            {
                MountQueue.IsPulseGuidingRa = false;
                MountQueue.IsPulseGuidingDec = false;
                return false;
            }

            // setup to log and graph the pulse
            var pulseEntry = new PulseEntry();
            if (MonitorPulse)
            {
                pulseEntry.Axis = (int)axis;
                pulseEntry.Duration = duration;
                pulseEntry.Rate = guideRate;
                pulseEntry.StartTime = HiResDateTime.UtcNow;
                //todo change back to 20
                if (duration < 20) pulseEntry.Rejected = true;
            }

            // execute pulse
            _ioSerial.Send($"pulse|{axis}|{guideRate}");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < duration)
            {
                //do something while waiting
            }
            sw.Reset();
            _ioSerial.Send($"pulse|{axis}|{0}");

            MountQueue.IsPulseGuidingRa = false;
            MountQueue.IsPulseGuidingDec = false;
            
            if (MonitorPulse)
            {
                MonitorLog.LogToMonitor(pulseEntry);
            }
            return false;
        }

        /// <summary>
        /// Start axis tracking
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void AxisTracking(Axis axis, double degrees)
        {
            _ioSerial.Send($"tracking|{axis}|{degrees}");
        }

        /// <summary>
        /// Slew an axis
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void AxisSlew(Axis axis, double degrees)
        {
            _ioSerial.Send($"slew|{axis}|{degrees}");
        }

        /// <summary>
        /// Get axis status
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal AxisStatus AxisStatus(Axis axis)
        {
            var a = _ioSerial.Send($"axisstatus|{axis}");
            var strings = a.Split('|');
            var b = new AxisStatus()
            {
                //Axis = axis,
                Slewing = Convert.ToBoolean(strings[0]),
                Stopped = Convert.ToBoolean(strings[1]),
                //Tracking = Convert.ToBoolean(strings[2]),
                //Rate = Convert.ToBoolean(strings[3])
            };
            return b;
        }

        /// <summary>
        /// Stop an axis
        /// </summary>
        /// <param name="axis"></param>
        internal void AxisStop(Axis axis)
        {
            _ioSerial.Send($"stop|{axis}");
        }

        /// <summary>
        /// Get axis in steps
        /// </summary>
        /// <returns></returns>
        internal int[] AxisSteps()
        {
            var x = Convert.ToInt32(_ioSerial.Send($"steps|{Axis.Axis1}"));
            var y = Convert.ToInt32(_ioSerial.Send($"steps|{Axis.Axis2}"));
            var i = new[] { x, y };
            return i;
        }

        internal Tuple<double?, DateTime> AxisStepsDt(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    return new Tuple<double?, DateTime>(Convert.ToInt32(_ioSerial.Send($"steps|{Axis.Axis1}")),
                        HiResDateTime.UtcNow);
                case Axis.Axis2:
                    return new Tuple<double?, DateTime>(Convert.ToInt32(_ioSerial.Send($"steps|{Axis.Axis2}")),
                        HiResDateTime.UtcNow);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Get factor to covert steps to degrees
        /// </summary>
        /// <returns></returns>
        internal int FactorSteps()
        {
            var x = Convert.ToInt32(_ioSerial.Send($"FactorSteps"));
            return x;
        }

        /// <summary>
        /// Hand control slew
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void HcSlew(Axis axis, double degrees)
        {
            _ioSerial.Send($"hc|{axis}|{degrees}");
            AxesDegrees();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        internal void InitializeAxes()
        {
            _ioSerial.Send("initialize");
            LoadDefaults();
        }

        /// <summary>
        /// Loads mount defaults, both axes are the same size
        /// </summary>
        private void LoadDefaults()
        {
            //steps per revolution
            var a = Convert.ToInt32(_ioSerial.Send("getrevsteps"));
            _revSteps[0] = a;
            _revSteps[1] = a;
            //steps per second
            var b = Conversions.StepPerArcSec(a);
            _stepsPerSec[0] = b;
            _stepsPerSec[1] = b;

            var c = _ioSerial.Send("capabilities");
            var d = c.Split('|');
            var mountInfo = new MountInfo
            {
                //CanAxisSlewsIndependent = Convert.ToBoolean(d[0]),
                //CanAzEq = Convert.ToBoolean(d[1]),
                //CanDualEncoders = Convert.ToBoolean(d[2]),
                //CanHalfTrack = Convert.ToBoolean(d[3]),
                CanHomeSensors = Convert.ToBoolean(d[4]),
                //CanPolarLed = Convert.ToBoolean(d[5]),
                //CanPPec = Convert.ToBoolean(d[6]),
                //CanWifi = Convert.ToBoolean(d[7]),
                //MountName = d[8],
                //MountVersion = d[9]
            };
            MountInfo = mountInfo;
        }

        /// <summary>
        /// home sensor status
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal int HomeSensor(Axis axis)
        {
            return Convert.ToInt32(_ioSerial.Send($"homesensor|{axis}"));
        }

        /// <summary>
        /// reset home sensor
        /// </summary>
        /// <param name="axis"></param>
        internal void HomeSensorReset(Axis axis)
        {
            _ioSerial.Send($"homesensorreset|{axis}");
        }

        /// <summary>
        /// Get name
        /// </summary>
        /// <returns></returns>
        internal string MountName()
        {
            return _ioSerial.Send("mountname");
        }

        /// <summary>
        /// Gets version
        /// </summary>
        /// <returns></returns>
        internal string MountVersion()
        {
            var result = _ioSerial.Send("mountversion");

            var first = int.Parse(result.Substring(0, 2), NumberStyles.HexNumber);
            var second = int.Parse(result.Substring(2, 2), NumberStyles.HexNumber);
            return $"{first}.{second:D2}";
        }

        internal void SnapPort(int port, bool on)
        {
            _ioSerial.Send($"snapport|{port}|{on}");
        }

        /// <summary>
        /// Gets Steps Per Revolution
        /// </summary>
        /// <returns></returns>
        internal long Spr()
        {
            return Convert.ToInt32(_ioSerial.Send("spr"));
        }

        /// <summary>
        /// Gets Steps Per Worm Revolution
        /// </summary>
        /// <returns></returns>
        internal double Spw()
        {
            return Convert.ToDouble(_ioSerial.Send("spw"));
        }

        /// <summary>
        /// Rates from driver
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void Rate(Axis axis, double degrees)
        {
            _ioSerial.Send($"rate|{axis}|{degrees}");
        }

        /// <summary>
        /// set simulator goto rate
        /// </summary>
        /// <param name="rate"></param>
        internal void GotoRate(int rate)
        {
            _ioSerial.Send($"gotorate|{rate}");
        }

        /// <summary>
        /// Axis rates from driver
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="degrees"></param>
        internal void RateAxis(Axis axis, double degrees)
        {
            _ioSerial.Send($"rateaxis|{axis}|{degrees}");
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        internal void Shutdown()
        {
            _ioSerial.Send("shutdown");
        }

        #endregion
    }
}
