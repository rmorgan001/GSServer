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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Speech.Synthesis;
//using Microsoft.Speech.Synthesis;
using System.Threading;

namespace GS.Server.Helpers
{
    internal static class Synthesizer
    {
        private static SpeechSynthesizer _synthesizer;
        //public static event PropertyChangedEventHandler StaticPropertyChanged;

        public static Exception LastError { get; set; }
        private static IList<string> VoiceNames;
        private static string VoiceName => Settings.Settings.VoiceName;
        private static int Rate { get; set; }
        private static int Volume => Settings.Settings.VoiceVolume;
        private static bool VoiceActive
        {
            get => Settings.Settings.VoiceActive;
            set => Settings.Settings.VoiceActive = value;
        }
        public static bool VoicePause { get; set; }
        //private static bool VoiceValid
        //{
        //    get
        //    {
        //        if (VoiceName != null && VoiceName.ToLower() != "none" && VoiceNames != null)
        //        {
        //            return VoiceNames.Contains(VoiceName);
        //        }
        //        return false;
        //    }
        //}

        //#region Synthesizer events

        //private static void Synthesizer_StateChanged(object sender, StateChangedEventArgs e)
        //{
        //}

        //private static void Synthesizer_SpeakStarted(object sender, SpeakStartedEventArgs e)
        //{
        //}

        //private static void Synthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        //{
        //}

        //private static void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        //{
        //}

        //#endregion

        static Synthesizer()
        {
            try
            {
                LastError = null;
                LoadSpeechSynthesizer();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                LastError = ex;
                VoiceActive = false;
            }
        }
        private static void LoadSpeechSynthesizer()
        {
            if (_synthesizer == null)
            {
                _synthesizer = new SpeechSynthesizer();
            }

            Rate = 0;
            //_synthesizer.StateChanged += Synthesizer_StateChanged;
            //_synthesizer.SpeakStarted += Synthesizer_SpeakStarted;
            //_synthesizer.SpeakProgress += Synthesizer_SpeakProgress;
            //_synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
            GetVoices();
            VoicePause = false;
        }
        public static void Speak(string text)
        {
            try
            {
                LastError = null;
                //if (!VoiceValid){return;}
                if (!VoiceActive){return;}
                if (VoicePause){return;}
                _synthesizer.SelectVoice(VoiceName);
                _synthesizer.Rate = Rate;
                _synthesizer.Volume = Volume;
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{text}| {ex.Message}| {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                LastError = ex;
                VoiceActive = false;
            }
        }
        public static IList<string> GetVoices()
        {
            try
            {
                VoiceNames = new List<string>();
                if (_synthesizer == null) { _synthesizer = new SpeechSynthesizer(); }
                var voices = _synthesizer.GetInstalledVoices();
                foreach (var voice in voices)
                {
                    if (voice.Enabled)
                    {
                        VoiceNames.Add(voice.VoiceInfo.Name);
                    }
                }
                if (voices.Count == 0) { VoiceActive = false; }
                return VoiceNames;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}| {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                //LastError = ex;
                //VoiceActive = false;
                return null;
            }
        }

        //private static void UnLoadSpeechSynthesizer()
        //{
        //    try
        //    {
        //        if (_synthesizer == null) return;
        //        // _synthesizer.StateChanged -= Synthesizer_StateChanged;
        //        // _synthesizer.SpeakStarted -= Synthesizer_SpeakStarted;
        //        // _synthesizer.SpeakProgress -= Synthesizer_SpeakProgress;
        //        // _synthesizer.SpeakCompleted -= Synthesizer_SpeakCompleted;
        //        _synthesizer.Dispose();
        //        _synthesizer = null;
        //    }
        //    catch (Exception e)
        //    {
        //        _synthesizer = null;
        //        var monitorItem = new MonitorEntry
        //            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{e.Message}|{e.StackTrace}" };
        //        MonitorLog.LogToMonitor(monitorItem);
        //    }

        //}

        //public static void SpeakBool(string text, bool bol)
        //{
        //    try
        //    {
        //        if (!VoiceValid) return;
        //        if (!VoiceActive) return;
        //        if (VoicePause) return;
        //        _synthesizer.SelectVoice(VoiceName);
        //        _synthesizer.Rate = Rate;
        //        _synthesizer.Volume = Volume;
        //        var b = bol ? " On" : " Off";
        //        _synthesizer.SpeakAsyncCancelAll();
        //        _synthesizer.SpeakAsync(text + b);
        //    }
        //    catch (Exception e)
        //    {
        //        VoiceActive = false;
        //        var monitorItem = new MonitorEntry
        //        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{text}|{bol}|{e.Message}" };
        //        MonitorLog.LogToMonitor(monitorItem);
        //    }

        //}

        ///// <summary>
        ///// called from the setter property.  Used to update UI elements.  property name is not required
        ///// </summary>
        ///// <param name="propertyName"></param>
        //private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        //}

        public static void Beep(int item = 0, int frequency = 0, int duration = 0)
        {
            try
            {
                if (!Settings.Settings.AllowBeeps){return;}

                var freq = 800;
                var dur = 200;
                if(frequency == 0){ freq = Settings.Settings.BeepFreq; }
                if(duration == 0){ dur = Settings.Settings.BeepDur;}

                if (freq < 37 || freq > 32767){return;}
                if (dur < 0 || dur > 5000) { return; }

                switch (item)
                {
                    case 1: // Slew complete
                        break;
                }
                new Thread(() => Console.Beep(freq, dur)).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }
    }
}
