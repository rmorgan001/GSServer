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
using XInputDotNetPure;

namespace GS.Server.Alignment
{
    partial class AlignmentModel
    {

        private double Delta_RA_Map(double raEncoder)
        {

            return raEncoder - this._oneStarAdjustment.x; // + gRASync01 (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private double Delta_DEC_Map(double decEncoder)
        {

            return decEncoder - this._oneStarAdjustment.y; // + gDECSync01  (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private void UpdateChartingPoints(AlignmentPoint point)
        {
            // Update charting lists
            if (_CurrentNearestPointId != point.Id)
            {
                ChartNearestPoint.Clear();
                ChartNearestPoint.Add(new CartesCoord() { x = point.SyncedCartesian.x, y = point.SyncedCartesian.y });
                _CurrentNearestPointId = point.Id;
            }
            ChartTrianglePoints.Clear();
        }
        private void ClearChartNearestPoint()
        {
            // Update charting lists
            ChartNearestPoint.Clear();
            _CurrentNearestPointId = null;
        }

        private MapResult Delta_Map(CartesCoord original)
        {
            AlignmentPoint nearestPoint = GetNearest(original);
            if (nearestPoint != null)
            {

                SelectedAlignmentPoint = nearestPoint;
                UpdateChartingPoints(nearestPoint);
                return new MapResult()
                {
                    Position = new CartesCoord(original.x - nearestPoint.Delta.x, original.y - nearestPoint.Delta.y)
                };
            }
            else
            {
                ClearChartNearestPoint();
                return new MapResult()
                {
                    Position = new CartesCoord(Delta_RA_Map(original.x), Delta_DEC_Map(original.y))
                };
            }
        }

        private double DeltaReverse_RA_Map(double raTarget)
        {

            return raTarget + this._oneStarAdjustment.x; // + gRASync01 (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private double DeltaReverse_DEC_Map(double decTarget)
        {

            return decTarget + this._oneStarAdjustment.y; // + gDECSync01  (Eqmod has an ASCOM Sync mode which would set this value)

        }

        private MapResult DeltaReverse_Map(CartesCoord original)
        {

            AlignmentPoint nearestPoint = GetNearest(original);
            if (nearestPoint != null)
            {
                SelectedAlignmentPoint = nearestPoint;
                UpdateChartingPoints(nearestPoint);
                return new MapResult()
                {
                    Position = new CartesCoord(original.x + nearestPoint.Delta.x, original.y + nearestPoint.Delta.y)
                };
            }
            else
            {
                return new MapResult()
                {
                    Position = new CartesCoord(DeltaReverse_RA_Map(original.x), DeltaReverse_DEC_Map(original.y))
                };
            }
        }


        private MapResult Delta_Matrix_Map(CartesCoord pos)
        {
            MapResult result = new MapResult();

            // re transform based on the nearest 3 stars
            bool inTriangle = EQ_UpdateTaki(pos);

            CartesCoord transformed = EQ_plTaki(pos);
            result.Position = transformed;
            result.InTriangle = inTriangle;

            return result;
        }


        private MapResult Delta_Matrix_Reverse_Map(CartesCoord pos)
        {

            MapResult result = new MapResult();


            // re transform using the 3 nearest stars
            bool inTriangle = EQ_UpdateAffine(pos);
            CartesCoord transformed = EQ_plAffine(pos);

            result.Position = transformed;
            result.InTriangle = inTriangle;


            return result;
        }

        /// <summary>
        /// Maps an encoder position to the calculate target position
        /// </summary>
        /// <param name="encoderPosition"></param>
        /// <returns></returns>
        private MapResult DeltaSync_Matrix_Map(CartesCoord encoderPosition)
        {
            MapResult result = new MapResult();

            this.SelectedAlignmentPoint = GetNearest(encoderPosition);
            if (this.SelectedAlignmentPoint != null)
            {
                UpdateChartingPoints(this.SelectedAlignmentPoint);
                result.Position = new CartesCoord(
                encoderPosition.x + this.SelectedAlignmentPoint.Delta.x,       // + gRASync01;
                encoderPosition.y + this.SelectedAlignmentPoint.Delta.y
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
        private MapResult DeltaSyncReverse_Matrix_Map(CartesCoord targetPosition)
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
                    result.Position = new CartesCoord(
                        targetPosition.x - this.SelectedAlignmentPoint.Delta.x,       // + gRASync01;
                        targetPosition.y - this.SelectedAlignmentPoint.Delta.y
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
        private int GetQuadrant(CartesCoord tmpcoord)
        {
            int ret;

            if (tmpcoord.x >= 0)
            {
                ret = tmpcoord.y >= 0 ? 0 : 1;
            }
            else
            {
                ret = tmpcoord.y >= 0 ? 2 : 3;
            }

            return ret;

        }

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
                ret = tmpcoord.y >= 0 ? 0 : 1;
            }
            else
            {
                ret = tmpcoord.y >= 0 ? 2 : 3;
            }

            return ret;

        }

        /// <summary>
        /// Return the nearest alignment point to an encoder position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private AlignmentPoint GetNearest(CartesCoord pos)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();

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
                        if (pt.UnsyncedCartesian.y * pos.y < 0)
                        {
                            continue;
                        }
                        break;
                    case ActivePointsEnum.LocalQuadrant:
                        // local quadrant 
                        if (GetQuadrant(pos) != GetQuadrant(pt.UnsyncedCartesian))
                        {
                            continue;
                        }
                        break;
                }

                // calculate cartesian distance
                distances.Add(pt.Id, Math.Pow(pt.UnsyncedCartesian.x - pos.x, 2) + Math.Pow(pt.UnsyncedCartesian.y - pos.y, 2));
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


