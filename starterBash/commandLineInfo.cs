using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace starterBash
{
    public class CommandLineInfo : INotifyPropertyChanged
    {
        private readonly string[] SaveNames = { "ShortParam", "LongParam", "Description", "VarName", "AcceptsValue", "Default" };

        public string Serialize()
        {
            return StaticHelpers.SerializeObject<CommandLineInfo>(this, SaveNames, "=", "\n\r");
        }

        public void Deserialize(string s)
        {
            this.DeserializeObject<CommandLineInfo>(s, "=", "\n\r");
        }
        public CommandLineInfo(string s)
        {
            Deserialize(s);
        }

        public CommandLineInfo() { }


        private string _ShortParam = "";
        public string ShortParam
        {
            get => _ShortParam;
            set
            {
                if (_ShortParam != value)
                {
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
