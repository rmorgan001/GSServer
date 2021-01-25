/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

using System;
using System.ComponentModel;

namespace NStarAlignment.DataTypes
{
    public enum Quadrant
    {
        [Description("Norteast")]
        Ne,
        [Description("Southeast")]
        Se,
        [Description("Southwest")]
        Sw,
        [Description("Northwest")]
        Nw
    }
    /// <summary>
    /// A structure to represent an EquatorialCoordinate
    /// </summary>
    public struct CarteseanCoordinate
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public double R { get; set; }

        public double Ra { get; set; }

        public bool Flag { get; set; }

        /// <summary>
        /// Equivalent AltAz quadrant.
        /// </summary>
        public Quadrant Quadrant
        {
            get
            {
                if (X >= 0)
                {
                    return Y >= 0 ? Quadrant.Sw : Quadrant.Se;
                }
                else
                {
                    return Y < 0 ? Quadrant.Ne : Quadrant.Nw;
                }
            }
        }

        public CarteseanCoordinate(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            R = 0.0;
            Ra = 0.0;
            Flag = false;
        }

        public double this[int index]
        {
            get
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return (index == 0 ? X : (index == 1 ? Y : Z));
            }
            set
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (index == 0)
                {
                    X = value;
                }
                else switch (index)
                {
                    case 1:
                        Y = value;
                        break;
                    default:
                        Z = value;
                        break;
                }
            }
        }


        #region Operator overloads ...
        /// <summary>
        /// Compares the two specified sets of Axis positions.
        /// </summary>
        public static bool operator ==(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
        {
            return (pos1.X == pos2.X && pos1.Y == pos2.Y && pos1.Z == pos2.Z && pos1.R == pos2.R && pos1.Ra == pos2.Ra && pos1.Flag == pos2.Flag);
        }

        public static bool operator !=(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
        {
            return !(pos1 == pos2);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj)
        {
            return (obj is CarteseanCoordinate coordinate
                    && this == coordinate);
        }

        public static CarteseanCoordinate operator -(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
        {
            return new CarteseanCoordinate(pos1.X - pos2.X, pos1.Y - pos2.Y, pos1.Z - pos2.Z);
        }

        public static CarteseanCoordinate operator +(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
        {
            return new CarteseanCoordinate(pos1.X + pos2.X, pos1.Y + pos2.Y, pos1.Z + pos2.Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
        #endregion

        
    }

}
