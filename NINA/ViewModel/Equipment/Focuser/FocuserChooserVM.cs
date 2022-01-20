#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

// Note: This is a flattened version of the NINA FocuserChooseVM and EquipmentChooserVM base class.
#endregion "copyright"

using GS.Shared;
using GS.Shared.Command;
using GS.Utilities.Helpers;
using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Focuser
{

    public class FocuserChooserVM : ObservableObject
    {

        public FocuserChooserVM() :base()
        {
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
        }

        private AsyncObservableCollection<IDevice> _devices;

        public AsyncObservableCollection<IDevice> Devices
        {
            get
            {
                if (_devices == null)
                {
                    _devices = new AsyncObservableCollection<IDevice>();
                }
                return _devices;
            }
            set
            {
                _devices = value;
            }
        }

        public void GetEquipment(string defaultDeviceId = "")
        {
            Devices.Clear();

            Devices.Add(new DummyDevice("No Focuser"));

            try
            {
                foreach (IFocuser focuser in NINA.Utility.Utility.GetFocusers())
                {
                    Devices.Add(focuser);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new GS.Shared.MonitorEntry
                { Datetime = GS.Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }

            DetermineSelectedDevice(defaultDeviceId);
        }

        private IDevice _selectedDevice;

        public IDevice SelectedDevice
        {
            get
            {
                return _selectedDevice;
            }
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();
            }
        }

        public ICommand SetupDialogCommand { get; private set; }

        private void OpenSetupDialog(object o)
        {
            if (SelectedDevice?.HasSetupDialog == true)
            {
                SelectedDevice.SetupDialog();
            }
        }

        public void DetermineSelectedDevice(string id)
        {
            if (Devices.Count > 0)
            {
                var items = (from device in Devices where device.Id == id select device);
                if (items.Count() > 0)
                {
                    SelectedDevice = items.First();
                }
                else
                {
                    SelectedDevice = Devices.First();
                }
            }
        }
    }
}
