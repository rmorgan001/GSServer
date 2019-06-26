/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GS.Server.SkyTelescope
{
    [Serializable]
    public class SkyServerException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public SkyServerException()
        {
        }

        public SkyServerException(ErrorCode err) : base($"Shared: {err}")
        {
            ErrorCode = err;
        }

        public SkyServerException(ErrorCode err, string message) : base($"Shared: {err}, {message}")
        {
            ErrorCode = err;
        }

        public SkyServerException(ErrorCode err, string message, Exception inner) : base($"Shared: {err}, {message}", inner)
        {
            ErrorCode = err;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        // Constructor should be protected for unsealed classes, private for sealed classes.
        // (The Serializer invokes this constructor through reflection, so it can be private)
        protected SkyServerException(SerializationInfo info, StreamingContext context)
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
