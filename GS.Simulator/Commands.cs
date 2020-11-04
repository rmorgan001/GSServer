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

namespace GS.Simulator
{
    public interface IMountCommand
    {
        long Id { get; }
        DateTime CreatedUtc { get; }
        bool Successful { get; set; }
        Exception Exception { get; set; }
        dynamic Result { get; }
        void Execute(Actions actions);
    }

    /// <summary>
    /// 
    /// </summary>
    public class CmdRate : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdRate(long id, Axis axis, double rate)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _rate = rate;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }
        public void Execute(Actions actions)
        {
            try
            {
                actions.Rate(_axis, _rate);
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
    public class CmdRateAxis : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdRateAxis(long id, Axis axis, double rate)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _rate = rate;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }
        public void Execute(Actions actions)
        {
            try
            {
                actions.RateAxis(_axis, _rate);
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
    public class CmdAxisSlew : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdAxisSlew(long id, Axis axis, double rate)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _rate = rate;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }
        public void Execute(Actions actions)
        {
            try
            {
                actions.AxisSlew(_axis, _rate);
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
    public class CmdAxesDegrees : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdAxesDegrees(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisSteps : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdAxisSteps(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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

    /// <summary>
    /// 
    /// </summary>
    public class CmdAxisStop : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;

        public CmdAxisStop(long id, Axis axis)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdHcSlew : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdHcSlew(long id, Axis axis, double rate)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _rate = rate;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }
        public void Execute(Actions actions)
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
    public class CmdAxisTracking : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _rate;
        public CmdAxisTracking(long id, Axis axis, double rate)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _rate = rate;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }
        public void Execute(Actions actions)
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
    public class CmdAxisGoToTarget : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _targetPosition;

        public CmdAxisGoToTarget(long id, Axis axis, double targetPosition)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _targetPosition = targetPosition;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdAxisToDegrees : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; }
        private readonly Axis _axis;
        private readonly double _degrees;

        public CmdAxisToDegrees(long id, Axis axis, double degrees)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _degrees = degrees;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdAxisStatus : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly Axis _axis;

        public CmdAxisStatus(long id, Axis axis)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdCapabilities : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdCapabilities(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
        {
            try
            {
                Result = actions.MountInfo;
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
    public class CmdAxisPulse : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly Axis _axis;
        private readonly double _guideRate;
        private readonly int _duration;
        //private readonly int _backlash;
        //private readonly double _declination;

        public CmdAxisPulse(long id, Axis axis, double guideRate, int duration)
        {
            Id = id;
            //_backlash = backlash;
            //_declination = declination;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            _guideRate = guideRate;
            _duration = duration;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
        {
            try
            {
                Result = actions.AxisPulse(_axis, _guideRate, _duration);
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
    public class CmdHomeSensor : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly Axis _axis;

        public CmdHomeSensor(long id, Axis axis)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            _axis = axis;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdHomeSensorReset : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly Axis _axis;

        public CmdHomeSensorReset(long id, Axis axis)
        {
            Id = id;
            _axis = axis;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    /// Get Mount Name
    /// </summary>
    public class CmdMountName : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdMountName(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    public class CmdMountVersion : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdMountVersion(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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

    public class CmdPulseDecRunning : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdPulseDecRunning(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
        {
            try
            {
                Result = actions.PulseDecRunning;
                Successful = true;

            }
            catch (Exception e)
            {
                Successful = false;
                Exception = e;
            }
        }
    }


    public class CmdPulseRaRunning : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdPulseRaRunning(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
        {
            try
            {
                Result = actions.PulseRaRunning;
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
    public class CmdSpr : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }

        public CmdSpr(long id)
        {
            Id = id;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
    /// Gets Steps Per Revolution
    /// </summary>
    public class CmdSetMonitorPulse : IMountCommand
    {
        public long Id { get; }
        public DateTime CreatedUtc { get; }
        public bool Successful { get; set; }
        public Exception Exception { get; set; }
        public dynamic Result { get; private set; }
        private readonly bool _on;

        public CmdSetMonitorPulse(long id, bool on)
        {
            Id = id;
            _on = on;
            CreatedUtc = Principles.HiResDateTime.UtcNow;
            Successful = false;
            Result = null;
            MountQueue.AddCommand(this);
        }

        public void Execute(Actions actions)
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
