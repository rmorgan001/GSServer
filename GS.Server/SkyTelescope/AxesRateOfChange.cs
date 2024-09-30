using System;
using System.Windows;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// 
    /// </summary>
    internal static class AxesRateOfChange
    {
        /// <summary>
        /// 
        /// </summary>
        static AxesRateOfChange()
        {
            Reset();
        }

        private static Vector _currentAxisAngles;

        private static Vector _previousAxisAngles;

        private static long _currentTicks;

        private static long _previousTicks;

        /// <summary>
        /// 
        /// </summary>
        public static Vector AxisVelocity
        {
            get
            {
                if (_previousTicks > 0 && _currentTicks > _previousTicks)
                {
                    var deltaTime = (double)(_currentTicks - _previousTicks) / TimeSpan.TicksPerSecond;
                    return (_currentAxisAngles - _previousAxisAngles) / deltaTime;
                }
                return new Vector(999, 999);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="time"></param>
        public static void Update(double xAxis, double yAxis, DateTime time)
        {
            _previousAxisAngles = _currentAxisAngles;
            _currentAxisAngles = new Vector(xAxis, yAxis);
            _previousTicks = _currentTicks;
            _currentTicks = time.Ticks;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Reset()
        {
            _currentAxisAngles = new Vector(0.0, 0.0);
            _previousAxisAngles = new Vector(0.0, 0.0);
            _currentTicks = 0;
            _previousTicks = 0;
        }
    }
}
