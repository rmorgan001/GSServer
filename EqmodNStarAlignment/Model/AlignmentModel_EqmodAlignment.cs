using EqmodNStarAlignment.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    partial class AlignmentModel
    {
        public bool EQ_NPointAppend(AlignmentPoint data)
        {
            bool eq_NPointAppend = true;
            _oneStarAdjustment = data.Delta;

            if (AlignmentPoints.Count < 3)
            {
                // Less than three alignment points so just add the incoming one.
                AlignmentPoints.Add(data);
            }
            else
            {
                if (AlignmentPoints.Count == 3)
                {
                    AlignmentPoints.Add(data);
                    // Update the matrices
                    SendToMatrix();
                }
                else
                {
                    // Now have more than 3 so see if this point is a replacement
                    var nearPoints = this.AlignmentPoints.Where(ap => Math.Abs(ap.Encoder.RA - data.Encoder.RA) < _proximityStepsRa
                                                                || Math.Abs(ap.Encoder.Dec - data.Encoder.Dec) < _proximityStepsDec).ToList();
                    foreach (AlignmentPoint ap in nearPoints)
                    {
                        this.AlignmentPoints.Remove(ap);
                    }

                    // Add the incoming point
                    AlignmentPoints.Add(data);

                    // Update the matrices
                    SendToMatrix();
                }
            }
            return eq_NPointAppend;
        }
        public void SendToMatrix()
        {
            if (AlignmentPoints.Count < 3)
            {
                return;
            }
            foreach (AlignmentPoint pt in AlignmentPoints)
            {
                pt.EncoderCartesian = EQ_sp2Cs(pt.Encoder);
                pt.TargetCartesian = EQ_sp2Cs(pt.Target);

            }

            ActivateMatrix();
        }

        private void ActivateMatrix()
        {
            _threeStarEnabled = false;
            if (AlignmentPoints.Count >= 3)
            {

                _ = EQ_AssembleMatrix_Taki(0, 0,
                    AlignmentPoints[0].TargetCartesian, AlignmentPoints[1].TargetCartesian, AlignmentPoints[2].TargetCartesian,
                    AlignmentPoints[0].EncoderCartesian, AlignmentPoints[1].EncoderCartesian, AlignmentPoints[2].EncoderCartesian);
                _ = EQ_AssembleMatrix_Affine(0, 0,
                    AlignmentPoints[0].EncoderCartesian, AlignmentPoints[1].EncoderCartesian, AlignmentPoints[2].EncoderCartesian,
                    AlignmentPoints[0].TargetCartesian, AlignmentPoints[1].TargetCartesian, AlignmentPoints[2].TargetCartesian);
                _threeStarEnabled = true;
            }
        }
        // /pec /showalignment
    }
}
