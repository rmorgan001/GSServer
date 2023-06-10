using GS.Shared.Transport;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Interaction logic for TelescopeView.xaml
    /// </summary>
    [ComVisible(false)]
    public partial class SkyTelescopeV
    {
        public SkyTelescopeV()
        {
            InitializeComponent();
        }

        private void SkyTelescopeV_OnLoaded(object sender, RoutedEventArgs e)
        {
            var ctx = DataContext as SkyTelescopeVM;
            var discoveryService = ctx?.DiscoveryService;
            if (discoveryService != null)
            {
                discoveryService.DiscoveredDeviceEvent += UdpDiscoveryService_DiscoveredDeviceEvent;
                discoveryService.StartAutoDiscovery();
            }
        }

        private void UdpDiscoveryService_DiscoveredDeviceEvent(object sender, DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (DataContext is SkyTelescopeVM ctx)
                {
                    var hasChanges = false;
                    for (var i = 0; i < e.Devices.Count; i++)
                    {
                        var discoveredDevice = e.Devices[i];
                        var existingIndex = SkySettings.Devices.IndexOf(discoveredDevice);
                        if (existingIndex >= 0)
                        {
                            SkySettings.Devices[existingIndex].DiscoverTimeMs = discoveredDevice.DiscoverTimeMs;
                        }
                        else
                        {
                            SkySettings.Devices.Add(discoveredDevice);
                            hasChanges = true;
                        }
                    }

                    var timeMs = Environment.TickCount;
                    var discardOlderThanMs = ctx.DiscoveryService.DiscoveryInterval.TotalMilliseconds;
                    for (var i = SkySettings.Devices.Count - 1; i >= 0; i--)
                    {
                        var existingDevice = SkySettings.Devices[i];
                        if (!e.Devices.Contains(existingDevice) && timeMs - existingDevice.DiscoverTimeMs > discardOlderThanMs)
                        {
                            SkySettings.Devices.RemoveAt(i);
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        ctx.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                        ctx.RaisePropertyChanged(nameof(SkyTelescopeVM.SelectedDevice));
                    }
                }
            });
        }
    }
}
