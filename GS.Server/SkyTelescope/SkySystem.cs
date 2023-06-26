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
using GS.Shared;
using GS.Shared.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GS.Server.SkyTelescope
{
    public static class SkySystem
    {
        public static event PropertyChangedEventHandler StaticPropertyChanged;
        private static long _idCount;
        private static readonly ConcurrentDictionary<long, bool> ConnectStates;

        static SkySystem()
        {
            ConnectStates = new ConcurrentDictionary<long, bool>();
            _idCount = 0;
            DiscoverSerialDevices();
        }

        public static ISerialPort Serial { get; private set; }

        private static IList<string> _devices;

        /// <summary>
        /// com and remote ip ports
        /// </summary>
        public static IList<string> Devices
        {
            get => _devices;
            private set
            {
                _devices = value;
                OnStaticPropertyChanged();
            }
        }

        public static bool Connected => ConnectStates.Count > 0;

        public static Exception Error { get; private set; }

        public static ConnectType ConnType { get; private set; }

        public static void SetConnected(long id, bool value)
        {
            // add or remove the instance, this is done once regardless of the number of calls
            if (value)
            {
                var notAlreadyPresent = ConnectStates.TryAdd(id, true);

                if (Connected){ if (!SkyServer.IsMountRunning) {SkyServer.IsMountRunning = true;}}

                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Add|{id}|{notAlreadyPresent}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            else
            {
                var successfullyRemoved = ConnectStates.TryRemove(id, out _);

                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Remove|{id}|{successfullyRemoved}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static void DiscoverSerialDevices()
        {
            var list = new List<string>();
            var allPorts = SerialPort.GetPortNames();
            foreach (var port in allPorts)
            {
                if (string.IsNullOrEmpty(port)) continue;
                var portNumber = Strings.GetNumberFromString(port);
                if (!(portNumber >= 1)) continue;
                if (!list.Contains(port))
                {
                    list.Add(port);
                }
            }

            if (!list.Contains(SkySettings.Port))
            {
                list.Add(SkySettings.Port);
            }
            Devices = list;
        }

        public static void AddRemoteIp(string ip)
        {
            var list = Devices;
            if (list.Contains(ip)) return;
            list.Add(ip);
            Devices = list;
            SkySettings.Port = ip;
        }

        public static bool ConnectSerial
        {
            get => Serial?.IsOpen == true;
            internal set
            {
                try
                {
                    Serial?.Dispose();
                    Serial = null;
                    if(ConnType != ConnectType.None){ConnType = ConnectType.None;}

                    if (value)
                    {
                        var readTimeout = TimeSpan.FromMilliseconds(SkySettings.ReadTimeout);
                        if (SkySettings.Port.Contains("COM"))
                        {
                            var options = SerialOptions.DiscardNull
                                | (SkySettings.DtrEnable ? SerialOptions.DtrEnable : SerialOptions.None)
                                | (SkySettings.RtsEnable ? SerialOptions.RtsEnable : SerialOptions.None);

                            Serial = new GSSerialPort(
                                SkySettings.Port,
                                (int)SkySettings.BaudRate,
                                readTimeout,
                                SkySettings.HandShake,
                                Parity.None,
                                StopBits.One,
                                SkySettings.DataBits,
                                options);
                            ConnType = ConnectType.Com;
                        }
                        else
                        {
                            var endpoint = CreateIPEndPoint(SkySettings.Port);
                            Serial = new SerialOverUdpPort(endpoint, readTimeout);
                            ConnType = ConnectType.Wifi;
                        }
                        Serial?.Open();
                    }
                    OnStaticPropertyChanged();
                }
                catch (Exception ex)
                {
                    Error = ex;
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = Principles.HiResDateTime.UtcNow,
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Warning,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"{ex.Message}|{ex.InnerException?.Message}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                    Serial = null;
                    ConnType = ConnectType.None;
                }
            }
        }

        /// <summary>
        /// Handles IPv4 and IPv6 notation.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            var ep = endPoint.Split(':');
            if (ep.Length < 2) {throw new FormatException("Invalid endpoint format");}
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-address");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-address");
                }
            }

            return !int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port)
                ? throw new FormatException("Invalid port")
                : new IPEndPoint(ip, port);
        }

        /// <summary>
        /// Get a thread-safe, unique ID.
        /// </summary>
        /// <returns></returns>
        public static long GetId() => Interlocked.Increment(ref _idCount);
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
