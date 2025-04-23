/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Threading.Tasks;

namespace GS.Shared
{
    public static class Tasks
    {
        /// <summary>
        /// a extension method version that incorporates cancellation of the timeout when the original task completes as suggested
        /// </summary>
        /// <remarks>https://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout</remarks>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task<TResult> TimeoutAfter<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask != task) throw new TimeoutException("The operation has timed out.");
                timeoutCancellationTokenSource.Cancel();
                return await task;  // Very important in order to propagate exceptions
            }
        }

        /// <summary>
        /// This method depends on the system clock. This means that the time delay will approximately equal
        /// the resolution of the system clock if the millisecondsDelay argument is less than the resolution
        /// of the system clock, which is approximately 15 milliseconds on Windows systems.
        /// </summary>
        /// <param name="ms"></param>
        public static async void DelayHandler(int ms)
        {
            // whatever you need to do before delay goes here         
            await Task.Delay(ms);
            // whatever you need to do after delay.
        }

        /// <summary>
        /// Delay with cancel token and no error
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="ct"></param>
        public static async Task DelayHandler(int ms, CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested) return;
                // whatever you need to do before delay goes here         
                await Task.Delay(ms, ct);
                // whatever you need to do after delay.
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
