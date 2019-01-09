using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                    _bashScript = value;
                    NotifyPropertyChanged();
                    //
                    //  whever the bash script changes, we might have changed one of these parameters...
                    GenerateBashScript = false;
                    NotifyPropertyChanged("LoggingSupport");
                    NotifyPropertyChanged("AcceptsInputFile");
                    NotifyPropertyChanged("CreateVerifyDelete");
                    NotifyPropertyChanged("Warnings");
                    NotifyPropertyChanged("JSON");
                    GenerateBashScript = true;

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




        private string _Version = "0.900";
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

        private ObservableCollection<string> _parseErrors = new ObservableCollection<string>();
        public ObservableCollection<string> ParseErrors
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
        string _json = "";
        public string JSON
        {
            get
            {
                return _json;
            }
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


    }

}