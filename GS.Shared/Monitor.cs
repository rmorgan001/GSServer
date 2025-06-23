/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Runtime.CompilerServices;

namespace GS.Shared
{
    /// <summary>
    /// Manages the logging to the monitor window then sends to queue for logging
    /// </summary>
    public static class MonitorLog
    {
        #region Fields

        private static readonly List<MonitorCategory> CategoryCheckList;
        private static readonly List<MonitorType> TypesCheckList;
        private static readonly List<MonitorDevice> DeviceCheckList;

        #endregion

        static MonitorLog()
        {
            CategoryCheckList = new List<MonitorCategory>();
            TypesCheckList = new List<MonitorType>();
            DeviceCheckList = new List<MonitorDevice>();
            //Load_Settings();
        }

        #region Settings

        public static void Load_Settings()
        {
            //MonitorDevice
            if (Settings.ServerDevice){DevicesToMonitor(MonitorDevice.Server, Settings.ServerDevice);}
            if (Settings.Telescope){DevicesToMonitor(MonitorDevice.Telescope, Settings.Telescope);}
            if (Settings.Telescope){DevicesToMonitor(MonitorDevice.Ui, Settings.Ui);}
            //MonitorCategory
            if (Settings.Other){CategoriesToMonitor(MonitorCategory.Other, Settings.Other);}
            if (Settings.Driver){CategoriesToMonitor(MonitorCategory.Driver, Settings.Driver);}
            if (Settings.Interface){CategoriesToMonitor(MonitorCategory.Interface, Settings.Interface);}
            if (Settings.Server){CategoriesToMonitor(MonitorCategory.Server, Settings.Server);}
            if (Settings.Mount){CategoriesToMonitor(MonitorCategory.Mount, Settings.Mount);}
            if (Settings.Alignment){CategoriesToMonitor(MonitorCategory.Alignment, Settings.Alignment);}
            //MonitorType
            if (Settings.Information){TypesToMonitor(MonitorType.Information, Settings.Information);}
            if (Settings.Data){TypesToMonitor(MonitorType.Data, Settings.Data);}
            if (Settings.Warning){TypesToMonitor(MonitorType.Warning, Settings.Warning);}
            if (Settings.Error){TypesToMonitor(MonitorType.Error, Settings.Error);}
            if (Settings.Debug){TypesToMonitor(MonitorType.Debug, Settings.Debug);}

            Settings.LogMonitor = Properties.Monitor.Default.LogMonitor;
            Settings.LogSession = Properties.Monitor.Default.LogSession;
            Settings.StartMonitor = Properties.Monitor.Default.StartMonitor;
        }

        private static void Save_MonitorDevice(MonitorDevice monitorDevice, bool value)
        {
            switch (monitorDevice)
            {
                case MonitorDevice.Server:              // general Server related
                    Settings.ServerDevice = value;      
                    break;
                case MonitorDevice.Telescope:           // Ascom and api interfaces
                    Settings.Telescope = value;
                    break;
                case MonitorDevice.Ui:                  // view and view models 
                    Settings.Ui = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorDevice), monitorDevice, null);
            }
        }

        private static void Save_MonitorCategory(MonitorCategory monitorCategory, bool value)
        {
            switch (monitorCategory)
            {
                case MonitorCategory.Other:         // Support or shared projects
                    Settings.Other = value;
                    break;
                case MonitorCategory.Driver:        // simulator and Sky watcher other data
                    Settings.Driver = value;
                    break;
                case MonitorCategory.Interface:
                    Settings.Interface = value;
                    break;
                case MonitorCategory.Server:        // Core Server processes
                    Settings.Server = value;
                    break;
                case MonitorCategory.Mount:         // simulator and Sky watcher commands
                    Settings.Mount = value;
                    break;
                case MonitorCategory.Alignment:     // Alignment related
                    Settings.Alignment = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorCategory), monitorCategory, null);
            }
        }

        private static void Save_MonitorType(MonitorType monitorType, bool value)
        {
            switch (monitorType)
            {
                case MonitorType.Information:           // also written to session log
                    Settings.Information = value;
                    break;
                case MonitorType.Data:                  // Core Information 
                    Settings.Data = value;
                    break;
                case MonitorType.Warning:               // also written to session log
                    Settings.Warning = value;
                    break;
                case MonitorType.Error:                 // also written to error and session logs
                    Settings.Error = value;
                    break;
                case MonitorType.Debug:                 // troubleshooting data
                    Settings.Debug = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(monitorType), monitorType, null);
            }
        }

        /// <summary>
        /// Turns on charting for CmdJSentEntry
        /// </summary>
        public static bool GetJEntries { get; set; }

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
            return DeviceCheckList.Exists(entry => entry == device);
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
                DeviceCheckList.Add(device);
            }
            else
            {
                DeviceCheckList.Remove(device);
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
            return CategoryCheckList.Exists(entry => entry == category);
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
                CategoryCheckList.Add(category);
            }
            else
            {
                CategoryCheckList.Remove(category);
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
            return TypesCheckList.Exists(entry => entry == type);
        }

        /// <summary>
        /// Turn on/off entry types that trigger the OnPropertyChanged event
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="add"></param>
        public static void TypesToMonitor(MonitorType entryType, bool add)
        {
            if (add)
            {
                TypesCheckList.Add(entryType);
            }
            else
            {
                TypesCheckList.Remove(entryType);
            }

            Save_MonitorType(entryType, add);
        }

        /// <summary>
        /// clears all the devices from the checklist
        /// </summary>
        public static void ClearDevicesToMonitor()
        {
            DeviceCheckList.Clear();
        }

        /// <summary>
        /// clears all the types from the checklist
        /// </summary>
        public static void ClearTypesToMonitor()
        {
            TypesCheckList.Clear();
        }

        /// <summary>
        /// clears all the categories from the checklist
        /// </summary>
        public static void ClearCategoriesToMonitor()
        {
            CategoryCheckList.Clear();
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
            // don't add to queue if not needed
            switch (entry.Type)
            {
                case MonitorType.Warning:
                case MonitorType.Error:
                case MonitorType.Debug:
                case MonitorType.Information:
                    MonitorQueue.AddEntry(entry);
                    break;
                case MonitorType.Data:
                    if (GetJEntries || Settings.StartMonitor) MonitorQueue.AddEntry(entry);
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

        /// <summary>
        /// Get name of current method using compile time function for async methods
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <returns></returns>
        public static string GetCurrentMethod([CallerMemberName] string methodName = "")
        {
            return methodName;
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
        Telescope,
        Focuser,
        Ui
    }

    /// <summary>
    /// Levels of monitor entries
    /// </summary>
    public enum MonitorType
    {
        Information,
        Data,
        Warning,
        Error,
        Debug
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
        Notes,
        Alignment
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
    //    public long PositionStart { get; set; }
    //    public long PositionEnd { get; set; }
    //    public DateTime EndTime { get; set; }
        public double Duration { get; set; }
        public double Rate { get; set; }
     //   public int BacklashSteps { get; set; }
     //   public bool PPECon { get; set; }
     //   public bool AltPPECon { get; set; }
    //    public double Declination { get; set; }
        public bool Rejected { get; set; }
    }

}
