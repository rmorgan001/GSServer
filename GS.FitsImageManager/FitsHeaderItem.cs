/* Copyright(C) 2019-2025  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Numerics;
using System.Text;
using GS.FitsImageManager.Header_Value_Converters;

namespace GS.FitsImageManager
{
	public class FitsHeaderItem : IFitsHeaderItem
	{
		#region Static Converter instances

		private static StringStringConverter _strCvt;

		private static StringStringConverter StrCvt => _strCvt ?? (_strCvt = new StringStringConverter());

        private static BoolStringConverter _boolCvt;

		private static BoolStringConverter BoolCvt => _boolCvt ?? (_boolCvt = new BoolStringConverter());

        private static ComplexStringConverter _cplxCvt;

		private static ComplexStringConverter CplxCvt => _cplxCvt ?? (_cplxCvt = new ComplexStringConverter());

        private static DateTimeStringConverter _dateCvt;

		private static DateTimeStringConverter DateCvt => _dateCvt ?? (_dateCvt = new DateTimeStringConverter());

        private static DoubleStringConverter _dblCvt;

		private static DoubleStringConverter DblCvt => _dblCvt ?? (_dblCvt = new DoubleStringConverter());

        private static FloatStringConverter _fltCvt;

		private static FloatStringConverter FltCvt => _fltCvt ?? (_fltCvt = new FloatStringConverter());

        private static IntegerStringConverter _intCvt;

		private static IntegerStringConverter IntCvt => _intCvt ?? (_intCvt = new IntegerStringConverter());

        #endregion Static Converter instances

		public FitsHeaderItem( string keyName)
		{
			KeyName = keyName;
			Value = null;
			Units = null;
			Comment = null;
		}

		/// <summary>
		/// Create a header item with a string value
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="value"></param>
		/// <param name="units"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, string value, string units, string comment )
		{
			KeyName = keyName;
			Value = value;
			Units = units;
			Comment = comment;

			// We got passed a string, which could contain any type of data so we need to parse
			// the Value to figure it out.

			ValueType = DiscoverValueType();
		}

		/// <summary>
		/// Create a header item with a boolean value
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, bool nativeValue, string comment )
		{
			KeyName = keyName;
			//var cvt = new BoolStringConverter();			
			Value = BoolCvt.Convert( nativeValue );
			ValueType = HeaderValueType.Logical;
			Units = null;
			Comment = comment;
		}


		/// <summary>
		/// Create a header item with a Int32 value, but no units
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, int nativeValue, string comment )
			: this( keyName, nativeValue, null, comment)
		{ }

		/// <summary>
		/// Create a header item with a Int32 value and units
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="units"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, int nativeValue, string units, string comment )
		{
			KeyName = keyName;
			Value = IntCvt.Convert( nativeValue );
			ValueType = HeaderValueType.Integer;
			Units = units;
			Comment = comment;
		}

		/// <summary>
		/// Create a header item with a single-precision real value with no units and default precision
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, float nativeValue, string comment )
			: this( keyName, nativeValue, 0, null, comment )
		{ }

		/// <summary>
		/// Create a header item with a single-precision real value with units and default precision
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="units"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, float nativeValue, string units, string comment )
			: this( keyName, nativeValue, 0, units, comment )
		{}

        /// <summary>
        /// Create a header item with a single-precision real value with no units but custom precision
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="nativeValue"></param>
        /// <param name="decimals"></param>
        /// <param name="comment"></param>
        public FitsHeaderItem( string keyName, float nativeValue, int decimals, string comment )
			: this( keyName, nativeValue, decimals, null, comment )
		{ }

        /// <summary>
        /// Create a header item with a single-precision real value with units and custom precision value 
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="nativeValue"></param>
        /// <param name="decimals"></param>
        /// <param name="units"></param>
        /// <param name="comment"></param>
        public FitsHeaderItem( string keyName, float nativeValue, int decimals, string units, string comment )
		{
			KeyName = keyName;
			Value = FltCvt.Convert( nativeValue, decimals );
			ValueType = HeaderValueType.Single;
			Units = units;
			Comment = comment;
		}

		/// <summary>
		/// Create a header item with a double-precision real value with no units and default precision
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, double nativeValue, string comment )
			: this( keyName, nativeValue, 0, null, comment )
		{ }

		/// <summary>
		/// Create a header item with a double-precision real value with units and default precision
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="units"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, double nativeValue, string units, string comment )
			: this( keyName, nativeValue, 0, units, comment )
		{ }

        /// <summary>
        /// Create a header item with a double-precision real value with no units but custom precision
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="nativeValue"></param>
        /// <param name="decimals"></param>
        /// <param name="comment"></param>
        public FitsHeaderItem( string keyName, double nativeValue, int decimals, string comment )
			: this( keyName, nativeValue, decimals, null, comment )
		{ }

		/// <summary>
		/// Create a header item with a double-precision real value along with custom precision and units
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="decimals"></param>
		/// <param name="units"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, double nativeValue, int decimals, string units, string comment )
		{
			KeyName = keyName;
			Value = DblCvt.Convert( nativeValue, decimals );
			ValueType = HeaderValueType.Double;
			Units = units;
			Comment = comment;
		}

		/// <summary>
		/// Create a header item with a complex value along with custom precision
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="decimals"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, Complex nativeValue, int decimals, string comment )
		{
			KeyName = keyName;
			Value = CplxCvt.Convert( nativeValue, decimals );
			ValueType = HeaderValueType.Complex;
			Units = null;
			Comment = comment;
		}

		/// <summary>
		/// Create a header item with a date/time value.
		/// 
		/// If the passed time is Local, it is converted to UTC.
		/// </summary>
		/// <param name="keyName"></param>
		/// <param name="nativeValue"></param>
		/// <param name="comment"></param>
		public FitsHeaderItem( string keyName, DateTime nativeValue, string comment )
		{
			KeyName = keyName;
			Value = DateCvt.Convert( nativeValue );
			ValueType = HeaderValueType.DateTime;
			Units = null;
			Comment = comment;
		}

		public string KeyName { get; private set; }
		public HeaderValueType ValueType { get; private set; }
		public string Value { get; private set; }
		public string Units { get; private set; }
		public string Comment { get; private set; }

		private HeaderValueType DiscoverValueType()
		{
			if ( IsNullValue( Value ) )
			{
				return HeaderValueType.None;
			}
			else if ( IsLogicalValue( Value ) )
			{
				return HeaderValueType.Logical;
			}
			else if ( IsIntegerValue( Value ) )
			{
				return HeaderValueType.Integer;
			}
			else if ( IsFloatValue( Value ) )
			{
				return HeaderValueType.Single;
			}
			else if ( IsDoubleValue( Value ) )
			{
				return HeaderValueType.Double;
			}
			else if ( IsComplexValue( Value ) )
			{
				return HeaderValueType.Complex;
			}
			else if ( IsStringValue( Value ) )
			{
				var value = StripStringDelimiter( Value );

				return IsDateString( value ) ? HeaderValueType.DateTime : HeaderValueType.String;
			}

			return HeaderValueType.Unknown;
		}

		private bool IsCommentRecord()
		{
			return KeyName == "COMMENT";
		}

		private bool IsComplexValue( string value )
		{
			var cvt = new ComplexStringConverter();

			return cvt.IsConvertible( value );
		}

		private bool IsDoubleValue( string value )
		{
			var cvt = new DoubleStringConverter();

			return cvt.IsConvertible( value );
		}

		private bool IsFloatValue( string value )
		{
			var cvt = new FloatStringConverter();

			return cvt.IsConvertible( value );
		}

		private bool IsIntegerValue( string value )
		{
			var cvt = new IntegerStringConverter();

			return cvt.IsConvertible( value );
		}

		private bool IsLogicalValue( string value )
		{
			return value == "T" || value == "F";
		}

		private bool IsDateString( string value )
		{
			var cvt = new DateTimeStringConverter();

			return cvt.IsConvertible( value );
		}

		private string StripStringDelimiter( string value )
		{
			// The string has already been confirmed to have apostrophes at the start and end.

			return value.Substring( 1, value.Length - 2 );
		}

		private bool IsStringValue( string value )
		{
			return StrCvt.IsConvertible( value );
		}

		private bool IsNullValue( string value )
		{
			return String.IsNullOrEmpty( value );
		}

		public override string ToString()
		{
			var okToTruncate = false;

			var comment = Comment;

			if ( !String.IsNullOrEmpty( Units ) )
			{
				comment = String.Format( "[{0}]{1}{2}", Units, !String.IsNullOrEmpty( comment) ? " " : "", comment );
			}

			var sb = new StringBuilder();

			if ( IsCommentRecord() )
			{
				sb.AppendFormat( "COMMENT  {0}", Comment );
			}
			else if ( String.IsNullOrEmpty( Value ) && String.IsNullOrEmpty( comment ) )
			{
				sb.Append( KeyName );
			}
			else if ( ValueType == HeaderValueType.String )
			{
				var delimitedValue = StrCvt.Convert( Value );

				if ( KeyName.Length <= 8 )
				{
					sb.AppendFormat( "{0,-8}= {1,-19}", KeyName, delimitedValue );
				}
				else
				{
					sb.AppendFormat( "HIERARCH {0} = {1}", KeyName, delimitedValue );

					okToTruncate = true;
				}
			}
			else
			{ 
				if ( KeyName.Length <= 8 )
				{
					sb.AppendFormat( "{0,-8}= {1,19}", KeyName, Value );
				}
				else
				{
					sb.AppendFormat( "HIERARCH {0} = {1}", KeyName, Value );
				}
			}

			if ( sb.Length > 80 )
			{
				if ( okToTruncate )
				{
					// Here we have a string value so it is OK to truncate the record to 80 characters, including
					// the closing delimiter.

					sb.Length = 79;
					sb.Append( "'" );
				}
			}
			else if ( !String.IsNullOrEmpty( comment ) && sb.Length + 4 < 80 ) // Add any comment as long as there is room to at least start it.
			{ 
				sb.AppendFormat( " / {0}", comment );

				if ( sb.Length > 80 )
				{
					sb.Length = 80;
				}
			}

			return sb.ToString();
		}
	}
}
