/* Copyright(C) 2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace GS.Shared
{
    public static class GSFile
    {
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);

        public static string GetFileName(string name, string dir, string filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*")
        {
            var openFileDialog = new OpenFileDialog
            {
                FileName = name,
                InitialDirectory = dir,
                Filter = filter,
                Multiselect = false,
            };
            return openFileDialog.ShowDialog() != true ? null : openFileDialog.FileName;
        }

        /// <summary>
        /// Send entries to a file async
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="message"></param>
        /// <param name="append"></param>
        public static async void FileWriteAsync(string filePath, string message, bool append = true)
        {
            try
            {
                await _lockFile.WaitAsync();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _lockFile.Release();
            }
        }
    }
}
