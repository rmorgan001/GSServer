#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM;
using GS.Shared;
using GS.Utilities.Helpers;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser
{
    internal class AscomFocuser : ObservableObject, IFocuser, IDisposable
    {

        public AscomFocuser(string focuser, string name)
        {
            Id = focuser;
            Name = name;
        }

        private IFocuserV3Ex _focuser;
        public IAscomFocuserProvider FocuserProvider { get; set; } = new AscomFocuserProvider();

        public string Category { get; } = "ASCOM";

        private string _id;

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool IsMoving
        {
            get
            {
                if (Connected)
                {
                    return _focuser.IsMoving;
                }
                else
                {
                    return false;
                }
            }
        }

        public int MaxIncrement
        {
            get
            {
                if (Connected)
                {
                    return Math.Abs(_focuser.MaxIncrement);
                }
                else
                {
                    return -1;
                }
            }
        }

        public int MaxStep
        {
            get
            {
                if (Connected)
                {
                    return Math.Abs(_focuser.MaxStep);
                }
                else
                {
                    return -1;
                }
            }
        }

        private bool _canGetPosition;

        public int Position
        {
            get
            {
                int pos = -1;
                try
                {
                    if (Connected && _canGetPosition)
                    {
                        pos = Math.Abs(_focuser.Position);
                    }
                }
                catch (PropertyNotImplementedException)
                {
                    _canGetPosition = false;
                }
                catch (System.NotImplementedException)
                {
                    _canGetPosition = false;
                }
                catch (DriverException ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                return pos;
            }
        }

        private bool _canGetStepSize;

        public double StepSize
        {
            get
            {
                double stepSize = double.NaN;
                try
                {
                    if (Connected && _canGetStepSize)
                    {
                        stepSize = _focuser.StepSize;
                    }
                }
                catch (PropertyNotImplementedException)
                {
                    _canGetStepSize = false;
                }
                catch (System.NotImplementedException)
                {
                    _canGetStepSize = false;
                }
                catch (DriverException ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                return stepSize;
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                if (Connected)
                {
                    return _focuser.TempCompAvailable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool TempComp
        {
            get
            {
                if (Connected && _focuser.TempCompAvailable)
                {
                    return _focuser.TempComp;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (Connected && _focuser.TempCompAvailable)
                {
                    _focuser.TempComp = value;
                }
            }
        }

        private bool _hasTemperature;

        public double Temperature
        {
            get
            {
                double temperature = double.NaN;
                try
                {
                    if (Connected && _hasTemperature)
                    {
                        temperature = _focuser.Temperature;
                    }
                }
                catch (PropertyNotImplementedException)
                {
                    _hasTemperature = false;
                }
                catch (System.NotImplementedException)
                {
                    _hasTemperature = false;
                }
                catch (DriverException ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                return temperature;
            }
        }

        private bool _connected;

        public bool Connected
        {
            get
            {
                if (_connected)
                {
                    bool val = false;
                    try
                    {
                        val = _focuser.Connected;
                        if (_connected != val)
                        {
                            var monitorItem = new MonitorEntry
                            { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Focuser connection lost" };
                            MonitorLog.LogToMonitor(monitorItem);
                            Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        var monitorItem = new MonitorEntry
                        { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                        MonitorLog.LogToMonitor(monitorItem);
                        
                        monitorItem = new MonitorEntry
                        { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Focuser connection lost" };
                        MonitorLog.LogToMonitor(monitorItem);
                        try
                        {
                            Disconnect();
                        }
                        catch (Exception disconnectEx)
                        {
                            monitorItem = new MonitorEntry
                            { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{disconnectEx.Message}" };
                            MonitorLog.LogToMonitor(monitorItem);
                        }
                    }
                    return val;
                }
                else
                {
                    return false;
                }
            }
            private set
            {
                try
                {
                    _focuser.Connected = value;
                    _connected = value;
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Reconnect focuser" };
                    MonitorLog.LogToMonitor(monitorItem);
                    _connected = false;
                }
                OnPropertyChanged();
            }
        }

        public async Task Move(int position, CancellationToken ct)
        {
            await _focuser.MoveAsync(position, ct);
        }

        private bool _canHalt;

        public void Halt()
        {
            if (Connected && _canHalt)
            {
                try
                {
                    _focuser.Halt();
                }
                catch (MethodNotImplementedException)
                {
                    _canHalt = false;
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
            }
        }

        public bool HasSetupDialog
        {
            get
            {
                return true;
            }
        }

        public string Description
        {
            get
            {
                if (Connected)
                {
                    return _focuser.Description;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string DriverInfo
        {
            get
            {
                return Connected ? _focuser?.DriverInfo ?? string.Empty : string.Empty;
            }
        }

        public string DriverVersion
        {
            get
            {
                return Connected ? _focuser?.DriverVersion ?? string.Empty : string.Empty;
            }
        }

        public void Dispose()
        {
            _focuser?.Dispose();
        }

        private IFocuserV3Ex GetFocuser(bool connect)
        {
            return FocuserProvider.GetFocuser(Id, connect);
        }

        public void SetupDialog()
        {
            if (HasSetupDialog)
            {
                try
                {
                    bool dispose = false;
                    if (_focuser == null)
                    {
                        _focuser = GetFocuser(false);
                        dispose = true;
                    }
                    _focuser.SetupDialog();
                    if (dispose)
                    {
                        _focuser.Dispose();
                        _focuser = null;
                    }
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Other, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    // TODO: Notification.ShowError(ex.Message);
                }
            }
        }

        public async Task<bool> Connect(CancellationToken token)
        {
            return await Task.Run(() => {
                try
                {
                    _focuser = GetFocuser(true);
                    Connected = true;
                    if (Connected)
                    {
                        Initialize();
                        // RaiseAllPropertiesChanged();
                    }
                }
                catch (DriverAccessCOMException ex)
                {
                    // Utility.Utility.HandleAscomCOMException(ex);
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // Utility.Utility.HandleAscomCOMException(ex);
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                catch (Exception ex)
                {
                    var monitorItem = new MonitorEntry
                    { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    // TODO: Notification.ShowError("Unable to connect to focuser " + ex.Message);
                }
                return Connected;
            });
        }

        private void Initialize()
        {
            _canGetPosition = true;
            _canGetStepSize = true;
            _hasTemperature = true;
            _canHalt = true;
        }

        public void Disconnect()
        {
            Connected = false;
            _focuser?.Dispose();
            _focuser = null;
        }
    }
}
