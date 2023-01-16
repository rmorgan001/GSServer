/* Copyright(C) 2020  Phil Crompton (phil@lunaticsoftware.org)
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    a double with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
//---------------------------------------------------------------------
// based on original work
// Copyright © 2006 Raymund Sarmiento
//
// Permission is hereby granted to use this Software for any purpose
// including combining with commercial products, creating derivative
// works, and redistribution of source or binary code, without
// limitation or consideration. Any redistributed copies of this
// Software must include the above Copyright Notice.
//
// THIS SOFTWARE IS PROVIDED "AS IS". THE AUTHOR OF THIS CODE MAKES NO
// WARRANTIES REGARDING THIS SOFTWARE, EXPRESS OR IMPLIED, AS TO ITS
// SUITABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
//---------------------------------------------------------------------
//
// EQMATH.bas - Math functions for EQMOD ASCOM RADECALTAZ computations
//
//
// Written:  07-Oct-06   Raymund Sarmiento
//
// Edits:
//
// When      Who     What
// --------- ---     --------------------------------------------------
// 04-Nov-06 rcs     Initial edit for EQ Mount Driver Function Prototype
// 20-Nov-06 rcs     wrote a new function for now_lst that will generate millisecond
//                   granularity
// 21-Nov-06 rcs     Append RA GOTO Compensation to minimize discrepancy
// 19-Mar-07 rcs     Initial Edit for Three star alignment
// 05-Apr-07 rcs     Add MAXSYNC
// 08-Apr-07 rcs     N-star implementation
// 13-Jun-20 jpc	 Copied into NPoint code base and converted from vb6 to C#
//---------------------------------------------------------------------
//
//
//  DISCLAIMER:

//  You can use the information on this site COMPLETELY AT YOUR OWN RISK.
//  The modification steps and other information on this site is provided
//  to you "AS IS" and WITHOUT WARRANTY OF ANY KIND, express, statutory,
//  implied or otherwise, including without limitation any warranty of
//  merchantability or fitness for any particular or intended purpose.
//  In no event the author will  be liable for any direct, indirect,
//  punitive, special, incidental or consequential damages or loss of any
//  kind whether or not the author  has been advised of the possibility
//  of such loss.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GS.Server.Alignment
{
    partial class AlignmentModel
    {

        private double Delta_RA_Map(double raEncoder)
        {

            return raEncoder - this._oneStarAdjustment.RA; // + gRASync01 (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private double Delta_DEC_Map(double decEncoder)
        {

            return decEncoder - this._oneStarAdjustment.Dec; // + gDECSync01  (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private MapResult Delta_Map(AxisPosition original)
        {
            AlignmentPoint nearestPoint = GetNearest(original);
            if (nearestPoint != null)
            {
                SelectedAlignmentPoint = nearestPoint;
                return new MapResult()
                {
                    Position = new AxisPosition(original.RA - nearestPoint.Delta.RA, original.Dec - nearestPoint.Delta.Dec)
                };
            }
            else
            {
                return new MapResult()
                {
                    Position = new AxisPosition(Delta_RA_Map(original.RA), Delta_DEC_Map(original.Dec))
                };
            }
        }

        private double DeltaReverse_RA_Map(double raTarget)
        {

            return raTarget + this._oneStarAdjustment.RA; // + gRASync01 (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private double DeltaReverse_DEC_Map(double decTarget)
        {

            return decTarget + this._oneStarAdjustment.Dec; // + gDECSync01  (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private MapResult DeltaReverse_Map(AxisPosition original)
        {

            AlignmentPoint nearestPoint = GetNearest(original);
            if (nearestPoint != null)
            {
                SelectedAlignmentPoint = nearestPoint;
                return new MapResult()
                {
                    Position = new AxisPosition(original.RA + nearestPoint.Delta.RA, original.Dec + nearestPoint.Delta.Dec)
                };
            }
            else
            {
                return new MapResult()
                {
                    Position = new AxisPosition(DeltaReverse_RA_Map(original.RA), DeltaReverse_DEC_Map(original.Dec))
                };
            }
        }


        private MapResult Delta_Matrix_Map(AxisPosition pos)
        {
            MapResult result = new MapResult();

            // re transform based on the nearest 3 stars
            bool inTriangle = EQ_UpdateTaki(pos);

            AxisPosition obtmp2 = EQ_plTaki(pos);
            result.Position = new AxisPosition(obtmp2.RA, obtmp2.Dec);
            result.InTriangle = inTriangle;

            return result;
        }


        private MapResult Delta_Matrix_Reverse_Map(AxisPosition pos)
        {

            MapResult result = new MapResult();


            // re transform using the 3 nearest stars
            bool inTriangle = EQ_UpdateAffine(pos);
            AxisPosition obtmp2 = EQ_plAffine(pos);

            result.Position = new AxisPosition(obtmp2.RA, obtmp2.Dec);
            result.InTriangle = inTriangle;


            return result;
        }

        /// <summary>
        /// Maps an encoder position to the calculate target position
        /// </summary>
        /// <param name="encoderPosition"></param>
        /// <returns></returns>
        private MapResult DeltaSync_Matrix_Map(AxisPosition encoderPosition)
        {
            MapResult result = new MapResult();

            this.SelectedAlignmentPoint = GetNearest(encoderPosition);
            if (this.SelectedAlignmentPoint != null)
            {
                result.Position = new AxisPosition(
                encoderPosition.RA + this.SelectedAlignmentPoint.Delta.RA,       // + gRASync01;
                encoderPosition.Dec + this.SelectedAlignmentPoint.Delta.Dec
                );    // + gDecSync01;
                      // result.z = 1;
                      // result.f = 0;
            }
            else
            {
                result.Position = encoderPosition;
                // result.z = 0;
                // result.f = 0;
            }
            return result;
        }

        /// <summary>
        /// Reverse map from an aligned target position to the encoder position
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private MapResult DeltaSyncReverse_Matrix_Map(AxisPosition targetPosition)
        {
            MapResult result = new MapResult();

            if (this.AlignmentPoints.Count == 0)
            {
                result.Position = targetPosition;
            }
            else
            {
                this.SelectedAlignmentPoint = GetNearest(targetPosition);
                if (this.SelectedAlignmentPoint != null)
                {
                    result.Position = new AxisPosition(
                        targetPosition.RA - this.SelectedAlignmentPoint.Delta.RA,       // + gRASync01;
                        targetPosition.Dec - this.SelectedAlignmentPoint.Delta.Dec
                        );
                }
                else
                {
                    result.Position = targetPosition;
                }
            }
            return result;
        }

        //TODO: Improve GetQuadrant to return an Enum value (NW, NE, SW or SE) instead of an int.

        /// <summary>
        /// Returns a quadrant based on a cartesean coordinate
        /// </summary>
        /// <param name="tmpcoord"></param>
        /// <returns></returns>
        private int GetQuadrant(Coord tmpcoord)
        {
            int ret;

            if (tmpcoord.x >= 0)
            {
                if (tmpcoord.y >= 0)
                {
                    ret = 0;
                }
                else
                {
                    ret = 1;
                }
            }
            else
            {
                if (tmpcoord.y >= 0)
                {
                    ret = 2;
                }
                else
                {
                    ret = 3;
                }
            }

            return ret;

        }


        /// <summary>
        /// Return the nearest alignment point to an encoder position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private AlignmentPoint GetNearest(AxisPosition pos)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();

            Coord posCartesean = EQ_sp2Cs(pos);

            foreach (AlignmentPoint pt in this.AlignmentPoints)
            {
                pt.SelectedForGoto = false;
                switch (ActivePoints)
                {
                    case ActivePointsEnum.All:
                        // all points 
                        break;
                    case ActivePointsEnum.PierSide:
                        // only consider points on this side of the meridian 
                        if (pt.UnsyncedCartesian.y * posCartesean.y < 0)
                        {
                            continue;
                        }
                        break;
                    case ActivePointsEnum.LocalQuadrant:
                        // local quadrant 
                        if (GetQuadrant(posCartesean) != GetQuadrant(pt.UnsyncedCartesian))
                        {
                            continue;
                        }
                        break;
                }

                if (CheckLocalPier)
                {
                    // calculate polar distance
                    distances.Add(pt.Id, Math.Pow(pt.Unsynced.RA - pos.RA, 2) + Math.Pow(pt.Unsynced.Dec - pos.Dec, 2));
                }
                else
                {
                    // calculate cartesian disatnce
                    distances.Add(pt.Id, Math.Pow(pt.UnsyncedCartesian.x - posCartesean.x, 2) + Math.Pow(pt.UnsyncedCartesian.y - posCartesean.y, 2));
                }
            }

            if (distances.Count == 0)
            {
                return null;
            }
            else
            {
                int nearestId = distances.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).First();
                AlignmentPoint result = AlignmentPoints.First(pt => pt.Id == nearestId);
                result.SelectedForGoto = true;
                return result;
            }

        }


    }
}


