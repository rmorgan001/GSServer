using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    internal class TriangleDataHolder
    {
        public double Distance { get; set; }
        public int AlignmentPointIndex { get; set; }
        public CarteseanCoordinate Coordinate { get; set; } = new CarteseanCoordinate();
    }
}
