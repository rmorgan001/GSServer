// This file contains code that is based on the logi from the EQMOD Alignment code.
using System;
using System.Linq;

namespace GS.Server.Alignment
{
    partial class AlignmentModel
    {
        public bool EQ_NPointAppend(AlignmentPoint data)
        {
            bool eq_NPointAppend = true;
            _oneStarAdjustment = data.Delta;

            if (AlignmentPoints.Count < 2)
            {
                // Less than three alignment points so just add the incoming one.
                AlignmentPoints.Add(data);
            }
            else
            {
                if (AlignmentPoints.Count == 2)
                {
                    AlignmentPoints.Add(data);
                    // Update the matrices
                    SendToMatrix();
                }
                else
                {
                    // Now have more than 3 so see if this point is a replacement
                    var nearPoints = this.AlignmentPoints.Where(ap => Math.Abs(ap.Unsynced.RA - data.Unsynced.RA) < _proximityLimit
                                                                || Math.Abs(ap.Unsynced.Dec - data.Unsynced.Dec) < _proximityLimit).ToList();
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
                pt.UnsyncedCartesian = EQ_sp2Cs(pt.Unsynced);
                pt.SyncedCartesian = EQ_sp2Cs(pt.Synced);

            }

            ActivateMatrix();
        }

        private void ActivateMatrix()
        {
            _threeStarEnabled = false;
            if (AlignmentPoints.Count >= 3)
            {

                _ = EQ_AssembleMatrix_Taki(0, 0,
                    AlignmentPoints[0].UnsyncedCartesian, AlignmentPoints[1].UnsyncedCartesian, AlignmentPoints[2].UnsyncedCartesian,
                    AlignmentPoints[0].SyncedCartesian, AlignmentPoints[1].SyncedCartesian, AlignmentPoints[2].SyncedCartesian);
                _ = EQ_AssembleMatrix_Affine(0, 0,
                    AlignmentPoints[0].SyncedCartesian, AlignmentPoints[1].SyncedCartesian, AlignmentPoints[2].SyncedCartesian,
                    AlignmentPoints[0].UnsyncedCartesian, AlignmentPoints[1].UnsyncedCartesian, AlignmentPoints[2].UnsyncedCartesian);
                _threeStarEnabled = true;
            }
        }
        // /pec /showalignment
    }
}
