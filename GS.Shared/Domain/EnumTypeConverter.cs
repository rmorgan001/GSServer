using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace GS.Shared.Domain
{
    public class EnumTypeConverter : EnumConverter
    {
        private readonly Type _numType;
        public EnumTypeConverter(Type type)
            : base(type)
        {
            _numType = type;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            DescriptionAttribute dna = null;
            if (value != null)
            {
                FieldInfo fi = _numType.GetField(Enum.GetName(_numType, value));
                dna =
                    (DescriptionAttribute)Attribute.GetCustomAttribute(
                        fi, typeof(DescriptionAttribute));
            }
            if (dna != null)
                return dna.Description;
            else
                return (value != null ? value.ToString() : "");
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return srcType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            foreach (FieldInfo fi in _numType.GetFields())
            {
                DescriptionAttribute dna =
                    (DescriptionAttribute)Attribute.GetCustomAttribute(
                        fi, typeof(DescriptionAttribute));

                if ((dna != null) && ((string)value == dna.Description))
                    return Enum.Parse(_numType, fi.Name);
            }
            return Enum.Parse(_numType, (string)value ?? string.Empty);
        }
    }
}
