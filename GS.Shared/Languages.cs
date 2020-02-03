// /* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
// 
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
//  */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace GS.Shared
{
    public static class Languages
    {
        static Languages()
        {
            var langs = new List<string>(){ "en-US" };
            SupportedLanguages = langs;
        }
        public static void SetLanguageDictionary(bool local, LanguageApp app)
        {
            if (local)
            {
                SetLanguageDictionary(Thread.CurrentThread.CurrentCulture.ToString(), app);
            }
            else
            {
                var lang = Settings.Language;
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
            switch (app)
            {
                case LanguageApp.GSServer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            break;
                        //case "fr-CA":
                        //    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.fr-CA.xaml", UriKind.Relative);
                        //    break;
                        default:
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResServer_en-us.xaml", UriKind.Relative);
                            break;
                    }
                    break;
                case LanguageApp.GSChartViewer:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            break;
                        //case "fr-CA":
                        //    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.fr-CA.xaml", UriKind.Relative);
                        //    break;
                        default:
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResChart_en-us.xaml", UriKind.Relative);
                            break;
                    }
                    break;
                case LanguageApp.GSUtilities:
                    switch (cultureInfo)
                    {
                        case "en-US":
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResUtil_en-us.xaml", UriKind.Relative);
                            break;
                        //case "fr-CA":
                        //    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.fr-CA.xaml", UriKind.Relative);
                        //    break;
                        default:
                            dict.Source = new Uri("GS.Shared;component/Languages/StringResUtil_en-us.xaml", UriKind.Relative);
                            break;
                    }
                    break;
            }

            if (dict.Source == null) { throw new Exception("Language source missing"); }
            
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
        public static List<string> SupportedLanguages { get; set; }
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