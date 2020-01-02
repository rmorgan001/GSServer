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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GS.LogView
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
                Properties.LogView.Default.ThirdColor = value;
                OnStaticPropertyChanged();

            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        private static string _fourthColor;
        public static string FourthColor
        {
            get => _fourthColor;
            set
            {
                if (_fourthColor == value) return;
                _fourthColor = value;
                Properties.LogView.Default.FourthColor = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        private static string _raColor;
        public static string RaColor
        {
            get => _raColor;
            set
            {
                if (_raColor == value) return;
                _raColor = value;
                Properties.LogView.Default.RaColor = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        private static string _decColor;
        public static string DecColor
        {
            get => _decColor;
            set
            {
                if (_decColor == value) return;
                _decColor = value;
                Properties.LogView.Default.DecColor = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _raLine;
        public static bool RaLine
        {
            get => _raLine;
            set
            {
                if (_raLine == value) return;
                _raLine = value;
                Properties.LogView.Default.RaLine = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _raBar;
        public static bool RaBar
        {
            get => _raBar;
            set
            {
                if (_raBar == value) return;
                _raBar = value;
                Properties.LogView.Default.RaBar = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _raStep;
        public static bool RaStep
        {
            get => _raStep;
            set
            {
                if (_raStep == value) return;
                _raStep = value;
                Properties.LogView.Default.RaStep = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _decLine;
        public static bool DecLine
        {
            get => _decLine;
            set
            {
                if (_decLine == value) return;
                _decLine = value;
                Properties.LogView.Default.DecLine = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _decBar;
        public static bool DecBar
        {
            get => _decBar;
            set
            {
                if (_decBar == value) return;
                _decBar = value;
                Properties.LogView.Default.DecBar = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _decStep;
        public static bool DecStep
        {
            get => _decStep;
            set
            {
                if (_decStep == value) return;
                _decStep = value;
                Properties.LogView.Default.DecStep = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _thirdLine;
        public static bool ThirdLine
        {
            get => _thirdLine;
            set
            {
                if (_thirdLine == value) return;
                _thirdLine = value;
                Properties.LogView.Default.ThirdLine = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _thirdBar;
        public static bool ThirdBar
        {
            get => _thirdBar;
            set
            {
                if (_thirdBar == value) return;
                _thirdBar = value;
                Properties.LogView.Default.ThirdBar = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _thirdStep;
        public static bool ThirdStep
        {
            get => _thirdStep;
            set
            {
                if (_thirdStep == value) return;
                _thirdStep = value;
                Properties.LogView.Default.ThirdStep = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _fourthLine;
        public static bool FourthLine
        {
            get => _fourthLine;
            set
            {
                if (_fourthLine == value) return;
                _fourthLine = value;
                Properties.LogView.Default.FourthLine = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _fourthBar;
        public static bool FourthBar
        {
            get => _fourthBar;
            set
            {
                if (_fourthBar == value) return;
                _fourthBar = value;
                Properties.LogView.Default.FourthBar = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        private static bool _fourthStep;
        public static bool FourthStep
        {
            get => _fourthStep;
            set
            {
                if (_fourthStep == value) return;
                _fourthStep = value;
                Properties.LogView.Default.FourthStep = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Chart Animations check
        /// </summary>
        private static bool _disableAnimations;
        public static bool DisableAnimations
        {
            get => _disableAnimations;
            set
            {
                if (_disableAnimations == value) return;
                _disableAnimations = value;
                Properties.LogView.Default.DisableAnimations = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _animationTime;
        public static int AnimationTime
        {
            get => _animationTime;
            set
            {
                if (_animationTime == value) return;
                _animationTime = value;
                Properties.LogView.Default.AnimationTime = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _lineSmoothness;
        public static int LineSmoothness
        {
            get => _lineSmoothness;
            set
            {
                if (_lineSmoothness == value) return;
                _lineSmoothness = value;
                Properties.LogView.Default.LineSmoothness = value;
                OnStaticPropertyChanged();
            }
        }

        private static double _lineSize;
        public static double LineSize
        {
            get => _lineSize;
            set
            {
                if (Math.Abs(_lineSize - value) <= 0) return;
                _lineSize = value;
                Properties.LogView.Default.LineSize = value;
                OnStaticPropertyChanged();
            }
        }

        private static int _pointSize;
        public static int PointSize
        {
            get => Properties.LogView.Default.PointSize;
            set
            {
                if (_pointSize == value) return;
                _pointSize = value;
                Properties.LogView.Default.PointSize = value;
                OnStaticPropertyChanged();
            }
        }

        #region Methods

        public static void Load()
        {
            Upgrade();

            ThirdColor = Properties.LogView.Default.ThirdColor;
            FourthColor = Properties.LogView.Default.FourthColor;
            RaColor = Properties.LogView.Default.RaColor;
            DecColor = Properties.LogView.Default.DecColor;
            RaLine = Properties.LogView.Default.RaLine;
            RaBar = Properties.LogView.Default.RaBar;
            RaStep = Properties.LogView.Default.RaStep;
            DecLine = Properties.LogView.Default.DecLine;
            DecBar = Properties.LogView.Default.DecBar;
            DecStep = Properties.LogView.Default.DecStep;
            ThirdLine = Properties.LogView.Default.ThirdLine;
            ThirdBar = Properties.LogView.Default.ThirdBar;
            ThirdStep = Properties.LogView.Default.ThirdStep;
            FourthLine = Properties.LogView.Default.FourthLine;
            FourthBar = Properties.LogView.Default.FourthBar;
            FourthStep = Properties.LogView.Default.FourthStep;
            DisableAnimations = Properties.LogView.Default.DisableAnimations;
            AnimationTime = Properties.LogView.Default.AnimationTime;
            LineSmoothness = Properties.LogView.Default.LineSmoothness;
            LineSize = Properties.LogView.Default.LineSize;
            PointSize = Properties.LogView.Default.PointSize;

        }

        private static void Upgrade()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName().Version;
            var version = Properties.LogView.Default.Version;
            if (version == assembly.ToString()) return;
            Properties.LogView.Default.Upgrade();
            Properties.LogView.Default.Version = assembly.ToString();
            Save();
        }

        public static void Save()
        {
            Properties.LogView.Default.Save();
            Properties.LogView.Default.Reload();
        }

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
