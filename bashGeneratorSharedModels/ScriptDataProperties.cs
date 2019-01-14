using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;

namespace bashWizardShared
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class ScriptData
    {
        private bool _updateProperties = false;
        public bool UpdateOnPropertyChanged
        {
            get => _updateProperties;
            set => _updateProperties = value;
        }


        private string _ScriptName = "";
        [JsonProperty]
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
                    bool oldGenerateBashScript = GenerateBashScript;
                    GenerateBashScript = false; // we don't generate a bash script when updating the text -- instead user clicks on "Refresh"
                    _bashScript = value;
                    //
                    //  whever the bash script changes, we might have changed one of these parameters...
                    
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("LoggingSupport");
                    NotifyPropertyChanged("AcceptsInputFile");
                    NotifyPropertyChanged("CreateVerifyDelete");
                    NotifyPropertyChanged("Warnings");
                    NotifyPropertyChanged("JSON");
                    GenerateBashScript = oldGenerateBashScript;

                }
            }
        }
        [JsonProperty]
        public bool LoggingSupport => ParameterExists("log-directory", "logDirectory");
        [JsonProperty]
        public bool AcceptsInputFile => ParameterExists("input-file", "inputFile");

        public string Warnings
        {
            get
            {
                string ret = "";
                foreach (var w in ParseErrors)
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
        [JsonProperty]
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




        private string _Version = "0.905";
        [JsonProperty]
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
        [JsonProperty]
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
        [JsonProperty]
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

      

        private ObservableCollection<ParseErrorInfo> _parseErrors = new ObservableCollection<ParseErrorInfo>();
        public ObservableCollection<ParseErrorInfo> ParseErrors
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

        private string _json = "";
        public string JSON
        {
            get => _json;
            set
            {
                //  Debug.WriteLine($"JSON.set: {value}");
                if (_json != value)
                {
                    _json = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool HasFatalErrors
        {
            get
            {
                foreach (var err in _parseErrors)
                {
                    if (err.ErrorLevel == ErrorLevel.Fatal)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool HasValidationErrors
        {
            get
            {
                foreach (var err in _parseErrors)
                {
                    if (err.ErrorLevel == ErrorLevel.Validation)
                    {
                        return true;
                    }
                }
                return false;
            }
        }




    }

}