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
        private static readonly TimeSpan GraphicsResumeDelay = TimeSpan.FromSeconds(5);
        private Stopwatch _resumeElapsed;
        private CancellationTokenSource _restoreCancellation;
        private int _powerCycle;
        private bool _disposed;

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

        public void HandlePowerResumed()
        {
            lock (_stateLock)
            {
                if (_disposed) return;
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

        // Called under _stateLock; the generation also invalidates already queued dispatcher work.
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
                if (_disposed || !_snapshotPending || _restoreQueued || _resumeElapsed == null)
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
                        if (_disposed || powerCycle != _powerCycle || cancellationToken.IsCancellationRequested)
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
                LogMonitor(MonitorType.Warning, nameof(RestoreAfterDelayAsync), $"GraphicsRestoreFailed|{ex}");
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

            try
            {
                RenderOptions.ProcessRenderMode = Server.Properties.Server.Default.DisableHardwareAcceleration
                    ? System.Windows.Interop.RenderMode.SoftwareOnly
                    : System.Windows.Interop.RenderMode.Default;
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, nameof(RestoreSuspendStateOnUiThread), $"RestoreRenderModeFailed|{ex.Message}");
                return;
            }

            try
            {
                var skyTelescopeVm = SkyTelescopeVm.ASkyTelescopeVm;
                if (skyTelescopeVm != null)
                {
                    skyTelescopeVm.Graphic = frontGraphic;
                }
                else if (SkySettings.FrontGraphic != frontGraphic)
                {
                    SkySettings.FrontGraphic = frontGraphic;
                }
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, nameof(RestoreSuspendStateOnUiThread), $"RestoreFrontGraphicFailed|{ex.Message}");
            }

            try
            {
                if (modelWindowWasOpen)
                {
                    SkyTelescopeVm.ASkyTelescopeVm?.OpenModelWindowCmd?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, nameof(RestoreSuspendStateOnUiThread), $"RestoreModelWindowFailed|{ex.Message}");
            }

            try
            {
                if (model3DTabWasActive)
                {
                    var mainWindowVm = MainWindowVm.MainWindow1Vm;
                    if (mainWindowVm != null && mainWindowVm.Model3DRadioVisible)
                    {
                        mainWindowVm.Model3DvmRadioRadio = false;
                        mainWindowVm.Model3DvmRadioRadio = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMonitor(MonitorType.Warning, nameof(RestoreSuspendStateOnUiThread), $"RestoreModel3DTabFailed|{ex.Message}");
            }
            finally
            {
                try
                {
                    MainWindowVm.MainWindow1Vm?.SetPowerWindowMinimized(false);
                }
                catch (Exception ex)
                {
                    LogMonitor(MonitorType.Warning, nameof(RestoreSuspendStateOnUiThread), $"RestoreWindowStateFailed|{ex}");
                }
                lock (_stateLock)
                {
                    _snapshotPending = false;
                    _restoreQueued = false;
                }
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