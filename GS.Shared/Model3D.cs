/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.DeviceInterface;
using System;
using System.Reflection;

namespace GS.Shared
{
    public static class Model3D
    {
        private static readonly string DirectoryPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\Models\";
        public static string GetModelFile(Model3DType modelType, String suffix = "")
        {
            // modelType is strongly typed enum so ToString() will succeed 
            string gpModel = modelType.ToString();
            var filePath = System.IO.Path.Combine(DirectoryPath ?? throw new InvalidOperationException(),
                gpModel+ suffix + ".obj");
            var file = new Uri(filePath).LocalPath;
            if (!System.IO.File.Exists(file)) file = String.Empty;                
            return file;
        }
        public static string GetCompassFile(bool southernHemisphere, bool altAz)
        {
            const string compassN = @"CompassN.png";
            const string compassS = @"CompassS.png";
            var compassFile = southernHemisphere && !altAz ? compassS : compassN;
            var filePath = System.IO.Path.Combine(DirectoryPath ?? throw new InvalidOperationException(), compassFile);
            var file = new Uri(filePath).LocalPath;
            if (!System.IO.File.Exists(file)) file = String.Empty;
            return file;
        }

        /// <summary>
        /// Calculates the rotated axes for a model based on the specified mount type, alignment mode, and other
        /// parameters.
        /// </summary>
        /// <remarks>The method applies different rotation logic based on the combination of the alignment
        /// mode, mount type, and hemisphere. For example, in <see cref="AlignmentModes.algPolar"/> mode with a
        /// "SkyWatcher" mount, the rotation logic differs  depending on whether the primary OTA is
        /// east-facing.</remarks>
        /// <param name="mountType">The type of mount being used. Supported values include "Simulator" and "SkyWatcher".</param>
        /// <param name="ax">The initial X-axis value to be rotated in degrees.</param>
        /// <param name="ay">The initial Y-axis value to be rotated in degrees.</param>
        /// <param name="southernHemisphere">A value indicating whether the model is located in the southern hemisphere</param>
        /// <param name="alignmentMode">The alignment mode to use for the rotation. Supported values are <see cref="AlignmentModes.algAltAz"/>, 
        /// <see cref="AlignmentModes.algPolar"/>, and <see cref="AlignmentModes.algGermanPolar"/>.</param>
        /// <param name="polarMode">A value indicating whether the polar alignment is east-facing. This parameter is relevant for  certain mount
        /// types and alignment modes.</param>
        /// <returns>An array of double values representing the rotated X and Y axes</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="mountType"/> is not recognized or unsupported for
        /// the specified <paramref name="alignmentMode"/>.</exception>
        public static double[] RotateModel(string mountType, double ax, double ay, bool southernHemisphere, AlignmentModes alignmentMode, int polarMode)
        {
            var axes = new[] { 0.0, 0.0 };
            switch (alignmentMode)
            {
                case AlignmentModes.algAltAz:
                    axes[0] = Math.Round(ax, 3);
                    axes[1] = Math.Round(ay * -1.0, 3);
                    break;
                case AlignmentModes.algPolar:
                    switch (mountType)
                    {
                        case "Simulator":
                            axes[0] = Math.Round(ax, 3);
                            axes[1] = Math.Round(ay * -1.0, 3);
                            break;
                        case "SkyWatcher":
                            if (polarMode == 0)
                            {
                                axes[0] = Math.Round(ax, 3);
                                axes[1] = Math.Round(ay - 180, 3);
                            }
                            else
                            {
                                axes[0] = Math.Round(ax, 3);
                                axes[1] = Math.Round(ay * -1.0, 3);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case AlignmentModes.algGermanPolar:
                    switch (mountType)
                    {
                        case "Simulator":
                            axes[0] = Math.Round(ax, 3);
                            axes[1] = southernHemisphere ? Math.Round(ay - 180, 3) : Math.Round(ay * -1.0, 3);
                            break;
                        case "SkyWatcher":
                            axes[0] = Math.Round(ax, 3);
                            axes[1] = Math.Round(ay - 180, 3);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    break;
            }
            return axes;
        }
    }

    public enum Model3DType
    {
        Default = 0,
        Reflector = 1,
        Refractor = 2,
        SchmidtCassegrain = 3,
        RitcheyChretien = 4,
        RitcheyChretienTruss = 5,
        DualTelescope = 6
    }
}
