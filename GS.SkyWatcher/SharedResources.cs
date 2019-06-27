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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GS.SkyWatcher
{
    [Serializable]
    public class MountControlException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public MountControlException()
        {
        }

        public MountControlException(ErrorCode err): base($"Mount: {err}")
        {
            ErrorCode = err;
        }

        public MountControlException(ErrorCode err, string message) : base($"Mount: {err}, {message}")
        {
            ErrorCode = err;
        }

        public MountControlException(ErrorCode err, string message, Exception inner): base($"Mount: {err}, {message}", inner)
        {
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        // Constructor should be protected for unsealed classes, private for sealed classes.
        // (The Serializer invokes this constructor through reflection, so it can be private)
        protected MountControlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Enum.TryParse("err", out ErrorCode err);
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("err", ErrorCode.ToString());
            // MUST call through to the base class to let it save its own state
            base.GetObjectData(info, context);
        }
    }

    internal static class Constant
    {
        //public const double Siderealrate in rad = 2 * Math.PI / 360;
        public const double Siderealrate = 2 * Math.PI / 86164.09065;
    }

    internal static class BasicMath
    {
        //public const double Rad1 = Math.PI / 180;
        public static double AngleDistance(double ang1, double ang2)
        {
            ang1 = UniformAngle(ang1);
            ang2 = UniformAngle(ang2);
            var d = ang2 - ang1;
            return UniformAngle(d);
        }

        private static double UniformAngle(double source)
        {
            source = source % (Math.PI * 2);
            if (source > Math.PI)
                return source - 2 * Math.PI;
            if (source < -Math.PI)
                return source + 2 * Math.PI;
            return source;
        }

        internal static double DegToRad(double degree) { return (degree / 180.0 * Math.PI); }
        internal static double RadToDeg(double rad) { return (rad / Math.PI * 180.0); }
        internal static double RadToMin(double rad) { return (rad / Math.PI * 180.0 * 60.0); }
        internal static double RadToSec(double rad) { return (rad / Math.PI * 180.0 * 60.0 * 60.0); }
        internal static double SecToRad(double sec) { return (sec * Math.PI / 180.0 * 60.0 * 60.0); }
        internal static double MilToRad(int millisecond) { return SecToRad(Convert.ToDouble(millisecond / 1000)); }
    }

    public struct AxisStatus
    {
        /// <summary>
        /// 4 different state
        /// 1. FullStop
        /// 2. Slewing
        /// 3. SlewingTo
        /// 4. Notinitialized
        /// </summary>

        public bool FullStop;
        public bool Slewing;
        public bool SlewingTo;
        public bool SlewingForward;
        public bool HighSpeed;
        public bool NotInitialized;
        public string StepSpeed;
        
        public void SetFullStop()
        {
            FullStop = true;
            SlewingTo = Slewing = false;
            StepSpeed = "*";
        }
        public void SetSlewing(bool forward, bool highspeed)
        {
            FullStop = SlewingTo = false;
            Slewing = true;

            SlewingForward = forward;
            HighSpeed = highspeed;
        }
        public void SetSlewingTo(bool forward, bool highspeed)
        {
            FullStop = Slewing = false;
            SlewingTo = true;

            SlewingForward = forward;
            HighSpeed = highspeed;
        }

        
        //// Mask for axis status
        //public const long AXIS_FULL_STOPPED = 0x0001;		// 該軸處於完全停止狀態
        //public const long AXIS_SLEWING = 0x0002;			// 該軸處於恒速運行狀態
        //public const long AXIS_SLEWING_TO = 0x0004;		    // 該軸處於運行到指定目標位置的過程中
        //public const long AXIS_SLEWING_FORWARD = 0x0008;	// 該軸正向運轉
        //public const long AXIS_SLEWING_HIGHSPEED = 0x0010;	// 該軸處於高速運行狀態
        //public const long AXIS_NOT_INITIALIZED = 0x0020;    // MC控制器尚未初始化, axis is not initialized.
    }

    public enum AxisId { Axis1 = 0, Axis2 = 1 };

    public enum ErrorCode
    {
        ErrInvalidId = 1,			    // Invalid mount ID
        ErrAlreadyConnected = 2,	    // Already connected to another mount ID
        ErrNotConnected = 3,		    // Telescope not connected.
        ErrInvalidData = 4, 		    // Invalid data, over range etc
        ErrSerialPortBusy = 5, 	        // Serial port is busy.
        ErrMountNotFound = 6,           // Serial test command did not get correct response
        ErrNoresponseAxis1 = 100,	    // No response from axis1
        ErrNoresponseAxis2 = 101,	    // The secondary axis of the telescope does not respond
        ErrAxisBusy = 102,			    // This operation cannot be performed temporarily
        ErrMaxPitch = 103,              // Target position elevation angle is too high
        ErrMinPitch = 104,			    // Target position elevation angle is too low
        ErrUserInterrupt = 105,	        // User forced termination
        ErrAlignFailed = 200,		    // Calibration telescope failed
        ErrUnimplement = 300,           // Unimplemented method
        ErrWrongAlignmentData = 400,	// The alignment data is incorect.
        ErrQueueFailed=500,             // Queue timeout or not running
        ErrTooManyRetries = 501         // retries hit max limit
    };

    public enum Mountid
    {
         // Telescope ID, they must be started from 0 and coded continuously.
         IdCelestronAz = 0,				// Celestron Alt/Az Mount
         IdCelestronEq = 1,				// Celestron EQ Mount
         IdSkywatcherAz = 2,			    // Skywatcher Alt/Az Mount
         IdSkywatcherEq = 3,			    // Skywatcher EQ Mount
         IdOrionEqg = 4,				    // Orion EQ Mount
         IdOrionTeletrack = 5,			// Orion TeleTrack Mount
         IdEqEmulator = 6,				// EQ Mount Emulator
         IdAzEmulator = 7,				// Alt/Az Mount Emulator
         IdNexstargt80 = 8,				// NexStarGT-80 mount
         IdNexstargt114 = 9,				// NexStarGT-114 mount
         IdStarseeker80 = 10,			    // NexStarGT-80 mount
         IdStarseeker114 = 11,			// NexStarGT-114 mount
    }
}
