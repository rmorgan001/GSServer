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
using ASCOM.Utilities;
using GS.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Server.Gps
{
    internal class GpsHardware
    {
        private readonly int _gpsPort;
        private readonly SerialSpeed _gpsSerialSpeed;
        private const int _readTimeout = 15;
        private CancellationTokenSource _ctsGps;
        private bool _gpsRunning;

        internal GpsHardware(int port, SerialSpeed serialSpeed)
        {
            _gpsPort = port;
            _gpsSerialSpeed = serialSpeed;
        }
        internal bool HasData { get; private set; }
        internal bool Rmc { get; set; }
        internal bool Gga { get; set; }
        private bool IsConnected { get; set; }
        internal bool GpsRunning
        {
            get => _gpsRunning;
            private set
            {
                _gpsRunning = value;
                if (value) return;
                _ctsGps?.Cancel();
                _ctsGps?.Dispose();
                _ctsGps = null;
            }
        }
        public void GpsOn()
        {
            GpsRunning = true;
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
        internal string NmeaTag { get; private set; }

        /// <summary>
        /// raw NMEA sentance
        /// </summary>
        internal string NmeaSentence { get; private set; }

        /// <summary>
        /// Date and time from the nema sentence
        /// </summary>
        internal DateTime TimeStamp { get; private set; }

        /// <summary>
        /// high res system utc date and time
        /// </summary>
        internal DateTime PcUtcNow { get; private set; }

        /// <summary>
        /// Difference from TimeStamp and PcUtcNow
        /// </summary>
        internal TimeSpan TimeSpan { get; private set; }

        /// <summary>
        /// Main async process
        /// </summary>
        private async void GpsLoopAsync()
        {
            try
            {
                if (_ctsGps == null) _ctsGps = new CancellationTokenSource();
                var ct = _ctsGps.Token;
                var task = Task.Run(() =>
                {
                    while (GpsRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            // ct.ThrowIfCancellationRequested();
                            GpsRunning = false;
                        }
                        else
                        {
                            ConnectSerial();
                            GpsRunning = false;
                            break;
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
                //throw;
            }
        }

        /// <summary>
        /// Serial connection to the gps device
        /// </summary>
        private void ConnectSerial()
        {
            var _serial = new Serial
            {
                Port = _gpsPort,
                Speed =  _gpsSerialSpeed,
                ReceiveTimeoutMs = 5000,
                StopBits = SerialStopBits.One,
                DataBits = 8,
                DTREnable = false,
                RTSEnable = false,
                Handshake = SerialHandshake.RequestToSendXonXoff,
                Parity = SerialParity.None,
            };

            try
            {
                _serial.Connected = true;
                IsConnected = _serial.Connected;
                ReadGpsData(_serial);
                _serial.Connected = false;
                _serial.Dispose();
            }
            catch (Exception)
            {
                GpsRunning = false;
                _serial.Connected = false;
                _serial.Dispose();
                throw;
            }

        }

        /// <summary>
        ///  Read Global Positioning Data
        /// </summary>
        /// <remarks>https://gpsd.gitlab.io/gpsd/NMEA.html#_rmc_recommended_minimum_navigation_information</remarks>
        /// <returns></returns>
        private void ReadGpsData(Serial _serial)
        {
            if (!Gga && !Rmc) return;
            if (!IsConnected) return;
            var _stopwatch = Stopwatch.StartNew();
            while (_stopwatch.Elapsed.Seconds < _readTimeout)
            {
                ClearProperties();
                HasData = false;
                PcUtcNow = Principles.HiResDateTime.UtcNow;
                var receivedData = _serial.ReceiveTerminated("\r\n");
                if (receivedData.Length <= 0) continue;
                if (!ValidateCheckSum(receivedData)) continue;
                var gpsDataArr = receivedData.Split(',');

                if (gpsDataArr[0].Length < 5) continue;
                var talkerid = gpsDataArr[0].Substring(1, 2);
                var code = gpsDataArr[0].Substring(3, 3);

                switch (code)
                {
                    case "GGA":
                        if (!Gga) break;
                        LogNmeaSentence(receivedData);
                        if (gpsDataArr.Length == 15)
                        {
                            ParseGga(talkerid, gpsDataArr);
                            if (CheckProperties())
                            {
                                HasData = true;
                                return;
                            }
                        }
                        break;
                    case "RMC":
                        if (!Rmc) break;
                        LogNmeaSentence(receivedData);
                        if (gpsDataArr.Length == 13)
                        {
                            ParseRmc(talkerid, gpsDataArr);
                            if (CheckProperties())
                            {
                                HasData = true;
                                return;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Write to Monitor the NMEA sentence before being parced
        /// </summary>
        /// <param name="sentence"></param>
        private void LogNmeaSentence(string sentence)
        {
            var monitorItem = new MonitorEntry
                { Datetime = PcUtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{sentence}" };
            MonitorLog.LogToMonitor(monitorItem);
            NmeaSentence = sentence;
        }

        /// <summary>
        /// Reset Properties
        /// </summary>
        private void ClearProperties()
        {
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
            NmeaTag = string.Empty;
            NmeaSentence = string.Empty;
            TimeStamp = new DateTime();
            PcUtcNow = new DateTime();
            TimeSpan = new TimeSpan(0);

        }

        /// <summary>
        /// Check if properties are loaded
        /// </summary>
        /// <returns></returns>
        private bool CheckProperties()
        {
            return Math.Abs(Latitude) > 0.0 && Math.Abs(Longitude) > 0.0 && NmeaTag != string.Empty;
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
        /// Parse the RMC sentence
        /// </summary>
        /// <example>$--RMC,hhmmss.ss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,xxxx,x.x,a,m,*hh CR LF></example>
        /// <param name="talkerid"></param>
        /// <param name="gpsDataArr"></param>
        private void ParseRmc(string talkerid, IReadOnlyList<string> gpsDataArr)
        {
            NmeaTag = gpsDataArr[0];

            var utctime = gpsDataArr[1];
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
            var utcdate = gpsDataArr[9];

            string timeformat;
            switch (talkerid)
            {
                case "GN":
                    timeformat = @"hhmmss\.ff";
                    break;
                case "GP":
                    timeformat = @"hhmmss\.fff";
                    break;
                default:
                    timeformat = @"hhmmss\.ff";
                    break;
            }

            TimeStamp = ConvertDateTime(utcdate, utctime, timeformat);
            TimeSpan = TimeStamp - PcUtcNow;
        }

        /// <summary>
        /// Parse the GGA sentence
        /// </summary>
        /// /// <example>$--GGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx*hh CR LF></example>
        /// <param name="talkerid"></param>
        /// <param name="gpsDataArr"></param>
        private void ParseGga(string talkerid, IReadOnlyList<string> gpsDataArr)
        {
            NmeaTag = gpsDataArr[0];

            var utctime = gpsDataArr[1];
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

            string timeformat;
            switch (talkerid)
            {
                case "GN":
                    timeformat = @"hhmmss\.ff";
                    break;
                case "GP":
                    timeformat = @"hhmmss\.fff";
                    break;
                default:
                    timeformat = @"hhmmss\.ff";
                    break;
            }

            TimeStamp = ConvertDateTime(null, utctime, timeformat);
            TimeSpan = TimeStamp - PcUtcNow;
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

        /// <summary>
        /// Convert found date and times to a timestamp
        /// </summary>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="timeformat"></param>
        /// <returns></returns>
        private DateTime ConvertDateTime(string date, string time, string timeformat)
        {
            try
            {
                var tmpdate = PcUtcNow.Date;
                var tmptime = PcUtcNow.TimeOfDay;
                if (date != null)
                {
                    const string format = @"ddMMyy";
                    if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpdate)) { }
                }

                if (time == null) { return tmpdate + tmptime; }

                if (TimeSpan.TryParseExact(time, timeformat, CultureInfo.InvariantCulture, TimeSpanStyles.None, out tmptime)) { }
                return tmpdate + tmptime;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message},{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                return PcUtcNow;
            }

        }
    }
}
