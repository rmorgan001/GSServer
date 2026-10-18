using GS.Principles;
using GS.Server.Main;
using GS.Server.Model3D;
using GS.Server.SkyTelescope;
using GS.Server.Windows;
using GS.Shared;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GS.Server.Helpers
{
    /// <summary>
    /// Coordinates the UI state during power suspend and resume events to ensure that the application restores its 
    /// previous state correctly after a power cycle.
    /// </summary>
    internal sealed class PowerUiCoordinator : IDisposable
    {
        private readonly object _stateLock = new object();
        private bool _snapshotPending;
        private bool _restoreQueued;
        private FrontGraphic _savedFrontGraphic = FrontGraphic.None;
        private bool _modelWindowWasOpen;
        private bool _model3DTabWasActive;
        private static readonly TimeSpan GraphicsResumeDelay = TimeSpan.FromSeconds(10);
        private Stopwatch _resumeElapsed;
        private CancellationTokenSource _restoreCancellation;
        private int _powerCycle;
        private bool _disposed;
        private bool _shutdownRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerUiCoordinator"/> class and subscribes to the power resume event.
        /// </summary>
        public PowerUiCoordinator()
        {
            SkyServer.PowerResumeHandled += SkyServer_PowerResumeHandled;
            Application.Current.Exit += Application_Exit;
        }

        /// <summary>
        /// Handles the power suspending event by capturing the current UI state on the UI thread.
        /// </summary>
        public void HandlePowerSuspending()
        {
            lock (_stateLock)
            {
                CancelPendingRestore();
                _resumeElapsed = null;
            }
            ExecuteOnUiThread(CaptureSuspendStateOnUiThread, MethodBase.GetCurrentMethod()?.Name);
        }

        /// <summary>
        /// Starts tracking the delay after the system resumes from a power event.
        /// </summary>
        public void HandlePowerResumed()
        {
            lock (_stateLock)
            {
                if (_disposed || _shutdownRequested) return;
                CancelPendingRestore();
                _resumeElapsed = Stopwatch.StartNew();
            }
            LogMonitor(MonitorType.Information, nameof(HandlePowerResumed), "GraphicsResumeDelayStarted|MinimumDelayMs:5000");
        }

        /// <summary>
        /// Cancels delayed restoration and restores the saved graphic setting during shutdown.
        /// </summary>
        public void RestorePendingStateOnShutdown()
        {
            FrontGraphic frontGraphic;
            var shouldRestore = false;

            lock (_stateLock)
            {
                CancelPendingRestore();
                _resumeElapsed = null;
                if (_snapshotPending)
                {
                    frontGraphic = _savedFrontGraphic;
                    _snapshotPending = false;
                    _restoreQueued = false;
                    shouldRestore = true;
                }
                else
                {
                    frontGraphic = FrontGraphic.None;
                }
            }

            if (!shouldRestore)
            {
                return;
            }

            try
            {
                if (SkySettings.FrontGraphic != frontGraphic)
                {
                    SkySettings.FrontGraphic = frontGraphic;
                }
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, MethodBase.GetCurrentMethod()?.Name, $"RestoreFrontGraphicOnShutdownFailed|{ex.Message}");
            }
        }

        public void Dispose()
        {
            lock (_stateLock)
            {
                _disposed = true;
                CancelPendingRestore();
            }
            SkyServer.PowerResumeHandled -= SkyServer_PowerResumeHandled;
            Application.Current.Exit -= Application_Exit;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// Cancels any pending restore operation and clears its queued state.
        /// </summary>
        /// <remarks>Invalidates already queued dispatcher work. Called under _stateLock; the generation also 
        /// invalidates already queued dispatcher work.</remarks>
        private void CancelPendingRestore()
        {
            _powerCycle++;
            _restoreCancellation?.Cancel();
            _restoreCancellation?.Dispose();
            _restoreCancellation = null;
            _restoreQueued = false;
        }

        /// <summary>
        /// Schedules UI restoration after server recovery and the minimum graphics resume delay.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SkyServer_PowerResumeHandled(object sender, EventArgs e)
        {
            lock (_stateLock)
            {
                if (_disposed || _shutdownRequested || !_snapshotPending || _restoreQueued || _resumeElapsed == null)
                {
                    return;
                }

                _restoreQueued = true;
                _restoreCancellation = new CancellationTokenSource();
                var remaining = GraphicsResumeDelay - _resumeElapsed.Elapsed;
                LogMonitor(MonitorType.Information, nameof(SkyServer_PowerResumeHandled),
                    $"GraphicsRecoveryHandled|RemainingDelayMs:{Math.Max(0, remaining.TotalMilliseconds):0}");
                _ = RestoreAfterDelayAsync(remaining, _powerCycle, _restoreCancellation.Token);
            }
        }

        /// <summary>
        /// Restores the graphics suspend state after the specified delay if the power cycle is still current.
        /// </summary>
        /// <param name="remaining">The time to wait before attempting restoration.</param>
        /// <param name="powerCycle">The power-cycle identifier used to verify that restoration is still valid.</param>
        /// <param name="cancellationToken">The token used to cancel the delay or restoration.</param>
        /// <returns>A task that represents the asynchronous restoration operation.</returns>
        private async Task RestoreAfterDelayAsync(TimeSpan remaining, int powerCycle, CancellationToken cancellationToken)
        {
            try
            {
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay((int)Math.Ceiling(remaining.TotalMilliseconds), cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.HasShutdownStarted)
                {
                    throw new InvalidOperationException("Graphics restoration dispatcher is unavailable or shutting down");
                }
                await dispatcher.InvokeAsync(() =>
                {
                    lock (_stateLock)
                    {
                        if (_disposed || _shutdownRequested || powerCycle != _powerCycle || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        RestoreSuspendStateOnUiThread();
                    }
                }).Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogMonitor(MonitorType.Information, nameof(RestoreAfterDelayAsync), "GraphicsRestoreCancelled");
            }
            catch (Exception ex)
            {
                await ShutdownAfterRestoreFailureAsync(ex, powerCycle, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_stateLock)
                {
                    if (powerCycle == _powerCycle)
                    {
                        _restoreQueued = false;
                        _restoreCancellation?.Dispose();
                        _restoreCancellation = null;
                    }
                }
            }
        }

        /// <summary>
        /// Requests application shutdown after a graphics restore failure.
        /// </summary>
        /// <param name="exception">The exception that caused the graphics restore failure.</param>
        /// <param name="powerCycle">The power-cycle identifier associated with the failed restore.</param>
        /// <param name="cancellationToken">A token that can cancel handling of the restore failure.</param>
        /// <returns>A task that represents the asynchronous shutdown request.</returns>
        private async Task ShutdownAfterRestoreFailureAsync(Exception exception, int powerCycle, CancellationToken cancellationToken)
        {
            lock (_stateLock)
            {
                if (_disposed || _shutdownRequested || powerCycle != _powerCycle || cancellationToken.IsCancellationRequested)
                {
                    LogMonitor(MonitorType.Information, nameof(ShutdownAfterRestoreFailureAsync), $"GraphicsRestoreFailureIgnored|{exception}");
                    return;
                }
                _shutdownRequested = true;
            }

            LogMonitor(MonitorType.Error, nameof(ShutdownAfterRestoreFailureAsync), $"GraphicsRestoreFailed|ShutdownRequested|{exception}");
            try
            {
                var app = Application.Current;
                var dispatcher = app?.Dispatcher;
                if (dispatcher == null)
                {
                    throw new InvalidOperationException("Cannot request graphics failure shutdown without an application dispatcher");
                }
                if (dispatcher.HasShutdownStarted)
                {
                    LogMonitor(MonitorType.Information, nameof(ShutdownAfterRestoreFailureAsync), "ApplicationShutdownAlreadyStarted");
                    return;
                }
                await dispatcher.InvokeAsync(() =>
                {
                    RestorePendingStateOnShutdown();
                    app.Shutdown(1);
                }).Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Error, nameof(ShutdownAfterRestoreFailureAsync), $"GraphicsFailureShutdownFailed|{ex}");
            }
        }

        /// <summary>
        /// Captures the current UI state on the UI thread, including the front graphic, model window state, and 3D tab state. 
        /// This method is called during the power suspending event to ensure that the application can restore its previous 
        /// state after a power cycle.
        /// </summary>
        private void CaptureSuspendStateOnUiThread()
        {
            var mainWindowVm = MainWindowVm.MainWindow1Vm;
            var skyTelescopeVm = SkyTelescopeVm.ASkyTelescopeVm;
            var modelWindow = Application.Current?.Windows.OfType<ModelV>().FirstOrDefault();
            var model3DTabIsActive = mainWindowVm?.CurrentPageViewModel is Model3Dvm;

            lock (_stateLock)
            {
                if (_disposed) return;
                if (!_snapshotPending)
                {
                    _savedFrontGraphic = SkySettings.FrontGraphic;
                    _modelWindowWasOpen = modelWindow != null;
                    _model3DTabWasActive = model3DTabIsActive;
                    _snapshotPending = true;
                }
            }

            modelWindow?.Close();

            if (skyTelescopeVm != null)
            {
                skyTelescopeVm.Graphic = FrontGraphic.None;
            }
            else if (SkySettings.FrontGraphic != FrontGraphic.None)
            {
                SkySettings.FrontGraphic = FrontGraphic.None;
            }

            if (model3DTabIsActive && mainWindowVm != null)
            {
                mainWindowVm.SkyWatcherVmRadio = false;
                mainWindowVm.SkyWatcherVmRadio = true;
            }

            // Remove active 3D surfaces before requesting software rendering.
            if (RenderOptions.ProcessRenderMode != System.Windows.Interop.RenderMode.SoftwareOnly)
            {
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            mainWindowVm?.SetPowerWindowMinimized(true);

            LogMonitor(MonitorType.Information, MethodBase.GetCurrentMethod()?.Name,
                $"SnapshotCaptured|Graphic:{_savedFrontGraphic}|ModelWindow:{_modelWindowWasOpen}|Model3DTab:{_model3DTabWasActive}|WindowState:{mainWindowVm?.WindowStates}");
        }

        /// <summary>
        /// Restores the UI state that was captured before the power suspend event on the UI thread. This includes 
        /// restoring the front graphic, reopening the model window if it was previously open, and reactivating 
        /// the 3D tab if it was previously active.
        /// </summary>
        private void RestoreSuspendStateOnUiThread()
        {
            FrontGraphic frontGraphic;
            bool modelWindowWasOpen;
            bool model3DTabWasActive;

            lock (_stateLock)
            {
                if (!_snapshotPending)
                {
                    _restoreQueued = false;
                    return;
                }

                frontGraphic = _savedFrontGraphic;
                modelWindowWasOpen = _modelWindowWasOpen;
                model3DTabWasActive = _model3DTabWasActive;
            }

            var step = "RestoreRenderMode";
            try
            {
                RenderOptions.ProcessRenderMode = Server.Properties.Server.Default.DisableHardwareAcceleration
                    ? System.Windows.Interop.RenderMode.SoftwareOnly
                    : System.Windows.Interop.RenderMode.Default;

                step = "RestoreFrontGraphic";
                var skyTelescopeVm = SkyTelescopeVm.ASkyTelescopeVm
                    ?? throw new InvalidOperationException("Telescope view model is unavailable for graphics restoration");
                skyTelescopeVm.Graphic = frontGraphic;

                step = "RestoreModelWindow";
                if (modelWindowWasOpen)
                {
                    skyTelescopeVm.OpenModelWindow(throwOnError: true);
                    if (!Application.Current.Windows.OfType<ModelV>().Any(window => window.IsVisible))
                    {
                        throw new InvalidOperationException("Floating model window did not reopen");
                    }
                }

                step = "RestoreModel3DTab";
                var mainWindowVm = MainWindowVm.MainWindow1Vm
                    ?? throw new InvalidOperationException("Main window view model is unavailable for graphics restoration");
                if (model3DTabWasActive)
                {
                    if (!mainWindowVm.Model3DRadioVisible)
                    {
                        throw new InvalidOperationException("Model3D tab is unavailable for restoration");
                    }
                    mainWindowVm.Model3DvmRadioRadio = false;
                    mainWindowVm.Model3DvmRadioRadio = true;
                    if (!(mainWindowVm.CurrentPageViewModel is Model3Dvm))
                    {
                        throw new InvalidOperationException("Model3D tab did not become active");
                    }
                }

                step = "RestoreWindowState";
                mainWindowVm.SetPowerWindowMinimized(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Graphics restoration failed at {step}", ex);
            }

            lock (_stateLock)
            {
                _snapshotPending = false;
                _restoreQueued = false;
            }

            LogMonitor(MonitorType.Information, MethodBase.GetCurrentMethod()?.Name,
                $"SnapshotRestored|Graphic:{frontGraphic}|ModelWindow:{modelWindowWasOpen}|Model3DTab:{model3DTabWasActive}|RenderMode:{RenderOptions.ProcessRenderMode}|ResumeElapsedMs:{_resumeElapsed?.Elapsed.TotalMilliseconds:0}|WindowState:{MainWindowVm.MainWindow1Vm?.WindowStates}");
        }

        private static void ExecuteOnUiThread(Action action, string methodName)
        {
            var app = Application.Current;
            var dispatcher = app?.Dispatcher;

            if (dispatcher == null)
            {
                LogMonitor(MonitorType.Warning, methodName, "UiActionSkipped|DispatcherUnavailable");
                return;
            }

            try
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                    return;
                }

                dispatcher.Invoke(action);
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, methodName, $"UiActionFailed|{ex.Message}");
            }
        }

        private static void LogMonitor(MonitorType type, string method, string message)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Ui,
                Category = MonitorCategory.Interface,
                Type = type,
                Method = method,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = message
            };

            MonitorLog.LogToMonitor(monitorItem);
        }
    }
}