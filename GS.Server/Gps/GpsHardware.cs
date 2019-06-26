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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.Utilities;
using GS.Shared;

namespace GS.Server.Gps
{
    internal class GpsHardware
    {
        private readonly int _gpsPort;
        private const int _readTimeout = 15;
        private CancellationTokenSource _ctsGps;
        private bool _gpsRunning;

        internal GpsHardware(int port)
        {
            _gpsPort = port;
        }

        internal bool HasData { get; private set; }

        internal bool IsConnected { get; private set; }

        internal bool GpsRunning
        {
            get => _gpsRunning;
            set
            {
                _gpsRunning = value;
                if (!value)
                { 
                    _ctsGps?.Cancel();
                    _ctsGps?.Dispose();
                    _ctsGps = null;
                }
            }
        }

        public void GpsOn()
        {
            GpsRunning = false;
            GpsLoopAsync();
        }

        public void GpsOff()
        {
            GpsRunning = false;
        }


        /// <summary>
        /// Data read from the GPS 
        /// </summary>
        internal double Latitude { get; private set; }

        /// <summary>
        /// Data read from the GPS 
        /// </summary>
        internal double Longitude { get; private set; }

        /// <summary>
        /// Data read from the GPS 
        /// </summary>
        internal double Altitude { get; private set; }

        /// <summary>
        /// Data read from the GPS 
        /// </summary>
        internal string NmeaSentence { get; private set; }

        private async void GpsLoopAsync()
        {
            try
            {
                if (_ctsGps == null) _ctsGps = new CancellationTokenSource();
                var ct = _ctsGps.Token;
                var KeepRunning = true;
                var task = Task.Run(() =>
                {
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            // ct.ThrowIfCancellationRequested();
                            KeepRunning = false;
                        }
                        else
                        {
                            ConnectSerial();

                            if (HasData)
                            {
                                KeepRunning = false;
                            }
                            else
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }
                }, ct);
                await task;
                task.Wait(ct);
                GpsRunning = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                GpsRunning = false;
            }
        }

        /// <summary>
        /// Save properties to telescope hardware profile
        /// </summary>
        internal void SaveGpsData()
        {
            //TelescopeServer.Latitude = Latitude;
            //TelescopeServer.Longitude = Longitude;
            //if (Altitude > 0) TelescopeServer.Elevation = Altitude;
        }

        /// <summary>
        /// Serial connection to the gps device
        /// </summary>
        internal void ConnectSerial()
        {
            var _serial = new Serial
            {
                Port = _gpsPort,
                Speed = SerialSpeed.ps4800,
                ReceiveTimeoutMs = 5000,
                StopBits = SerialStopBits.One,
                DataBits = 8,
                DTREnable = false,
                RTSEnable = false,
                Handshake = SerialHandshake.RequestToSendXonXoff,
                Parity = SerialParity.None,
                Connected = true
            };
            IsConnected = _serial.Connected;
            ReadGpsData(_serial);
            _serial.Connected = false;
            _serial.Dispose();
        }

        /// <summary>
        ///  Read Global Positioning Data
        /// </summary>
        /// <returns></returns>
        private void ReadGpsData(Serial _serial)
        {
            HasData = false;
            if (!IsConnected) return;
            string[] gga = { };
            string[] rmc = { };
            var _stopwatch = new Stopwatch();
            _stopwatch.Start();
            while (_stopwatch.Elapsed.Seconds < _readTimeout)
            {
                var receivedData = _serial.ReceiveTerminated("\r\n");
                if (receivedData.Length <= 0) continue;
                if (!ValidateCheckSum(receivedData)) continue;
                var gpsDataArr = receivedData.Split(',');
                if (gpsDataArr[0] == "$GPGGA") gga = gpsDataArr;
                if (gpsDataArr[0] == "$GPRMC") rmc = gpsDataArr;
                if (gga.Length > 0) break;
            }
            _stopwatch.Reset();
            if (gga.Length > 0)
            {
                ParseGpgga(gga);
                HasData = true;
                return;
            }
            if (rmc.Length <= 0) return;
            ParseGprmc(rmc);
            HasData = true;
        }

        /// <summary>
        /// Check for a complete NMEA sentenence 
        /// </summary>
        /// <param name="receivedData"></param>
        /// <returns></returns>
        private static bool ValidateCheckSum(string receivedData)
        {
            var checkSum = 0;
            var checkChar = Strings.GetTxtBetween(receivedData, "*", "\r");
            var strToCheck = Strings.GetTxtBetween(receivedData, "$", "*");
            foreach (var chracter in strToCheck)
            {
                checkSum = checkSum ^ Convert.ToByte(chracter);
            }
            var final = checkSum.ToString("X2");
            return checkChar == final;
        }

        /// <summary>
        /// Parse the GPRMC sentence
        /// </summary>
        /// <param name="gpsDataArr"></param>
        private void ParseGprmc(IReadOnlyList<string> gpsDataArr)
        {
            var lat = gpsDataArr[3];
            var ns = gpsDataArr[4];
            if (lat != null && ns != null)
            {
                Latitude = ConvertLatLong(lat, ns);
            }

            var lon = gpsDataArr[5];
            var ew = gpsDataArr[6];
            if (lon != null && ew != null)
            {
                Longitude = ConvertLatLong(lon, ew);
            }
            Altitude = 0;
            NmeaSentence = "RMC";
        }

        /// <summary>
        /// Parse the GPGGA sentence
        /// </summary>
        /// <param name="gpsDataArr"></param>
        private void ParseGpgga(IReadOnlyList<string> gpsDataArr)
        {
            var lat = gpsDataArr[2];
            var ns = gpsDataArr[3];
            if (lat != null && ns != null)
            {
                Latitude = ConvertLatLong(lat, ns);
            }

            var lon = gpsDataArr[4];
            var ew = gpsDataArr[5];
            if (lon != null && ew != null)
            {
                Longitude = ConvertLatLong(lon, ew);
            }

            double.TryParse(gpsDataArr[9], out var d);
            Altitude = d;

            NmeaSentence = "GGA";
        }

        /// <summary>
        /// Convert the Sentence data for lat or long
        /// </summary>
        /// <param name="num"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static double ConvertLatLong(string num, string dir)
        {
            try
            {
                var num1 = Convert.ToDouble(num) / 100;
                var num2 = num1.ToString(CultureInfo.InvariantCulture).Split('.');
                var num3 = num2[0] + "." + (Convert.ToDouble(num2[1]) / 60).ToString("#####");
                var returnNumber = Convert.ToDouble(num3);
                switch (dir.ToUpper())
                {
                    case "S":
                    case "W":
                        return -returnNumber;
                    case "N":
                    case "E":
                        return returnNumber;
                    default:
                        return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }

        }
    }
}
