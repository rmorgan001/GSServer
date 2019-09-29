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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;


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
        private readonly long _objectId;

        /// <summary>
        /// Constructor - Must be public for COM registration!
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public Telescope()
        {
            try
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
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
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Instance ID: {_objectId}, Driver ID: {driverId}" };
                MonitorLog.LogToMonitor(monitorItem);

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Error: {ex.Message} Stack: {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region Public com Interface ITelescope Implementaion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ActionName")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ActionParameters")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public string Action(string ActionName, string ActionParameters)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" ActionName: {ActionName}, ActionParameters{ActionParameters}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new MethodNotImplementedException("Action");
        }

        /// <summary>
        /// Gets the supported actions.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public ArrayList SupportedActions
        {
            // no supported actions, return empty array
            get
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
                MonitorLog.LogToMonitor(monitorItem);

                var sa = new ArrayList();
                return sa;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void AbortSlew()
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Started" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckParked("AbortSlew");
            SkyServer.AbortSlew();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public AlignmentModes AlignmentMode{
            get
            {
                CheckCapability(SkySettings.CanAlignMode, "AlignmentMode");
                var r = SkySettings.AlignmentMode;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public double Altitude
        {
            get
            {
                CheckCapability(SkySettings.CanAltAz, "Altitude", false);
                var r = SkyServer.Altitude;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public double ApertureArea
        {
            get
            {
                CheckCapability(SkySettings.CanOptics, "ApertureArea", false);
                var r = SkySettings.ApertureArea;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public double ApertureDiameter
        {
            get
            {
                CheckCapability(SkySettings.CanOptics, "ApertureDiameter", false);
                var r = SkySettings.ApertureDiameter;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool AtHome
        {
            get
            {
                CheckVersionOne("AtHome", false);
                var r = SkyServer.AtHome;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool AtPark
        {
            get
            {
                CheckVersionOne("AtPark", false);
                var r = SkySettings.AtPark;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"  {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"   {Axis}" };
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public double Azimuth
        {
            get
            {
                CheckCapability(SkySettings.CanAltAz, "Azimuth", false);
                var r = SkyServer.Azimuth;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanFindHome
        {
            get
            {
                var r = SkySettings.CanFindHome;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            CheckVersionOne("CanMoveAxis");
            var r = SkyServer.CanMoveAxis(Axis);

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
            MonitorLog.LogToMonitor(monitorItem);

            return r;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanPark
        {
            get
            {
                var r = SkySettings.CanPark;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanPulseGuide
        {
            get
            {
                var r = SkySettings.CanPulseGuide;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetDeclinationRate
        {
            get
            {
                CheckVersionOne("CanSetDeclinationRate", false);
                var r = SkySettings.CanSetDeclinationRate;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetGuideRates
        {
            get
            {
                var r = SkySettings.CanSetGuideRates;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetPark
        {
            get
            {
                var r = SkySettings.CanSetPark;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetPierSide
        {
            get
            {
                CheckVersionOne("CanSetPierSide", false);
                var r = SkySettings.CanSetPierSide;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetRightAscensionRate
        {
            get
            {
                CheckVersionOne("CanSetRightAscensionRate", false);
                var r = SkySettings.CanSetRightAscensionRate;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSetTracking
        {
            get
            {
                var r = SkySettings.CanSetTracking;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSlew
        {
            get
            {
                var r = SkySettings.CanSlew;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSlewAltAz
        {
            get
            {
                CheckVersionOne("CanSlewAltAz", false);
                var r = SkySettings.CanSlewAltAz;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSlewAltAzAsync
        {
            get
            {
                CheckVersionOne("CanSlewAltAzAsync", false);
                var r = SkySettings.CanSlewAltAzAsync;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSlewAsync
        {
            get
            {
                var r = SkySettings.CanSlewAsync;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSync
        {
            get
            {
                var r = SkySettings.CanSync;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanSyncAltAz
        {
            get
            {
                var r = SkySettings.CanSyncAltAz;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool CanUnpark
        {
            get
            {
                var r = SkySettings.CanUnpark;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
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
            throw new MethodNotImplementedException("CommandString");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public bool Connected
        {
            get
            {
                var r = SkySystem.Connected;
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {r}" };
                MonitorLog.LogToMonitor(monitorItem);
                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkySystem.SetConnected(_objectId, value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double Declination
        {
            get
            {
                CheckCapability(SkySettings.CanEquatorial, "Declination", false);
                var dec = SkyServer.DeclinationXform;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(dec, "° ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return dec;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double DeclinationRate
        {
            get
            {
                var r = SkyServer.RateDec;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.RateDec = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public string Description
        {
            get
            {
                string r = SkySettings.InstrumentDescription;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            CheckVersionOne("DestinationSideOfPier");

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Started RA {RightAscension}, Dec {Declination}" };
            MonitorLog.LogToMonitor(monitorItem);

            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            var r = SkyServer.SideOfPierRaDec(radec.X);
            return r;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public bool DoesRefraction
        {
            get
            {
                var r = SkySettings.CanDoesRefraction;
                CheckVersionOne("DoesRefraction", false);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
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
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
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
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
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
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkySettings.EquatorialCoordinateType.ToString()}" };
                MonitorLog.LogToMonitor(monitorItem);

                return SkySettings.EquatorialCoordinateType;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void FindHome()
        {
            if (!SkyServer.AscomOn) return;
            CheckCapability(SkySettings.CanFindHome, "FindHome");
            CheckParked("FindHome");
            SkyServer.GoToHome();
            while (SkyServer.SlewState == SlewType.SlewHome || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Finished" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double FocalLength
        {
            get
            {
                CheckVersionOne("FocalLength", false);
                CheckCapability(SkySettings.CanOptics, "FocalLength", false);
                var r = SkySettings.FocalLength;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ r }" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double GuideRateDeclination
        {
            get
            {
                CheckVersionOne("GuideRateDeclination", false);
                var r = SkyServer.GuideRateDec;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ r }" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("GuideRateDeclination", true);
                SkyServer.GuideRateDec = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double GuideRateRightAscension
        {
            get
            {
                CheckVersionOne("GuideRateRightAscension", false);
                var r = SkyServer.GuideRateRa;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("GuideRateRightAscension", true);
                SkyServer.GuideRateRa = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public short InterfaceVersion
        {
            get
            {
                CheckVersionOne("InterfaceVersion", false);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "3" };
                MonitorLog.LogToMonitor(monitorItem);

                return 3;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public bool IsPulseGuiding
        {
            get
            {
                CheckCapability(SkySettings.CanPulseGuide, "IsPulseGuiding", false);
                var r = SkyServer.IsPulseGuiding;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            if (!SkyServer.AscomOn) return;
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver,
                Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Axis}, {Rate}"};
            MonitorLog.LogToMonitor(monitorItem);

            CheckVersionOne("MoveAxis");
            CheckRate(Axis, Rate);
            if (!CanMoveAxis(Axis))
                throw new MethodNotImplementedException("CanMoveAxis " + Enum.GetName(typeof(TelescopeAxes), Axis));
            CheckParked("MoveAxis");

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary:
                    SkyServer.RateAxisRa = Rate;
                    break;
                case TelescopeAxes.axisSecondary:
                    SkyServer.RateAxisDec = Rate;
                    break;
                case TelescopeAxes.axisTertiary:
                    // not implemented
                    break;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "GSServer")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public string Name
        {
            get
            {
                string r = SkySettings.InstrumentName;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void Park()
        {
            if (!SkyServer.AscomOn) return;
            MonitorEntry monitorItem;
            CheckCapability(SkySettings.CanPark, "Park");
            if (SkyServer.AtPark)
            {
                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "At Park" };
                MonitorLog.LogToMonitor(monitorItem);

                return;
            }
            SkyServer.GoToPark();
            while (SkyServer.SlewState == SlewType.SlewPark)
            {
                Thread.Sleep(1);
                DoEvents();
            }

            monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Parked" };
            MonitorLog.LogToMonitor(monitorItem);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            if (!SkyServer.AscomOn) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Direction}"};
            MonitorLog.LogToMonitor(monitorItem);

            if (SkyServer.AtPark) throw new ParkedException();
            CheckCapability(SkySettings.CanPulseGuide, "PulseGuide");
            CheckRange(Duration, 0, 30000, "PulseGuide", "Duration");
            SkyServer.PulseGuide(Direction, Duration);
            if (!SkySettings.CanDualAxisPulseGuide) Thread.Sleep(Duration); // Must be synchronous so wait out the pulseguide duration here
        }

        /// <inheritdoc />
        /// <summary>
        /// The right ascension (hours) of the telescope's current equatorial coordinates,
        /// in the coordinate system given by the EquatorialSystem property 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double RightAscension
        {
            get
            {
                CheckCapability(SkySettings.CanEquatorial, "RightAscension", false);
                var ra = SkyServer.RightAscensionXform;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(ra, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);


                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" current: {_util.HoursToHMS(SkyServer.RightAscension, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return ra;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// This property, together with DeclinationRate, provides support for "offset tracking".
        /// Offset tracking is used primarily for tracking objects that move relatively slowly against
        /// the equatorial coordinate system. It also may be used by a software guiding system that
        /// controls rates instead of using the PulseGuide method. 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double RightAscensionRate
        {
            get
            {
                var r = SkyServer.RateRa;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSetEquRates, "RightAscensionRate", true);
                SkyServer.RateRa = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SetPark()
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSetPark, "SetPark");
            SkyServer.SetParkAxis();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SetupDialog()
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Started" };
            MonitorLog.LogToMonitor(monitorItem);

            var alreadyopen = SkyServer.OpenSetupDialog;
            if (alreadyopen) throw new InvalidOperationException("Driver is already open");
            SkyServer.OpenSetupDialog = true;
            while (true)
            {
                Thread.Sleep(100);
                if (SkyServer.OpenSetupDialogFinished)break;
            }
            
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public PierSide SideOfPier
        {
            get
            {
                var r = SkyServer.SideOfPier;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
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
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "No Change Needed" };
                    MonitorLog.LogToMonitor(monitorItem);

                    return;
                }
                SkyServer.SideOfPier = value;

                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
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
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToHMS(r)}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double SiteElevation
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteElevation", false);
                var r = SkySettings.Elevation;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteElevation", true);
                CheckRange(value, -300, 10000, "SiteElevation");
                SkySettings.Elevation = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double SiteLatitude
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteLatitude", false);
                var r = SkySettings.Latitude;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteLatitude", true);
                CheckRange(value, -90, 90, "SiteLatitude");
                SkySettings.Latitude = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double SiteLongitude
        {
            get
            {
                CheckCapability(SkySettings.CanLatLongElev, "SiteLongitude", false);
                var r = SkySettings.Longitude;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanLatLongElev, "SiteLongitude", true);
                CheckRange(value, -180, 180, "SiteLongitude");
                SkySettings.Longitude = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public short SlewSettleTime
        {
            get
            {
                var r = (short)(SkyServer.SlewSettleTime);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckRange(value, 0, 100, "SlewSettleTime");
                var r = value;
                SkyServer.SlewSettleTime = r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            if (!SkyServer.AscomOn) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)},{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAltAz, "SlewToAltAz");
            CheckParked("SlewToAltAz");
            CheckTracking(false, "SlewToAltAz");
            CheckRange(Azimuth, 0, 360, "SlewToltAz", "azimuth");
            CheckRange(Altitude, -90, 90, "SlewToAltAz", "Altitude");
            SkyServer.SlewAltAz(Altitude, Azimuth);
            while (SkyServer.SlewState == SlewType.SlewAltAz || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            if (!SkyServer.AscomOn) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)},{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAltAzAsync, "SlewToAltAzAsync");
            CheckParked("SlewToAltAz");
            CheckTracking(false, "SlewToAltAzAsync");
            CheckRange(Azimuth, 0, 360, "SlewToAltAzAsync", "Azimuth");
            CheckRange(Altitude, -90, 90, "SlewToAltAzAsync", "Altitude");
            SkyServer.SlewAltAz(Altitude, Azimuth);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            if (!SkyServer.AscomOn) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)},{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlew, "SlewToCoordinates");
            CheckRange(RightAscension, 0, 24, "SlewToCoordinates", "RightAscension");
            CheckRange(Declination, -90, 90, "SlewToCoordinates", "Declination");
            CheckParked("SlewToCoordinates");
            CheckTracking(true, "SlewToCoordinates");

            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            SkyServer.TargetRa = radec.X;
            SkyServer.TargetDec = radec.Y;
            SkyServer.SlewRaDec(radec.X, radec.Y);
            while (SkyServer.SlewState == SlewType.SlewRaDec || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            if (!SkyServer.AscomOn) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)},{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);


            CheckCapability(SkySettings.CanSlewAsync, "SlewToCoordinatesAsync");
            CheckRange(RightAscension, 0, 24, "SlewToCoordinatesAsync", "RightAscension");
            CheckRange(Declination, -90, 90, "SlewToCoordinatesAsync", "Declination");
            CheckParked("SlewToCoordinatesAsync");
            CheckTracking(true, "SlewToCoordinatesAsync");

            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            SkyServer.TargetRa = radec.X;
            SkyServer.TargetDec = radec.Y;
            SkyServer.SlewRaDec(radec.X, radec.Y);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToTarget()
        {
            if (!SkyServer.AscomOn) return;

            var ra = SkyServer.TargetRa;
            var dec = SkyServer.TargetDec;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ra},{dec}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlew, "SlewToTarget");
            CheckRange(SkyServer.TargetRa, 0, 24, "SlewToTarget", "TargetRightAscension");
            CheckRange(SkyServer.TargetDec, -90, 90, "SlewToTarget", "TargetDeclination");
            CheckParked("SlewToTarget");
            CheckTracking(true, "SlewToTarget");
            SkyServer.SlewRaDec(ra, dec);
            while (SkyServer.SlewState == SlewType.SlewRaDec || SkyServer.SlewState == SlewType.SlewSettle)
            {
                Thread.Sleep(1);
                DoEvents();
            }
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SlewToTargetAsync()
        {
            if (!SkyServer.AscomOn) return;

            var ra = SkyServer.TargetRa;
            var dec = SkyServer.TargetDec;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ra},{dec}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSlewAsync, "SlewToTargetAsync");
            CheckRange(SkyServer.TargetRa, 0, 24, "SlewToTargetAsync", "TargetRightAscension");
            CheckRange(SkyServer.TargetDec, -90, 90, "SlewToTargetAsync", "TargetDeclination");
            CheckParked("SlewToTargetAsync");
            CheckTracking(true, "SlewToTargetAsync");
            SkyServer.SlewRaDec(ra, dec);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public bool Slewing
        {
            get
            {
                var r = SkyServer.IsSlewing;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(Azimuth, "° ", ":", "", 2)},{_util.DegreesToDMS(Altitude, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSyncAltAz, "SyncToAltAz");
            CheckRange(Azimuth, 0, 360, "SyncToAltAz", "Azimuth");
            CheckRange(Altitude, -90, 90, "SyncToAltAz", "Altitude");
            CheckParked("SyncToAltAz");
            CheckTracking(false, "SyncToAltAz");
            SkyServer.AtPark = false;
            SkyServer.SyncToAltAzm(Azimuth, Altitude);
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                  Message = $"{_util.HoursToHMS(RightAscension, "h ", ":", "", 2)},{_util.DegreesToDMS(Declination, "° ", ":", "", 2)}" };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSync, "SyncToCoordinates");
            CheckRange(RightAscension, 0, 24, "SyncToCoordinates", "RightAscension");
            CheckRange(Declination, -90, 90, "SyncToCoordinates", "Declination");
            CheckParked("SyncToCoordinates");
            CheckTracking(true, "SyncToCoordinates");
            var radec = Transforms.CoordTypeToInternal(RightAscension, Declination);
            SkyServer.TargetDec = radec.Y;
            SkyServer.TargetRa = radec.X;
            SkyServer.AtPark = false;
            SkyServer.SyncToTargetRaDec();
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void SyncToTarget()
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{_util.HoursToHMS(SkyServer.TargetRa, "h ", ":", "", 2)},{_util.DegreesToDMS(SkyServer.TargetDec, "° ", ":", "", 2)}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            CheckCapability(SkySettings.CanSync, "SyncToTarget");
            CheckRange(SkyServer.TargetRa, 0, 24, "SyncToTarget", "TargetRightAscension");
            CheckRange(SkyServer.TargetDec, -90, 90, "SyncToTarget", "TargetDeclination");
            CheckParked("SyncToTarget");
            CheckTracking(true, "SyncToTarget");
            SkyServer.AtPark = false;
            SkyServer.SyncToTargetRaDec();
            DelayInterval();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double TargetDeclination
        {
            get
            {
                CheckCapability(SkySettings.CanSlew, "TargetDeclination", false);
                CheckRange(SkyServer.TargetDec, -90, 90, "TargetDeclination");
                var r = SkyServer.TargetDec;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AscomOn) return;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.DegreesToDMS(value, "° ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSlew, "TargetDeclination", true);
                CheckRange(value, -90, 90, "TargetDeclination");
                var radec = Transforms.CoordTypeToInternal(RightAscension, value);
                SkyServer.TargetDec = radec.Y;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public double TargetRightAscension
        {
            get
            {
                CheckCapability(SkySettings.CanSlew, "TargetRightAscension", false);
                CheckRange(SkyServer.TargetRa, 0, 24, "TargetRightAscension");
                var r = SkyServer.TargetRa;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AscomOn) return;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_util.HoursToHMS(value, "h ", ":", "", 2)}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckCapability(SkySettings.CanSlew, "TargetRightAscension", true);
                CheckRange(value, 0, 24, "TargetRightAscension");
                var radec = Transforms.CoordTypeToInternal(value, TargetDeclination); 
                SkyServer.TargetRa = radec.X;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public bool Tracking
        {
            get
            {
                var r = SkyServer.Tracking;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                if (!SkyServer.AscomOn) return;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.Tracking = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public DriveRates TrackingRate
        {
            get
            {
                var r = SkySettings.TrackingRate;
                CheckVersionOne("TrackingRate", false);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
                MonitorLog.LogToMonitor(monitorItem);

                CheckVersionOne("TrackingRate", true);
                CheckTrackingRate("TrackingRate", value);
                SkySettings.TrackingRate = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public ITrackingRates TrackingRates
        {
            get
            {
                MonitorEntry monitorItem;
                if (SkySettings.CanTrackingRates)
                {
                    monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_mTrackingRates}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    return _mTrackingRates;
                }
                monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{_mTrackingRatesSimple}" };
                MonitorLog.LogToMonitor(monitorItem);

                return _mTrackingRatesSimple;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public DateTime UTCDate
        {
            get
            {
                var r = HiResDateTime.UtcNow.AddSeconds(SkySettings.UTCDateOffset);

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);

                return r;
            }
            set
            {
                var r = (int)value.Subtract(HiResDateTime.UtcNow).TotalSeconds;
                SkySettings.UTCDateOffset = r;

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{r}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        public void Unpark()
        {
            CheckCapability(SkySettings.CanUnpark, "UnPark");
            SkyServer.AtPark = false;
            SkyServer.Tracking = true;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Finished" };
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
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{propertyOrMethod},{enumValue}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidValueException("TrackingRate invalid");
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private void CheckRate(TelescopeAxes axis, double rate)
        {
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{rate}" };
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
            throw new InvalidValueException("MoveAxis", rate.ToString(CultureInfo.InvariantCulture), ratesStr);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private static void CheckRange(double value, double min, double max, string propertyOrMethod, string valueName)
        {
            if (double.IsNaN(value))
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value},{min},{max},{propertyOrMethod},{valueName}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new ValueNotSetException(propertyOrMethod + ":" + valueName);
            }

            if (value < min || value > max)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value},{min},{max},{propertyOrMethod},{valueName}" };
                MonitorLog.LogToMonitor(monitorItem);
                throw new InvalidValueException(propertyOrMethod, value.ToString(CultureInfo.CurrentCulture),
                    string.Format(CultureInfo.CurrentCulture, "{0}, {1} to {2}", valueName, min, max));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private static void CheckRange(double value, double min, double max, string propertyOrMethod)
        {
            if (double.IsNaN(value))
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value},{min},{max},{propertyOrMethod}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new ValueNotSetException(propertyOrMethod);
            }

            if (value < min || value > max)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value},{min},{max},{propertyOrMethod}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw new InvalidValueException(propertyOrMethod, value.ToString(CultureInfo.CurrentCulture),
                    string.Format(CultureInfo.CurrentCulture, "{0} to {1}", min, max));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private static void CheckVersionOne(string property, bool accessorSet)
        {
            CheckVersionOne(property);
            if (accessorSet) {
                //nothing
            }
            if (!SkySettings.VersionOne) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property},{accessorSet}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property, accessorSet);
        }

        private static void CheckVersionOne(string property)
        {
            if (!SkySettings.VersionOne) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property);
        }

        private static void CheckCapability(bool capability, string method)
        {
            if (capability) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{method}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new MethodNotImplementedException(method);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private static void CheckCapability(bool capability, string property, bool setNotGet)
        {
            if (capability) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property},{setNotGet}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new PropertyNotImplementedException(property, setNotGet);
        }

        private static void CheckParked(string property)
        {
            if (!SkyServer.AtPark) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{property}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new ParkedException(property +@": Telescope parked");
        }

        /// <summary>
        /// Checks the slew type and tracking state and raises an exception if they don't match.
        /// </summary>
        /// <param name="raDecSlew">if set to <c>true</c> this is a Ra Dec slew is <c>false</c> an Alt Az slew.</param>
        /// <param name="method">The method name.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "GS.Shared.MonitorEntry.set_Message(System.String)")]
        private static void CheckTracking(bool raDecSlew, string method)
        {
            if (raDecSlew == SkyServer.Tracking) return;

            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{SkyServer.Tracking},{raDecSlew},{method}" };
            MonitorLog.LogToMonitor(monitorItem);

            throw new InvalidOperationException($"{method} is not allowed when tracking is {SkyServer.Tracking}");
        }

        /// <summary>
        /// Allow application events to occur while witing for something
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
            switch (SkySystem.Mount)
            {
                case MountType.Simulator:
                    delay += SkySettings.DisplayInterval;
                    break;
                case MountType.SkyWatcher:
                    delay += 20;  // some gotos have been off .10 to .70 seconds, not sure exactly why
                    delay += SkySettings.DisplayInterval;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            //Thread.Sleep(delay);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < delay) { }
            sw.Stop();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // tried destructor from objectbase and doesn't work
            // disposed seems to not run, not sure why
            Dispose(true);
            GC.SuppressFinalize(this);
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
