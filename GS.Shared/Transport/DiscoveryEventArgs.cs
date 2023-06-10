using System;
using System.Collections.Generic;

namespace GS.Shared.Transport
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryEventArgs(IReadOnlyList<Device> devices, bool isSynchronous = false)
        {
            Devices = devices;
            IsSynchronous = isSynchronous;
        }

        /// <summary>
        /// If true then only synchronous discovery was done (serial ports).
        /// </summary>
        public bool IsSynchronous { get; }

        public IReadOnlyList<Device> Devices { get; }
    }
}

