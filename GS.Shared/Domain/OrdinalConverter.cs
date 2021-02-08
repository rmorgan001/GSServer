using System.Windows.Controls;
using System.Windows.Data;

namespace GS.Shared.Domain
{
    public class OrdinalConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            var lvi = value as DataGridRow;
            var ordinal = 0;

            if (lvi != null)
            {
                if (ItemsControl.ItemsControlFromItemContainer(lvi) is DataGrid lv) ordinal = lv.ItemContainerGenerator.IndexFromContainer(lvi) + 1;
            }

            return ordinal;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // This converter does not provide conversion back from ordinal position to list view item
            throw new System.InvalidOperationException();
        }
    }
}
