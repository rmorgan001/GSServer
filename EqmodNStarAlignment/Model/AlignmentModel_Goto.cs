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
        /// </summary>
        /// <param name="encoderSteps"></param>
        /// <returns></returns>
        public EncoderPosition GetTargetSteps(EncoderPosition encoder)
        {
            MapResult result;
            EncoderPosition target = encoder;
            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                result = Delta_Map(encoder);
                target = result.EncoderPosition;
            }
            else
            {
                switch (this.ThreePointAlgorithm)
                {
                    case ThreePointAlgorithmEnum.BestCentre:
                        result = DeltaSync_Matrix_Map(encoder);
                        break;
                    case ThreePointAlgorithmEnum.ClosestPoints:
                        result = Delta_Matrix_Reverse_Map(encoder);
                        break;
                    default:
                        result = Delta_Matrix_Reverse_Map(encoder);
                        if (!result.InTriangle)
                        {
                            result = DeltaSync_Matrix_Map(target);
                        }
                        target = result.EncoderPosition;
                        break;
                }
            }
            return target;
        }

        /// <summary>
        /// Returns the target steps for a given encoder position.
        /// </summary>
        /// <param name="targetSteps"></param>
        /// <returns></returns>
        public double[] GetEncoderSteps(double[] targetSteps)
        {
            return targetSteps;
        }


    }
}
