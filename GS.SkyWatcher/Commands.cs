/* Copyright(C) 2019-2025 Rob  Morgan (robert.morgan.e@gmail.com)

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
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GS.SkyWatcher
{
    /// <summary>
    /// SkyWatcher Commands class
    /// Based on information from Andrew Johansen and the original skyWatcher-pacific project 5/31/2011: release C# basic API 1.0
    /// Constructor takes a connected serial object. 
    /// </summary>
    public class Commands
    {
        #region Fields

        private const char EndChar = (char)13;                         // Tailing character of command and response.
        private readonly AxisStatus[] _axesStatus = new AxisStatus[2];  // Status and state information for each axis
        private readonly double[] _positions = { 0, 0 };
        private readonly double[] _factorStepToRad = { 0, 0 };          // radians per step based on gear ratio
        private readonly double[] _factorRadToStep = { 0, 0 };          // steps per radian based on gear ratio
        private readonly long[] _breakSteps = new long[2];
        private readonly long[] _stepTimerFreq = new long[2];
        //private readonly double[] _peSteps = new double[2];
        private readonly long[] _axisGearRatios = new long[2];
        private readonly double[] _factorRadRateToInt = { 0, 0 };
        private readonly long[] _lowSpeedGotoMargin = new long[2];
        private readonly long[] _axisVersion = new long[2];             // Axes versions
        private readonly string[] _axisStringVersion = new string[2];   // Readable string version format
        private readonly int[] _axisModel = new int[2];                 // Mount Model Number
        private readonly long[] _highSpeedRatio = new long[2];          // HiSpeed multiplier  EQ6Pro, AZEeQ5, EQ8 = 16   AZeQ6 = 32
        private const int ThreadLockTimeout = 50;                      // milliseconds
        private readonly object _syncObject = new object();
        private int _conErrCnt;                                         // Number of continuous errors
        private const int ConErrMax = 50;                               // Max number of allowed continuous errors
        private readonly int[] _stepsPerRev = { 0, 0 };                 // From mount :a or :X0002
        private readonly int[] _resolutionFactor = { 1, 1 };            // Step division factor from :a and :X0002

        #endregion

        #region Properties

        public DateTime LastI1RunTime { get; private set; }
        public DateTime LastJ1RunTime { get; private set; }
        public DateTime LastJ2ARunTime { get; private set; }
        public DateTime LastJ2RunTime { get; private set; }
        private bool MountConnected { get; set; }

        /// <summary>
        /// Quick check to see if serial is connected and mount is receiving and sending data
        /// </summary> 
        internal bool IsConnected => SkyQueue.Serial.IsOpen && MountConnected;

        /// <summary>
        /// Indicate whether the motor controller supports advanced command set (Firmware version 3.22.xx or above)
        /// </summary> 
        public bool SupportAdvancedCommandSet { get; private set; }

        /// <summary>
        /// Indicate whether the advanced command set is allowed to be used
        /// </summary> 
        public bool AllowAdvancedCommandSet { get; set; }

        #endregion

        #region Methods

        public Commands()
        {
            if (SkyQueue.Serial.IsOpen)
            {
                //Serial.DataReceived += DataReceived;
                //Serial.ErrorReceived += ErrorReceived;
                //Serial.PinChanged += PinReceived;
                MountConnected = true;
            }
        }

        /// <summary>
        /// Load settings from the mount
        /// </summary>
        internal void LoadMountDefaults()
        {
            MountConnected = true;
            GetAxisVersion(AxisId.Axis1);
            GetAxisVersion(AxisId.Axis2);
            //calculate resolution needed
            GetResolutionFactors();
            // Inquire Gear Rate
            GetStepsPerRevolution(AxisId.Axis1);
            GetStepsPerRevolution(AxisId.Axis2);
            // Inquire motor high speed ratio
            GetTimerInterruptFreq(AxisId.Axis1);
            GetTimerInterruptFreq(AxisId.Axis2);
            // Inquire motor high speed ratio
            GetHighSpeedRatio(AxisId.Axis1);
            GetHighSpeedRatio(AxisId.Axis2);
            //
            InitializeAxes();
            // Inquire Axis Position
            GetStartupPosition(AxisId.Axis1);
            GetStartupPosition(AxisId.Axis2);
            // encoders
            LogEncoderCount(AxisId.Axis1);
            LogEncoderCount(AxisId.Axis2);
            // These two LowSpeedGotoMargin are calculate from slewing for 5 seconds in 128x sidereal rate
            _lowSpeedGotoMargin[(int)AxisId.Axis1] = (long)(640 * Constant.SiderealRate * _factorRadToStep[(int)AxisId.Axis1]);
            _lowSpeedGotoMargin[(int)AxisId.Axis2] = (long)(640 * Constant.SiderealRate * _factorRadToStep[(int)AxisId.Axis2]);
            // Default break steps
            _breakSteps[(int)AxisId.Axis1] = 3500;
            _breakSteps[(int)AxisId.Axis2] = 3500;
        }

        /// <summary>
        /// Common readable format for the axes versions
        /// </summary>
        /// <returns></returns>
        internal string[] GetAxisStringVersions()
        {
            return (string[])_axisStringVersion.Clone();
        }

        /// <summary>
        /// Long format of axes versions
        /// </summary>
        /// <returns></returns>
        internal long[] GetAxisVersions()
        {
            return (long[])_axisVersion.Clone();
        }

        /// <summary>
        /// int of model number
        /// </summary>
        /// <returns></returns>
        internal int[] GetModel()
        {
            return (int[])_axisModel.Clone();
        }

        /// <summary>
        /// Gear ratios
        /// </summary>
        /// <returns></returns>
        internal long[] GetStepsPerRevolution()
        {
            return (long[])_axisGearRatios.Clone();
        }

        /// <summary>
        /// Time Frequency per step
        /// </summary>
        /// <returns></returns>
        internal long[] GetStepTimeFreq()
        {
            return (long[])_stepTimerFreq.Clone();
        }

        /// <summary>
        /// High Speed ratio
        /// </summary>
        /// <returns></returns>
        internal long[] GetHighSpeedRatio()
        {
            return (long[])_highSpeedRatio.Clone();
        }

        /// <summary>
        /// margin to move from high speed to low speed
        /// </summary>
        /// <returns></returns>
        internal long[] GetLowSpeedGotoMargin()
        {
            return (long[])_lowSpeedGotoMargin.Clone();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal double[] GetFactorRadRateToInt()
        {
            return (double[])_factorRadRateToInt.Clone();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal double[] GetFactorStepToRad()
        {
            return (double[])_factorStepToRad.Clone();
        }

        /// <summary>
        /// Break Steps
        /// </summary>
        /// <returns></returns>
        internal long[] GetBreakSteps()
        {
            return (long[])_breakSteps.Clone();
        }

        /// <summary>
        /// Updates an axis status for slew movements
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="forward"></param>
        /// <param name="highSpeed"></param>
        internal void SetSlewing(int axis, bool forward, bool highSpeed)
        {
            _axesStatus[axis].SetSlewing(forward, highSpeed);
        }

        /// <summary>
        /// Updates an axis status for goto movements
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="forward"></param>
        /// <param name="highSpeed"></param>
        internal void SetSlewingTo(int axis, bool forward, bool highSpeed)
        {
            _axesStatus[axis].SetSlewingTo(forward, highSpeed);
        }

        /// <summary>
        /// return the last known status without polling the mount
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal AxisStatus GetCacheAxisStatus(AxisId axis)
        {
            return _axesStatus[(int)axis];
        }

        #endregion

        #region Commands

        /// <summary>
        /// calculates the correct resolution needed to factor the advanced set into the correct steps and speeds
        /// </summary>
        private void GetResolutionFactors()
        {
            string response, msg;
            MonitorEntry monitorItem;
            var newMsg = $"Adv:N/A;N/A";
            long[] revOld = { 0, 0 };
            long[] revNew = { 0, 0 };

            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                // New
                response = CmdToMount(AxisId.Axis1, 'X', "0002");
                revNew[0] = String32ToInt(response, true, 1);
                msg = $"Axis1|:X0002|{response}";
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                MonitorLog.LogToMonitor(monitorItem);

                response = CmdToMount(AxisId.Axis2, 'X', "0002");
                revNew[1] = String32ToInt(response, true, 1);
                msg = $"Axis2|:X0002|{response}";
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                MonitorLog.LogToMonitor(monitorItem);

                newMsg = $"Adv:{revNew[0]};{revNew[1]}";
            }

            // Old
            response = CmdToMount(AxisId.Axis1, 'a', null);
            revOld[0] = StringToLong(response);
            if ((_axisVersion[0] & 0x0000FF) == 0x80) { revOld[0] = 0x162B97; } // for 80GT mount
            if ((_axisGearRatios[0] & 0x0000FF) == 0x82) { revOld[0] = 0x205318; } // for 114GT mount
            msg = $"Axis1|:a|{response}";
            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
            MonitorLog.LogToMonitor(monitorItem);

            response = CmdToMount(AxisId.Axis2, 'a', null);
            revOld[1] = StringToLong(response);
            if ((_axisVersion[1] & 0x0000FF) == 0x80) { revOld[1] = 0x162B97; } // for 80GT mount
            if ((_axisGearRatios[1] & 0x0000FF) == 0x82) { revOld[1] = 0x205318; } // for 114GT mount
            msg = $"Axis2|:a|{response}";
            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
            MonitorLog.LogToMonitor(monitorItem);

            // default _resolutionFactor is already set to 1
            if (revOld[0] > 0) { _resolutionFactor[0] = (int)(revNew[0] / revOld[0]); }
            if (_resolutionFactor[0] == 0) { _resolutionFactor[0] = 1; } //make sure its not 0

            if (revOld[1] > 0) { _resolutionFactor[1] = (int)(revNew[1] / revOld[1]); }
            if (_resolutionFactor[1] == 0) { _resolutionFactor[1] = 1; } //make sure it's not 0

            // log
            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{newMsg}|Old:{revOld[0]};{revOld[1]}|Factor:{_resolutionFactor[0]};{_resolutionFactor[1]}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// a or X0002 Steps Per Revolution 
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        private void GetStepsPerRevolution(AxisId axis)
        {
            string response, msg;
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                response = CmdToMount(axis, 'X', "0002");    //0x02（’02’）: Resolution of the axis (Counts per revolution)
                var gearRatio = String32ToInt(response, true, _resolutionFactor[(int)axis]);
                msg = "X0002";
                switch (axis)
                {
                    case AxisId.Axis1:
                        _stepsPerRev[0] = gearRatio;
                        if (SkyQueue.CustomMount360Steps[0] > 0) { gearRatio = SkyQueue.CustomMount360Steps[0]; } //Setup for custom :a
                        _axisGearRatios[0] = gearRatio;
                        break;
                    case AxisId.Axis2:
                        _stepsPerRev[1] = gearRatio;
                        if (SkyQueue.CustomMount360Steps[1] > 0) { gearRatio = SkyQueue.CustomMount360Steps[1]; } //Setup for custom :a
                        _axisGearRatios[1] = gearRatio;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                }

                _factorRadToStep[(int)axis] = gearRatio / (2 * Math.PI);
                _factorStepToRad[(int)axis] = 2 * Math.PI / gearRatio;
            }
            else
            {
                msg = "a";
                response = CmdToMount(axis, 'a', null);
                var gearRatio = StringToLong(response);
                // There is a issue in the earlier version firmware(Before 2.00) of motor controller MC001.
                // Overwrite the GearRatio reported by the MC for 80GT mount and 114GT mount.
                if (axis == AxisId.Axis1)
                {
                    if ((_axisVersion[0] & 0x0000FF) == 0x80)
                    {
                        gearRatio = 0x162B97; // for 80GT mount
                    }
                    if ((_axisGearRatios[0] & 0x0000FF) == 0x82)
                    {
                        gearRatio = 0x205318; // for 114GT mount
                    }
                    _stepsPerRev[0] = (int)gearRatio;
                    if (SkyQueue.CustomMount360Steps[0] > 0) { gearRatio = SkyQueue.CustomMount360Steps[0]; } //Setup for custom :a
                    _axisGearRatios[0] = gearRatio;
                }
                else
                {
                    if ((_axisVersion[1] & 0x0000FF) == 0x80)
                    {
                        gearRatio = 0x162B97; // for 80GT mount
                    }
                    if ((_axisVersion[1] & 0x0000FF) == 0x82)
                    {
                        gearRatio = 0x205318; // for 114GT mount
                    }
                    _stepsPerRev[1] = (int)gearRatio;
                    if (SkyQueue.CustomMount360Steps[1] > 0) { gearRatio = SkyQueue.CustomMount360Steps[1]; } //Setup for custom :a
                    _axisGearRatios[1] = gearRatio;
                }
                _factorRadToStep[(int)axis] = gearRatio / (2 * Math.PI);
                _factorStepToRad[(int)axis] = 2 * Math.PI / gearRatio;
            }
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":{msg}|{axis}|{response}|{ _stepsPerRev[(int)axis]}|Custom:{ SkyQueue.CustomMount360Steps[(int)axis]}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// b Inquire Timer Interrupt Freq ":b1".
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        private void GetTimerInterruptFreq(AxisId axis)
        {
            var response = CmdToMount(axis, 'b', null);

            var timeFreq = StringToLong(response);
            _stepTimerFreq[(int)axis] = timeFreq;

            _factorRadRateToInt[(int)axis] = _stepTimerFreq[(int)axis] / _factorRadToStep[(int)axis];

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":b|{axis}|{response}|{timeFreq}|{_factorRadRateToInt[(int)axis]}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// c micro steps from target where the ramp down process begins
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal double GetRampDownRange(AxisId axis)
        {
            var response = CmdToMount(axis, 'c', null);
            var responseString = StringToLong(response);
            return responseString;
        }

        /// <summary>
        /// d Get Current Encoder count
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal double GetEncoderCount(AxisId axis)
        {
            var response = CmdToMount(axis, 'd', null);
            var responseString = StringToLong(response);
            return responseString;
        }

        /// <summary>
        /// d Get Current Encoder count and log it to monitor
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        private void LogEncoderCount(AxisId axis)
        {
            var response = CmdToMount(axis, 'd', null);
            var responseString = StringToLong(response);
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":d|{axis}|{response}|{responseString}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        private static readonly Version AzgTiAdvancedSetSupportedVersion = new Version(3, 40);

        /// <summary>
        /// e or X0005 Gets version of the axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void GetAxisVersion(AxisId axis)
        {
            string response, mountVersion, msg;
            int intVersion, first, second, mountModel;

            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                response = CmdToMount(axis, 'X', "0005");    //0x05（’05’）: MountID and motor controller firmware Version
                var d = response.Substring(3, 6);
                var b = String32ToInt(d, false, 1);
                intVersion = b;
                //example =23032700
                // 2 digit - mount id
                // 2 digits major version
                // 2 digits minor version
                // + 00
                first = int.Parse(response.Substring(3, 2), NumberStyles.HexNumber);
                second = int.Parse(response.Substring(5, 2), NumberStyles.HexNumber);
                mountModel = int.Parse(response.Substring(1, 2), NumberStyles.HexNumber);
                mountVersion = $"{first}.{second:D2}";
                msg = "X0005";
            }
            else
            {
                response = CmdToMount(axis, 'e', null);
                var tmpInt = Convert.ToInt32(StringToLong(response));
                intVersion = ((tmpInt & 0xFF) << 16) | ((tmpInt & 0xFF00)) | ((tmpInt & 0xFF0000) >> 16);
                //example =032723
                first = int.Parse(response.Substring(1, 2), NumberStyles.HexNumber);
                second = int.Parse(response.Substring(3, 2), NumberStyles.HexNumber);
                mountModel = int.Parse(response.Substring(5, 2), NumberStyles.HexNumber);
                mountVersion = $"{first}.{second:D2}";
                msg = "e";
            }

            if (axis == AxisId.Axis1)
            {
                _axisStringVersion[0] = mountVersion;
                _axisVersion[0] = intVersion;
                _axisModel[0] = mountModel;
            }
            else
            {
                _axisStringVersion[1] = mountVersion;
                _axisVersion[1] = intVersion;
                _axisModel[1] = mountModel;
            }

            var version = new Version(first, second);
            // Exclude AZ GTI in EQ mode prior version 3.40
            if (mountModel == 165 && version < AzgTiAdvancedSetSupportedVersion)
            {
                SupportAdvancedCommandSet = false;
            }
            // SW recommends no support for single axis trackers 0x07, 0x08, 0x0A, and 0x0F
            // "Star Adventurer Mount" advanced firmware 3.130.07 exclude
            else if (intVersion == 0x038207)
            {
                SupportAdvancedCommandSet = false;
            }
            else if (intVersion > 0x032200)  //205312
            {
                SupportAdvancedCommandSet = true;
            }
            else
            {
                SupportAdvancedCommandSet = false;
            }

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":{msg}|{axis}|{response}|{_axisStringVersion[(int)axis]}|Advanced:{SupportAdvancedCommandSet}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// f Get the target axis's status as a struct including low voltage event state.
        /// X 0001 does not return low voltage event state.
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns></returns>
        internal AxisStatus GetAxisStatus(AxisId axis)
        {
            string response;
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                response = CmdToMount(axis, 'X', "0001");
                var val = String32ToInt(response, true, 1);

                if (IsBitSet(val, 0))
                {
                    _axesStatus[(int)axis].FullStop = false;
                    _axesStatus[(int)axis].Slewing = true;
                    _axesStatus[(int)axis].SlewingTo = true;
                    _axesStatus[(int)axis].SlewingForward = true;
                }
                else
                {
                    _axesStatus[(int)axis].FullStop = true;
                    _axesStatus[(int)axis].Slewing = false;
                    _axesStatus[(int)axis].SlewingTo = false;
                    _axesStatus[(int)axis].StepSpeed = "*";
                }

                _axesStatus[(int)axis].Response = response;
                _axesStatus[(int)axis].TrajectoryMode = IsBitSet(val, 2);
                _axesStatus[(int)axis].HighSpeed = false;
                _axesStatus[(int)axis].Initialized = IsBitSet(val, 6);
                _axesStatus[(int)axis].LowVoltageEventState = IsBitSet(val, 9);
                return _axesStatus[(int)axis];
            }

            response = CmdToMount(axis, 'f', null);
            
            //check if at full stop = 1
            if ((response[2] & 0x01) == 0)
            {
                _axesStatus[(int)axis].FullStop = true;
                _axesStatus[(int)axis].Slewing = false;
                _axesStatus[(int)axis].SlewingTo = false;
                _axesStatus[(int)axis].StepSpeed = "*";
            }
            else
            {
                // Axis is running
                _axesStatus[(int)axis].FullStop = false;
                _axesStatus[(int)axis].Slewing = (response[1] & 0x01) != 0;
                _axesStatus[(int)axis].SlewingTo = (response[1] & 0x01) == 0;
            }

            _axesStatus[(int)axis].Response = response;
            _axesStatus[(int)axis].SlewingForward = (response[1] & 0x02) == 0;
            _axesStatus[(int)axis].HighSpeed = (response[1] & 0x04) != 0;
            _axesStatus[(int)axis].Initialized = (response[3] & 1) == 1;
            _axesStatus[(int)axis].TrajectoryMode = false;
            _axesStatus[(int)axis].LowVoltageEventState = (response[3] & 0x04) == 4; // Set low voltage event status
            return _axesStatus[(int)axis];
        }

        /// <summary>
        /// Retrieves the current voltage level of the controller using :qx030000.
        /// </summary>
        /// <remarks>This method is intended to obtain the voltage level of the controller for diagnostic
        /// or monitoring purposes. The voltage value may vary depending on the controller's operational
        /// state.</remarks>
        internal double GetControllerVoltage(AxisId axis)
        {
            const string szCmd = "030000";
            var response = CmdToMount(axis, 'q', szCmd);
            if (string.IsNullOrEmpty(response)) return double.NaN;
            var voltage = StringToLong(response) / 100.0; // Convert to volts
            return voltage;
        }
        /// <summary>
        /// g Inquire High Speed Ratio, EQ6Pro, AZeQ5, EQ8 = 16   AZeQ6 = 32
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        private void GetHighSpeedRatio(AxisId axis)
        {
            var response = CmdToMount(axis, 'g', null);
            var highSpeedRatio = StringToLong(response);
            _highSpeedRatio[(int)axis] = highSpeedRatio;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":g|{axis}|{response}|{highSpeedRatio}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// h Get Current "goto" target
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal double GetLastGoToTarget(AxisId axis)
        {
            var response = CmdToMount(axis, 'h', null);
            var responseString = StringToLong(response);
            return responseString;
        }

        /// <summary>
        /// i or X0007 Get last "slew" speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="oldI">force old i: command</param>
        internal long GetLastSlewSpeed(AxisId axis, bool oldI = true)
        {
            string response;
            long iSpeed;
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet && !oldI)
            {
                response = CmdToMount(axis, 'X', "0007");    // 0x07（’07’）: Current slewing speed in ticks /1024 seconds
                iSpeed = String32ToInt(response, true, _resolutionFactor[(int)axis]);
            }
            else
            {
                response = CmdToMount(axis, 'i', null);
                iSpeed = StringToLong(response);
            }
            return iSpeed;
        }

        /// <summary>
        /// Gets :j data or returns double.NaN for the given axis
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        internal double Get_j(AxisId axis, bool raw = false)
        {
            var response = CmdToMount(axis, 'j', null, true);
            if (string.IsNullOrEmpty(response)) return double.NaN;
            var iPosition = StringToLong(response);
            if (!raw) { iPosition -= 0x00800000; }
            return iPosition;
        }

        /// <summary>
        /// j or X0003 Gets radians position of an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Radians of the axis</returns> 
        internal double GetAxisPosition(AxisId axis)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                var response = CmdToMount(axis, 'X', "0003");    // 0x03（’03’）: Position reading of the axis
                var iPosition = String32ToInt(response, true, _resolutionFactor[(int)axis]);
                _positions[(int)axis] = StepToAngle(axis, iPosition);
                return _positions[(int)axis];
            }
            else
            {
                var response = CmdToMount(axis, 'j', null);
                var iPosition = StringToLong(response);
                iPosition -= 0x00800000;
                _positions[(int)axis] = StepToAngle(axis, iPosition);
                return _positions[(int)axis];
            }
        }

        /// <summary>
        /// j or X0003 Gets startup position of an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Radians of the axis</returns>
        private void GetStartupPosition(AxisId axis)
        {
            try
            {
                long iPosition;
                string response, msg;
                if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
                {
                    response = CmdToMount(axis, 'X', "0003", true);
                    iPosition = String32ToInt(response, true, _resolutionFactor[(int)axis]);
                    // _positions[(int)axis] = StepToAngle(axis, iPosition);
                    msg = "X";
                }
                else
                {
                    response = CmdToMount(axis, 'j', null, true);
                    if (string.IsNullOrEmpty(response))
                    {
                        throw new Exception("Empty response");
                    }
                    iPosition = StringToLong(response);
                    iPosition -= 0x00800000;
                    msg = "j";
                }

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":{msg}|{axis}|{response}|{iPosition}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// j or X0003 Gets radians position of an axis or returns NaN
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Radians of the axis or NaN if no response is received</returns>
        internal double GetAxisPositionNaN(AxisId axis)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                var response = CmdToMount(axis, 'X', "0003", true);    // 0x03（’03’）: Position reading of the axis
                if (string.IsNullOrEmpty(response)) return double.NaN;
                try
                {
                    var iPosition = String32ToInt(response, true, _resolutionFactor[(int)axis]);
                    _positions[(int)axis] = StepToAngle(axis, iPosition);
                    return _positions[(int)axis];
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                    MonitorLog.LogToMonitor(monitorItem);
                    return double.NaN;
                }
            }
            else
            {
                var response = CmdToMount(axis, 'j', null, true);
                if (string.IsNullOrEmpty(response)) return double.NaN;
                try
                {
                    var iPosition = StringToLong(response);
                    iPosition -= 0x00800000;
                    _positions[(int)axis] = StepToAngle(axis, iPosition);
                    return _positions[(int)axis];
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                    MonitorLog.LogToMonitor(monitorItem);
                    return double.NaN;
                }
            }
        }

        /// <summary>
        /// j or X0003 Gets axis position steps or returns NaN
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Cardinal encoder count</returns>
        internal double GetAxisStepsNaN(AxisId axis)
        {
            try
            {
                if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
                {
                    var response = CmdToMount(axis, 'X', "0003", true);    // 0x03（’03’）: Position reading of the axis
                    if (string.IsNullOrEmpty(response)) return double.NaN;
                    var iPosition = String32ToInt(response, true, _resolutionFactor[(int)axis]);
                    return iPosition;
                }
                else
                {
                    var response = CmdToMount(axis, 'j', null, true);
                    if (string.IsNullOrEmpty(response)) return double.NaN;
                    var iPosition = StringToLong(response);
                    iPosition -= 0x00800000;
                    return iPosition;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                return double.NaN;
            }
        }

        /// <summary>
        /// j or X0003 Gets axis position steps
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// /// <param name="raw">false to subtract 0x00800000</param>
        /// <returns>Cardinal encoder count</returns>
        internal long GetAxisPositionCounter(AxisId axis, bool raw = false)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                var response = CmdToMount(axis, 'X', "0003");    // 0x03（’03’）: Position reading of the axis
                var res = raw ? 1 : _resolutionFactor[(int)axis];
                var iPosition = String32ToInt(response, true, res);
                return iPosition;
            }
            else
            {
                string response = null;
                for (var i = 3; i > 0; i--)
                {
                    response = CmdToMount(axis, 'j', null, true);
                    if (response != null)
                    {
                        break;
                    }
                }
                var iPosition = StringToLong(response);
                if(!raw){iPosition -= 0x00800000;}
                return iPosition;
            }
        }

        /// <summary>
        /// qx00 or X000B Home position
        /// Send :qx000000[0D]
        ///     =000000[0D] or =80000000   if axis is CW  from home ( ie -ve ) just after home sensor trip has been reset
        ///     =FFFFFF[0D] or =7FFFFFFF   CCW from home(ie +ve ) just after home sensor trip has been reset )
        ///     =llhhLL[0D] or =1234ABCD   if sensor has tripped since reset(use :W to clear data first )
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>  
        internal long GetHomePosition(AxisId axis)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                var response = CmdToMount(axis, 'X', "000B");    // Read 32-bit axis home index position
                var position = String32ToInt(response, true, 1);
                switch (position)
                {
                    case -2147483648:
                        return 100000000000;
                    case 2147483647:
                        return 200000000000;
                    default:
                        return Convert.ToInt32(position / _resolutionFactor[(int)axis]);
                }
            }
            else
            {
                var szCmd = LongToHex(0);
                var response = CmdToMount(axis, 'q', szCmd);
                var iPosition = StringToLong(response);
                switch (iPosition)
                {
                    case 0:
                        return 300000000000;
                    case 16777215:
                        return 400000000000;
                    default:
                        iPosition -= 0x00800000;
                        return iPosition;
                }
            }
        }

        /// <summary>
        /// qx01 Capabilities
        ///    :qx010000[0D]=ABCDEF[0D]  ie the bit mapped nybbles for current status
        /// A    8  not defined
        ///      4  not defined
        ///      2  pPEC ON
        ///      1  pPEC training in progress,
        /// B    8  supports AZ/EQ
        ///      4  has Home Sensors
        ///      2  supports pPEC
        ///      1  supports dual encoders
        /// C    8  has WIFI
        ///      4  supports half current tracking          // ref :Wx06....
        ///      2  axes slews must start independently     // ie cant use :J3
        ///      1  has polar LED
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>  
        internal string GetCapabilities(AxisId axis)
        {
            var szCmd = LongToHex(1);
            var response = CmdToMount(axis, 'q', szCmd);

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":q|{axis}|{response}|{szCmd}" };
            MonitorLog.LogToMonitor(monitorItem);

            return response;
        }

        /// <summary>
        /// s or X000E Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal double GetPecPeriod(AxisId axis)
        {
            var response = CmdToMount(axis, 's', null);
            var pecPeriod = (double)StringToLong(response);
            var ax = (int)axis;
            if (SkyQueue.CustomRaWormSteps[ax] > 0) { pecPeriod = SkyQueue.CustomRaWormSteps[ax]; } // Setup custom mount worm steps
            //_peSteps[ax] = pecPeriod;
            var ret = pecPeriod;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":s|{axis}|{response}|{pecPeriod}|Custom:{SkyQueue.CustomRaWormSteps[ax]}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (!SupportAdvancedCommandSet || !AllowAdvancedCommandSet) return ret;
            response = CmdToMount(axis, 'X', "000E");    // Read 32-bit Resolution of the worm(Counts per revolution)
            pecPeriod = String32ToInt(response, true, _resolutionFactor[(int)axis]);
            ax = (int)axis;
            if (SkyQueue.CustomRaWormSteps[ax] > 0) { pecPeriod = SkyQueue.CustomRaWormSteps[ax]; } // Setup custom mount worm steps
            //_peSteps[ax] = pecPeriod;
            ret = pecPeriod;

            monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":X000E|{axis}|{response}|{pecPeriod}|Custom:{SkyQueue.CustomRaWormSteps[ax]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return ret;
        }

        /// <summary>
        /// Capture axes positions and timestamp. 8 hex for ra, 8 hex for dec, 16 hex in microseconds
        /// </summary>
        /// <param name="raw">process hex or not</param>
        /// <param name="pp"></param>
        /// <returns></returns>
        internal string GetPositionsAndTime(bool raw, string pp)
        {
            var response = string.Empty;
            if (string.IsNullOrEmpty(pp)) { pp = "00000000"; }

            if (!SupportAdvancedCommandSet || !AllowAdvancedCommandSet) return response;

            var szCmd = "0F" + pp.Substring(0, 8);
            response = CmdToMount(AxisId.Axis1, 'X', szCmd);

            if (raw) { return response; }
            if (response.Length != 33) { return response; }
            try
            {
                var a = String32ToInt(response.Substring(1, 8), false, 1);
                var b = String32ToInt(response.Substring(9, 8), false, 1);
                var c = response.Substring(17, 16);
                response = a + ";" + b + ";" + c;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return response;
        }

        /// <summary>
        /// D Sidereal rate in step counts
        /// </summary>
        /// <returns></returns>
        internal long GetSiderealRate(AxisId axis)
        {
            var response = CmdToMount(axis, 'D', null);
            return StringToLong(response);
        }

        /// <summary>
        /// E or X01 Set the target axis position to the specify value
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="radians">radian value</param>
        internal void SetAxisPosition(AxisId axis, double radians)
        {
            var newStepIndex = AngleToStep(axis, radians);

            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                newStepIndex *= _resolutionFactor[(int)axis];
                var szCmd = "01" + newStepIndex.ToString("X8");
                CmdToMount(axis, 'X', szCmd);
            }
            else
            {
                newStepIndex += 0x800000;

                var szCmd = LongToHex(newStepIndex);
                CmdToMount(axis, 'E', szCmd);
            }

            _positions[(int)axis] = radians;
        }

        /// <summary>
        /// E or X01 Set the target axis position to the specify value
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="steps">radian value</param>
        internal void SetAxisPositionCounter(AxisId axis, int steps)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                var szCmd = "01" + steps.ToString("X8");
                CmdToMount(axis, 'X', szCmd);
            }
            else
            {
                //steps += 0x800000;
                var szCmd = LongToHex(steps);
                CmdToMount(axis, 'E', szCmd);
            }
        }

        /// <summary>
        /// F or X0505 Initial the target axis
        /// </summary>
        internal void InitializeAxes()
        {
            GetAxisStatus(AxisId.Axis1);
            GetAxisStatus(AxisId.Axis2);

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":f|Axis1|{_axesStatus[0].Response}|Initialized|{_axesStatus[0].Initialized}" };
            MonitorLog.LogToMonitor(monitorItem);

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":f|Axis2|{_axesStatus[1].Response}|Initialized|{_axesStatus[1].Initialized}" };
            MonitorLog.LogToMonitor(monitorItem);

            string msg;

            if (_axesStatus[0].Initialized == false)
            {
                if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
                {
                    CmdToMount(AxisId.Axis1, 'X', "0505");
                    msg = "X10505|Axis1";
                }
                else
                {
                    CmdToMount(AxisId.Axis1, 'F', null);
                    msg = ":F|Axis1";
                }
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Initialized|" + msg };
                MonitorLog.LogToMonitor(monitorItem);
            }

            if (_axesStatus[1].Initialized == false)
            {
                if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
                {
                    CmdToMount(AxisId.Axis2, 'X', "0505");
                    msg = "X20505|Axis2";
                }
                else
                {
                    CmdToMount(AxisId.Axis2, 'F', null);
                    msg = ":F|Axis2";
                }
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Initialized|" + msg };
                MonitorLog.LogToMonitor(monitorItem);
            }

        }

        /// <summary>
        /// G Set the different motion mode
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="func">'0' high speed GOTO slewing,'1' low speed slewing mode,'2' low speed GOTO mode,'3' High slewing mode</param>
        /// <param name="direction">0=forward/right, 1=backward/left</param>
        /// <param name="southernHemisphere">is mount in the south</param>
        internal void SetMotionMode(AxisId axis, int func, int direction, bool southernHemisphere)
        {
            switch (direction)
            {
                case 0:
                case 2:
                    direction = southernHemisphere ? 2 : 0;
                    break;
                case 1:
                case 3:
                    direction = southernHemisphere ? 3 : 1;
                    break;
                default:
                    return;
            }
            var szCmd = $"{func}{direction}";
            CmdToMount(axis, 'G', szCmd);
        }

        /// <summary>
        /// H Set the goto target increment
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="stepsCount"></param>
        internal void SetGotoTargetIncrement(AxisId axis, long stepsCount)
        {
            var cmd = LongToHex(stepsCount);
            CmdToMount(axis, 'H', cmd);
        }

        /// <summary>
        /// I Set slewing rate, seems to relate to amount of skipped step counts.  
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="stepSpeed">StepSpeed = 1 motor step movement, higher counts means slower movements</param>
        internal void SetStepSpeed(AxisId axis, long stepSpeed)
        {
            var szCmd = LongToHex(stepSpeed);
            CmdToMount(axis, 'I', szCmd);
        }

        /// <summary>
        /// J Start motion based on previous settings
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void StartMotion(AxisId axis)
        {
            CmdToMount(axis, 'J', null);
        }

        /// <summary>
        /// K or X0504 Stop the target axis normally
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStop(AxisId axis)
        {
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                CmdToMount(axis, 'X', "0504");
            }
            else
            {
                CmdToMount(axis, 'K', null);
            }
            _axesStatus[(int)axis].SetFullStop();
        }

        /// <summary>
        /// L Stop the target axis instantly
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStopInstant(AxisId axis)
        {
            CmdToMount(axis, 'L', null);
            _axesStatus[(int)axis].SetFullStop();
        }

        /// <summary>
        /// M Set the break point increment
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="stepsCount"></param>
        internal void SetBreakPointIncrement(AxisId axis, long stepsCount)
        {
            var szCmd = LongToHex(stepsCount);
            CmdToMount(axis, 'M', szCmd);
        }

        /// <summary>
        /// O or X05 on/off trigger
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="on"></param>
        internal string SetSnapPort(AxisId axis, bool on)
        {
            string response;
            if (SupportAdvancedCommandSet && AllowAdvancedCommandSet)
            {
                response = CmdToMount(axis, 'X', on ? "0500" : "0501");
            }
            else
            {
                response = CmdToMount(axis, 'O', on ? "1" : "0");
            }
            return response;
        }

        /// <summary>
        /// set ST4 guide rate
        /// </summary>
        /// <param name="rate"> 0..4 (1.0, 0.75, 0.50, 0.25, 0.125)</param>
        internal void SetSt4GuideRate(int rate)
        {
            CmdToMount(AxisId.Axis1, 'P', $"{rate}");

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":P|Axis1|{rate}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// S Set absolute goto target 
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="position"></param>
        internal void SetTargetPosition(AxisId axis, double position)
        {
            var szCmd = LongToHex(Convert.ToInt64(position));
            CmdToMount(axis, 'S', szCmd);
        }

        /// <summary>
        /// U Set the Break Steps
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="newBrakeSteps"></param>
        internal void SetBreakSteps(AxisId axis, long newBrakeSteps)
        {
            var szCmd = LongToHex(newBrakeSteps);
            CmdToMount(axis, 'U', szCmd);
        }

        /// <summary>
        /// V Set Polar LED brightness
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="level">x00 to xFF</param>
        internal void SetPolarLedLevel(AxisId axis, int level)
        {
            if (level < 0 || level > 255) { return; }
            var szCmd = level.ToString("X2");
            CmdToMount(axis, 'V', szCmd);
        }

        /// <summary>
        /// Wx01 on/off pPEC train
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on"></param>
        internal void SetPPecTrain(AxisId axis, bool on)
        {
            var szCmd = LongToHex(1);
            if (on)
            {
                szCmd = LongToHex(0);
            }
            CmdToMount(axis, 'W', szCmd);
        }

        /// <summary>
        /// Wx03 on/off pPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on"></param>
        internal string SetPPec(AxisId axis, bool on)
        {
            var szCmd = LongToHex(3);
            if (on)
            {
                szCmd = LongToHex(2);
            }
            var response = CmdToMount(axis, 'W', szCmd, true);
            return response;
        }

        /// <summary>
        /// Wx04 Wx05 on/off encoders
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on"></param>
        internal void SetEncoders(AxisId axis, bool on)
        {
            var szCmd = LongToHex(5);
            if (on)
            {
                szCmd = LongToHex(4);
            }
            CmdToMount(axis, 'W', szCmd);

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $":W|{axis}|{on}|{szCmd}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Wx06 Wx0601 on/off Full Current Low speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on"></param>
        internal void SetLowSpeedCurrent(AxisId axis, bool on)
        {
            var szCmd = LongToHex(6);
            if (on)
            {
                szCmd = "060100";
            }
            CmdToMount(axis, 'W', szCmd);
        }

        /// <summary>
        /// Wx07 Set Stride for Slewing
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void SetSlewingStride(AxisId axis)
        {
            var szCmd = LongToHex(7);
            CmdToMount(axis, 'W', szCmd);
        }

        /// <summary>
        /// Wx08 reset the home position index
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        internal void SetHomePositionIndex(AxisId axis)
        {
            var szCmd = LongToHex(8);
            CmdToMount(axis, 'W', szCmd);
        }

        /// <summary>
        /// ":Xn02vvvvvvvvvvvvvvvv" Slew an axis at a given speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="rateInRadian">rate in radian / second</param>
        internal void AxisSlew_Advanced(AxisId axis, double rateInRadian)
        {
            var ratePad = '0';

            //var irateInSteps = AngleToStep(axis, rateInRadian * 1024);
            //irateInSteps *= _resolutionFactor[(int)axis];
            var irateInSteps = AngleToStep(axis, rateInRadian * 1024 * _resolutionFactor[(int)axis]);

            if (rateInRadian < 0) { ratePad = 'F'; } // F for negative numbers
            var szCmd = "02" + irateInSteps.ToString("X").PadLeft(16, ratePad);

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"axis|{axis}|X szCmd|{szCmd}|Rate|{rateInRadian}|Steps|{irateInSteps}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            CmdToMount(axis, 'X', szCmd);
        }

        /// <summary>
        /// ":Xn04ppppppppvvvvvvvvvvvvvvvv" Slew to a position and then slew in 0 velocity
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="targetInRadian">target position in radian</param>
        /// <param name="rateInRadian"></param>
        internal void AxisSlewTo_Advanced(AxisId axis, double targetInRadian, double rateInRadian = 0)
        {
            var targetPad = '0';
            //var itargetInSteps = AngleToStep(axis, targetInRadian);
            //itargetInSteps *= _resolutionFactor[(int)axis];
            var itargetInSteps = AngleToStep(axis, targetInRadian * _resolutionFactor[(int)axis]);
            if (itargetInSteps < 0) { targetPad = 'F'; } // F for negative numbers

            var ratePad = '0';
            //var irateInSteps = AngleToStep(axis, rateInRadian * 1024);
            //irateInSteps *= _resolutionFactor[(int)axis];
            var irateInSteps = AngleToStep(axis, rateInRadian * 1024 * _resolutionFactor[(int)axis]);
            if (irateInSteps < 0) { ratePad = 'F'; } // F for negative numbers

            var szCmd = "04" + itargetInSteps.ToString("X").PadLeft(8, targetPad) + irateInSteps.ToString("X").PadLeft(16, ratePad);
            CmdToMount(axis, 'X', szCmd);

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Data,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"axis|{axis}|X szCmd|{szCmd}|Target|{targetInRadian}|TSteps|{itargetInSteps}|RSteps|{irateInSteps}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        #endregion

        #region SerialIO

        /// <summary>
        /// Bypass for mount commands
        /// </summary>
        /// <param name="axis">1 or 2</param>
        /// <param name="cmd">The command char set</param>
        /// <param name="cmdData">The data need to send</param>
        /// <param name="ignoreWarnings">to ignore serial response issues</param>
        /// <returns>mount data, null for IsNullOrEmpty</returns>
        /// <example>CmdToMount(1,"X","0003","true")</example>
        internal string CmdToMount(int axis, string cmd, string cmdData, string ignoreWarnings)
        {
            AxisId a;
            switch (axis)
            {
                case 1:
                    a = AxisId.Axis1;
                    break;
                case 2:
                    a = AxisId.Axis2;
                    break;
                default:
                    throw new Exception("Invalid axis parameter");
            }
            var b = bool.Parse(ignoreWarnings.Trim());
            var c = char.Parse(cmd.Trim());
            var d = cmdData == null ? string.Empty : cmdData.Trim();
            var response = CmdToMount(a, c, d, b);
            return response;
        }

        /// <summary>
        /// One communication between mount and client
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="command">The command char set</param>
        /// <param name="cmdDataStr">The data need to send</param>
        /// <param name="ignoreWarnings">to ignore serial response issues</param>
        /// <returns>The response string from mount</returns>
        private string CmdToMount(AxisId axis, char command, string cmdDataStr, bool ignoreWarnings = false)
        {
            MonitorEntry monitorItem;
            for (var i = 0; i <= 5; i++)
            {
                var acquiredLock = false;
                try
                {
                    Monitor.TryEnter(_syncObject, ThreadLockTimeout, ref acquiredLock);
                    if (acquiredLock)
                    {
                        // Code that accesses resources that are protected by the lock.
                        try
                        {
                            string responseString = null;
                            for (var c = 0; c <= 10; c++)
                            {
                                SkyQueue.Serial.DiscardInBuffer();
                                SkyQueue.Serial.DiscardOutBuffer();
                                // send the request
                                var cmdData = SendRequest(axis, command, cmdDataStr);
                                // receive the response
                                responseString = ReceiveResponse(axis, command, cmdData);
                                if (!string.IsNullOrEmpty(responseString))
                                {
                                    _conErrCnt = 0;
                                    break;
                                }

                                _conErrCnt++;
                                monitorItem = new MonitorEntry
                                {
                                    Datetime = HiResDateTime.UtcNow,
                                    Device = MonitorDevice.Telescope,
                                    Category = MonitorCategory.Mount,
                                    Type = MonitorType.Warning,
                                    Method = MethodBase.GetCurrentMethod()?.Name,
                                    Thread = Thread.CurrentThread.ManagedThreadId,
                                    Message =
                                        $"Serial Retry:{_conErrCnt}|{cmdData}|{ignoreWarnings}"
                                };
                                MonitorLog.LogToMonitor(monitorItem);
                                if (_conErrCnt > ConErrMax)
                                {
                                    var msg = "Count:" + _conErrCnt;
                                    _conErrCnt = 0;
                                    throw new MountControlException(ErrorCode.ErrTooManyRetries, msg);
                                }
                                Thread.Sleep(10);
                            }

                            if (string.IsNullOrEmpty(responseString))
                            {
                                // sometimes :j will not return a response so this is used to ignore it
                                if (ignoreWarnings) { return null; }

                                // serial issue stop axes
                                SendRequest(AxisId.Axis1, 'K', null);
                                SendRequest(AxisId.Axis2, 'K', null);
                                throw new TimeoutException($"Null Response for :{command}{(int)axis + 1} {cmdDataStr}".Trim());
                            }
                            MountConnected = true;
                            return responseString;
                        }
                        catch (TimeoutException ex)
                        {
                            if (ignoreWarnings) { return null; }

                            MountConnected = false;
                            Debug.WriteLine("OOPS: " + ex.Message);
                            throw new MountControlException(axis == AxisId.Axis1 ? ErrorCode.ErrNoResponseAxis1 : ErrorCode.ErrNoResponseAxis2, "Timeout", ex);
                        }
                        catch (IOException ex)
                        {
                            MountConnected = false;
                            monitorItem = new MonitorEntry
                            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                            MonitorLog.LogToMonitor(monitorItem);

                            throw new MountControlException(ErrorCode.ErrNotConnected, "IO Error", ex);
                        }
                        catch (Exception ex)
                        {
                            MountConnected = false;
                            monitorItem = new MonitorEntry
                            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                            MonitorLog.LogToMonitor(monitorItem);
                            throw;
                        }
                    }
                    else
                    {
                        // deal with the fact that the lock was not acquired.
                        monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Lock not acquired #{i} Command:{command} String:{cmdDataStr}" };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                }
                finally
                {
                    if (acquiredLock) Monitor.Exit(_syncObject);
                }
                Thread.Sleep(3);
            }
            // deal with the fact that the lock was not acquired.
            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Thread Lock Timeout" };
            MonitorLog.LogToMonitor(monitorItem);
            return null;
        }

        /// <summary>
        /// Builds the command string
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="command"></param>
        /// <param name="cmdDataStr"></param>
        private string SendRequest(AxisId axis, char command, string cmdDataStr)
        {
            const char startCharOut = ':';
            if (cmdDataStr == null) cmdDataStr = "";
            const int bufferSize = 20;
            var commandStr = new StringBuilder(bufferSize);
            commandStr.Append(startCharOut);                    // 0: Leading char
            commandStr.Append(command);                         // 1: Length of command( Source, destination, command char, data )
            // Target Device
            commandStr.Append(axis == AxisId.Axis1 ? '1' : '2');// 2: Target Axis
            // Copy command data to buffer
            commandStr.Append(cmdDataStr);
            commandStr.Append(EndChar);                         // CR Character            

            var dt = HiResDateTime.UtcNow;
            var monitorItem = new MonitorEntry
            { Datetime = dt, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{commandStr.ToString().Trim()}" };
            MonitorLog.LogToMonitor(monitorItem);

            switch (command)
            {
                // store time for any measurements
                case 'j' when axis == AxisId.Axis1:
                    LastJ1RunTime = dt;
                    break;
                case 'j' when axis == AxisId.Axis2:
                    LastJ2ARunTime = dt;
                    break;
                case 'J' when axis == AxisId.Axis2:
                    LastJ2RunTime = dt;
                    break;
                case 'I' when axis == AxisId.Axis1:
                    LastI1RunTime = dt;
                    break;
                case 'X':
                    if (cmdDataStr == "0003")
                    {
                        switch (axis)
                        {
                            case AxisId.Axis1:
                                LastJ1RunTime = dt;
                                break;
                            case AxisId.Axis2:
                                LastJ2ARunTime = dt;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                        }
                    }
                    if (cmdDataStr.Substring(0, 2) == "02")
                    {
                        switch (axis)
                        {
                            case AxisId.Axis1:
                                LastI1RunTime = dt;
                                break;
                            case AxisId.Axis2:
                                LastJ2RunTime = dt;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                        }
                    }
                    break;
            }

            //Serial.Transmit(commandStr.ToString());
            SkyQueue.Serial.Write(commandStr.ToString());

            return commandStr.ToString().Trim();
        }

        ///// <summary>
        ///// Work for serial port event - medium cpu usage
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    IncomingData = ReceiveResponse();
        //}

        ///// <summary>
        ///// Errors for serial port event
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private static void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        //{
        //    var monitorItem = new MonitorEntry
        //        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{e}" };
        //    MonitorLog.LogToMonitor(monitorItem);
        //}

        ///// <summary>
        ///// Reads serial port buffer - medium cpu usage
        ///// </summary>
        //private StringBuilder serialBuffer = new StringBuilder();
        //private const string terminationSequence = "\r";
        //private string DataReceived(string cmdDataStr)
        //{
        //    try
        //    {
        //        var data = Serial.ReadExisting();
        //        string message = null;
        //        serialBuffer.Append(data);
        //        var bufferString = serialBuffer.ToString();
        //        int index;
        //        do
        //        {
        //            index = bufferString.IndexOf(terminationSequence, StringComparison.Ordinal);
        //            if (index <= -1) continue;
        //            message = bufferString.Substring(0, index);
        //            bufferString = bufferString.Remove(0, index + terminationSequence.Length);

        //        } while (index > -1);
        //        serialBuffer = new StringBuilder(bufferString);
        //        return message;
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceInformation("Retry {0}", ex.Message);
        //        var monitorItem = new MonitorEntry
        //            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{cmdDataStr}|{ex.Message}" };
        //        MonitorLog.LogToMonitor(monitorItem);
        //        throw;
        //    }
        //}

        /// <summary>
        /// Read serial port buffer - skyWatcher original source
        /// </summary>
        /// <returns></returns>
        private static string ReceiveResponse()
        {
            // format "::e1\r=020883\r"
            var mBuffer = new StringBuilder(15);
            var startReading = false;

            var sw = Stopwatch.StartNew();
            var readTimeout = SkyQueue.Serial.ReadTimeout;
            while (sw.ElapsedMilliseconds < readTimeout)
            {
                var data = SkyQueue.Serial.ReadExisting();
                foreach (var byt in data)
                {
                    // this code order is important
                    if (byt == '=' || byt == '!' || byt == EndChar) startReading = true;
                    if ((byt == EndChar) && (mBuffer.Length == 0)) continue;
                    if (startReading) mBuffer.Append(byt);
                    if (byt != EndChar) continue;
                    if (!startReading) continue;
                    return mBuffer.ToString();
                }
                Thread.Sleep(1);
            }
            return null;
        }

        /// <summary>
        /// Constructs a string from the response
        /// </summary>
        /// <returns></returns>
        private string ReceiveResponse(AxisId axis, char command, string cmdDataStr)
        {
            //var sw  = Stopwatch.StartNew();
            //while (sw.Elapsed.TotalMilliseconds < 1000)
            //{
            //    // alternative method
            //    //receivedData = DataReceived(cmdDataStr);

            //    receivedData = ReceiveResponse();
            //    if (!string.IsNullOrEmpty(receivedData)) break;

            //    // alternative using events
            //    //if (!string.IsNullOrEmpty(IncomingData))
            //    //{
            //    //    receivedData = IncomingData;
            //    //    IncomingData = null;
            //    //    break;
            //    //}
            //    //Thread.Sleep(10);
            //}
            //sw.Stop();

            var receivedData = ReceiveResponse();

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{cmdDataStr}|{receivedData}" };
            MonitorLog.LogToMonitor(monitorItem);

            // process incoming data string
            receivedData = receivedData?.Trim();
            receivedData = receivedData?.Replace("\0", string.Empty);
            if (string.IsNullOrEmpty(receivedData)) { return null; }

            switch (receivedData[0].ToString())
            {
                //receive '=DDDDDD [0D]'    or '!D [0D]'
                case "=":  // Normal response
                    break;
                case "!":  // Abnormal response.
                    string errorMsg;
                    var subData = string.Empty;
                    switch (receivedData)
                    {
                        case "!":
                            errorMsg = "Invalid Reason Code";
                            if (command == 'q') subData = "=000000";
                            break;
                        case "!0":
                            errorMsg = "Unknown Command: Command doesn't apply to the model";
                            switch (command)
                            {
                                case 'q':
                                case 'W':
                                    subData = "=000000"; // EQ6R W1060100,!0 workaround
                                    break;
                                case 'O':
                                    subData = "!0"; // for not supported
                                    break;
                            }
                            break;
                        case "!1":
                            errorMsg = "Invalid Param count: Valid command was passed with invalid param count";
                            break;
                        case "!2":
                            errorMsg = "Motor not Stopped: Valid command failed to run ( ie sending :G whilst motor is running )";
                            // send stop
                            SendRequest(axis, 'K', null);
                            Thread.Sleep(500);
                            break;
                        case "!3":
                            errorMsg = "NonHex Param: Parameter contains a non uppercase Hex Char ";
                            break;
                        case "!4":
                            errorMsg = "Not energized: Motor is not energized";
                            break;
                        case "!5":
                            errorMsg = "Driver Asleep: card is in sleep mode";
                            break;
                        case "!6":
                            errorMsg = "Mount is not tracking";
                            break;
                        case "!7":
                            errorMsg = "Unknown";
                            break;
                        case "!8":
                            errorMsg = "Invalid pPEC model";
                            subData = "!8";
                            break;
                        case "!9":
                            errorMsg = "Invalid Command";
                            break;
                        case "!A":
                            errorMsg = "Extra following data overtime";
                            break;
                        default:
                            errorMsg = "Code Not Found";
                            break;
                    }

                    monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Abnormal Response|Axis|{axis}|Command|{command}|Received|{receivedData}|CommandStr|{cmdDataStr}|Message|{errorMsg}" };
                    MonitorLog.LogToMonitor(monitorItem);
                    if (!string.IsNullOrEmpty(subData)) { return subData; }
                    receivedData = null;
                    break;
                default:
                    receivedData = null;
                    break;
            }
            return receivedData;
        }

        ///// <summary>
        ///// Sends :e1 to the mounts and evaluates response to see its an appropriate response.
        ///// </summary>
        //internal void TestSerial()
        //{
        //    var isError = true;
        //    //Serial.ClearBuffers();
        //    // send the request
        //    SendRequest(AxisId.Axis1, 'e', null);
        //    // receive the response
        //     var responseString = Serial.ReceiveCounted(8);
        //    //var responseString = Serial.ReadLine();

        //    if (responseString.Length > 0)
        //    {
        //        responseString = responseString.Replace("\0", string.Empty).Trim();
        //        // check to see if the response is valid 
        //        switch (responseString[0].ToString())
        //        {
        //            case "=":
        //                isError = false;
        //                break;
        //            case "!":
        //                isError = false;
        //                break;
        //        }
        //        // check to see if the number for the mount type is valid
        //        if (!isError)
        //        {
        //            var parsed = int.TryParse(responseString.Substring(6, 1), out var mountNumber);
        //            if (parsed)
        //            {
        //                if (mountNumber < 0 || mountNumber > 6)isError = true;
        //            }
        //            else
        //            {
        //                isError = true;
        //            }
        //        }

        //    }

        //    var monitorItem = new MonitorEntry
        //        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Response:{responseString}"};
        //    MonitorLog.LogToMonitor(monitorItem);

        //    if (!isError) return;
        //    throw new MountControlException(ErrorCode.ErrMountNotFound);
        //}

        /// <summary>
        /// Converts the string to a long
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static long StringToLong(string str)
        {
            try
            {
                long value = 0;
                for (var i = 1; i + 1 < str.Length; i += 2)
                {
                    value += (long)(int.Parse(str.Substring(i, 2), NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
                }

                var msg = $"|{str}|{value}";
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                MonitorLog.LogToMonitor(monitorItem);

                return value;
            }
            catch (FormatException e)
            {
                throw new MountControlException(ErrorCode.ErrInvalidData, "Response Parse Error: " + str, e);
            }
        }

        /// <summary>
        /// Converts a long to Hex command
        /// </summary>
        /// <param name="number"></param>
        /// <returns>31 -> 1F0000</returns>
        private static string LongToHex(long number)
        {
            var chars = new char[6];

            for (var i = 0; i < chars.Length; i += 2)
            {
                var part = ((int)number & 0xFF).ToString("X2");
                chars[i] = part[0];
                chars[i + 1] = part[1];
                number >>= 8;
            }

            return new string(chars);
        }

        /// <summary>
        /// Converts steps to angle
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="steps"></param>
        /// <returns></returns>
        internal double StepToAngle(AxisId axis, long steps)
        {
            var a = steps * _factorStepToRad[(int)axis];
            return a;
        }

        /// <summary>
        /// Converts Angle to steps
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="angleInRad"></param>
        /// <returns></returns>
        public int AngleToStep(AxisId axis, double angleInRad)
        {
            //var a = (long)(angleInRad * _factorRadToStep[(int)axis]);
            //var a = (int)Math.Floor(angleInRad * _factorRadToStep[(int)axis]);
            var a = (int) Math.Round(angleInRad * _factorRadToStep[(int)axis]);
            return a;
        }

        /// <summary>
        /// Converts hex response to integer for new advanced command set
        /// </summary>
        /// <notes>
        /// The stepper motor driver IC runs in 256 micro-step, while the previous motor board runs in 64 micro-step.
        /// Since the CPR value is larger than a 24-bit integer, we have to cheat the host in the old command set.
        /// </notes>
        private static int String32ToInt(string response, bool parseFirst, int divFactor)
        {
            try
            {
                if (parseFirst && response.Length > 0)
                { response = response.Substring(1, response.Length - 1); }
                var parsed = int.Parse(response, NumberStyles.HexNumber);
                var a = parsed / divFactor;

                var msg = $"|{response}|{parseFirst}|{divFactor}|{a}";
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = msg };
                MonitorLog.LogToMonitor(monitorItem);
                return a;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{response}|{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }

        private static bool IsBitSet(int b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        #endregion
    }
}
