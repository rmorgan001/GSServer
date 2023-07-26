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
using System;

namespace GS.SkyWatcher
{
    public interface ISkyCommand
    {
        long Id { get; }
        DateTime CreatedUtc { get; }
        bool Successful { get; set; }
        Exception Exception { get; set; }
        dynamic Result { get; }
        void Execute(SkyWatcher skyWatcher);
    }

    public class SkyAllowAdvancedCommandSet : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly bool _on;

        public SkyAllowAdvancedCommandSet(long id, bool on)
        {
            Id = id;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyAxisMoveSteps : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly long _steps;

        public SkyAxisMoveSteps(long id, AxisId axis, long steps)
        {
            Id = id;
            _axis = axis;
            _steps = steps;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyAxisPulse : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _guideRate;
        private readonly int _duration;
        private readonly int _backlashSteps;
        //private readonly double _declination;

        public SkyAxisPulse(long id, AxisId axis, double guideRate, int duration, int backlashSteps = 0)
        {
            Id = id;
            _axis = axis;
            _guideRate = guideRate;
            _duration = duration;
            _backlashSteps = backlashSteps;
            //_declination = declination;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                skyWatcher.AxisPulse(_axis, _guideRate, _duration, _backlashSteps);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyAxisStop : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;

        public SkyAxisStop(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyAxisStopInstant : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;

        public SkyAxisStopInstant(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyAxisSlew : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _rate;

        public SkyAxisSlew(long id, AxisId axis, double rate)
        {
            Id = id;
            _axis = axis;
            _rate = rate;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanAxisSlewsIndependent : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanAxisSlewsIndependent(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanAzEq : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanAzEq(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanDualEncoders : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanDualEncoders(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanHalfTrack : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanHalfTrack(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanHomeSensors : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanHomeSensors(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanPolarLed : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanPolarLed(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanPPec : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanPPec(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyCanWifi : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyCanWifi(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAdvancedCmdSupport : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetAdvancedCmdSupport(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAlternatingPPec : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetAlternatingPPec(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.AlternatingPPec;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetAngleToStep : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;
        private readonly double _angleInRad;

        public SkyGetAngleToStep(long id, AxisId axis, double angleInRad)
        {
            Id = id;
            _axis = axis;
            _angleInRad = angleInRad;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAxisPosition : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetAxisPosition(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAxisPositionCounter : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;
        private readonly bool _raw;

        public SkyGetAxisPositionCounter(long id, AxisId axis, bool raw = false)
        {
            Id = id;
            _axis = axis;
            _raw = raw;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAxisPositionDate : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetAxisPositionDate(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetRampDownRange : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetRampDownRange(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetCapabilities : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetCapabilities(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetCacheAxisStatus : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetCacheAxisStatus(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.GetCacheAxisStatus(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetDecPulseToGoTo : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetDecPulseToGoTo(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
        {
            try
            {
                Result = skyWatcher.DecPulseGoTo;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class SkyGetEncoderCount : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetEncoderCount(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetLastGoToTarget : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetLastGoToTarget(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetLastSlewSpeed : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetLastSlewSpeed(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetHomePosition : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetHomePosition(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetMotorCardVersion : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetMotorCardVersion(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetPecPeriod : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetPecPeriod(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetPositionsAndTime : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly bool _raw;

        public SkyGetPositionsAndTime(long id, bool raw)
        {
            Id = id;
            _raw = raw;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetPositionsInDegrees : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetPositionsInDegrees(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetSteps : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetSteps(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyUpdateSteps : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }

        public SkyUpdateSteps(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetSiderealRate : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyGetSiderealRate(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetStepToAngle : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;
        private readonly long _steps;

        public SkyGetStepToAngle(long id, AxisId axis, long steps)
        {
            Id = id;
            _axis = axis;
            _steps = steps;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetAxisPosition : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySetAxisPosition(long id, AxisId axis, double position)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _position = position;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetAxisPositionCounter : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly int _position;

        public SkySetAxisPositionCounter(long id, AxisId axis, int position)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _position = position;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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


    public class SkyInitializeAxes : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }

        public SkyInitializeAxes(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyMountType : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyMountType(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyMountVersion : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyMountVersion(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetMotionMode : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly int _func;
        private readonly int _direction;

        public SkySetMotionMode(long id, AxisId axis, int func, int direction)
        {
            Id = id;
            _axis = axis;
            _func = func;
            _direction = direction;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyStartMotion : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;

        public SkyStartMotion(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyAxisGoToTarget : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _targetPosition;

        public SkyAxisGoToTarget(long id, AxisId axis, double targetPosition)
        {
            Id = id;
            _axis = axis;
            _targetPosition = targetPosition;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetAlternatingPPec : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly bool _on;

        public SkySetAlternatingPPec(long id, bool on)
        {
            Id = id;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetDecPulseToGoTo : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly bool _on;

        public SkySetDecPulseToGoTo(long id, bool on)
        {
            Id = id;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetSouthernHemisphere : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result => null;
        private readonly bool _southernHemisphere;

        public SkySetSouthernHemisphere(long id, bool southernHemisphere)
        {
            Id = id;
            _southernHemisphere = southernHemisphere;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsAxisFullStop : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyIsAxisFullStop(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsConnected : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyIsConnected(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsHighSpeed : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyIsHighSpeed(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsPPecOn : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyIsPPecOn(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsPPecInTrainingOn : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyIsPPecInTrainingOn(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsSlewing : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyIsSlewing(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsSlewingForward : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyIsSlewingForward(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyIsSlewingTo : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;

        public SkyIsSlewingTo(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAxisVersions : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetAxisVersions(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetAxisStringVersions : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetAxisStringVersions(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetEncoder : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetEncoder(long id, AxisId axis, bool on)
        {
            Id = id;
            _axis = axis;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetMonitorPulse : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly bool _on;

        public SkySetMonitorPulse(long id, bool on)
        {
            Id = id;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetMinPulseDuration : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly int _duration;
        private readonly AxisId _axis;

        public SkySetMinPulseDuration(long id, AxisId axis, int duration)
        {
            Id = id;
            _axis = axis;
            _duration = duration;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetPPec : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetPPec(long id, AxisId axis, bool on)
        {
            Id = id;
            _axis = axis;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetPPecTrain : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetPPecTrain(long id, AxisId axis, bool on)
        {
            Id = id;
            _axis = axis;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetPolarLedLevel : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly int _level;

        public SkySetPolarLedLevel(long id, AxisId axis, int level)
        {
            Id = id;
            _axis = axis;
            _level = level;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetFullCurrent : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly bool _on;

        public SkySetFullCurrent(long id, AxisId axis, bool on)
        {
            Id = id;
            _axis = axis;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetGotoTargetIncrement : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly long _stepsCount;

        public SkySetGotoTargetIncrement(long id, AxisId axis, long stepsCount)
        {
            Id = id;
            _axis = axis;
            _stepsCount = stepsCount;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetStepSpeed : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly long _stepSpeed;

        public SkySetStepSpeed(long id, AxisId axis, long stepSpeed)
        {
            Id = id;
            _axis = axis;
            _stepSpeed = stepSpeed;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetBreakPointIncrement : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly long _stepsCount;

        public SkySetBreakPointIncrement(long id, AxisId axis, long stepsCount)
        {
            Id = id;
            _axis = axis;
            _stepsCount = stepsCount;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetSt4GuideRate : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly int _rate;

        public SkySetSt4GuideRate(long id, int rate)
        {
            Id = id;
            _rate = rate;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetSnapPort : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly bool _on;
        private readonly int _port;

        public SkySetSnapPort(long id, int port, bool on)
        {
            Id = id;
            _on = on;
            _port = port;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetTargetPosition : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySetTargetPosition(long id, AxisId axis, double position)
        {
            Id = id;
            _axis = axis;
            _position = position;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySetHomePositionIndex : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;

        public SkySetHomePositionIndex(long id, AxisId axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetStepsPerRevolution : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetStepsPerRevolution(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetStepTimeFreq : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetStepTimeFreq(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetHighSpeedRatio : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetHighSpeedRatio(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetLowSpeedGotoMargin : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetLowSpeedGotoMargin(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetFactorRadRateToInt : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetFactorRadRateToInt(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyGetFactorStepToRad : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public SkyGetFactorStepToRad(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkyLoadDefaultMountSettings : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }

        public SkyLoadDefaultMountSettings(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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

    public class SkySyncAxis : ISkyCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly AxisId _axis;
        private readonly double _position;

        public SkySyncAxis(long id, AxisId axis, double position)
        {
            Id = id;
            _axis = axis;
            _position = position;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            SkyQueue.AddCommand(this);
        }

        public void Execute(SkyWatcher skyWatcher)
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
