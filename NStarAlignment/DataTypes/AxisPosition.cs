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
using Newtonsoft.Json;

namespace NStarAlignment.DataTypes
{

    /// <summary>
    /// A structure to represent telescope mount axis positions
    /// </summary>
    public struct AxisPosition
    {
        private Angle _raAxis;
        private Angle _decAxis;

        [JsonProperty]
        public Angle RaAxis
        {
            get => _raAxis;
            private set => _raAxis = value;
        }

        [JsonProperty]
        public Angle DecAxis
        {
            get => _decAxis;
            private set => _decAxis = value;
        }


        public AxisPosition(double[] axes) : this(axes[0], axes[1])
        { }

        /// <summary>
        /// Create a new axis position
        /// </summary>
        /// <param name="ra">RA axis angle in degrees</param>
        /// <param name="dec">Dec axis angle in degrees</param>
        public AxisPosition(double ra, double dec)
        {
                _raAxis = ra;
                _decAxis = dec;
        }


        public AxisPosition(string axisPositions)
        {
            var positions = axisPositions.Split('|');
            try
            {
                _raAxis = double.Parse(positions[0]);
                _decAxis = double.Parse(positions[1]);
            }
            catch
            {
                throw new ArgumentException("Badly formed axis position string");
            }
        }


        public static implicit operator double[](AxisPosition axes)
        {
            return new[]{axes[0], axes[1]};
        }

        public static implicit operator AxisPosition(double[] axes)
        {
            return new AxisPosition(axes);
        }

        /// <summary>
        /// The axis position in degrees
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double this[int index]
        {
            get
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return (index == 0 ? _raAxis.Value : _decAxis.Value);
            }
            set
            {
                if (index < 0 || index > 2)
                {
                    throw new ArgumentOutOfRangeException();
                }
                if (index == 0)
                {
                    _raAxis = value;
                }
                else
                {
                    _decAxis = value;
                }
            }
        }

        /// <summary>
        /// Compares the two specified sets of Axis positions.
        /// </summary>
        public static bool operator ==(AxisPosition pos1, AxisPosition pos2)
        {
            return (pos1.RaAxis == pos2.RaAxis && pos1.DecAxis == pos2.DecAxis);
        }

        public static bool operator !=(AxisPosition pos1, AxisPosition pos2)
        {
            return !(pos1 == pos2);
        }

        public static AxisPosition operator -(AxisPosition pos1, AxisPosition pos2)
        {
            return new AxisPosition(pos1.RaAxis.Value - pos2.RaAxis.Value, pos1.DecAxis.Value - pos2.DecAxis.Value);
        }

        public static AxisPosition operator +(AxisPosition pos1, AxisPosition pos2)
        {
            return new AxisPosition(pos1.RaAxis.Value + pos2.RaAxis.Value, pos1.DecAxis.Value + pos2.DecAxis.Value);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + _raAxis.GetHashCode();
                hash = hash * 23 + _decAxis.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is AxisPosition position
                    && this == position);
        }


        public bool Equals(AxisPosition obj, double toleranceDegrees)
        {
            var deltaRa = Math.Abs(obj.RaAxis.Value - RaAxis.Value);
            deltaRa = (deltaRa + 180) % 360 - 180;
            var deltaDec = Math.Abs(obj.DecAxis.Value - DecAxis.Value);
            deltaDec = (deltaDec + 180) % 360 - 180;
            return (deltaRa <= toleranceDegrees
               && deltaDec <= toleranceDegrees);
        }

        public override string ToString()
        {
            return $"{Math.Round(_raAxis.Value, 2)}°/{Math.Round(_decAxis.Value, 2)}°";
        }

        /// <summary>
        /// Flip and axis position as would happen on a telescope doing a meridian flip.
        /// </summary>
        /// <returns></returns>
        public AxisPosition Flip()
        {
            var d = new[] { 0.0, 0.0 };
            if (RaAxis > 90)
            {
                d[0] = RaAxis - 180;
                d[1] = 180 - DecAxis;
            }
            else
            {
                d[0] = RaAxis + 180;
                d[1] = 180 - DecAxis;
            }
            return new AxisPosition(d);
        }

        public Angle IncludedAngleTo(AxisPosition axisPosition2)
        {
            double piby2 = Math.PI * 0.5;
            // Using the law of Cosines
            double c = (Math.Cos(piby2 - this.DecAxis.Radians) * Math.Cos(piby2 - axisPosition2.DecAxis.Radians))
                       + (Math.Sin(piby2 - this.DecAxis.Radians) * Math.Sin(piby2 - axisPosition2.DecAxis.Radians) *
                          Math.Cos(this.RaAxis.Radians - axisPosition2.RaAxis.Radians));
            Angle result = new Angle(Math.Abs(Math.Acos(c)), true);
            return result;

        }

    }
}
