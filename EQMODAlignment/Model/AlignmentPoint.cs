using EqmodNStarAlignment.DataTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    public class AlignmentPoint
    {
        /// <summary>
        /// Unique ID for alignment point
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Alignment date and time of the sync
        /// </summary>
        public DateTime AlignTime { get;set;}

        /// <summary>
        /// The Encoder positions at sync time
        /// </summary>
        public EncoderPosition Encoder { get;set;}

        /// <summary>
        /// The Cartesian equivalent of the EncoderPosition
        /// </summary>
        public Coord EncoderCartesian { get; set;}


        /// <summary>
        /// The Target Ra/Dec in hours/degrees
        /// </summary>
        public AxisPosition OrigRaDec { get;set;}


        /// <summary>
        /// The unadjusted encoder positions
        /// </summary>
        public EncoderPosition Target { get;set;}

        /// <summary>
        /// The cartesean version of the unadjusted encoder positions
        /// </summary>
        [JsonIgnore]
        public Coord TargetCartesian { get; set;}

        public EncoderPosition Delta => (Target - Encoder);

        public AlignmentPoint(long[] encoder, double[] origRaDec, long[] target, DateTime syncTime)
        {
            Encoder = new EncoderPosition(encoder);
            OrigRaDec = new AxisPosition(origRaDec);
            Target = new EncoderPosition(target);
            
        }

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
