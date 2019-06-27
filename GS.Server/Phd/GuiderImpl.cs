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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GS.Server.Phd
{
    internal class GuiderConnection : IDisposable
    {
        private TcpClient tcpCli;
        private StreamWriter sw;
        private StreamReader sr;

        public bool Connect(string hostname, ushort port)
        {
            try
            {
                tcpCli = new TcpClient();
                if (!tcpCli.ConnectAsync(hostname, port).Wait(5000))
                {
                    // connection failure
                    throw new GuiderException(ErrorCode.NewConnection, $"Timeout connecting to PHD2 instance {port} on {hostname}");
                }
                sw = new StreamWriter(tcpCli.GetStream()) { AutoFlush = true, NewLine = "\r\n" };
                sr = new StreamReader(tcpCli.GetStream());
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (sw != null)
            {
                sw.Close();
                sw.Dispose();
                sw = null;
            }
            if (sr != null)
            {
                sr.Close();
                sr.Dispose();
                sr = null;
            }

            if (tcpCli == null) return;
            Debug.WriteLine("Disconnect from phd2");
            tcpCli.Close();
            tcpCli = null;
        }

        public bool IsConnected => tcpCli != null && tcpCli.Connected;

        public string ReadLine()
        {
            try
            {
                return sr.ReadLine();
            }
            catch (Exception)
            {
                // phd2 disconnected
                return null;
            }
        }

        public void WriteLine(string s)
        {
            sw.WriteLine(s);
        }

        public void Terminate()
        {
            tcpCli?.Close();
        }
    }

    class Accum
    {
        uint n;
        double a;
        double q;
        double peak;

        public Accum()
        {
            Reset();
        }
        public void Reset()
        {
            n = 0;
            a = q = peak = 0;
        }
        public void Add(double x)
        {
            var ax = Math.Abs(x);
            if (ax > peak) peak = ax;
            ++n;
            var d = x - a;
            a += d / n;
            q += (x - a) * d;
        }
        public double Mean()
        {
            return a;
        }
        public double Stdev()
        {
            return n >= 1 ? Math.Sqrt(q / n) : 0.0;
        }
        public double Peak()
        {
            return peak;
        }
    }


    internal class GuiderImpl : Guider
    {
        public static PropertyChangedEventHandler PropertyChanged;
        private CancellationTokenSource Token { get; }

        private readonly string m_host;
        private readonly uint m_instance;
        private readonly GuiderConnection m_conn;
        private Thread m_worker;
        private bool m_terminate;
        private readonly object m_sync = new object();
        private JObject m_response;
        private readonly Accum accum_ra = new Accum();
        private readonly Accum accum_dec = new Accum();
        private bool accum_active;
        private double settle_px;
        private string AppState;
        private double AvgDist;
        private GuideStats Stats;
        //private string Version;
        //private string PHDSubver;
        private SettleProgress mSettle;
        public double LastPixelScale { get; private set; }

        private GuideStep _phdGuideStep;
        public GuideStep PhdGuideStep
        {
            get => _phdGuideStep;
            private set
            {
                _phdGuideStep = value;
                OnPropertyChanged();
            }
        }

        //private async Task PhdLoopAsync()
        //{
        //    try
        //    {
        //        if (ctsPhd == null) ctsPhd = new CancellationTokenSource();
        //        _cts = ctsPhd.Token;
        //        _taskRunning = true;
        //        var task = Task.Run(() =>
        //        {
        //            while (_taskRunning)
        //            {
        //                if (_cts.IsCancellationRequested)
        //                {
        //                    _taskRunning = false;
        //                }
        //                else
        //                {
        //                    DoWork();
        //                }
        //            }
        //        }, _cts);
        //        await task;
        //        task.Wait(_cts);
        //        _taskRunning = false;
        //        m_conn.Terminate();
        //        m_conn.Close();
        //        Close();
        //    }
        //    catch (GuiderException ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        switch (ex.ErrorCode)
        //        {
        //            case ErrorCode.LostConnection:
        //                PhdException = ex;
        //                Close();
        //                break;
        //            case ErrorCode.NewConnection:
        //                PhdException = ex;
        //                Close();
        //                break;
        //            case ErrorCode.Disconnected:
        //                PhdException = ex;
        //                Close();
        //                break;
        //            case ErrorCode.GuidingError:
        //                PhdException = ex;
        //                Close();
        //                break;
        //            case ErrorCode.NoResponse:
        //                PhdException = ex;
        //                Close();
        //                break;
        //            default:
        //                throw;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var monitorItem = new MonitorEntry
        //        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
        //        MonitorLog.LogToMonitor(monitorItem);

        //        Close();
        //        throw;
        //    }
        //    finally
        //    {
        //        _taskRunning = false;
        //        m_conn.Terminate();
        //    }
        //}

        public void DoWork()
        {
            if (m_terminate) return;

            var line = m_conn.ReadLine();
            if (Token.IsCancellationRequested) return;

            if (line == null)
            {
                //  phd2 disconnected
                throw new GuiderException(ErrorCode.LostConnection, $"PHD2 instance {m_instance} on {m_host}");
            }

            Debug.WriteLine($"L: {line}");

            JObject j = null;
            try
            {
                j = JObject.Parse(line);
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine($"ignoring invalid json from server: {ex.Message}: {line}");
            }

            if (j != null && j.ContainsKey("jsonrpc"))
            {
                // a response
                Debug.WriteLine($"R: {line}");
                lock (m_sync)
                {
                    m_response = j;
                    Monitor.Pulse(m_sync);
                }
            }
            else
            {
                HandleEvent(j);
            }
        }

        //private static void Worker(object obj)
        //{
        //    var impl = (GuiderImpl)obj;
        //    impl.DoWork();
        //}

        private void HandleEvent(JObject ev)
        {
            var e = (string)ev["Event"];

            if (e == "AppState")
            {
                lock (m_sync)
                {
                    AppState = (string)ev["State"];
                    if (Is_guiding(AppState))
                        AvgDist = 0.0;   // until we get a GuideStep event
                }
            }
            else if (e == "Version")
            {
                lock (m_sync)
                {
                    //Version = (string)ev["PHDVersion"];
                    //PHDSubver = (string)ev["PHDSubver"];
                }
            }
            else if (e == "StartGuiding")
            {
                accum_active = true;
                accum_ra.Reset();
                accum_dec.Reset();

                var stats = Accum_get_stats(accum_ra, accum_dec);

                //GuideStartTime = DateTime.Now;
                // LastPixelScale = PixelScale();

                lock (m_sync)
                {
                    Stats = stats;

                }
            }
            else if (e == "GuideStep")
            {
                GuideStats stats = null;
                if (accum_active)
                {
                    accum_ra.Add((double)ev["RADistanceRaw"]);
                    accum_dec.Add((double)ev["DECDistanceRaw"]);
                    stats = Accum_get_stats(accum_ra, accum_dec);
                }

                lock (m_sync)
                {
                    AppState = "Guiding";
                    AvgDist = (double)ev["AvgDist"];
                    if (accum_active) Stats = stats;
                }

                //send to UI
                ProcessGuideStep(ev);
            }
            else if (e == "SettleBegin")
            {
                accum_active = false;  // exclude GuideStep messages from stats while settling
            }
            else if (e == "Settling")
            {
                var s = new SettleProgress
                {
                    Done = false,
                    Distance = (double)ev["Distance"],
                    SettlePx = settle_px,
                    Time = (double)ev["Time"],
                    SettleTime = (double)ev["SettleTime"],
                    Status = 0
                };
                lock (m_sync)
                {
                    mSettle = s;
                }
            }
            else if (e == "SettleDone")
            {
                accum_active = true;
                accum_ra.Reset();
                accum_dec.Reset();

                var stats = Accum_get_stats(accum_ra, accum_dec);

                var s = new SettleProgress
                {
                    Done = true,
                    Status = (int)ev["Status"],
                    Error = (string)ev["Error"]
                };

                lock (m_sync)
                {
                    mSettle = s;
                    Stats = stats;
                }
            }
            else if (e == "Paused")
            {
                lock (m_sync)
                {
                    AppState = "Paused";
                }
            }
            else if (e == "StartCalibration")
            {
                lock (m_sync)
                {
                    AppState = "Calibrating";
                }
            }
            else if (e == "LoopingExposures")
            {
                lock (m_sync)
                {
                    AppState = "Looping";
                }
            }
            else if (e == "LoopingExposuresStopped" || e == "GuidingStopped")
            {
                lock (m_sync)
                {
                    AppState = "Stopped";
                }
            }
            else if (e == "StarLost")
            {
                lock (m_sync)
                {
                    AppState = "LostLock";
                    AvgDist = (double)ev["AvgDist"];
                }
            }
            else
            {
                Debug.WriteLine($"todo: handle event {e}");
            }
        }

        private static string MakeJsonrpc(string method, JToken param)
        {
            var req = new JObject { ["method"] = method, ["id"] = 1 };

            if (param == null || param.Type == JTokenType.Null) return req.ToString(Formatting.None);
            if (param.Type == JTokenType.Array || param.Type == JTokenType.Object)
                req["params"] = param;
            else
            {
                // single non-null parameter
                var ary = new JArray { param };
                req["params"] = ary;
            }
            return req.ToString(Formatting.None);
        }

        private static bool Failed(JObject res)
        {
            return res.ContainsKey("error");
        }

        public GuiderImpl(string hostname, uint phd2_instance, CancellationTokenSource token)
        {
            m_host = hostname;
            m_instance = phd2_instance;
            m_conn = new GuiderConnection();
            Token = token;
        }

        public override void Close()
        {
            Dispose();
            //GC.SuppressFinalize(this);
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_worker != null)
                {
                    m_terminate = true;
                    m_conn.Terminate();
                    m_worker.Join();
                    m_worker = null;
                }

                m_terminate = true;
                m_conn.Terminate();
            }
        }

        // connect to PHD2 -- you'll need to call Connect before calling any of the server API methods below
        public override void Connect()
        {

                var port = (ushort)(4400 + m_instance - 1);
                if (!m_conn.Connect(m_host, port))
                {
                    throw new GuiderException(ErrorCode.NewConnection, $"Could not connect to PHD2 instance {m_instance} on {m_host}");
                }
   

            m_terminate = false;
            //PhdLoopAsync();
        }

        private static GuideStats Accum_get_stats(Accum ra, Accum dec)
        {
            var stats = new GuideStats
            {
                rms_ra = ra.Stdev(),
                rms_dec = dec.Stdev(),
                peak_ra = ra.Peak(),
                peak_dec = dec.Peak()
            };
            return stats;
        }

        private static bool Is_guiding(string st)
        {
            return st == "Guiding" || st == "LostLock";
        }

        // these two member functions are for raw JSONRPC method invocation. Generally you won't need to
        // use these functions as it is much more convenient to use the higher-level methods below
        public override JObject Call(string method)
        {
            return Call(method, null);
        }

        public override JObject Call(string method, JToken param)
        {
            var s = MakeJsonrpc(method, param);
            Debug.WriteLine($"Call: {s}");

            // send request
            m_conn.WriteLine(s);

            // wait for response
            JObject response = null;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed.TotalMilliseconds < 5000)
            {
                if (m_response == null) continue;
                response = m_response;
                m_response = null;
                break;
            }
            stopwatch.Stop();

            //lock (m_sync)
            //{
            //    while (m_response == null)
            //        Monitor.Wait(m_sync);

            //    var response = m_response;
            //    m_response = null;

            if (!Failed(response)) return response;
            if (response != null)
                    throw new GuiderException(ErrorCode.NoResponse, (string) response["error"]["message"]);

            return response;
           // }
        }

        private static JObject SettleParam(double settlePixels, double settleTime, double settleTimeout)
        {
            var s = new JObject { ["pixels"] = settlePixels, ["time"] = settleTime, ["timeout"] = settleTimeout };
            return s;
        }

        private void CheckConnected()
        {
            if (!m_conn.IsConnected)
            {
                throw new GuiderException(ErrorCode.Disconnected, "PHD2 Server disconnected");
            }
        }

        public bool IsConnected()
        {
            return m_conn.IsConnected;
        }

        // Start guiding with the given settling parameters. PHD2 takes care of looping exposures,
        // guide star selection, and settling. Call CheckSettling() periodically to see when settling
        // is complete.
        public override void Guide(double settlePixels, double settleTime, double settleTimeout)
        {
            CheckConnected();

            var s = new SettleProgress
            {
                Done = false,
                Distance = 0.0,
                SettlePx = settlePixels,
                Time = 0.0,
                SettleTime = settleTime,
                Status = 0
            };

            lock (m_sync)
            {
                if (mSettle != null)
                    throw new GuiderException(ErrorCode.GuidingError, "cannot guide while settling");
                mSettle = s;
            }

            try
            {
                var param = new JArray { SettleParam(settlePixels, settleTime, settleTimeout), false };
                // don't force calibration

                Call("guide", param);
                settle_px = settlePixels;
            }
            catch (Exception)
            {
                // failed - remove the settle state
                lock (m_sync)
                {
                    mSettle = null;
                }
                throw;
            }
        }

        // Dither guiding with the given dither amount and settling parameters. Call CheckSettling()
        // periodically to see when settling is complete.
        public override void Dither(double ditherPixels, double settlePixels, double settleTime, double settleTimeout)
        {
            CheckConnected();

            var s = new SettleProgress
            {
                Done = false,
                Distance = ditherPixels,
                SettlePx = settlePixels,
                Time = 0.0,
                SettleTime = settleTime,
                Status = 0
            };

            lock (m_sync)
            {
                if (mSettle != null)
                    throw new GuiderException(ErrorCode.GuidingError, "cannot dither while settling");

                mSettle = s;
            }

            try
            {
                var param = new JArray { ditherPixels, false, SettleParam(settlePixels, settleTime, settleTimeout) };

                Call("dither", param);
                settle_px = settlePixels;
            }
            catch (Exception)
            {
                // call failed - remove the settle state
                lock (m_sync)
                {
                    mSettle = null;
                }
                throw;
            }
        }

        // Check if phd2 is currently in the process of settling after a Guide or Dither
        public override bool IsSettling()
        {
            CheckConnected();

            lock (m_sync)
            {
                if (mSettle != null)
                {
                    return true;
                }
            }

            // for app init, initialize the settle state to a consistent value
            // as if Guide had been called

            var res = Call("get_settling");

            var val = (bool)res["result"];

            if (val)
            {
                var s = new SettleProgress
                {
                    Done = false,
                    Distance = -1.0,
                    SettlePx = 0.0,
                    Time = 0.0,
                    SettleTime = 0.0,
                    Status = 0
                };
                lock (m_sync)
                {
                    if (mSettle == null)
                        mSettle = s;
                }
            }

            return val;
        }

        // Get the progress of settling
        public override SettleProgress CheckSettling()
        {
            CheckConnected();

            var ret = new SettleProgress();

            lock (m_sync)
            {
                if (mSettle == null)
                    throw new GuiderException(ErrorCode.GuidingError, "not settling");

                if (mSettle.Done)
                {
                    // settle is done
                    ret.Done = true;
                    ret.Status = mSettle.Status;
                    ret.Error = mSettle.Error;
                    mSettle = null;
                }
                else
                {
                    // settle in progress
                    ret.Done = false;
                    ret.Distance = mSettle.Distance;
                    ret.SettlePx = settle_px;
                    ret.Time = mSettle.Time;
                    ret.SettleTime = mSettle.SettleTime;
                }
            }

            return ret;
        }

        // Get the guider statistics since guiding started. Frames captured while settling is in progress
        // are excluded from the stats.
        public override GuideStats GetStats()
        {
            CheckConnected();

            GuideStats stats;
            lock (m_sync)
            {
                stats = Stats.Clone();
            }
            stats.rms_tot = Math.Sqrt(stats.rms_ra * stats.rms_ra + stats.rms_dec * stats.rms_dec);
            return stats;
        }

        // stop looping and guiding
        public override void StopCapture(uint timeoutSeconds = 10)
        {
            Call("stop_capture");

            for (uint i = 0; i < timeoutSeconds; i++)
            {
                string appstate;
                lock (m_sync)
                {
                    appstate = AppState;
                }
                Debug.WriteLine($"StopCapture: AppState = {appstate}");
                if (appstate == "Stopped")
                    return;

                Thread.Sleep(1000);
                CheckConnected();
            }
            Debug.WriteLine("StopCapture: timed-out waiting for stopped");

            // hack! workaround bug where PHD2 sends a GuideStep after stop request and fails to send GuidingStopped
            var res = Call("get_app_state");
            var st = (string)res["result"];

            lock (m_sync)
            {
                AppState = st;
            }

            if (st == "Stopped")
                return;
            // end workaround

            throw new GuiderException(ErrorCode.GuidingError, $"guider did not stop capture after {timeoutSeconds} seconds!");
        }

        // start looping exposures
        public override void Loop(uint timeoutSeconds = 10)
        {
            CheckConnected();

            // already looping?
            lock (m_sync)
            {
                if (AppState == "Looping")
                    return;
            }

            var res = Call("get_exposure");
            var exp = (int)res["result"];

            Call("loop");

            Thread.Sleep(exp);

            for (uint i = 0; i < timeoutSeconds; i++)
            {
                lock (m_sync)
                {
                    if (AppState == "Looping")
                        return;
                }

                Thread.Sleep(1000);
                CheckConnected();
            }

            throw new GuiderException(ErrorCode.GuidingError, "timed-out waiting for guiding to start looping");
        }

        // get the guider pixel scale in arc-seconds per pixel
        public override double PixelScale()
        {
            var res = Call("get_pixel_scale");
            var r = (double)res["result"];
            LastPixelScale = r;
            return r;
        }

        // get a list of the Equipment Profile names
        public override List<string> GetEquipmentProfiles()
        {
            var res = Call("get_profiles");

            var profiles = new List<string>();

            var ary = (JArray)res["result"];
            foreach (var p in ary)
            {
                var name = (string)p["name"];
                profiles.Add(name);
            }

            return profiles;
        }

        static JToken GetDefault(JToken obj, string name, JToken dflt)
        {
            return ((JObject)obj).ContainsKey(name) ? obj[name] : dflt;
        }

        //private const uint DEFAULT_STOPCAPTURE_TIMEOUT = 10;

        // connect the equipment in an equipment profile
        public override void ConnectEquipment(string profileName)
        {
            var res = Call("get_profile");

            var prof = (JObject)res["result"];

            var profname = profileName;

            if ((string)prof["name"] != profname)
            {
                res = Call("get_profiles");
                var profiles = (JArray)res["result"];
                var profid = -1;
                foreach (var p in profiles)
                {
                    var name = (string)p["name"];
                    Debug.WriteLine($"found profile {name}");
                    if (name == profname)
                    {
                        profid = (int)GetDefault(p, "id", new JValue(-1));
                        Debug.WriteLine($"found profid {profid}");
                        break;
                    }
                }
                if (profid == -1)
                    throw new GuiderException(ErrorCode.GuidingError, "invalid phd2 profile name: " + profname);

                StopCapture();

                Call("set_connected", new JValue(false));
                Call("set_profile", new JValue(profid));
            }

            Call("set_connected", new JValue(true));
        }

        // disconnect equipment
        public override void DisconnectEquipment()
        {
            StopCapture();
            Call("set_connected", new JValue(false));
        }

        // get the AppState (https://github.com/OpenPHDGuiding/phd2/wiki/EventMonitoring#appstate)
        // and current guide error
        public override void GetStatus(out string appState, out double avgDist)
        {
            CheckConnected();

            lock (m_sync)
            {
                appState = AppState;
                avgDist = AvgDist;
            }
        }

        // check if currently guiding
        public override bool IsGuiding()
        {
            GetStatus(out var st, out _);
            return Is_guiding(st);
        }

        // pause guiding (looping exposures continues)
        public override void Pause()
        {
            Call("set_paused", new JValue(true));
        }

        // un-pause guiding
        public override void Unpause()
        {
            Call("set_paused", new JValue(false));
        }

        // save the current guide camera frame (FITS format), returning the name of the file.
        // The caller will need to remove the file when done.
        public override string SaveImage()
        {
            var res = Call("save_image");
            return (string)res["result"]["filename"];
        }

        /// <summary>
        /// trigger the property event for the UI to pick up the property
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        private void ProcessGuideStep(JObject ev)
        {
            var guideevent = JsonConvert.DeserializeObject<GuideStep>(ev.ToString());

            //convert unix epoch time to local time
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            guideevent.LocalTimeStamp = epoch.AddSeconds(guideevent.TimeStamp).ToLocalTime();
            var result = DateTime.Now.Subtract(guideevent.LocalTimeStamp);
            guideevent.LocalTimeStamp += result;

            // check connection before updating property
            CheckConnected();

            //update property to send to UI
            PhdGuideStep = guideevent;

        }

        #region Test Data
        private CancellationTokenSource ctsTest;
        public bool TestDataOn
        {
            get => IsTestDataOn;
            set
            {
                if (value)
                {
                    RunTestdata();
                }
                else
                {
                    StopTestdata();
                }
            }
        }
        private bool IsTestDataOn { get; set; }
        private void RunTestdata()
        {
            IsTestDataOn = true;
            TestDataLoopAsync();
        }
        private void StopTestdata()
        {
            ctsTest?.Cancel();
            ctsTest?.Dispose();
            ctsTest = null;
            IsTestDataOn = false;
        }
        private async void TestDataLoopAsync()
        {
            IsTestDataOn = true;
            var r = new Random();
            //var start = 0;
            try
            {
                if (ctsTest == null) ctsTest = new CancellationTokenSource();
                var ct = ctsTest.Token;
                var IsReading = true;
                var task = Task.Run(() =>
                {
                    while (IsReading)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            // Clean up here, then...
                            // ct.ThrowIfCancellationRequested();
                            IsReading = false;
                        }
                        else
                        {
                            CheckConnected();

                            IsTestDataOn = true;
                            //var end = start + r.Next(0, 100);
                            const string str = @"{ 'Event':'GuideStep','Timestamp':1548901403.326,'Host':'ANDREW-W7','Inst':1,'Frame':30,'Time':10.663,'Mount':'GSServer','dx':-0.037,'dy':-0.069,'RADistanceRaw':-0.037,'DECDistanceRaw':-0.069,'RADistanceGuide':0.000,'DECDistanceGuide':0.000,'StarMass':20716,'SNR':75.51,'AvgDist':6.87}";
                            var j = JObject.Parse(str);

                            var eve = JsonConvert.DeserializeObject<GuideStep>(j.ToString());

                            eve.RADistanceRaw = r.NextDouble() * (-.5 - .5) + .5;
                            eve.DecDistanceRaw = r.NextDouble() * (-.5 - .5) + .5;

                            //converted time
                            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            eve.LocalTimeStamp = epoch.AddSeconds(eve.TimeStamp).ToLocalTime();
                            var result = DateTime.Now.Subtract(eve.LocalTimeStamp);
                            eve.LocalTimeStamp += result;
                            //LastPixelScale = 12.8916;
                            LastPixelScale = .99;

                            //   TimeSpan tm = Principles.HiResDateTime.UtcNow - new DateTime(1970, 1, 1);
                            //   int secondsSinceEpoch = (int)t.TotalSeconds;

                            if (!ct.IsCancellationRequested) PhdGuideStep = eve;

                            Thread.Sleep(300);

                            //TimeZoneInfo.ConvertTime(DateTimeOffset, TimeZoneInfo)
                            //public static DateTimeOffset FromUnixTimeSeconds(long seconds);

                        }
                    }
                }, ct);
                await task;
                task.Wait(ct);
                IsTestDataOn = false;
                StopTestdata();
            }
            catch (Exception)
            {
                IsTestDataOn = false;
                StopTestdata();
                throw;
            }
        }
        #endregion
    }
}
