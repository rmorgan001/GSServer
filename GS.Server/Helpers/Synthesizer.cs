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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Threading;
using GS.Principles;
using GS.Shared;

namespace GS.Server.Helpers
{
    internal static class Synthesizer
    {
        private static readonly SpeechSynthesizer _synthesizer;
        public static event PropertyChangedEventHandler _staticPropertyChanged;

        private static IList<string> VoiceNames;
        
        private static string VoiceName => Settings.Settings.VoiceName;

        private static int Rate { get; }

        private static int Volume => Settings.Settings.VoiceVolume; 

        internal static bool VoiceActive
        {
            get => Settings.Settings.VoiceActive;
            set
            {
                Settings.Settings.VoiceActive = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool VoiceValid
        {
            get
            {
                if (VoiceName != null && VoiceName.ToLower() != "none" && VoiceNames != null)
                    return VoiceNames.Contains(VoiceName);
                VoiceActive = false;
                return false;
            }
        }

        #region Synthesizer events

        private static void Synthesizer_StateChanged(object sender, StateChangedEventArgs e)
        {

        }

        private static void Synthesizer_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {

        }

        private static void Synthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {

        }

        private static void Synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {

        }

        #endregion

        static Synthesizer()
        {
            Rate = 0;
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.StateChanged += Synthesizer_StateChanged;
            _synthesizer.SpeakStarted += Synthesizer_SpeakStarted;
            _synthesizer.SpeakProgress += Synthesizer_SpeakProgress;
            _synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
            GetVoices();
            if (!VoiceValid) VoiceActive = false;
        }

        public static void Speak(string text)
        {
            try
            {
                if (!VoiceValid) return;
                if (!VoiceActive) return;
                _synthesizer.SelectVoice(VoiceName);
                _synthesizer.Rate = Rate;
                _synthesizer.Volume = Volume;
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.SpeakAsync(text);
            }
            catch (Exception e)
            {
                VoiceActive = false;
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{text},{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        public static void SpeakBool(string text, bool bol)
        {
            try
            {
                if (!VoiceValid) return;
                if (!VoiceActive) return;
                _synthesizer.SelectVoice(VoiceName);
                _synthesizer.Rate = Rate;
                _synthesizer.Volume = Volume;
                var b = bol ? " On" : " Off";
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.SpeakAsync(text + b);
            }
            catch (Exception e)
            {
                VoiceActive = false;
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{text},{bol},{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }

        }

        public static IList<string> GetVoices()
        {
            VoiceNames = new List<string>();
            var voices = _synthesizer.GetInstalledVoices();
            foreach (var voice in voices)
            {
                VoiceNames.Add(voice.VoiceInfo.Name);
            }

            return VoiceNames;
        }

        /// <summary>
        /// called from the setter property.  Used to update UI elements.  propertyname is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            _staticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
