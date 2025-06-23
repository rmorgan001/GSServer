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
        private static readonly string DirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\LanguageFiles\";
        private const string GsServer = "GSServer_";
        private const string GsChart = "GSChart_";
        private const string GsUtil = "GSUtil_";

        static Languages()
        {
            var langs = new List<string>(){ "en-US","de-DE","fr-FR","it-IT" };
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
                     case LanguageApp.GsServer:
                         lang = Settings.Language;
                         break;
                     case LanguageApp.GsChartViewer:
                         lang = language;
                         break;
                     case LanguageApp.GsUtilities:
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
            var localPath = string.Empty;
            var fileNotFound = false;
            switch (app)
            {
                case LanguageApp.GsServer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localPath = new Uri(Path.Combine(DirectoryPath, $"{GsServer}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localPath))
                            {
                                fileNotFound = true;
                                uri = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            }
                            break;
                    }
                    break;
                case LanguageApp.GsChartViewer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localPath = new Uri(Path.Combine(DirectoryPath, $"{GsChart}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localPath))
                            {
                                fileNotFound = true;
                                uri = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            }
                            break;
                    }
                    break;
                case LanguageApp.GsUtilities:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            uri = new Uri("GS.Shared;component/Languages/StringResUtil_en-us.xaml", UriKind.Relative);
                            break;
                        default:
                            localPath = new Uri(Path.Combine(DirectoryPath, $"{GsUtil}{cultureInfo}.xaml")).LocalPath;
                            if (!File.Exists(localPath))
                            {
                                fileNotFound = true;
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
                     if (fileNotFound)
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
                         using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read))
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
        GsServer = 1,
        GsChartViewer = 2,
        GsUtilities = 3
    }
}