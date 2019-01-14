using System;
using System.Collections.ObjectModel;
using System.Linq;
using bashWizardShared;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace BashWizard
{
    public class OrdinalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int ordinal = 0;

            if (value is ListViewItem lvi)
            {
                ListView lv = ItemsControl.ItemsControlFromItemContainer(lvi) as ListView;
                ordinal = lv.IndexFromContainer(lvi) + 1;
            }

            return ordinal;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // This converter does not provide conversion back from ordinal position to list view item
            throw new System.InvalidOperationException();
        }
    }

    public class CountPlusString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string toAdd = parameter as string;


            if (value == null || toAdd == null)
            {
                return "Bad XAML";
            }
            if (!(value is int count))
            {
                return $"{toAdd} (0)";
            }
            return $"{toAdd} ({count})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // oneway only
        }

    }



}
