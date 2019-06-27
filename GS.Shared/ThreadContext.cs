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
using System;
using System.Threading;
using System.Windows;

namespace GS.Shared
{
    public static class ThreadContext
    {
        /// <summary>
        /// Executes on the UI thread, but calling thread waits for completion before continuing.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        public static void InvokeOnUiThread(Action action, CancellationToken token = default(CancellationToken))
        {
            if (Application.Current == null) return;
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                if (token.IsCancellationRequested) return;
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Executes on the UI thread, and calling thread doesn't wait for completion
        /// </summary>
        /// <param name="action"></param>
        public static void BeginInvokeOnUiThread(Action action)
        {
            if (Application.Current == null) return;
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(action);
            }
        }
    }
}
