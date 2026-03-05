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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace GS.Shared.EnvironmentInfo
{
    /// <summary>
    /// Basic environment information - no WMI dependencies, fast and reliable
    /// </summary>
    internal static class EnvironmentInfo
    {
        /// <summary>
        /// Obscure sensitive text by replacing all characters except first and last with asterisks
        /// </summary>
        /// <param name="text">Text to obscure</param>
        /// <returns>Obscured text, or original if null/empty or less than 3 characters</returns>
        private static string ObscureText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 3)
            {
                return text;
            }

            var firstChar = text[0];
            var lastChar = text[text.Length - 1];
            var asterisks = new string('*', text.Length - 2);

            return $"{firstChar}{asterisks}{lastChar}";
        }

        /// <summary>
        /// Obscure username in a file path
        /// </summary>
        /// <param name="path">Full path potentially containing username</param>
        /// <returns>Path with username obscured, or original path if no username found</returns>
        private static string ObscurePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // Look for \Users\ pattern (case-insensitive)
            var usersIndex = path.IndexOf("\\Users\\", StringComparison.OrdinalIgnoreCase);
            if (usersIndex < 0)
            {
                return path; // No username pattern found
            }

            // Find the start of the username (after \Users\)
            var usernameStart = usersIndex + 7; // Length of "\Users\"

            // Find the end of the username (next \ or end of string)
            var nextSlash = path.IndexOf('\\', usernameStart);

            string username;
            if (nextSlash < 0)
            {
                // Username is at the end of the path
                username = path.Substring(usernameStart);
                return path.Substring(0, usernameStart) + ObscureText(username);
            }
            else
            {
                // Username is followed by more path
                username = path.Substring(usernameStart, nextSlash - usernameStart);
                return path.Substring(0, usernameStart) + ObscureText(username) + path.Substring(nextSlash);
            }
        }

        /// <summary>
        /// Log application information
        /// </summary>
        public static void LogBasicInfo(StreamWriter writer)
        {
            writer.WriteLine("--- Application ---");

            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                writer.WriteLine($"Name: {assembly.GetName().Name}");
                writer.WriteLine($"Version: {version}");
                writer.WriteLine($"File Version: {fileVersionInfo.FileVersion}");
                writer.WriteLine($"Product Version: {fileVersionInfo.ProductVersion}");
                writer.WriteLine($"Copyright: {fileVersionInfo.LegalCopyright}");
                writer.WriteLine($"Build Date: {File.GetLastWriteTime(assembly.Location):yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Location: {assembly.Location}");
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving application info: {ex.Message}");
            }

            writer.WriteLine();

            writer.WriteLine("--- Operating System ---");

            try
            {
                writer.WriteLine($"OS: {System.Environment.OSVersion}");
                writer.WriteLine($"Version: {System.Environment.OSVersion.Version}");
                writer.WriteLine($"Platform: {System.Environment.OSVersion.Platform}");
                writer.WriteLine($"Service Pack: {System.Environment.OSVersion.ServicePack}");
                writer.WriteLine($"64-bit OS: {System.Environment.Is64BitOperatingSystem}");
                writer.WriteLine($"Machine Name: {ObscureText(System.Environment.MachineName)}");
                writer.WriteLine($"User Domain: {ObscureText(System.Environment.UserDomainName)}");
                writer.WriteLine($"User Name: {ObscureText(System.Environment.UserName)}");
                writer.WriteLine($"System Directory: {System.Environment.SystemDirectory}");
                writer.WriteLine($"Uptime: {TimeSpan.FromMilliseconds(System.Environment.TickCount):dd\\:hh\\:mm\\:ss}");
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving OS info: {ex.Message}");
            }

            writer.WriteLine();

            writer.WriteLine("--- Runtime ---");

            try
            {
                writer.WriteLine($"CLR Version: {System.Environment.Version}");
                writer.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
                writer.WriteLine($"64-bit Process: {System.Environment.Is64BitProcess}");
                writer.WriteLine($"Processor Count: {System.Environment.ProcessorCount}");
                writer.WriteLine($"System Page Size: {System.Environment.SystemPageSize:N0} bytes");

                // Check admin privileges
                try
                {
                    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    var isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                    writer.WriteLine($"Running as Admin: {isAdmin}");
                }
                catch
                {
                    writer.WriteLine("Running as Admin: Unknown");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving runtime info: {ex.Message}");
            }

            writer.WriteLine();

            writer.WriteLine("--- Process ---");

            try
            {
                var process = Process.GetCurrentProcess();

                writer.WriteLine($"Process ID: {process.Id}");
                writer.WriteLine($"Process Name: {process.ProcessName}");
                writer.WriteLine($"Start Time: {process.StartTime:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Threads: {process.Threads.Count}");
                writer.WriteLine($"Handles: {process.HandleCount}");
                writer.WriteLine($"Working Set: {process.WorkingSet64 / (1024.0 * 1024):N2} MB");
                writer.WriteLine($"Private Memory: {process.PrivateMemorySize64 / (1024.0 * 1024):N2} MB");
                writer.WriteLine($"Virtual Memory: {process.VirtualMemorySize64 / (1024.0 * 1024):N2} MB");
                writer.WriteLine($"Peak Working Set: {process.PeakWorkingSet64 / (1024.0 * 1024):N2} MB");
                writer.WriteLine($"GC Memory: {GC.GetTotalMemory(false) / (1024.0 * 1024):N2} MB");
                writer.WriteLine($"GC Max Generation: {GC.MaxGeneration}");

                // Command line args
                var args = System.Environment.GetCommandLineArgs();
                if (args.Length > 0)
                {
                    writer.WriteLine($"Command Line Arguments: {args.Length}");
                    for (int i = 0; i < args.Length && i < 10; i++)
                    {
                        writer.WriteLine($"  [{i}]: {args[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving process info: {ex.Message}");
            }

            writer.WriteLine();

            writer.WriteLine("--- Culture & Locale ---");

            try
            {
                var culture = CultureInfo.CurrentCulture;
                var uiCulture = CultureInfo.CurrentUICulture;

                writer.WriteLine($"Current Culture: {culture.Name} ({culture.DisplayName})");
                writer.WriteLine($"UI Culture: {uiCulture.Name} ({uiCulture.DisplayName})");
                writer.WriteLine($"Installed UI Culture: {CultureInfo.InstalledUICulture.Name}");
                writer.WriteLine($"Time Zone: {TimeZoneInfo.Local.DisplayName}");
                writer.WriteLine($"UTC Offset: {TimeZoneInfo.Local.BaseUtcOffset}");
                writer.WriteLine($"DST Active: {TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now)}");
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving culture info: {ex.Message}");
            }

            writer.WriteLine();
        }

        /// <summary>
        /// Log WPF-specific environment information
        /// </summary>
        public static void LogWpfInfo(StreamWriter writer)
        {
            writer.WriteLine("--- WPF Environment ---");

            try
            {
                // Check if we're in a WPF application context
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        writer.WriteLine($"Primary Screen: {SystemParameters.PrimaryScreenWidth} x {SystemParameters.PrimaryScreenHeight}");
                        writer.WriteLine($"Virtual Screen: {SystemParameters.VirtualScreenWidth} x {SystemParameters.VirtualScreenHeight}");
                        writer.WriteLine($"Work Area: {SystemParameters.WorkArea}");

                        // DPI - different API in .NET Framework 4.7.2
                        var presentationSource = System.Windows.PresentationSource.FromVisual(Application.Current.MainWindow);
                        if (presentationSource?.CompositionTarget != null)
                        {
                            var dpiX = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M11;
                            var dpiY = 96.0 * presentationSource.CompositionTarget.TransformToDevice.M22;
                            writer.WriteLine($"DPI: {dpiX} x {dpiY} ({dpiX / 96.0:P0} scale)");
                        }

                        writer.WriteLine($"Border Width: {SystemParameters.BorderWidth}");
                        writer.WriteLine($"Caption Height: {SystemParameters.CaptionHeight}");
                        writer.WriteLine($"Wheel Scroll Lines: {SystemParameters.WheelScrollLines}");
                        writer.WriteLine($"Client Area Animation: {SystemParameters.ClientAreaAnimation}");
                        writer.WriteLine($"Menu Animation: {SystemParameters.MenuAnimation}");
                    });
                }
                else
                {
                    writer.WriteLine("Not running in WPF application context");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving WPF info: {ex.Message}");
            }

            writer.WriteLine();
        }

        /// <summary>
        /// Log paths and directories
        /// </summary>
        public static void LogPaths(StreamWriter writer)
        {
            writer.WriteLine("--- Paths ---");

            try
            {
                writer.WriteLine($"Current Directory: {ObscurePath(System.Environment.CurrentDirectory)}");
                writer.WriteLine($"Base Directory: {ObscurePath(AppDomain.CurrentDomain.BaseDirectory)}");
                writer.WriteLine($"Temp Path: {ObscurePath(Path.GetTempPath())}");
                writer.WriteLine($"AppData (Roaming): {ObscurePath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData))}");
                writer.WriteLine($"AppData (Local): {ObscurePath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData))}");
                writer.WriteLine($"My Documents: {ObscurePath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments))}");
                writer.WriteLine($"Program Files: {ObscurePath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))}");

                if (System.Environment.Is64BitOperatingSystem)
                {
                    writer.WriteLine($"Program Files (x86): {ObscurePath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86))}");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error retrieving paths: {ex.Message}");
            }

            writer.WriteLine();
        }

        /// <summary>
        /// Log drive information
        /// </summary>
        public static void LogDrives(StreamWriter writer)
        {
            writer.WriteLine("--- Drives ---");

            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady);

                foreach (var drive in drives)
                {
                    try
                    {
                        var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                        var totalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                        var pct = (freeGB / totalGB) * 100;

                        writer.WriteLine($"{drive.Name} ({drive.DriveType}): {freeGB:N2} GB free / {totalGB:N2} GB total ({pct:N1}%)");
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine($"{drive.Name}: Error - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error enumerating drives: {ex.Message}");
            }

            writer.WriteLine();
        }
    }
}
