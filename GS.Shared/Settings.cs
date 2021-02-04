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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GS.Shared
{
    public static class Settings
    {

        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region Properties

        private static bool _serverDevice;
        public static bool ServerDevice
        {
            get => _serverDevice;
            set
            {
                if (_serverDevice == value) return;
                _serverDevice = value;
                Properties.Monitor.Default.ServerDevice = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _telescope;
        public static bool Telescope
        {
            get => _telescope;
            set
            {
                if (_telescope == value) return;
                _telescope = value;
                Properties.Monitor.Default.Telescope = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _information;
        public static bool Information
        {
            get => _information;
            set
            {
                if (_information == value) return;
                _information = value;
                Properties.Monitor.Default.Information = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _data;
        public static bool Data
        {
            get => _data;
            set
            {
                if (_data == value) return;
                _data = value;
                Properties.Monitor.Default.Data = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _warning;
        public static bool Warning
        {
            get => _warning;
            set
            {
                if (_warning == value) return;
                _warning = value;
                Properties.Monitor.Default.Warning = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _error;
        public static bool Error
        {
            get => _error;
            set
            {
                if (_error == value) return;
                _error = value;
                Properties.Monitor.Default.Error = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _other;
        public static bool Other
        {
            get => _other;
            set
            {
                if (_other == value) return;
                _other = value;
                Properties.Monitor.Default.Other = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _driver;
        public static bool Driver
        {
            get => _driver;
            set
            {
                if (_driver == value) return;
                _driver = value;
                Properties.Monitor.Default.Driver = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _interface;
        public static bool Interface
        {
            get => _interface;
            set
            {
                if (_interface == value) return;
                _interface = value;
                Properties.Monitor.Default.Interface = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _server;
        public static bool Server
        {
            get => _server;
            set
            {
                if (_server == value) return;
                _server = value;
                Properties.Monitor.Default.Server = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _mount;
        public static bool Mount
        {
            get => _mount;
            set
            {
                if (_mount == value) return;
                _mount = value;
                Properties.Monitor.Default.Mount = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _alignment;
        public static bool Alignment
        {
            get => _alignment;
            set
            {
                if (_alignment == value) return;
                _alignment = value;
                Properties.Monitor.Default.Alignment = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static string _language;
        public static string Language
        {
            get => _language;
            set
            {
                if (_language == value) return;
                _language = value;
                Properties.Monitor.Default.Language = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _logMonitor;
        public static bool LogMonitor
        {
            get => _logMonitor;
            set
            {
                if (_logMonitor == value) return;
                _logMonitor = value;
                Properties.Monitor.Default.LogMonitor = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        private static bool _logCharting;
        public static bool LogCharting
        {
            get => _logCharting;
            set
            {
                if (_logCharting == value) return;
                _logCharting = value;
                Properties.Monitor.Default.LogCharting = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }


        /// <summary>
        /// starts sending entries to a file in my documents
        /// </summary>
        private static bool _logSession;
        public static bool LogSession
        {
            get => _logSession;
            set
            {
                if (_logSession == value) return;
                _logSession = value;
                Properties.Monitor.Default.LogSession = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
            }
        }

        private static bool _startMonitor;
        public static bool StartMonitor
        {
            get => _startMonitor;
            set
            {
                if (_startMonitor == value) return;
                _startMonitor = value;
                Properties.Monitor.Default.StartMonitor = value;
                LogSetting(MethodBase.GetCurrentMethod().Name, $"{value}");
                OnStaticPropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// will upgrade if necessary
        /// </summary>
        public static void Load()
        {
            Upgrade();

            ServerDevice = Properties.Monitor.Default.ServerDevice;
            Server = Properties.Monitor.Default.Server;
            Interface = Properties.Monitor.Default.Interface;
            Driver = Properties.Monitor.Default.Driver;
            Other = Properties.Monitor.Default.Other;
            Error = Properties.Monitor.Default.Error;
            Warning = Properties.Monitor.Default.Warning;
            Data = Properties.Monitor.Default.Data;
            Information = Properties.Monitor.Default.Information;
            Telescope = Properties.Monitor.Default.Telescope;
            Mount = Properties.Monitor.Default.Mount;
            LogMonitor = Properties.Monitor.Default.LogMonitor;
            StartMonitor = Properties.Monitor.Default.StartMonitor;
            Language = Properties.Monitor.Default.Language;
            LogCharting = Properties.Monitor.Default.LogCharting;
            LogSession = Properties.Monitor.Default.LogSession;
        }

        /// <summary>
        /// upgrade and set new version
        /// </summary>
        private static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Monitor.Default.Version;

            if (version == assembly.ToString()) return;
            Properties.Monitor.Default.Upgrade();
            Properties.Monitor.Default.Version = assembly.ToString();
            Save();
        }

        /// <summary>
        /// save and reload
        /// </summary>
        public static void Save()
        {
            Properties.Monitor.Default.Save();
            Properties.Monitor.Default.Reload();
        }

        /// <summary>
        /// output to session log
        /// </summary>
        /// <param name="method"></param>
        /// <param name="value"></param>
        private static void LogSetting(string method, string value)
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = $"{method}", Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{value}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// property event notification
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
