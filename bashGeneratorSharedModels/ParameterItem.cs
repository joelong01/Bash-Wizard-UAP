using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bashGeneratorSharedModels
{

    public class ParameterItem : INotifyPropertyChanged
    {

        public override string ToString()
        {
            return $"{LongParam}: {Description}";
        }



        private string _ShortParam = "";
        public string ShortParam
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
        public string LongParam
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
        public string VarName
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

        private bool _AcceptsValue = true;
        public bool AcceptsValue
        {
            get => _AcceptsValue;
            set
            {
                if (_AcceptsValue != value)
                {
                    _AcceptsValue = value;
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
        public bool Required
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
        public string SetVal
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
