/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GS.Utilities.Helpers
{
    public static class Settings
    {
        #region Events

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        /// <summary>
        /// Store for selected color
        /// </summary>
        private static string _thirdColor;
        public static string ThirdColor
        {
            get => _thirdColor;
            set
            {
                if (_thirdColor == value) return;
                _thirdColor = value;
                // Properties.LogView.Default.ThirdColor = value;
                OnStaticPropertyChanged();

            }
        }

        #region Methods

        public static void Load()
        {
            Upgrade();
            //ThirdColor = Properties.LogView.Default.ThirdColor;

        }

        private static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.Utilities.Default.Version;
            if (version == assembly.ToString()) return;
            Properties.Utilities.Default.Upgrade();
            Properties.Utilities.Default.Version = assembly.ToString();
            Save();
        }

        public static void Save()
        {
            Properties.Utilities.Default.Save();
            Properties.Utilities.Default.Reload();
        }

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
