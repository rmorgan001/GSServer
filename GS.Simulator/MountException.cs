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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GS.Simulator
{
    [Serializable]
    public class MountException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public MountException()
        {
        }

        public MountException(ErrorCode err) : base($"Mount: {err}")
        {
            ErrorCode = err;
        }

        public MountException(ErrorCode err, string message) : base($"Mount: {err}, {message}")
        {
            ErrorCode = err;
        }

        public MountException(ErrorCode err, string message, Exception inner) : base($"Mount: {err}, {message}", inner)
        {
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        // Constructor should be protected for unsealed classes, private for sealed classes.
        // (The Serializer invokes this constructor through reflection, so it can be private)
        protected MountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Enum.TryParse("err", out ErrorCode err);
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("err", ErrorCode.ToString());
            // MUST call through to the base class to let it save its own state
            base.GetObjectData(info, context);
        }
    }
}
