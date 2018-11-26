using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bashGeneratorSharedModels
{

    public class ParameterItem : INotifyPropertyChanged
    {

        public override string ToString()
        {
            return $"{LongParameter}: {Description}";
        }



        private string _ShortParam = "";
        public string ShortParameter
        {
            get => _ShortParam;
            set
            {
                if (_ShortParam != value)
                {
                    value = value.TrimStart(new char[] { '-' });
                    _ShortParam = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _LongParam = "";
        public string LongParameter
        {
            get => _LongParam;
            set
            {
                if (_LongParam != value)
                {
                    value = value.TrimStart(new char[] { '-' });
                    _LongParam = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _Description = "";
        public string Description
        {
            get => _Description;
            set
            {
                if (_Description != value)
                {
                    _Description = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _VarName = "";
        public string VariableName
        {
            get => _VarName;
            set
            {
                if (_VarName != value)
                {
                    _VarName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _RequiresInputString = true;
        public bool RequiresInputString
        {
            get => _RequiresInputString;
            set
            {
                if (_RequiresInputString != value)
                {
                    _RequiresInputString = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _Default = "";
        public string Default
        {
            get => _Default;
            set
            {
                if (_Default != value)
                {
                    _Default = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _Required = true;
        public bool RequiredParameter
        {
            get => _Required;
            set
            {
                if (_Required != value)
                {
                    _Required = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _SetVal = "$2";
        public string ValueIfSet
        {
            get => _SetVal;
            set
            {
                if (_SetVal != value)
                {
                    _SetVal = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
