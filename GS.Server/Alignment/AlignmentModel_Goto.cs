/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GS.Server.SkyTelescope;

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
            CartesCoord sXy = EQ_sp2Cs(synced);
            MapResult mapResult;

            if (!_threeStarEnabled)
            {
                SelectedAlignmentPoint = null;
                mapResult = Delta_Map(sXy);
            }
            else
            {
                switch (this.AlignmentBehaviour)
                {
                    case AlignmentBehaviourEnum.Nearest:
                        mapResult = DeltaSync_Matrix_Map(sXy);
                        break;
                    default:
                        mapResult = Delta_Matrix_Reverse_Map(sXy);
                        if (!mapResult.InTriangle)
                        {
                            mapResult = DeltaSync_Matrix_Map(sXy);
                        }
                        break;
                }
            }

            
            // Current telescope position
            CurrentPoint.Clear();
            CurrentPoint.Add(sXy);

            AxisPosition result = EQ_cs2Sp(mapResult.Position, sXy);
            return result;
        }

        /// <summary>
        /// Returns the synced axis position for a given unsynced axis position.
        /// Used for goto.
        /// </summary>
        /// <param name="target">The steps for the target</param>
        /// <returns></returns>
        public AxisPosition GetSyncedValue(AxisPosition unsynced)
        {
            CartesCoord uXy = EQ_sp2Cs(unsynced);
            MapResult mapResult;
            if (!_threeStarEnabled)
            {
                mapResult = DeltaReverse_Map(uXy);

            }
            else
            {
                switch (this.AlignmentBehaviour)
                {
                    case AlignmentBehaviourEnum.Nearest:
                        mapResult = DeltaSyncReverse_Matrix_Map(uXy);
                        break;
                    default:
                        mapResult = Delta_Matrix_Map(uXy);
                        if (!mapResult.InTriangle)
                        {
                            mapResult = DeltaSyncReverse_Matrix_Map(uXy);
                        }
                        break;
                }
            }

            AxisPosition result = EQ_cs2Sp(mapResult.Position, uXy);
            return result;
        }


    }
}
