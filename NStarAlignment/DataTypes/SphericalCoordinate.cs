using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    public struct SphericalCoordinate
    {
        public Angle X;
        public Angle Y;
        public bool WeightsDown;

        #region Operator overloads ...
        /// <summary>
        /// Compares the two specified sets of Axis positions.
        /// </summary>
        public static bool operator ==(SphericalCoordinate pos1, SphericalCoordinate pos2)
        {
            return (pos1.X == pos2.X && pos1.Y == pos2.Y && pos1.WeightsDown == pos2.WeightsDown);
        }

        public static bool operator !=(SphericalCoordinate pos1, SphericalCoordinate pos2)
        {
            return !(pos1 == pos2);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                int wd = (WeightsDown ? 1 : 0);
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + wd.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj)
        {
            return (obj is SphericalCoordinate coordinate
                    && this == coordinate);
        }
        #endregion
    }
}
