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
using System.ComponentModel;
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

        public MountControlException(ErrorCode err) : base($"Mount: {err}")
        {
            ErrorCode = err;
        }

        public MountControlException(ErrorCode err, string message) : base($"Mount: {err}, {message}")
        {
            ErrorCode = err;
        }

        public MountControlException(ErrorCode err, string message, Exception inner) : base($"Mount: {err}, {message}", inner)
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
        //public const double SiderealRate in rad = 2 * Math.PI / 360;
        public const double SiderealRate = 2 * Math.PI / 86164.09065;
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
            source %= (Math.PI * 2);
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
        /// 4. NotInitialized
        /// </summary>

        public bool FullStop;
        public bool Slewing;
        public bool SlewingTo;
        public bool SlewingForward;
        public bool HighSpeed;
        public bool Initialized;
        public bool TrajectoryMode; 
        public string StepSpeed;
        public string Response;

        public void SetFullStop()
        {
            FullStop = true;
            SlewingTo = Slewing = false;
            StepSpeed = "*";
        }
        public void SetSlewing(bool forward, bool highSpeed)
        {
            FullStop = SlewingTo = false;
            Slewing = true;

            SlewingForward = forward;
            HighSpeed = highSpeed;
        }
        public void SetSlewingTo(bool forward, bool highSpeed)
        {
            FullStop = Slewing = false;
            SlewingTo = true;

            SlewingForward = forward;
            HighSpeed = highSpeed;
        }


        //// Mask for axis status
        //public const long AXIS_FULL_STOPPED = 0x0001;		// The axis is at a complete stop
        //public const long AXIS_SLEWING = 0x0002;			// The axis is running at a constant speed
        //public const long AXIS_SLEWING_TO = 0x0004;		// The axis is in the process of traveling to the specified target position
        //public const long AXIS_SLEWING_FORWARD = 0x0008;	// The shaft runs forward
        //public const long AXIS_SLEWING_HIGHSPEED = 0x0010;// The axis is running at high speed
        //public const long AXIS_NOT_INITIALIZED = 0x0020;  // MC controller has not been initialized
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
        ErrNoResponseAxis1 = 100,	    // No response from axis1
        ErrNoResponseAxis2 = 101,	    // The secondary axis of the telescope does not respond
        ErrAxisBusy = 102,			    // This operation cannot be performed temporarily
        ErrMaxPitch = 103,              // Target position elevation angle is too high
        ErrMinPitch = 104,			    // Target position elevation angle is too low
        ErrUserInterrupt = 105,	        // User forced termination
        ErrAlignFailed = 200,		    // Calibration telescope failed
        ErrUnimplemented = 300,           // Unimplemented method
        ErrWrongAlignmentData = 400,	// The alignment data is incorrect.
        ErrQueueFailed = 500,             // Queue timeout or not running
        ErrTooManyRetries = 501         // retries hit max limit
    };
    public enum Mountid
    {
        // Telescope ID, they must be started from 0 and coded continuously.
        IdCelestronAz = 0,              // Celestron Alt/Az Mount
        IdCelestronEq = 1,              // Celestron EQ Mount
        IdSkywatcherAz = 2,             // Skywatcher Alt/Az Mount
        IdSkywatcherEq = 3,             // Skywatcher EQ Mount
        IdOrionEqg = 4,                 // Orion EQ Mount
        IdOrionTeletrack = 5,           // Orion TeleTrack Mount
        IdEqEmulator = 6,               // EQ Mount Emulator
        IdAzEmulator = 7,               // Alt/Az Mount Emulator
        IdNexstargt80 = 8,              // NexStarGT-80 mount
        IdNexstargt114 = 9,             // NexStarGT-114 mount
        IdStarseeker80 = 10,                // NexStarGT-80 mount
        IdStarseeker114 = 11,			// NexStarGT-114 mount
    }
    public enum McModel
    {
        [Description("EQ6(EQ6 MC)")] EQ6 = 0,
        [Description("HEQ5(HEQ5 MC)")] HEQ5 = 1,
        [Description("EQ5 Goto mount(MC002)")] EQ5 = 2,
        [Description("EQ3 Goto mount(MC002)")] EQ3 = 3,
        [Description("EQ8(MC009)")] EQ8 = 4,
        [Description("AZ-EQ6 and EQ6-R(MC007)")] AzEQ6EQ6R = 5,
        [Description("AZ-EQ5 (MC0011)")] AzEQ5 = 6,
        [Description("Star Adventurer(MC012)")] StarAdventurer = 7,
        [Description("Star Adventurer Mini(MC013)")] StarAdventurerMini = 8,
        [Description("Avant(MC018)")] AvAnt = 11,
        [Description("Star Adventurer GTi(MC021)")] StarAdventurerGTi = 12,
        [Description("EQM35(MC002)")] EQM35 = 26,
        [Description("EQ8-R(MC015)")] EQ8R = 32,
        [Description("EQ8(MC015)")] EQ8a = 33,
        [Description("AZ-EQ6(MC015)")] AzEQ6 = 34,
        [Description("EQ6-R(MC015)")] EQ6R = 35,
        [Description("NEQ6 PRO(MC015)")] NEQ6PRO = 36,
        [Description("CQ350(MC015)")] CQ350 = 37,
        [Description("DOB 18(MC015)")] DOB18 = 183,
        [Description("EQ3(MC019)")] EQ_3 = 48,
        [Description("EQ5(MC019)")] EQ_5 = 49,
        [Description("EQM35(MC019)")] EQM_35 = 50,
        [Description("Prototyping(MC019)")] Prototyping = 55,
        [Description("HEQ5(MC020)")] HEQ5a = 56,
        [Description("SynTrek hand control")] SynTrekHC = 127,
        [Description("80GT(MC001)")] a80GT = 128,
        [Description("Multi-Function mount Bushnell(MC001)")] MultiFunctionBushnell = 129,
        [Description("114GT(MC001)")] a114GT = 130,
        [Description("80GT(MC001)")] b80GT = 131,
        [Description("Multi-Function mount Merlin(MC001)")] MultiFunctionMerlin = 132,
        [Description("114GT(MC001)")] b114GT = 133,
        [Description("80GTSLT (MC001)")] c80GtSLT = 134,
        [Description("114GT SynScan AZ 360(MC001)")] c114GTSlt = 135,
        [Description("Dob Tracking 8 to 12(MC003)")] DobTracking8to12 = 144,
        [Description("Dob Goto 8 to 12(MC003)")] DobGoto8to12 = 145,
        [Description("Dob Goto 8 to 12 Clutchless)(MC004)")] DobGoto8to12cl = 152,
        [Description("Dob Goto 14 to 16(MC004)")] DobGoto14to16 = 153,
        [Description("Dob Goto 8 to 12 Clutch(MC003)")] DobGoto8to12c = 154,
        [Description("AllView(MC005)")] AllView = 160,
        [Description("DobMini Tracking(MC006)")] DobMini = 161,
        [Description("Star Discovery(MC006)")] StarDiscovery = 162,
        [Description("Dob 18 StarGate(MC009)")] Dob18StarGate = 164,
        [Description("AZ-GTi(MC014)")] AZGTi = 165,
        [Description("Discovery(MC014)")] DiscoverySolar = 166,
        [Description("Merlin(MC014)")] Merlin = 167,
        [Description("SynScan AZ 130GT(MC014)")] SynScanAZ130GT = 168,
        [Description("DOB 18/20(MC014)")] DOB18_20 = 169,
        [Description("DOB 8/10/12 Clutch(MC014)")] DOB8_10_12c = 170,
        [Description("DOB 8/10/12 Clutchless(MC014)")] DOB8_10_12cl = 171,
        [Description("DOB 14/16 Clutch(MC014)")] DOB14_16 = 172,
        [Description("SynScan 80GT(MC014)")] SynScan80GT = 173,
        [Description("Dob14 StarGate(MC007)")] Dob14StarGate = 182,
        [Description("Undefined")] Undefined = 999,

    }
}
