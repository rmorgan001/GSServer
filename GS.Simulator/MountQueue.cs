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
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Simulator
{
    public static class MountQueue
    {
        #region Fields

        private static BlockingCollection<IMountCommand> _commandBlockingCollection;
        private static Actions _actions;
        private static CancellationTokenSource _cts;
        private static Task _processingTask;
        private static bool _isInWarningState;
        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region properties

        public static bool IsRunning { get; private set; }
        private static long _id;
        public static long NewId => Interlocked.Increment(ref _id);

        private static bool _isPulseGuidingDec;
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

        private static bool _isPulseGuidingRa;
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

        private static double[] _steps;
        /// <summary>
        /// current micro steps, used to update SkyServer and UI
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
        public static void AddCommand(IMountCommand command)
        {
            if (!IsRunning || _cts.IsCancellationRequested) return;

            if (_commandBlockingCollection.TryAdd(command) == false)
            {
                throw new MountException(ErrorCode.ErrQueueFailed, $"Unable to Add Command {command.Id}, {command}");
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
        public static IMountCommand GetCommandResult(IMountCommand command)
        {
            try
            {
                if (!IsRunning || _cts?.IsCancellationRequested != false)
                {
                    var a = "Queue | IsRunning:" + IsRunning + "| IsCancel:" + _cts?.IsCancellationRequested;
                    if (command.Exception != null) { a += "| Ex:" + command.Exception.Message; }
                    var e = new MountException(ErrorCode.ErrQueueFailed, a);
                    command.Exception = e;
                    command.Successful = false;
                    return command;
                }

                // Wait for command to complete with 22 second timeout
                if (command.CompletionEvent.Wait(22000, _cts.Token))
                {
                    // Command completed - return it
                    return command;
                }

                // Timeout occurred
                var ex = new MountException(ErrorCode.ErrQueueFailed, $"Queue Read Timeout {command.Id}, {command}");
                command.Exception = ex;
                command.Successful = false;
                return command;
            }
            catch (OperationCanceledException)
            {
                command.Exception = new MountException(ErrorCode.ErrQueueFailed, "Operation cancelled");
                command.Successful = false;
                return command;
            }
            catch (Exception e)
            {
                command.Exception = e;
                command.Successful = false;
                return command;
            }
        }

        /// <summary>
        /// Process command queue
        /// </summary>
        /// <param name="command"></param>
        private static void ProcessCommandQueue(IMountCommand command)
        {
            // Declare variables outside try-catch so they're accessible in catch block
            bool diagnosticsEnabled = false;
            DateTime dequeuedAt = default;
            int queueDepth = 0;
            DateTime executionStart = default;
            string commandType = null;

            try
            {
                // Check once if diagnostic logging is enabled to avoid overhead
                diagnosticsEnabled = MonitorLog.InTypes(MonitorType.Debug);

                // Always capture basic metrics for Warning/Information detection (minimal overhead)
                dequeuedAt = HiResDateTime.UtcNow;
                queueDepth = _commandBlockingCollection.Count;

                if (diagnosticsEnabled)
                {
                    commandType = command.GetType().Name;
                }

                if (!IsRunning || _cts.IsCancellationRequested || !Actions.IsConnected)
                {
                    command.Exception = new MountException(ErrorCode.ErrQueueFailed, "Queue stopped or not connected");
                    command.Successful = false;
                }
                else
                {
                    var executionStart = HiResDateTime.UtcNow;

                    command.Execute(_actions);

                    // Calculate queue wait time for both diagnostic logging and performance monitoring
                    var queueWaitMs = (executionStart - dequeuedAt).TotalMilliseconds;

                    // Log diagnostic timing info only when Debug logging is enabled
                    if (diagnosticsEnabled)
                    {
                        var executionMs = (HiResDateTime.UtcNow - executionStart).TotalMilliseconds;

                        var diagItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Server,
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
                            Device = MonitorDevice.Server,
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
                            Device = MonitorDevice.Server,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Queue performance normal - QueueDepth:{queueDepth}|QueueWait:{queueWaitMs:F3}ms"
                        };
                        MonitorLog.LogToMonitor(infoItem);
                    }
                }
            }
            catch (Exception e)
            {
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
                        Device = MonitorDevice.Server,
                        Category = MonitorCategory.Mount,
                        Type = MonitorType.Debug,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"CmdId:{command.Id}|Type:{commandType ?? "Unknown"}|QueueWait:{queueWaitMs:F3}ms|Execution:{executionMs:F3}ms|Total:{(queueWaitMs + executionMs):F3}ms|QueueDepth:{queueDepth}|Success:False|Exception:{e.Message}"
                    };
                    MonitorLog.LogToMonitor(diagItem);
                }
            }
            finally
            {
                // Always signal completion - success or failure
                command.CompletionEvent.Set();
            }
        }

        public static void Start()
        {
            try
            {
                Stop();
                if (_cts == null) _cts = new CancellationTokenSource();
                var ct = _cts.Token;

                _actions = new Actions();
                _actions.InitializeAxes();
                _commandBlockingCollection = new BlockingCollection<IMountCommand>();
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
                        // Expected when ct is cancelled
                    }
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

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

            _actions?.Shutdown();
            _cts?.Dispose();
            _cts = null;
            _commandBlockingCollection?.Dispose();
            _commandBlockingCollection = null;
        }

        #endregion

        /// <summary>
        /// called from the setter property.  property name is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
