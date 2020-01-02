/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Shared
{
    /// <summary>
    /// Manages the logging to the monitor window then sends to queue for logging
    /// </summary>
    public static class MonitorLog
    {
        #region Fields

        private static readonly List<MonitorCategory> _categoryCheckList;
        private static readonly List<MonitorType> _typesCheckList;
        private static readonly List<MonitorDevice> _deviceCheckList;

        #endregion

        static MonitorLog()
        {
            _categoryCheckList = new List<MonitorCategory>();
            _typesCheckList = new List<MonitorType>();
            _deviceCheckList = new List<MonitorDevice>();
            Settings.Load();
            Load_Settings();
        }

        #region Settings

        private static void Load_Settings()
        {
            if (Settings.ServerDevice) DevicesToMonitor(MonitorDevice.Server, Settings.ServerDevice);
            if (Settings.Telescope) DevicesToMonitor(MonitorDevice.Telescope, Settings.Telescope);

            if (Settings.Other) CategoriesToMonitor(MonitorCategory.Other, Settings.Other);
            if (Settings.Driver) CategoriesToMonitor(MonitorCategory.Driver, Settings.Driver);
            if (Settings.Interface) CategoriesToMonitor(MonitorCategory.Interface, Settings.Interface);
            if (Settings.Server) CategoriesToMonitor(MonitorCategory.Server, Settings.Server);
            if (Settings.Mount) CategoriesToMonitor(MonitorCategory.Mount, Settings.Mount);

            if (Settings.Information) TypesToMonitor(MonitorType.Information, Settings.Information);
            if (Settings.Data) TypesToMonitor(MonitorType.Data, Settings.Data);
            if (Settings.Warning) TypesToMonitor(MonitorType.Warning, Settings.Warning);
            if (Settings.Error) TypesToMonitor(MonitorType.Error, Settings.Error);

            Settings.LogMonitor = Properties.Monitor.Default.LogMonitor;
            Settings.LogSession = Properties.Monitor.Default.LogSession;
            Settings.StartMonitor = Properties.Monitor.Default.StartMonitor;

        }

        private static void Save_MonitorDevice(MonitorDevice monitorDevice, bool value)
        {
            switch (monitorDevice)
            {
                case MonitorDevice.Server:
                    Settings.ServerDevice = value;
                    break;
                case MonitorDevice.Telescope:
                    Settings.Telescope = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorDevice), monitorDevice, null);
            }
        }

        private static void Save_MonitorCategory(MonitorCategory monitorCategory, bool value)
        {
            switch (monitorCategory)
            {
                case MonitorCategory.Other:
                    Settings.Other = value;
                    break;
                case MonitorCategory.Driver:
                    Settings.Driver = value;
                    break;
                case MonitorCategory.Interface:
                    Settings.Interface = value;
                    break;
                case MonitorCategory.Server:
                    Settings.Server = value;
                    break;
                case MonitorCategory.Mount:
                    Settings.Mount = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorCategory), monitorCategory, null);
            }
        }

        private static void Save_MonitorType(MonitorType monitorType, bool value)
        {
            switch (monitorType)
            {
                case MonitorType.Information:
                    Settings.Information = value;
                    break;
                case MonitorType.Data:
                    Settings.Data = value;
                    break;
                case MonitorType.Warning:
                    Settings.Warning = value;
                    break;
                case MonitorType.Error:
                    Settings.Error = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorType), monitorType, null);
            }
        }

        /// <summary>
        /// Turns on charting for CmdjSentEntry
        /// </summary>
        public static bool GetjEntries { get; set; }

        /// <summary>
        /// Turns on charting for pulses
        /// </summary>
        public static bool GetPulses { get; set; }

        #endregion

        #region Monitor

        /// <summary>
        /// reset the count from the UI
        /// </summary>
        public static void ResetIndex()
        {
            MonitorQueue.ResetMonitorIndex();
            // _index = 0;
        }

        /// <summary>
        /// Contains in Devices list
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool InDevices(MonitorDevice device)
        {
            return _deviceCheckList.Exists(entry => entry == device);
        }

        /// <summary>
        /// Turn on/off devices that trigger the OnPropertyChanged event
        /// </summary>
        /// <param name="device"></param>
        /// <param name="add"></param>
        public static void DevicesToMonitor(MonitorDevice device, bool add)
        {
            if (add)
            {
                _deviceCheckList.Add(device);
            }
            else
            {
                _deviceCheckList.Remove(device);
            }

            Save_MonitorDevice(device, add);
        }

        /// <summary>
        /// Contains in Category list
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static bool InCategory(MonitorCategory category)
        {
            return _categoryCheckList.Exists(entry => entry == category);
        }

        /// <summary>
        /// Turn on/off categories that trigger the OnPropertyChanged event
        /// </summary>
        /// <param name="category"></param>
        /// <param name="add"></param>
        public static void CategoriesToMonitor(MonitorCategory category, bool add)
        {
            if (add)
            {
                _categoryCheckList.Add(category);
            }
            else
            {
                _categoryCheckList.Remove(category);
            }

            Save_MonitorCategory(category, add);
        }

        /// <summary>
        /// Contains in Type list
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool InTypes(MonitorType type)
        {
            return _typesCheckList.Exists(entry => entry == type);
        }

        /// <summary>
        /// Turn on/off entry types that trigger the OnPropertyChanged event
        /// </summary>
        /// <param name="entrytype"></param>
        /// <param name="add"></param>
        public static void TypesToMonitor(MonitorType entrytype, bool add)
        {
            if (add)
            {
                _typesCheckList.Add(entrytype);
            }
            else
            {
                _typesCheckList.Remove(entrytype);
            }

            Save_MonitorType(entrytype, add);
        }

        /// <summary>
        /// clears all the devices from the checklist
        /// </summary>
        public static void ClearDevicesToMonitor()
        {
            _deviceCheckList.Clear();
        }

        /// <summary>
        /// clears all the types from the checklist
        /// </summary>
        public static void ClearTypesToMonitor()
        {
            _typesCheckList.Clear();
        }

        /// <summary>
        /// clears all the categories from the checklist
        /// </summary>
        public static void ClearCategoriesToMonitor()
        {
            _categoryCheckList.Clear();
        }

        /// <summary>
        /// Log multiple entries to the monitor
        /// </summary>
        public static void LogToMonitor(MonitorEntry entry, IEnumerable<MonitorCategory> categories)
        {
            foreach (var category in categories)
            {
                entry.Category = category;
                LogToMonitor(entry);
            }
        }

        /// <summary>
        /// Send a MonitorEntry to the queue to be processed
        /// </summary>
        public static void LogToMonitor(MonitorEntry entry)
        {
            entry.Message = entry.Message.Trim();
            entry.Method = entry.Method.Trim();
            // dont add to queue if not needed
            switch (entry.Type)
            {
                case MonitorType.Warning:
                case MonitorType.Error:
                case MonitorType.Information:
                    MonitorQueue.AddEntry(entry);
                    break;
                case MonitorType.Data:
                    if (GetjEntries || Settings.StartMonitor) MonitorQueue.AddEntry(entry);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Send a PulseEntry to the queue to be processed
        /// </summary>
        public static void LogToMonitor(PulseEntry entry)
        {
            MonitorQueue.AddPulse(entry);
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// List of Driver Devices 
    /// </summary>
    public enum MonitorDevice
    {
        Server,
        Telescope
    }

    /// <summary>
    /// Levels of monitor entries
    /// </summary>
    public enum MonitorType
    {
        Information,
        Data,
        Warning,
        Error
    }

    /// <summary>
    /// used to define where or what process monitor items are being logged
    /// </summary>
    public enum MonitorCategory
    {
        Other,
        Driver,
        Interface,
        Server,
        Mount,
        Notes
    }
    #endregion

    /// <summary>
    /// individual Monitor Item
    /// </summary>
    public class MonitorEntry
    {
        public DateTime Datetime { get; set; }
        public int Index { get; set; }
        public MonitorDevice Device { get; set; }
        public MonitorCategory Category { get; set; }
        public MonitorType Type { get; set; }
        public string Method { get; set; }
        public int Thread { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// individual Pulse Item
    /// </summary>
    public class PulseEntry
    {
        public int Axis { get; set; }
        public DateTime StartTime { get; set; }
        public long PositionStart { get; set; }
        public long PositionEnd { get; set; }
        public DateTime EndTime { get; set; }
        public double Duration { get; set; }
        public double Rate { get; set; }
        public int BacklashSteps { get; set; }
        public bool PPECon { get; set; }
        public bool AltPPECon { get; set; }
        public double Declination { get; set; }
        public bool Rejected { get; set; }
    }

}
