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
using GS.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GS.SkyWatcher
{
    /// <summary>
    /// Main SkyWatcher class for movements, status, and information.
    /// Based on the original skyWatcher-pacific project 5/31/2011: release C# basic API 1.0
    /// Constructor takes a connected AsCom serial object. if AsCom isn't needed replace with
    /// standard serial object and remove AsCom references. 
    /// </summary>
    public class SkyWatcher
    {
        #region Fields

        private readonly Commands _commands;
        // 128.9 steps per sidereal second * rads per second = steps per rad second
        private const double _lowSpeedMargin = (128 * Constant.SiderealRate);
        private readonly double[] _slewingSpeed = { 0, 0 }; // store for last used speed in radians
        private readonly double[] _targetPositions = { 0, 0 }; //  Store for last used target coordinate 
        private readonly double[] _trackingRates = { 0, 0 };
        private readonly long[] _trackingSpeeds = { 0, 0 };
        private double[] _factorRadRateToInt = { 0, 0 };
        private long[] _highSpeedRatio = new long[2];
        private long[] _axisVersion = new long[2];
        private long[] _lowSpeedGotoMargin = new long[2];
        private long[] _breakSteps = new long[2];
        private readonly double[] _stepsPerSecond = new double[2];

        #endregion

        #region Properties

        internal bool IsConnected => _commands.IsConnected;

        private bool _pPecOn;
        internal bool IsPPecOn
        {
            get
            {
                ParseCapabilities();
                return _pPecOn;
            }
            private set => _pPecOn = value;
        }

        private bool _pPecTraining;
        internal bool IsPPecInTrainingOn
        {
            get
            {
                ParseCapabilities();
                return _pPecTraining;
            }
            private set => _pPecTraining = value;
        }
        internal bool DecPulseGoTo { get; set; }
        internal bool AlternatingPPec { get; set; }
        internal bool CanAzEq { get; private set; }
        internal bool CanHomeSensors { get; private set; }
        internal bool CanPPec { get; private set; }
        internal bool CanOneStepDec { get; private set; }
        internal bool CanOneStepRa { get; private set; }
        internal bool CanDualEncoders { get; private set; }
        internal bool CanWifi { get; private set; }
        internal bool CanHalfTrack { get; private set; }
        internal bool CanAxisSlewsIndependent { get; private set; }
        internal bool CanPolarLed { get; private set; }
        internal bool MonitorPulse { private get; set; }
        internal string MountType { get; private set; }
        private int MountNum { get; set; }
        internal string MountVersion { get; private set; }
        internal bool SouthernHemisphere { private get; set; }
        internal bool PulseRaRunning { get; private set; }
        internal bool PulseDecRunning { get; private set; }
        internal int MinPulseDurationRa { get; set; }
        internal int MinPulseDurationDec { get; set; }
        #endregion

        #region Server Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serial">Needs a connected serial object</param>
        /// <param name="customMount360Steps">Custom Mount :a replacement</param>
        /// <param name="customRaWormSteps">Custom Mount :s replacement</param>
        internal SkyWatcher(SerialPort serial, int[] customMount360Steps, double[] customRaWormSteps)
        {
            _commands = new Commands(serial, customMount360Steps,  customRaWormSteps);
            // _commands.TestSerial();
            MinPulseDurationRa = 20;
            MinPulseDurationDec = 20;
        }

        /// <summary>
        /// Start tracking based degree rate.  Use for tracking and guiding, not go tos
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="rate">Rate in degrees per arc sec</param>
        internal void AxisSlew(AxisId axis, double rate)
        {
            rate = Principles.Units.Deg2Rad1(rate);

            var internalSpeed = Math.Abs(rate); // setup a positive speed
            if (internalSpeed > 500) { internalSpeed = 500; }

            var forward = rate > 0.0; // figures out the direction of motion
            var highspeed = false;

            // Stop if speed is too slow
            if (internalSpeed <= Constant.SiderealRate / 1000.0)
            {
                AxisStop(axis);

                // Wait until the axis stops or counter runs out
                var stopwatch = Stopwatch.StartNew();
                var counter = 1;
                while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                {
                    var axesstop = _commands.GetAxisStatus(axis);
                    // Return if the axis has stopped.
                    if (axesstop.FullStop) { break; }
                    // issue new stop
                    if (counter % 5 == 0) { AxisStop(axis); }
                    counter++;
                    Thread.Sleep(50);
                }
                return;
            }

            // Calculate and set step period. 
            if (internalSpeed > _lowSpeedMargin)
            {
                internalSpeed /= _highSpeedRatio[(int)axis]; // High speed adjustment
                highspeed = true;
            }

            var speedInt = CalculateSpeed(axis, internalSpeed); // Calculate mount speed

            SetRates(axis, rate);

            var axesstatus = _commands.GetAxisStatus(axis); // Get axis status
            if (axesstatus.FullStop || // Already stopped
                (axesstatus.HighSpeed != highspeed) || // Change high speed
                highspeed ||
                (axesstatus.SlewingForward && !forward) || // Change direction 
                (!axesstatus.SlewingForward && forward) // Change direction
            )
            {
                if (!axesstatus.FullStop)
                {
                    // stop the motor to change motion
                    AxisStop(axis);

                    // Wait until the axis stops or counter runs out
                    var stopwatch = Stopwatch.StartNew();
                    var counter = 1; 
                    while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                    {
                        axesstatus = _commands.GetAxisStatus(axis);
                        // Return if the axis has stopped.
                        if (axesstatus.FullStop) { break; }
                        // issue new stop
                        if (counter % 5 == 0) { AxisStop(axis); }
                        counter++;
                        Thread.Sleep(50);
                    }
                }

                // Once stopped start a new motion mode
                _commands.SetMotionMode(axis, highspeed ? 3 : 1, forward ? 0 : 1, SouthernHemisphere); // G
            }

            _commands.SetStepSpeed(axis, speedInt); // I Set the axis step count
            _commands.StartMotion(axis); // J Start motion
            _commands.SetSlewing((int)axis, forward, highspeed); // Set the axis status
            _slewingSpeed[(int)axis] = rate; //store axis rate
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Directs a pulse guide command to an axis, hemisphere and tracking rate needs to be set first
        /// </summary>
        /// <param name="axis">Axis 1 or 2</param>
        /// <param name="guideRate">Guide rate degrees, 15.041/3600*.5, negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, always positive numbers</param>
        /// <param name="backlashSteps">Positive micro steps added for backlash</param>
        internal void AxisPulse(AxisId axis, double guideRate, int duration, int backlashSteps = 0)
        {
            if (axis == AxisId.Axis1)
            {
                PulseRaRunning = true;
                PulseDecRunning = false;
            }
            else
            {
                PulseDecRunning = true;
                PulseRaRunning = false;
            }

            var datetime = Principles.HiResDateTime.UtcNow;
            var monitorItem = new MonitorEntry
            { Datetime = datetime, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{guideRate}|{duration}|{backlashSteps}|{MinPulseDurationRa}|{MinPulseDurationDec}|{DecPulseGoTo}" };
            MonitorLog.LogToMonitor(monitorItem);

            var pulseEntry = new PulseEntry
            {
                Axis = (int)axis,
                Duration = duration,
                Rate = guideRate,
                Rejected = false,
                StartTime = datetime,
            }; // setup to log and graph the pulse

            backlashSteps = Math.Abs(backlashSteps);
            var arcsecs = duration / 1000.0 * Math.Abs(guideRate) * 3600.0;

            if (axis == AxisId.Axis1)
            {
                if (backlashSteps > 0) // Convert lash to extra pulse duration in milliseconds
                {
                    var lashduration = backlashSteps / _stepsPerSecond[0] / 3600 / Math.Abs(guideRate) * 1000;
                    duration += (int)lashduration;
                }
                 
                if (duration < MinPulseDurationRa)
                {
                    PulseRaRunning = false;
                    if (!MonitorPulse) { return; }
                    pulseEntry.Rejected = true;
                    MonitorLog.LogToMonitor(pulseEntry);
                    return;
                }

                var rate = SouthernHemisphere ? -Math.Abs(_trackingRates[0]) : Math.Abs(_trackingRates[0]);

                var radRate = BasicMath.DegToRad(guideRate);
                var aplyRate = rate + radRate;
                if (Math.Abs(aplyRate) < 0.000001)               // When guide rate is 100% check to avoid possible zero                        
                {                                                // small numbers will mess up the CalculateSpeed.
                    aplyRate = Constant.SiderealRate / 1000.0;   // assign a number so mount looks like it's stopped
                }
                var speedInt = CalculateSpeed(AxisId.Axis1, aplyRate);  // Calculate mount speed  
                
                if (_pPecOn && AlternatingPPec){SetPPec(AxisId.Axis1, false);} // implements the alternating pPEC 

                _commands.SetStepSpeed(AxisId.Axis1, speedInt); // :I Send pulse to axis

                pulseEntry.StartTime = _commands.LastI1RunTime; // get the last :I start time

                var raPulseTime = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; // possible use for min pulse duration time
                var msspan = duration - raPulseTime.TotalMilliseconds; // remove execute time from the duration

                if (msspan > 0 && msspan < duration) // checking duration is met
                {
                    var sw1 = Stopwatch.StartNew();
                    while (sw1.Elapsed.TotalMilliseconds < msspan) { Thread.Sleep(1); } // loop while counting to duration
                }

                if (MonitorPulse){pulseEntry.Duration = duration;}

                _commands.SetStepSpeed(AxisId.Axis1, _trackingSpeeds[0]); // :I set speed back to current tracking speed

                PulseRaRunning = false;

                if (_pPecOn && AlternatingPPec){SetPPec(AxisId.Axis1, true);} // implements the alternating pPEC
            }

            else
            {
                if (DecPulseGoTo)
                {
                    // Turns the pulse into steps and does a goto which is faster than using guide rate
                    var stepsNeeded = (int)(arcsecs * _stepsPerSecond[1]);
                    stepsNeeded += backlashSteps;

                    if (backlashSteps > 0) // Convert lash to pulse duration in milliseconds
                    {
                        var lashduration = backlashSteps / _stepsPerSecond[1] / 3600 / Math.Abs(guideRate) * 1000;
                        duration += (int)lashduration;
                    }

                    if (stepsNeeded < 1 || duration < MinPulseDurationDec)
                    {
                        PulseDecRunning = false;
                        if (!MonitorPulse) return;
                        pulseEntry.Rejected = true;
                        MonitorLog.LogToMonitor(pulseEntry);
                        return;
                    }
                    if (guideRate < 0) { stepsNeeded = -stepsNeeded; }

                    //Firmware for the EQ8 and EQ6 can't move a single step so this compensates, 2015B3 corrects this 
                    AxisStatus axesstatus;
                    if (!CanOneStepDec) // check if mount is capable of small steps in goto mode
                    {
                        var jumpstep = 0;
                        if (Math.Abs(stepsNeeded) < 20)
                        {
                            if (stepsNeeded < 0) // -5
                            {
                                jumpstep = 20;
                                stepsNeeded -= jumpstep; // -5 - 20
                            }

                            if (stepsNeeded > 0) // 5
                            {
                                jumpstep = -20;
                                stepsNeeded += Math.Abs(jumpstep); // 5 + 20
                            }

                            AxisMoveSteps(AxisId.Axis2, jumpstep);

                            axesstatus = _commands.GetAxisStatus(AxisId.Axis2);
                            if (!axesstatus.FullStop)
                            {
                                // Wait until the axis stops or counter runs out
                                var sw2 = Stopwatch.StartNew();
                                while (sw2.Elapsed.TotalMilliseconds <= 3500)
                                {
                                    axesstatus = _commands.GetAxisStatus(axis);
                                    // Return if the axis has stopped.
                                    if (axesstatus.FullStop) { break; }
                                    Thread.Sleep(10);
                                }
                                if (!axesstatus.FullStop) { AxisStop(axis); }
                            }
                        }
                    }

                    pulseEntry.StartTime = Principles.HiResDateTime.UtcNow;

                    AxisMoveSteps(AxisId.Axis2, stepsNeeded);

                    axesstatus = _commands.GetAxisStatus(AxisId.Axis2);
                    if (!axesstatus.FullStop)
                    {
                        // Wait until the axis stops or counter runs out
                        var stopwatch = Stopwatch.StartNew();
                        while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                        {
                            axesstatus = _commands.GetAxisStatus(AxisId.Axis2);
                            // Return if the axis has stopped.
                            if (axesstatus.FullStop) { break; }
                            Thread.Sleep(10);
                        }
                    }
                    PulseDecRunning = false;
                    if (MonitorPulse)
                    {
                        pulseEntry.Duration = duration;

                    }
                }
                else
                {
                    if (backlashSteps > 0) // Convert lash to extra pulse duration in milliseconds
                    {
                        var lashduration = Convert.ToInt32(backlashSteps / _stepsPerSecond[1] / 3600 / Math.Abs(guideRate) * 1000);
                        if (lashduration > 1000)
                        {
                            lashduration = 1000;
                        } // PHD will error if pulse doesn't return within 2 seconds.
                        duration += lashduration; // add the lash time to duration
                    }

                    if (duration < MinPulseDurationDec)
                    {
                        PulseDecRunning = false;
                        if (!MonitorPulse) { return; }
                        pulseEntry.Rejected = true;
                        MonitorLog.LogToMonitor(pulseEntry);
                        return;
                    }

                    AxisSlew(AxisId.Axis2, guideRate); // Send pulse to axis 

                    pulseEntry.StartTime = _commands.LastJ2RunTime; // last :J2 start time

                    var decPulseTime = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; // possible use for min pulse duration time
                    var msspan = duration - decPulseTime.TotalMilliseconds; // remove execute time from the duration

                    if (msspan > 0 && msspan < duration) // checking duration is met
                    {
                        var sw3 = Stopwatch.StartNew();
                        while (sw3.Elapsed.TotalMilliseconds < msspan) { Thread.Sleep(1); } // do something while waiting;
                    }

                    AxisStop(AxisId.Axis2);

                    PulseDecRunning = false;

                    if (MonitorPulse)
                    {
                        pulseEntry.Duration = duration;
                    }
                }
            }

            if (MonitorPulse)
            {
                MonitorLog.LogToMonitor(pulseEntry);//send to monitor
            }

        }

        /// <summary>
        /// Stop the target axis normally
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStop(AxisId axis)
        {
            _slewingSpeed[(int)axis] = 0;
            _commands.AxisStop(axis);
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Stop the target axis instantly
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStopInstant(AxisId axis)
        {
            _slewingSpeed[(int)axis] = 0;
            _commands.AxisStopInstant(axis);
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Use goto mode to target position
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="movingSteps">Micro steps to move</param>
        internal void AxisMoveSteps(AxisId axis, long movingSteps)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{movingSteps}" };
            MonitorLog.LogToMonitor(monitorItem);

            int direction;

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return;
            }

            // Set moving direction
            if (movingSteps > 0)
            {
                direction = 0;
            }
            else
            {
                direction = 1;
                movingSteps = Math.Abs(movingSteps);
            }

            //Might need to check whether motor has stopped.
            var axesstatus = _commands.GetAxisStatus(axis);
            if (!axesstatus.FullStop)
            {
                AxisStop(axis);

                // Wait until the axis stops or counter runs out
                var stopwatch = Stopwatch.StartNew();
                var counter = 1;
                while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                {
                    axesstatus = _commands.GetAxisStatus(axis);
                    // Return if the axis has stopped.
                    if (axesstatus.FullStop) { break; }
                    // issue new stop
                    if (counter % 5 == 0) { AxisStop(axis); }
                    counter++;
                    Thread.Sleep(50);
                }
            }

            _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // G: '2' low  speed GOTO mode, '0'  +CW  and Nth Hemisphere
            _commands.SetGotoTargetIncrement(axis, movingSteps); // H:
            _commands.SetBreakPointIncrement(axis, 0); // M: send 0 steps
            _commands.StartMotion(axis); // J: Start moving
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Use goto mode to target position 
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="targetPosition">Total radians of target position</param>
        internal void AxisGoToTarget(AxisId axis, double targetPosition)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{targetPosition}" };
            MonitorLog.LogToMonitor(monitorItem);

            int direction;

            var curPosition = _commands.GetAxisPosition(axis); // :j Get current position (radians) of the axis.

            // Calculate slewing distance.
            // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in GetAxisPosition().
            var movingAngle = targetPosition - curPosition;

            var movingSteps = _commands.AngleToStep(axis, movingAngle);// Convert distance in radian into steps.

            bool forward;
            bool highspeed;

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return;
            }

            // Set moving direction
            if (movingSteps > 0)
            {
                direction = 0;
                forward = true;
            }
            else
            {
                direction = 1;
                movingSteps = Math.Abs(movingSteps);
                forward = false;
            }

            //Might need to check whether motor has stopped.
            var axesstatus = _commands.GetAxisStatus(axis);
            if (!axesstatus.FullStop)
            {
                // stop the motor to change motion
                AxisStop(axis);

                // Wait until the axis stops or counter runs out
                var sw = Stopwatch.StartNew();
                var counter = 1;
                while (sw.Elapsed.TotalMilliseconds <= 3500)
                {
                    axesstatus = _commands.GetAxisStatus(axis);
                    // Return if the axis has stopped.
                    if (axesstatus.FullStop) { break; }
                    if (counter % 5 == 0) { AxisStop(axis); }
                    counter++;
                    Thread.Sleep(50);
                }
            }

            // Check if the distance is long enough to trigger a high speed GOTO.
            if (movingSteps > _lowSpeedGotoMargin[(int)axis])
            {
                _commands.SetMotionMode(axis, 0, direction, SouthernHemisphere); // :G high speed GOTO slewing 
                highspeed = true;
            }
            else
            {
                _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // :G low speed GOTO slewing
                highspeed = false;
            }


            _commands.SetGotoTargetIncrement(axis, movingSteps); // :H
            _commands.SetBreakPointIncrement(axis, _breakSteps[(int)axis]); // :M
            _commands.StartMotion(axis); // :J
            _commands.GetAxisPositionCounter(axis); // read for plotting

            _targetPositions[(int)axis] = targetPosition;
            _commands.SetSlewingTo((int)axis, forward, highspeed);
        }

        /// <summary>
        /// Get axis position in degrees
        /// </summary>
        /// <returns>array in degrees, could return array of NaN if no responses returned</returns>
        internal double[] GetPositionsInDegrees()
        {
            var positions = new double[] { 0, 0 };

            var x = _commands.GetAxisPositionNaN(AxisId.Axis1);
            if (!double.IsNaN(x)) { x = Principles.Units.Rad2Deg1(x); }
            positions[0] = x;

            var y = _commands.GetAxisPositionNaN(AxisId.Axis2);
            if (!double.IsNaN(y)) { y = Principles.Units.Rad2Deg1(y); }
            positions[1] = y;

            return positions;
        }

        /// <summary>
        /// Get axis position in steps
        /// </summary>
        /// <returns>array in steps, could return array of NaN if no responses returned</returns>
        internal double[] GetSteps()
        {
            var positions = new double[] { 0, 0 };

            var x = _commands.GetAxisStepsNaN(AxisId.Axis1);
            positions[0] = x;

            var y = _commands.GetAxisStepsNaN(AxisId.Axis2);
            positions[1] = y;

            return positions;
        }

        /// <summary>
        /// Gets axes board versions in a readable format
        /// </summary>
        /// <returns></returns>
        internal string[] GetAxisStringVersions()
        {
            var ret = _commands.GetAxisStringVersions();

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ret[0]}|{ret[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return ret;
        }

        /// <summary>
        /// Gets the long format of the axes board versions
        /// </summary>
        /// <returns></returns>
        internal long[] GetAxisVersions()
        {
            var ret = _commands.GetAxisVersions();

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ret[0]}|{ret[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return ret;
        }

        /// <summary>
        /// Gets a struct of status information from an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns></returns>
        internal AxisStatus GetAxisStatus(AxisId axis)
        {
            return _commands.GetAxisStatus(axis);
        }

        /// <summary>
        /// Gets last struct of status information but does not run a new mount command
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns></returns>
        internal AxisStatus GetCacheAxisStatus(AxisId axis)
        {
            return _commands.GetCacheAxisStatus(axis);
        }

        /// <summary>
        /// Get the sidereal rate in step counts
        /// </summary>
        /// <returns></returns>
        internal long GetSiderealRate(AxisId axis)
        {
            return _commands.GetSiderealRate(axis);
        }

        /// <summary>
        /// reset the position of an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="newValue">radians</param>
        internal void SetAxisPosition(AxisId axis, double newValue)
        {
            _commands.SetAxisPosition(axis, newValue);
        }

        /// <summary>
        /// Turn on/off individual axis encoders
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetEncoder(AxisId axis, bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {axis}|{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (!CanDualEncoders) { return; }
            _commands.SetEncoders(axis, on);
        }

        /// <summary>
        /// Turn on/off pPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal string SetPPec(AxisId axis, bool on)
        {
            return !CanPPec ? null : _commands.SetPPec(axis, on);
        }

        /// <summary>
        /// Turn on/off training pPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetPPecTrain(AxisId axis, bool on)
        {
            if (!CanPPec) { return; }
            _commands.SetPPecTrain(axis, on);
        }

        /// <summary>
        /// Enable or Disable Full Current Low speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetFullCurrent(AxisId axis, bool on)
        {
            if (!CanHalfTrack) { return; }
            _commands.SetLowSpeedCurrent(axis, on);
        }

        /// <summary>
        /// Loads default settings directly from the mount
        /// </summary>
        internal void LoadDefaultMountSettings()
        {
            _commands.LoadMountDefaults();
            _highSpeedRatio = _commands.GetHighSpeedRatio();
            _axisVersion = _commands.GetAxisVersions();
            _lowSpeedGotoMargin = _commands.GetLowSpeedGotoMargin();
            _factorRadRateToInt = _commands.GetFactorRadRateToInt();
            _breakSteps = _commands.GetBreakSteps();
            ParseCapabilities();
            SetStepsPerSecond();
        }

        #endregion

        #region SciptCommands

        /// <summary>
        /// Axis Position
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>Degrees</returns>
        internal double GetAxisPosition(AxisId axis)
        {
            return Principles.Units.Rad2Deg1(_commands.GetAxisPosition(axis));
        }

        /// <summary>
        /// j Gets axis position counter
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Cardinal encoder count</returns>
        internal long GetAxisPositionCounter(AxisId axis)
        {
            return _commands.GetAxisPositionCounter(axis);
        }

        /// <summary>
        /// Gets the position and the timestamp
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal Tuple<double?, DateTime> GetAxisPositionDate(AxisId axis)
        {
            switch (axis)
            {
                case AxisId.Axis1:
                    return new Tuple<double?, DateTime>(_commands.GetAxisPositionCounter(axis),
                        _commands.Last_j1RunTime);
                case AxisId.Axis2:
                    return new Tuple<double?, DateTime>(_commands.GetAxisPositionCounter(axis),
                        _commands.Last_j2RunTime);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        internal double GetRampDownRange(AxisId axis)
        {
            return _commands.GetRampDownRange(axis);
        }

        internal double GetEncoderCount(AxisId axis)
        {
            return _commands.GetEncoderCount(axis);
        }

        internal double GetLastGoToTarget(AxisId axis)
        {
            return _commands.GetLastGoToTarget(axis);
        }

        internal long GetLastSlewSpeed(AxisId axis)
        {
            return _commands.GetLastSlewSpeed(axis);
        }

        internal long? GetHomePosition(AxisId axis)
        {
            return !CanHomeSensors ? ((long?)null).GetValueOrDefault() : _commands.GetHomePosition(axis);
        }

        internal string GetMotorCardVersion(AxisId axis)
        {
            var result = _commands.GetMotorCardVersion(axis);

            try
            {
                if (result.Length < 7) { return string.Empty; }

                var a = Convert.ToInt32(result.Substring(5, 2), 16);
                if (!Enum.IsDefined(typeof(McModel), a)){ a = 999;}

                MountType = GetEnumDescription((McModel) a);
                MountNum = a;
                
                var first = int.Parse(result.Substring(1, 2), NumberStyles.HexNumber);
                var second = int.Parse(result.Substring(3, 2), NumberStyles.HexNumber);
                MountVersion = $"{first}.{second:D2}";
            }
            catch (Exception)
            {
                MountVersion = "99";
            }
            
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{result}|{MountType}|{MountVersion}|{MountNum}" };
            MonitorLog.LogToMonitor(monitorItem);

            return result.Substring(1, 6);
        }

        internal double GetPecPeriod(AxisId axis)
        {
            return _commands.GetPecPeriod(axis);
        }

        internal void InitializeAxes()
        {
            _commands.InitializeAxes();

        }

        /// <summary>
        /// G Set the different motion mode
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="func">'0' high speed GOTO slewing,'1' low speed slewing mode,'2' low speed GOTO mode,'3' High slewing mode</param>
        /// <param name="direction">0=forward/right, 1=backward/left</param>
        internal void SetMotionMode(AxisId axis, int func, int direction)
        {
            _commands.SetMotionMode(axis, func, direction, SouthernHemisphere);
        }

        internal void StartMotion(AxisId axis)
        {
            _commands.StartMotion(axis);
        }

        internal void SetGotoTargetIncrement(AxisId axis, long stepsCount)
        {
            _commands.SetGotoTargetIncrement(axis, stepsCount);
        }

        internal void SetStepSpeed(AxisId axis, long stepSpeed)
        {
            _commands.SetStepSpeed(axis, stepSpeed);
        }

        internal void SetBreakPointIncrement(AxisId axis, long stepsCount)
        {
            _commands.SetBreakPointIncrement(axis, stepsCount);
        }

        /// <summary>
        /// set ST4 guide rate
        /// </summary>
        /// <param name="rate"> 0..4 (1.0, 0.75, 0.50, 0.25, 0.125)</param>
        internal void SetSt4GuideRate(int rate)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{rate}" };
            MonitorLog.LogToMonitor(monitorItem);

            int cmd;
            switch (rate)
            {
                case 0:
                    cmd = 0;
                    break;
                case 1:
                    cmd = 1;
                    break;
                case 2:
                    cmd = 2;
                    break;
                case 3:
                    cmd = 3;
                    break;
                case 4:
                    cmd = 4;
                    break;
                default:
                    cmd = 2;
                    break;
            }
            _commands.SetSt4GuideRate(cmd);
        }

        /// <summary>
        /// O on/off trigger
        /// </summary>
        /// <param name="port"></param>
        /// <param name="on"></param>
        internal bool SetSnapPort(int port, bool on)
        {
            AxisId axis;
            switch (port)
            {
                case 1:
                    axis = AxisId.Axis1;
                    break;
                case 2:
                    axis = AxisId.Axis2;
                    break;
                default:
                    return false;
            }
            var a = _commands.SetSnapPort(axis, on).Trim();
            return a == "=";
        }

        internal void SetTargetPosition(AxisId axis, double position)
        {
            _commands.SetTargetPosition(axis, position);
        }

        /// <summary>
        /// Store the rates to use later 
        /// </summary>
        /// <param name="axis">which axis</param>
        /// <param name="rate">arc seconds in rad</param>
        private void SetRates(AxisId axis, double rate)
        {
            if (axis == AxisId.Axis1)
            {
                _trackingRates[0] = rate;
                _trackingSpeeds[0] = CalculateSpeed(AxisId.Axis1, rate);
            }
            else
            {
                _trackingRates[1] = rate;
                _trackingSpeeds[1] = CalculateSpeed(AxisId.Axis2, rate);
            }
        }

        internal void SetHomePositionIndex(AxisId axis)
        {
            _commands.SetHomePositionIndex(axis);
        }

        internal long[] GetStepsPerRevolution()
        {
            return _commands.GetStepsPerRevolution();
        }

        internal long[] GetStepTimeFreq()
        {
            return _commands.GetStepTimeFreq();
        }

        internal long[] GetHighSpeedRatio()
        {
            return _commands.GetHighSpeedRatio();
        }

        internal long[] GetLowSpeedGotoMargin()
        {
            return _commands.GetLowSpeedGotoMargin();
        }

        internal double[] GetFactorRadRateToInt()
        {
            return _commands.GetFactorRadRateToInt();
        }

        internal double[] GetFactorStepToRad()
        {
            return _commands.GetFactorStepToRad();
        }

        internal string GetCapabilities()
        {
            return _commands.GetCapabilities(AxisId.Axis1);
        }

        internal long GetAngleToStep(AxisId axis, double angleInRad)
        {
            return _commands.AngleToStep(axis, angleInRad);
        }

        internal double GetStepToAngle(AxisId axis, long steps)
        {
            return _commands.StepToAngle(axis, steps);
        }

        /// <summary>
        /// Tests GoTo mode if mount can move 1 step
        /// Sets properties CanOneStepDec and CanOneStepRa, Some mounts cannot move less than 20 for a GoTo
        /// This is for the 1 step issue for the EQ8 and EQ6 prior to firmware 02.0F 
        /// </summary>
        /// <returns></returns>
        internal bool[] GetOneStepIndicators()
        {
            var indicators = new bool[2];  //initialize 0,1 Booleans
            const int step = 1;  // 1 step to check
            var stopwatch = new Stopwatch(); // new timer to check for stops

            // check axis 1
            AxisStop(AxisId.Axis1); // normal stop
            var startCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position
            AxisMoveSteps(AxisId.Axis1, step); // Move 1 in positive direction
            var axesstatus1 = _commands.GetAxisStatus(AxisId.Axis1); // get status
            if (!axesstatus1.FullStop) // check if stopped
            {
                // Wait until the axis stops or counter runs out
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalMilliseconds <= 2000)
                {
                    axesstatus1 = _commands.GetAxisStatus(AxisId.Axis1);
                    // Break loop if the axis has stopped.
                    if (axesstatus1.FullStop) { break; }
                    Thread.Sleep(10); // no need to hurry
                    AxisStop(AxisId.Axis1); // force another stop
                }
                stopwatch.Reset(); //stop any interval measurement in progress and clear the elapsed time value
            }
            var endCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position again
            var count1 = Math.Abs(startCount1 - endCount1);
            if (count1 == 0)
            {
                CanOneStepRa = indicators[0] == false;
            }
            else
            {
                CanOneStepRa = indicators[0] = count1 <= 4; // check the difference with a variance of 4, populate return array and property
                // all done move back
                AxisMoveSteps(AxisId.Axis1, step); // Move 1 in positive direction
                endCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position again
                count1 = Math.Abs(startCount1 - endCount1);
                AxisMoveSteps(AxisId.Axis1, -count1);
            }

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Axis1|{count1}|{CanOneStepRa}" };
            MonitorLog.LogToMonitor(monitorItem);

            // check axis 2
            AxisStop(AxisId.Axis2); // all stop
            var startCount2 = GetAxisPositionCounter(AxisId.Axis2);  // where are we
            AxisMoveSteps(AxisId.Axis2, step); // Move in positive direction
            var axesstatus2 = _commands.GetAxisStatus(AxisId.Axis2);  // get status
            if (!axesstatus2.FullStop)
            {
                // Wait until the axis stops or counter runs out
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalMilliseconds <= 2000)
                {
                    axesstatus2 = _commands.GetAxisStatus(AxisId.Axis2);
                    // Break loop if the axis has stopped.
                    if (axesstatus2.FullStop) { break; }
                    Thread.Sleep(10);
                    AxisStop(AxisId.Axis2); // force another stop
                }
                stopwatch.Reset();
            }
            var endCount2 = GetAxisPositionCounter(AxisId.Axis2);  // where are we
            var count2 = Math.Abs(startCount2 - endCount2);
            if (count2 == 0)
            {
                CanOneStepDec = indicators[1] == false;
            }
            else
            {
                CanOneStepDec = indicators[1] = count2 <= 4; // check the difference with a variance of 4, populate return array and property
                // all done move back
                AxisMoveSteps(AxisId.Axis2, step); // Move 1 in positive direction
                endCount2 = GetAxisPositionCounter(AxisId.Axis2); // get position again
                count2 = Math.Abs(startCount2 - endCount2);
                AxisMoveSteps(AxisId.Axis2, -count2);
            }

            stopwatch.Stop();

            monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Axis2|{count2}|{CanOneStepDec}" };
            MonitorLog.LogToMonitor(monitorItem);

            return indicators;
        }

        #endregion

        #region Methods

        private void SetStepsPerSecond()
        {
            _stepsPerSecond[0] = Convert.ToDouble(GetStepsPerRevolution()[0] / 360.0 / 3600);
            _stepsPerSecond[1] = Convert.ToDouble(GetStepsPerRevolution()[1] / 360.0 / 3600);
        }

        /// <summary>
        /// Used as a multiplier to determine speed for the I command
        /// in general divide 1 by radians then multiply by this number 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="rateInRad"></param>
        /// <returns></returns>
        private long RadSpeedToInt(AxisId axis, double rateInRad)
        {
            _factorRadRateToInt = _commands.GetFactorRadRateToInt();
            var r = (rateInRad * _factorRadRateToInt[(int)axis]);
            return (long)Math.Round(r, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// parses status from the q command using =010000
        /// </summary>
        private void ParseCapabilities()
        {
            var result = GetCapabilities();
            if (result == null) { return; }
            if (result.Contains("!")) { return; }
            if (result.Contains("=")) { result = result.Replace("=", ""); }
            if (result.Length < 3) { return; }

            var code = result.ToCharArray();
            for (var i = 0; i < code.Length; i++)
            {
                var b = Enums.GetFlags((StatusBit)uint.Parse(code[i].ToString(), NumberStyles.HexNumber));

                switch (i)
                {
                    case 0:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    IsPPecOn = false;
                                    IsPPecInTrainingOn = false;
                                    break;
                                case StatusBit.A: // 1
                                    IsPPecInTrainingOn = true;
                                    break;
                                case StatusBit.B: // 2
                                    IsPPecOn = true;
                                    break;
                                case StatusBit.C: // 4
                                case StatusBit.D: // 8
                                    //not defined
                                    break;
                            }
                        }
                        break;
                    case 1:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    CanAzEq = false;
                                    CanHomeSensors = false;
                                    CanPPec = false;
                                    CanDualEncoders = false;
                                    break;
                                case StatusBit.A: // 1
                                    CanDualEncoders = true;
                                    break;
                                case StatusBit.B: // 2
                                    CanPPec = true;
                                    break;
                                case StatusBit.C: // 4
                                    CanHomeSensors = true;
                                    break;
                                case StatusBit.D: // 8
                                    CanAzEq = true;
                                    break;
                            }
                        }
                        break;
                    case 2:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    CanWifi = false;
                                    CanHalfTrack = false;
                                    CanPolarLed = false;
                                    CanAxisSlewsIndependent = false;
                                    break;
                                case StatusBit.A: // 1
                                    CanPolarLed = true;
                                    break;
                                case StatusBit.B: // 2
                                    CanAxisSlewsIndependent = true;
                                    break;
                                case StatusBit.C: // 4
                                    CanHalfTrack = true;
                                    break;
                                case StatusBit.D: // 8
                                    CanWifi = true;
                                    break;
                            }
                        }
                        break;
                    case 3:
                    case 4:
                    case 5:
                        //not defined all zeros
                        break;
                }
            }
        }

        /// <summary>
        /// Converts a rate in radians to mount speed
        /// </summary>
        /// <param name="axis">which axis</param>
        /// <param name="rate">rate in radians</param>
        /// <returns></returns>
        private long CalculateSpeed(AxisId axis, double rate)
        {
            var speed = Math.Abs(rate);

            // For using function RadSpeedToInt(), change to unit Seconds/Rad.
            // ie. sending sidereal as internal speed will calculate how many rads per second
            speed = 1 / speed;

            //Conversion of radians to int which sets the speed in steps
            var speedInt = RadSpeedToInt(axis, speed);

            //Set maximum speed, The lower the number the faster speed
            if (_axisVersion[0] == 0x010600 || _axisVersion[0] == 0x010601)
            {
                // For special MC version.
                speedInt -= 3;
            }

            if (speedInt < 3) { 
                speedInt = 3; 
            }

            return speedInt;
        }

        private static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        #endregion
    }

    [Flags]
    public enum StatusBit
    {
        None = 0,
        A = 1,
        B = 1 << 1,
        C = 1 << 2,
        D = 1 << 3
    }
}
