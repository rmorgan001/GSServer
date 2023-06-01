using System;
using System.Collections.Generic;

namespace GS.Shared.Transport
{
    public interface ISerialUdpPortDiscoveryService : IDisposable
    {
        IEnumerable<RemoteDevice> ActiveDevices { get; }

        /// <summary>
        /// Asynchronously sends UDP broadcast messages to discover all reachable devices.
        /// </summary>
        void Discover();

        event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;

        event EventHandler<DiscoveryEventArgs> RemovedDeviceEvent;
    }
}

