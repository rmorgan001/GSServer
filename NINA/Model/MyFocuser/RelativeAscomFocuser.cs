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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser
{
    internal class RelativeAscomFocuser : IFocuserV4Ex
    {
        private readonly IFocuserV4 _focuser;
        public RelativeAscomFocuser(IFocuserV4 relativeFocuser)
        {
            if (relativeFocuser.Absolute)
            {
                throw new InvalidOperationException($"Focuser {relativeFocuser.Name} is an absolute focuser");
            }
            this._focuser = relativeFocuser;
            this.Position = 5000;
        }

        /// <summary>
        /// Connect to the device asynchronously using Connecting as the completion variable
        /// </summary>
        public void Connect()
        {
        }

        /// <summary>
        /// Disconnect from the device asynchronously using Connecting as the completion variable
        /// </summary>
        public void Disconnect()
        {
        }

        /// <summary>
        /// Completion variable for the asynchronous Connect() and Disconnect()  methods
        /// </summary>
        public bool Connecting => false;

        /// <summary>
        /// Return the device's state in one call
        /// </summary>
        public IStateValueCollection DeviceState
        {
            get
            {
                // Create an array list to hold the IStateValue entries
                var deviceState = new List<IStateValue>
                {
                    // Add one entry for each operational state, if possible
                    new StateValue(nameof(IFocuserV4.IsMoving), IsMoving),
                    new StateValue(nameof(IFocuserV4.Position), Position),
                    new StateValue(nameof(IFocuserV4.Temperature), Temperature),
                    new StateValue(DateTime.Now)
                };

                // Return the overall device state
                return new StateValueCollection(deviceState);
            }
        }

        public bool Connected { get => _focuser.Connected; set => _focuser.Connected = value; }

        public string Description => _focuser.Description;

        public string DriverInfo => _focuser.DriverInfo;

        public string DriverVersion => _focuser.DriverVersion;

        public short InterfaceVersion => _focuser.InterfaceVersion;

        public string Name => _focuser.Name;

        public ArrayList SupportedActions => _focuser.SupportedActions;

        public bool Absolute => true;

        public bool IsMoving => _focuser.IsMoving;

        public bool Link { get => _focuser.Link; set => _focuser.Link = value; }

        public int MaxIncrement => _focuser.MaxIncrement;

        public int MaxStep => _focuser.MaxStep;

        public int Position { get; private set; }

        public double StepSize => _focuser.StepSize;

        public bool TempComp { get => _focuser.TempComp && _focuser.TempCompAvailable; set => _focuser.TempComp = value; }

        public bool TempCompAvailable => _focuser.TempCompAvailable;

        public double Temperature => _focuser.Temperature;

        public string Action(string actionName, string actionParameters)
        {
            return _focuser.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw = false)
        {
            _focuser.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw = false)
        {
            return _focuser.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw = false)
        {
            return _focuser.CommandString(command, raw);
        }

        public void Dispose()
        {
            _focuser.Dispose();
        }

        public void Halt()
        {
            _focuser.Halt();
        }

        public void Move(int position)
        {
            throw new NotSupportedException("MoveAsync should be used instead of Move");
        }

        public void SetupDialog()
        {
            _focuser.SetupDialog();
        }

        public async Task MoveAsync(int pos, CancellationToken ct)
        {
            if (Connected)
            {
                bool reEnableTempComp = TempComp;
                if (reEnableTempComp)
                {
                    TempComp = false;
                }

                var relativeOffsetRemaining = pos - this.Position;
                while (relativeOffsetRemaining != 0)
                {
                    var moveAmount = Math.Min(MaxStep, Math.Abs(relativeOffsetRemaining));
                    if (relativeOffsetRemaining < 0)
                    {
                        moveAmount *= -1;
                    }
                    _focuser.Move(moveAmount);
                    while (IsMoving)
                    {
                        await Utility.Utility.Wait(TimeSpan.FromSeconds(1), ct);
                    }
                    relativeOffsetRemaining -= moveAmount;
                    this.Position += moveAmount;
                }

                if (reEnableTempComp)
                {
                    TempComp = true;
                }
            }
        }
    }
}
