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
using System.Windows.Input;

namespace GS.Shared
{
    /// <inheritdoc />
    /// <summary>
    /// makes a wait cursor and then changes it back when disposed
    /// </summary>
    public sealed class WaitCursor : IDisposable
    {
        private Cursor _previousCursor;

        /// <summary>
        /// makes a wait cursor
        /// </summary>
        public WaitCursor()
        {
            if (Mouse.OverrideCursor == Cursors.Wait) return;
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        /// <inheritdoc />
        /// <summary>
        /// change cursor back
        /// </summary>
        public void Dispose()
        {
            Mouse.OverrideCursor = _previousCursor;
            Dispose(true);
        }

        /// <summary>
        /// clean up managed and native
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (_previousCursor != null)
                {
                    _previousCursor.Dispose();
                    _previousCursor = null;
                }
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
    }
}
