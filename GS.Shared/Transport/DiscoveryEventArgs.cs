using System;

namespace GS.Shared.Transport
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryEventArgs(Device device)
        {
            Device = device;
        }

        public Device Device { get; }
    }
}

