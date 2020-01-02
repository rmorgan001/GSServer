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
        SlewNone
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
        AlternatingPpec,
        CanPpec,
        CanHomeSensor,
        DecPulseToGoTo,
        Encoders,
        FullCurrent,
        GetOneStepIndicators,
        LoadDefaults,
        StopAxes,
        InitialiseAxes,
        InstantStopAxes,
        SetSouthernHemisphere,
        SyncAxes,
        SyncTarget,
        SyncAltAz,
        MonitorPulse,
        Pec,
        PecTraining,
        Capabilities,
        SetHomePositions,
        SetSt4Guiderate,
        SkySetSnapPort,
        MountName,
        GetAxisVersions,
        GetAxisStrVersions,
        MountVersion,
        StepsPerRevolution
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

}
