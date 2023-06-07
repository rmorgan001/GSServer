using System;
using System.Collections.Generic;

namespace GS.Shared.Transport
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryEventArgs(IReadOnlyList<Device> devices)
        {
            Devices = devices;
        }

        public IReadOnlyList<Device> Devices { get; }
    }
}

