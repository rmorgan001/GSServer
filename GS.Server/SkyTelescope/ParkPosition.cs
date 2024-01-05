﻿/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.DeviceInterface;

namespace GS.Server.SkyTelescope
{
    public class ParkPosition
    {
        public string Name { get; set; } = "Blank";
        public double X { get; set; } = (SkySettings.AlignmentMode == AlignmentModes.algAltAz) ?  0 : 90;
        public double Y { get; set; } = (SkySettings.AlignmentMode == AlignmentModes.algAltAz) ? 0 : 90;
    }
}
