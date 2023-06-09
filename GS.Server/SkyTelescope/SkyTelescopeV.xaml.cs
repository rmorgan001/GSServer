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
                discoveryService.RemovedDeviceEvent += UdpDiscoveryService_RemovedDeviceEvent;
                discoveryService.StartAutoDiscovery();
            }
        }

        private void UdpDiscoveryService_DiscoveredDeviceEvent(object sender, Shared.Transport.DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (DataContext is SkyTelescopeVM ctx)
                {
                    var hasChanges = false;

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
                        ctx.RaisePropertyChanged(nameof(SkyTelescopeVM.Devices));
                        ctx.RaisePropertyChanged(nameof(SkyTelescopeVM.SelectedDevice));
                    }
                }
            });
        }

        private void UdpDiscoveryService_RemovedDeviceEvent(object sender, Shared.Transport.DiscoveryEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (DataContext is SkyTelescopeVM ctx)
                {
                    var hasChanges = false;

                    foreach (var device in e.Devices)
                    {
                        if (ctx.Devices.Remove(device))
                        {
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
