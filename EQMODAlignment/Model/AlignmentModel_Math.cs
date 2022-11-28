/* Copyright(C) 2020  Phil Crompton (phil@lunaticsoftware.org)
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
//---------------------------------------------------------------------
// based on original work
// Copyright © 2006 Raymund Sarmiento
//
// Permission is hereby granted to use this Software for any purpose
// including combining with commercial products, creating derivative
// works, and redistribution of source or binary code, without
// limitation or consideration. Any redistributed copies of this
// Software must include the above Copyright Notice.
//
// THIS SOFTWARE IS PROVIDED "AS IS". THE AUTHOR OF THIS CODE MAKES NO
// WARRANTIES REGARDING THIS SOFTWARE, EXPRESS OR IMPLIED, AS TO ITS
// SUITABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
//---------------------------------------------------------------------
//
// EQMATH.bas - Math functions for EQMOD ASCOM RADECALTAZ computations
//
//
// Written:  07-Oct-06   Raymund Sarmiento
//
// Edits:
//
// When      Who     What
// --------- ---     --------------------------------------------------
// 04-Nov-06 rcs     Initial edit for EQ Mount Driver Function Prototype
// 20-Nov-06 rcs     wrote a new function for now_lst that will generate millisecond
//                   granularity
// 21-Nov-06 rcs     Append RA GOTO Compensation to minimize discrepancy
// 19-Mar-07 rcs     Initial Edit for Three star alignment
// 05-Apr-07 rcs     Add MAXSYNC
// 08-Apr-07 rcs     N-star implementation
// 13-Jun-20 jpc	 Copied into NPoint code base and converted from vb6 to C#
//---------------------------------------------------------------------
//
//
//  DISCLAIMER:

//  You can use the information on this site COMPLETELY AT YOUR OWN RISK.
//  The modification steps and other information on this site is provided
//  to you "AS IS" and WITHOUT WARRANTY OF ANY KIND, express, statutory,
//  implied or otherwise, including without limitation any warranty of
//  merchantability or fitness for any particular or intended purpose.
//  In no event the author will  be liable for any direct, indirect,
//  punitive, special, incidental or consequential damages or loss of any
//  kind whether or not the author  has been advised of the possibility
//  of such loss.

using EqmodNStarAlignment.DataTypes;
using EqmodNStarAlignment.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EqmodNStarAlignment.Model
{
    partial class AlignmentModel
    {

        //private const double DEG_RAD = 0.0174532925d;
        //private const double RAD_DEG = 57.2957795d;
        //private const double HRS_RAD = 0.2617993881d;
        //private const double RAD_HRS = 3.81971863d;

        //private const double SID_RATE = 15.041067d;
        //private const double SOL_RATE = 15;
        //private const double LUN_RATE = 14.511415d;

        //private const double gEMUL_RATE = 20.98d; // 0.2 * 9024000/( (23*60*60)+(56*60)+4)
        //                                          // 0.2 = 200ms

        //private const double gEMUL_RATE2 = 104.730403903004d; // (9024000/86164.0905)

        //// 104.73040390300411747513310083625

        //private const double gARCSECSTEP = 0.144d; // .144 arcesconds / step

        //// Iterative GOTO Constants
        ////Public Const NUM_SLEW_RETRIES As Long = 5                   ' Iterative MAX retries
        //private const double gRA_Allowed_diff = 10; // Iterative Slew minimum difference


        //// Home Position of the mount (pointing at NCP/SCP)

        //private const double RAEncoder_Home_pos = 0x800000; // Start at 0 Hour
        //private const double DECEncoder_Home_pos = 0xA26C80; // Start at 90 Degree position

        //private const double gRAEncoder_Zero_pos = 0x800000; // ENCODER 0 Hour initial position
        //private const double gDECEncoder_Zero_pos = 0x800000; // ENCODER 0 Degree Initial position

        //private const double gDefault_step = 9024000; // Total Encoder count (EQ5/6)



        ////Public Const EQ_MAXSYNC As Double = &H111700

        //// Public Const EQ_MAXSYNC_Const As Double = &H88B80                 ' Allow a 45 degree discrepancy


        //private const double EQ_MAXSYNC_Const = 0x113640; // Allow a 45 degree discrepancy

        ////------------------------------------------------------------------------------------------------

        //// Define all Global Variables


        //public static double gXshift = 0;
        //public static double gYshift = 0;
        //public static double gXmouse = 0;
        //public static double gYmouse = 0;


        //public static double gEQ_MAXSYNC = 0; // Max Sync Diff
        //public static double gSiderealRate = 0; // Sidereal rate arcsecs/sec
        //public static double gMount_Ver = 0; // Mount Version
        //public static int gMount_Features = 0; // Mount Features

        //public static double gRA_LastRate = 0; // Last PEC Rate
        //public static int gpl_interval = 0; // Pulseguide Interval

        //public static double eqres = 0;
        //public static double gTot_step = 0; // Total Common RA-Encoder Steps
        //public static double gTot_RA = 0; // Total RA Encoder Steps
        //public static double gTot_DEC = 0; // Total DEC Encoder Steps
        //public static double gRAWormSteps = 0; // Steps per RA worm revolution
        //public static double gRAWormPeriod = 0; // Period of RA worm revolution
        //public static double gDECWormSteps = 0; // Steps per DEC worm revolution
        //public static double gDECWormPeriod = 0; // Period of DEC worm revolution

        //public static double gLatitude = 0; // Site Latitude
        //public static double gLongitude = 0; // Site Longitude
        //public static double gElevation = 0; // Site Elevation
        //public static int gHemisphere = 0;

        //public static double gDECEncoder_Home_pos = 0; // DEC HomePos - Varies with different mounts

        //public static double gRA_Encoder = 0; // RA Current Polled RA Encoder value
        //public static double gDec_Encoder = 0; // DEC Current Polled Encoder value
        //public static double gRA_Hours = 0; // RA Encoder to Hour position
        //public static double gDec_Degrees = 0; // DEC Encoder to Degree position Ranged to -90 to 90
        //public static double gDec_DegNoAdjust = 0; // DEC Encoder to actual degree position
        //public static double gRAStatus = 0; // RA Polled Motor Status
        //public static bool gRAStatus_slew = false; // RA motor tracking poll status
        //public static double gDECStatus = 0; // DEC Polloed motor status

        //public static double gRA_Limit_East = 0; // RA Limit at East Side
        //public static double gRA_Limit_West = 0; // RA Limit at West Side

        //public static double gRA1Star = 0; // Initial RA Alignment adjustment
        //public static double gDEC1Star = 0; // Initial DEC Alignment adjustment

        //public static double gRASync01 = 0; // Initial RA sync adjustment
        //public static double gDECSync01 = 0; // Initial DEC sync adjustment

        //public static double gRA = 0;
        //public static double gDec = 0;
        //public static double gAlt = 0;
        //public static double gAz = 0;
        //public static double gha = 0;
        //public static double gSOP = 0;

        //public static string gPort = "";
        //public static int gBaud = 0;
        //public static int gTimeout = 0;
        //public static int gRetry = 0;

        //public static int gTrackingStatus = 0;
        //public static bool gSlewStatus = false;

        //public static double gRAMoveAxis_Rate = 0;
        //public static double gDECMoveAxis_Rate = 0;


        //// Added for emulated Stepper Counters
        //public static double gEmulRA = 0;
        //public static double gEmulDEC = 0;
        //public static bool gEmulOneShot = false;
        //public static bool gEmulNudge = false;

        //public static double gCurrent_time = 0;
        //public static double gLast_time = 0;
        //public static double gEmulRA_Init = 0;

        //public enum PierSide2
        //{
        //    pierUnknown2 = -1,
        //    PierEast2 = 0,
        //    PierWest2 = 1
        //}

        //public static PierSide2 gSideofPier = PierSide2.PierEast2;


        //public static int gRAEncoderPolarHomeGoto = 0;
        //public static int gDECEncoderPolarHomeGoto = 0;
        //public static int gRAEncoderUNPark = 0;
        //public static int gDECEncoderUNPark = 0;
        //public static int gRAEncoderPark = 0;
        //public static int gDECEncoderPark = 0;
        //public static int gRAEncoderlastpos = 0;
        //public static int gDECEncoderlastpos = 0;
        //public static int gEQparkstatus = 0;

        //public static int gEQRAPulseDuration = 0;
        //public static int gEQDECPulseDuration = 0;
        //public static int gEQRAPulseEnd = 0;
        //public static int gEQDECPulseEnd = 0;
        //public static int gEQDECPulseStart = 0;
        //public static int gEQRAPulseStart = 0;
        //public static bool gEQPulsetimerflag = false;

        //public static double gEQTimeDelta = 0;



        //// Public variables for Custom Tracking rates

        //public static double gDeclinationRate = 0;
        //public static double gRightAscensionRate = 0;


        //// Public Variables for Spiral Slew

        //public static int gSPIRAL_JUMP = 0;
        //public static double gDeclination_Start = 0;
        //public static double gRightAscension_Start = 0;
        //public static double gDeclination_Dir = 0;
        //public static double gRightAscension_Dir = 0;
        //public static int gDeclination_Len = 0;
        //public static int gRightAscension_Len = 0;

        //public static double gSpiral_AxisFlag = 0;



        //// Public variables for debugging

        //public static double gAffine1 = 0;
        //public static double gAffine2 = 0;
        //public static double gAffine3 = 0;

        //public static double gTaki1 = 0;
        //public static double gTaki2 = 0;
        //public static double gTaki3 = 0;


        ////Pulseguide Indicators

        //public const int gMAX_plotpoints = 100;
        //public static int gMAX_RAlevel = 0;
        //public static int gMAX_DEClevel = 0;
        //public static int gPlot_ra_pos = 0;
        //public static int gPlot_dec_pos = 0;
        //public static double gplot_ra_cur = 0;
        //public static double gPlot_dec_cur = 0;
        //public static double gRAHeight = 0;
        //public static double gDecHeight = 0;

        //// Polar Alignment Variables

        //public static double gPolarAlign_RA = 0;
        //public static double gPolarAlign_DEC = 0;

        ////UPGRADE_NOTE: (2041) The following line was commented. More Information: https://www.mobilize.net/vbtonet/ewis/ewi2041
        //////UPGRADE_TODO: (1050) Structure SYSTEMTIME may require marshalling attributes to be passed as an argument in this Declare statement. More Information: https://www.mobilize.net/vbtonet/ewis/ewi1050
        ////[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        ////extern public static void GetSystemTime(ref UpgradeSolution1Support.PInvoke.UnsafeNative.Structures.SYSTEMTIME lpSystemTime);



        public double Get_EncoderHours(double RAencoderPosition)
        {

            double hours;

            // Compute in Hours the encoder value based on 0 position value (RAOffset0)
            // and Total 360 degree rotation microstep count (Tot_Enc

            if (RAencoderPosition > this.HomeEncoder.RA)
            {
                hours = ((RAencoderPosition - this.HomeEncoder.RA) / StepsPerRev.RA) * 24;
                hours = 24 - hours;
            }
            else
            {
                hours = ((this.HomeEncoder.RA - RAencoderPosition) / StepsPerRev.RA) * 24;
            }

            if (Hemisphere == HemisphereEnum.Northern)
            {
                return Range.Range24(hours + 6d); // Set to true Hours which is perpendicula to RA Axis
            }
            else
            {
                return Range.Range24((24 - hours) + 6d);
            }

        }

        //private int Get_EncoderfromHours(double encOffset0, double hourval, double Tot_enc, int hmspr)
        //{

        //    hourval = Range24(hourval - 6d); // Re-normalize from a perpendicular position
        //    if (hmspr == 0)
        //    {
        //        if (hourval < 12)
        //        {
        //            return Convert.ToInt32(encOffset0 - ((hourval / 24d) * Tot_enc));
        //        }
        //        else
        //        {
        //            return Convert.ToInt32((((24 - hourval) / 24d) * Tot_enc) + encOffset0);
        //        }
        //    }
        //    else
        //    {
        //        if (hourval < 12)
        //        {
        //            return Convert.ToInt32(((hourval / 24d) * Tot_enc) + encOffset0);
        //        }
        //        else
        //        {
        //            return Convert.ToInt32(encOffset0 - (((24 - hourval) / 24d) * Tot_enc));
        //        }
        //    }

        //}

        //private int Get_EncoderfromDegrees(double encOffset0, double degval, double Tot_enc, double Pier, int hmspr)
        //{

        //    if (hmspr == 1)
        //    {
        //        degval = 360 - degval;
        //    }
        //    if ((degval > 180) && (Pier == 0))
        //    {
        //        return Convert.ToInt32(encOffset0 - (((360 - degval) / 360d) * Tot_enc));
        //    }
        //    else
        //    {
        //        return Convert.ToInt32(((degval / 360d) * Tot_enc) + encOffset0);
        //    }

        //}


        private double Get_EncoderDegrees(double DecEncoderPosition)
        {

            double decDegrees = 0;

            // Compute in Hours the encoder value based on 0 position value (EncOffset0)
            // and Total 360 degree rotation microstep count (Tot_Enc

            if (DecEncoderPosition > this.HomeEncoder.Dec)
            {
                decDegrees = ((DecEncoderPosition - this.HomeEncoder.Dec) / this.StepsPerRev.Dec) * 360;
            }
            else
            {
                decDegrees = ((this.HomeEncoder.Dec - DecEncoderPosition) / this.StepsPerRev.Dec) * 360;
                decDegrees = 360 - decDegrees;
            }

            if (this.Hemisphere == HemisphereEnum.Northern)
            {
                return Range.Range360(decDegrees);
            }
            else
            {
                return Range.Range360(360 - decDegrees);
            }
        }

        //// Function that will ensure that the DEC value will be between -90 to 90
        //// Even if it is set at the other side of the pier

        //private double Range_DEC(double decdegrees)
        //{

        //    if ((decdegrees >= 270) && (decdegrees <= 360))
        //    {
        //        return decdegrees - 360;
        //    }

        //    if ((decdegrees >= 180) && (decdegrees < 270))
        //    {
        //        return 180 - decdegrees;
        //    }

        //    if ((decdegrees >= 90) && (decdegrees < 180))
        //    {
        //        return 180 - decdegrees;
        //    }

        //    return decdegrees;

        //}



        //internal int Get_RAEncoderfromRA(double ra_in_hours, double dec_in_degrees, double pLongitude, double encOffset0, double Tot_enc, int hmspr)
        //{


        //    double i = ra_in_hours - SiderealTime.GetLocalSiderealTime(SiteLongitude);	//   EQnow_lst(pLongitude * DEG_RAD);

        //    if (hmspr == 0)
        //    {
        //        if ((dec_in_degrees > 90) && (dec_in_degrees <= 270))
        //        {
        //            i -= 12d;
        //        }
        //    }
        //    else
        //    {
        //        if ((dec_in_degrees > 90) && (dec_in_degrees <= 270))
        //        {
        //            i += 12d;
        //        }
        //    }

        //    i = Range24(i);

        //    return Get_EncoderfromHours(encOffset0, ref i, Tot_enc, hmspr);

        //}

        //private int Get_RAEncoderfromAltAz(double Alt_in_deg, double Az_in_deg, double pLongitude, double pLatitude, double encOffset0, double Tot_enc, int hmspr)
        //{
        //    object[,,,,] aa_hadec = null;

        //    double ttha = 0;
        //    double ttdec = 0;

        //    object tempAuxVar = aa_hadec[Convert.ToInt32(pLatitude * DEG_RAD), Convert.ToInt32(Alt_in_deg * DEG_RAD), Convert.ToInt32((360d - Az_in_deg) * DEG_RAD), Convert.ToInt32(ttha), Convert.ToInt32(ttdec)];
        //    double i = (ttha * RAD_HRS);
        //    i = Range24(i);
        //    return Get_EncoderfromHours(encOffset0, ref i, Tot_enc, hmspr);

        //}

        //private int Get_DECEncoderfromAltAz(double Alt_in_deg, double Az_in_deg, double pLongitude, double pLatitude, double encOffset0, double Tot_enc, double Pier, int hmspr)
        //{
        //    object[,,,,] aa_hadec = null;

        //    double ttha = 0;
        //    double ttdec = 0;

        //    object tempAuxVar = aa_hadec[Convert.ToInt32(pLatitude * DEG_RAD), Convert.ToInt32(Alt_in_deg * DEG_RAD), Convert.ToInt32((360d - Az_in_deg) * DEG_RAD), Convert.ToInt32(ttha), Convert.ToInt32(ttdec)];
        //    double i = ttdec * RAD_DEG; // tDec was in Radians
        //    if (Pier == 1)
        //    {
        //        i = 180 - i;
        //    }
        //    return Get_EncoderfromDegrees(encOffset0, ref i, Tot_enc, Pier, hmspr);

        //}

        //private int Get_DECEncoderfromDEC(double dec_in_degrees, double Pier, double encOffset0, double Tot_enc, int hmspr)
        //{


        //    double i = dec_in_degrees;
        //    if (Pier == 1)
        //    {
        //        i = 180 - i;
        //    }
        //    return Get_EncoderfromDegrees(encOffset0, ref i, Tot_enc, Pier, hmspr);

        //}

        //private string printhex(double inpval)
        //{

        //    return " " + (Convert.ToInt32((Convert.ToInt32(inpval) & 0xF00000) / 1048576d) & 0xF).ToString("X") + (Convert.ToInt32((Convert.ToInt32(inpval) & 0xF0000) / 65536d) & 0xF).ToString("X") + (Convert.ToInt32((Convert.ToInt32(inpval) & 0xF000) / 4096d) & 0xF).ToString("X") + (Convert.ToInt32((Convert.ToInt32(inpval) & 0xF00) / 256d) & 0xF).ToString("X") + (Convert.ToInt32((Convert.ToInt32(inpval) & 0xF0) / 16d) & 0xF).ToString("X") + (Convert.ToInt32(inpval) & 0xF).ToString("X");

        //}

        ////private string FmtSexa(double N, bool ShowPlus)
        ////{
        ////	string result = "";

        ////	string sg = "+"; // Assume positive
        ////	if (N < 0)
        ////	{ // Check neg.
        ////		N = -N; // Make pos.
        ////		sg = "-"; // Remember sign
        ////	}

        ////	int m = Convert.ToInt32((N > 0) ? Math.Floor(N) : Math.Ceiling(N)); // Units (deg or hr)
        ////	string us = StringsHelper.Format(m, "00");

        ////	N = (N - m) * 60d;
        ////	m = Convert.ToInt32((N > 0) ? Math.Floor(N) : Math.Ceiling(N)); // Minutes
        ////	string ms = StringsHelper.Format(m, "00");

        ////	N = (N - m) * 60d;
        ////	m = Convert.ToInt32((N > 0) ? Math.Floor(N) : Math.Ceiling(N)); // Minutes
        ////	string ss = StringsHelper.Format(m, "00");

        ////	result = us + ":" + ms + ":" + ss;
        ////	if (ShowPlus || (sg == "-"))
        ////	{
        ////		result = sg + result;
        ////	}

        ////	return result;
        ////}

        ////private double EQnow_lst(double plong)
        ////{
        ////	object mjd_hr = null;
        ////	object mjd_day = null;
        ////	double[] vb_mjd = null;
        ////	object[, ] obliq = null;
        ////	double[] radhr = null;
        ////	object[, ] range = null;
        ////	object[, , ] utc_gst = null;
        ////	object[, , ] nut = null;

        ////	UpgradeSolution1Support.PInvoke.UnsafeNative.Structures.SYSTEMTIME typTime = new UpgradeSolution1Support.PInvoke.UnsafeNative.Structures.SYSTEMTIME();
        ////	double eps = 0;
        ////	double lst = 0;
        ////	double deps = 0;
        ////	double dpsi = 0;

        ////	//    mjd = vb_mjd(CDbl(Now) + gGPSTimeDelta)

        ////	UpgradeSolution1Support.PInvoke.SafeNative.kernel32.GetSystemTime(ref typTime);
        ////	double mjd = vb_mjd[Convert.ToInt32(DateTime.Now.AddDays(gEQTimeDelta).AddDays(typTime.wMilliseconds / 86400000d).ToOADate())];
        ////	object tempAuxVar = utc_gst[ReflectionHelper.GetPrimitiveValue<int>(((Array) mjd_day).GetValue(Convert.ToInt32(mjd))), ReflectionHelper.GetPrimitiveValue<int>(((Array) mjd_hr).GetValue(Convert.ToInt32(mjd))), Convert.ToInt32(lst)];
        ////	lst += radhr[Convert.ToInt32(plong)];
        ////	object tempAuxVar2 = obliq[Convert.ToInt32(mjd), Convert.ToInt32(eps)];
        ////	object tempAuxVar3 = nut[Convert.ToInt32(mjd), Convert.ToInt32(deps), Convert.ToInt32(dpsi)];
        ////	lst += radhr[Convert.ToInt32(dpsi * Math.Cos(eps + deps))];
        ////	object tempAuxVar4 = range[Convert.ToInt32(lst), 24L];

        ////	return lst;
        ////	//    EQnow_lst = now_lst(plong)

        ////}


        ////private double EQnow_lst_norange()
        ////{

        ////	UpgradeSolution1Support.PInvoke.UnsafeNative.Structures.SYSTEMTIME typTime = new UpgradeSolution1Support.PInvoke.UnsafeNative.Structures.SYSTEMTIME();

        ////	UpgradeSolution1Support.PInvoke.SafeNative.kernel32.GetSystemTime(ref typTime);
        ////	double mjd = (typTime.wMinute * 60) + (typTime.wSecond) + (typTime.wMilliseconds / 1000d);
        ////	double MTMP = (typTime.wHour);
        ////	MTMP *= 3600;
        ////	mjd = mjd + MTMP + (typTime.wDay * 86400);

        ////	return mjd;

        ////}


        ////private double EQnow_lst_time(double plong, double ptime)
        ////{
        ////	object mjd_hr = null;
        ////	object mjd_day = null;
        ////	double[] vb_mjd = null;
        ////	object[, ] obliq = null;
        ////	double[] radhr = null;
        ////	object[, ] range = null;
        ////	object[, , ] utc_gst = null;
        ////	object[, , ] nut = null;

        ////	double eps = 0;
        ////	double lst = 0;
        ////	double deps = 0;
        ////	double dpsi = 0;

        ////	double mjd = vb_mjd[Convert.ToInt32(ptime)];
        ////	object tempAuxVar = utc_gst[ReflectionHelper.GetPrimitiveValue<int>(((Array) mjd_day).GetValue(Convert.ToInt32(mjd))), ReflectionHelper.GetPrimitiveValue<int>(((Array) mjd_hr).GetValue(Convert.ToInt32(mjd))), Convert.ToInt32(lst)];
        ////	lst += radhr[Convert.ToInt32(plong)];
        ////	object tempAuxVar2 = obliq[Convert.ToInt32(mjd), Convert.ToInt32(eps)];
        ////	object tempAuxVar3 = nut[Convert.ToInt32(mjd), Convert.ToInt32(deps), Convert.ToInt32(dpsi)];
        ////	lst += radhr[Convert.ToInt32(dpsi * Math.Cos(eps + deps))];
        ////	object tempAuxVar4 = range[Convert.ToInt32(lst), 24L];

        ////	return lst;

        ////}


        //private PierSide2 SOP_DEC(double DEC)
        //{

        //    DEC = Math.Abs(DEC - 180);

        //    if (DEC <= 90)
        //    {
        //        return PierSide2.PierEast2;
        //    }
        //    else
        //    {
        //        return PierSide2.PierWest2;
        //    }

        //}

        ////private PierSide2 SOP_Physical(double vha)
        ////{
        ////	object gAscomCompatibility = null;

        ////	double ha = RangeHA(vha - 6d);

        ////	if (ReflectionHelper.GetMember<bool>(gAscomCompatibility, "SwapPhysicalSideOfPier"))
        ////	{
        ////		return (ha >= 0) ? PierSide2.PierWest2 : PierSide2.PierEast2;
        ////	}
        ////	else
        ////	{
        ////		return (ha >= 0) ? PierSide2.PierEast2 : PierSide2.PierWest2;
        ////	}



        ////}

        ////private PierSide2 SOP_Pointing(double DEC)
        ////{
        ////	PierSide2 result = PierSide2.PierEast2;
        ////	object gAscomCompatibility = null;

        ////	if (DEC <= 90 || DEC >= 270)
        ////	{
        ////		if (ReflectionHelper.GetMember<bool>(gAscomCompatibility, "SwapPointingSideOfPier"))
        ////		{
        ////			result = PierSide2.PierEast2;
        ////		}
        ////		else
        ////		{
        ////			result = PierSide2.PierWest2;
        ////		}
        ////	}
        ////	else
        ////	{
        ////		if (ReflectionHelper.GetMember<bool>(gAscomCompatibility, "SwapPointingSideOfPier"))
        ////		{
        ////			result = PierSide2.PierWest2;
        ////		}
        ////		else
        ////		{
        ////			result = PierSide2.PierEast2;
        ////		}
        ////	}

        ////	// in the south east is west and west is east!
        ////	if (gHemisphere == 1)
        ////	{
        ////		if (result == PierSide2.PierWest2)
        ////		{
        ////			result = PierSide2.PierEast2;
        ////		}
        ////		else
        ////		{
        ////			result = PierSide2.PierWest2;
        ////		}
        ////	}

        ////	return result;
        ////}
        ////private PierSide2 SOP_RA(double vRA, double pLongitude)
        ////{

        ////	double i = vRA - EQnow_lst(pLongitude * DEG_RAD);
        ////	i = RangeHA(i - 6d);
        ////	return (i < 0) ? PierSide2.PierEast2 : PierSide2.PierWest2;

        ////}

        //private double Range24(double vha)
        //{

        //    while (vha < 0d)
        //    {
        //        vha += 24d;
        //    }
        //    while (vha >= 24d)
        //    {
        //        vha -= 24d;
        //    }

        //    return vha;

        //}

        //private double Range360(double vdeg)
        //{

        //    while (vdeg < 0d)
        //    {
        //        vdeg += 360d;
        //    }
        //    while (vdeg >= 360d)
        //    {
        //        vdeg -= 360d;
        //    }

        //    return vdeg;

        //}
        //private double Range90(double vdeg)
        //{

        //    while (vdeg < -90d)
        //    {
        //        vdeg += 360d;
        //    }
        //    while (vdeg >= 360d)
        //    {
        //        vdeg -= 90d;
        //    }

        //    return vdeg;

        //}


        //private double GetSlowdown(double deltaval)
        //{

        //    double i = deltaval - 80000;
        //    if (i < 0)
        //    {
        //        i = deltaval * 0.5d;
        //    }
        //    return i;

        //}

        //private double Delta_RA_Map(double RAENCODER)
        //{

        //    return RAENCODER + gRA1Star + gRASync01;

        //}

        //private double Delta_DEC_Map(double DecEncoder)
        //{

        //    return DecEncoder + gDEC1Star + gDECSync01;

        //}


        //private Coordt Delta_Matrix_Map(double RA, double DEC)
        //{
        //    Coordt result = new Coordt();
        //    Coord obtmp = new Coord();

        //    if ((RA >= 0x1000000) || (DEC >= 0x1000000))
        //    {
        //        result.x = RA;
        //        result.y = DEC;
        //        result.z = 1;
        //        result.f = 0;
        //        return result;
        //    }

        //    obtmp.x = RA;
        //    obtmp.y = DEC;
        //    obtmp.z = 1;

        //    // re transform based on the nearest 3 stars
        //    int i = EQ_UpdateTaki(RA, DEC);

        //    Coord obtmp2 = EQ_plTaki(obtmp);

        //    result.x = obtmp2.x;
        //    result.y = obtmp2.y;
        //    result.z = 1;
        //    result.f = (short)i;

        //    return result;
        //}


        //private Coordt Delta_Matrix_Reverse_Map(double RA, double DEC)
        //{

        //    Coordt result = new Coordt();
        //    Coord obtmp = new Coord();

        //    if ((RA >= 0x1000000) || (DEC >= 0x1000000))
        //    {
        //        result.x = RA;
        //        result.y = DEC;
        //        result.z = 1;
        //        result.f = 0;
        //        return result;
        //    }

        //    obtmp.x = RA + gRASync01;
        //    obtmp.y = DEC + gDECSync01;
        //    obtmp.z = 1;

        //    // re transform using the 3 nearest stars
        //    int i = EQ_UpdateAffine(obtmp.x, obtmp.y);
        //    Coord obtmp2 = EQ_plAffine(obtmp);

        //    result.x = obtmp2.x;
        //    result.y = obtmp2.y;
        //    result.z = 1;
        //    result.f = (short)i;


        //    return result;
        //}


        //private Coordt DeltaSync_Matrix_Map(double RA, double DEC)
        //{
        //    Coordt result = new Coordt();
        //    int i = 0;

        //    if ((RA >= 0x1000000) || (DEC >= 0x1000000))
        //    {
        //        result.x = RA;
        //        result.y = DEC;
        //        result.z = 0;
        //        result.f = 0;
        //    }
        //    else
        //    {
        //        i = GetNearest(RA, DEC);
        //        if (i != -1)
        //        {
        //            EQASCOM.gSelectStar = i;
        //            result.x = RA + (EQASCOM.ct_Points[i].x - EQASCOM.my_Points[i].x) + gRASync01;
        //            result.y = DEC + (EQASCOM.ct_Points[i].y - EQASCOM.my_Points[i].y) + gDECSync01;
        //            result.z = 1;
        //            result.f = 0;
        //        }
        //        else
        //        {
        //            result.x = RA;
        //            result.y = DEC;
        //            result.z = 0;
        //            result.f = 0;
        //        }
        //    }
        //    return result;
        //}


        //private Coordt DeltaSyncReverse_Matrix_Map(double RA, double DEC)
        //{
        //    Coordt result = new Coordt();
        //    int i = 0;

        //    if ((RA >= 0x1000000) || (DEC >= 0x1000000) || EQASCOM.gAlignmentStars_count == 0)
        //    {
        //        result.x = RA;
        //        result.y = DEC;
        //        result.z = 1;
        //        result.f = 0;
        //    }
        //    else
        //    {
        //        i = GetNearest(RA, DEC);

        //        if (i != -1)
        //        {
        //            EQASCOM.gSelectStar = i;
        //            result.x = RA - (EQASCOM.ct_Points[i].x - EQASCOM.my_Points[i].x);
        //            result.y = DEC - (EQASCOM.ct_Points[i].y - EQASCOM.my_Points[i].y);
        //            result.z = 1;
        //            result.f = 0;
        //        }
        //        else
        //        {
        //            result.x = RA;
        //            result.y = DEC;
        //            result.z = 1;
        //            result.f = 0;
        //        }
        //    }
        //    return result;
        //}
        //private int GetQuadrant(Coord tmpcoord)
        //{
        //    int ret = 0;

        //    if (tmpcoord.x >= 0)
        //    {
        //        if (tmpcoord.y >= 0)
        //        {
        //            ret = 0;
        //        }
        //        else
        //        {
        //            ret = 1;
        //        }
        //    }
        //    else
        //    {
        //        if (tmpcoord.y >= 0)
        //        {
        //            ret = 2;
        //        }
        //        else
        //        {
        //            ret = 3;
        //        }
        //    }

        //    return ret;

        //}


        //private int GetNearest(double RA, double DEC)
        //{
        //    int i = 0;
        //    Coord tmpcoord = new Coord();
        //    Coord tmpcoord2 = new Coord();
        //    double[] datholder = new double[EQASCOM.MAX_STARS];
        //    int[] datholder2 = new int[EQASCOM.MAX_STARS];

        //    tmpcoord.x = RA;
        //    tmpcoord.y = DEC;
        //    tmpcoord = EQ_sp2Cs(tmpcoord);

        //    int Count = 0;

        //    for (i = 1; i <= EQASCOM.gAlignmentStars_count; i++)
        //    {

        //        tmpcoord2 = EQASCOM.my_PointsC[i];

        //        switch (ActivePoints)
        //        {
        //            case ActivePointsEnum.All:
        //                // all points 

        //                break;
        //            case ActivePointsEnum.PierSide:
        //                // only consider points on this side of the meridian 
        //                if (tmpcoord2.y * tmpcoord.y < 0)
        //                {
        //                    goto NextPoint;
        //                }

        //                break;
        //            case ActivePointsEnum.LocalQuadrant:
        //                // local quadrant 
        //                if (GetQuadrant(tmpcoord) != GetQuadrant(tmpcoord2))
        //                {
        //                    goto NextPoint;
        //                }

        //                break;
        //        }

        //        Count++;
        //        if (CheckLocalPier)
        //        {
        //            // calculate polar distance
        //            datholder[Count - 1] = Math.Pow(EQASCOM.my_Points[i].x - RA, 2) + Math.Pow(EQASCOM.my_Points[i].y - DEC, 2);
        //        }
        //        else
        //        {
        //            // calculate cartesian disatnce
        //            datholder[Count - 1] = Math.Pow(tmpcoord2.x - tmpcoord.x, 2) + Math.Pow(tmpcoord2.y - tmpcoord.y, 2);
        //        }

        //        datholder2[Count - 1] = i;

        //    NextPoint:;
        //    }

        //    if (Count == 0)
        //    {
        //        return -1;
        //    }
        //    else
        //    {
        //        //    i = EQ_FindLowest(datholder(), 1, gAlignmentStars_count)
        //        i = EQ_FindLowest(datholder, 1, Count);
        //        if (i == -1)
        //        {
        //            return -1;
        //        }
        //        else
        //        {
        //            return datholder2[i - 1];
        //        }
        //    }

        //}


    }
}


