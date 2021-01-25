using System;

namespace NStarAlignment.DataTypes
{
    [Serializable]
    public struct Matrix
    {
        private double[][] _element; //2D array of elements

        public static Matrix CreateInstance()
        {
            Matrix result = new Matrix();
            result.Initialise(3, 3);
            return result;
        }

        public static Matrix CreateInstance(int[] size)
        {
            return CreateInstance(size[0], size[1]);
        }

        public static Matrix CreateInstance(int rows, int columns)
        {
            Matrix result = new Matrix();
            result.Initialise(rows, columns);
            return result;
        }

        public static Matrix CreateInstance(double[][] values)
        {
            Matrix result = new Matrix();
            result.Initialise(values);
            return result;
        }


        private void Initialise(int rows, int cols)
        {
            _element = new double[rows][];
            for (int i = 0; i < rows; i++)
            {
                _element[i] = new double[cols];
            }
        }

        private void Initialise(double[][] values)
        {
            _element = new double[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                _element[i] = new double[values[i].Length];
                for (int j = 0; j < values[i].Length; j++)
                {
                    _element[i][j] = values[i][j];
                }
            }
        }

        public double this[int i, int j]
        {
            get => _element[i][j];

            set => _element[i][j] = value;
        }

        public double[] this[int i]
        {
            get => _element[i];

            set => _element[i] = value;
        }

        public int Length
        {
            get
            {
                return _element.GetLength(0);
            }
        }

        public static Matrix operator *(Matrix matrix1, Matrix matrix2)
        {
            return MatrixProduct(matrix1, matrix2);
        }

        public override bool Equals(object obj)
        {
            return (obj is Matrix matrix
                    && this == matrix);
        }
        public static bool operator ==(Matrix matrix1, Matrix matrix2)
        {
            /* Just in case of rounding errors... */
            return MatrixAreEqual(matrix1, matrix2, 0.0);
        }

        public bool IsEqualTo(Matrix matrix1, double tolerance)
        {
            return MatrixAreEqual(this, matrix1, tolerance);
        }

        public static bool operator !=(Matrix matrix1, Matrix matrix2)
        {
            return !(matrix1 == matrix2);
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                // Suitable nullity checks etc, of course :)
                for (int i = 0; i < this.Length; i++)
                {
                    for (int j = 0; j < this[i].Length; j++)
                    {
                        hash = 23 + this[i, j].GetHashCode();
                    }
                }
                return hash;
            }
        }


