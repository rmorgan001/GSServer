using GS.Principles;
using GS.Server.Main;
using GS.Server.Model3D;
using GS.Server.SkyTelescope;
using GS.Server.Windows;
using GS.Shared;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerUiCoordinator"/> class and subscribes to the power resume event.
        /// </summary>
        public PowerUiCoordinator()
        {
            SkyServer.PowerResumeHandled += SkyServer_PowerResumeHandled;
        }

        /// <summary>
        /// Handles the power suspending event by capturing the current UI state on the UI thread.
        /// </summary>
        public void HandlePowerSuspending()
        {
            ExecuteOnUiThread(CaptureSuspendStateOnUiThread, MethodBase.GetCurrentMethod()?.Name);
        }

        /// <summary>
        /// Restores the UI state that was captured before the power suspend event. This method should be called during 
        /// application shutdown to ensure that the UI state is restored correctly.
        /// </summary>
        public void RestorePendingStateOnShutdown()
        {
            FrontGraphic frontGraphic;
            var shouldRestore = false;

            lock (_stateLock)
            {
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
            SkyServer.PowerResumeHandled -= SkyServer_PowerResumeHandled;
        }

        /// <summary>
        /// Handles the power resume event by restoring the UI state that was captured before the power suspend event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SkyServer_PowerResumeHandled(object sender, EventArgs e)
        {
            lock (_stateLock)
            {
                if (!_snapshotPending || _restoreQueued)
                {
                    return;
                }

                _restoreQueued = true;
            }

            var app = Application.Current;
            var dispatcher = app?.Dispatcher;

            if (dispatcher == null)
            {
                lock (_stateLock)
                {
                    _restoreQueued = false;
                }

                LogMonitor(MonitorType.Warning, nameof(SkyServer_PowerResumeHandled), "ResumeRestoreSkipped|DispatcherUnavailable");
                return;
            }

            dispatcher.BeginInvoke(new Action(RestoreSuspendStateOnUiThread));
        }

        /// <summary>
        /// Captures the current UI state on the UI thread, including the front graphic, model window state, and 3D tab state. 
        /// This method is called during the power suspending event to ensure that the application can restore its previous 
        /// state after a power cycle.
        /// </summary>
        private void CaptureSuspendStateOnUiThread()
        {
            lock (_stateLock)
            {
                if (_snapshotPending)
                {
                    RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
                    return;
                }

                _savedFrontGraphic = SkySettings.FrontGraphic;
                _restoreQueued = false;
                _snapshotPending = true;
            }

            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            var mainWindowVm = MainWindowVm.MainWindow1Vm;
            var skyTelescopeVm = SkyTelescopeVm.ASkyTelescopeVm;
            var modelWindow = Application.Current?.Windows.OfType<ModelV>().FirstOrDefault();

            var modelWindowWasOpen = modelWindow != null;
            var model3DTabWasActive = mainWindowVm?.CurrentPageViewModel is Model3Dvm;

            if (modelWindowWasOpen)
            {
                modelWindow.Close();
            }

            if (skyTelescopeVm != null)
            {
                skyTelescopeVm.Graphic = FrontGraphic.None;
            }
            else if (SkySettings.FrontGraphic != FrontGraphic.None)
            {
                SkySettings.FrontGraphic = FrontGraphic.None;
            }

            if (model3DTabWasActive && mainWindowVm != null)
            {
                mainWindowVm.SkyWatcherVmRadio = false;
                mainWindowVm.SkyWatcherVmRadio = true;
            }

            lock (_stateLock)
            {
                _modelWindowWasOpen = modelWindowWasOpen;
                _model3DTabWasActive = model3DTabWasActive;
            }

            LogMonitor(MonitorType.Information, MethodBase.GetCurrentMethod()?.Name,
                $"SnapshotCaptured|Graphic:{_savedFrontGraphic}|ModelWindow:{modelWindowWasOpen}|Model3DTab:{model3DTabWasActive}");
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
                lock (_stateLock)
                {
                    _snapshotPending = false;
                    _restoreQueued = false;
                }
            }

            LogMonitor(MonitorType.Information, MethodBase.GetCurrentMethod()?.Name,
                $"SnapshotRestored|Graphic:{frontGraphic}|ModelWindow:{modelWindowWasOpen}|Model3DTab:{model3DTabWasActive}|RenderMode:{RenderOptions.ProcessRenderMode}");
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