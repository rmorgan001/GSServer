using GS.Shared.Transport;
using System;
using System.Runtime.InteropServices;
using System.Windows;


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
            if (discoveryService == null) return;
            discoveryService.DiscoveredDeviceEvent += UdpDiscoveryService_DiscoveredDeviceEvent;
            discoveryService.StartAutoDiscovery();
            discoveryService.Wifi = SkySettings.Wifi;
        }

        private void UdpDiscoveryService_DiscoveredDeviceEvent(object sender, DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (!(DataContext is SkyTelescopeVM ctx)) return;

                var hasChanges = false;
                foreach (var discoveredDevice in e.Devices)
                {
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
                // discard anything not detected in two scans
                var discardOlderThanMs = ctx.DiscoveryService.DiscoveryInterval.TotalMilliseconds * 2;
                for (var i = SkySettings.Devices.Count - 1; i >= 0; i--)
                {
                    var existingDevice = SkySettings.Devices[i];
                    // Do not remove UDP devices when doing asynchronous discovery
                    if ( timeMs - existingDevice.DiscoverTimeMs > discardOlderThanMs) // Removed so dropdown will update (!e.IsSynchronous || existingDevice.Index > 0) &&
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

                ctx.DiscoveryService.Wifi = SkySettings.Wifi;
            });
        }

    }
}
