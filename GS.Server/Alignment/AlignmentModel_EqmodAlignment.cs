/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published
//    by the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */
// This file contains code that is based on the logi from the EQMOD Alignment code.
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
            _oneStarAdjustment = new CartesCoord(data.Delta);

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
