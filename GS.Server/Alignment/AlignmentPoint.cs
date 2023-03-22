using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
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
        /// The axis position following slew but before sync (where the mount thinks it is pointing)
        /// </summary>
        public AxisPosition Unsynced { get; set; }

        /// <summary>
        /// The Cartesian equivalent of the Unmapped axis position
        /// </summary>
        public Coord UnsyncedCartesian { get; set; }

        /// <summary>
        /// The unadjusted axis position after sync
        /// </summary>
        public AxisPosition Synced { get; set; }

        /// <summary>
        /// The cartesean version of the mapped (synched) axis positions
        /// </summary>
        public Coord SyncedCartesian { get; set; }

        [JsonIgnore]
        public Coord Delta => (SyncedCartesian - UnsyncedCartesian);

        [JsonIgnore]
        public double OffsetDistance => Math.Sqrt(((SyncedCartesian.x - UnsyncedCartesian.x) * (SyncedCartesian.x - UnsyncedCartesian.x)) 
            + ((SyncedCartesian.y - UnsyncedCartesian.y) * (SyncedCartesian.y - UnsyncedCartesian.y)));

        [JsonIgnore]
        public string SyncedTime => $"{AlignTime:G}";

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

        public AlignmentPoint(double[] unsynced, double[] synced, DateTime syncTime)
        {
            Unsynced = new AxisPosition(unsynced);
            Synced = new AxisPosition(synced);
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
