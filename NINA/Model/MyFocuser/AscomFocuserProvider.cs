﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;

namespace NINA.Model.MyFocuser
{
    public class AscomFocuserProvider : IAscomFocuserProvider
    {

        public IFocuserV3Ex GetFocuser(string focuserId, bool connect)
        {
            var ascomFocuser = new ASCOM.DriverAccess.Focuser(focuserId);
            if (connect)
            {
                ascomFocuser.Connected = true;
            }
            if (ascomFocuser.Absolute)
            {
                // Logger.Debug($"Absolute ASCOM Focuser detected {focuserId}");
                return new AbsoluteAscomFocuser(ascomFocuser);
            }
            else
            {
                // Logger.Debug($"Relative ASCOM Focuser detected {focuserId}");
                return new RelativeAscomFocuser(ascomFocuser);
            }
        }
    }
}
