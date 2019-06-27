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
namespace GS.Simulator
{
    public struct MountInfo
    {
        public bool CanAxisSlewsIndependent { get; set; }
        public bool CanAzEq { get; set; }
        public bool CanDualEncoders { get; set; }
        public bool CanHalfTrack { get; set; }
        public bool CanHomeSensors { get; set; }
        public bool CanPolarLed { get; set; }
        public bool CanPpec { get; set; }
        public bool CanWifi { get; set; }
        public string MountName { get; set; }
        public string MountVersion { get; set; }
    }
}
