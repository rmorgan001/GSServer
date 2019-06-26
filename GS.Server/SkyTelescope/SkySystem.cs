/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ASCOM.Utilities;
using GS.Shared;

namespace GS.Server.SkyTelescope
{
    public static class SkySystem
    {

        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Fields

        private static readonly object GetIdLockObj = new object();
        private static long _idCount;
        private static readonly ConcurrentDictionary<long, bool> ConnectStates;
        //public static Serial Serial;
        public static SerialPort Serial;

        #endregion Fields

        static SkySystem()
        {
            Enum.TryParse<MountType>(Properties.SkyTelescope.Default.Mount, true, out var eparse);
            Mount = eparse;

            ConnectStates = new ConcurrentDictionary<long, bool>();
                _idCount = 0;
                //TraceLogger = new TraceLogger("", "GS Server Sky Telescope Trace") { Enabled = TraceLogging };

        }

        #region Properties

        public static bool Connected => ConnectStates.Count > 0;

        public static void SetConnected(long id, bool value)
        {
            // add or remove the instance, this is done once regardless of the number of calls
            if (value)
            {
                var notAlreadyPresent = ConnectStates.TryAdd(id, true);

                if (Connected) if (!SkyServer.IsMountRunning) SkyServer.IsMountRunning = true; 

                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Add:{id},{notAlreadyPresent}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            else
            {
                var successfullyRemoved = ConnectStates.TryRemove(id, out value);

                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"Remove:{id},{successfullyRemoved}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static void CloseConnected()
        {
            if (ConnectStates.Count <= 0) return;
            foreach (var cons in ConnectStates)
            {
                SetConnected(cons.Key, false);
            }
        }

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
                            PortName = $"COM{ComPort}",
                            BaudRate = (int)BaudRate,
                            ReadTimeout = ReadTimeout,
                            StopBits = StopBits.One,
                            DataBits = DataBits,
                            DtrEnable = DtrEnable,
                            RtsEnable = RtsEnable,
                            Handshake = HandShake,
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
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Server,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"{ex.Message},{ex.InnerException?.Message}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    Serial = null;
                }

            }
        }

        private static bool DtrEnable => Properties.SkyTelescope.Default.DTREnable;

        private static bool RtsEnable => Properties.SkyTelescope.Default.RTSEnable;

        private static int DataBits => Properties.SkyTelescope.Default.DataBits;

        internal static int ComPort
        {
            get => Properties.SkyTelescope.Default.ComPort;
            set
            {
                if (ComPort == value) return;
                Properties.SkyTelescope.Default.ComPort = value;
            }
        }

        private static int ReadTimeout => Properties.SkyTelescope.Default.ReadTimeout;

        internal static SerialSpeed BaudRate
        {
            get
            {
                Enum.TryParse<SerialSpeed>(Properties.SkyTelescope.Default.BaudRate, true, out var eparse);
                return eparse;
            }
            set
            {
                if (BaudRate == value) return;
                Properties.SkyTelescope.Default.BaudRate = $"{value}";
            }
        }

        private static Handshake HandShake
        {
            get
            {
                Enum.TryParse<Handshake>(Properties.SkyTelescope.Default.HandShake, true, out var eparse);
                return eparse;
            }
        }

        public static string Version
        {
            get => Properties.SkyTelescope.Default.Version;
            set
            {
                if (Version == value) return;
                Properties.SkyTelescope.Default.Version = value;
            }
        }

        private static MountType _mount;
        public static MountType Mount
        {
            get => _mount;
            set
            {
                if (Mount == value) return;
                _mount = value;
                SkyServer.IsMountRunning = false;
                Properties.SkyTelescope.Default.Mount = $"{value}";  
            }
        }

        #endregion Properties

        #region Methods

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

        #endregion


    }
}
