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
using System;
using System.Reflection;

namespace GS.Shared
{
    public static class Model3D
    {
        private static readonly string DirectoryPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\Models\";
        public static string GetModelFile(Model3DType modelType, String altAz = "")
        {
            string gpModel;
            switch (modelType)
            {
                case Model3DType.Default:
                    gpModel = @"Default.obj";
                    break;
                case Model3DType.Reflector:
                    gpModel = @"Reflector.obj";
                    break;
                case Model3DType.Refractor:
                    gpModel = @"Refractor.obj";
                    break;
                case Model3DType.SchmidtCassegrain:
                    gpModel = @"SchmidtCassegrain.obj";
                    break;
                case Model3DType.RitcheyChretien:
                    gpModel = @"RitcheyChretien.obj";
                    break;
                case Model3DType.RitcheyChretienTruss:
                    gpModel = @"RitcheyChretienTruss.obj";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modelType), modelType, null);
            }
            var filePath = System.IO.Path.Combine(DirectoryPath ?? throw new InvalidOperationException(),
                gpModel.Replace(".obj", altAz + ".obj"));
            var file = new Uri(filePath).LocalPath;
            return file;
        }
        public static string GetCompassFile(bool southernHemisphere, bool altAz)
        {
            const string compassN = @"CompassN.png";
            const string compassS = @"CompassS.png";
            var compassFile = southernHemisphere && !altAz ? compassS : compassN;
            var filePath = System.IO.Path.Combine(DirectoryPath ?? throw new InvalidOperationException(), compassFile);
            var file = new Uri(filePath).LocalPath;
            return file;
        }
        public static double[] RotateModel(string mountType, double ax, double ay, bool southernHemisphere, bool altAz)
        {
            var axes = new[] { 0.0, 0.0 };
            if (altAz)
            {
                axes[0] = Math.Round(ax, 3);
                axes[1] = Math.Round(ay * -1.0, 3);
            }
            else
            {
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
        RitcheyChretienTruss = 5
    }
}
