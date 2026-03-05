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
using GS.Principles;
using GS.Shared;
using GS.Shared.Transport;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GS.SkyWatcher
{
    public static class SkyQueue
    {
        #region Fields

        private static BlockingCollection<ISkyCommand> _commandBlockingCollection;
        private static ConcurrentDictionary<long, (ISkyCommand command, ManualResetEventSlim waitHandle)> _resultsDictionary;
        private static SkyWatcher _skyWatcher;
        private static CancellationTokenSource _cts;
        private static Task _processingTask;
        private static int _cleanupCounter;
        private static bool _isInWarningState;
        public static event PropertyChangedEventHandler StaticPropertyChanged;

        private static long _id;
        private static bool _isPulseGuidingDec;
        private static bool _isPulseGuidingRa;
        private static double[] _steps;

        #endregion

        #region Properties

        /// <summary>
        /// Serial object
        /// </summary>
        internal static ISerialPort Serial { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        /// <summary>
        /// Custom Mount :s replacement
        /// </summary>
        internal static int[] CustomMount360Steps { get; private set; }
        /// <summary>
        /// Custom Mount :s replacement
        /// </summary>// Custom Mount :a replacement
        internal static double[] CustomRaWormSteps { get; private set; }
        /// <summary>
        /// IsRunning
        /// </summary>
        public static bool IsRunning { get; private set; }
        /// <summary>
        /// Locking id
        /// </summary>
        public static long NewId => Interlocked.Increment(ref _id);
        /// <summary>
        /// status for Dec Pulse
        /// </summary>
        public static bool IsPulseGuidingDec
        {
            get => _isPulseGuidingDec;
            set
            {
                _isPulseGuidingDec = value;
                OnStaticPropertyChanged();
            }
        }
        /// <summary>
        /// status for Dec Pulse
        /// </summary>
        public static bool IsPulseGuidingRa
        {
            get => _isPulseGuidingRa;
            set
            {
                _isPulseGuidingRa = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// current steps, main property used to update Server and UI
        /// </summary>
        public static double[] Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                OnStaticPropertyChanged();
            }
        }
        #endregion

        #region Queues

        /// <summary>
        /// Add a command to the blocking queue
        /// </summary>
        /// <param name="command"></param>
        public static void AddCommand(ISkyCommand command)
        {
            if (!IsRunning || _cts.IsCancellationRequested || _skyWatcher?.IsConnected != true) return;

            // Clean every 20 commands instead of every command
            if (Interlocked.Increment(ref _cleanupCounter) % 20 == 0)
            {
                CleanResults(40, 180);
            }

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
            if (!IsRunning || _cts.IsCancellationRequested || _skyWatcher?.IsConnected != true) return;
            if (_resultsDictionary.IsEmpty) return;
            var recordsCount = _resultsDictionary.Count;
            if (recordsCount == 0) return;
            if (count == 0 && seconds == 0)
            {
                foreach (var kvp in _resultsDictionary)
                {
                    kvp.Value.waitHandle?.Dispose();
                }
                _resultsDictionary.Clear();
                return;
            }

            if (recordsCount < count) return;
            var now = HiResDateTime.UtcNow;
            foreach (var result in _resultsDictionary)
            {
                if (result.Value.command == null) continue;
                if (result.Value.command.CreatedUtc.AddSeconds(seconds) >= now) continue;
                if (_resultsDictionary.TryRemove(result.Key, out var removed))
                {
                    removed.waitHandle?.Dispose();
                }
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
            try
            {
                if (!IsRunning || _cts?.IsCancellationRequested != false)
                {
                    var a = "Queue | IsRunning:" + IsRunning + "| IsCancel:" + _cts?.IsCancellationRequested + "| IsConnected:" + (_skyWatcher?.IsConnected == true);
                    if (command.Exception != null) { a += "| Ex:" + command.Exception.Message; }
                    var e = new MountControlException(ErrorCode.ErrQueueFailed, a);
                    command.Exception = e;
                    command.Successful = false;
                    return command;
                }

                var dict = _resultsDictionary;
                if (dict == null)
                {
                    var e = new MountControlException(ErrorCode.ErrQueueFailed, "Queue stopped");
                    command.Exception = e;
                    command.Successful = false;
                    return command;
                }

                // Fast path: check if already completed
                if (dict.TryGetValue(command.Id, out var existing) && existing.command != null)
                {
                    if (dict.TryRemove(command.Id, out var completed))
                    {
                        completed.waitHandle?.Dispose();
                        return completed.command;
                    }
                }

                // Not completed yet - register wait handle
                var waitHandle = new ManualResetEventSlim(false);
                if (!dict.TryAdd(command.Id, (null, waitHandle)))
                {
                    // Another thread registered - try to get the result
                    waitHandle.Dispose();
                    if (dict.TryRemove(command.Id, out var result))
                    {
                        result.waitHandle?.Dispose();
                        return result.command ?? command;
                    }
                    return command;
                }

                // Double-check: command might have completed between first check and registration
                if (dict.TryGetValue(command.Id, out var doubleCheck) && doubleCheck.command != null)
                {
                    if (dict.TryRemove(command.Id, out var completed))
                    {
                        waitHandle.Dispose();
                        completed.waitHandle?.Dispose();
                        return completed.command;
                    }
                }

                try
                {
                    // Wait for command to complete with 40 second timeout
                    if (waitHandle.Wait(40000, _cts.Token))
                    {
                        // Command completed - retrieve result
                        if (dict.TryRemove(command.Id, out var result))
                        {
                            return result.command ?? command;
                        }
                    }

                    // Timeout occurred
                    if (dict.TryRemove(command.Id, out _))
                    {
                        var ex = new MountControlException(ErrorCode.ErrQueueFailed, $"Queue Read Timeout {command.Id}, {command}");
                        command.Exception = ex;
                        command.Successful = false;
                    }
                    return command;
                }
                catch (OperationCanceledException)
                {
                    dict.TryRemove(command.Id, out _);
                    command.Exception = new MountControlException(ErrorCode.ErrQueueFailed, "Operation cancelled");
                    command.Successful = false;
                    return command;
                }
                finally
                {
                    waitHandle.Dispose();
                }
            }
            catch (Exception e)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command.Id}|{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                command.Exception = e;
                command.Successful = false;
                return command;
            }
        }

        /// <summary>
        /// Process command queue
        /// </summary>
        /// <param name="command"></param>
        private static void ProcessCommandQueue(ISkyCommand command)
        {
            // Check once if diagnostic logging is enabled to avoid overhead
            var diagnosticsEnabled = MonitorLog.InTypes(MonitorType.Debug);
            var commandTypesToLog = new string[] {"SkyAxisPulse"};

            // Always capture basic metrics for Warning/Information detection (minimal overhead)
            var dequeuedAt = HiResDateTime.UtcNow;
            var queueDepth = _commandBlockingCollection.Count;

            // Only capture detailed data if diagnostics enabled
            DateTime executionStart = default;
            string commandType = null;

            if (diagnosticsEnabled)
            {
                commandType = command.GetType().Name;
                // Check if command type should be logged
                bool shouldLog = false;
                for (int i = 0; i < commandTypesToLog.Length; i++)
                {
                    if (commandType == commandTypesToLog[i])
                    {
                        shouldLog = true;
                        break;
                    }
                }
                if (!shouldLog) diagnosticsEnabled = false;
            }

            try
            {
                if (!IsRunning || _cts.IsCancellationRequested)
                {
                    command.Exception = new MountControlException(ErrorCode.ErrQueueFailed, "Queue stopped");
                    command.Successful = false;
                }
                else if (_skyWatcher?.IsConnected != true)
                {
                    command.Exception = new MountControlException(ErrorCode.ErrQueueFailed, "Not connected");
                    command.Successful = false;
                }
                else
                {
                    executionStart = HiResDateTime.UtcNow;

                    command.Execute(_skyWatcher);

                    if (command.Exception != null)
                    {
                        var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command.Exception.Message}|{command.Exception.StackTrace}" };
                        MonitorLog.LogToMonitor(monitorItem);
                    }

                    // Calculate queue wait time for both diagnostic logging and performance monitoring
                    var queueWaitMs = (executionStart - dequeuedAt).TotalMilliseconds;

                    // Log diagnostic timing info only when Debug logging is enabled
                    if (diagnosticsEnabled)
                    {
                        var executionMs = (HiResDateTime.UtcNow - executionStart).TotalMilliseconds;

                        var diagItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Debug,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"CmdId:{command.Id}|Type:{commandType}|QueueWait:{queueWaitMs:F3}ms|Execution:{executionMs:F3}ms|Total:{(queueWaitMs + executionMs):F3}ms|QueueDepth:{queueDepth}|Success:{command.Successful}"
                        };
                        MonitorLog.LogToMonitor(diagItem);
                    }

                    // Check for performance degradation (always check, regardless of debug logging)
                    // Monitor record only created when state transition occurs
                    var isSlowOrDeep = queueDepth > 10 || queueWaitMs > 100.0;

                    if (isSlowOrDeep && !_isInWarningState)
                    {
                        _isInWarningState = true;
                        var warnItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Warning,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Queue performance degraded - QueueDepth:{queueDepth}|QueueWait:{queueWaitMs:F3}ms"
                        };
                        MonitorLog.LogToMonitor(warnItem);
                    }
                    else if (!isSlowOrDeep && _isInWarningState)
                    {
                        _isInWarningState = false;
                        var infoItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Queue performance normal - QueueDepth:{queueDepth}|QueueWait:{queueWaitMs:F3}ms"
                        };
                        MonitorLog.LogToMonitor(infoItem);
                    }
                }

                // Always store result if command has an ID, even if execution failed
                if (command.Id > 0)
                {
                    var dict = _resultsDictionary;
                    if (dict != null)
                    {
                        if (dict.TryGetValue(command.Id, out var entry))
                        {
                            // Wait handle already registered - update and signal
                            dict.TryUpdate(command.Id, (command, entry.waitHandle), entry);
                            entry.waitHandle?.Set();
                        }
                        else
                        {
                            // No waiter yet - store result for fast-path retrieval
                            dict.TryAdd(command.Id, (command, null));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command.Id}|{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                command.Exception = e;
                command.Successful = false;

                // Log diagnostic timing info even on exception - only when Debug monitoring is enabled
                if (diagnosticsEnabled)
                {
                    var queueWaitMs = (dequeuedAt != default ? (dequeuedAt - command.CreatedUtc).TotalMilliseconds : 0);
                    var executionMs = (dequeuedAt != default ? (HiResDateTime.UtcNow - dequeuedAt).TotalMilliseconds : 0);

                    var diagItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Mount,
                        Type = MonitorType.Debug,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"CmdId:{command.Id}|Type:{commandType ?? "Unknown"}|QueueWait:{queueWaitMs:F3}ms|Execution:{executionMs:F3}ms|Total:{(queueWaitMs + executionMs):F3}ms|QueueDepth:{queueDepth}|Success:False|Exception:{e.Message}"
                    };
                    MonitorLog.LogToMonitor(diagItem);
                }

                // Still store result even on exception
                if (command.Id > 0)
                {
                    var dict = _resultsDictionary;
                    if (dict != null)
                    {
                        if (dict.TryGetValue(command.Id, out var entry))
                        {
                            dict.TryUpdate(command.Id, (command, entry.waitHandle), entry);
                            entry.waitHandle?.Set();
                        }
                        else
                        {
                            dict.TryAdd(command.Id, (command, null));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Startup Queues
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="customMount360Steps"></param>
        /// <param name="customRaWormSteps"></param>
        /// <param name="lowVoltageEventHandler"></param>
        public static void Start(ISerialPort serial, int[] customMount360Steps, double[] customRaWormSteps, EventHandler lowVoltageEventHandler = null)
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{serial?.IsOpen}|{customMount360Steps}|{customRaWormSteps}" };
                MonitorLog.LogToMonitor(monitorItem);

                Serial = serial;
                CustomMount360Steps = customMount360Steps;
                CustomRaWormSteps = customRaWormSteps;
                Stop();
                if (_cts == null) _cts = new CancellationTokenSource();
                var ct = _cts.Token;

                _skyWatcher = new SkyWatcher();
                _skyWatcher.LowVoltageEvent += lowVoltageEventHandler;
                _resultsDictionary = new ConcurrentDictionary<long, (ISkyCommand command, ManualResetEventSlim waitHandle)>();
                _commandBlockingCollection = new BlockingCollection<ISkyCommand>();
                IsRunning = true;

                _processingTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        foreach (var command in _commandBlockingCollection.GetConsumingEnumerable(ct))
                        {
                            ProcessCommandQueue(command);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during shutdown
                    }
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{IsRunning}|{ex}" };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;

            // Signal completion to BlockingCollection
            _commandBlockingCollection?.CompleteAdding();

            _cts?.Cancel();

            // Wake up all waiting threads and dispose wait handles safely
            var dict = _resultsDictionary;
            if (dict != null)
            {
                // First pass: wake up all waiters
                foreach (var kvp in dict)
                {
                    kvp.Value.waitHandle?.Set();
                }

                // Brief delay to let threads wake up and clean up their entries
                Thread.Sleep(10);

                // Second pass: dispose all wait handles
                foreach (var kvp in dict)
                {
                    kvp.Value.waitHandle?.Dispose();
                }
            }

            // Wait for processing task to complete
            if (_processingTask != null)
            {
                try
                {
                    _processingTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Ignore cancellation exceptions
                }
                _processingTask = null;
            }

            _cts?.Dispose();
            _cts = null;
            _skyWatcher = null;
            _resultsDictionary = null;
            _commandBlockingCollection?.Dispose();
            _commandBlockingCollection = null;
        }

        /// <summary>
        /// called from the setter property.  property name is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}