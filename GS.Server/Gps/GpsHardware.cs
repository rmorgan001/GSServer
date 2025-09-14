﻿/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.IO.Ports;

namespace GS.Server.Gps
{
    internal class GpsHardware
    {
        private readonly string _gpsPort;
        private readonly SerialSpeed _gpsSerialSpeed;
        private readonly int _readTimeout;
        private CancellationTokenSource _ctsGps;
        private bool _gpsRunning;

        internal GpsHardware(string port, SerialSpeed serialSpeed, int timeout)
        {
            _gpsPort = port;
            _gpsSerialSpeed = serialSpeed;
            _readTimeout = timeout;
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
            ConnectSerialPort();
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
        internal string NmEaTag { get; private set; }

        /// <summary>
        /// raw NmEa sentence
        /// </summary>
        internal string NmEaSentence { get; private set; }

        /// <summary>
        /// Date and time from the NmEa sentence
        /// </summary>
        internal DateTime TimeStamp { get; private set; }

        /// <summary>
        /// High Res system utc date and time
        /// </summary>
        internal DateTime PcUtcNow { get; private set; }

        /// <summary>
        /// Difference from TimeStamp and PcUtcNow
        /// </summary>
        internal TimeSpan TimeSpan { get; private set; }

        private void ConnectSerialPort()
        {
            var serial = new SerialPort()
            {
                PortName = _gpsPort,
                //PortName = "COM" + _gpsPort,
                BaudRate = (int)_gpsSerialSpeed,
                ReadTimeout = _readTimeout * 1000,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.RequestToSendXOnXOff,
                Parity = Parity.None,
            };

            try
            {
                serial.Open();
                IsConnected = serial.IsOpen;
                ReadGpsData(serial);
                serial.Close();
                serial.Dispose();
            }
            catch (Exception)
            {
                GpsRunning = false;
                serial.Close();
                serial.Dispose();
                throw;
            }

        }

        /// <summary>
        ///  Read Global Positioning Data
        /// </summary>
        /// <remarks>https://gpsd.gitlab.io/gpsd/NMEA.html#_rmc_recommended_minimum_navigation_information</remarks>
        /// <returns></returns>
        private void ReadGpsData(SerialPort serial)
        {
            if (!Gga && !Rmc) return;
            if (!IsConnected) return;
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < _readTimeout * 1000)
            {
                ClearProperties();
                HasData = false;
                PcUtcNow = Principles.HiResDateTime.UtcNow;
                var receivedData = serial.ReadLine();
                //var receivedData = "$GPGGA,010537,2934.2442,N,09816.2099,W,1,05,2.1,227.0,M,-22.2,M,,*76\r\n";
                if (receivedData.Length <= 0) continue;
                var gpsDataArr = receivedData.Split(',');
                if (gpsDataArr[0].Length < 6) continue;
                var code = gpsDataArr[0].Substring(3, 3);
                
                switch (code)
                {
                    case "GGA":
                        if (!Gga) break;
                        LogNmEaSentence(receivedData, true);
                        if (gpsDataArr.Length == 15)
                        {
                            if (!ValidateCheckSum(receivedData)) return;
                            ParseGga(gpsDataArr);
                            if (CheckProperties())
                            {
                                NmEaSentence = receivedData;
                                HasData = true;
                                return;
                            }
                        }
                        break;
                    case "RMC":
                        if (!Rmc) break;
                        LogNmEaSentence(receivedData,true);
                        if (gpsDataArr.Length == 13)
                        {
                            if (!ValidateCheckSum(receivedData)) return;
                            ParseRmc(gpsDataArr);
                            if (CheckProperties())
                            {
                                NmEaSentence = receivedData;
                                HasData = true;
                                return;
                            }
                        }
                        break;
                    default:
                        LogNmEaSentence(receivedData, false);
                        break;
                }
            }
        }

