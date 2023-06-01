using System;

namespace GS.Shared.Transport
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryEventArgs(RemoteDevice device)
        {
            Device = device;
        }

        public RemoteDevice Device { get; }
    }
}

