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
/* Adapted from Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace GS.Principles
{
    /// <summary>
    /// Represents the Windows multimedia timer.
    /// </summary>
    public sealed class MediaTimer : IComponent
    {
        internal delegate void TimeProc(int id, int msg, int user, int param1, int param2); //timer event occurs
        private delegate void EventRaiser(EventArgs e); // methods that raise events.
        private const int TIMERR_NOERROR = 0;           // successful.
        private int _timerID;                           // Timer identifier.
        private volatile TimerMode _mode;
        private volatile int _period;                   // Period between timer events in milliseconds.
        private volatile int _resolution;               // resolution in milliseconds.
        private TimeProc _timeProcPeriodic;             // Called by Windows when a timer periodic event occurs.
        private TimeProc _timeProcOneShot;              // Called by Windows when a timer one shot event occurs.
        private EventRaiser _tickRaiser;                // Represents the method that raises the Tick event.
        private volatile bool _disposed;                // Indicates whether or not the timer has been disposed.
        private ISynchronizeInvoke _synchronizingObject;// The ISynchronizeInvoke object to use for marshaling events.
        private static readonly TimerCaps _caps;        // Multimedia timer capabilities.

        /// <summary>
        /// Occurs when the Timer has started;
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs when the Timer has stopped;
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        public event EventHandler Tick;

        /// <summary>
        /// Initialize class.
        /// </summary>
        static MediaTimer()
        {
            NativeMethods.TimeGetDevCaps(ref _caps, Marshal.SizeOf(_caps)); // Get multimedia timer capabilities.
        }

        /// <summary>
        /// Initializes a new instance of the Timer class with the specified IContainer.
        /// </summary>
        /// <param name="container">
        /// The IContainer to which the Timer will add itself.
        /// </param>
        public MediaTimer(IContainer container)
        {
            container.Add(this); // Required for Windows.Forms Class Composition Designer support
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        public MediaTimer()
        {
            Initialize();
        }

        ~MediaTimer()
        {
            if (IsRunning)
            {
                // Stop and destroy timer.
                NativeMethods.TimeKillEvent(_timerID);
            }
        }

        /// <summary>
        /// Initialize timer with default values.
        /// </summary>
        private void Initialize()
        {
            _mode = TimerMode.Periodic;
            _period = Capabilities.periodMin;
            _resolution = 1;
            IsRunning = false;
            _timeProcPeriodic = TimerPeriodicEventCallback;
            _timeProcOneShot = TimerOneShotEventCallback;
            _tickRaiser = OnTick;
        }
        
        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The timer has already been disposed.
        /// </exception>
        /// <exception cref="TimerStartException">
        /// The timer failed to start.
        /// </exception>
        public void Start()
        {
            if (_disposed) { throw new ObjectDisposedException("Timer"); }
            if (IsRunning) { return; }

            // If the periodic event callback should be used.
            var userCtx = 0;
            _timerID = NativeMethods.TimeSetEvent(Period, Resolution, Mode == TimerMode.Periodic ? _timeProcPeriodic : _timeProcOneShot, ref userCtx,(int) Mode);

            // If the timer was created successfully.
            if (_timerID != 0)
            {
                IsRunning = true;

                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                {
                    SynchronizingObject.BeginInvoke(
                        new EventRaiser(OnStarted),
                        new object[] { EventArgs.Empty });
                }
                else
                {
                    OnStarted(EventArgs.Empty);
                }
            }
            else
            {
                throw new TimerStartException("Unable to start multimedia Timer.");
            }
        }

        /// <summary>
        /// Stops timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public void Stop()
        {
            if (_disposed) { return; }
            if (!IsRunning) { return; }

            // Stop and destroy timer.
            var result = NativeMethods.TimeKillEvent(_timerID);
            Debug.Assert(result == TIMERR_NOERROR);

            IsRunning = false;

            if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
            {
                SynchronizingObject.BeginInvoke(
                    new EventRaiser(OnStopped),
                    new object[] { EventArgs.Empty });
            }
            else
            {
                OnStopped(EventArgs.Empty);
            }
        }

        // Callback method called by the Win32 multimedia timer when a timer periodic event occurs.
        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (_synchronizingObject != null)
            {
                _synchronizingObject.BeginInvoke(_tickRaiser, new object[] { EventArgs.Empty });
            }
            else
            {
                OnTick(EventArgs.Empty);
            }
        }

        // Callback method called by the Win32 multimedia timer when a timer one shot event occurs.
        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (_synchronizingObject != null)
            {
                _synchronizingObject.BeginInvoke(_tickRaiser, new object[] { EventArgs.Empty });
                Stop();
            }
            else
            {
                OnTick(EventArgs.Empty);
                Stop();
            }
        }

        // Raises the Disposed event.
        private void OnDisposed(EventArgs e)
        {
            var handler = Disposed;
            handler?.Invoke(this, e);
        }

        // Raises the Started event.
        private void OnStarted(EventArgs e)
        {
            var handler = Started;
            handler?.Invoke(this, e);
        }

        // Raises the Stopped event.
        private void OnStopped(EventArgs e)
        {
            var handler = Stopped;
            handler?.Invoke(this, e);
        }

        // Raises the Tick event.
        private void OnTick(EventArgs e)
        {
            var handler = Tick;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Gets or sets the object used to marshal event-handler calls.
        /// </summary>
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return _synchronizingObject;
            }
            set
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                _synchronizingObject = value;
            }
        }

        /// <summary>
        /// Gets or sets the time between Tick events in milliseconds.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>   
        public int Period
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }
                return _period;
            }
            set
            {
                if (_disposed)
                {
                    return;
                    // throw new ObjectDisposedException("Timer");
                }

                if (value < Capabilities.periodMin || value > Capabilities.periodMax)
                {
                    throw new ArgumentOutOfRangeException($"Period", value, "Multimedia Timer period out of range.");
                }
                _period = value;
                if (!IsRunning) return;
                Stop();
                Start();
            }
        }

        /// <summary>
        /// Gets or sets the timer resolution.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>        
        /// <remarks>
        /// The resolution is in milliseconds. The resolution increases 
        /// with smaller values; a resolution of 0 indicates periodic events 
        /// should occur with the greatest possible accuracy. To reduce system 
        /// overhead, however, you should use the maximum value appropriate 
        /// for your application.
        /// </remarks>
        public int Resolution
        {
            get
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return _resolution;
            }
            set
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }
                else if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"Resolution", value,
                        "Multimedia timer resolution out of range.");
                }

                #endregion

                _resolution = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets the timer mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public TimerMode Mode
        {
            get
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                return _mode;
            }
            set
            {
                #region Require

                if (_disposed)
                {
                    throw new ObjectDisposedException("Timer");
                }

                #endregion

                _mode = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets the timer capabilities.
        /// </summary>
        private static TimerCaps Capabilities => _caps;

        public event EventHandler Disposed;

        public ISite Site { get; set; }

        /// <summary>
        /// Frees timer resources.
        /// </summary>
        public void Dispose()
        {
            #region Guard

            if (_disposed)
            {
                return;
            }

            #endregion               

            if (IsRunning)
            {
                Stop();
            }

            _disposed = true;

            OnDisposed(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Defines constants for the multimedia Timer's event types.
    /// </summary>
    public enum TimerMode
    {
        /// <summary>
        /// Timer event occurs once.
        /// </summary>
        OneShot,

        /// <summary>
        /// Timer event occurs periodically.
        /// </summary>
        Periodic
    };

    /// <inheritdoc />
    /// <summary>
    /// The exception that is thrown when a timer fails to start.
    /// </summary>
    [Serializable]
    public class TimerStartException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the TimerStartException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception. 
        /// </param>
        public TimerStartException(string message)
            : base(message)
        {
        }

        public TimerStartException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        // Without this constructor, deserialization will fail
        protected TimerStartException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    
    /// <summary>
    /// Represents information about the multimedia Timer's capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TimerCaps
    {
        /// <summary>
        /// Minimum supported period in milliseconds.
        /// </summary>
        public readonly int periodMin;

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public readonly int periodMax;
    }
}
