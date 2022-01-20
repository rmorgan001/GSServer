#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DeviceInterface;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser
{
    public class AbsoluteAscomFocuser : IFocuserV3Ex
    {
        private readonly IFocuserV3 focuser;
        public AbsoluteAscomFocuser(IFocuserV3 focuser)
        {
            this.focuser = focuser;
        }

        public bool Connected { get => focuser.Connected; set => focuser.Connected = value; }

        public string Description => focuser.Description;

        public string DriverInfo => focuser.DriverInfo;

        public string DriverVersion => focuser.DriverVersion;

        public short InterfaceVersion => focuser.InterfaceVersion;

        public string Name => focuser.Name;

        public ArrayList SupportedActions => focuser.SupportedActions;

        public bool Absolute => true;

        public bool IsMoving => focuser.IsMoving;

        public bool Link { get => focuser.Link; set => focuser.Link = value; }

        public int MaxIncrement => focuser.MaxIncrement;

        public int MaxStep => focuser.MaxStep;

        public int Position => focuser.Position;

        public double StepSize => focuser.StepSize;

        public bool TempComp { get => focuser.TempComp && focuser.TempCompAvailable; set => focuser.TempComp = value; }

        public bool TempCompAvailable => focuser.TempCompAvailable;

        public double Temperature => focuser.Temperature;

        public string Action(string actionName, string actionParameters)
        {
            return focuser.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw = false)
        {
            focuser.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw = false)
        {
            return focuser.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw = false)
        {
            return focuser.CommandString(command, raw);
        }

        public void Dispose()
        {
            focuser.Dispose();
        }

        public void Halt()
        {
            focuser.Halt();
        }

        public void Move(int position)
        {
            throw new NotSupportedException("MoveAsync should be used instead of Move");
        }

        public void SetupDialog()
        {
            focuser.SetupDialog();
        }

        public async Task MoveAsync(int position, CancellationToken ct)
        {
            if (Connected)
            {
                bool reEnableTempComp = TempComp;
                if (reEnableTempComp)
                {
                    TempComp = false;
                }

                while (position != focuser.Position)
                {
                    focuser.Move(position);
                    while (IsMoving)
                    {
                        await NINA.Utility.Utility.Wait(TimeSpan.FromSeconds(1), ct);
                    }
                }

                if (reEnableTempComp)
                {
                    TempComp = true;
                }
            }
        }
    }
}
