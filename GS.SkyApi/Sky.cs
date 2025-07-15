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
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.SkyWatcher;
using GS.Simulator;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace GS.SkyApi
{
    /// <summary>
    /// GS Server API using the SkyWatcher Protocol
    /// </summary>
    [Gss("GreenSwamp")]
    [Guid("9D65CC8C-4E34-4FCF-9703-8632A202363E")]
    [ProgId("GS.SkyApi")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Sky : ObjectBase, ISky
    {
        /// <inheritdoc />
        public bool AscomOn
        {
            get
            {
                ValidateMount();
                var r = SkyServer.AsComOn;
                return r;
            }
            set
            {
                ValidateMount();

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AsComOn = value;
            }
        }
        /// <inheritdoc />
        public void AutoHomeStart(int degreeLimit = 100, int offsetDec = 0)
        {
            if (!SkyServer.CanHomeSensor)
            { throw new Exception("Mount doesn't support home sensors"); }
            if(!SkyServer.IsMountRunning)
            { throw new Exception("Mount not connected"); }
            if (degreeLimit < 20 || degreeLimit > 179)
            { throw new Exception("Degrees out of limits"); }
            if (offsetDec < -90 || offsetDec > 90)
            { throw new Exception("Offset out of limits"); }

            SkyServer.AutoHomeStop = false;
            SkyServer.AutoHomeAsync(degreeLimit, offsetDec);
        }

        /// <inheritdoc />
        public void AutoHomeStop()
        {
           SkyServer.AutoHomeStop = true;
        }

        /// <inheritdoc />
        public void AxisGoToTarget(int axis, double targetPosition)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{targetPosition}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            if (validAxis == AxisId.Axis1)
            {
                targetPosition = SkyServer.ConvertToAzEastWest(targetPosition);
            }
            var command = new SkyAxisGoToTarget(SkyQueue.NewId, validAxis, targetPosition);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisMoveSteps(int axis, long steps)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{steps}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisMoveSteps(SkyQueue.NewId, validAxis, steps);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisPulse(int axis, double guideRate, int duration, int backlashSteps = 0)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{guideRate}|{duration}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisPulse(SkyQueue.NewId, validAxis, guideRate, duration, backlashSteps, new CancellationToken());
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisSlew(int axis, double rate)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{rate}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisSlew(SkyQueue.NewId, validAxis, rate);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisStop(int axis)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisStop(SkyQueue.NewId, validAxis);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisStopInstant(int axis)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisStopInstant(SkyQueue.NewId, validAxis);
            GetResult(command);
        }

        /// <inheritdoc />
        public bool CanAxisSlewsIndependent
        {
            get
            {
                ValidateMount();
                var command = new SkyCanAxisSlewsIndependent(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanAzEq
        {
            get
            {
                ValidateMount();
                var command = new SkyCanAzEq(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanDualEncoders
        {
            get
            {
                ValidateMount();
                var command = new SkyCanDualEncoders(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanHalfTrack
        {
            get
            {
                ValidateMount();
                var command = new SkyCanHalfTrack(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanHomeSensors
        {
            get
            {
                ValidateMount();
                var command = new SkyCanHomeSensors(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanPolarLed
        {
            get
            {
                ValidateMount();
                var command = new SkyCanPolarLed(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanPPec
        {
            get
            {
                ValidateMount();
                var command = new SkyCanPPec(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanWifi
        {
            get
            {
                ValidateMount();
                var command = new SkyCanWifi(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public string CmdToMount(int axis, string cmd, string cmdData, string ignoreWarnings)
        {
            ValidateMount();
            ValidateAxis(axis);
            var command = new SkyCmdToMount(SkyQueue.NewId, axis, cmd, cmdData, ignoreWarnings);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public int DecBacklash
        {
            get => SkySettings.DecBacklash;
            set
            {
                if (value < 0 || value > 500) return;
                SkySettings.DecBacklash = value;
            }
        }

        /// <inheritdoc />
        public long GetAngleToStep(int axis, double angleInRad)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAngleToStep(SkyQueue.NewId, validAxis, angleInRad);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetAxisVersion(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAxisVersions(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public string GetAxisStringVersion(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAxisStringVersions(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public double GetAxisPosition(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAxisPosition(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetAxisPositionCounter(int axis, bool raw)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAxisPositionCounter(SkyQueue.NewId, validAxis, raw);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetControllerVoltage(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetControllerVoltage(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetEncoderCount(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetEncoderCount(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double Get_j(int axis, bool raw)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetJ(SkyQueue.NewId, validAxis, raw);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetFactorRadRateToInt(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetFactorRadRateToInt(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public long GetHighSpeedRatio(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetHighSpeedRatio(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public long GetHomePosition(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetHomePosition(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetLastGoToTarget(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetLastGoToTarget(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetLastSlewSpeed(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetLastSlewSpeed(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetLowSpeedGotoMargin(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetLowSpeedGotoMargin(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public bool GetLowVoltageErrorState()
        {
            return SkyServer.LowVoltageEventState;
        }

        /// <inheritdoc />
        public string GetMotorCardVersion(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetMotorCardVersion(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetPecPeriod(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetPecPeriod(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }
        
        /// <inheritdoc />
        public string GetPositionsAndTime(bool raw)
        {
            ValidateMount();
            var command = new SkyGetPositionsAndTime(SkyQueue.NewId, raw);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetRampDownRange(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetRampDownRange(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetSiderealRate(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetSiderealRate(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public double GetStepToAngle(int axis, long steps)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetStepToAngle(SkyQueue.NewId, validAxis, steps);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long GetStepsPerRevolution(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetStepsPerRevolution(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public long GetStepTimeFreq(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetStepTimeFreq(SkyQueue.NewId);
            var results = GetResult(command);
            switch (validAxis)
            {
                case AxisId.Axis1:
                    return results.Result[0];
                case AxisId.Axis2:
                    return results.Result[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public double GuideRateDeclination
        {
            get => SkyServer.GuideRateDec;
            set => SkyServer.GuideRateDec = value;
        }

        /// <inheritdoc />
        public double GuideRateRightAscension
        {
            get => SkyServer.GuideRateRa;
            set => SkyServer.GuideRateRa = value;
        }

        /// <inheritdoc />
        public void InitializeAxes()
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkyInitializeAxes(SkyQueue.NewId);
            GetResult(command);
        }

        /// <inheritdoc />
        public bool IsAutoHomeRunning
        {
            get
            {
                var running = SkyServer.IsAutoHomeRunning;
                return running;
            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get
            {
                var command = new SkyIsConnected(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool IsMountRunning
        {
            get => SkyServer.IsMountRunning;
            set
            {
                if (SkyServer.IsMountRunning != value) SkyServer.IsMountRunning = value;
            }
        }

        /// <inheritdoc />
        public bool IsParked
        {
            get
            {
                CheckRunning();
                return SkyServer.AtPark;
            }
        }

        /// <inheritdoc />
        public bool IsPPecInTrainingOn
        {
            get
            {
                ValidateMount();
                var command = new SkyIsPPecInTrainingOn(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool IsPPecOn
        {
            get
            {
                ValidateMount();
                var command = new SkyIsPPecOn(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool IsFullStop(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsAxisFullStop(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public bool IsHighSpeed(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsHighSpeed(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public bool IsServerSkyWatcher => SkySettings.Mount == Server.SkyTelescope.MountType.SkyWatcher;

        /// <inheritdoc />
        public bool IsSlewing(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsSlewing(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public bool IsSlewingForward(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsSlewingForward(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public bool IsSlewingTo(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsSlewingTo(SkyQueue.NewId, validAxis);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public string LastAutoHomeError
        {
            get
            {
                if (string.IsNullOrEmpty(SkyServer.LastAutoHomeError.Message)) return string.Empty;
                return SkyServer.LastAutoHomeError.Message;
            }
        }
        
        /// <inheritdoc />
        public string MountType
        {
            get
            {
                ValidateMount();
                var command = new SkyMountType(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool MountVersion
        {
            get
            {
                ValidateMount();
                var command = new SkyMountVersion(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public void Park()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);
            CheckRunning();

            if (SkyServer.AtPark)
            {
                throw new Exception("Parked");
            }

            var found = SkySettings.ParkPositions.Find(x => x.Name == SkyServer.ParkSelected.Name);
            if (found != null)
            {
                SkyServer.GoToPark();
            }
            else
            {
                throw new Exception("Not Found");
            }
        }

        /// <inheritdoc />
        public string ParkPosition
        {
            get => SkyServer.ParkSelected.Name;
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);
                if (IsMountRunning == false) { return; }

                if (string.IsNullOrEmpty(value)) return;
                var found = SkySettings.ParkPositions.Find(x => x.Name == value);
                if (found != null)
                {
                    SkyServer.ParkSelected = found;
                }
                else
                {
                    throw new Exception("Not Found");
                }
            }
        }

        /// <inheritdoc />
        public void SkySetAlternatingPPec(bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkySetAlternatingPPec(SkyQueue.NewId, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetAxisPosition(int axis, double position)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{position}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetAxisPosition(SkyQueue.NewId, validAxis, position);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetAxisPositionCounter(int axis, int position)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{position}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetAxisPositionCounter(SkyQueue.NewId, validAxis, position);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetBreakPointIncrement(int axis, long stepsCount)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{stepsCount}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetBreakPointIncrement(SkyQueue.NewId, validAxis, stepsCount);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetDecPulseToGoTo(bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkySetDecPulseToGoTo(SkyQueue.NewId, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetEncoder(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetEncoder(SkyQueue.NewId, validAxis, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetFullCurrentLowSpeed(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetFullCurrent(SkyQueue.NewId, validAxis, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetGotoTargetIncrement(int axis, long stepsCount)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{stepsCount}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetGotoTargetIncrement(SkyQueue.NewId, validAxis, stepsCount);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetHomePositionIndex(int axis)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetHomePositionIndex(SkyQueue.NewId, validAxis);
            GetResult(command);
        }

        /// <inheritdoc />
        public void StartMotion(int axis)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyStartMotion(SkyQueue.NewId, validAxis);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetMotionMode(int axis, int func, int direction)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{func}|{direction}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetMotionMode(SkyQueue.NewId, validAxis, func, direction);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetPPec(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            SkyServer.PPecOn = on;
            //var validAxis = ValidateAxis(axis);
            //var command = new SkySetPPec(SkyQueue.NewId, validAxis, on);
            //GetResult(command);

        }

        /// <inheritdoc />
        public void SetPPecTrain(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            SkyServer.PecTraining = on;
            //var validAxis = ValidateAxis(axis);
            //var command = new SkySetPPecTrain(SkyQueue.NewId, validAxis, on);
            //GetResult(command);
        }

        /// <inheritdoc />
        public void SetStepSpeed(int axis, long stepSpeed)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{stepSpeed}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetStepSpeed(SkyQueue.NewId, validAxis, stepSpeed);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetSimGotoSpeed(int rate)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{rate}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateRange(1,20,rate);
            var command = new CmdGotoSpeed(MountQueue.NewId, rate);
            var results = GetSimResult(command, true);
            if (!results.Successful){throw new Exception("Error");}
        }

        /// <inheritdoc />
        public void SetTargetPosition(int axis, double position)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{position}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetTargetPosition(SkyQueue.NewId, validAxis, position);
            GetResult(command);
        }

        /// <inheritdoc />
        public void ShutdownServer()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "API Shutdown" };
            MonitorLog.LogToMonitor(monitorItem);

            SkyServer.ShutdownServer();
        }

        /// <inheritdoc />
        public void UnPark()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckRunning();
            SkyServer.AtPark = false;
            SkyServer.Tracking = true;
        }
        
        /// <summary>
        /// Validate axis number as 1 or 2
        /// </summary>
        private void ValidateMount()
        {
            if (!IsServerSkyWatcher) { throw new Exception("Server not set to correct mount type"); }
            if (!IsConnected) throw new Exception("Mount not connected");
        }

        private void CheckRunning()
        {
            if (!IsMountRunning) {throw new Exception("Mount not connected");}
        }

        /// <summary>
        /// validate primary or secondary axis
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        private static AxisId ValidateAxis(int axis)
        {
            switch (axis)
            {
                case 1:
                    return AxisId.Axis1;
                case 2:
                    return AxisId.Axis2;
                default:
                    var exception = new ArgumentOutOfRangeException();
                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    throw exception;
            }

        }

        /// <summary>
        /// validate a double within a range
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="value"></param>
        private static void ValidateRange(double lower, double upper, double value )
        {
            if (value >= lower && value <= upper)
            {
                return;
            }
            var exception = new ArgumentOutOfRangeException($"{value}", $@"{lower}-{upper}");
            throw exception;
        }

        /// <summary>
        /// Check the results of a Sky command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private static ISkyCommand GetResult(ISkyCommand command)
        {
            // look for a result by id and return it
            var result = SkyQueue.GetCommandResult(command);

            if (result == null)
            {
                var exception = new Exception("Timeout processing command");

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw exception;
            }
            else
            {
                if (result.Successful) return result;

                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{result.Exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw result.Exception;
            }
        }

        /// <summary>
        /// Check the simulator results of a command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="allowNull"></param>
        /// <returns></returns>
        private static IMountCommand GetSimResult(IMountCommand command, bool allowNull)
        {
            // look for a result by id and return it
            var result = MountQueue.GetCommandResult(command);

            if (result == null)
            {
                if (allowNull){return null;}
                var exception = new Exception("Timeout processing command");

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw exception;
            }
            else
            {
                if (result.Successful) return result;

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{result.Exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw result.Exception;
            }
        }
    }

    [Guid("61B480B2-C2C7-453F-B197-CC585D3CD390")]
    public interface ISky
    {
        /// <summary>
        /// Tells GSS not to process any AsCoM moment commands for external programs using the AsCoM driver. 
        /// </summary>
        /// <returns>bool</returns>
        bool AscomOn { get; set; }
        /// <summary>
        /// Starts the AutoHome slew to home sensors
        /// </summary>
        /// <param name="degreelimit"></param>
        /// <param name="offsetdec"></param>
        void AutoHomeStart(int degreelimit = 100, int offsetdec = 0);
        /// <summary>
        /// Stops Auto home from completing
        /// </summary>
        void AutoHomeStop();
        /// <summary>
        /// Move axis number of micro steps, not marked as slewing
        /// </summary>
        /// <param name="axis">>axis number 1 or 2</param>
        /// <param name="steps">number of micro steps</param>
        /// <returns>nothing</returns>
        void AxisMoveSteps(int axis, long steps);
        /// <summary>
        /// Send a pulse command
        /// </summary>
        /// <param name="axis">Axis 1 or 2</param>
        /// <param name="guideRate">GuideRate degrees, 15.041/3600*.5, negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, always positive numbers</param>
        /// <param name="backlashSteps">Positive micro steps added for backlash</param>
        /// <returns>nothing</returns>
        void AxisPulse(int axis, double guideRate, int duration, int backlashSteps = 0);
        /// <summary>
        /// Goto position in degrees
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="targetPosition">position in degrees</param>
        /// <returns>nothing</returns>
        void AxisGoToTarget(int axis, double targetPosition);
        /// <summary>
        /// Slew axis based on a rate in degrees.  Use this for small movements
        /// like pulse guiding, rate changes, guiding changes, not go tos
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="rate">rate/sec in degrees</param>
        /// <returns>nothing</returns>
        void AxisSlew(int axis, double rate);
        /// <summary>
        /// K Slows to a stop movement of an Axis 
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        void AxisStop(int axis);
        /// <summary>
        /// L Abruptly stops movement of an Axis
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>nothing</returns>
        void AxisStopInstant(int axis);
        /// <summary>
        /// q Axes slews must start independently 
        /// </summary>
        bool CanAxisSlewsIndependent { get; }
        /// <summary>
        /// q Does mount support AZ/EQ mode
        /// </summary>
        bool CanAzEq { get; }
        /// <summary>
        /// q Does mount support dual encoders
        /// </summary>
        bool CanDualEncoders { get; }
        /// <summary>
        /// q Does mount support half current tracking
        /// </summary>
        bool CanHalfTrack { get; }
        /// <summary>
        /// q Does mount support home sensors
        /// </summary>
        bool CanHomeSensors { get; }
        /// <summary>
        /// q Does mount support a polar LED
        /// </summary>
        bool CanPolarLed { get; }
        /// <summary>
        /// q Does mount support PPec
        /// </summary>
        bool CanPPec { get; }
        /// <summary>
        /// q Does mount support WiFi
        /// </summary>
        bool CanWifi { get; }
        /// <summary>
        /// Bypass for mount commands
        /// </summary>
        /// <param name="axis">1 or 2</param>
        /// <param name="cmd">The command char set</param>
        /// <param name="cmdData">The data need to send</param>
        /// <param name="ignoreWarnings">ignore serial response issues?</param>
        /// <returns>mount data, null for IsNullOrEmpty</returns>
        /// <example>CmdToMount(1,"X","0003","true")</example>
        string CmdToMount(int axis, string cmd, string cmdData, string ignoreWarnings);
        /// <summary>
        /// Sets the amount of steps added to Dec for reverse backlash pulse
        /// </summary>
        int DecBacklash { get; set; }
        /// <summary>
        /// Gets the number of steps from the angle in rad
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angleInRad"></param>
        /// <returns>Steps in rad</returns>
        long GetAngleToStep(int axis, double angleInRad);
        /// <summary>
        /// e Gets versions of axis in long format
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>long axis version</returns>
        long GetAxisVersion(int axis);
        /// <summary>
        /// e Gets version of axis in string readable format
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>string axis version as string</returns>
        string GetAxisStringVersion(int axis);
        /// <summary>
        /// j Gets current axis position in degrees
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>Get Current Axis position as double</returns>
        double GetAxisPosition(int axis);
        /// <summary>
        /// j Gets axis position counter
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="raw">false to subtract 0x00800000</param>
        /// <returns>Cardinal encoder count as long</returns>
        long GetAxisPositionCounter(int axis, bool raw = false);
        /// <summary>
        /// Retrieves the current voltage of the controller for the specified axis.
        /// </summary>
        /// <param name="axis">The axis identifier for which the voltage is requested. Must be a valid axis index.</param>
        /// <returns>The voltage of the controller for the specified axis, in volts.</returns>
        double GetControllerVoltage(int axis);
        /// <summary>
        /// d Gets Axis Current Encoder count
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>count as double</returns>
        double GetEncoderCount(int axis);
        /// <summary>
        /// Get :j data only not dependent on the advanced set
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="raw">false to subtract 0x00800000</param>
        /// <returns>Cardinal encoder count as long</returns>
        double Get_j(int axis, bool raw);
        /// <summary>
        /// Multiply the value of radians/second by this factor to get a 32-bit integer for the set speed used by the motor board.
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>factor used to get the speed</returns>
        double GetFactorRadRateToInt(int axis);
        /// <summary>
        /// Inquire motor high speed ratio
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>Ratio used to determine high speed</returns>
        long GetHighSpeedRatio(int axis);
        /// <summary>
        /// q Get Home position 
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param> 
        long GetHomePosition(int axis);
        /// <summary>
        /// h Get Current "goto" target
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        double GetLastGoToTarget(int axis);
        /// <summary>
        /// i Get Current "slew" speed
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        long GetLastSlewSpeed(int axis);
        /// <summary>
        /// Margin used to move from high speed to low speed
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        long GetLowSpeedGotoMargin(int axis);
        /// <summary>
        /// Determines whether a low voltage error has been logged by the controller.
        /// </summary>
        /// <returns><see langword="true"/> if a low voltage error has occured; otherwise, <see langword="false"/>.</returns>
        bool GetLowVoltageErrorState();
        /// <summary>
        /// e Gets the complete version string
        /// </summary>
        /// <returns></returns>
        string GetMotorCardVersion(int axis);
        /// <summary>
        /// s Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        double GetPecPeriod(int axis);
        /// <summary>
        /// Capture axes position and timestamp. 8 hex for ra, 8 hex for dec, 16 hex in microseconds
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        string GetPositionsAndTime(bool raw);
        /// <summary>
        /// c Micro steps from target where the ramp down process begins
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        double GetRampDownRange(int axis);
        /// <summary>
        /// D Sidereal rate in axis speed
        /// </summary>
        /// <returns></returns>
        /// <param name="axis">axis number 1 or 2</param>
        long GetSiderealRate(int axis);
        /// <summary>
        /// Gets the angle in rad from amount of steps
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        double GetStepToAngle(int axis, long steps);
        /// <summary>
        /// a Steps per revolution
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>Step Count</returns>
        long GetStepsPerRevolution(int axis);
        /// <summary>
        /// b Frequency of stepping timer
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>Frequency of stepping timer</returns>
        long GetStepTimeFreq(int axis);
        /// <summary>
        /// The current Declination guide rate 
        /// </summary>
        double GuideRateDeclination { get; set; }
        /// <summary>
        /// The current Right Ascension guide rate 
        /// </summary>
        double GuideRateRightAscension { get; set; }
        /// <summary>
        /// F Initialize both Axes
        /// </summary>
        void InitializeAxes();
        /// <summary>
        /// Is the auto home process running
        /// </summary>
        bool IsAutoHomeRunning { get; }
        /// <summary>
        /// Is mount in a connected serial state
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// Starts or Stops mount and connection
        /// </summary>
        bool IsMountRunning { get; set; }
        /// <summary>
        /// Is mount parked
        /// </summary>
        bool IsParked { get; }
        /// <summary>
        /// q Is the mount collecting PPec data
        /// </summary>
        bool IsPPecInTrainingOn { get; }
        /// <summary>
        /// q Does the mount have PPec turned on
        /// </summary>
        bool IsPPecOn { get; }
        /// <summary>
        /// j Is axis at full stop
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsFullStop(int axis);
        /// <summary>
        /// j Is axis in high speed mode
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsHighSpeed(int axis);
        /// <summary>
        /// Is mount type set to SkyWatcher
        /// </summary>
        bool IsServerSkyWatcher { get; }
        /// <summary>
        /// f Is axis slewing normal mode
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsSlewing(int axis);
        /// <summary>
        /// f Is axis slewing in a positive direction
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsSlewingForward(int axis);
        /// <summary>
        /// f Is axis slewing in goto mode
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsSlewingTo(int axis);
        /// <summary>
        /// Last known error from the AutoHome Process
        /// </summary>
        string LastAutoHomeError { get; }
        /// <summary>
        /// e Identify type of mount
        /// </summary>
        string MountType { get; }
        /// <summary>
        /// e Identify board version
        /// </summary>
        bool MountVersion { get; }
        /// <summary>
        /// Park mount to the current selected park position
        /// </summary>
        void Park();
        /// <summary>
        /// Get parked selected or Set to an existing park position name
        /// </summary>
        string ParkPosition { get; set; }
        /// <summary>
        /// Turns PPec off during movements and then back on for error correction moves
        /// </summary>
        /// <param name="on"></param>
        void SkySetAlternatingPPec(bool on);
        /// <summary>
        /// E Reset the position of an axis
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="position">degrees</param>
        void SetAxisPosition(int axis, double position);
        /// <summary>
        /// E Reset the position of an axis
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="position">degrees</param>
        void SetAxisPositionCounter(int axis, int position);
        /// <summary>
        /// M Set the break point increment
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="stepsCount">The steps count.</param>
        void SetBreakPointIncrement(int axis, long stepsCount);
        /// <summary>
        /// Turns on or off converting a Dec pulse guide into a Dec GoTo
        /// </summary>
        /// <param name="on"></param>
        void SetDecPulseToGoTo(bool on);
        /// <summary>
        /// W 4-5 Turn on off encoders
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetEncoder(int axis, bool on);
        /// <summary>
        /// W 6 Enable or Disable Full Current Low speed
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetFullCurrentLowSpeed(int axis, bool on);
        /// <summary>
        ///  H Set the goto target increment in steps
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="stepsCount"></param>
        void SetGotoTargetIncrement(int axis, long stepsCount);
        /// <summary>
        /// W 8 Reset the home position index
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        void SetHomePositionIndex(int axis);
        /// <summary>
        /// J Start motion based on previous settings
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        void StartMotion(int axis);
        /// <summary>
        /// G Set a different motion mode
        /// </summary>
        /// <param name="axis">Axis number 1 or 2</param>
        /// <param name="func">'0' high speed GOTO slewing,'1' low speed slewing mode,'2' low speed GOTO mode,'3' High slewing mode</param>
        /// <param name="direction">0=forward (CW) right, 1=backward (CCW) left, also based on observatory settings</param>
        void SetMotionMode(int axis, int func, int direction);
        /// <summary>
        /// W 2-3 Turn on off PPec
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetPPec(int axis, bool on);
        /// <summary>
        /// W 0-1 Turn on off PPEC training
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetPPecTrain(int axis, bool on);
        /// <summary>
        /// I Set slewing rate, seems to relate to amount of skipped step counts.  
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="stepSpeed">StepSpeed = 1 motor step movement, higher counts means slower movements</param>
        void SetStepSpeed(int axis, long stepSpeed);
        /// <summary>
        /// Set simulator goto rate 
        /// </summary>
        /// <param name="rate">1-20</param>
        void SetSimGotoSpeed(int rate);
        /// <summary>
        /// S Set absolute goto target 
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="position"></param>
        void SetTargetPosition(int axis, double position);
        /// <summary>
        /// shutdown and close the server
        /// </summary>
        void ShutdownServer();
        /// <summary>
        /// UnPark mount
        /// </summary>
        void UnPark();
    }
}
