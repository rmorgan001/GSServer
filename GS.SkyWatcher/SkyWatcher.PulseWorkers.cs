/*Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Threading;
using System.Threading.Tasks;

namespace GS.SkyWatcher
{
    public partial class SkyWatcher
    {
        private AxisPulseWorker _raPulseWorker;
        private AxisPulseWorker _decPulseWorker;

        /// <summary>
        /// Starts the right ascension and declination pulse workers and waits until both are ready.
        /// </summary>
        /// <remarks>If startup fails, stops the pulse workers and rethrows the exception.</remarks>
        internal void StartPulseWorkers()
        {
            try
            {
                _raPulseWorker = new AxisPulseWorker(AxisId.Axis1);
                _decPulseWorker = new AxisPulseWorker(AxisId.Axis2);
                _raPulseWorker.WaitUntilReady();
                _decPulseWorker.WaitUntilReady();
            }
            catch
            {
                StopPulseWorkers();
                throw;
            }
        }

        /// <summary>
        /// Requests the right ascension and declination pulse workers to stop, disposes them, and clears their
        /// references.
        /// </summary>
        internal void StopPulseWorkers()
        {
            _raPulseWorker?.RequestStop();
            _decPulseWorker?.RequestStop();
            _raPulseWorker?.Dispose();
            _decPulseWorker?.Dispose();
            _raPulseWorker = null;
            _decPulseWorker = null;
        }

        /// <summary>
        /// Gets the pulse worker for the specified axis.
        /// </summary>
        /// <param name="axis">The axis whose pulse worker is retrieved.</param>
        /// <returns>The pulse worker associated with <paramref name="axis"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified axis is not supported.</exception>
        /// <exception cref="MountControlException">The pulse workers are not running.</exception>
        private AxisPulseWorker GetPulseWorker(AxisId axis)
        {
            AxisPulseWorker worker;
            switch (axis)
            {
                case AxisId.Axis1:
                    worker = _raPulseWorker;
                    break;
                case AxisId.Axis2:
                    worker = _decPulseWorker;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            return worker ?? throw new MountControlException(ErrorCode.ErrQueueFailed, "Pulse workers are not running");
        }

        /// <summary>
        /// Processes pulse requests for an axis on a dedicated background worker.
        /// </summary>
        /// <remarks>Accepts one pending or active pulse at a time. Supports cancellation and waits for
        /// the worker to stop before releasing its resources.</remarks>
        private sealed class AxisPulseWorker : IDisposable
        {
            private const int BusyGraceMs = 50; // how long Submit waits for the previous pulse to finish unwinding (runs on the queue thread, keep short)
            private readonly AxisId _axis;
            private readonly object _sync = new object();
            private readonly BlockingCollection<PulseWorkItem> _requests = new BlockingCollection<PulseWorkItem>(1);
            private readonly CancellationTokenSource _stop = new CancellationTokenSource();
            private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
            private readonly Task _task;
            private bool _busy;
            private bool _stopping;
            private bool _disposed;

            /// <summary>
            /// Initializes a pulse worker for the specified axis and starts its background task.
            /// </summary>
            /// <param name="axis">The axis whose pulse operations are handled by the worker.</param>
            internal AxisPulseWorker(AxisId axis)
            {
                _axis = axis;

                _task = Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                    Run();
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            }

            /// <summary>
            /// Waits up to five seconds for the pulse worker to become ready.
            /// </summary>
            /// <exception cref="MountControlException">Thrown if the pulse worker does not become ready within five seconds.</exception>
            internal void WaitUntilReady()
            {
                if (!_ready.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new MountControlException(ErrorCode.ErrQueueFailed, $"{_axis} pulse worker failed to start within timeout");
                }
            }

            /// <summary>
            /// Queues a pulse operation for execution.
            /// </summary>
            /// <param name="execute">The operation to execute, which receives a cancellation token and a completion callback.</param>
            /// <param name="token">The token used to cancel the pulse operation.</param>
            /// <param name="startedEvent">The event signaled when the pulse operation starts.</param>
            /// <exception cref="MountControlException">The pulse cannot be queued because the worker is stopping, another pulse is
            /// pending or active, or the worker cannot accept the pulse.</exception>
            /// <param name="startFailed">Called with the reason when the pulse fails or is cancelled before it reaches the mount</param>
            internal void Submit(Action<CancellationToken, Action> execute, CancellationToken token, ManualResetEventSlim startedEvent, Action<Exception> startFailed = null)
            {
                lock (_sync)
                {
                    if (_stopping)
                    {
                        throw new MountControlException(ErrorCode.ErrQueueFailed, $"{_axis} pulse worker is stopping");
                    }
                    if (_busy)
                    {
                        // The previous pulse publishes IsPulseGuiding=false shortly before this worker marks itself
                        // idle, so a guider that starts the next pulse as soon as it sees false lands in that gap.
                        // Wait briefly for the worker to finish instead of rejecting the pulse.
                        var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * BusyGraceMs / 1000;
                        while (_busy && !_stopping)
                        {
                            var remainingMs = (int)((deadline - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency);
                            if (remainingMs <= 0) break;
                            Monitor.Wait(_sync, remainingMs);
                        }

                        if (_stopping)
                        {
                            throw new MountControlException(ErrorCode.ErrQueueFailed, $"{_axis} pulse worker is stopping");
                        }
                        if (_busy)
                        {
                            throw new MountControlException(ErrorCode.ErrQueueFailed, $"{_axis} pulse rejected: a pulse is already pending or active");
                        }
                    }
                    if (!_requests.TryAdd(new PulseWorkItem(execute, token, startedEvent, startFailed)))
                    {
                        throw new MountControlException(ErrorCode.ErrQueueFailed, $"{_axis} pulse worker could not accept the pulse");
                    }
                    _busy = true;
                }
            }

            /// <summary>
            /// Stops accepting queued requests and signals cancellation.
            /// </summary>
            internal void RequestStop()
            {
                lock (_sync)
                {
                    if (_stopping) return;
                    _stopping = true;
                    _requests.CompleteAdding();
                    _stop.Cancel();
                    Monitor.PulseAll(_sync); // release any Submit waiting for the worker to become idle
                }
            }

            /// <summary>
            /// Stops processing and releases the resources used by the pulse worker.
            /// </summary>
            /// <remarks>Waits for the worker task to complete before releasing resources. Repeated
            /// calls have no additional effect.</remarks>
            public void Dispose()
            {
                RequestStop();
                // Do not release the mount or serial connection while a pulse can still use it.
                _task.GetAwaiter().GetResult();
                lock (_sync)
                {
                    if (_disposed) return;
                    _disposed = true;
                    _requests.Dispose();
                    _stop.Dispose();
                    _ready.Dispose();
                }
            }

            /// <summary>
            /// Processes queued requests until the collection is completed, logging dispatch, cancellation, and failure
            /// events.
            /// </summary>
            private void Run()
            {
                _ready.Set();
                Log(MonitorType.Information, "Ready");
                foreach (var request in _requests.GetConsumingEnumerable())
                {
                    try
                    {
                        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, request.Token))
                        {
                            linked.Token.ThrowIfCancellationRequested();
                            var dispatchMs = (Stopwatch.GetTimestamp() - request.QueuedAt) * 1000.0 / Stopwatch.Frequency;
                            Log(MonitorType.Debug, $"DispatchWait:{dispatchMs:F3}ms");
                            request.Execute(linked.Token, request.SignalStarted);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log(MonitorType.Warning, "Pulse cancelled before completion");
                        // No effect once the pulse already reached the mount
                        request.FailStart(new OperationCanceledException($"{_axis} pulse cancelled before it was sent to the mount"));
                    }
                    catch (Exception ex)
                    {
                        Log(MonitorType.Error, $"Pulse failed|{ex}");
                        request.FailStart(ex);
                        if (SkyQueue.IsRunning && SkyQueue.IsSerialIoException(ex)) SkyQueue.RaiseSerialIoError(ex);
                    }
                    finally
                    {
                        lock (_sync)
                        {
                            _busy = false;
                            Monitor.PulseAll(_sync); // wake a Submit waiting in its grace period
                        }
                        request.SignalStarted();
                    }
                }
                Log(MonitorType.Information, "Stopped");
            }

            private void Log(MonitorType type, string message)
            {
                MonitorLog.LogToMonitor(new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Mount,
                    Type = type,
                    Method = "AxisPulseWorker",
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{_axis}|{message}"
                });
            }

            /// <summary>
            /// Represents a queued pulse operation and its execution state.
            /// </summary>
            private sealed class PulseWorkItem
            {
                private ManualResetEventSlim _startedEvent;
                private readonly Action<Exception> _startFailed;

                internal PulseWorkItem(Action<CancellationToken, Action> execute, CancellationToken token, ManualResetEventSlim startedEvent, Action<Exception> startFailed)
                {
                    Execute = execute;
                    Token = token;
                    _startedEvent = startedEvent;
                    _startFailed = startFailed;
                    QueuedAt = Stopwatch.GetTimestamp();
                }

                /// <summary>
                /// Reports a failure to the caller, but only if the pulse has not already been signalled as started.
                /// The reason is recorded before the started event is set so the waiting caller can read it.
                /// </summary>
                internal void FailStart(Exception reason)
                {
                    var startedEvent = Interlocked.Exchange(ref _startedEvent, null);
                    if (startedEvent == null) return;
                    try { _startFailed?.Invoke(reason); }
                    catch (Exception ex) { LogNotification($"Pulse failure callback threw|{ex.Message}"); }
                    try { startedEvent.Set(); }
                    catch (ObjectDisposedException ex) { LogNotification($"Pulse start notification arrived after the caller disposed it|{ex.Message}"); }
                }

                private static void LogNotification(string message)
                {
                    MonitorLog.LogToMonitor(new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.Telescope,
                        Category = MonitorCategory.Mount,
                        Type = MonitorType.Warning,
                        Method = "AxisPulseWorker",
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = message
                    });
                }

                internal Action<CancellationToken, Action> Execute { get; }
                internal CancellationToken Token { get; }
                internal long QueuedAt { get; }

                internal void SignalStarted()
                {
                    var startedEvent = Interlocked.Exchange(ref _startedEvent, null);
                    if (startedEvent == null) return;
                    try
                    {
                        startedEvent.Set();
                    }
                    catch (ObjectDisposedException ex)
                    {
                        MonitorLog.LogToMonitor(new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.Telescope,
                            Category = MonitorCategory.Mount,
                            Type = MonitorType.Warning,
                            Method = "AxisPulseWorker",
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Pulse start notification arrived after the caller disposed it|{ex.Message}"
                        });
                    }
                }
            }
        }
    }
}
