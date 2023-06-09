using System;
using System.Collections.Generic;

namespace GS.Shared.Transport
{
    public interface IDiscoveryService : IDisposable
    {
        IEnumerable<Device> AllDevices { get; }
        IEnumerable<Device> ActiveDevices { get; }

        /// <summary>
        /// Start periodic discovery if not already running.
        /// </summary>
        void StartAutoDiscovery();

        /// <summary>
        /// Stops automatically discoverying devices.
        /// </summary>
        void StopAutoDiscovery();

        event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;

        event EventHandler<DiscoveryEventArgs> RemovedDeviceEvent;
    }
}

