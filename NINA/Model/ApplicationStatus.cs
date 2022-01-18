﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GS.Utilities.Helpers;
using NINA.Utility;

namespace NINA.Model
{

    public class ApplicationStatus : ObservableObject
    {
        private string _source;

        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                _source = value;
                OnPropertyChanged();
            }
        }

        private string _status;

        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        private double _progress = -1;

        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private int _maxProgress = 1;

        public int MaxProgress
        {
            get
            {
                return _maxProgress;
            }
            set
            {
                _maxProgress = value;
                OnPropertyChanged();
            }
        }

        private StatusProgressType _progressType = StatusProgressType.Percent;

        public StatusProgressType ProgressType
        {
            get
            {
                return _progressType;
            }
            set
            {
                _progressType = value;
                OnPropertyChanged();
            }
        }

        private string _status2;

        public string Status2
        {
            get
            {
                return _status2;
            }
            set
            {
                _status2 = value;
                OnPropertyChanged();
            }
        }

        private double _progress2 = -1;

        public double Progress2
        {
            get
            {
                return _progress2;
            }
            set
            {
                _progress2 = value;
                OnPropertyChanged();
            }
        }

        private int _maxProgress2 = 1;

        public int MaxProgress2
        {
            get
            {
                return _maxProgress2;
            }
            set
            {
                _maxProgress2 = value;
                OnPropertyChanged();
            }
        }

        private StatusProgressType _progressType2 = StatusProgressType.Percent;

        public StatusProgressType ProgressType2
        {
            get
            {
                return _progressType2;
            }
            set
            {
                _progressType2 = value;
                OnPropertyChanged();
            }
        }

        private string _status3;

        public string Status3
        {
            get
            {
                return _status3;
            }
            set
            {
                _status3 = value;
                OnPropertyChanged();
            }
        }

        private double _progress3 = -1;

        public double Progress3
        {
            get
            {
                return _progress3;
            }
            set
            {
                _progress3 = value;
                OnPropertyChanged();
            }
        }

        private int _maxProgress3 = 1;

        public int MaxProgress3
        {
            get
            {
                return _maxProgress3;
            }
            set
            {
                _maxProgress3 = value;
                OnPropertyChanged();
            }
        }

        private StatusProgressType _progressType3 = StatusProgressType.Percent;

        public StatusProgressType ProgressType3
        {
            get
            {
                return _progressType3;
            }
            set
            {
                _progressType3 = value;
                OnPropertyChanged();
            }
        }

        public enum StatusProgressType
        {
            Percent,
            ValueOfMaxValue
        }
    }
}