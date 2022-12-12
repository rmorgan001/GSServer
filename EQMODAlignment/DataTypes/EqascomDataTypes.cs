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
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
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
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace EqmodNStarAlignment.DataTypes
{
    public struct Coord
    {
        public double x; //x = X Coordinate
        public double y; //y = Y Coordinate
        public double z;
    }

    //[Serializable]
    //public struct Tdatholder
    //{
    //    public double dat;
    //    public short idx;
    //    public Coord cc; // cartesian coordinate
    //    public static Tdatholder CreateInstance()
    //    {
    //        Tdatholder result = new Tdatholder();
    //        return result;
    //    }
    //}

    //[Serializable]
    //public struct THolder
    //{
    //    public double a;
    //    public double b;
    //    public double c;
    //}


    public struct Matrix
    {
        public double[,] Element; //2D array of elements
        public static Matrix CreateInstance()
        {
            Matrix result = new Matrix();
            result.Element = new double[3, 3];
            return result;
        }
    }

    //[Serializable]
    //public struct Matrix2
    //{
    //    public double[,] Element; //2D array of elements
    //    public static Matrix2 CreateInstance()
    //    {
    //        Matrix2 result = new Matrix2();
    //        result.Element = new double[4, 4];
    //        return result;
    //    }
    //}

    public struct Coordt
    {
        public double x; //x = X Coordinate
        public double y; //y = Y Coordinate
        public double z;
        public short f;
    }

    public struct CartesCoord
    {
        public double x; //x = X Coordinate
        public double y; //y = Y Coordinate
        public double r; // Radius Sign
        public double ra; // Radius Alpha

        public CartesCoord(double xValue, double yValue, double rValue, double raValue)
        {
            x = xValue;
            y = yValue;
            r = rValue;
            ra = raValue;
        }
    }

    public struct SphericalCoord
    {
        public double x; //x = X Coordinate
        public double y; //y = Y Coordinate
        public double r; //r = RA Range Flag

        public SphericalCoord(double xValue, double yValue, double rValue)
        {
            x = xValue;
            y = yValue;
            r = rValue;
        }
    }


    public struct TriangleCoord
    {
        public double i; // Offset 1
        public double j; // Offset 2
        public double k; // offset 3

    }
}
