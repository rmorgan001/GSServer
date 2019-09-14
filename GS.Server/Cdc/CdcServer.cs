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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using ASCOM.Utilities;
using GS.Shared;

namespace GS.Server.Cdc
{
    internal sealed class CdcServer: IDisposable
    {
        private TcpClient _tcpClient;
        private const string _crlf = "\r\n";
        private readonly Util _util = new Util();

        private IPAddress Ip { get; }
        private int Port { get; }
        private bool HasData { get; set; }
        private string Data { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public CdcServer(string ip, int port)
        {
            IPAddress.TryParse(ip, out var tmpIp);
            Ip = tmpIp;
            Port = port;
        }

        /// <summary>
        /// Connection to CDC server
        /// </summary>
        private void Connect()
        {
            _tcpClient = new TcpClient();
            _tcpClient?.Connect(Ip, Port);
            if (!_tcpClient.Connected)
            {
                throw new Exception(Application.Current.Resources["CdcNotFound"].ToString());
            }

        }

        /// <summary>
        /// Closes connection to CDC server
        /// </summary>
        private void Close()
        {
            if (_tcpClient == null) return;
            if (_tcpClient.Connected) _tcpClient.Close();
            _tcpClient.Close();
            _tcpClient = null;
        }

        /// <summary>
        /// Sends and Receives data from CDC server
        /// each call opens and closes any connection
        /// </summary>
        /// <param name="command"></param>
        private void SendCommand(string command)
        {
            if (command == null) return;
            Connect();
            Data = null;
            HasData = false;
            var enc = new ASCIIEncoding();
            var ba = enc.GetBytes(command + _crlf);
            Stream stm = _tcpClient.GetStream();
            stm.WriteTimeout = 3000;
            stm.ReadTimeout = 3000;
            stm.Write(ba, 0, ba.Length);
            var data = new byte[100];
            var byteCount = stm.Read(data, 0, 100);
            if (byteCount > 0)
            {
                Data = Encoding.ASCII.GetString(data);
                HasData = true;
            }
            Close();
        }

        /// <summary>
        /// Gets observatory information from CDC server
        /// </summary>
        /// <returns></returns>
        internal double[] GetObs()
        {
            SendCommand("GETOBS");
            var darray = new double[3];
            if (HasData)
            {
                if (Data.Contains("LAT:"))
                {
                    var lat = Strings.GetTxtBetween(Data, "LAT:", "LON:");
                    if (string.IsNullOrEmpty(lat))
                    {
                        darray[1] = 0;
                    }
                    else
                    {
                        darray[0] = _util.DMSToDegrees(lat.Trim());
                    }
                }

                if (Data.Contains("LON:"))
                {
                    var lon = Strings.GetTxtBetween(Data, "LON:", "ALT:");
                    if (string.IsNullOrEmpty(lon))
                    {
                        darray[1] = 0;
                    }
                    else
                    {
                        darray[1] = _util.DMSToDegrees(lon.Trim()) * -1;
                    }
                }

                if (!Data.Contains("ALT:")) return darray;
                var alt = Strings.GetTxtBetween(Data, "ALT:", "OBS");
                if (string.IsNullOrEmpty(alt))
                {
                    darray[2] = 0;
                }
                else
                {
                    alt = alt.Replace("M", string.Empty);
                    alt = alt.Replace("m", string.Empty);
                    var parsed = double.TryParse(alt.Trim(), out var tmpalt);
                    if (parsed)
                    {
                        darray[2] = tmpalt;
                    }
                    else
                    {
                        darray[2] = 0;
                    }
                }
            }
            else
            {
                throw new Exception(Application.Current.Resources["CdcOBNotFound"].ToString());
            }
            return darray;
        }

        /// <summary>
        /// Updates CDC server observatory information
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="alt"></param>
        internal void SetObs(double lat, double lon, double alt)
        {
            //LAT:+00d00m00sLON:+000d00m00s ALT:000mOBS:name
            var latplus = lat > 0.0 ? "+" : "-";
            var lonplus = lon > 0.0 ? "-" : "+";
            lat = Math.Abs(lat);
            lon = Math.Abs(lon);
            var latstr = _util.DegreesToDMS(lat, "d", "m", "s", 3);
            var lonstr = _util.DegreesToDMS(lon, "d", "m", "s", 3);
            var altstr = $"{alt}m";
            const string name = "GSServer";
            var command = $"SETOBS LAT:{latplus}{latstr}LON:{lonplus}{lonstr}ALT:{altstr}OBS:{name}";
            SendCommand(command);
        }

        public void Dispose()
        {
            Dispose(true);
            // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            // dispose managed resources
            _util.Dispose();
            _tcpClient.Dispose();
            // free native resources
        }
    }
}
