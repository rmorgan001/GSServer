using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    public struct Matrix
    {
        private double[,] _element; //2D array of elements
        public static Matrix CreateInstance()
        {
            Matrix result = new Matrix
            {
                _element = new double[3, 3]
            };
            return result;
        }

        public double Determinant3X3()
        {
            double det = this[0, 0] * ((this[1, 1] * this[2, 2]) - (this[2, 1] * this[1, 2]))
            - (this[0, 1] * ((this[1, 0] * this[2, 2]) - (this[2, 0] * this[1, 2])))
            + (this[0, 2] * ((this[1, 0] * this[2, 1]) - (this[2, 0] * this[1, 1])));
            return det;
        }

        public Matrix Inverse3X3()
        {
            double det = this[0, 0] * ((this[1, 1] * this[2, 2]) - (this[2, 1] * this[1, 2]))
            - (this[0, 1] * ((this[1, 0] * this[2, 2]) - (this[2, 0] * this[1, 2])))
            + (this[0, 2] * ((this[1, 0] * this[2, 1]) - (this[2, 0] * this[1, 1])));

            if (!(det != 0))
            {
                throw new Exception("999, AssembleMatrix, Cannot invert matrix with Determinant = 0");
            }

            Matrix inverse = CreateInstance();
            inverse[0, 0] = ((this[1, 1] * this[2, 2]) - (this[2, 1] * this[1, 2])) / det;
            inverse[0, 1] = ((this[0, 2] * this[2, 1]) - (this[0, 1] * this[2, 2])) / det;
            inverse[0, 2] = ((this[0, 1] * this[1, 2]) - (this[1, 1] * this[0, 2])) / det;
            inverse[1, 0] = ((this[1, 2] * this[2, 0]) - (this[2, 2] * this[1, 0])) / det;
            inverse[1, 1] = ((this[0, 0] * this[2, 2]) - (this[2, 0] * this[0, 2])) / det;
            inverse[1, 2] = ((this[0, 2] * this[1, 0]) - (this[1, 2] * this[0, 0])) / det;
            inverse[2, 0] = ((this[1, 0] * this[2, 1]) - (this[2, 0] * this[1, 1])) / det;
            inverse[2, 1] = ((this[0, 1] * this[2, 0]) - (this[2, 1] * this[0, 0])) / det;
            inverse[2, 2] = ((this[0, 0] * this[1, 1]) - (this[1, 0] * this[0, 1])) / det;
            return inverse;
        }

        public Matrix Inverse2X2()
        {

            double det = (this[0, 0] * this[1, 1]) - (this[0, 1] * this[1, 0]);

            // Make sure Determinant is NON ZERO
            if (det == 0.0)
            {
                throw new Exception("999, AssembleMatrix, Cannot invert matrix with Determinant = 0");
            }

            Matrix inverse = CreateInstance();
            inverse[0, 0] = (this[1, 1]) / det;
            inverse[0, 1] = (-this[0, 1]) / det;
            inverse[1, 0] = (-this[1, 0]) / det;
            inverse[1, 1] = (this[0, 0]) / det;
            return inverse;

        }



        public double this[int i, int j]
        {
            get => _element[i, j];

            set => _element[i, j] = value;
        }
    }
}
