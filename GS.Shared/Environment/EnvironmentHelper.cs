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
using System.IO;
using System.Threading.Tasks;

namespace GS.Shared.EnvironmentInfo
{
    /// <summary>
    /// Convenience helper for environment logging with sensible defaults for GSServer
    /// </summary>
    public static class EnvironmentHelper
    {
        private const string DefaultLogPattern = "GSSEnvironment*.log";
        private const int DefaultKeepCount = 3;
        private const int DefaultTimeoutSeconds = 5;

        /// <summary>
        /// Log environment to the standard GSServer location in Documents\GSServer
        /// </summary>
        /// <returns>Path to the created log file, or null if logging failed</returns>
        public static async Task<string> LogToDefaultLocationAsync()
        {
            try
            {
                var logPath = GetDefaultLogPath();
                await EnvironmentLogger.LogEnvironmentAsync(logPath, DefaultTimeoutSeconds).ConfigureAwait(false);

                // Clean up old logs
                var logDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    EnvironmentLogger.CleanupOldLogs(logDir, DefaultLogPattern, DefaultKeepCount);
                }

                return logPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Log environment to the standard GSServer location (synchronous)
        /// </summary>
        /// <returns>Path to the created log file, or null if logging failed</returns>
        public static string LogToDefaultLocation()
        {
            try
            {
                var logPath = GetDefaultLogPath();
                EnvironmentLogger.LogEnvironmentSync(logPath, DefaultTimeoutSeconds);

                // Clean up old logs
                var logDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    EnvironmentLogger.CleanupOldLogs(logDir, DefaultLogPattern, DefaultKeepCount);
                }

                return logPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the default log file path for GSServer using Documents\GSServer
        /// </summary>
        /// <returns>Full path to log file</returns>
        public static string GetDefaultLogPath()
        {
            var baseDir = GsFile.GetLogPath();
            var fileName = $"GSSEnvironment{DateTime.Now:yyyy-MM-dd_HHmmss}.log";
            return Path.Combine(baseDir, fileName);
        }

        /// <summary>
        /// Get the directory where environment logs are stored (Documents\GSServer)
        /// </summary>
        /// <returns>Directory path</returns>
        public static string GetLogDirectory()
        {
            return GsFile.GetLogPath();
        }
    }
}
