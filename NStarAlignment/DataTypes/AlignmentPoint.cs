using System;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NStarAlignment.DataTypes
{
    public enum PierSide
    {
        EastLookingWest,
        WestLookingEast,
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
