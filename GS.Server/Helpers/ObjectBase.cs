/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using GS.Server.SkyTelescope;
using GS.Shared;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GS.Server.Helpers
{
    [ComVisible(false)]
    public class ObjectBase
    {
        private int _released;

        /// <summary>
        /// Unique id of this COM object instance, assigned at construction
        /// </summary>
        protected long InstanceId { get; }

        internal long ComInstanceId => InstanceId;

        protected ObjectBase()
        {
            InstanceId = SkySystem.GetId();

            // We increment the global count of objects.
            var objects = GSServer.CountObject();
            GSServer.LogCom(".ctor", $"CountObject|Id|{InstanceId}|Type|{GetType().Name}|Objects|{objects}");
        }

        ~ObjectBase()
        {
            // Finalizers must never throw, an unhandled exception here ends the process
            try
            {
                // Runs once, and removes any connection the client left behind
                if (Interlocked.Exchange(ref _released, 1) != 0) return;

                var wasConnected = SkySystem.ReleaseInstance(InstanceId, "Finalize");

                // We decrement the global count of objects.
                var objects = GSServer.UncountObject();
                GSServer.LogCom("Finalize",
                    $"UnCountObject|Id|{InstanceId}|Type|{GetType().Name}|WasConnected|{wasConnected}|Objects|{objects}|Connections|{SkySystem.ConnectedCount}",
                    wasConnected ? MonitorType.Warning : MonitorType.Information);
            }
            catch (Exception ex)
            {
                try { GSServer.LogCom("Finalize", $"Error|Id|{InstanceId}|{ex.Message}|{ex.StackTrace}", MonitorType.Error); }
                catch { /* nothing more can be done in a finalizer */ }
            }
            finally
            {
                // We then immediately test to see if we the conditions are right to attempt to terminate this server application.
                try { GSServer.ExitIf(); }
                catch { /* nothing more can be done in a finalizer */ }
            }
        }
    }
}
