/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using GS.Shared;

namespace GS.Server.Helpers
{
    [ComVisible(false)]
    public class ObjectBase
    {
        protected ObjectBase()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "CountObject" };
            MonitorLog.LogToMonitor(monitorItem);

            // We increment the global count of objects.
            GSServer.CountObject();
        }

        ~ObjectBase()
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "UncountObject" };
            MonitorLog.LogToMonitor(monitorItem);

            // We decrement the global count of objects.
            GSServer.UncountObject();
            // We then immediately test to see if we the conditions are right to attempt to terminate this server application.
            GSServer.ExitIf();
        }
    }
}
