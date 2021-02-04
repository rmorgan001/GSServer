/* Copyright(C) 2019-2021-2021  Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.FitsImageManager
{
	public class ImageManager
	{
		#region Private Properties

		private static ImageManager Mgr { get; }

		private IntPtr _fitsPointer;
		private int Status { get; set; }

		private string FilePath { get; set; }

		#endregion Private Properties

		#region Constructors

		static ImageManager()
		{
			Mgr = new ImageManager();
		}

		private ImageManager()
		{
			_fitsPointer = IntPtr.Zero;
			Status = 0;
			FilePath = null;
		}

		#endregion Constructors

		#region Private Properties

		private FitsHeader Header { get; set; }

		#endregion Private Properties

		public static int LoadImage( string filepath, out FitsHeader header )
		{
			int retval;

            header = null;

			try
			{
				Mgr.Status = 0;

				if ( Mgr.OpenFile( filepath ) )
				{
					header = Mgr.Header;
				}
			}
			finally
			{
				retval = Mgr.Status;

				Mgr.CloseFile();
			}

			return retval;
		}
		
		public static string GetErrorText( int status )
		{
			return CFits.GetErrorStatus( status );
		}

		#region Helper Methods

		private bool OpenFile( string filePath )
		{
			var retval = false;

			var status = 0;

			if ( string.IsNullOrEmpty( filePath ) )
			{
				throw new ArgumentException( "The file path cannot be a null or empty string!" );
			}

			if ( FilePath != null )
			{
				throw new InvalidOperationException( "OpenFile cannot be called a again once a file has been opened!" );
			}


			CFits.OpenFile( out _fitsPointer, filePath, CFits.READONLY, ref status );
			Header = GetHeaderData();
			Status = status;

			if ( status == 0 )
			{
				FilePath = filePath;

				retval = true;
			}

			return retval;
		}

		private FitsHeader GetHeaderData()
		{
			if ( Status != 0 )
			{
				return null;
			}

			var status = 0;

			var fitsHeader = new FitsHeader();

			var ndx = 1;

			while ( true )
			{
				string keyName = null;
				string keyValue = null;
				string keyComment = null;

				CFits.ReadHeaderRecord( _fitsPointer, ndx++, ref keyName, ref keyValue, ref keyComment, ref status );

				if ( status == (int)ErrorStatusCode.KEY_OUT_BOUNDS )
				{
					break;
				}
				else if ( status != 0 )
				{
					Status = status;
					var errText = CFits.GetErrorStatus( status );

					throw new Exception( "FITS ReadHeaderRecord error - " + errText );
				}

				var keyUnits = "";

				ExtractUnits( ref keyUnits, ref keyComment );
				var item = new FitsHeaderItem( keyName, keyValue, keyUnits, keyComment);

				fitsHeader.Add( item );
			}

			Header = fitsHeader;

			return fitsHeader;
		}

		private void ExtractUnits( ref string keyUnits, ref string keyComment )
		{
			// The units, if they exist are between the square brackets at the start of the comment and 
			// the comment follows the whitespace after the closing bracket.

			var rgx = new Regex(@"^\[(.+)\]\s{1}?(.*)$");

			var match = rgx.Match( keyComment );

			if ( match.Success && match.Groups.Count == 3 )
			{
				keyUnits = match.Groups[1].Value;
				keyComment = match.Groups[2].Value;
			}
		}

		public void CloseFile()
		{
			if ( _fitsPointer != IntPtr.Zero )
			{
				var status = 0;

				FilePath = null;
				CFits.CloseFile( _fitsPointer, ref status );
				_fitsPointer = IntPtr.Zero;
				Status = status;
			}
		}

		#endregion Helper Methods
	}
}
