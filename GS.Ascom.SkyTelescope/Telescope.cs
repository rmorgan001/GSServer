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
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace ASCOM.GS.Sky.Telescope
{

    [Guid("DFAC33EA-34DB-460D-810F-5EBFA40FB478")]
    [ServedClassName("ASCOM GS Sky Telescope")]
    [ProgId("ASCOM.GS.Sky.Telescope")]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class Telescope : ObjectBase, ITelescopeV3, IDisposable
    {
        // Driver private data (rate collections)
        private AxisRates[] _mAxisRates;
        private TrackingRates _mTrackingRates;
        private TrackingRatesSimple _mTrackingRatesSimple;
        private Util _util;
        private CommandStrings _mCommandStrings;
        private readonly long _objectId;

        /// <summary>
        /// Constructor - Must be public for COM registration!
        /// </summary>
        public Telescope()
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
                MonitorLog.LogToMonitor(monitorItem);

                var driverId = Marshal.GenerateProgIdForType(GetType());
                _mAxisRates = new AxisRates[3];
                _mAxisRates[0] = new AxisRates(TelescopeAxes.axisPrimary);
                _mAxisRates[1] = new AxisRates(TelescopeAxes.axisSecondary);
                _mAxisRates[2] = new AxisRates(TelescopeAxes.axisTertiary);
                _mTrackingRates = new TrackingRates();
                _mTrackingRatesSimple = new TrackingRatesSimple();
                _util = new Util();
                // get a unique instance id
                _objectId = SkySystem.GetId();

                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Instance ID|{_objectId}|Driver ID|{driverId}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Error|{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region Public com Interface ITelescope Implementaion

        public string Action(string ActionName, string ActionParameters)
        {
            ActionName = ActionName?.Trim();
            ActionParameters = ActionParameters?.Trim();

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Driver,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $" ActionName:{ActionName}, ActionParameters:'{ActionParameters}'"
            };
            MonitorLog.LogToMonitor(monitorItem);

            switch (ActionName)
            {
                // ReSharper disable once StringLiteralTypo
                case string str when str.Equals("telescope:setparkposition", StringComparison.InvariantCultureIgnoreCase):
                    if (SkyServer.IsMountRunning == false) { throw new NotConnectedException("Mount Not Connected"); }
                    var found = SkySettings.ParkPositions.Find(x => string.Equals(x.Name, ActionParameters, StringComparison.InvariantCultureIgnoreCase));
                    if (found == null)
                    {
                        var _parkPositions = SkySettings.ParkPositions.OrderBy(ParkPosition => ParkPosition.Name).ToList();
                        var output = JsonConvert.SerializeObject(_parkPositions);
                        throw new Exception($"Param Not Found:'{ActionParameters}', {output}");
                    }
                    SkyServer.ParkSelected = found;
                    return found.Name;
                default:
                    throw new ActionNotImplementedException($"Not Found:'{ActionName}'");
            }
        }

        /// <summary>
        /// Gets the supported actions.
        /// </summary>
        public ArrayList SupportedActions
        {
            // no supported actions, return empty array
            get
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
                MonitorLog.LogToMonitor(monitorItem);

                // ReSharper disable once StringLiteralTypo
                var sa = new ArrayList { @"Telescope:SetParkPosition" };

                return sa;
            }
        }

        public void AbortSlew()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckParked("AbortSlew");
            SkyServer.AbortSlew(true);
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                CheckCapability(SkySettings.CanAlignMode, "AlignmentMode");
                var r = SkySettings.AlignmentMode;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                switch (r)
                {
                    case AlignmentModes.algAltAz:
                        return AlignmentModes.algAltAz;
                    case AlignmentModes.algGermanPolar:
                        return AlignmentModes.algGermanPolar;
                    case AlignmentModes.algPolar:
                        return AlignmentModes.algPolar;
                    default:
                        return AlignmentModes.algGermanPolar;
                }
            }
        }

        public double Altitude
        {
            get
            {
                CheckCapability(SkySettings.CanAltAz, "Altitude", false);
                var r = SkyServer.Altitude;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public double ApertureArea
        {
            get
            {
                CheckCapability(SkySettings.CanOptics, "ApertureArea", false);
                var r = SkySettings.ApertureArea;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                CheckCapability(SkySettings.CanOptics, "ApertureDiameter", false);
                var r = SkySettings.ApertureDiameter;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool AtHome
        {
            get
            {
                CheckVersionOne("AtHome", false);
                var r = SkyServer.AtHome;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool AtPark
        {
            get
            {
                CheckVersionOne("AtPark", false);
                var r = SkySettings.AtPark;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"   {Axis}" };
            MonitorLog.LogToMonitor(monitorItem);

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary:
                    return new AxisRates(TelescopeAxes.axisPrimary);
                case TelescopeAxes.axisSecondary:
                    return new AxisRates(TelescopeAxes.axisSecondary);
                case TelescopeAxes.axisTertiary:
                    return new AxisRates(TelescopeAxes.axisTertiary);
                default:
                    return null;
            }
        }

        public double Azimuth
        {
            get
            {
                CheckCapability(SkySettings.CanAltAz, "Azimuth", false);
                var r = SkyServer.Azimuth;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanFindHome
        {
            get
            {
                var r = SkySettings.CanFindHome;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            CheckVersionOne("CanMoveAxis");
            var r = SkyServer.CanMoveAxis(Axis);

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
            MonitorLog.LogToMonitor(monitorItem);

            return r;
        }

        public bool CanPark
        {
            get
            {
                var r = SkySettings.CanPark;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                var r = SkySettings.CanPulseGuide;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                CheckVersionOne("CanSetDeclinationRate", false);
                var r = SkySettings.CanSetDeclinationRate;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                var r = SkySettings.CanSetGuideRates;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetPark
        {
            get
            {
                var r = SkySettings.CanSetPark;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                CheckVersionOne("CanSetPierSide", false);
                var r = SkySettings.CanSetPierSide;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                CheckVersionOne("CanSetRightAscensionRate", false);
                var r = SkySettings.CanSetRightAscensionRate;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                var r = SkySettings.CanSetTracking;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSlew
        {
            get
            {
                var r = SkySettings.CanSlew;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                CheckVersionOne("CanSlewAltAz", false);
                var r = SkySettings.CanSlewAltAz;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                CheckVersionOne("CanSlewAltAzAsync", false);
                var r = SkySettings.CanSlewAltAzAsync;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                var r = SkySettings.CanSlewAsync;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSync
        {
            get
            {
                var r = SkySettings.CanSync;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                var r = SkySettings.CanSyncAltAz;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public bool CanUnpark
        {
            get
            {
                var r = SkySettings.CanUnPark;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public void CommandBlind(string Command, bool Raw)
        {
            throw new MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string Command, bool Raw)
        {
            throw new MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string Command, bool Raw)
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{Command},{Raw}") };
                MonitorLog.LogToMonitor(monitorItem);

                if (string.IsNullOrWhiteSpace(Command)) { throw new MethodNotImplementedException("CommandString"); }

                if (_mCommandStrings == null) { _mCommandStrings = new CommandStrings(); }
                return CommandStrings.ProcessCommand(Command, Raw);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Driver,
                    Type = MonitorType.Warning,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = FormattableString.Invariant($"{ex.Message},{ex.StackTrace}")
                };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }

        public bool Connected
        {
            get
            {
                var r = SkySystem.Connected;
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);
                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkySystem.SetConnected(_objectId, value);
            }
        }

        public double Declination
        {
            get
            {
                CheckCapability(SkySettings.CanEquatorial, "Declination", false);
                var dec = SkyServer.DeclinationXForm;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(dec, "° ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return dec;
            }
        }

        /// <summary>
        /// The declination tracking rate (arc seconds per second, default = 0.0) 
        /// </summary>
        public double DeclinationRate
        {
            get
            {
                var r = SkyServer.RateDecOrg;
                
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSetEquRates, "DeclinationRate", true);
                CheckRate(value);
                SkyServer.RateDecOrg = value;
                SkyServer.RateDec = Conversions.ArcSec2Deg(value);
            }
        }

        public string Description
        {
            get
            {
                string r = SkySettings.InstrumentDescription;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            CheckVersionOne("DestinationSideOfPier");

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"RA|{RightAscension}|Dec|{Declination}" };
            MonitorLog.LogToMonitor(monitorItem);

            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            var r = SkyServer.DetermineSideOfPier(radec.X, radec.Y);
            return r;
        }

        public bool DoesRefraction
        {
            get
            {
                var r = SkySettings.CanDoesRefraction;
                CheckVersionOne("DoesRefraction", false);

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("DoesRefraction", true);
                SkySettings.CanDoesRefraction = value;
            }
        }

        public string DriverInfo
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly();
                var r = asm.FullName;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public string DriverVersion
        {
            get
            {
                CheckVersionOne("DriverVersion", false);
                var asm = Assembly.GetExecutingAssembly();
                var r = asm.GetName().Version.ToString();

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                CheckVersionOne("EquatorialSystem", false);

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.EquatorialCoordinateType}" };
                MonitorLog.LogToMonitor(monitorItem);

                return SkySettings.EquatorialCoordinateType;
            }
        }

        public void FindHome()
        {
            if (!SkyServer.AsComOn) return;
            CheckCapability(SkySettings.CanFindHome, "FindHome");
            CheckParked("FindHome");
            SkyServer.GoToHome();
            while (SkyServer.SlewState == SlewType.SlewHome || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Finished" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        public double FocalLength
        {
            get
            {
                CheckVersionOne("FocalLength", false);
                CheckCapability(SkySettings.CanOptics, "FocalLength", false);
                var r = SkySettings.FocalLength;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ r }" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                CheckVersionOne("GuideRateDeclination", false);
                var r = SkyServer.GuideRateDec;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ r }" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("GuideRateDeclination", true);
                SkyServer.GuideRateDec = value;
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                CheckVersionOne("GuideRateRightAscension", false);
                var r = SkyServer.GuideRateRa;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("GuideRateRightAscension", true);
                SkyServer.GuideRateRa = value;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                CheckVersionOne("InterfaceVersion", false);

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "3" };
                MonitorLog.LogToMonitor(monitorItem);

                return 3;
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                CheckCapability(SkySettings.CanPulseGuide, "IsPulseGuiding", false);
                var r = SkyServer.IsPulseGuiding;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        /// <summary>
        /// Move one axis at the given AxisRates(TelescopeAxes axis). 
        /// </summary>
        /// <param name="Axis"></param>
        /// <param name="Rate"></param>
        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            if (!SkyServer.AsComOn) return;
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Driver,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{Axis}, {Rate}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            CheckVersionOne("MoveAxis");
            CheckRate(Axis, Rate);
            if (!CanMoveAxis(Axis)){throw new MethodNotImplementedException("CanMoveAxis " + Enum.GetName(typeof(TelescopeAxes), Axis));}
            CheckParked("MoveAxis");

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary:
                    SkyServer.RateMoveAxisRa = Rate;
                    break;
                case TelescopeAxes.axisSecondary:
                    SkyServer.RateMoveAxisDec = Rate;
                    break;
                case TelescopeAxes.axisTertiary:
                default:
                    // not implemented
                    break;
            }
        }

        public string Name
        {
            get
            {
                string r = SkySettings.InstrumentName;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public void Park()
        {
            if (!SkyServer.AsComOn) return;
            MonitorEntry monitorItem;
            CheckCapability(SkySettings.CanPark, "Park");
            if (SkyServer.AtPark)
            {
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "At Park" };
                MonitorLog.LogToMonitor(monitorItem);

                return;
            }
            SkyServer.GoToPark();
            //while (SkyServer.SlewState == SlewType.SlewPark)
            //{
            //    Thread.Sleep(1);
            //    DoEvents();
            //}

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Parking" };
            MonitorLog.LogToMonitor(monitorItem);

        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            try
            {
                switch (Direction)
                {
                    case GuideDirections.guideNorth:
                    case GuideDirections.guideSouth:
                        SkyServer.IsPulseGuidingDec = true;
                        break;
                    case GuideDirections.guideEast:
                    case GuideDirections.guideWest:
                        SkyServer.IsPulseGuidingRa = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null);
                }

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{Direction},{Duration}") };
                MonitorLog.LogToMonitor(monitorItem);

                if (!SkyServer.AsComOn) { throw new InvalidOperationException("Not accepting commands"); }
                if (SkyServer.AtPark) { throw new ParkedException(); }

                if (SkyServer.IsSlewing) { throw new InvalidValueException("Pulse rejected when slewing"); }
                //if (!SkyServer.Tracking) { throw new InvalidValueException("Pulse rejected when tracking is off"); }

                CheckCapability(SkySettings.CanPulseGuide, "PulseGuide");
                CheckRange(Duration, 0, 30000, "PulseGuide", "Duration");
                SkyServer.PulseGuide(Direction, Duration);
                if (!SkySettings.CanDualAxisPulseGuide) { Thread.Sleep(Duration); } // Must be synchronous so wait out the pulse guide duration here
            }
            catch (Exception e)
            {
                SkyServer.IsPulseGuidingRa = false;
                SkyServer.IsPulseGuidingDec = false;
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{e.Message}") };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// The right ascension (hours) of the telescope's current equatorial coordinates,
        /// in the coordinate system given by the EquatorialSystem property 
        /// </summary>
        public double RightAscension
        {
            get
            {
                CheckCapability(SkySettings.CanEquatorial, "RightAscension", false);
                var ra = SkyServer.RightAscensionXForm;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"XForm|{_util.HoursToHMS(ra, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);


                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Internal|{_util.HoursToHMS(SkyServer.RightAscension, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return ra;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// The right ascension tracking rate offset from sidereal (seconds per sidereal second, default = 0.0) 
        /// This property, together with DeclinationRate, provides support for "offset tracking".
        /// Offset tracking is used primarily for tracking objects that move relatively slowly against
        /// the equatorial coordinate system. It also may be used by a software guiding system that
        /// controls rates instead of using the PulseGuide method. 
        /// </summary>
        public double RightAscensionRate
        {
            get
            {
                var r = SkyServer.RateRaOrg;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSetEquRates, "RightAscensionRate ", true);
                CheckRate(value);
                SkyServer.RateRaOrg = value;
                SkyServer.RateRa = Conversions.ArcSec2Deg(Conversions.SideSec2ArcSec(value));
            }
        }

        public void SetPark()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSetPark, "SetPark");
            SkyServer.SetParkAxis("External");
        }

        public void SetupDialog()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            SkyServer.OpenSetupDialog = true;
            //check if window is minimized or not top most
            NativeMethods.SetForegroundWindow("GS.Server"); //may cause flashing in task bar due to windows restrictions 
            //Calling app will destroy instance after the dialog is finished
            while (true)
            {
                Thread.Sleep(100);
                if (SkyServer.OpenSetupDialogFinished) { break; }
            }

            monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Open:{SkyServer.OpenSetupDialog}, Finished:{SkyServer.OpenSetupDialogFinished}" };
            MonitorLog.LogToMonitor(monitorItem);

        }

        public PierSide SideOfPier
        {
            get
            {
                var r = SkyServer.SideOfPier;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                CheckCapability(SkySettings.CanSetPierSide, "SideOfPier", true);
                MonitorEntry monitorItem;
                if (value == SkyServer.SideOfPier)
                {
                    monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "No Change Needed" };
                    MonitorLog.LogToMonitor(monitorItem);

                    return;
                }
                SkyServer.SideOfPier = value;

                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
        }

        public double SiderealTime
        {
            get
            {
                CheckCapability(SkySettings.CanSiderealTime, "SiderealTime", false);
                var r = SkyServer.SiderealTime;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(r)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public double SiteElevation
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteElevation", false);
                var r = SkySettings.Elevation;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteElevation", true);
                CheckRange(value, -300, 10000, "SiteElevation");
                SkySettings.Elevation = value;
            }
        }

        public double SiteLatitude
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteLatitude", false);
                var r = SkySettings.Latitude;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteLatitude", true);
                CheckRange(value, -90, 90, "SiteLatitude");
                SkySettings.Latitude = value;
            }
        }

        public double SiteLongitude
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteLongitude", false);
                var r = SkySettings.Longitude;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteLongitude", true);
                CheckRange(value, -180, 180, "SiteLongitude");
                SkySettings.Longitude = value;
            }
        }

        public short SlewSettleTime
        {
            get
            {
                var r = (short)(SkyServer.SlewSettleTime);

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckRange(value, 0, 100, "SlewSettleTime");
                var r = value;
                SkyServer.SlewSettleTime = r;
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            if (!SkyServer.AsComOn) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)}|{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAltAz, "SlewToAltAz");
            CheckParked("SlewToAltAz");
            CheckTracking(false, "SlewToAltAz");
            CheckRange(Azimuth, 0, 360, "SlewToAltAz", "azimuth");
            CheckRange(Altitude, -90, 90, "SlewToAltAz", "Altitude");
            SkyServer.SlewAltAz(Altitude, Azimuth);
            while (SkyServer.SlewState == SlewType.SlewAltAz || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            if (!SkyServer.AsComOn) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)}|{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAltAzAsync, "SlewToAltAzAsync");
            CheckParked("SlewToAltAz");
            CheckTracking(false, "SlewToAltAzAsync");
            CheckRange(Azimuth, 0, 360, "SlewToAltAzAsync", "Azimuth");
            CheckRange(Altitude, -90, 90, "SlewToAltAzAsync", "Altitude");
            SkyServer.SlewAltAz(Altitude, Azimuth);
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            if (!SkyServer.AsComOn) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)}|{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlew, "SlewToCoordinates");
            CheckRange(RightAscension, 0, 24, "SlewToCoordinates", "RightAscension");
            CheckRange(Declination, -90, 90, "SlewToCoordinates", "Declination");
            CheckParked("SlewToCoordinates");
            CheckTracking(true, "SlewToCoordinates");

            SkyServer.TargetRa = RightAscension;
            SkyServer.TargetDec = Declination;
            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            SkyServer.SlewRaDec(radec.X, radec.Y);
            while (SkyServer.SlewState == SlewType.SlewRaDec || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            if (!SkyServer.AsComOn) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)}|{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);
            
            CheckCapability(SkySettings.CanSlewAsync, "SlewToCoordinatesAsync");
            CheckRange(RightAscension, 0, 24, "SlewToCoordinatesAsync", "RightAscension");
            CheckRange(Declination, -90, 90, "SlewToCoordinatesAsync", "Declination");
            CheckParked("SlewToCoordinatesAsync");

            CycleOnTracking(true);
            SkyServer.TargetRa = RightAscension;
            SkyServer.TargetDec = Declination;
            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            SkyServer.SlewRaDec(radec.X, radec.Y);
        }

        public void SlewToTarget()
        {
            if (!SkyServer.AsComOn) return;

            var ra = SkyServer.TargetRa;
            var dec = SkyServer.TargetDec;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Driver,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message =
                    FormattableString.Invariant($"{ra}|{dec}")
            };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlew, "SlewToTarget");
            CheckRange(ra, 0, 24, "SlewToTarget", "TargetRightAscension");
            CheckRange(dec, -90, 90, "SlewToTarget", "TargetDeclination");
            CheckParked("SlewToTarget");
            CheckTracking(true, "SlewToTarget");
            var xy = Transforms.CoordTypeToInternal(ra, dec);
            SkyServer.SlewRaDec(xy.X, xy.Y);
            while (SkyServer.SlewState == SlewType.SlewRaDec || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        public void SlewToTargetAsync()
        {
            if (!SkyServer.AsComOn) return;

            var ra = SkyServer.TargetRa;
            var dec = SkyServer.TargetDec;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{ra}|{dec}") };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAsync, "SlewToTargetAsync");
            CheckRange(ra, 0, 24, "SlewToTargetAsync", "TargetRightAscension");
            CheckRange(dec, -90, 90, "SlewToTargetAsync", "TargetDeclination");
            CheckParked("SlewToTargetAsync");
            CheckTracking(true, "SlewToTargetAsync");

            var xy = Transforms.CoordTypeToInternal(ra, dec);
            SkyServer.SlewRaDec(xy.X, xy.Y);
        }

        public bool Slewing
        {
            get
            {
                var r = SkyServer.IsSlewing;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)}|{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSyncAltAz, "SyncToAltAz");
            CheckRange(Azimuth, 0, 360, "SyncToAltAz", "Azimuth");
            CheckRange(Altitude, -90, 90, "SyncToAltAz", "Altitude");
            CheckParked("SyncToAltAz");
            CheckTracking(false, "SyncToAltAz");
            CheckAltAzSync(Altitude, Azimuth, "SyncToAltAz");
            SkyServer.AtPark = false;
            SkyServer.SyncToAltAzm(Azimuth, Altitude);
            DelayInterval();
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Driver,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)}|{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSync, "SyncToCoordinates");
            CheckRange(RightAscension, 0, 24, "SyncToCoordinates", "RightAscension");
            CheckRange(Declination, -90, 90, "SyncToCoordinates", "Declination");
            CheckParked("SyncToCoordinates");
            CheckTracking(true, "SyncToCoordinates");

            SkyServer.TargetDec = Declination;
            SkyServer.TargetRa = RightAscension;
            var a = Transforms.CoordTypeToInternal(RightAscension, Declination);
            CheckRaDecSync(a.X, a.Y, "SyncToCoordinates");

            SkyServer.AtPark = false;
            SkyServer.SyncToTargetRaDec();
            DelayInterval();
        }

        public void SyncToTarget()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Driver,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_util.HoursToHMS(SkyServer.TargetRa, "h ", ":", "", 2)}|{_util.DegreesToDMS(SkyServer.TargetDec, "° ", ":", "", 2)}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSync, "SyncToTarget");
            CheckRange(SkyServer.TargetRa, 0, 24, "SyncToTarget", "TargetRightAscension");
            CheckRange(SkyServer.TargetDec, -90, 90, "SyncToTarget", "TargetDeclination");
            CheckParked("SyncToTarget");
            CheckTracking(true, "SyncToTarget");

            var a = Transforms.CoordTypeToInternal(RightAscension, Declination);
            CheckRaDecSync(a.X, a.Y, "SyncToTarget");

            SkyServer.AtPark = false;
            SkyServer.SyncToTargetRaDec();
            DelayInterval();
        }

        public double TargetDeclination
        {
            get
            {
                CheckCapability(SkySettings.CanSlew, "TargetDeclination", false);
                CheckRange(SkyServer.TargetDec, -90, 90, "TargetDeclination");
                var r = SkyServer.TargetDec;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AsComOn) return;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(value, "° ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSlew, "TargetDeclination", true);
                CheckRange(value, -90, 90, "TargetDeclination");

                SkyServer.TargetDec = value;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                CheckCapability(SkySettings.CanSlew, "TargetRightAscension", false);
                CheckRange(SkyServer.TargetRa, 0, 24, "TargetRightAscension");
                var r = SkyServer.TargetRa;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AsComOn) return;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(value, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSlew, "TargetRightAscension", true);
                CheckRange(value, 0, 24, "TargetRightAscension");

                SkyServer.TargetRa = value;
            }
        }

        public bool Tracking
        {
            get
            {
                var r = SkyServer.Tracking;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AsComOn) return;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.Tracking = value;
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                var r = SkySettings.TrackingRate;
                CheckVersionOne("TrackingRate", false);

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("TrackingRate", true);
                CheckTrackingRate("TrackingRate", value);
                SkySettings.TrackingRate = value;
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                MonitorEntry monitorItem;
                if (SkySettings.CanTrackingRates)
                {
                    monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_mTrackingRates}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    return _mTrackingRates;
                }
                monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_mTrackingRatesSimple}" };
                MonitorLog.LogToMonitor(monitorItem);

                return _mTrackingRatesSimple;
            }
        }

        public DateTime UTCDate
        {
            get
            {
                // var r = HiResDateTime.UtcNow.Add(SkySettings.UTCDateOffset);
                var r = HiResDateTime.UtcNow;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {

                //var r = value.Subtract(HiResDateTime.UtcNow);
                //if (Math.Abs(r.TotalMilliseconds) < 100) r = new TimeSpan();
                //SkySettings.UTCDateOffset = r;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new PropertyNotImplementedException(MethodBase.GetCurrentMethod()?.Name);
            }
        }

        public void Unpark()
        {
            CheckCapability(SkySettings.CanUnPark, "UnPark");
            SkyServer.AtPark = false;
            SkyServer.Tracking = true;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Finished" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        #endregion

        #region Pier Side Properties

        //public double AvailableTimeInThisPointingState
        //{
        //    get
        //    {
        //        if (AlignmentMode != AlignmentModes.algGermanPolar)
        //        {
        //            return 86400;
        //        }
        //        return TelescopeHardware.AvailableTimeInThisPointingState;
        //    }
        //}

        //public double TimeUntilPointingStateCanChange
        //{
        //    get
        //    {
        //        if (AlignmentMode != AlignmentModes.algGermanPolar)
        //        {
        //            return 0;
        //        }
        //        return TelescopeHardware.TimeUntilPointingStateCanChange;
        //    }
        //}

        #endregion

        #region Private Methods

        private static void CheckTrackingRate(string propertyOrMethod, DriveRates enumValue)
        {
            var success = Enum.IsDefined(typeof(DriveRates), enumValue);
            if (success) return;
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{propertyOrMethod}|{enumValue}") };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidValueException("TrackingRate invalid");
        }

        private static void CheckRange(double value, double min, double max, string propertyOrMethod, string valueName)
        {
            if (double.IsNaN(value))
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}|{min}|{max}|{propertyOrMethod}|{valueName}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new ValueNotSetException(propertyOrMethod + ":" + valueName);
            }

            if (value < min || value > max)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}|{min}|{max}|{propertyOrMethod}|{valueName}" };
                MonitorLog.LogToMonitor(monitorItem);
                throw new InvalidValueException(propertyOrMethod, value.ToString(CultureInfo.CurrentCulture),
                    string.Format(CultureInfo.CurrentCulture, "{0}, {1} to {2}", valueName, min, max));
            }
        }

        private static void CheckRange(double value, double min, double max, string propertyOrMethod)
        {
            if (double.IsNaN(value))
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}|{min}|{max}|{propertyOrMethod}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new ValueNotSetException(propertyOrMethod);
            }

            if (value < min || value > max)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}|{min}|{max}|{propertyOrMethod}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new InvalidValueException(propertyOrMethod, value.ToString(CultureInfo.CurrentCulture),
                    string.Format(CultureInfo.CurrentCulture, "{0} to {1}", min, max));
            }
        }

        private static void CheckVersionOne(string property, bool accessorSet)
        {
            CheckVersionOne(property);
            if (accessorSet)
            {
                //nothing
            }
            if (!SkySettings.VersionOne) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}|{accessorSet}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property, accessorSet);
        }

        private static void CheckVersionOne(string property)
        {
            if (!SkySettings.VersionOne) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property);
        }

        private static void CheckCapability(bool capability, string method)
        {
            if (capability) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{method}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new MethodNotImplementedException(method);
        }

        private static void CheckCapability(bool capability, string property, bool setNotGet)
        {
            if (capability) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}|{setNotGet}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property, setNotGet);
        }

        private static void CheckParked(string property)
        {
            if (!SkyServer.AtPark) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new ParkedException(property + @": Telescope parked");
        }

        /// <summary>
        /// Check slew rate for amount limit
        /// </summary>
        /// <param name="rate"></param>
        private static void CheckRate(double rate)
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{rate}" };
            MonitorLog.LogToMonitor(monitorItem);
            var deg = Conversions.ArcSec2Deg(rate);
            if (deg > SkyServer.SlewSpeedEight || deg < -SkyServer.SlewSpeedEight){
                throw new InvalidValueException($"{rate} is out of limits");
            }
        }

        /// <summary>
        /// CheckRate in degrees against the axis rates
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="rate"></param>
        private void CheckRate(TelescopeAxes axis, double rate)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{rate}" };
            MonitorLog.LogToMonitor(monitorItem);

            var rates = AxisRates(axis);
            var ratesStr = string.Empty;
            foreach (Rate item in rates)
            {
                if (Math.Abs(rate) >= item.Minimum && Math.Abs(rate) <= item.Maximum)
                {
                    return;
                }
                ratesStr = $"{ratesStr}, {item.Minimum} to {item.Maximum}";
            }
            throw new InvalidValueException($"MoveAxis", rate.ToString(CultureInfo.InvariantCulture), ratesStr);
        }

        /// <summary>
        /// Checks the slew type and tracking state and raises an exception if they don't match.
        /// </summary>
        /// <param name="raDecSlew">if set to <c>true</c> this is a Ra Dec slew is <c>false</c> an Alt Az slew.</param>
        /// <param name="method">The method name.</param>
        private static void CheckTracking(bool raDecSlew, string method)
        {
            if (raDecSlew == SkyServer.Tracking) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{SkyServer.Tracking}|{raDecSlew}|{method}") };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidOperationException($"{method} is not allowed when tracking is {SkyServer.Tracking}");
        }

        /// <summary>
        /// Checks the sync is too far from the current position
        /// </summary>
        /// <param name="ra">Syncing Ra to check</param>
        /// <param name="dec">Syncing Dec to check</param>
        /// <param name="method">The method name</param>
        private static void CheckRaDecSync(double ra, double dec, string method)
        {
            var pass = SkyServer.CheckRaDecSyncLimit(ra, dec);
            if (pass) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{ra}|{dec}|{method}") };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidOperationException($"{method} out of sync limits");
        }

        /// <summary>
        /// Checks the sync is too far from the current Alt/Az position
        /// </summary>
        /// <param name="alt">Syncing Ra to check</param>
        /// <param name="az">Syncing az to check</param>
        /// <param name="method">The method name</param>
        private static void CheckAltAzSync(double alt, double az, string method)
        {
            var pass = SkyServer.CheckAltAzSyncLimit(alt, az);
            if (pass) return;

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = FormattableString.Invariant($"{alt}|{az}|{method}") };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidOperationException($"{method} out of sync limits");
        }

        /// <summary>
        /// Allow application events to occur while waiting for something
        /// </summary>
        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }

        /// <summary>
        /// Allows the UI and Server time for the Event to update positions from the mount
        /// </summary>
        /// <para>additional milliseconds</para>
        /// <returns></returns>
        private static void DelayInterval(int additional = 0)
        {
            var delay = additional;
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    delay += SkySettings.DisplayInterval;
                    break;
                case MountType.SkyWatcher:
                    delay += 20;  // some go tos have been off .10 to .70 seconds, not sure exactly why
                    delay += SkySettings.DisplayInterval;
                    break;
            }
            //Thread.Sleep(delay);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < delay) { }
            sw.Stop();
        }

        /// <summary>
        /// Cycles tracking
        /// </summary>
        /// <param name="silence">turns off voice</param>
        /// <remarks>planetarium programs fix which doesn't turn on tracking before a goto</remarks>
        private static void CycleOnTracking(bool silence)
        {
            if (silence) { SkyServer.TrackingSpeak = false; }

            SkyServer.Tracking = false;
            SkyServer.Tracking = true;

            if (silence) { SkyServer.TrackingSpeak = true; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // tried destructor from object base and doesn't work
            // disposed seems to not run, not sure why
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            // free managed resources
            Connected = false;
            _mAxisRates[0].Dispose();
            _mAxisRates[1].Dispose();
            _mAxisRates[2].Dispose();
            _mAxisRates = null;
            _mTrackingRates.Dispose();
            _mTrackingRates = null;
            _mTrackingRatesSimple.Dispose();
            _mTrackingRatesSimple = null;
            _util.Dispose();
            _util = null;
            // free native resources if there are any.
        }

        #endregion
    }
}
