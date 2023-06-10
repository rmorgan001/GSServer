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
        /// Stops automatically discoverying devices.
        /// </summary>
        void StopAutoDiscovery();

        event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;
    }
}

