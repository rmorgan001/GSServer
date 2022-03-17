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
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GS.Server.SkyTelescope
{
    public static class SkySystem
    {
        public static event PropertyChangedEventHandler StaticPropertyChanged;
        private static readonly object GetIdLockObj = new object();
        private static long _idCount;
        private static readonly ConcurrentDictionary<long, bool> ConnectStates;
        public static SerialPort Serial;

        static SkySystem()
        {
            ConnectStates = new ConcurrentDictionary<long, bool>();
            _idCount = 0;
        }

        public static bool Connected => ConnectStates.Count > 0;

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

        //public static void CloseConnected()
        //{
        //    if (ConnectStates.Count <= 0) return;
        //    foreach (var cons in ConnectStates)
        //    {
        //        SetConnected(cons.Key, false);
        //    }
        //}

        public static bool ConnectSerial
        {
            get => Serial != null && Serial.IsOpen;
            internal set
            {
                try
                {
                    if (value)
                    {
                        if (Serial != null)
                        {
                            if (Serial.IsOpen)
                            {
                                Serial.Close();
                            }
                            Serial.Dispose();
                            Serial = null;
                        }

                        Serial = new SerialPort
                        {
                            PortName = $"COM{SkySettings.ComPort}",
                            BaudRate = (int)SkySettings.BaudRate,
                            ReadTimeout = SkySettings.ReadTimeout,
                            StopBits = StopBits.One,
                            DataBits = SkySettings.DataBits,
                            DtrEnable = SkySettings.DtrEnable,
                            RtsEnable = SkySettings.RtsEnable,
                            Handshake = SkySettings.HandShake,
                            Parity = Parity.None,
                            DiscardNull = true,
                        };
                        Serial.Open();
                    }
                    else
                    {
                        if (Serial != null)
                        {
                            if (Serial.IsOpen)
                            {
                                Serial.Close();
                            }
                            Serial.Dispose();
                            Serial = null;
                        }
                    }
                    OnStaticPropertyChanged();
                }
                catch (Exception ex)
                {
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
                }

            }
        }

        public static long GetId()
        {
            lock (GetIdLockObj)
            {
                Interlocked.Increment(ref _idCount); // Increment the counter in a threadsafe fashion
                return _idCount;
            }
        }

        /// <summary>
        /// called from the setter property.  Used to update UI elements.  propertyname is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
