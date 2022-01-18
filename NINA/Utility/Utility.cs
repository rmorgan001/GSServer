#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFocuser;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Utility
{
    // A portion of the NINA Utility class.
    public static class Utility
    {
        public static async Task<TimeSpan> Delay(int milliseconds, CancellationToken token)
        {
            var t = TimeSpan.FromMilliseconds(milliseconds);
            return await Delay(t, token);
        }

        public static async Task<TimeSpan> Delay(TimeSpan span, CancellationToken token)
        {
            var now = DateTime.UtcNow;
            await Task.Delay(span, token);
            return DateTime.UtcNow.Subtract(now);
        }
        public static async Task<TimeSpan> Wait(TimeSpan t, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = default, string status = "")
        {
            TimeSpan elapsed = new TimeSpan(0);
            do
            {
                var delta = await Delay(100, token);
                elapsed += delta;
                progress?.Report(new ApplicationStatus() { MaxProgress = (int)t.TotalSeconds, Progress = (int)elapsed.TotalSeconds, Status = string.IsNullOrWhiteSpace(status) ? "Waiting ..." : status, ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue });
            } while (elapsed < t && !token.IsCancellationRequested);
            return elapsed;
        }

        public static List<IFocuser> GetFocusers()
        {
            var l = new List<IFocuser>();
            using (var ascomDevices = new ASCOM.Utilities.Profile())
            {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Focuser"))
                {
                    try
                    {
                        AscomFocuser focuser = new AscomFocuser(device.Key, device.Value);
                        l.Add(focuser);
                    }
                    catch (Exception)
                    {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }


    }
}
