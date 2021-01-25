/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GS.FitsImageManager
{
	public enum ErrorStatusCode
	{
		CREATE_DISK_FILE   = -106, // create disk file, without extended filename syntax 
		OPEN_DISK_FILE     = -105, // open disk file, without extended filename syntax 
		SKIP_TABLE         = -104, // move to 1st image when opening file 
		SKIP_IMAGE         = -103, // move to 1st table when opening file 
		SKIP_NULL_PRIMARY  = -102, // skip null primary array when opening file 
		USE_MEM_BUFF       = -101, // use memory buffer when opening file 
		OVERFLOW_ERR       =  -11, // overflow during datatype conversion 
		PREPEND_PRIMARY    =   -9, // used in ffiimg to insert new primary array 
		SAME_FILE          =  101, // input and output files are the same 
		TOO_MANY_FILES     =  103, // tried to open too many FITS files 
		FILE_NOT_OPENED    =  104, // could not open the named file 
		FILE_NOT_CREATED   =  105, // could not create the named file 
		WRITE_ERROR        =  106, // error writing to FITS file 
		END_OF_FILE        =  107, // tried to move past end of file 
		READ_ERROR         =  108, // error reading from FITS file 
		FILE_NOT_CLOSED    =  110, // could not close the file 
		ARRAY_TOO_BIG      =  111, // array dimensions exceed internal limit 
		READONLY_FILE      =  112, // Cannot write to readonly file 
		MEMORY_ALLOCATION  =  113, // Could not allocate memory 
		BAD_FILEPTR        =  114, // invalid fitsfile pointer	
		NULL_INPUT_PTR     =  115, // NULL input pointer to routine 
		SEEK_ERROR         =  116, // error seeking position in file 
		BAD_NETTIMEOUT     =  117, // bad value for file download timeout setting 

		BAD_URL_PREFIX     =  121, // invalid URL prefix on file name 
		TOO_MANY_DRIVERS   =  122, // tried to register too many IO drivers 
		DRIVER_INIT_FAILED =  123, // driver initialization failed 
		NO_MATCHING_DRIVER =  124, // matching driver is not registered 
		URL_PARSE_ERROR    =  125, // failed to parse input file URL 
		RANGE_PARSE_ERROR  =  126, // failed to parse input file URL 

		SHARED_ERRBASE     =  150,
		SHARED_BADARG      =  151,
		SHARED_NULPTR      =  152,
		SHARED_TABFULL     =  153,
		SHARED_NOTINIT     =  154,
		SHARED_IPCERR      =  155,
		SHARED_NOMEM       =  156,
		SHARED_AGAIN       =  157,
		SHARED_NOFILE      =  158,
		SHARED_NORESIZE    =  159,

		HEADER_NOT_EMPTY   =  201, // header already contains keywords
		KEY_NO_EXIST       =  202, // keyword not found in header
		KEY_OUT_BOUNDS     =  203, // keyword record number is out of bounds
		VALUE_UNDEFINED    =  204, // keyword value field is blank
		NO_QUOTE           =  205, // string is missing the closing quote
		BAD_INDEX_KEY      =  206, // illegal indexed keyword name
		BAD_KEYCHAR        =  207, // illegal character in keyword name or card
		BAD_ORDER          =  208, // required keywords out of order
		NOT_POS_INT        =  209, // keyword value is not a positive integer
		NO_END             =  210, // couldn't find END keyword
		BAD_BITPIX         =  211, // illegal BITPIX keyword value*/
		BAD_NAXIS          =  212, // illegal NAXIS keyword value
		BAD_NAXES          =  213, // illegal NAXISn keyword value
		BAD_PCOUNT         =  214, // illegal PCOUNT keyword value
		BAD_GCOUNT         =  215, // illegal GCOUNT keyword value
		BAD_TFIELDS        =  216, // illegal TFIELDS keyword value
		NEG_WIDTH          =  217, // negative table row size
		NEG_ROWS           =  218, // negative number of rows in table
		COL_NOT_FOUND      =  219, // column with this name not found in table
		BAD_SIMPLE         =  220, // illegal value of SIMPLE keyword 
		NO_SIMPLE          =  221, // Primary array doesn't start with SIMPLE
		NO_BITPIX          =  222, // Second keyword not BITPIX
		NO_NAXIS           =  223, // Third keyword not NAXIS
		NO_NAXES           =  224, // Couldn't find all the NAXISn keywords
		NO_XTENSION        =  225, // HDU doesn't start with XTENSION keyword
		NOT_ATABLE         =  226, // the CHDU is not an ASCII table extension
		NOT_BTABLE         =  227, // the CHDU is not a binary table extension
		NO_PCOUNT          =  228, // couldn't find PCOUNT keyword
		NO_GCOUNT          =  229, // couldn't find GCOUNT keyword
		NO_TFIELDS         =  230, // couldn't find TFIELDS keyword
		NO_TBCOL           =  231, // couldn't find TBCOLn keyword
		NO_TFORM           =  232, // couldn't find TFORMn keyword
		NOT_IMAGE          =  233, // the CHDU is not an IMAGE extension
		BAD_TBCOL          =  234, // TBCOLn keyword value < 0 or > rowlength
		NOT_TABLE          =  235, // the CHDU is not a table
		COL_TOO_WIDE       =  236, // column is too wide to fit in table
		COL_NOT_UNIQUE     =  237, // more than 1 column name matches template
		BAD_ROW_WIDTH      =  241, // sum of column widths not = NAXIS1
		UNKNOWN_EXT        =  251, // unrecognizable FITS extension type
		UNKNOWN_REC        =  252, // unrecognizable FITS record
		END_JUNK           =  253, // END keyword is not blank
		BAD_HEADER_FILL    =  254, // Header fill area not blank
		BAD_DATA_FILL      =  255, // Data fill area not blank or zero
		BAD_TFORM          =  261, // illegal TFORM format code
		BAD_TFORM_DTYPE    =  262, // unrecognizable TFORM datatype code
		BAD_TDIM           =  263, // illegal TDIMn keyword value
		BAD_HEAP_PTR       =  264, // invalid BINTABLE heap address

		BAD_HDU_NUM        =  301, // HDU number < 1 or > MAXHDU
		BAD_COL_NUM        =  302, // column number < 1 or > tfields
		NEG_FILE_POS       =  304, // tried to move before beginning of file 
		NEG_BYTES          =  306, // tried to read or write negative bytes
		BAD_ROW_NUM        =  307, // illegal starting row number in table
		BAD_ELEM_NUM       =  308, // illegal starting element number in vector
		NOT_ASCII_COL      =  309, // this is not an ASCII string column
		NOT_LOGICAL_COL    =  310, // this is not a logical datatype column
		BAD_ATABLE_FORMAT  =  311, // ASCII table column has wrong format
		BAD_BTABLE_FORMAT  =  312, // Binary table column has wrong format
		NO_NULL            =  314, // null value has not been defined
		NOT_VARI_LEN       =  317, // this is not a variable length column
		BAD_DIMEN          =  320, // illegal number of dimensions in array
		BAD_PIX_NUM        =  321, // first pixel number greater than last pixel
		ZERO_SCALE         =  322, // illegal BSCALE or TSCALn keyword = 0
		NEG_AXIS           =  323, // illegal axis length < 1

		NOT_GROUP_TABLE        = 340,
		HDU_ALREADY_MEMBER     = 341,
		MEMBER_NOT_FOUND       = 342,
		GROUP_NOT_FOUND        = 343,
		BAD_GROUP_ID           = 344,
		TOO_MANY_HDUS_TRACKED  = 345,
		HDU_ALREADY_TRACKED    = 346,
		BAD_OPTION             = 347,
		IDENTICAL_POINTERS     = 348,
		BAD_GROUP_ATTACH       = 349,
		BAD_GROUP_DETACH       = 350,

		BAD_I2C            = 401, // bad int to formatted string conversion
		BAD_F2C            = 402, // bad float to formatted string conversion
		BAD_INTKEY         = 403, // can't interprete keyword value as integer
		BAD_LOGICALKEY     = 404, // can't interprete keyword value as logical
		BAD_FLOATKEY       = 405, // can't interprete keyword value as float
		BAD_DOUBLEKEY      = 406, // can't interprete keyword value as double
		BAD_C2I            = 407, // bad formatted string to int conversion
		BAD_C2F            = 408, // bad formatted string to float conversion
		BAD_C2D            = 409, // bad formatted string to double conversion
		BAD_DATATYPE       = 410, // bad keyword datatype code
		BAD_DECIM          = 411, // bad number of decimal places specified
		NUM_OVERFLOW       = 412, // overflow during datatype conversion

		DATA_COMPRESSION_ERR   = 413, // error in imcompress routines
		DATA_DECOMPRESSION_ERR = 414, // error in imcompress routines
		NO_COMPRESSED_TILE     = 415, // compressed tile doesn't exist

		BAD_DATE           = 420, // error in date or time conversion

		PARSE_SYNTAX_ERR   = 431, // syntax error in parser expression
		PARSE_BAD_TYPE     = 432, // expression did not evaluate to desired type
		PARSE_LRG_VECTOR   = 433, // vector result too large to return in array
		PARSE_NO_OUTPUT    = 434, // data parser failed not sent an out column
		PARSE_BAD_COL      = 435, // bad data encounter while parsing column
		PARSE_BAD_OUTPUT   = 436, // Output file not of proper type         

		ANGLE_TOO_BIG      = 501, // celestial angle too large for projection
		BAD_WCS_VAL        = 502, // bad celestial coordinate or pixel value
		WCS_ERROR          = 503, // error in celestial coordinate calculation
		BAD_WCS_PROJ       = 504, // unsupported type of celestial projection
		NO_WCS_KEY         = 505, // celestial coordinate keywords not found
		APPROX_WCS_KEY     = 506, // approximate WCS keywords were calculated

		NO_CLOSE_ERROR     = 999  // special value used internally to switch off
								  // the error message from ffclos and ffchdu
	};

	public class CFits
	{
		private const string DLL_NAME = @"cfitsio";

		private const int FLEN_ERRMSG = 80;     // max length of a FITSIO error message

		public const int FLEN_KEYWORD   = 67;   // max length of a keyword (HIERARCH convention)
		public const int FLEN_VALUE    = 70;    // max length of a keyword value string 
		public const int FLEN_COMMENT = 70;     // max length of a keyword comment string

		public const int READONLY = 0;
		public const int READWRITE = 1;

		//static CFits()
		//{}

		/// Return Type: int
		///fptr: fitsfile**
		///filename: char*
		///iomode: int
		///status: int*
		[DllImport( DLL_NAME, EntryPoint = "ffopen", CallingConvention = CallingConvention.Cdecl )]
		public static extern int OpenFile( out IntPtr fptr, string filename, int iomode, ref int status );

		/// Return Type: int
		///fptr: fitsfile*
		///status: int*
		[DllImport( DLL_NAME, EntryPoint = "ffclos", CallingConvention = CallingConvention.Cdecl )]
		public static extern int CloseFile( IntPtr fptr, ref int status );


		/// Return Type: int
		///fptr: fitsfile*
		///keynum: int
		///keyname: char*
		///value: char*
		///comment: char*
		///status: int*
		[DllImport( DLL_NAME, EntryPoint = "ffgkyn", CallingConvention = CallingConvention.Cdecl )]
		private static extern int ReadHeaderRecordInternal( IntPtr fptr, int keynum, StringBuilder keyname, StringBuilder value, StringBuilder comment, ref int status );

        [HandleProcessCorruptedStateExceptions]
		public static int ReadHeaderRecord( IntPtr fptr, int keynum, ref string keyname, ref string value, ref string comment, ref int status )
		{
            try
            {
                status = 0;

                var sbKey = new StringBuilder(FLEN_KEYWORD);
                var sbValue = new StringBuilder(FLEN_VALUE);
                var sbComment = new StringBuilder(FLEN_COMMENT);

                ReadHeaderRecordInternal(fptr, keynum, sbKey, sbValue, sbComment, ref status);

                if (status != 0) return status;
                keyname = sbKey.ToString();
                value = sbValue.ToString();
                comment = sbComment.ToString();

                return status;
			}
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 108;

            }

		}

		/// Return Type: void
		///status: int
		///errtext: char*
		[DllImport( DLL_NAME, EntryPoint = "ffgerr", CallingConvention = CallingConvention.Cdecl )]
		private static extern void GetErrorStatusInternal( int status, StringBuilder errtext );

		public static string GetErrorStatus( int status )
		{
			var errorText = new StringBuilder( FLEN_ERRMSG );
			GetErrorStatusInternal( status, errorText );

			return errorText.ToString();
		}

		public static void ReportError( TextWriter writer, int status )
        {
            if (status == 0) return;
            var errorText = new StringBuilder( FLEN_ERRMSG );

            GetErrorStatusInternal( status, errorText );
            writer.WriteLine( "Fits Error Occurred - {0}", errorText );
        }
	}
}
