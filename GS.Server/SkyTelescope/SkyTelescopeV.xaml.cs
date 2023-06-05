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
            var udpDiscoverService = ctx?.DiscoveryService;
            if (udpDiscoverService != null)
            {
                udpDiscoverService.DiscoveredDeviceEvent += UdpDiscoveryService_DiscoveredDeviceEvent;
                udpDiscoverService.RemovedDeviceEvent += UdpDiscoveryService_RemovedDeviceEvent;
                udpDiscoverService.Discover();
            }
        }

        private void UdpDiscoveryService_DiscoveredDeviceEvent(object sender, Shared.Transport.DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var hasChanges = false;
                var ctx = DataContext as SkyTelescopeVM;

                foreach (var device in e.Devices)
                {
                    if (!ctx.Devices.Contains(device))
                    {
                        ctx.Devices.Add(device);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    ctx?.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                }
            });
        }

        private void UdpDiscoveryService_RemovedDeviceEvent(object sender, Shared.Transport.DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var hasChanges = false;
                var ctx = DataContext as SkyTelescopeVM;

                foreach (var device in e.Devices)
                {
                    if (ctx.Devices.Remove(device))
                    {
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    ctx?.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                }
            });
        }
    }
}
