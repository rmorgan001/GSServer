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
            SendToMatrix();

            return eq_NPointAppend;
        }

        public void SendToMatrix()
        {
            // Matrices are assembled for the selected triangle at mapping time.
            // A degenerate initial triple must not prevent later points being used.
            _threeStarEnabled = AlignmentPoints.Count >= 3;
        }
        // /pec /showalignment
    }
}
