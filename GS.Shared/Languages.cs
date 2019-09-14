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
using System.Globalization;
using System.Threading;
using System.Windows;

namespace GS.Shared
{
    public class Languages
    {
        public void SetLanguageDictionary()
        {
            SetLanguageDictionary(Thread.CurrentThread.CurrentCulture);
        }

        public void SetLanguageDictionary(CultureInfo cultureInfo)
        {
            SetLanguageDictionary(cultureInfo.ToString());
        }

        public void SetLanguageDictionary(string cultureInfo)
        {
            var dict = new ResourceDictionary();
            switch (cultureInfo)
            {
                case "en-US":
                    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.xaml", UriKind.Relative);
                    break;
                //case "fr-CA":
                //    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.fr-CA.xaml", UriKind.Relative);
                //    break;
                default:
                    dict.Source = new Uri("GS.Shared;component/Languages/StringResources.xaml", UriKind.Relative);
                    break;
            }
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }


    }
} 