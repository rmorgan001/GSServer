/* Copyright(C) 2019-2024 Rob Morgan (robert.morgan.e@gmail.com)
   Copyright(C) 2024 Andy Watson

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

// ReSharper disable RedundantAssignment
using GS.Principles;
using GS.Shared;
using System;
using System.Reflection;
using System.Threading;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Manage all Right Ascension and Declination offset tracking calculations for use by AltAz slewing and goto actions
    /// Ra and Dec are in external coords, convert to topo for Alt Az conversions and mount control
    /// </summary>
    internal static class SkyPredictor
    {
        /// <summary>
        /// Initialise Right Ascension, Declination, Rates and ReferenceTime to default values
        /// </summary>
        static SkyPredictor()
        {
            Reset();
        }

        private static double _ra;
        /// <summary>
        /// Right Ascension value at ReferenceTime
        /// </summary>
        public static double Ra
        {
            get => _ra;
            private set => _ra = value;
        }

        private static double _dec;
        /// <summary>
        /// Declination value at ReferenceTime
        /// </summary>
        public static double Dec
        {
            get => _dec;
            private set => _dec = value;
        }

        private static double _rateRa;
        /// <summary>
        /// Right Ascension rate used by predictor
        /// </summary>
        public static double RateRa
        {
            get => _rateRa;
            set
            {
                SetRaDecNow();
                _rateRa = value;
            }
        }

        private static double _rateDec;
        /// <summary>
        /// Declination rate used by predictor
        /// </summary>
        public static double RateDec
        {
            get => _rateDec;
            set
            {
                SetRaDecNow();
                _rateDec = value;
            }
        }

        /// <summary>
        /// ReferenceTime for Right Ascension and Declination, used for delta time calculation
        /// </summary>
        public static DateTime ReferenceTime { get; set; }

        /// <summary>
        /// Check for Right Ascension or Declination not set
        /// </summary>
        public static bool RaDecSet => !(Double.IsNaN(_ra) || Double.IsNaN(_dec));

        /// <summary>
        /// Check for Right Ascension or Declination Rates set
        /// </summary>
        public static bool RatesSet => (RateRa != 0 || RateDec != 0);

        /// <summary>
        /// Set Right Ascension, Declination, Rates and ReferenceTime to default values 
        /// </summary>
        public static void Reset()
        {
            Ra = Double.NaN;
            Dec = Double.NaN;
            RateRa = 0;
            RateDec = 0;
            ReferenceTime = DateTime.MaxValue;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Debug,
                Method = "P." + MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ReferenceTime:yyyy-mm-dd H:mm:ss}|{_ra}|{_dec}|{_rateRa}|{_rateDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Set Right Ascension, Declination, Rates and ReferenceTime ready for use
        /// </summary>
        /// <param name="ra">Right Ascension</param>
        /// <param name="dec">Declination</param>
        /// <param name="raRate">Right Ascension rate</param>
        /// <param name="decRate">Declination rate</param>
        public static void Set(double ra, double dec, double raRate, double decRate)
        {
            RateRa = raRate;
            RateDec = decRate;
            Ra = ra;
            Dec = dec;
            ReferenceTime = HiResDateTime.UtcNow;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Debug,
                Method = "P." + MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ReferenceTime:yyyy-mm-dd H:mm:ss}|{_ra}|{_dec}|{_rateRa}|{_rateDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        /// <summary>
        /// Set Right Ascension, Declination and ReferenceTime ready for use with rates unchanged
        /// </summary>
        /// <param name="ra">Right Ascension</param>
        /// <param name="dec">Declination</param>
        public static void Set(double ra, double dec)
        {
            Set(ra, dec, RateRa, RateDec);
        }

        /// <summary>
        /// Calculate and return Right Ascension, Declination at given time
        /// Internal values of Right Ascension, Declination and ReferenceTime are not changed
        /// </summary>
        /// <param name="time">Future time</param>
        /// <returns></returns>
        public static double[] GetRaDecAtTime(DateTime time)
        {
            double[] result = { Ra, Dec, };
            if (!Double.IsNaN(Ra) && !Double.IsNaN(Dec) && (ReferenceTime != DateTime.MaxValue))
                if (_rateRa == 0 && _rateDec == 0)
                {
                    ReferenceTime = HiResDateTime.UtcNow;
                }
                else
                {
                    var deltaTime = (time - ReferenceTime).TotalSeconds;
                    result[0] = Range.Range24(Ra + (deltaTime * _rateRa) / 15.0);
                    result[1] = Dec + deltaTime * _rateDec;
                }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Debug,
                Method = "P." + MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ReferenceTime:yyyy-mm-dd H:mm:ss}|{result[0]}|{result[1]}|{_ra}|{_dec}|{_rateRa}|{_rateDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            return result;
        }

        /// <summary>
        /// Calculate and return Right Ascension, Declination at given time
        /// Internal values of Right Ascension, Declination and ReferenceTime are not changed
        /// </summary>
        /// <param name="time">Future time</param>
        /// <param name="raOut">Right Ascension at future time</param>
        /// <param name="decOut">Declination at future time</param>
        public static void GetRaDecAtTime(DateTime time, out double raOut, out double decOut)
        {
            raOut = Double.NaN;
            decOut = Double.NaN;
            if (!Double.IsNaN(Ra) && !Double.IsNaN(Dec) && !(ReferenceTime == DateTime.MaxValue))
            {
                if (RateRa == 0 && RateDec == 0)
                {
                    ReferenceTime = HiResDateTime.UtcNow;
                }
                else
                {
                    var deltaTime = (time - ReferenceTime).TotalSeconds;
                    var deltaRaRate = (SkyServer.CurrentTrackingRate() - SkySettings.SiderealRate) * 3600;
                    raOut = Range.Range24(_ra + deltaTime * (_rateRa + deltaRaRate) / 15.0);
                    decOut = _dec + deltaTime * _rateDec;
                }
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Debug,
                Method = "P." + MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ReferenceTime:yyyy-mm-dd H:mm:ss}|{_ra}|{_dec}|{_rateRa}|{_rateDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Calculate and set Right Ascension and Declination at current time
        /// ReferenceTime is set to time now ready for tracking
        /// </summary>
        public static void SetRaDecNow()
        {
            if (!Double.IsNaN(Ra) && !Double.IsNaN(Dec) && !(ReferenceTime == DateTime.MaxValue))
            {
                var timeNow = HiResDateTime.UtcNow;
                var deltaTime = (timeNow - ReferenceTime).TotalSeconds;
                _ra += deltaTime * _rateRa / 15.0;
                _dec += deltaTime * _rateDec;
                ReferenceTime = timeNow;
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Debug,
                Method = "P." + MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{ReferenceTime:yyyy-mm-dd H:mm:ss}|{_ra}|{_dec}|{_rateRa}|{_rateDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

    }
}