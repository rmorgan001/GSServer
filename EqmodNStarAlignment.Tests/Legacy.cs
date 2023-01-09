/*
MIT License

Copyright (c) 2017 Phil Crompton

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

   Portions
   Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EqmodNStarAlignment.Tests
{

    public static class Legacy
    {
        #region Astro32.dll ...
        ///
        /// =================
        /// LIBRARY FUNCTIONS
        /// =================
        ///
        /// NOTES:
        ///
        /// (1) For whatever reason, the authors of the original C functions chose
        ///     to pass back and forth via parameters only for most functions.
        ///
        /// (2) The descriptive comments below were lifted straight out of the C
        ///     functions. Some variables are listed with the C dereferening ///*///.
        ///     Note that these are passed ByRef in the declarations, then forget
        ///     about the ///*///.
        ///
        /// (3) Modified Julian Dates (number of days elapsed since 1900 jan 0.5,)
        ///     are used for most times. Several functions are provided for converting
        ///     between mjd and other time systems (C runtime, VB, Win32).
        ///
        ///
        /// given latitude (n+, radians), lat, altitude (up+, radians), alt, and
        /// azimuth (angle around to the east from north+, radians),
        /// return hour angle (radians), ha, and declination (radians), dec.
        ///
        [DllImport("astro32.dll")]
        public static extern void aa_hadec(double lat, double alt, double az, ref double ha, ref double dec);
        // Declare Sub aa_hadec Lib "astro32" (ByVal lat As Double, ByVal Alt As Double, ByVal Az As Double, ByRef ha As Double, ByRef DEC As Double)

        ///
        /// given latitude (n+, radians), lat, hour angle (radians), ha, and declination
        ///   (radians), dec, return altitude (up+, radians), alt, and azimuth (angle
        ///   round to the east from north+, radians),
        ///

        [DllImport("astro32.dll")]
        public static extern void hadec_aa(double lat, double ha, double dec, ref double alt, ref double az);
        // Declare Sub hadec_aa Lib "astro32" (ByVal lat As Double, ByVal ha As Double, ByVal DEC As Double, ByRef Alt As Double, ByRef Az As Double)

        #endregion
    }
}
