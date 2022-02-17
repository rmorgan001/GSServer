/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Shared
{
    /// <summary>
    /// Specific GS Server attribute that is check when a driver assembly loads
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GssAttribute : Attribute
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="T:ASCOM.GreenSwamp.GssAttribute" /> class.
        /// </summary>
        /// <param name="name">The 'friendly name' of the served class.</param>
        public GssAttribute(string name)
        {
            DisplayName = name;
        }

        /// <summary>
        ///   Gets or sets the 'friendly name' of the served class, as registered with the AsCom Chooser.
        /// </summary>
        /// <value>The 'friendly name' of the served class.</value>
        public string DisplayName { get; }
    }
}
