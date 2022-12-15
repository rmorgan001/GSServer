using EqmodNStarAlignment.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    public class AlignmentPoint : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique ID for alignment point
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Alignment date and time of the sync
        /// </summary>
        public DateTime AlignTime { get; set; }

        /// <summary>
        /// The Encoder positions at sync time
        /// </summary>
        public EncoderPosition Encoder { get; set; }

        /// <summary>
        /// The Cartesian equivalent of the EncoderPosition
        /// </summary>
        public Coord EncoderCartesian { get; set; }


        /// <summary>
        /// The Target Ra/Dec in hours/degrees
        /// </summary>
        public AxisPosition OrigRaDec { get; set; }


        /// <summary>
        /// The unadjusted encoder positions
        /// </summary>
        public EncoderPosition Target { get; set; }

        /// <summary>
        /// The cartesean version of the unadjusted encoder positions
        /// </summary>
        public Coord TargetCartesian { get; set; }

        [JsonIgnore]
        public EncoderPosition Delta => (Target - Encoder);


        [JsonIgnore]
        public string Synched => $"{AlignTime:G}";

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


        public AlignmentPoint()
        {

        }

        public AlignmentPoint(long[] encoder, double[] origRaDec, long[] target, DateTime syncTime)
        {
            Encoder = new EncoderPosition(encoder);
            OrigRaDec = new AxisPosition(origRaDec);
            Target = new EncoderPosition(target);
            AlignTime = syncTime;
        }

        #region INotifyPropertyChanged interface ...
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

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


}