        public Matrix Transpose()
        {
            int w = this.Length;
            int h = this[0].Length;

            Matrix result = Matrix.CreateInstance(w, h);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    result[j, i] = this[i, j];
                }
            }

            return result;
        }


        public Matrix Invert()
        {
            return MatrixInverse(this);
        }


        public new string ToString()
        {
            return MatrixAsString(this, 4);
        }


        #region James McCaffrey code MSDN magazine ...
        // https://jamesmccaffrey.wordpress.com/2015/03/06/inverting-a-matrix-using-c/
        static double[][] MatrixCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        // --------------------------------------------------

        static Matrix MatrixRandom(int rows, int cols, double minVal, double maxVal, int seed)
        {
            // return a matrix with random values
            Random ran = new Random(seed);
            Matrix result = Matrix.CreateInstance(rows, cols);
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    result[i][j] = (maxVal - minVal) *
                      ran.NextDouble() + minVal;
            return result;
        }

        // --------------------------------------------------

        static Matrix MatrixIdentity(int n)
        {
            // return an n x n Identity matrix
            Matrix result = Matrix.CreateInstance(n, n);
            for (int i = 0; i < n; ++i)
                result[i][i] = 1.0;

            return result;
        }

        // --------------------------------------------------

        static string MatrixAsString(Matrix matrix, int dec)
        {
            string s = "";
            for (int i = 0; i < matrix.Length; ++i)
            {
                for (int j = 0; j < matrix[i].Length; ++j)
                    s += matrix[i][j].ToString("F" + dec).PadLeft(8) + " ";
                s += Environment.NewLine;
            }
            return s;
        }

        // --------------------------------------------------

        static bool MatrixAreEqual(Matrix matrixA, Matrix matrixB, double epsilon)
        {
            // true if all values in matrixA == values in matrixB
            int aRows = matrixA.Length; int aCols = matrixA[0].Length;
            int bRows = matrixB.Length; int bCols = matrixB[0].Length;
            if (aRows != bRows || aCols != bCols)
                throw new Exception("Non-conformable matrices");

            for (int i = 0; i < aRows; ++i) // each row of A and B
                for (int j = 0; j < aCols; ++j) // each col of A and B
                                                //if (matrixA[i][j] != matrixB[i][j])
                    if (Math.Abs(matrixA[i][j] - matrixB[i][j]) > epsilon)
                        return false;
            return true;
        }

        // --------------------------------------------------

        static Matrix MatrixProduct(Matrix matrixA, Matrix matrixB)
        {
            int aRows = matrixA.Length; int aCols = matrixA[0].Length;
            int bRows = matrixB.Length; int bCols = matrixB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");

            Matrix result = Matrix.CreateInstance(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use k less-than bRows
                        result[i][j] += matrixA[i][k] * matrixB[k][j];
            return result;
        }

        // --------------------------------------------------

        static double[] MatrixVectorProduct(Matrix matrix, double[] vector)
        {
            // result of multiplying an n x m matrix by a m x 1 
            // column vector (yielding an n x 1 column vector)
            int mRows = matrix.Length; int mCols = matrix[0].Length;
            int vRows = vector.Length;
            if (mCols != vRows)
                throw new Exception("Non-conformable matrix and vector");
            double[] result = new double[mRows];
            for (int i = 0; i < mRows; ++i)
                for (int j = 0; j < mCols; ++j)
                    result[i] += matrix[i][j] * vector[j];
            return result;
        }

        // --------------------------------------------------

        static Matrix MatrixDecompose(Matrix matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.Length;
            int cols = matrix[0].Length; // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            Matrix result = MatrixDuplicate(matrix);

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                double colMax = Math.Abs(result[j][j]); // find largest val in col
                int pRow = j;

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i][j]) > colMax)
                    {
                        colMax = Math.Abs(result[i][j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    double[] rowPtr = result[pRow];
                    result[pRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j][j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row][j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        throw new Exception("Cannot use Doolittle's method");

                    // swap rows so 0.0 no longer on diagonal
                    double[] rowPtr = result[goodRow];
                    result[goodRow] = result[j];
                    result[j] = rowPtr;

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20) 
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i][j] /= result[j][j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i][k] -= result[i][j] * result[j][k];
                    }
                }


            } // main j column loop

            return result;
        } // MatrixDecompose


        // --------------------------------------------------


        static Matrix MatrixInverse(Matrix matrix)
        {
            int n = matrix.Length;
            Matrix result = MatrixDuplicate(matrix);

            Matrix lum = MatrixDecompose(matrix, out var perm, out _);
            if (lum == null)
                throw new Exception("Unable to compute inverse");

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }

                double[] x = HelperSolve(lum, b); // 

                for (int j = 0; j < n; ++j)
                    result[j][i] = x[j];
            }
            return result;
        }


        // --------------------------------------------------

        private double MatrixDeterminant(Matrix matrix)
        {
            int[] perm;
            int toggle;
            Matrix lum = MatrixDecompose(matrix, out perm, out toggle);
            if (lum == null)
                throw new Exception("Unable to compute MatrixDeterminant");
            double result = toggle;
            for (int i = 0; i < lum.Length; ++i)
                result *= lum[i][i];
            return result;
        }


        // --------------------------------------------------

        static double[] HelperSolve(Matrix luMatrix, double[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.Length;
            double[] x = new double[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1][n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum / luMatrix[i][i];
            }

            return x;
        }

        // --------------------------------------------------

        static double[] SystemSolve(Matrix A, double[] b)
        {
            // Solve Ax = b
            int n = A.Length;

            // 1. decompose A
            Matrix luMatrix = MatrixDecompose(A, out var perm, out _);
            if (luMatrix == null)
                return null;

            // 2. permute b according to perm[] into bp
            double[] bp = new double[b.Length];
            for (int i = 0; i < n; ++i)
                bp[i] = b[perm[i]];

            // 3. call helper
            double[] x = HelperSolve(luMatrix, bp);
            return x;
        } // SystemSolve

        // --------------------------------------------------

        static Matrix MatrixDuplicate(Matrix matrix)
        {
            // allocates/creates a duplicate of a matrix.
            Matrix result = Matrix.CreateInstance(matrix.Length, matrix[0].Length);
            for (int i = 0; i < matrix.Length; ++i) // copy the values
                for (int j = 0; j < matrix[i].Length; ++j)
                    result[i][j] = matrix[i][j];
            return result;
        }

        // --------------------------------------------------
        #endregion
    }
}
