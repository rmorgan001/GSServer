using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Helpers
{
    public class EnumLocalizationTypeConverter : EnumConverter
    {
        private readonly Type _enumType;
        public EnumLocalizationTypeConverter(Type type)
            : base(type)
        {
            _enumType = type;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
        {
            return destType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destType)
        {
            FieldInfo fi = _enumType.GetField(Enum.GetName(_enumType, value));
            DescriptionAttribute dna =
                (DescriptionAttribute) Attribute.GetCustomAttribute(
                    fi, typeof(DescriptionAttribute));

            if (dna != null)
            {
                var localizedDescription = (string)System.Windows.Application.Current.TryFindResource(dna.Description);
                return localizedDescription ?? value.ToString();
            }
            else
            {
                return value.ToString();
            }

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType)
        {
            return false;
        }

    }
}
