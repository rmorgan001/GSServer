/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Threading;

namespace GS.SkyWatcher
{
    public interface ISkyCommand
    {
        long Id { get; }
        DateTime CreatedUtc { get; }
        bool Successful { get; set; }
        Exception Exception { get; set; }
        dynamic Result { get; }
        ManualResetEventSlim CompletionEvent { get; }
        void Execute(SkyWatcher skyWatcher);
    }

    public abstract class SkyCommandBase : ISkyCommand
    {
        private readonly ManualResetEventSlim _completionEvent = new ManualResetEventSlim(false);

        protected SkyCommandBase(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
        }

        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; protected set; }
        public ManualResetEventSlim CompletionEvent => _completionEvent;

        public abstract void Execute(SkyWatcher skyWatcher);
    }

    public class SkyAllowAdvancedCommandSet : SkyCommandBase
    {
        private readonly bool _on;

        public SkyAllowAdvancedCommandSet(long id, bool on) : base(id)
        {
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AllowAdvancedCommandSet(_on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisMoveSteps : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly long _steps;

        public SkyAxisMoveSteps(long id, AxisId axis, long steps) : base(id)
        {
            _axis = axis;
            _steps = steps;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisMoveSteps(_axis, _steps);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisPulse : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _guideRate;
        private readonly int _duration;
        private readonly int _backlashSteps;
        private readonly CancellationToken _token;
        private readonly ManualResetEventSlim _pulseStartedEvent;
        //private readonly double _declination;

        public SkyAxisPulse(long id, AxisId axis, double guideRate, int duration, int backlashSteps, CancellationToken token, ManualResetEventSlim pulseStartedEvent = null) : base(id)
        {
            _axis = axis;
            _guideRate = guideRate;
            _duration = duration;
            _backlashSteps = backlashSteps;
            _token = token;
            _pulseStartedEvent = pulseStartedEvent;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisPulse(_axis, _guideRate, _duration, _backlashSteps, _token, _pulseStartedEvent);
                Successful = true;
            }
            catch (Exception e)
            {
                if (_pulseStartedEvent != null && !_pulseStartedEvent.IsSet) _pulseStartedEvent.Set();
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisStop : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyAxisStop(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisStop(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisStopInstant : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyAxisStopInstant(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisStopInstant(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisSlew : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _rate;

        public SkyAxisSlew(long id, AxisId axis, double rate) : base(id)
        {
            _axis = axis;
            _rate = rate;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisSlew(_axis, _rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanAxisSlewsIndependent : SkyCommandBase
    {
        public SkyCanAxisSlewsIndependent(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanAxisSlewsIndependent;
                Successful = true;

            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanAzEq : SkyCommandBase
    {
        public SkyCanAzEq(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanAzEq;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanDualEncoders : SkyCommandBase
    {
        public SkyCanDualEncoders(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanDualEncoders;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanHalfTrack : SkyCommandBase
    {
        public SkyCanHalfTrack(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanHalfTrack;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanHomeSensors : SkyCommandBase
    {
        public SkyCanHomeSensors(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanHomeSensors;
                Successful = true;

            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class SkyCanOneStepDec : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyCanOneStepDec(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.CanOneStepDec;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    //public class SkyCanOneStepRa : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyCanOneStepRa(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.CanOneStepRa;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class SkyCanPolarLed : SkyCommandBase
    {
        public SkyCanPolarLed(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanPolarLed;
                Successful = true;

            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanPPec : SkyCommandBase
    {
        public SkyCanPPec(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanPPec;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCanWifi : SkyCommandBase
    {
        public SkyCanWifi(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CanWifi;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyCmdToMount : SkyCommandBase
    {
        private readonly int _axis;
        private readonly string _cmd;
        private readonly string _cmdData;
        private readonly string _ignoreWarnings;

        public SkyCmdToMount(long id, int axis, string cmd, string cmdData, string ignoreWarnings) : base(id)
        {
            _axis = axis;
            _cmd = cmd;
            _cmdData = cmdData;
            _ignoreWarnings = ignoreWarnings;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.CmdToMount(_axis, _cmd, _cmdData, _ignoreWarnings);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAdvancedCmdSupport : SkyCommandBase
    {
        public SkyGetAdvancedCmdSupport(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAdvancedCmdSupport();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class SkyGetAlternatingPPec : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyGetAlternatingPPec(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.AlternatingPPec;
    //            Successful = true;
    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class SkyGetAngleToStep : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _angleInRad;

        public SkyGetAngleToStep(long id, AxisId axis, double angleInRad) : base(id)
        {
            _axis = axis;
            _angleInRad = angleInRad;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAngleToStep(_axis, _angleInRad);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAxisPosition : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetAxisPosition(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisPosition(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAxisPositionCounter : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _raw;

        public SkyGetAxisPositionCounter(long id, AxisId axis, bool raw = false) : base(id)
        {
            _axis = axis;
            _raw = raw;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisPositionCounter(_axis, _raw);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAxisPositionDate : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetAxisPositionDate(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisPositionDate(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetControllerVoltage : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetControllerVoltage(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetControllerVoltage(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetRampDownRange : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetRampDownRange(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetRampDownRange(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetCapabilities : SkyCommandBase
    {
        public SkyGetCapabilities(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetCapabilities();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class SkyGetCacheAxisStatus : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }
    //    private readonly AxisId _axis;

    //    public SkyGetCacheAxisStatus(long id, AxisId axis)
    //    {
    //        Id = id;
    //        _axis = axis;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.GetCacheAxisStatus(_axis);
    //            Successful = true;
    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    //public class SkyGetDecPulseToGoTo : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyGetDecPulseToGoTo(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.DecPulseGoTo;
    //            Successful = true;
    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class SkyGetEncoderCount : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetEncoderCount(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetEncoderCount(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetJ: SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _raw;

        public SkyGetJ(long id, AxisId axis, bool raw) : base(id)
        {
            _axis = axis;
            _raw = raw;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.Get_j(_axis, _raw);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetLastGoToTarget : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetLastGoToTarget(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetLastGoToTarget(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetLastSlewSpeed : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetLastSlewSpeed(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetLastSlewSpeed(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetHomePosition : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetHomePosition(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetHomePosition(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetMotorCardVersion : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetMotorCardVersion(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetMotorCardVersion(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class SkyGetOneStepIndicators : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyGetOneStepIndicators(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.GetOneStepIndicators();
    //            Successful = true;
    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class SkyGetPecPeriod : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetPecPeriod(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetPecPeriod(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetPositionsAndTime : SkyCommandBase
    {
        private readonly bool _raw;

        public SkyGetPositionsAndTime(long id, bool raw) : base(id)
        {
            _raw = raw;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetPositionsAndTime(_raw);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetPositionsInDegrees : SkyCommandBase
    {
        public SkyGetPositionsInDegrees(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetPositionsInDegrees();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetSteps : SkyCommandBase
    {
        public SkyGetSteps(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetSteps();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyUpdateSteps : SkyCommandBase
    {
        public SkyUpdateSteps(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.UpdateSteps();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetSiderealRate : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyGetSiderealRate(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetSiderealRate(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetStepToAngle : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly long _steps;

        public SkyGetStepToAngle(long id, AxisId axis, long steps) : base(id)
        {
            _axis = axis;
            _steps = steps;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetStepToAngle(_axis, _steps);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetAxisPosition : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySetAxisPosition(long id, AxisId axis, double position) : base(id)
        {
            _axis = axis;
            _position = position;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetAxisPosition(_axis, BasicMath.DegToRad(_position));
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetAxisPositionCounter : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly int _position;

        public SkySetAxisPositionCounter(long id, AxisId axis, int position) : base(id)
        {
            _axis = axis;
            _position = position;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetAxisPositionCounter(_axis, _position);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }


    public class SkyInitializeAxes : SkyCommandBase
    {
        public SkyInitializeAxes(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.InitializeAxes();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyMountType : SkyCommandBase
    {
        public SkyMountType(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.MountType;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyMountVersion : SkyCommandBase
    {
        public SkyMountVersion(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.MountVersion;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetMotionMode : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly int _func;
        private readonly int _direction;

        public SkySetMotionMode(long id, AxisId axis, int func, int direction) : base(id)
        {
            _axis = axis;
            _func = func;
            _direction = direction;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetMotionMode(_axis, _func, _direction);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyStartMotion : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyStartMotion(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.StartMotion(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisGoToTarget : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _targetPosition;

        public SkyAxisGoToTarget(long id, AxisId axis, double targetPosition) : base(id)
        {
            _axis = axis;
            _targetPosition = targetPosition;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisGoToTarget(_axis, BasicMath.DegToRad(_targetPosition));
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetAlternatingPPec : SkyCommandBase
    {
        private readonly bool _on;

        public SkySetAlternatingPPec(long id, bool on) : base(id)
        {
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AlternatingPPec = _on;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetDecPulseToGoTo : SkyCommandBase
    {
        private readonly bool _on;

        public SkySetDecPulseToGoTo(long id, bool on) : base(id)
        {
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.DecPulseGoTo = _on;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetSouthernHemisphere : SkyCommandBase
    {
        private readonly bool _southernHemisphere;

        public SkySetSouthernHemisphere(long id, bool southernHemisphere) : base(id)
        {
            _southernHemisphere = southernHemisphere;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SouthernHemisphere = _southernHemisphere;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsAxisFullStop : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyIsAxisFullStop(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStatus(_axis).FullStop;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsConnected : SkyCommandBase
    {
        public SkyIsConnected(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.IsConnected;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsHighSpeed : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyIsHighSpeed(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStatus(_axis).HighSpeed;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsPPecOn : SkyCommandBase
    {
        public SkyIsPPecOn(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.IsPPecOn;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsPPecInTrainingOn : SkyCommandBase
    {
        public SkyIsPPecInTrainingOn(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.IsPPecInTrainingOn;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsSlewing : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyIsSlewing(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStatus(_axis).Slewing;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsSlewingForward : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyIsSlewingForward(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStatus(_axis).SlewingForward;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyIsSlewingTo : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkyIsSlewingTo(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStatus(_axis).SlewingTo;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }    

    public class SkyGetAxisVersions : SkyCommandBase
    {
        public SkyGetAxisVersions(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisVersions();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAxisStringVersions : SkyCommandBase
    {
        public SkyGetAxisStringVersions(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetAxisStringVersions();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class SkyPulseDecRunning : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyPulseDecRunning(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.PulseDecRunning;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    //public class SkyPulseRaRunning : ISkyCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public SkyPulseRaRunning(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        SkyQueue.AddCommand(this);
    //    }

    //    public void Execute(SkyWatcher skyWatcher)
    //    {
    //        try
    //        {
    //            Result = skyWatcher.PulseRaRunning;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class SkySetEncoder : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetEncoder(long id, AxisId axis, bool on) : base(id)
        {
            _axis = axis;
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetEncoder(_axis, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetMonitorPulse : SkyCommandBase
    {
        private readonly bool _on;

        public SkySetMonitorPulse(long id, bool on) : base(id)
        {
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.MonitorPulse = _on;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetMinPulseDuration : SkyCommandBase
    {
        private readonly int _duration;
        private readonly AxisId _axis;

        public SkySetMinPulseDuration(long id, AxisId axis, int duration) : base(id)
        {
            _axis = axis;
            _duration = duration;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                if (_axis == AxisId.Axis1)
                { skyWatcher.MinPulseDurationRa = _duration; }
                else
                { skyWatcher.MinPulseDurationDec = _duration; }
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetPPec : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetPPec(long id, AxisId axis, bool on) : base(id)
        {
            _axis = axis;
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.SetPPec(_axis, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetPPecTrain : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetPPecTrain(long id, AxisId axis, bool on) : base(id)
        {
            _axis = axis;
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetPPecTrain(_axis, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetPolarLedLevel : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly int _level;

        public SkySetPolarLedLevel(long id, AxisId axis, int level) : base(id)
        {
            _axis = axis;
            _level = level;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetPolarLedLevel(_axis, _level);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetFullCurrent : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetFullCurrent(long id, AxisId axis, bool on) : base(id)
        {
            _axis = axis;
            _on = on;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetFullCurrent(_axis, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetGotoTargetIncrement : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly long _stepsCount;

        public SkySetGotoTargetIncrement(long id, AxisId axis, long stepsCount) : base(id)
        {
            _axis = axis;
            _stepsCount = stepsCount;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetGotoTargetIncrement(_axis, _stepsCount);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetStepSpeed : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly long _stepSpeed;

        public SkySetStepSpeed(long id, AxisId axis, long stepSpeed) : base(id)
        {
            _axis = axis;
            _stepSpeed = stepSpeed;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetStepSpeed(_axis, _stepSpeed);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetBreakPointIncrement : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly long _stepsCount;

        public SkySetBreakPointIncrement(long id, AxisId axis, long stepsCount) : base(id)
        {
            _axis = axis;
            _stepsCount = stepsCount;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetBreakPointIncrement(_axis, _stepsCount);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetSt4GuideRate : SkyCommandBase
    {
        private readonly int _rate;

        public SkySetSt4GuideRate(long id, int rate) : base(id)
        {
            _rate = rate;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetSt4GuideRate(_rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetSnapPort : SkyCommandBase
    {
        private readonly bool _on;
        private readonly int _port;

        public SkySetSnapPort(long id, int port, bool on) : base(id)
        {
            _on = on;
            _port = port;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.SetSnapPort(_port, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetTargetPosition : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySetTargetPosition(long id, AxisId axis, double position) : base(id)
        {
            _axis = axis;
            _position = position;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetTargetPosition(_axis, _position);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySetHomePositionIndex : SkyCommandBase
    {
        private readonly AxisId _axis;

        public SkySetHomePositionIndex(long id, AxisId axis) : base(id)
        {
            _axis = axis;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetHomePositionIndex(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetStepsPerRevolution : SkyCommandBase
    {
        public SkyGetStepsPerRevolution(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetStepsPerRevolution();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetStepTimeFreq : SkyCommandBase
    {
        public SkyGetStepTimeFreq(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetStepTimeFreq();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetHighSpeedRatio : SkyCommandBase
    {
        public SkyGetHighSpeedRatio(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetHighSpeedRatio();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetLowSpeedGotoMargin : SkyCommandBase
    {
        public SkyGetLowSpeedGotoMargin(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetLowSpeedGotoMargin();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetFactorRadRateToInt : SkyCommandBase
    {
        public SkyGetFactorRadRateToInt(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetFactorRadRateToInt();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetFactorStepToRad : SkyCommandBase
    {
        public SkyGetFactorStepToRad(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetFactorStepToRad();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyLoadDefaultMountSettings : SkyCommandBase
    {
        public SkyLoadDefaultMountSettings(long id) : base(id)
        {
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.LoadDefaultMountSettings();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkySyncAxis : SkyCommandBase
    {
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySyncAxis(long id, AxisId axis, double position) : base(id)
        {
            _axis = axis;
            _position = position;
            SkyQueue.AddCommand(this);
        }

        public override void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.SetAxisPosition(_axis, BasicMath.DegToRad(_position));
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }
}
