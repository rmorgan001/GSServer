/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.IO;
using System.Management;
using System.Threading;

namespace GS.Shared.EnvironmentInfo
{
    /// <summary>
    /// Extended environment information using WMI - may be slow or fail
    /// </summary>
    internal static class WmiEnvironmentInfo
    {
        private const int DefaultQueryTimeoutSeconds = 2;

        /// <summary>
        /// Log WMI-based environment information with timeout protection
        /// </summary>
        public static void LogWmiInfo(StreamWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteLine("--- Extended Information (WMI) ---");

            if (cancellationToken.IsCancellationRequested)
            {
                writer.WriteLine("Cancelled before WMI queries");
                writer.WriteLine();
                return;
            }

            // Operating System Details
            QueryWmi(writer, "Operating System Details",
                "SELECT Caption, Version, BuildNumber, OSArchitecture, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem",
                obj =>
                {
                    writer.WriteLine($"OS Caption: {obj["Caption"]}");
                    writer.WriteLine($"OS Version: {obj["Version"]}");
                    writer.WriteLine($"OS Build: {obj["BuildNumber"]}");
                    writer.WriteLine($"OS Architecture: {obj["OSArchitecture"]}");

                    if (obj["TotalVisibleMemorySize"] != null)
                    {
                        var totalMemoryGB = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / (1024.0 * 1024);
                        writer.WriteLine($"Total Visible Memory: {totalMemoryGB:N2} GB");
                    }

                    if (obj["FreePhysicalMemory"] != null)
                    {
                        var freeMemoryGB = Convert.ToInt64(obj["FreePhysicalMemory"]) / (1024.0 * 1024);
                        writer.WriteLine($"Free Physical Memory: {freeMemoryGB:N2} GB");
                    }
                },
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                writer.WriteLine("Cancelled during WMI queries");
                writer.WriteLine();
                return;
            }

            // Computer System (includes total physical memory)
            QueryWmi(writer, "Computer System",
                "SELECT TotalPhysicalMemory, Manufacturer, Model FROM Win32_ComputerSystem",
                obj =>
                {
                    if (obj["TotalPhysicalMemory"] != null)
                    {
                        var totalMemoryGB = Convert.ToInt64(obj["TotalPhysicalMemory"]) / (1024.0 * 1024 * 1024);
                        writer.WriteLine($"Total Physical Memory: {totalMemoryGB:N2} GB");
                    }

                    writer.WriteLine($"Manufacturer: {obj["Manufacturer"]}");
                    writer.WriteLine($"Model: {obj["Model"]}");
                },
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                writer.WriteLine("Cancelled during WMI queries");
                writer.WriteLine();
                return;
            }

            // Processor Information
            QueryWmi(writer, "Processor",
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, CurrentClockSpeed FROM Win32_Processor",
                obj =>
                {
                    writer.WriteLine($"CPU: {obj["Name"]}");
                    writer.WriteLine($"Physical Cores: {obj["NumberOfCores"]}");
                    writer.WriteLine($"Logical Processors: {obj["NumberOfLogicalProcessors"]}");
                    writer.WriteLine($"Max Clock Speed: {obj["MaxClockSpeed"]} MHz");

                    if (obj["CurrentClockSpeed"] != null)
                    {
                        writer.WriteLine($"Current Clock Speed: {obj["CurrentClockSpeed"]} MHz");
                    }
                },
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                writer.WriteLine("Cancelled during WMI queries");
                writer.WriteLine();
                return;
            }

            // Video Controller (optional, sometimes slow)
            QueryWmi(writer, "Video Controller",
                "SELECT Name, AdapterRAM, DriverVersion, VideoModeDescription FROM Win32_VideoController",
                obj =>
                {
                    writer.WriteLine($"Video Card: {obj["Name"]}");

                    if (obj["AdapterRAM"] != null)
                    {
                        var ramMB = Convert.ToInt64(obj["AdapterRAM"]) / (1024 * 1024);
                        writer.WriteLine($"Video RAM: {ramMB:N0} MB");
                    }

                    if (obj["DriverVersion"] != null)
                    {
                        writer.WriteLine($"Driver Version: {obj["DriverVersion"]}");
                    }

                    if (obj["VideoModeDescription"] != null)
                    {
                        writer.WriteLine($"Video Mode: {obj["VideoModeDescription"]}");
                    }
                },
                cancellationToken,
                firstResultOnly: true);

            writer.WriteLine();
        }

        /// <summary>
        /// Execute a WMI query with error handling and timeout
        /// </summary>
        private static void QueryWmi(
            StreamWriter writer,
            string description,
            string query,
            Action<ManagementObject> processResult,
            CancellationToken cancellationToken,
            bool firstResultOnly = false)
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    // Set timeout for the query
                    searcher.Options.Timeout = TimeSpan.FromSeconds(DefaultQueryTimeoutSeconds);

                    var results = searcher.Get();
                    var found = false;

                    foreach (ManagementObject obj in results)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            writer.WriteLine($"{description}: Cancelled");
                            return;
                        }

                        processResult(obj);
                        found = true;

                        if (firstResultOnly) break;
                    }

                    if (!found)
                    {
                        writer.WriteLine($"{description}: No data returned");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                writer.WriteLine($"{description}: Access Denied");
                writer.WriteLine($"  {ex.Message}");
            }
            catch (ManagementException ex)
            {
                writer.WriteLine($"{description}: WMI Error");
                writer.WriteLine($"  {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                writer.WriteLine($"{description}: Timeout");
                writer.WriteLine($"  {ex.Message}");
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{description}: Failed");
                writer.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
