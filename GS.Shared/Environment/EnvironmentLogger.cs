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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Shared.EnvironmentInfo
{
    /// <summary>
    /// Main environment logging coordinator - handles async execution, timeouts, and file management
    /// </summary>
    public static class EnvironmentLogger
    {
        /// <summary>
        /// Log environment information asynchronously with timeout protection
        /// </summary>
        /// <param name="logFilePath">Full path to log file</param>
        /// <param name="timeoutSeconds">Maximum time to wait for logging to complete</param>
        /// <returns>Task that completes when logging finishes or times out</returns>
        public static async Task LogEnvironmentAsync(string logFilePath, int timeoutSeconds = 5)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create cancellation token for timeout
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    await Task.Run(() => LogEnvironmentSync(logFilePath, cts.Token), cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                SafeWriteTimeoutMarker(logFilePath);
            }
            catch (Exception ex)
            {
                SafeWriteErrorMarker(logFilePath, ex);
            }
        }

        /// <summary>
        /// Log environment information synchronously (for blocking scenarios)
        /// </summary>
        /// <param name="logFilePath">Full path to log file</param>
        /// <param name="timeoutSeconds">Maximum time to wait</param>
        public static void LogEnvironmentSync(string logFilePath, int timeoutSeconds = 5)
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    LogEnvironmentSync(logFilePath, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                SafeWriteTimeoutMarker(logFilePath);
            }
            catch (Exception ex)
            {
                SafeWriteErrorMarker(logFilePath, ex);
            }
        }

        /// <summary>
        /// Internal sync logging with cancellation support
        /// </summary>
        private static void LogEnvironmentSync(string logFilePath, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(logFilePath, false))
            {
                writer.WriteLine("=== GSSERVER ENVIRONMENT LOG ===");
                writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine();

                // Fast section - no WMI, always succeeds
                EnvironmentInfo.LogBasicInfo(writer);
                writer.Flush();

                if (cancellationToken.IsCancellationRequested) return;

                // WPF section - requires UI context
                EnvironmentInfo.LogWpfInfo(writer);
                writer.Flush();

                if (cancellationToken.IsCancellationRequested) return;

                // Extended section - uses WMI, may be slow
                WmiEnvironmentInfo.LogWmiInfo(writer, cancellationToken);
                writer.Flush();

                if (cancellationToken.IsCancellationRequested) return;

                // Additional details
                EnvironmentInfo.LogPaths(writer);
                EnvironmentInfo.LogDrives(writer);

                writer.WriteLine("=== END LOG ===");
            }
        }

        /// <summary>
        /// Clean up old log files, keeping only the most recent
        /// </summary>
        /// <param name="directory">Directory containing log files</param>
        /// <param name="pattern">File pattern to match (e.g., "Environment_*.log")</param>
        /// <param name="keepCount">Number of recent files to keep</param>
        public static void CleanupOldLogs(string directory, string pattern, int keepCount = 10)
        {
            try
            {
                if (!Directory.Exists(directory)) return;

                var files = Directory.GetFiles(directory, pattern)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(keepCount);

                foreach (var file in files)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors - non-critical
            }
        }

        private static void SafeWriteTimeoutMarker(string logFilePath)
        {
            try
            {
                File.AppendAllText(logFilePath, 
                    $"{System.Environment.NewLine}[TIMEOUT] Logging operation timed out{System.Environment.NewLine}");
            }
            catch
            {
                // Best effort only
            }
        }

        private static void SafeWriteErrorMarker(string logFilePath, Exception ex)
        {
            try
            {
                File.AppendAllText(logFilePath, 
                    $"{System.Environment.NewLine}[ERROR] Logging failed: {ex.Message}{System.Environment.NewLine}");
            }
            catch
            {
                // Best effort only
            }
        }
    }
}
