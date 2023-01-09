﻿// This file contains code that is based on the logi from the EQMOD Alignment code.
using System;
using System.Linq;

namespace GS.Server.Alignment
{
    partial class AlignmentModel
    {
        public bool EQ_NPointAppend(AlignmentPoint data)
        {
            bool eq_NPointAppend = true;

            // Check for points within proximity distance of the new point and remove them
            if (AlignmentPoints.Count > 0)
            {
                var nearPoints = this.AlignmentPoints
                    .Where(p => p.Unsynced.IncludedAngleTo(data.Unsynced) <= ProximityLimit).ToList();
                foreach (AlignmentPoint ap in nearPoints)
                {
                    this.AlignmentPoints.Remove(ap);
                }

            }
            
            // Add the new point.
            AlignmentPoints.Add(data);

            // Update one star alignment to use the latest data
            _oneStarAdjustment = data.Delta;

            // Check if matrix should be updated.
            if (AlignmentPoints.Count > 2)
            {
                // Update the matrices
                SendToMatrix();
            }

            return eq_NPointAppend;
        }

        public void SendToMatrix()
        {
            if (AlignmentPoints.Count < 3)
            {
                return;
            }
            System.Diagnostics.Debug.WriteLine("");
            foreach (AlignmentPoint pt in AlignmentPoints)
            {
                pt.UnsyncedCartesian = EQ_sp2Cs(pt.Unsynced);
                pt.SyncedCartesian = EQ_sp2Cs(pt.Synced);

                // Testing round trip
                //var tmpobj4 = EQ_SphericalPolar(pt.Unsynced);
                //var tmpobj1 = EQ_Polar2Cartes(tmpobj4);
                //var tmpobj2 = EQ_Cartes2Polar(tmpobj1, tmpobj1);
                //var result = EQ_PolarSpherical(tmpobj2, tmpobj4);
                //System.Diagnostics.Debug.WriteLine($"{pt.Id}: ({pt.Unsynced.RA}, {pt.Unsynced.Dec}) => ({tmpobj4.x}, {tmpobj4.y})  => ({result.RA}, {result.Dec}) ");
                // End of round trip test.
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
