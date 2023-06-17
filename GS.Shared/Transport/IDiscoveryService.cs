using System;
namespace GS.Shared.Transport
{
    public interface IDiscoveryService : IDisposable
    {
        TimeSpan DiscoveryInterval { get; }

        /// <summary>
        /// Start periodic discovery if not already running.
        /// </summary>
        void StartAutoDiscovery();

        /// <summary>
        /// Stops automatically discovering devices.
        /// </summary>
        void StopAutoDiscovery();

        bool Wifi { get; set; }

        event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;
    }
}

