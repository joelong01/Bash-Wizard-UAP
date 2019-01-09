using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace BashWizard
{
    public class ParseWarningToTextConverter: IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ObservableCollection<string> warnings = value as ObservableCollection<string>;
            string ret = "";

            for (int i = 0; i< warnings.Count(); i++)
            {

                ret += $"{i}:  {warnings[i]}\n";
            }
            if (ret == "")
            {
                ret = "No Warnings!";
            }
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // oneway only
        }

    }
    public class ListCountPlusStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string toAdd = parameter as string;

            if (value == null || toAdd == null)
            {
                return "Bad XAML";
            }
            if (!(value is ObservableCollection<string> list))
            {
                return $"{toAdd} (0)";
            }
            return $"{toAdd} ({list.Count})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException(); // oneway only
        }

    }



}