        /// <summary>
        /// Write to Monitor the NmEa sentence before being parsed
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="valid">Passed prechecks</param>
        private void LogNmEaSentence(string sentence, bool valid)
        {
            var terminated = sentence.Contains("\r\n");
            var monitorItem = new MonitorEntry
                { Datetime = PcUtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{valid}|{terminated}|{sentence}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Reset Properties
        /// </summary>
        private void ClearProperties()
        {
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
            NmEaTag = string.Empty;
            NmEaSentence = string.Empty;
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
            return Math.Abs(Latitude) > 0.0 && Math.Abs(Longitude) > 0.0 && NmEaTag != string.Empty;
        }

        /// <summary>
        /// Check for a complete NmEa sentence 
        /// </summary>
        /// <param name="receivedData"></param>
        /// <returns></returns>
        private bool ValidateCheckSum(string receivedData)
        {
            var checkSum = 0;
            var checkChar = Strings.GetTxtBetween(receivedData, "*", "\r");
            if (!string.IsNullOrEmpty(checkChar))
            {
                var strToCheck = Strings.GetTxtBetween(receivedData, "$", "*");
                foreach (var chrA in strToCheck)
                {
                    checkSum ^= Convert.ToByte(chrA);
                }
            }

            var final = checkSum.ToString("X2");
            var retBol = checkChar == final;
            if (retBol) return true;
            var monitorItem = new MonitorEntry
                { Datetime = PcUtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{receivedData}" };
            MonitorLog.LogToMonitor(monitorItem);
            return false;
        }

        /// <summary>
        /// Parse the RMC sentence
        /// </summary>
        /// <example>$--RMC,hhmmss.ss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,xxxx,x.x,a,m,*hh CR LF></example>
        /// <param name="gpsDataArr"></param>
        private void ParseRmc(IReadOnlyList<string> gpsDataArr)
        {
            try
            {
                NmEaTag = gpsDataArr[0];
                var utcTime = gpsDataArr[1];
                var lat = gpsDataArr[3];
                var ns = gpsDataArr[4];
                if (lat != null && ns != null)
                {
                    //Latitude = ConvertLatLong(lat, ns);
                    Latitude = NmeaToDecimal(lat, ns);
                }

                var lon = gpsDataArr[5];
                var ew = gpsDataArr[6];
                if (lon != null && ew != null)
                {
                    //Longitude = ConvertLatLong(lon, ew);
                    Longitude = NmeaToDecimal(lon, ew);
                }
                Altitude = 0;
                var utcDate = gpsDataArr[9];

                string timeFormat;
                if (string.IsNullOrEmpty(utcTime)) return;
                if (!utcTime.Contains("."))
                {
                    utcTime += ".00";
                }
                var utcArr = utcTime.Split('.');
                if (utcArr.Length != 2) return;
                switch (utcArr[1].Length)
                {
                    case 0:
                        return;
                    case 1:
                        timeFormat = @"hhmmss\.f";
                        break;
                    case 2:
                        timeFormat = @"hhmmss\.ff";
                        break;
                    case 3:
                        timeFormat = @"hhmmss\.fff";
                        break;
                    case 4:
                        timeFormat = @"hhmmss\.ffff";
                        break;
                    default:
                        return;
                }

                TimeStamp = ConvertDateTime(utcDate, utcTime, timeFormat);
                TimeSpan = TimeStamp - PcUtcNow;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
            
        }

        /// <summary>
        /// Parse the GGA sentence
        /// </summary>
        /// /// <example>$--GGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx*hh CR LF></example>
        /// <param name="gpsDataArr"></param>
        private void ParseGga(IReadOnlyList<string> gpsDataArr)
        {
            try
            {
                NmEaTag = gpsDataArr[0];
                var utcTime = gpsDataArr[1];
                var lat = gpsDataArr[2];
                var ns = gpsDataArr[3];
                if (lat != null && ns != null)
                {
                    //Latitude = ConvertLatLong(lat, ns);
                    Latitude = NmeaToDecimal(lat, ns);
                }

                var lon = gpsDataArr[4];
                var ew = gpsDataArr[5];
                if (lon != null && ew != null)
                {
                    //Longitude = ConvertLatLong(lon, ew);
                    Longitude = NmeaToDecimal(lon, ew);
                }

                double.TryParse(gpsDataArr[9], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d);
                Altitude = d;

                string timeFormat;
                if (string.IsNullOrEmpty(utcTime)) return;
                if (!utcTime.Contains("."))
                {
                    utcTime += ".00";
                }
                var utcArr = utcTime.Split('.');
                if (utcArr.Length != 2) return;
                switch (utcArr[1].Length)
                {
                    case 0:
                        return;
                    case 1:
                        timeFormat = @"hhmmss\.f";
                        break;
                    case 2:
                        timeFormat = @"hhmmss\.ff";
                        break;
                    case 3:
                        timeFormat = @"hhmmss\.fff";
                        break;
                    case 4:
                        timeFormat = @"hhmmss\.ffff";
                        break;
                    default:
                        return;
                }

                TimeStamp = ConvertDateTime(null, utcTime, timeFormat);
                TimeSpan = TimeStamp - PcUtcNow;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        ///// <summary>
        ///// Convert the Sentence data for lat or long
        ///// </summary>
        ///// <param name="num"></param>
        ///// <param name="dir"></param>
        ///// <returns></returns>
        //private static double ConvertLatLong(string num, string dir)
        //{
        //    try
        //    {
        //        const NumberStyles style = NumberStyles.AllowDecimalPoint;
        //        double.TryParse(num, style, CultureInfo.InvariantCulture, out var num1);
        //        num1 /= 100;
        //        var num2 = Math.Truncate(num1);
        //        var dec = num1 - Math.Truncate(num1);
        //        var intDec = dec * 1000000000;
        //        var num3 = (int)(intDec / 60);
        //        double.TryParse(num2 + "." + num3, style, CultureInfo.InvariantCulture, out var returnNumber);
        //        switch (dir.ToUpper())
        //        {
        //            case "S":
        //            case "W":
        //                return -returnNumber;
        //            case "N":
        //            case "E":
        //                return returnNumber;
        //            default:
        //                return 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
        //        MonitorLog.LogToMonitor(monitorItem);
        //        return 0;
        //    }

        //}

        /// <summary>
        /// Convert NMEA lat or long to decimal
        /// </summary>
        /// <param name="num"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static double NmeaToDecimal(string num, string dir)
        {
            try
            {
                const NumberStyles style = NumberStyles.AllowDecimalPoint;
                var success = double.TryParse(num, style, CultureInfo.InvariantCulture, out var num1);

                if (!success)
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server,
                        Category = MonitorCategory.Server, Type = MonitorType.Error,
                        Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"Failed Conversion|{num}|{dir}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);
                }

                var hemi = 1;
                switch (dir.ToUpper())
                {
                    case "S":
                    case "W":
                        hemi = -hemi;
                        break;
                    case "N":
                    case "E":
                        break;
                }
                var a = Math.Round((Convert.ToInt32(num1 / 100, CultureInfo.InvariantCulture) + (num1 - Convert.ToInt32(num1 / 100, CultureInfo.InvariantCulture) * 100) / 60) * hemi,5);
                return a;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                return 0;
            }

        }

        /// <summary>
        /// Convert found date and times to a timestamp
        /// </summary>
        /// <param name="date"></param>
        /// <param name="time"></param>
        /// <param name="timeFormat"></param>
        /// <returns></returns>
        private DateTime ConvertDateTime(string date, string time, string timeFormat)
        {
            try
            {
                var tmpDate = PcUtcNow.Date;
                var tmpTime = PcUtcNow.TimeOfDay;
                if (date != null)
                {
                    const string format = "ddMMyy";
                    if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpDate)) { }
                }

                if (time == null) { return tmpDate + tmpTime; }

                if (TimeSpan.TryParseExact(time, timeFormat, CultureInfo.InvariantCulture, TimeSpanStyles.None, out tmpTime)) { }
                return tmpDate + tmpTime;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                return PcUtcNow;
            }

        }
    }
}
