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
//using System.Diagnostics;
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
        private static SkyWatcher _skyWatcher;
        private static CancellationTokenSource _cts;
        private static Task _processingTask;
        private static bool _isInWarningState;
        private static ManualResetEventSlim _taskReadySignal;
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
        /// Gets the current session statistics for the sky command queue
        /// </summary>
        public static CommandQueueStatistics Statistics { get; private set; }

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
            if (!IsRunning || _cts.IsCancellationRequested) return;

            if (_commandBlockingCollection.TryAdd(command) == false)
            {
                throw new MountControlException(ErrorCode.ErrQueueFailed, $"Unable to Add Command {command.Id}, {command}");
            }
        }

        /// <summary>
        /// Mount data results
        /// </summary>
        /// <remarks>
        /// Waits for command completion using the command's embedded completion event
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public static ISkyCommand GetCommandResult(ISkyCommand command)
        {
            try
            {
                if (!IsRunning || _cts?.IsCancellationRequested != false)
                {
                    var a = "Queue | IsRunning:" + IsRunning + "| IsCancel:" + _cts?.IsCancellationRequested;
                    if (command.Exception != null) { a += "| Ex:" + command.Exception.Message; }
                    var e = new MountControlException(ErrorCode.ErrQueueFailed, a);
                    command.Exception = e;
                    command.Successful = false;
                    return command;
                }

                // Wait for command to complete with 40 second timeout
                if (command.CompletionEvent.Wait(40000, _cts.Token))
                {
                    // Command completed - return it
                    return command;
                }

                // Timeout occurred
                Statistics?.IncrementTimedOut();
                var ex = new MountControlException(ErrorCode.ErrQueueFailed, $"Queue Read Timeout {command.Id}, {command}");
                command.Exception = ex;
                command.Successful = false;
                return command;
            }
            catch (OperationCanceledException)
            {
                Statistics?.IncrementExceptions();
                command.Exception = new MountControlException(ErrorCode.ErrQueueFailed, "Operation cancelled");
                command.Successful = false;
                return command;
            }
            catch (Exception e)
            {
                Statistics?.IncrementExceptions();
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
            Statistics?.IncrementTotalProcessed();

            // Check once if diagnostic logging is enabled to avoid overhead
            var diagnosticsEnabled = MonitorLog.InTypes(MonitorType.Debug);

            //todo switch comment to turn on/off specific command types
            var commandTypesToLog = new[]{ "SkyAxisPulse"};
            //var commandTypesToLog = new string[]{};

            // Always capture basic metrics for Warning/Information detection (minimal overhead)
            var dequeuedAt = HiResDateTime.UtcNow;
            var queueDepth = _commandBlockingCollection.Count;

            // Only capture detailed data if diagnostics enabled
            string commandType = null;

            if (diagnosticsEnabled)
            {
                commandType = command.GetType().Name;
                // Check if command type should be logged
                var shouldLog = false;
                for (var i = commandTypesToLog.Length - 1; i >= 0; i--)
                {
                    if (commandType != commandTypesToLog[i]) continue;
                    shouldLog = true;
                    break;
                }
                if (!shouldLog) diagnosticsEnabled = false;
            }

            try
            {

                if (!IsRunning || _cts.IsCancellationRequested || _skyWatcher?.IsConnected != true)
                {
                    command.Exception = new MountControlException(ErrorCode.ErrQueueFailed, "Queue stopped or not connected");
                    command.Successful = false;
                    Statistics?.IncrementFailed();
                }
                else
                {
                    var executionStart = HiResDateTime.UtcNow;

                    command.Execute(_skyWatcher);

                    if (command.Successful)
                    {
                        Statistics?.IncrementSuccessful();
                    }
                    else
                    {
                        Statistics?.IncrementFailed();
                    }

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

                        ThreadPool.GetAvailableThreads(out var worker, out var io);
                        var threadMsg = $"|Worker threads:{worker:N0}|Asynchronous I/O threads:{io:N0}";
                        ThreadPool.GetMinThreads(out var minWorker, out var minIoc);       
                        threadMsg += $"|Min Worker threads:{minWorker:N0}|Min Asynchronous I/O threads:{minIoc:N0}";
                        ThreadPool.GetMaxThreads(out var maxWorker, out var portThreads);
                        threadMsg += $"|Max Worker threads:{maxWorker:N0}|Max completion port threads:{portThreads:N0}";


                        var diagnosticItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Debug,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"CmdId:{command.Id}|Type:{commandType}|QueueWait:{queueWaitMs:F3}ms|Execution:{executionMs:F3}ms|Total:{(queueWaitMs + executionMs):F3}ms|QueueDepth:{queueDepth}|Success:{command.Successful}|{threadMsg}"
                        };
                        MonitorLog.LogToMonitor(diagnosticItem);
                    }

                    // Check for performance degradation (always check, regardless of debug logging)
                    // Monitor record only created when state transition occurs
                    var isSlowOrDeep = queueDepth > 10 || queueWaitMs > 100.0;

                    switch (isSlowOrDeep)
                    {
                        case true when !_isInWarningState:
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
                            break;
                        }
                        case false when _isInWarningState:
                        {
                            _isInWarningState = false;
                            var infoItem = new MonitorEntry
                            {
                                Datetime = HiResDateTime.UtcNow,
                                Device = MonitorDevice.Telescope,
                                Category = MonitorCategory.Mount,
                                Type = MonitorType.Warning,
                                Method = MethodBase.GetCurrentMethod()?.Name,
                                Thread = Thread.CurrentThread.ManagedThreadId,
                                Message = $"Queue performance normal - QueueDepth:{queueDepth}|QueueWait:{queueWaitMs:F3}ms"
                            };
                            MonitorLog.LogToMonitor(infoItem);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                command.Exception = e;
                command.Successful = false;
                Statistics?.IncrementFailed();
                Statistics?.IncrementExceptions();

                // Log diagnostic timing info even on exception - only when Debug monitoring is enabled
                if (diagnosticsEnabled)
                {
                    var queueWaitMs = (dequeuedAt != default ? (dequeuedAt - command.CreatedUtc).TotalMilliseconds : 0);
                    var executionMs = (dequeuedAt != default ? (HiResDateTime.UtcNow - dequeuedAt).TotalMilliseconds : 0);

                    var diagnosticItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Mount,
                        Type = MonitorType.Debug,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"CmdId:{command.Id}|Type:{commandType ?? "Unknown"}|QueueWait:{queueWaitMs:F3}ms|Execution:{executionMs:F3}ms|Total:{(queueWaitMs + executionMs):F3}ms|QueueDepth:{queueDepth}|Success:False|Exception:{e.Message}"
                    };
                    MonitorLog.LogToMonitor(diagnosticItem);
                }
            }
            finally
            {
                // Always signal completion - success or failure
                command.CompletionEvent.Set();
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
                _commandBlockingCollection = new BlockingCollection<ISkyCommand>();
                _taskReadySignal = new ManualResetEventSlim(false);

                if (Statistics == null)
                {
                    Statistics = new CommandQueueStatistics();
                }
                Statistics.Reset();

                _processingTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        // Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        // Signal that background task is ready to consume commands
                        // ReSharper disable once AccessToDisposedClosure
                        _taskReadySignal?.Set();

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

                // Wait for background task to be ready to consume commands
                if (_taskReadySignal.Wait(TimeSpan.FromSeconds(5)))
                {
                    IsRunning = true;
                    // Pragmatic delay to ensure queue is fully operational
                    Thread.Sleep(100);
                }
                else
                {
                    // Background task failed to start - clean up
                    Stop();
                    throw new MountControlException(ErrorCode.ErrQueueFailed, 
                        "Background processing task failed to start within timeout");
                }

                _taskReadySignal?.Dispose();
                _taskReadySignal = null;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Exception:|{ex}" };
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

            _skyWatcher = null;
            _cts?.Dispose();
            _cts = null;
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