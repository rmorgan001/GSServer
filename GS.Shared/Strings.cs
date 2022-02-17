/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Text.RegularExpressions;

namespace GS.Shared
{
    public static class Strings
    {
        /// <summary>
        /// Get text between two characters, index
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="strStart"></param>
        /// <param name="strEnd"></param>
        /// <returns></returns>
        public static string GetTxtBetween(string strSource, string strStart, string strEnd)
        {
            if (!strSource.Contains(strStart) || !strSource.Contains(strEnd)) return "";
            var start = strSource.IndexOf(strStart, 0, StringComparison.Ordinal) + strStart.Length;
            var end = strSource.IndexOf(strEnd, start, StringComparison.Ordinal);
            return strSource.Substring(start, end - start);
        }

        /// <summary>
        /// Get text between two words, Regex
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="strStart"></param>
        /// <param name="strEnd"></param>
        /// <returns></returns>
        public static string GetTextBetween(string strSource, string strStart, string strEnd)
        {
            return
                Regex.Match(strSource, $@"{strStart}\s(?<words>[\w\s]+)\s{strEnd}",
                    RegexOptions.IgnoreCase).Groups["words"].Value;
        }

        /// <summary>
        /// Pulls a number from a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int? GetNumberFromString(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            var numbers = Regex.Split(str, @"\D+");
            foreach (var value in numbers)
            {
                if (string.IsNullOrEmpty(value)) continue;
                var ok = int.TryParse(value.Trim(), out var i);
                if (ok) { return i; }
            }
            return null;
        }

        /// <summary>
        /// Converts a Mount received hex string to type long
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long StringToLong(string str)
        {
            long value = 0;
            for (var i = 1; i + 1 < str.Length; i += 2)
            {
                value += (long)(int.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier) * Math.Pow(16, i - 1));
            }
            return value;
        }

    }
}
