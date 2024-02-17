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
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace GS.Shared
{
    public static class GSFile
    {
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);

        /// <summary>
        /// Uses the file dialog to retrieve a file path
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dir"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
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
        /// uses the Folder Browser Dialog to retrieve a folder path
        /// </summary>
        /// <remarks>needs system.windows.forms</remarks>
        /// <param name="startDir"></param>
        /// <param name="showNew"></param>
        /// <returns>Folder Path</returns>
        public static string GetFolderName(string startDir, bool showNew = false)
        {
            using (var folderDlg = new FolderBrowserDialog())
            {
                folderDlg.ShowNewFolderButton = showNew;
                folderDlg.SelectedPath = startDir;
                return folderDlg.ShowDialog() != DialogResult.OK ? null : folderDlg.SelectedPath;
            }
        }
        /// <summary>
        /// saves logging path or a default path to settings
        /// </summary>
        /// <param name="path"></param>
        public static void SaveLogPath(string path)
        {
            Settings.LogPath = Directory.Exists(path) ? path : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GSServer");
        }
        /// <summary>
        /// Get the logging path or a default path from settings
        /// </summary>
        /// <returns>logPath</returns>
        public static string GetLogPath()
        {
            var path = Settings.LogPath;
            return Directory.Exists(path) ? path : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"GSServer");
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
