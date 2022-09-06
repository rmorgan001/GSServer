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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace GS.Shared
{
    public static class Languages
    {
        private static readonly string _directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\LanguageFiles\\";
        private const string gsServer = "GSServer_";
        private const string gsChart = "GSChart_";
        private const string gsUtil = "GSUtil_";

        static Languages()
        {
            var langs = new List<string>(){ "en-US","fr-FR","it-IT" };
            SupportedLanguages = langs;
            Settings.Load();
        }
        public static void SetLanguageDictionary(bool local, LanguageApp app, string language)
        {
             if (local)
             {
                 SetLanguageDictionary(Thread.CurrentThread.CurrentCulture.ToString(), app);
             }
             else
             {
                 string lang;
                 switch (app)
                 {
                     case LanguageApp.GSServer:
                         lang = Settings.Language;
                         break;
                     case LanguageApp.GSChartViewer:
                         lang = language;
                         break;
                     case LanguageApp.GSUtilities:
                         lang = language;
                         break;
                     default:
                         lang = "en-US";
                         break;
                 }
                
                 SetLanguageDictionary(DoesCultureExist(lang) ? lang : "en-US", app);
             }
        }
        private static bool DoesCultureExist(string cultureName)
        {
            return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(culture => string.Equals(culture.Name, cultureName, StringComparison.CurrentCultureIgnoreCase));
        }
        private static void SetLanguageDictionary(string cultureInfo, LanguageApp app)
        {
            var dict = new ResourceDictionary();
            Uri uri = null;
            var localpath = string.Empty;
            var filenotfound = false;
            switch (app)
            {
                case LanguageApp.GSServer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localpath = new Uri(Path.Combine(_directoryPath, $"{gsServer}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localpath))
                            {
                                filenotfound = true;
                                uri = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            }
                            break;
                    }
                    break;
                case LanguageApp.GSChartViewer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localpath = new Uri(Path.Combine(_directoryPath, $"{gsChart}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localpath))
                            {
                                filenotfound = true;
                                uri = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            }
                            break;
                    }
                    break;
                case LanguageApp.GSUtilities:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResUtil_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localpath = new Uri(Path.Combine(_directoryPath, $"{gsUtil}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localpath))
                            {
                                filenotfound = true;
                                uri = new Uri("GS.Shared;component/Languages/StringResUtil_en-us.xaml", UriKind.Relative);
                            }
                            break;
                    }
                    break;
            }

            switch (cultureInfo)
            {
                case "en-US":
                    dict.Source = uri;
                    if (dict.Source == null) { throw new Exception("Language source missing"); }
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                    break;
                 default:
                     if (filenotfound)
                     {
                         dict.Source = uri;
                         if (dict.Source == null)
                         {
                             throw new Exception("Language source missing");
                         }
                         Application.Current.Resources.MergedDictionaries.Add(dict);
                     }
                     else
                     {
                         using (var fs = new FileStream(localpath, FileMode.Open, FileAccess.Read))
                         {
                             var dic = (ResourceDictionary) XamlReader.Load(fs);
                             //Resources.MergedDictionaries.Clear();
                             Application.Current.Resources.MergedDictionaries.Add(dic);
                         }
                     }
                     break;
            }
        }
        public static List<string> SupportedLanguages { get; }
        public static string Language
        {
            get => Settings.Language;
            set => Settings.Language = DoesCultureExist(value) ? value : "en-US";
        }
    }

    public enum LanguageApp
    {
        GSServer = 1,
        GSChartViewer = 2,
        GSUtilities = 3
    }
}