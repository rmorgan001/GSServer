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
using System;
using System.Diagnostics;
using System.Threading;

namespace GS.Principles
{
    /// <summary>
    /// Windows 8 or Server 2012 and higher. All others return using System.Diagnostics.Stopwatch />.
    /// </summary>
    public static class HiResDateTime
    {
        private static readonly long maxIdle = TimeSpan.FromSeconds(10).Ticks;
        private const long TicksMultiplier = 1000 * TimeSpan.TicksPerMillisecond;
        private static readonly ThreadLocal<DateTime> startTime = new ThreadLocal<DateTime>(() => DateTime.UtcNow, false);
        private static readonly ThreadLocal<double> startTimestamp = new ThreadLocal<double>(() => Stopwatch.GetTimestamp(), false);

        /// <summary>
        /// High resolution supported
        /// Returns True on Windows 8 and Server 2012 and higher.
        /// </summary>
        private static bool IsPrecise { get; }

        /// <summary>
        /// Gets the datetime in UTC.
        /// </summary>
        public static DateTime UtcNow
        {
            get
            {
                if (IsPrecise)
                {
                    NativeMethods.GetSystemTimePreciseAsFileTime(out var preciseTime);
                    return DateTime.FromFileTimeUtc(preciseTime);
                }
                double endTimestamp = Stopwatch.GetTimestamp();
                var durationInTicks = (endTimestamp - startTimestamp.Value) / Stopwatch.Frequency * TicksMultiplier;
                if (!(durationInTicks >= maxIdle)) return startTime.Value.AddTicks((long)durationInTicks);
                startTimestamp.Value = Stopwatch.GetTimestamp();
                startTime.Value = DateTime.UtcNow;
                return startTime.Value;
            }
        }

        /// <summary>
        /// Creates an instance
        /// </summary>
        static HiResDateTime()
        {
            try
            {
                NativeMethods.GetSystemTimePreciseAsFileTime(out _);
                IsPrecise = true;
            }
            catch (EntryPointNotFoundException)
            {
                IsPrecise = false;
            }
        }
    }
}
