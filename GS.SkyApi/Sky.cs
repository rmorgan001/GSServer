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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using GS.Shared;
using GS.SkyWatcher;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;

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
                var r = SkyServer.AscomOn;
                return r;
            }
            set
            {
                ValidateMount();

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AscomOn = value;
            }
        }

        /// <inheritdoc />
        public void AxisGoToTarget(int axis, double targetPosition)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{targetPosition}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisGoToTarget(SkyQueue.NewId, validAxis, targetPosition);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisMoveSteps(int axis, long steps)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{steps}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisMoveSteps(SkyQueue.NewId, validAxis, steps);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisPulse(int axis, double guiderate, int duration, int backlashsteps = 0)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{guiderate},{duration}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyAxisPulse(SkyQueue.NewId, validAxis, guiderate, duration, backlashsteps, SkyServer.Declination);
            GetResult(command);
        }

        /// <inheritdoc />
        public void AxisSlew(int axis, double rate)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{rate}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
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
        public bool CanOneStepDec
        {
            get
            {
                ValidateMount();
                var command = new SkyCanOneStepDec(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }

        /// <inheritdoc />
        public bool CanOneStepRa
        {
            get
            {
                ValidateMount();
                var command = new SkyCanOneStepRa(SkyQueue.NewId);
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
        public bool CanPpec
        {
            get
            {
                ValidateMount();
                var command = new SkyCanPpec(SkyQueue.NewId);
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
        public long GetAngleToStep(int axis, double angleinrad)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAngleToStep(SkyQueue.NewId, validAxis, angleinrad);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long[] GetAxisVersions()
        {
            ValidateMount();
            var command = new SkyGetAxisVersions(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public string[] GetAxisStringVersions()
        {
            ValidateMount();
            var command = new SkyGetAxisStringVersions(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
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
        public long GetAxisPositionCounter(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyGetAxisPositionCounter(SkyQueue.NewId, validAxis);
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
        public double[] GetFactorRadRateToInt()
        {
            ValidateMount();
            var command = new SkyGetFactorRadRateToInt(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long[] GetHighSpeedRatio()
        {
            ValidateMount();
            var command = new SkyGetHighSpeedRatio(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
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
        public long[] GetLowSpeedGotoMargin()
        {
            ValidateMount();
            var command = new SkyGetLowSpeedGotoMargin(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
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
        public bool[] GetOneStepIndicators()
        {
            ValidateMount();
            var command = new SkyGetOneStepIndicators(SkyQueue.NewId);
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
        public long[] GetStepsPerRevolution()
        {
            ValidateMount();
            var command = new  SkyGetStepsPerRevolution(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public long[] GetStepTimeFreq()
        {
            ValidateMount();
            var command = new SkyGetStepTimeFreq(SkyQueue.NewId);
            var results = GetResult(command);
            return results.Result;
        }

        /// <inheritdoc />
        public void InitializeAxes()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkyInitializeAxes(SkyQueue.NewId);
            GetResult(command);
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
        public bool IsPpecInTrainingOn
        {
            get
            {
                ValidateMount();
                var command = new SkyIsPpecInTrainingOn(SkyQueue.NewId);
                var results = GetResult(command);
                return results.Result;
            }
        }
        
        /// <inheritdoc />
        public bool IsPpecOn
        {
            get
            {
                ValidateMount();
                var command = new SkyIsPpecOn(SkyQueue.NewId);
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
        public bool IsSlewingFoward(int axis)
        {
            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkyIsSlewingFoward(SkyQueue.NewId, validAxis);
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
        public bool MountType
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
        public void SkySetAlternatingPpec(bool on)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}"};
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkySetAlternatingPpec(SkyQueue.NewId,on);
             GetResult(command);
        }

        /// <inheritdoc />
        public void SetAxisPosition(int axis, double position)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{position}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetAxisPosition(SkyQueue.NewId, validAxis, position);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetBreakPointIncrement(int axis, long stepsCount)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{stepsCount}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var command = new SkySetDecPulseToGoTo(SkyQueue.NewId, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetEncoder(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{on}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{on}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{stepsCount}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
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
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{func},{direction}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetMotionMode(SkyQueue.NewId, validAxis, func, direction);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetPpec(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetPpec(SkyQueue.NewId, validAxis, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetPpecTrain(int axis, bool on)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetPpecTrain(SkyQueue.NewId, validAxis, on);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetStepSpeed(int axis, long stepSpeed)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{stepSpeed}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetStepSpeed(SkyQueue.NewId, validAxis, stepSpeed);
            GetResult(command);
        }

        /// <inheritdoc />
        public void SetTargetPosition(int axis, double position)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{position}" };
            MonitorLog.LogToMonitor(monitorItem);

            ValidateMount();
            var validAxis = ValidateAxis(axis);
            var command = new SkySetTargetPosition(SkyQueue.NewId, validAxis, position);
            GetResult(command);
        }

        /// <summary>
        /// Validate axis number as 1 or 2
        /// </summary>
        private void ValidateMount()
        {
            if (!IsServerSkyWatcher) {throw new Exception("Server not set to correct mount type");}
            if (!IsConnected) throw new Exception("Mount not connected");
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
                    var exception =  new ArgumentOutOfRangeException();
                    var monitorItem = new MonitorEntry
                        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    throw exception;
            }
            
        }

        /// <summary>
        /// pull the results of a command from the dictionary
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
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw exception;
            }
            else
            {
                if (result.Successful) return result;

                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{result.Exception}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw result.Exception;
            }
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [Guid("61B480B2-C2C7-453F-B197-CC585D3CD390")]
    public interface ISky
    {
        /// <summary>
        /// Tells GSS not to process any ASCOM moment commands for external programs using the ASCOM driver. 
        /// </summary>
        /// <returns></returns>
        bool AscomOn { get; set; }
        /// <summary>
        /// Move axis number of microsteps, not marked as slewing
        /// </summary>
        /// <param name="axis">>axis number 1 or 2</param>
        /// <param name="steps">number of microsteps</param>
        void AxisMoveSteps(int axis, long steps);
        /// <summary>
        /// Send a pulse command
        /// </summary>
        /// <param name="axis">Axis 1 or 2</param>
        /// <param name="guiderate">Guiderate degrees, 15.041/3600*.5, negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, aways positive numbers</param>
        /// <param name="backlashsteps">Positive microsteps added for backlash</param>
        void AxisPulse(int axis, double guiderate, int duration, int backlashsteps = 0);
        /// <summary>
        /// Goto position in degrees
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="targetPosition">position in degrees</param>
        void AxisGoToTarget(int axis, double targetPosition);
        /// <summary>
        /// Slew axis based on a rate in degrees.  Use this for small movements
        /// like pulseguiding, rate changes, guiding changes, not gotos
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="rate">rate/sec in degrees</param>
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
        /// Test result if the mount can move Dec a single step in GoTo mode
        /// </summary>
        bool CanOneStepDec { get; }
        /// <summary>
        /// Test result if the mount can move Ra a single step in GoTo mode
        /// </summary>
        bool CanOneStepRa { get; }
        /// <summary>
        /// q Does mount support a polar LED
        /// </summary>
        bool CanPolarLed { get; }
        /// <summary>
        /// q Does mount support PPEC
        /// </summary>
        bool CanPpec { get; }
        /// <summary>
        /// q Does mount support WiFi
        /// </summary>
        bool CanWifi { get; }
        /// <summary>
        /// Sets the amount of steps added to Dec for reverse backlash pulse
        /// </summary>
        int DecBacklash { get; set; }
        /// <summary>
        /// Gets the number of steps from the angle in rad
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angleinrad"></param>
        /// <returns></returns>
        long GetAngleToStep(int axis, double angleinrad);
        /// <summary>
        /// e Gets versions of each axis in long format
        /// </summary>
        long[] GetAxisVersions();
        /// <summary>
        /// e Gets versions of each axis in string readable format
        /// </summary>
        string[] GetAxisStringVersions();
        /// <summary>
        /// j Gets current axis position in degrees
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>position in degrees</returns>
        double GetAxisPosition(int axis);
        /// <summary>
        /// j Gets axis poistion counter
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns>Cardinal encoder count</returns>
        long GetAxisPositionCounter(int axis);
        /// <summary>
        /// d Gets Axis Current Encoder count
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        double GetEncoderCount(int axis);
        /// <summary>
        /// Multiply the value of radians/second by this factor to get a 32-bit integer for the set speed used by the motor board.
        /// </summary>
        /// <returns>factor used to get the speed</returns>
        double[] GetFactorRadRateToInt();
        /// <summary>
        /// Inquire motor high speed ratio
        /// </summary>
        /// <returns>Ratio used to determine high speed</returns>
        long[] GetHighSpeedRatio();
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
        /// <returns></returns>
        long[] GetLowSpeedGotoMargin();
        /// <summary>
        /// e Gets the complete version string
        /// </summary>
        /// <returns></returns>
        string GetMotorCardVersion(int axis);
        /// <summary>
        /// Runs a motor test to see of each axis can move one step in GoTo mode
        /// </summary>
        /// <returns>Result of each axis test, 0=axis1 1=axis2</returns>
        bool[] GetOneStepIndicators();
        /// <summary>
        /// s Inquire PEC Period ":s(*1)", where *1: '1'= CH1, '2'= CH2, '3'= Both.
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        double GetPecPeriod(int axis);
        /// <summary>
        /// c Microsteps from target where the rampdown process begins
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
        /// <returns>Step Count</returns>
        long[] GetStepsPerRevolution();
        /// <summary>
        /// b Frequency of stepping timer
        /// </summary>
        /// <returns></returns>
        long[] GetStepTimeFreq();
        /// <summary>
        /// F Initial the target axis
        /// </summary>
        void InitializeAxes();
        /// <summary>
        /// Is mount in a connected serial state
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// q Is the mount collecting PPEC data
        /// </summary>
        bool IsPpecInTrainingOn { get; }
        /// <summary>
        /// q Does the mount have PPEC turned on
        /// </summary>
        bool IsPpecOn { get; }
        /// <summary>
        /// j Is axis at full stop
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsFullStop(int axis);
        /// <summary>
        /// j Is axis in highspeed mode
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
        bool IsSlewingFoward(int axis);
        /// <summary>
        /// f Is axis slewing in goto mode
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <returns></returns>
        bool IsSlewingTo(int axis);
        /// <summary>
        /// e Identify type of mount
        /// </summary>
        bool MountType { get; }
        /// <summary>
        /// e Identify board version
        /// </summary>
        bool MountVersion { get; }
        /// <summary>
        /// Turns PPEC off during movements and then back on for error correction moves
        /// </summary>
        /// <param name="on"></param>
        void SkySetAlternatingPpec(bool on);
        /// <summary>
        /// E Reset the position of an axis
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="position">degrees</param>
        void SetAxisPosition(int axis, double position);
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
        /// <param name="direction">0=forward (CW) right, 1=backaward (CCW) left, also based on obsertatory settings</param>
        void SetMotionMode(int axis, int func, int direction);
        /// <summary>
        /// W 2-3 Turn on off PPEC
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetPpec(int axis, bool on);
        /// <summary>
        /// W 0-1 Turn on off PPEC training
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="on"></param>
        void SetPpecTrain(int axis, bool on);
        /// <summary>
        /// I Set slewing rate, seems to relate to amount of skipped step counts.  
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="stepSpeed">StepSpeed = 1 motor step movement, higher counts means slower movements</param>
        void SetStepSpeed(int axis, long stepSpeed);
        /// <summary>
        /// S Set absolute goto target 
        /// </summary>
        /// <param name="axis">axis number 1 or 2</param>
        /// <param name="position"></param>
        void SetTargetPosition(int axis, double position);
    }
}
