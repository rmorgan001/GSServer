using EqmodNStarAlignment.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    public partial class AlignmentModel
    {

        /// <summary>
        /// Returns the encoder steps for a given target.
        /// Based on code from EQMOD's EncoderTimer.Timer event.
        /// Used when reporting current position
        /// </summary>
        /// <param name="encoderSteps"></param>
        /// <returns></returns>
        public EncoderPosition GetEncoderSteps(EncoderPosition encoder)
        {
            MapResult result;
            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                result = Delta_Map(encoder);
            }
            else
            {
                switch (this.AlignmentMode)
                {
                    case AlignmentModeEnum.Nearest:
                        result = DeltaSync_Matrix_Map(encoder);
                        break;
                    default:
                        result = Delta_Matrix_Reverse_Map(encoder);
                        if (!result.InTriangle)
                        {
                            result = DeltaSync_Matrix_Map(encoder);
                        }
                        break;
                }
            }
            return result.EncoderPosition;
        }

        /// <summary>
        /// Returns the target steps to go to to an aligned target.
        /// Used for goto.
        /// </summary>
        /// <param name="target">The steps for the target</param>
        /// <returns></returns>
        public EncoderPosition GetTargetSteps(EncoderPosition target)
        {
            MapResult result;
            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                result = DeltaReverse_Map(target);

            }
            {
                switch (this.AlignmentMode)
                {
                    case AlignmentModeEnum.Nearest:
                        result = DeltaSyncReverse_Matrix_Map(target);
                        break;
                    default:
                        result = Delta_Matrix_Map(target);
                        if (!result.InTriangle)
                        {
                            result = DeltaSyncReverse_Matrix_Map(target);
                        }
                        break;
                }
            }
            return result.EncoderPosition;
        }


    }
}
