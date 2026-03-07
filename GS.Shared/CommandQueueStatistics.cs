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
using System.Threading;

namespace GS.Shared
{
    /// <summary>
    /// Thread-safe statistics for command queue processing
    /// </summary>
    public class CommandQueueStatistics
    {
        private long _totalCommandsProcessed;
        private long _commandsSuccessful;
        private long _commandsFailed;
        private long _commandsTimedOut;
        private long _exceptionsHandled;

        /// <summary>
        /// Total number of commands that entered the processing queue
        /// </summary>
        public long TotalCommandsProcessed => Interlocked.Read(ref _totalCommandsProcessed);

        /// <summary>
        /// Number of commands that completed successfully
        /// </summary>
        public long CommandsSuccessful => Interlocked.Read(ref _commandsSuccessful);

        /// <summary>
        /// Number of commands that failed (excluding timeouts)
        /// </summary>
        public long CommandsFailed => Interlocked.Read(ref _commandsFailed);

        /// <summary>
        /// Number of commands that timed out waiting for completion
        /// </summary>
        public long CommandsTimedOut => Interlocked.Read(ref _commandsTimedOut);

        /// <summary>
        /// Number of exceptions caught during command processing
        /// </summary>
        public long ExceptionsHandled => Interlocked.Read(ref _exceptionsHandled);

        /// <summary>
        /// Resets all statistics to zero
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalCommandsProcessed, 0);
            Interlocked.Exchange(ref _commandsSuccessful, 0);
            Interlocked.Exchange(ref _commandsFailed, 0);
            Interlocked.Exchange(ref _commandsTimedOut, 0);
            Interlocked.Exchange(ref _exceptionsHandled, 0);
        }

        /// <summary>
        /// Increments the total commands processed counter
        /// </summary>
        public void IncrementTotalProcessed()
        {
            Interlocked.Increment(ref _totalCommandsProcessed);
        }

        /// <summary>
        /// Increments the successful commands counter
        /// </summary>
        public void IncrementSuccessful()
        {
            Interlocked.Increment(ref _commandsSuccessful);
        }

        /// <summary>
        /// Increments the failed commands counter
        /// </summary>
        public void IncrementFailed()
        {
            Interlocked.Increment(ref _commandsFailed);
        }

        /// <summary>
        /// Increments the timed out commands counter
        /// </summary>
        public void IncrementTimedOut()
        {
            Interlocked.Increment(ref _commandsTimedOut);
        }

        /// <summary>
        /// Increments the exceptions handled counter
        /// </summary>
        public void IncrementExceptions()
        {
            Interlocked.Increment(ref _exceptionsHandled);
        }

        /// <summary>
        /// Returns a formatted string with all statistics
        /// </summary>
        /// <returns>Human-readable statistics summary</returns>
        public override string ToString()
        {
            return $"Total:{TotalCommandsProcessed}|Success:{CommandsSuccessful}|Failed:{CommandsFailed}|TimedOut:{CommandsTimedOut}|Exceptions:{ExceptionsHandled}";
        }
    }
}
