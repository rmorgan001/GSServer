/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

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
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NStarAlignment.Utilities;

namespace NStarAlignment.DataTypes
{
    [TypeConverter(typeof(EnumTypeConverter))]
    public enum PierSide
    {
        [Description("East")]
        EastLookingWest,
        [Description("West")]
        WestLookingEast,
        [Description("Unknown")]
        Unknown
    }


    public class AlignmentPointCollection : ObservableCollection<AlignmentPoint>
    {
        protected override void InsertItem(int index, AlignmentPoint item)
        {
            if (item != null && item.Id == 0)
            {
                if (Items.Any())
                {
                    item.Id = Items.Max(i => i.Id) + 1;
                }
                else
                {
                    item.Id = 1;
                }
            }
            base.InsertItem(index, item);
        }


    }

    public class AlignmentPoint:INotifyPropertyChanged
    {
        public int Id { get; set; }

        /// <summary>
        /// Altitude and azimuth stored for UI purposes.
        /// </summary>
        public double[] AltAz { get; set; }

        public AxisPosition MountAxes { get; set; }
        public AxisPosition ObservedAxes { get; set; }

        public CarteseanCoordinate ObservedXy { get; set; }

        public CarteseanCoordinate MountXy { get; set; }

        public PierSide PierSide { get; set; }

        private bool _selected;

        /// <summary>
        /// Selected for correcting display
        /// </summary>
        [JsonIgnore]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (value == _selected) return;
                _selected = value;
                OnPropertyChanged();
            }
        }

        private bool _selectedForGoto;

        /// <summary>
        /// Selected for slew/goto calculation
        /// </summary>
        [JsonIgnore]
        public bool SelectedForGoto
        {
            get => _selectedForGoto;
            set
            {
                if (value == _selectedForGoto) return;
                _selectedForGoto = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string Correction => $"{(ObservedAxes[0] - MountAxes[0]):F3}/{(ObservedAxes[1] - MountAxes[1]):F3}";

        [JsonIgnore]
        public string Synched => $"{SyncTime:G}";

        /// <summary>
        /// Local time of synchronization (UTC)
        /// </summary>
        [JsonProperty]
        public DateTime SyncTime { get; private set; }


        private AlignmentPoint()
        {
        }

        public AlignmentPoint(double[] observedAltAz, AxisPosition mountAxes, AxisPosition observedAxes, CarteseanCoordinate mountXy, CarteseanCoordinate observedXy, PierSide pierSide, DateTime syncTime)
        :this()
        {
            AltAz = observedAltAz;
            MountAxes = mountAxes;
            ObservedAxes = observedAxes;
            MountXy = mountXy;
            ObservedXy = observedXy;
            PierSide = pierSide;
            SyncTime = syncTime;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
