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
                var ctx = DataContext as SkyTelescopeVM;
                if (!ctx.Devices.Contains(e.Device))
                {
                    ctx.Devices.Add(e.Device);
                    ctx?.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                }
            });
        }

        private void UdpDiscoveryService_RemovedDeviceEvent(object sender, Shared.Transport.DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var ctx = DataContext as SkyTelescopeVM;
                if (ctx.Devices.Remove(e.Device))
                {
                    ctx?.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                }
            });
        }
    }
}
