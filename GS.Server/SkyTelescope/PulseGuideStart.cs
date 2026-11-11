/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Threading;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Returned by <see cref="SkyServer.PulseGuide"/>. <see cref="Started"/> is set once the pulse has been
    /// issued to the hardware, or once it has been rejected; in the latter case <see cref="Error"/> holds the reason.
    /// </summary>
    public sealed class PulseGuideStart : IDisposable
    {
        private readonly Func<Exception> _error;

        internal PulseGuideStart(ManualResetEventSlim started, Func<Exception> error)
        {
            Started = started ?? throw new ArgumentNullException(nameof(started));
            _error = error;
        }

        /// <summary>
        /// Set when the hardware command has been sent or the pulse was rejected
        /// </summary>
        public ManualResetEventSlim Started { get; }

        /// <summary>
        /// The reason the pulse was rejected before it started, or null. Read after <see cref="Started"/> is set.
        /// </summary>
        public Exception Error => _error?.Invoke();

        public void Dispose() => Started.Dispose();
    }
}
