using System;
using System.Collections.Generic;
using System.Text;

namespace GS.Server.Alignment
{
    public partial class AlignmentModel
    {

        /// <summary>
        /// Returns the unsynced axis position for a given synced axis position.
        /// Based on code from EQMOD's EncoderTimer.Timer event.
        /// Used when reporting current position
        /// </summary>
        /// <param name="encoderSteps"></param>
        /// <returns></returns>
        public AxisPosition GetUnsyncedValue(AxisPosition synced)
        {
            MapResult result;
            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                result = Delta_Map(synced);
            }
            else
            {
                switch (this.AlignmentBehaviour)
                {
                    case AlignmentBehaviourEnum.Nearest:
                        result = DeltaSync_Matrix_Map(synced);
                        break;
                    default:
                        result = Delta_Matrix_Reverse_Map(synced);
                        if (!result.InTriangle)
                        {
                            result = DeltaSync_Matrix_Map(synced);
                        }
                        break;
                }
            }
            return result.Position;
        }

        /// <summary>
        /// Returns the synced axis position for a given unsynced axis position.
        /// Used for goto.
        /// </summary>
        /// <param name="target">The steps for the target</param>
        /// <returns></returns>
        public AxisPosition GetSyncedValue(AxisPosition unsynced)
        {
            MapResult result;
            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                result = DeltaReverse_Map(unsynced);

            }
            else
            {
                switch (this.AlignmentBehaviour)
                {
                    case AlignmentBehaviourEnum.Nearest:
                        result = DeltaSyncReverse_Matrix_Map(unsynced);
                        break;
                    default:
                        result = Delta_Matrix_Map(unsynced);
                        if (!result.InTriangle)
                        {
                            result = DeltaSyncReverse_Matrix_Map(unsynced);
                        }
                        break;
                }
            }
            return result.Position;
        }


    }
}
