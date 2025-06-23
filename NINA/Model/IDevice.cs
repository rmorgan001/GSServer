#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model
{ 
    public interface IDevice : INotifyPropertyChanged
    {
        bool HasSetupDialog { get; }
        string Id { get; }
        string Name { get; }

        string Category { get; }
        bool Connected { get; }
        string Description { get; }
        string DriverInfo { get; }
        string DriverVersion { get; }

        Task<bool> Connect(CancellationToken token);

        void Disconnect();

        void SetupDialog();
    }

    public class DummyDevice : IDevice
    {

        public DummyDevice(string name)
        {
            Name = name;
        }

        public bool HasSetupDialog => false;

        public string Category { get; } = string.Empty;

        public string Id
        {
            get
            {
                return "No_Device";
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            private set => _name = value;
        }

        public bool Connected => false;

        public string Description => string.Empty;

        public string DriverInfo => string.Empty;

        public string DriverVersion => string.Empty;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        public async Task<bool> Connect(CancellationToken token)
        {
            return await Task.Run(() => false);
        }

        public void Disconnect()
        {
        }

        public void SetupDialog()
        {
        }
    }

}
