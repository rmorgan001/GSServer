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

namespace GS.Simulator
{
    public interface IMountCommand
    {
        long Id { get; }
        DateTime CreatedUtc { get; }
        bool Successful { get; set; }
        Exception Exception { get; set; }
        dynamic Result { get; }
        ManualResetEventSlim CompletionEvent { get; }
        void Execute(Actions actions);
    }

    /// <summary>
    /// Base class for mount commands providing synchronization infrastructure
    /// </summary>
    public abstract class MountCommandBase : IMountCommand
    {
        private readonly ManualResetEventSlim _completionEvent = new ManualResetEventSlim(false);

        protected MountCommandBase(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
        }

        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public virtual dynamic Result { get; protected set; }
        public ManualResetEventSlim CompletionEvent => _completionEvent;

        public abstract void Execute(Actions actions);
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdRaDecRate : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdRaDecRate(long id, Axis axis, double rate) : base(id)
        {
            _axis = axis;
            _rate = rate;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.RaDecRate(_axis, _rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdMoveAxisRate : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdMoveAxisRate(long id, Axis axis, double rate) : base(id)
        {
            _axis = axis;
            _rate = rate;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.MoveAxisRate(_axis, _rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class CmdGotoSpeed : MountCommandBase
    {
        private readonly int _rate;
        public CmdGotoSpeed(long id, int rate) : base(id)
        {
            _rate = rate;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.GotoRate(_rate);
                Result = _rate;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class CmdAxisSlew : IMountCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; }
    //    private readonly Axis _axis;
    //    private readonly double _rate;
    //    public CmdAxisSlew(long id, Axis axis, double rate)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        _axis = axis;
    //        _rate = rate;
    //        Successful = false;
    //        Result = null;
    //        MountQueue.AddCommand(this);
    //    }
    //    public void Execute(Actions actions)
    //    {
    //        try
    //        {
    //            actions.AxisSlew(_axis, _rate);
    //            Successful = true;
    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxesDegrees : MountCommandBase
    {
        public CmdAxesDegrees(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.AxesDegrees();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class CmdAxesSteps : MountCommandBase
    {
        public CmdAxesSteps(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxesSteps();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisSteps : MountCommandBase
    {
        public CmdAxisSteps(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.AxisSteps();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    public class AxisStepsDt : MountCommandBase
    {
        private readonly Axis _axis;

        public AxisStepsDt(long id, Axis axis) : base(id)
        {
            _axis = axis;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.AxisStepsDt(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisStop : MountCommandBase
    {
        private readonly Axis _axis;

        public CmdAxisStop(long id, Axis axis) : base(id)
        {
            _axis = axis;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxisStop(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdHcSlew : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdHcSlew(long id, Axis axis, double rate) : base(id)
        {
            _axis = axis;
            _rate = rate;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.HcSlew(_axis, _rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisTracking : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdAxisTracking(long id, Axis axis, double rate) : base(id)
        {
            _axis = axis;
            _rate = rate;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxisTracking(_axis, _rate);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisGoToTarget : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _targetPosition;

        public CmdAxisGoToTarget(long id, Axis axis, double targetPosition) : base(id)
        {
            _axis = axis;
            _targetPosition = targetPosition;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxisGoToTarget(_axis, _targetPosition);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisToDegrees : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _degrees;

        public CmdAxisToDegrees(long id, Axis axis, double degrees) : base(id)
        {
            _axis = axis;
            _degrees = degrees;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxisToDegrees(_axis, _degrees);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisStatus : MountCommandBase
    {
        private readonly Axis _axis;

        public CmdAxisStatus(long id, Axis axis) : base(id)
        {
            _axis = axis;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.AxisStatus(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetHomeSensorCapability : MountCommandBase
    {
        public GetHomeSensorCapability(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.MountInfo.CanHomeSensors;
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisPulse : MountCommandBase
    {
        private readonly Axis _axis;
        private readonly double _guideRate;
        private readonly int _duration;
        private readonly CancellationToken _token;

        public CmdAxisPulse(long id, Axis axis, double guideRate, int duration, CancellationToken token) : base(id)
        {
            _axis = axis;
            _guideRate = guideRate;
            _duration = duration;
            _token = token;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.AxisPulse(_axis, _guideRate, _duration, _token);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }


    /// <summary>
    /// Get Home Sensor Status
    /// </summary>
    public class CmdHomeSensor : MountCommandBase
    {
        private readonly Axis _axis;

        public CmdHomeSensor(long id, Axis axis) : base(id)
        {
            _axis = axis;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.HomeSensor(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// Reset home sensor
    /// </summary>
    public class CmdHomeSensorReset : MountCommandBase
    {
        private readonly Axis _axis;

        public CmdHomeSensorReset(long id, Axis axis) : base(id)
        {
            _axis = axis;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.HomeSensorReset(_axis);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdFactorSteps : MountCommandBase
    {
        public CmdFactorSteps(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.FactorSteps();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }


    /// <summary>
    /// Get Mount Name
    /// </summary>
    public class CmdMountName : MountCommandBase
    {
        public CmdMountName(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.MountName();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// Get Mount Version
    /// </summary>
    public class CmdMountVersion : MountCommandBase
    {
        public CmdMountVersion(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.MountVersion();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    //public class CmdPulseDecRunning : IMountCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public CmdPulseDecRunning(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        MountQueue.AddCommand(this);
    //    }

    //    public void Execute(Actions actions)
    //    {
    //        try
    //        {
    //            Result = actions.PulseDecRunning;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}
    
    //public class CmdPulseRaRunning : IMountCommand
    //{
    //    public long Id { get; }
    //    public DateTime CreatedUtc { get; }
    //    public bool Successful { get; set; }
    //    public Exception Exception { get; set; }
    //    public dynamic Result { get; private set; }

    //    public CmdPulseRaRunning(long id)
    //    {
    //        Id = id;
    //        CreatedUtc = Principles.HiResDateTime.UtcNow;
    //        Successful = false;
    //        MountQueue.AddCommand(this);
    //    }

    //    public void Execute(Actions actions)
    //    {
    //        try
    //        {
    //            Result = actions.PulseRaRunning;
    //            Successful = true;

    //        }
    //        catch (Exception e)
    //        {
    //            Successful = false;
    //            Exception = e;
    //        }
    //    }
    //}

    public class CmdSnapPort : MountCommandBase
    {
        private readonly int _port;
        private readonly bool _on;
        public CmdSnapPort(long id, int port, bool on) : base(id)
        {
            _port = port;
            _on = on;
            MountQueue.AddCommand(this);
        }
        public override void Execute(Actions actions)
        {
            try
            {
                actions.SnapPort(_port, _on);
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// Gets Steps Per Revolution
    /// </summary>
    public class CmdSpr : MountCommandBase
    {
        public CmdSpr(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.Spr();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// Gets Steps Per Worm Revolution
    /// </summary>
    public class CmdSpw : MountCommandBase
    {
        public CmdSpw(long id) : base(id)
        {
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                Result = actions.Spw();
                Successful = true;
            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }

    /// <summary>
    /// Gets Steps Per Revolution
    /// </summary>
    public class CmdSetMonitorPulse : MountCommandBase
    {
        private readonly bool _on;

        public CmdSetMonitorPulse(long id, bool on) : base(id)
        {
            _on = on;
            MountQueue.AddCommand(this);
        }

        public override void Execute(Actions actions)
        {
            try
            {
                actions.MonitorPulse = _on;
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
