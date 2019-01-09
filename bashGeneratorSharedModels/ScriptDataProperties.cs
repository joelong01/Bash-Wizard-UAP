using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace bashWizardShared
{
    public partial class ScriptData
    {
        private bool _updateProperties = false;
        public bool UpdateOnPropertyChanged
        {
            get => _updateProperties;
            set => _updateProperties = value;
        }


        private string _ScriptName = "";
        public string ScriptName
        {
            get => _ScriptName;
            set
            {
                if (_ScriptName != value)
                {
                    _ScriptName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _bashScript = "";
        public string BashScript
        {
            get => _bashScript;
            set
            {
                if (_bashScript != value)
                {
                    _bashScript = value;
                    NotifyPropertyChanged();
                    //
                    //  whever the bash script changes, we might have changed one of these parameters...
                    GenerateBashScript = false;
                    NotifyPropertyChanged("LoggingSupport");
                    NotifyPropertyChanged("AcceptsInputFile");
                    NotifyPropertyChanged("CreateVerifyDelete");
                    NotifyPropertyChanged("Warnings");
                    GenerateBashScript = true;

                }
            }
        }

        public bool LoggingSupport => ParameterExists("log-directory", "logDirectory");
        public bool AcceptsInputFile => ParameterExists("input-file", "inputFile");

        public string Warnings
        {
            get
            {
                string ret = "";
                foreach (var w in ParseErrorList)
                {
                    ret += w + "\n";
                }
                if (ret == "")
                {
                    ret = "No Warnings!";
                }

                return ret;
            }
        }

        /// <summary>
        ///     returns true if the /create /verify and /delete parameters are in the collection
        /// </summary>
        public bool CreateVerifyDelete
        {
            get
            {
                if (ParameterExists("create", "create") && ParameterExists("verify", "verify") && ParameterExists("delete", "delete"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        private bool ParameterExists(string longName, string varName)
        {
            if (FindParameterByLongName(longName) != null && FindParameterByVarName(varName) != null)

            {
                return true;

            }
            return false;
        }




        private string _Version = "0.900";
        public string Version
        {
            get => _Version;
            set
            {
                if (_Version != value)
                {
                    _Version = value;
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

        private ObservableCollection<ParameterItem> _Parameters = new ObservableCollection<ParameterItem>();
        public ObservableCollection<ParameterItem> Parameters
        {
            get => _Parameters;
            set
            {
                if (_Parameters != value)
                {
                    _Parameters = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string _UserCode = "";
        public string UserCode
        {
            get => _UserCode;
            set
            {
                if (_UserCode != value)
                {
                    _UserCode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private List<string> _validatationErrors = new List<string>();
        private List<string> ValidationErrorList
        {
            get => _validatationErrors;
            set
            {
                if (_validatationErrors != value)
                {
                    _validatationErrors = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string ValidationErrors
        {
            get
            {
                string ret = "Validation Errors\n=================\n";
                for (int i = 0; i < ValidationErrorList.Count; i++)
                {
                    ret += $"{i + 1}. {ValidationErrorList[i]}\n";
                }
                return ret;
            }
        }

        private List<string> _parseErrors = new List<string>();
        private List<string> ParseErrorList
        {
            get => _parseErrors;
            set
            {
                if (_parseErrors != value)
                {
                    _parseErrors = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string ParseErrors
        {
            get
            {
                string ret = "Parse Errors\n============\n";
                for (int i = 0; i < ParseErrorList.Count; i++)
                {
                    ret += $"{i + 1}. {ParseErrorList[i]}\n";
                }
                return ret;
            }
        }

        public string AllErrors => ParseErrors + ValidationErrors;

    }

}