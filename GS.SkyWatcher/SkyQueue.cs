/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Shared;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GS.SkyWatcher
{
    public static class SkyQueue
    {
        #region Fields

        private static BlockingCollection<ISkyCommand> _commandBlockingCollection;
        private static ConcurrentDictionary<long, ISkyCommand> _resultsDictionary;
        private static SkyWatcher _skyWatcher;
        private static CancellationTokenSource _cts;

        #endregion

        #region Properties

        public static bool IsRunning { get; private set; }
        private static long _id;
        public static long NewId => Interlocked.Increment(ref _id);

        #endregion

        #region Queues

        /// <summary>
        /// Add a command to the blocking queue
        /// </summary>
        /// <param name="command"></param>
        public static void AddCommand(ISkyCommand command)
        {
            if (!IsRunning || _cts.IsCancellationRequested || !_skyWatcher.IsConnected) return;
            CleanResults(20, 120);
            if (_commandBlockingCollection.TryAdd(command) == false)
            {
                throw new MountControlException(ErrorCode.ErrQueueFailed, $"Unable to Add Command {command.Id}, {command}");
            }

        }

        /// <summary>
        /// Cleans up the results dictionary
        /// </summary>
        /// <param name="count"></param>
        /// <param name="seconds"></param>
        private static void CleanResults(int count, int seconds)
        {
            if (!IsRunning || _cts.IsCancellationRequested || !_skyWatcher.IsConnected) return;
            if (_resultsDictionary.IsEmpty) return;
            var recordscount = _resultsDictionary.Count;
            if (recordscount == 0) return;
            if (count == 0 && seconds == 0)
            {
                _resultsDictionary.Clear();
                return;
            }

            if (recordscount < count) return;
            var now = HiResDateTime.UtcNow;
            foreach (var result in _resultsDictionary)
            {
                if (result.Value.CreatedUtc.AddSeconds(seconds) >= now) continue;
                _resultsDictionary.TryRemove(result.Key, out _);
            }
        }

        /// <summary>
        /// Mount data results
        /// </summary>
        /// <remarks>
        /// There could be timing issues between this method and timeouts for commands reading mount data
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public static ISkyCommand GetCommandResult(ISkyCommand command)
        {
            if (!IsRunning || _cts.IsCancellationRequested || !_skyWatcher.IsConnected)
            {
                var e = new MountControlException(ErrorCode.ErrQueueFailed, "Queue not running");
                command.Exception = e;
                command.Successful = false;
                return command;
            }
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 22000)
            {
                if (_resultsDictionary == null) break;
                var success = _resultsDictionary.TryRemove(command.Id, out var result);
                if (success) return result;
                Thread.Sleep(1);
            }
            var ex = new MountControlException(ErrorCode.ErrQueueFailed, $"Unable to Find Results {command.Id}, {command}, {sw.Elapsed.TotalMilliseconds}");
            command.Exception = ex;
            command.Successful = false;
            return command;
        }

        /// <summary>
        /// Process command queue
        /// </summary>
        /// <param name="command"></param>
        private static void ProcessCommandQueue(ISkyCommand command)
        {
            try
            {
                if (!IsRunning || _cts.IsCancellationRequested || !_skyWatcher.IsConnected) return;
                command.Execute(_skyWatcher);
                if (command.Exception != null)
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command.Exception.Message}, {command.Exception.StackTrace}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
                if (command.Id <= 0) return;
                if (_resultsDictionary.TryAdd(command.Id, command) == false)
                {
                    throw new MountControlException(ErrorCode.ErrQueueFailed, $"Unable to post results {command.Id}, {command}");
                }
            }
            catch (Exception e)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command.Id},{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                command.Exception = e;
                command.Successful = false;
            }
        }

        /// <summary>
        /// Startup Queues
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="customMount360Steps"></param>
        /// <param name="customRaWormSteps"></param>
        public static void Start(SerialPort serial, int[] customMount360Steps, double[] customRaWormSteps)
        {
            Stop();
            if (_cts == null) _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _skyWatcher = new SkyWatcher(serial, customMount360Steps,  customRaWormSteps);
            _resultsDictionary = new ConcurrentDictionary<long, ISkyCommand>();
            _commandBlockingCollection = new BlockingCollection<ISkyCommand>();

            Task.Factory.StartNew(() =>
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var command in _commandBlockingCollection.GetConsumingEnumerable())
                    {
                        ProcessCommandQueue(command);
                    }
                }
            }, ct);

            IsRunning = true;
        }

        /// <summary>
        /// Stop
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _skyWatcher = null;
            _resultsDictionary = null;
            _commandBlockingCollection = null;
        }

        #endregion
    }
}
