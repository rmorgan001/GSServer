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

namespace GS.Server.SkyTelescope
{
    public enum HCMode
    {
        Axes,
        Guiding
    }

    public enum SlewType
    {
        SlewNone,
        SlewSettle,
        SlewMoveAxis,
        SlewRaDec,
        SlewAltAz,
        SlewPark,
        SlewHome,
        SlewHandpad,
        SlewComplete
    }

    [Flags]
    public enum SlewSpeed
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8
    }

    public enum SlewDirection
    {
        SlewNorth,
        SlewSouth,
        SlewEast,
        SlewWest,
        SlewUp,
        SlewDown,
        SlewLeft,
        SlewRight,
        SlewNoneRa,
        SlewNoneDec
    }

    public enum PointingState
    {
        Normal,
        ThroughThePole
    }

    public enum TrackingMode
    {
        Off,
        AltAz,
        EqN,
        EqS
    }

    public enum MountTaskName
    {
        AllowAdvancedCommandSet,
        AlternatingPpec,
        CanPpec,
        CanPolarLed,
        CanHomeSensor,
        DecPulseToGoTo,
        Encoders,
        FullCurrent,
        LoadDefaults,
        StopAxes,
        InstantStopAxes,
        MinPulseRa,
        MinPulseDec,
        SetSouthernHemisphere,
        SyncAxes,
        SyncTarget,
        SyncAltAz,
        MonitorPulse,
        Pec,
        PecTraining,
        PolarLedLevel,
        Capabilities,
        SetHomePositions,
        SetSt4Guiderate,
        SetSnapPort1,
        SetSnapPort2,
        MountName,
        GetAxisVersions,
        GetAxisStrVersions,
        MountVersion,
        StepsPerRevolution,
        StepsWormPerRevolution,
        StepTimeFreq,
        GetFactorStep,
        CanAdvancedCmdSupport
    }

    public enum MountType
    {
        Simulator,
        SkyWatcher
    }

    public enum ErrorCode
    {
        ErrMount = 1,
        ErrExecutingCommand = 2,
        ErrUnableToDeqeue = 3,
        ErrSerialFailed = 4
    };

    public enum MountAxis
    {
        Ra,
        Dec
    }

    public enum PecMode
    {
        PecWorm = 0,
        Pec360 = 1
    }

    public enum PecFileType
    {
        GSPecWorm = 0,
        GSPec360 = 1,
        GSPecDebug = 2
    }

    public enum ReSyncMode
    {
        Home = 0,
        Park = 1
    }

    public enum BeepType
    {
        WinDefault,
        Default,
        SlewComplete
    }

    public enum FrontGraphic
    {
        None,
        AltAz,
        RaDec,
        Model3D
    }

    public enum ConnectType
    {
        None,
        Com,
        Wifi
    }

}
