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

using GS.Principles;
using GS.Shared;
using System.Reflection;
using System.Threading;

namespace GS.Simulator
{
    internal class IOSerial
    {
        private readonly Controllers _controllers;

        internal static bool IsConnected => true;

        internal IOSerial()
        {
            _controllers = new Controllers();
        }

        internal string Send(string command)
        {
            //if (Queues.Serial.Connected) return null; 
            var received = _controllers.Command(command.ToLowerInvariant().Trim());

            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{command}={received}" };
            MonitorLog.LogToMonitor(monitorItem);

            return received;
        }

        /*  Sample Serial Code....

        #region SerialIO

        private const int _threadLockTimeout = 50; // milliseconds
        private const char _endChar = (char)13;

        /// <summary>
        /// One communication between mount and client
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="command">The command char set</param>
        /// <param name="cmdDataStr">The data need to send</param>
        /// <returns>The response string from mount</returns>
        private string CmdToAxis(AxisId axis, char command, string cmdDataStr)
        {
            for (var i = 0; i < 5; i++)
            {
                var acquiredLock = false;
                try
                {
                    Monitor.TryEnter(MountQueue.Serial, _threadLockTimeout, ref acquiredLock);
                    if (acquiredLock)
                    {
                        // Code that accesses resources that are protected by the lock.
                        try
                        {
                            MountQueue.Serial.ClearBuffers();
                            // send the request
                            cmdDataStr = SendRequest(axis, command, cmdDataStr);
                            // receive the response
                            var responseString = Receive Response(axis, command, cmdDataStr);
                            return responseString;
                        }
                        catch (TimeoutException ex)
                        {
                            throw axis == AxisId.Axis1
                                ? new MountException(ErrorCode.ErrNoResponseAxis1, "Timeout", ex)
                                : new MountException(ErrorCode.ErrNoResponseAxis2, "Timeout", ex);
                        }
                        catch (IOException ex)
                        {
                            //log

                            throw new MountException(ErrorCode.ErrNotConnected, "IO Error", ex);
                        }
                        catch (Exception)
                        {
                            //log
                            throw;
                        }
                    }
                    else
                    {
                        // deal with the fact that the lock was not acquired.
                    }
                }
                finally
                {
                    if (acquiredLock) Monitor.Exit(MountQueue.Serial);
                }
                Thread.Sleep(3);
            }
            // deal with the fact that the lock was not acquired.
            return null;
        }

        /// <summary>
        /// Builds the command string
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="command"></param>
        /// <param name="cmdDataStr"></param>
        private string SendRequest(AxisId axis, char command, string cmdDataStr)
        {
            const char startCharOut = ':';
            if (cmdDataStr == null) cmdDataStr = "";
            const int bufferSize = 20;
            var commandStr = new StringBuilder(bufferSize);
            commandStr.Append(startCharOut);                    // 0: Leading char
            commandStr.Append(command);                         // 1: Length of command( Source, destination, command char, data )
            // Target Device
            commandStr.Append(axis == AxisId.Axis1 ? '1' : '2');// 2: Target Axis
            // Copy command data to buffer
            commandStr.Append(cmdDataStr);
            commandStr.Append(_endChar);                         // CR Character            

            MountQueue.Serial.Transmit(commandStr.ToString());
            return $"{commandStr}";
        }

        /// <summary>
        /// Constructs a string from the response
        /// </summary>
        /// <returns></returns>
        private string ReceiveResponse(AxisId axis, char command, string cmdDataStr)
        {
            var sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds < MountQueue.Serial.ReceiveTimeoutMs)
            {
                var receivedData = MountQueue.Serial.ReceiveTerminated("\r");
                if (receivedData.Length <= 0) continue;

                receivedData = receivedData.Replace("\0", string.Empty).Trim();

                switch (receivedData[0].ToString())
                {
                    //receive '= DDDDDD [0D]'    or '! D [0D]'
                    case "=":  // Normal response
                        sw.Stop();
                        return receivedData;
                    case "!":  // Abnormal response.
                        string errorMsg;
                        switch (receivedData.Trim())
                        {
                            case "!0":
                                errorMsg = "Invalid Command: Command doesn't apply to the model";
                                break;
                        }
                        //log and do something about errors
                        sw.Stop();
                        return receivedData;
                }
                Thread.Sleep(1);
            }
            sw.Stop();
            throw new TimeoutException();
        }

        /// <summary>
        /// Sends :e1 to the mounts and evaluates response to see its an appropriate response.
        /// </summary>
        internal void TestSerial()
        {
            var isError = true;
            MountQueue.Serial.ClearBuffers();
            // send the request
            SendRequest(AxisId.Axis1, 'e', null);
            // receive the response
            var responseString = MountQueue.Serial.ReceiveCounted(8);

            if (responseString.Length > 0)
            {
                responseString = responseString.Replace("\0", string.Empty).Trim();
                // check to see if the response is valid 
                switch (responseString[0].ToString())
                {
                    case "=":
                        isError = false;
                        break;
                    case "!":
                        isError = false;
                        break;
                }
                // check to see if the number for the mount type is valid
                if (!isError)
                {
                    var parsed = int.TryParse(responseString.Substring(6, 1), out var mountNumber);
                    if (parsed)
                    {
                        if (mountNumber < 0 || mountNumber > 6) isError = true;
                    }
                    else
                    {
                        isError = true;
                    }
                }

            }

            if (!isError) return;
            throw new MountException(ErrorCode.ErrMountNotFound);
        }

        /// <summary>
        /// Converts the string to a long
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private long StringToLong(string str)
        {
            try
            {
                long value = 0;
                for (var i = 1; i + 1 < str.Length; i += 2)
                {
                    value += (long)(int.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
                }
                return value;
            }
            catch (FormatException e)
            {
               // AxisStop(AxisId.Axis1);
               // AxisStop(AxisId.Axis2);
                throw new MountException(ErrorCode.ErrInvalidData, "Response Parse Error: " + str, e);
            }
        }

        /// <summary>
        /// Converts a long to Hex command
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static string LongToHex(long number)
        {
            // 31 -> 0F0000
            var a = ((int)number & 0xFF).ToString("X").ToUpper();
            var b = (((int)number & 0xFF00) / 256).ToString("X").ToUpper();
            var c = (((int)number & 0xFF0000) / 256 / 256).ToString("X").ToUpper();

            if (a.Length == 1)
                a = "0" + a;
            if (b.Length == 1)
                b = "0" + b;
            if (c.Length == 1)
                c = "0" + c;
            return a + b + c;
        }
        
        #endregion
        */
    }
}
