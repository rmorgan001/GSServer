using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace GS.Shared.Domain
{
    //Property="{Binding Value, Converter={namespace:DebugExtension}}"
    public class DebugConverter : IValueConverter
    {
        public static readonly DebugConverter Instance = new DebugConverter();
        private DebugConverter() { }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value; //Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value; //Binding.DoNothing;
        }

        #endregion
    }

    public class DebugExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return DebugConverter.Instance;
        }
    }
}
