﻿/* Copyright(C) 2020  Phil Crompton (phil@lunaticsoftware.org)
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
//---------------------------------------------------------------------
// based on original work
// Copyright � 2006 Raymund Sarmiento
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
// EQMODVECTOR.BAS - Matrix Transformation Routines for 3-Star Alignment
// (Renamed EQTakiAffine.cs)
// Written:  10-Dec-06   Raymund Sarmiento
//
// Edits:
//
// When      Who     What
// --------- ---     --------------------------------------------------
// 10-Dec-06 rcs     Initial edit for EQ Mount 3-Star Matrix Transformation
// 14-Dec-06 rcs     Added Taki Method on top of Affine Mapping Method for Comparison
//                   Taki Routines based on John Archbold's Excel computation
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

//  WARNING:

//  Circuit modifications implemented on your setup could invalidate
//  any warranty that you may have with your product. Use this
//  information at your own risk. The modifications involve direct
//  access to the stepper motor controls of your mount. Any "mis-control"
//  or "mis-command"  / "invalid parameter" or "garbage" data sent to the
//  mount could accidentally activate the stepper motors and allow it to
//  rotate "freely" damaging any equipment connected to your mount.
//  It is also possible that any garbage or invalid data sent to the mount
//  could cause its firmware to generate mis-steps pulse sequences to the
//  motors causing it to overheat. Make sure that you perform the
//  modifications and testing while there is no physical "load" or
//  dangling wires on your mount. Be sure to disconnect the power once
//  this event happens or if you notice any unusual sound coming from
//  the motor assembly.
//

using EqmodNStarAlignment.DataTypes;
using EqmodNStarAlignment.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    partial class AlignmentModel
    {




        //Define Affine Matrix

        public static Matrix EQMP = Matrix.CreateInstance();
        public static Matrix EQMQ = Matrix.CreateInstance();

        public static Matrix EQMI = Matrix.CreateInstance();
        public static Matrix EQMM = Matrix.CreateInstance();
        public static Coord EQCO = new Coord();


        //Define Taki Matrix

        public static Matrix EQLMN1 = Matrix.CreateInstance();
        public static Matrix EQLMN2 = Matrix.CreateInstance();

        public static Matrix EQMI_T = Matrix.CreateInstance();
        public static Matrix EQMT = Matrix.CreateInstance();
        public static Coord EQCT = new Coord();



        //Function to put coordinate values into a LMN/lmn matrix array

        private Matrix GETLMN(Coord p1, Coord p2, Coord p3)
        {

            Matrix temp = Matrix.CreateInstance();
            Matrix UnitVect = Matrix.CreateInstance();


            temp.Element[0, 0] = p2.x - p1.x;
            temp.Element[1, 0] = p3.x - p1.x;

            temp.Element[0, 1] = p2.y - p1.y;
            temp.Element[1, 1] = p3.y - p1.y;

            temp.Element[0, 2] = p2.z - p1.z;
            temp.Element[1, 2] = p3.z - p1.z;




            UnitVect.Element[0, 0] = (temp.Element[0, 1] * temp.Element[1, 2]) - (temp.Element[0, 2] * temp.Element[1, 1]);
            UnitVect.Element[0, 1] = (temp.Element[0, 2] * temp.Element[1, 0]) - (temp.Element[0, 0] * temp.Element[1, 2]);
            UnitVect.Element[0, 2] = (temp.Element[0, 0] * temp.Element[1, 1]) - (temp.Element[0, 1] * temp.Element[1, 0]);
            UnitVect.Element[1, 0] = Math.Pow(UnitVect.Element[0, 0], 2) + Math.Pow(UnitVect.Element[0, 1], 2) + Math.Pow(UnitVect.Element[0, 2], 2);
            UnitVect.Element[1, 1] = Math.Sqrt(UnitVect.Element[1, 0]);
            if (UnitVect.Element[1, 1] != 0)
            {
                UnitVect.Element[1, 2] = 1 / UnitVect.Element[1, 1];
            }



            temp.Element[2, 0] = UnitVect.Element[1, 2] * UnitVect.Element[0, 0];
            temp.Element[2, 1] = UnitVect.Element[1, 2] * UnitVect.Element[0, 1];
            temp.Element[2, 2] = UnitVect.Element[1, 2] * UnitVect.Element[0, 2];




            return temp;

        }

        //Function to put coordinate values into a P/Q Affine matrix array

        private Matrix GETPQ(Coord p1, Coord p2, Coord p3)
        {

            Matrix temp = Matrix.CreateInstance();

            temp.Element[0, 0] = p2.x - p1.x;
            temp.Element[1, 0] = p3.x - p1.x;
            temp.Element[0, 1] = p2.y - p1.y;
            temp.Element[1, 1] = p3.y - p1.y;

            return temp;

        }

        // Subroutine to draw the Transformation Matrix (Taki Method)

        private bool EQ_AssembleMatrix_Taki(double x, double Y, Coord a1, Coord a2, Coord a3, Coord m1, Coord m2, Coord m3)
        {


            double Det = 0;


            // Get the LMN Matrix

            EQLMN1 = GETLMN(a1, a2, a3);

            // Get the lmn Matrix

            EQLMN2 = GETLMN(m1, m2, m3);




            // Get the Determinant

            Det = EQLMN1.Element[0, 0] * ((EQLMN1.Element[1, 1] * EQLMN1.Element[2, 2]) - (EQLMN1.Element[2, 1] * EQLMN1.Element[1, 2]));
            Det -= (EQLMN1.Element[0, 1] * ((EQLMN1.Element[1, 0] * EQLMN1.Element[2, 2]) - (EQLMN1.Element[2, 0] * EQLMN1.Element[1, 2])));
            Det += (EQLMN1.Element[0, 2] * ((EQLMN1.Element[1, 0] * EQLMN1.Element[2, 1]) - (EQLMN1.Element[2, 0] * EQLMN1.Element[1, 1])));


            // Compute for the Matrix Inverse of EQLMN1

            if (Det == 0)
            {
                throw new System.Exception("999, AssembleMatrix, Cannot invert matrix with Determinant = 0");
            }
            else
            {

                EQMI_T.Element[0, 0] = ((EQLMN1.Element[1, 1] * EQLMN1.Element[2, 2]) - (EQLMN1.Element[2, 1] * EQLMN1.Element[1, 2])) / Det;
                EQMI_T.Element[0, 1] = ((EQLMN1.Element[0, 2] * EQLMN1.Element[2, 1]) - (EQLMN1.Element[0, 1] * EQLMN1.Element[2, 2])) / Det;
                EQMI_T.Element[0, 2] = ((EQLMN1.Element[0, 1] * EQLMN1.Element[1, 2]) - (EQLMN1.Element[1, 1] * EQLMN1.Element[0, 2])) / Det;
                EQMI_T.Element[1, 0] = ((EQLMN1.Element[1, 2] * EQLMN1.Element[2, 0]) - (EQLMN1.Element[2, 2] * EQLMN1.Element[1, 0])) / Det;
                EQMI_T.Element[1, 1] = ((EQLMN1.Element[0, 0] * EQLMN1.Element[2, 2]) - (EQLMN1.Element[2, 0] * EQLMN1.Element[0, 2])) / Det;
                EQMI_T.Element[1, 2] = ((EQLMN1.Element[0, 2] * EQLMN1.Element[1, 0]) - (EQLMN1.Element[1, 2] * EQLMN1.Element[0, 0])) / Det;
                EQMI_T.Element[2, 0] = ((EQLMN1.Element[1, 0] * EQLMN1.Element[2, 1]) - (EQLMN1.Element[2, 0] * EQLMN1.Element[1, 1])) / Det;
                EQMI_T.Element[2, 1] = ((EQLMN1.Element[0, 1] * EQLMN1.Element[2, 0]) - (EQLMN1.Element[2, 1] * EQLMN1.Element[0, 0])) / Det;
                EQMI_T.Element[2, 2] = ((EQLMN1.Element[0, 0] * EQLMN1.Element[1, 1]) - (EQLMN1.Element[1, 0] * EQLMN1.Element[0, 1])) / Det;
            }



            // Get the M Matrix by Multiplying EQMI and EQLMN2
            // EQMI_T - Matrix A
            // EQLMN2 - Matrix B


            EQMT.Element[0, 0] = (EQMI_T.Element[0, 0] * EQLMN2.Element[0, 0]) + (EQMI_T.Element[0, 1] * EQLMN2.Element[1, 0]) + (EQMI_T.Element[0, 2] * EQLMN2.Element[2, 0]);
            EQMT.Element[0, 1] = (EQMI_T.Element[0, 0] * EQLMN2.Element[0, 1]) + (EQMI_T.Element[0, 1] * EQLMN2.Element[1, 1]) + (EQMI_T.Element[0, 2] * EQLMN2.Element[2, 1]);
            EQMT.Element[0, 2] = (EQMI_T.Element[0, 0] * EQLMN2.Element[0, 2]) + (EQMI_T.Element[0, 1] * EQLMN2.Element[1, 2]) + (EQMI_T.Element[0, 2] * EQLMN2.Element[2, 2]);

            EQMT.Element[1, 0] = (EQMI_T.Element[1, 0] * EQLMN2.Element[0, 0]) + (EQMI_T.Element[1, 1] * EQLMN2.Element[1, 0]) + (EQMI_T.Element[1, 2] * EQLMN2.Element[2, 0]);
            EQMT.Element[1, 1] = (EQMI_T.Element[1, 0] * EQLMN2.Element[0, 1]) + (EQMI_T.Element[1, 1] * EQLMN2.Element[1, 1]) + (EQMI_T.Element[1, 2] * EQLMN2.Element[2, 1]);
            EQMT.Element[1, 2] = (EQMI_T.Element[1, 0] * EQLMN2.Element[0, 2]) + (EQMI_T.Element[1, 1] * EQLMN2.Element[1, 2]) + (EQMI_T.Element[1, 2] * EQLMN2.Element[2, 2]);

            EQMT.Element[2, 0] = (EQMI_T.Element[2, 0] * EQLMN2.Element[0, 0]) + (EQMI_T.Element[2, 1] * EQLMN2.Element[1, 0]) + (EQMI_T.Element[2, 2] * EQLMN2.Element[2, 0]);
            EQMT.Element[2, 1] = (EQMI_T.Element[2, 0] * EQLMN2.Element[0, 1]) + (EQMI_T.Element[2, 1] * EQLMN2.Element[1, 1]) + (EQMI_T.Element[2, 2] * EQLMN2.Element[2, 1]);
            EQMT.Element[2, 2] = (EQMI_T.Element[2, 0] * EQLMN2.Element[0, 2]) + (EQMI_T.Element[2, 1] * EQLMN2.Element[1, 2]) + (EQMI_T.Element[2, 2] * EQLMN2.Element[2, 2]);


            // Get the Coordinate Offset Vector and store it at EQCO Matrix

            EQCT.x = m1.x - ((a1.x * EQMT.Element[0, 0]) + (a1.y * EQMT.Element[1, 0]) + (a1.z * EQMT.Element[2, 0]));
            EQCT.y = m1.y - ((a1.x * EQMT.Element[0, 1]) + (a1.y * EQMT.Element[1, 1]) + (a1.z * EQMT.Element[2, 1]));
            EQCT.z = m1.z - ((a1.x * EQMT.Element[0, 2]) + (a1.y * EQMT.Element[1, 2]) + (a1.z * EQMT.Element[2, 2]));


            if ((x + Y) == 0)
            {
                return false;
            }
            else
            {
                return EQ_CheckPoint_in_Triangle(x, Y, a1.x, a1.y, a2.x, a2.y, a3.x, a3.y);
            }


        }


        /// <summary>
        /// Function to transform the Coordinates (Taki Method)  using the MT Matrix and Offset Vector
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
        private CartesCoord EQ_Transform_Taki(CartesCoord pos)
        {
            CartesCoord result = new CartesCoord();
            result.x = EQCT.x + ((pos.x * EQMT.Element[0, 0]) + (pos.y * EQMT.Element[1, 0]) + (pos.z * EQMT.Element[2, 0]));
            result.y = EQCT.y + ((pos.x * EQMT.Element[0, 1]) + (pos.y * EQMT.Element[1, 1]) + (pos.z * EQMT.Element[2, 1]));
            result.z = EQCT.z + ((pos.x * EQMT.Element[0, 2]) + (pos.y * EQMT.Element[1, 2]) + (pos.z * EQMT.Element[2, 2]));
            return result;
        }

        /// <summary>
        /// Subroutine to draw the Transformation Matrix (Affine Mapping Method) 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="Y"></param>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="a3"></param>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <param name="m3"></param>
        /// <returns></returns>
        private bool EQ_AssembleMatrix_Affine(double x, double Y, Coord a1, Coord a2, Coord a3, Coord m1, Coord m2, Coord m3)
        {

            double Det = 0;

            // Get the P Matrix
            EQMP = GETPQ(a1, a2, a3);

            // Get the Q Matrix
            EQMQ = GETPQ(m1, m2, m3);

            // Get the Inverse of P
            // Get the EQMP Determinant for Inverse Computation
            Det = (EQMP.Element[0, 0] * EQMP.Element[1, 1]) - (EQMP.Element[0, 1] * EQMP.Element[1, 0]);

            // Make sure Determinant is NON ZERO
            if (Det == 0)
            {
                throw new System.Exception("999, AssembleMatrix, Cannot invert matrix with Determinant = 0");
            }
            else
            {
                //Perform the Matrix Inversion, put result to EQMI matrix
                EQMI.Element[0, 0] = (EQMP.Element[1, 1]) / Det;
                EQMI.Element[0, 1] = (-EQMP.Element[0, 1]) / Det;
                EQMI.Element[1, 0] = (-EQMP.Element[1, 0]) / Det;
                EQMI.Element[1, 1] = (EQMP.Element[0, 0]) / Det;
            }

            // Get the M Matrix by Multiplying EQMI and EQMQ
            // EQMI - Matrix A
            // EQMQ - Matrix B
            EQMM.Element[0, 0] = (EQMI.Element[0, 0] * EQMQ.Element[0, 0]) + (EQMI.Element[0, 1] * EQMQ.Element[1, 0]);
            EQMM.Element[0, 1] = (EQMI.Element[0, 0] * EQMQ.Element[0, 1]) + (EQMI.Element[0, 1] * EQMQ.Element[1, 1]);
            EQMM.Element[1, 0] = (EQMI.Element[1, 0] * EQMQ.Element[0, 0]) + (EQMI.Element[1, 1] * EQMQ.Element[1, 0]);
            EQMM.Element[1, 1] = (EQMI.Element[1, 0] * EQMQ.Element[0, 1]) + (EQMI.Element[1, 1] * EQMQ.Element[1, 1]);

            // Get the Coordinate Offset Vector and store it at EQCO Matrix
            EQCO.x = m1.x - ((a1.x * EQMM.Element[0, 0]) + (a1.y * EQMM.Element[1, 0]));
            EQCO.y = m1.y - ((a1.x * EQMM.Element[0, 1]) + (a1.y * EQMM.Element[1, 1]));

            if ((x + Y) == 0)
            {
                return false;
            }
            else
            {
                return EQ_CheckPoint_in_Triangle(x, Y, m1.x, m1.y, m2.x, m2.y, m3.x, m3.y);
            }

        }


        /// <summary>
        /// Function to transform the Coordinates (Affine Mapping) using the M Matrix and Offset Vector
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private CartesCoord EQ_Transform_Affine(CartesCoord pos)
        {
            CartesCoord result = new CartesCoord();
            result.x = EQCO.x + ((pos.x * EQMM.Element[0, 0]) + (pos.y * EQMM.Element[1, 0]));
            result.y = EQCO.y + ((pos.x * EQMM.Element[0, 1]) + (pos.y * EQMM.Element[1, 1]));
            return result;
        }

        //Function to convert spherical coordinates to Cartesian using the Coord structure

        public Coord EQ_sp2Cs(EncoderPosition pos)
        {
            Coord result = new Coord();

            CartesCoord tmpobj = new CartesCoord();
            SphericalCoord tmpobj4 = new SphericalCoord();

            tmpobj4 = EQ_SphericalPolar(pos);
            tmpobj = EQ_Polar2Cartes(tmpobj4);
            result.x = tmpobj.x;
            result.y = tmpobj.y;
            result.z = 1;

            return result;
        }


        ////Function to convert spherical coordinates to Cartesian using the Coord structure

        //internal Coord EQ_sp2Cs2(Coord obj)
        //{
        //    Coord result = new Coord();
        //    double gDECEncoder_Home_pos = 0;
        //    double gLatitude = 0;
        //    double gTot_step = 0;
        //    object HC = null;
        //    double RAEncoder_Home_pos = 0;

        //    CartesCoord tmpobj = new CartesCoord();
        //    SphericalCoord tmpobj4 = new SphericalCoord();

        //    if (PolarEnable)
        //    {
        //        tmpobj4 = EQ_SphericalPolar(obj.x, obj.y, this.StepsPerRev, this.HomeEncoder, Math.Abs(gLatitude));
        //        tmpobj = EQ_Polar2Cartes(tmpobj4.x, tmpobj4.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos);
        //        result.x = tmpobj.x;
        //        result.y = tmpobj.y;
        //        result.z = 1;
        //    }
        //    else
        //    {
        //        result.x = obj.x;
        //        result.y = obj.y;
        //        result.z = 1;
        //    }

        //    return result;
        //}


        ////Function to convert polar coordinates to Cartesian using the Coord structure


        //internal Coord EQ_pl2Cs(Coord obj)
        //{
        //    Coord result = new Coord();
        //    double gDECEncoder_Home_pos = 0;
        //    double gTot_step = 0;
        //    object HC = null;
        //    double RAEncoder_Home_pos = 0;

        //    CartesCoord tmpobj = new CartesCoord();

        //    if (PolarEnable)
        //    {
        //        tmpobj = EQ_Polar2Cartes(obj.x, obj.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos);

        //        result.x = tmpobj.x;
        //        result.y = tmpobj.y;
        //        result.z = 1;
        //    }
        //    else
        //    {
        //        result.x = obj.x;
        //        result.y = obj.y;
        //        result.z = 1;
        //    }

        //    return result;
        //}


        /// <summary>
        /// Implement an Affine transformation on a Polar coordinate system
        /// This is done by converting the Polar Data to Cartesian, Apply affine transformation
        /// Then restore the transformed Cartesian Coordinates back to polar
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal EncoderPosition EQ_plAffine(EncoderPosition pos)
        {
            EncoderPosition result = new EncoderPosition();
            CartesCoord tmpobj1 = new CartesCoord();
            CartesCoord tmpobj3 = new CartesCoord();
            SphericalCoord tmpobj2 = new SphericalCoord();
            SphericalCoord tmpobj4 = new SphericalCoord();

            tmpobj4 = EQ_SphericalPolar(pos);
            tmpobj1 = EQ_Polar2Cartes(tmpobj4);

            tmpobj3 = EQ_Transform_Affine(tmpobj1);

            tmpobj2 = EQ_Cartes2Polar(tmpobj3, tmpobj1);
            result = EQ_PolarSpherical(tmpobj2, tmpobj4);
            return result;
        }


        //internal Coord EQ_plAffine2(Coord obj)
        //{
        //    Coord result = new Coord();
        //    double gDECEncoder_Home_pos = 0;
        //    double gLatitude = 0;
        //    double gTot_step = 0;
        //    object HC = null;
        //    double RAEncoder_Home_pos = 0;

        //    CartesCoord tmpobj1 = new CartesCoord();
        //    Coord tmpobj2 = new Coord();
        //    Coord tmpobj3 = new Coord();
        //    SphericalCoord tmpobj4 = new SphericalCoord();

        //    if (PolarEnable)
        //    {
        //        tmpobj4 = EQ_SphericalPolar(obj.x, obj.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos, gLatitude);

        //        tmpobj1 = EQ_Polar2Cartes(tmpobj4.x, tmpobj4.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos);

        //        tmpobj2.x = tmpobj1.x;
        //        tmpobj2.y = tmpobj1.y;
        //        tmpobj2.z = 1;

        //        tmpobj3 = EQ_Transform_Affine(tmpobj2);

        //        tmpobj2 = EQ_Cartes2Polar(tmpobj3.x, tmpobj3.y, tmpobj1.r, tmpobj1.ra, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos);


        //        result = EQ_PolarSpherical(tmpobj2.x, tmpobj2.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos, gLatitude, tmpobj4.r);

        //    }
        //    else
        //    {
        //        tmpobj3 = EQ_Transform_Affine(obj);
        //        result.x = tmpobj3.x;
        //        result.y = tmpobj3.y;
        //        result.z = 1;
        //    }

        //    return result;
        //}


        /// <summary>
        /// Implement a TAKI transformation on a Polar coordinate system
        /// This is done by converting the Polar Data to Cartesian, Apply TAKI transformation
        /// Then restore the transformed Cartesian Coordinates back to polar
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal EncoderPosition EQ_plTaki(EncoderPosition pos)
        {
            EncoderPosition result = new EncoderPosition();
            CartesCoord tmpobj1 = new CartesCoord();
            CartesCoord tmpobj3 = new CartesCoord();
            SphericalCoord tmpobj2 = new SphericalCoord();
            SphericalCoord tmpobj4 = new SphericalCoord();

            tmpobj4 = EQ_SphericalPolar(pos);
            tmpobj1 = EQ_Polar2Cartes(tmpobj4);

            tmpobj3 = EQ_Transform_Taki(tmpobj1);

            tmpobj2 = EQ_Cartes2Polar(tmpobj3, tmpobj1);
            result = EQ_PolarSpherical(tmpobj2, tmpobj4);


            return result;
        }

        /// <summary>
        /// Function to Convert Polar RA/DEC Stepper coordinates to Cartesian Coordinates
        /// </summary>
        /// <param name="polar"></param>
        /// <returns></returns>
        private CartesCoord EQ_Polar2Cartes(SphericalCoord polar)
        {
            CartesCoord result = new CartesCoord();
            double i = 0;

            // make angle stays within the 360 bound
            if (polar.x > this.HomeEncoder.RA)
            {
                i = ((polar.x - this.HomeEncoder.RA) / this.StepsPerRev.RA) * 360;
            }
            else
            {
                i = ((this.HomeEncoder.RA - polar.x) / this.StepsPerRev.RA) * 360;
                i = 360 - i;
            }

            double theta = AstroConvert.DegToRad(Range.Range360(i));

            //treat y as the radius of the polar coordinate

            double radius = polar.y - this.HomeEncoder.Dec;

            double radpeak = 0;


            // Avoid division 0 errors

            if (radius == 0)
            {
                radius = 1;
            }

            // Get the cartesian coordinates

            result.x = Math.Cos(theta) * radius;
            result.y = Math.Sin(theta) * radius;
            result.ra = radpeak;

            // if radius is a negative number, pass this info on the next conversion routine

            if (radius > 0)
            {
                result.r = 1;
            }
            else
            {
                result.r = -1;
            }

            return result;
        }

        ////Function to convert the Cartesian Coordinate data back to RA/DEC polar

        private SphericalCoord EQ_Cartes2Polar(CartesCoord cart, CartesCoord rads)
        {
            SphericalCoord result = new SphericalCoord();

            // Ah the famous radius formula
            double radiusder = Math.Sqrt((cart.x * cart.x) + (cart.y * cart.y)) * rads.r;


            // And the nasty angle compute routine (any simpler way to impelent this ?)

            double angle = 0;
            if (cart.x > 0)
            {
                angle = Math.Atan(cart.y / cart.x);
            }
            if (cart.x < 0)
            {
                if (cart.y >= 0)
                {
                    angle = Math.Atan(cart.y / cart.x) + Math.PI;

                }
                else
                {
                    angle = Math.Atan(cart.y / cart.x) - Math.PI;
                }
            }
            if (cart.x == 0)
            {
                if (cart.y > 0)
                {
                    angle = Math.PI / 2d;
                }
                else
                {
                    angle = -1 * (Math.PI / 2d);
                }
            }

            // Convert angle to degrees

            angle = AstroConvert.RadToDeg(angle);

            if (angle < 0)
            {
                angle = 360 + angle;
            }

            if (rads.r < 0)
            {
                angle = Range.Range360((angle + 180));
            }

            if (angle > 180)
            {
                result.x = this.HomeEncoder.RA - (((360 - angle) / 360d) * this.StepsPerRev.RA);
            }
            else
            {
                result.x = ((angle / 360d) * this.StepsPerRev.RA) + this.HomeEncoder.RA;
            }

            //treat y as the polar coordinate radius (ra var not used - always 0)

            result.y = radiusder + this.HomeEncoder.Dec + rads.ra;

            return result;
        }

        internal bool EQ_UpdateTaki(EncoderPosition pos)
        {
            bool result = false;
            List<AlignmentPoint> nearestPoints = new List<AlignmentPoint>();

            // Adjust only if there are four alignment stars
            if (this.AlignmentPoints.Count < 3)
            {
                return result;
            }


            switch (this.ThreePointAlgorithm)
            {
                case ThreePointAlgorithmEnum.BestCentre:
                    // find the 50 nearest points - then find the nearest enclosing triangle 
                    nearestPoints = EQ_ChooseNearest3Points(pos);
                    break;
                default:
                    // find the 50 nearest points - then find the enclosing triangle with the nearest centre point 
                    nearestPoints = EQ_Choose_3Points(pos);
                    break;
            }

            if (nearestPoints.Count < 3)
            {
                return false;
            }

            Coord tmpCoord = EQ_sp2Cs(pos);

            return EQ_AssembleMatrix_Taki(tmpCoord.x, tmpCoord.y,
                nearestPoints[0].TargetCartesian,
                nearestPoints[1].TargetCartesian,
                nearestPoints[2].TargetCartesian,
                nearestPoints[0].EncoderCartesian,
                nearestPoints[1].EncoderCartesian,
                nearestPoints[2].EncoderCartesian);

        }

        internal bool EQ_UpdateAffine(EncoderPosition pos)
        {
            bool result = false;

            List<AlignmentPoint> nearestPoints = new List<AlignmentPoint>();

            if (this.AlignmentPoints.Count < 3)
            {
                return result;
            }

            switch (this.ThreePointAlgorithm)
            {
                case ThreePointAlgorithmEnum.BestCentre:
                    // find the 50 nearest points - then find the nearest enclosing triangle 
                    nearestPoints = EQ_ChooseNearest3Points(pos);
                    break;
                default:
                    // find the 50 nearest points - then find the enclosing triangle with the nearest centre point 
                    nearestPoints = EQ_Choose_3Points(pos);
                    break;
            }


            if (nearestPoints.Count < 3)
            {
                return false;
            }

            Coord tmpcoord = EQ_sp2Cs(pos);

            return EQ_AssembleMatrix_Affine(tmpcoord.x, tmpcoord.y,
                nearestPoints[0].EncoderCartesian,
                nearestPoints[1].EncoderCartesian,
                nearestPoints[2].EncoderCartesian,
                nearestPoints[0].TargetCartesian,
                nearestPoints[1].TargetCartesian,
                nearestPoints[2].TargetCartesian);
        }

        //// Subroutine to implement find Array index with the lowest value
        //private int EQ_FindLowest(double[] List, int min, int max)
        //{
        //    double val = 0;
        //    double newval = 0;
        //    int i = 0;

        //    int idx = -1;
        //    int tempForEndVar = 0;
        //    if (!(min >= max || max > List.GetUpperBound(0)))
        //    {

        //        val = List[min];
        //        tempForEndVar = max;
        //        for (i = min; i <= tempForEndVar; i++)
        //        {
        //            newval = List[i];
        //            if (newval <= val)
        //            {
        //                val = newval;
        //                idx = i;
        //            }
        //        }

        //    }

        //    return idx;
        //}

        //private void EQ_FindLowest3(double[] List, int[] Sublist, int min, int max)
        //{
        //    double val = 0;
        //    double min1 = 0;
        //    double min2 = 0;
        //    double min3 = 0;
        //    int i = 0;

        //    int tempForEndVar = 0;
        //    if (!(min >= max || max > List.GetUpperBound(0)))
        //    {

        //        if (List[1] <= List[2] && List[1] <= List[3])
        //        {
        //            //List 1 is first
        //            min1 = List[1];
        //            if (List[2] <= List[3])
        //            {
        //                //List2 is second
        //                //List3 is third
        //                min2 = List[2];
        //                min3 = List[3];
        //            }
        //            else
        //            {
        //                //List3 is second
        //                //List2 is third
        //                min2 = List[3];
        //                min3 = List[2];
        //            }
        //        }
        //        else
        //        {
        //            if (List[2] <= List[1] && List[2] <= List[3])
        //            {
        //                //List 2 is first
        //                min1 = List[2];
        //                if (List[1] <= List[3])
        //                {
        //                    //List1 is second
        //                    //List3 is third
        //                    min2 = List[1];
        //                    min3 = List[3];
        //                }
        //                else
        //                {
        //                    //List3 is second
        //                    //List1 is third
        //                    min2 = List[3];
        //                    min3 = List[1];
        //                }
        //            }
        //            else
        //            {
        //                if (List[3] <= List[1] && List[3] <= List[2])
        //                {
        //                    //List 3 is first
        //                    min1 = List[3];
        //                    if (List[1] <= List[2])
        //                    {
        //                        //List1 is second
        //                        //List2 is third
        //                        min2 = List[1];
        //                        min3 = List[2];
        //                    }
        //                    else
        //                    {
        //                        //List2 is second
        //                        //List1 is third
        //                        min2 = List[2];
        //                        min3 = List[1];
        //                    }
        //                }
        //            }
        //        }

        //        val = List[min];

        //        tempForEndVar = max;
        //        for (i = min; i <= tempForEndVar; i++)
        //        {
        //            val = List[i];
        //            if (val < min1)
        //            {
        //                min1 = val;
        //                Sublist[3] = Sublist[2];
        //                Sublist[2] = Sublist[1];
        //                Sublist[1] = i;
        //            }
        //            else
        //            {
        //                if (val < min2)
        //                {
        //                    min2 = val;
        //                    Sublist[3] = Sublist[2];
        //                    Sublist[2] = i;
        //                }
        //                else
        //                {
        //                    if (val < min3)
        //                    {
        //                        Sublist[3] = i;
        //                    }
        //                }
        //            }
        //        }

        //    }


        //}




        //// Subroutine to implement an Array sort
        //private void EQ_Quicksort(double[] List, double[] Sublist, int min, int max)
        //{



        //    if (min >= max)
        //    {
        //        return;
        //    }

        //    int i = Convert.ToInt32(Math.Floor((double)((max - min + 1) * GetRnd() + min)));
        //    double med_value = List[i];
        //    double submed = Sublist[i];

        //    List[i] = List[min];
        //    Sublist[i] = Sublist[min];

        //    int lo = min;
        //    int hi = max;
        //    do
        //    {

        //        while (List[hi] >= med_value)
        //        {
        //            hi--;
        //            if (hi <= lo)
        //            {
        //                break;
        //            }
        //        };
        //        if (hi <= lo)
        //        {
        //            List[lo] = med_value;
        //            Sublist[lo] = submed;
        //            break;
        //        }

        //        List[lo] = List[hi];
        //        Sublist[lo] = Sublist[hi];

        //        lo++;

        //        while (List[lo] < med_value)
        //        {
        //            lo++;
        //            if (lo >= hi)
        //            {
        //                break;
        //            }
        //        };

        //        if (lo >= hi)
        //        {
        //            lo = hi;
        //            List[hi] = med_value;
        //            Sublist[hi] = submed;
        //            break;
        //        }

        //        List[hi] = List[lo];
        //        Sublist[hi] = Sublist[lo];

        //    }
        //    while (true);

        //    EQ_Quicksort(List, Sublist, min, lo - 1);
        //    EQ_Quicksort(List, Sublist, lo + 1, max);

        //}


        //// Subroutine to implement an Array sort

        //private void EQ_Quicksort2(Tdatholder[] List, int min, int max)
        //{

        //    if (min >= max)
        //    {
        //        return;
        //    }

        //    int i = Convert.ToInt32(Math.Floor((double)((max - min + 1) * GetRnd() + min)));
        //    Tdatholder med_value = Tdatholder.CreateInstance();

        //    List[i] = List[min];

        //    int lo = min;
        //    int hi = max;

        //    do
        //    {

        //        while (List[hi].dat >= med_value.dat)
        //        {
        //            hi--;
        //            if (hi <= lo)
        //            {
        //                break;
        //            }
        //        };
        //        if (hi <= lo)
        //        {
        //            List[lo] = med_value;
        //            break;
        //        }

        //        List[lo] = List[hi];

        //        lo++;

        //        while (List[lo].dat < med_value.dat)
        //        {
        //            lo++;
        //            if (lo >= hi)
        //            {
        //                break;
        //            }
        //        };
        //        if (lo >= hi)
        //        {
        //            lo = hi;
        //            List[hi] = med_value;
        //            break;
        //        }

        //        List[hi] = List[lo];
        //    }
        //    while (true);

        //    EQ_Quicksort2(List, min, lo - 1);
        //    EQ_Quicksort2(List, lo + 1, max);

        //}

        //// Subroutine to implement an Array sort with three sublists

        //private void EQ_Quicksort3(double[] List, double[] Sublist1, double[] Sublist2, double[] Sublist3, int min, int max)
        //{

        //    if (min >= max)
        //    {
        //        return;
        //    }

        //    int i = Convert.ToInt32(Math.Floor((double)((max - min + 1) * GetRnd() + min)));
        //    double med_value = List[i];
        //    double submed1 = Sublist1[i];
        //    double submed2 = Sublist2[i];
        //    double submed3 = Sublist3[i];

        //    List[i] = List[min];
        //    Sublist1[i] = Sublist1[min];
        //    Sublist2[i] = Sublist2[min];
        //    Sublist3[i] = Sublist3[min];

        //    int lo = min;
        //    int hi = max;
        //    do
        //    {


        //        while (List[hi] >= med_value)
        //        {
        //            hi--;
        //            if (hi <= lo)
        //            {
        //                break;
        //            }
        //        };
        //        if (hi <= lo)
        //        {
        //            List[lo] = med_value;
        //            Sublist1[lo] = submed1;
        //            Sublist2[lo] = submed2;
        //            Sublist3[lo] = submed3;
        //            break;
        //        }


        //        List[lo] = List[hi];
        //        Sublist1[lo] = Sublist1[hi];
        //        Sublist2[lo] = Sublist2[hi];
        //        Sublist3[lo] = Sublist3[hi];

        //        lo++;

        //        while (List[lo] < med_value)
        //        {
        //            lo++;
        //            if (lo >= hi)
        //            {
        //                break;
        //            }
        //        };
        //        if (lo >= hi)
        //        {
        //            lo = hi;
        //            List[hi] = med_value;
        //            Sublist1[hi] = submed1;
        //            Sublist2[hi] = submed2;
        //            Sublist3[hi] = submed3;
        //            break;
        //        }

        //        List[hi] = List[lo];
        //        Sublist1[hi] = Sublist1[lo];
        //        Sublist2[hi] = Sublist2[lo];
        //        Sublist3[hi] = Sublist3[lo];
        //    }
        //    while (true);

        //    EQ_Quicksort3(List, Sublist1, Sublist2, Sublist3, min, lo - 1);
        //    EQ_Quicksort3(List, Sublist1, Sublist2, Sublist3, lo + 1, max);

        //}

        // Function to compute for an area of a triangle

        private double EQ_Triangle_Area(double px1, double py1, double px2, double py2, double px3, double py3)
        {


            //True formula is this
            //    EQ_Triangle_Area = Abs(((px2 * py1) - (px1 * py2)) + ((px3 * py2) - (px2 * py3)) + ((px1 * py3) - (px3 * py1))) / 2

            // Make LARGE  numerical value safe for Windows by adding a scaling factor

            double ta = (((px2 * py1) - (px1 * py2)) / 10000d) + (((px3 * py2) - (px2 * py3)) / 10000d) + (((px1 * py3) - (px3 * py1)) / 10000d);

            return Math.Abs(ta) / 2d;

        }

        // Function to check if a point is inside the triangle. Computed based sum of areas method

        private bool EQ_CheckPoint_in_Triangle(double px, double py, double px1, double py1, double px2, double py2, double px3, double py3)
        {


            double ta = EQ_Triangle_Area(px1, py1, px2, py2, px3, py3);
            double t1 = EQ_Triangle_Area(px, py, px2, py2, px3, py3);
            double t2 = EQ_Triangle_Area(px1, py1, px, py, px3, py3);
            double t3 = EQ_Triangle_Area(px1, py1, px2, py2, px, py);


            if (Math.Abs(ta - t1 - t2 - t3) < 2)
            {
                return true;
            }
            else
            {
                return false;
            }

        }



        /// <summary>
        /// Returns the centroid of a triangle.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        private Coord EQ_GetCenterPoint(Coord p1, Coord p2, Coord p3)
        {

            Coord result = new Coord();
            double p2x = 0d;
            double p2y = 0d;
            double p4x = 0d;
            double p4y = 0d;

            // Get the two line 4 point data

            double p1x = p1.x;
            double p1y = p1.y;


            if (p3.x > p2.x)
            {
                p2x = ((p3.x - p2.x) / 2d) + p2.x;
            }
            else
            {
                p2x = ((p2.x - p3.x) / 2d) + p3.x;
            }

            if (p3.y > p2.y)
            {
                p2y = ((p3.y - p2.y) / 2d) + p2.y;
            }
            else
            {
                p2y = ((p2.y - p3.y) / 2d) + p3.y;
            }

            double p3x = p2.x;
            double p3y = p2.y;


            if (p1.x > p3.x)
            {
                p4x = ((p1.x - p3.x) / 2d) + p3.x;
            }
            else
            {
                p4x = ((p3.x - p1.x) / 2d) + p1.x;
            }

            if (p1.y > p3.y)
            {
                p4y = ((p1.y - p3.y) / 2d) + p3.y;
            }
            else
            {
                p4y = ((p3.y - p1.y) / 2d) + p1.y;
            }


            double XD1 = p2x - p1x;
            double XD2 = p4x - p3x;
            double YD1 = p2y - p1y;
            double YD2 = p4y - p3y;
            double XD3 = p1x - p3x;
            double YD3 = p1y - p3y;


            double dv = (YD2 * XD1) - (XD2 * YD1);

            if (dv == 0)
            {
                dv = 0.00000001d;
            } //avoid div 0 errors


            double ua = ((XD2 * YD3) - (YD2 * XD3)) / dv;
            double ub = ((XD1 * YD3) - (YD1 * XD3)) / dv;

            result.x = p1x + (ua * XD1);
            result.y = p1y + (ub * YD1);

            return result;
        }


        private SphericalCoord EQ_SphericalPolar(EncoderPosition spherical)
        {
            //Debug.WriteLine($"Spherical -> Polar");
            //Debug.WriteLine($"Input = {spherical.RA}/{spherical.Dec}");
            SphericalCoord result = new SphericalCoord();

            double haRad = AstroConvert.HrsToRad(Get_EncoderHours(spherical.RA));
            double decRad = AstroConvert.DegToRad(Range.Range360(Get_EncoderDegrees(spherical.Dec) + 270));
            double latRad = AstroConvert.DegToRad(this.SiteLatitude);
            //Debug.WriteLine($"lat/ha/dec (Radians) = {latRad}/{haRad}/{decRad}");
            double[] altAzRad = AstroConvert.GetAltAz(latRad, haRad, decRad);
            //Debug.WriteLine($"alt/Az (Radians) = {altAzRad[0]}/{altAzRad[1]}");
            double[] altAz = new double[] { AstroConvert.RadToDeg(altAzRad[0]), AstroConvert.RadToDeg(altAzRad[1]) };
            //Debug.WriteLine($"alt/Az = {altAz[0]}/{altAz[1]}");

            result.x = (((altAz[1] - 180) / 360d) * this.StepsPerRev.RA) + this.HomeEncoder.RA;
            result.y = (((altAz[0] + 90) / 180d) * this.StepsPerRev.Dec) + this.HomeEncoder.Dec;

            // Check if RA value is within allowed visible range
            double quadrant = this.StepsPerRev.RA / 4d;
            if ((spherical.RA <= (this.HomeEncoder.RA + quadrant)) && (spherical.RA >= (this.HomeEncoder.RA - quadrant)))
            {
                result.r = 1;
            }
            else
            {
                result.r = 0;
            }
            //Debug.WriteLine($"Output = {result.x}/{result.y}/{result.r}");
            return result;
        }

        private EncoderPosition EQ_PolarSpherical(SphericalCoord pos, SphericalCoord range)
        {
            //Debug.WriteLine($"Polar -> Spherical");
            //Debug.WriteLine($"Input = {pos.x}/{pos.y}/{range.r}");
            EncoderPosition result = new EncoderPosition();
            double az = (((pos.x - this.HomeEncoder.RA) / this.StepsPerRev.RA) * 360) + 180;
            double azRad = AstroConvert.DegToRad(az);
            double alt = (((pos.y - this.HomeEncoder.Dec) / this.StepsPerRev.Dec) * 180) - 90;
            double altRad = AstroConvert.DegToRad(alt);
            double latRad = AstroConvert.DegToRad(this.SiteLatitude);
            //Debug.WriteLine($"az/alt (Degrees) = {az}/{alt}");
            //Debug.WriteLine($"lat/alt/Az (Radians) = {latRad}/{altRad}/{azRad}");
            double[] haDecRad = AstroConvert.GetHaDec(latRad, altRad, azRad);
            //Debug.WriteLine($"ha/dec (Radians) = {haDecRad[0]}/{haDecRad[1]}");
            double[] haDec = new double[] { AstroConvert.RadToHrs(haDecRad[0]), AstroConvert.RadToDeg(haDecRad[1]) };
            //Debug.WriteLine($"ha/dec = {haDec[0]}/{haDec[1]}");

            if (az > 180)
            {
                if (range.r == 0)
                {
                    haDec[1] = Range.Range360(180 - haDec[1]);
                }
                else
                {
                    haDec[1] = Range.Range360(haDec[1]);
                }
            }
            else
            {
                if (range.r == 0)
                {
                    haDec[1] = Range.Range360(haDec[1]);
                }
                else
                {
                    haDec[1] = Range.Range360(180 - haDec[1]);
                }
            }

            alt = Range.Range360(haDec[1] + 90);

            if (alt < 180)
            {
                if (range.r == 1)
                {
                    haDec[0] = Range.Range24(haDec[0]);
                }
                else
                {
                    haDec[0] = Range.Range24(24 + haDec[0]);
                }
            }
            else
            {
                haDec[0] = Range.Range24(12 + haDec[0]);
            }

            //Debug.WriteLine($"X/Y = {haDec[0]}/{haDec[1]}");

            result.RA = Get_EncoderfromHours(haDec[0]);
            result.Dec = Get_EncoderfromDegrees(haDec[1] + 90);

            //Debug.WriteLine($"Output = {result.RA}/{result.Dec}");
            return result;
        }


        //private CartesCoord EQ_Spherical2Cartes(double RA, double DEC, double TOT, double RACENTER, double DECCENTER)
        //{
        //    CartesCoord result = new CartesCoord();
        //    double gLatitude = 0;


        //    SphericalCoord tmpobj4 = EQ_SphericalPolar(RA, DEC, TOT, RACENTER, DECCENTER, gLatitude);

        //    CartesCoord tmpobj1 = EQ_Polar2Cartes(tmpobj4.x, tmpobj4.y, TOT, RACENTER, DECCENTER);

        //    result.x = tmpobj1.x;
        //    result.y = tmpobj1.y;
        //    result.ra = tmpobj1.ra;
        //    result.r = tmpobj1.r;

        //    return result;
        //}

        //private Coord EQ_Cartes2Spherical(double x, double Y, double r, double RA, double range, double TOT, double RACENTER, double DECCENTER)
        //{
        //    double gDECEncoder_Home_pos = 0;
        //    double gLatitude = 0;
        //    double gTot_step = 0;
        //    double RAEncoder_Home_pos = 0;

        //    Coord tmpobj2 = EQ_Cartes2Polar(x, Y, r, RA, TOT, RACENTER, DECCENTER);
        //    return EQ_PolarSpherical(tmpobj2.x, tmpobj2.y, gTot_step, RAEncoder_Home_pos, gDECEncoder_Home_pos, gLatitude, range);

        //}

        /// <summary>
        /// Returns the 3 points making up at triangle with the centre nearest the position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>List of 3 points or an empty list</returns>
        internal List<AlignmentPoint> EQ_Choose_3Points(EncoderPosition pos)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();
            List<AlignmentPoint> results = new List<AlignmentPoint>();
            // Adjust only if there are three alignment stars

            if (AlignmentPoints.Count <= 3)
            {
                foreach (AlignmentPoint pt in this.AlignmentPoints)
                {
                    pt.SelectedForGoto = true;
                    results.Add(pt);
                }
                return results.OrderBy(p => p.AlignTime).ToList();
            }

            Coord posCartesean = EQ_sp2Cs(pos);

            // first find out the distances to the alignment stars
            foreach (AlignmentPoint pt in this.AlignmentPoints)
            {
                pt.SelectedForGoto = false;
                switch (this.ActivePoints)
                {
                    case ActivePointsEnum.All:
                        // all points 

                        break;
                    case ActivePointsEnum.PierSide:
                        // only consider points on this side of the meridian 
                        if (pt.EncoderCartesian.y * posCartesean.y < 0)
                        {
                            continue;
                        }

                        break;
                    case ActivePointsEnum.LocalQuadrant:
                        // local quadrant 
                        if (!GetQuadrant(posCartesean).Equals(GetQuadrant(pt.EncoderCartesian)))
                        {
                            continue;
                        }

                        break;
                }

                if (CheckLocalPier)
                {
                    // calculate polar distance
                    distances.Add(pt.Id, Math.Pow(pt.Encoder.RA - pos.RA, 2) + Math.Pow(pt.Encoder.Dec - pos.Dec, 2));
                }
                else
                {
                    // calculate cartesian disatnce
                    distances.Add(pt.Id, Math.Pow(pt.EncoderCartesian.x - posCartesean.x, 2) + Math.Pow(pt.EncoderCartesian.y - posCartesean.y, 2));
                }
            }

            if (distances.Count < 3)
            {
                return results; // Empty list.
            }

            // now sort the distances so the closest stars are at the top
            //Just use the nearest 50 stars (or max) - saves processing time
            List<int> sortedIds = distances.OrderBy(d => d.Value).Select(d => d.Key).Take(this.NStarMaxCombinationCount).ToList();

            var tmp1 = sortedIds.Count - 1;
            var tmp2 = tmp1 - 1;

            // iterate through all the triangles posible using the nearest alignment points
            double minCentreDistance = double.MaxValue;
            double centreDistance;
            Coord triangleCentre;
            for (int i = 0; i <= tmp2; i++)
            {
                var p1 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[i]);
                for (int j = i + 1; j < tmp1; j++)
                {
                    var p2 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[j]);
                    for (int k = (j + 1); k < sortedIds.Count; k++)
                    {
                        var p3 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[k]);


                        if (EQ_CheckPoint_in_Triangle(posCartesean.x, posCartesean.y,
                            p1.EncoderCartesian.x, p1.EncoderCartesian.y,
                            p2.EncoderCartesian.x, p2.EncoderCartesian.y,
                            p3.EncoderCartesian.x, p3.EncoderCartesian.y))
                        {
                            // Compute for the center point
                            triangleCentre = EQ_GetCenterPoint(p1.EncoderCartesian, p2.EncoderCartesian, p3.EncoderCartesian);
                            // don't need full pythagoras - sum of squares is good enough
                            centreDistance = Math.Pow(triangleCentre.x - posCartesean.x, 2) + Math.Pow(triangleCentre.y - posCartesean.y, 2);
                            if (centreDistance < minCentreDistance)
                            {
                                results = new List<AlignmentPoint> { p3, p2, p1 };  // Reversed to match EQMOD sort order
                                minCentreDistance = centreDistance;
                            }
                        }
                    }
                }
            }
            results.ForEach(p => p.SelectedForGoto = true);
            return results.ToList();

        }



        /// <summary>
        /// Returns the nearest 3 alignment points that form and enclosing triangle around a position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>List of 3 points or an empty list.</returns>
        internal List<AlignmentPoint> EQ_ChooseNearest3Points(EncoderPosition pos)
        {
            Dictionary<int, double> distances = new Dictionary<int, double>();
            List<AlignmentPoint> results = new List<AlignmentPoint>();
            // Adjust only if there are three alignment stars

            if (AlignmentPoints.Count <= 3)
            {
                foreach (AlignmentPoint pt in this.AlignmentPoints)
                {
                    pt.SelectedForGoto = true;
                    results.Add(pt);
                }
                return results.OrderBy(p => p.AlignTime).ToList();
            }

            Coord posCartesean = EQ_sp2Cs(pos);

            // first find out the distances to the alignment stars
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
                        if (pt.EncoderCartesian.y * posCartesean.y < 0)
                        {
                            continue;
                        }

                        break;
                    case ActivePointsEnum.LocalQuadrant:
                        // local quadrant 
                        if (!GetQuadrant(posCartesean).Equals(GetQuadrant(pt.EncoderCartesian)))
                        {
                            continue;
                        }

                        break;
                }

                if (CheckLocalPier)
                {
                    // calculate polar distance
                    distances.Add(pt.Id, Math.Pow(pt.Encoder.RA - pos.RA, 2) + Math.Pow(pt.Encoder.Dec - pos.Dec, 2));
                }
                else
                {
                    // calculate cartesian disatnce
                    distances.Add(pt.Id, Math.Pow(pt.EncoderCartesian.x - posCartesean.x, 2) + Math.Pow(pt.EncoderCartesian.y - posCartesean.y, 2));
                }
            }

            if (distances.Count < 3)
            {
                return results; // Empty list.
            }

            // now sort the distances so the closest stars are at the top
            //Just use the nearest 50 stars (or max) - saves processing time
            List<int> sortedIds = distances.OrderBy(d => d.Value).Select(d => d.Key).Take(this.NStarMaxCombinationCount).ToList();

            var tmp1 = sortedIds.Count - 1;
            var tmp2 = tmp1 - 1;
            bool done = false;


            // iterate through all the triangles posible using the nearest alignment points
            for (int i = 0; i <= tmp2; i++)
            {
                var p1 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[i]);
                for (int j = i + 1; j < tmp1; j++)
                {
                    var p2 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[j]);
                    for (int k = (j + 1); k < sortedIds.Count; k++)
                    {
                        var p3 = this.AlignmentPoints.First(pt => pt.Id == sortedIds[k]);


                        if (EQ_CheckPoint_in_Triangle(posCartesean.x, posCartesean.y,
                            p1.EncoderCartesian.x, p1.EncoderCartesian.y,
                            p2.EncoderCartesian.x, p2.EncoderCartesian.y,
                            p3.EncoderCartesian.x, p3.EncoderCartesian.y))
                        {
                            results.Add(p1);
                            results.Add(p2);
                            results.Add(p3);
                            done = true;
                        }
                        if (done) break;
                    }
                    if (done) break;
                }
                if (done) break;
            }
            results.ForEach(p => p.SelectedForGoto = true);
            return results.ToList();

        }

        //public static float GetRnd()
        //{
        //    Random r = new Random();
        //    return (float)(r.Next(1, 1000) / 1000.0);
        //}

    }
}


