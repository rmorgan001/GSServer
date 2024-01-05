/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Management;

namespace GS.Shared
{
    public class SystemInfo
    {

        public static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        // alternative listing of ports
        public IList<string> GetManagedComPorts()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portNames = System.IO.Ports.SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                var portList = portNames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();

                foreach (var s in portList)
                {
                    Console.WriteLine(s);
                }

                return portList;
            }
        }
        public IList<int> GetComPorts()
        {
                var ports = new List<int>();
                foreach (var item in System.IO.Ports.SerialPort.GetPortNames())
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    var tmp = Strings.GetNumberFromString(item);
                    if (tmp.HasValue)
                    {
                        ports.Add((int)tmp);
                    }
                }
                return ports;
        }
    }
}
