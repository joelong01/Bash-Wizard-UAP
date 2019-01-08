using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace bashWizardShared
{
    
    public partial class ScriptData : INotifyPropertyChanged
    {
        private bool GenerateBashScript = false; 
        public ScriptData()
        {
            UpdateOnPropertyChanged = false;
            try
            {
                Init();
            }
            finally
            {
                UpdateOnPropertyChanged = true;
            }

        }
        public ScriptData(string name, IEnumerable<ParameterItem> list, bool createLogFile, bool acceptsInputFile, bool createVerifyDeletePattern, string description, string userCode)
        {
            UpdateOnPropertyChanged = false;
            try
            {
                Init();
                ScriptName = name;
                if (list != null)
                {
                    Parameters.AddRange(list);
                }
                SetCreateLogDirectory(createLogFile);
                SetCreateVerifyDelete(createVerifyDeletePattern);
                SetAcceptsInputFile(acceptsInputFile);                
                Description = description;
                UserCode = userCode;
            }
            finally
            {
                UpdateOnPropertyChanged = true;
                NotifyPropertyChanged("BashScript");
            }
        }

        private void Init()
        {
            this.PropertyChanged += ParameterOrScriptData_PropertyChanged;
            this.Parameters.CollectionChanged += Parameters_CollectionChanged;
        }

        /// <summary>
        ///     We subscribe to the ObservableCollection<ParameterItem> collection changed event so that we can 
        ///     update the UI whenever a parameter is added or removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            // sender is the collection
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ParameterItem p in e.NewItems)
                {
                    p.PropertyChanged += ParameterOrScriptData_PropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ParameterItem p in e.OldItems)
                {
                    p.PropertyChanged -= ParameterOrScriptData_PropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var p in Parameters)
                {
                    p.PropertyChanged -= ParameterOrScriptData_PropertyChanged;
                }
            }

            if (UpdateOnPropertyChanged)
            {
                ToBash();
            }


        }


        //
        //  when any property changes -- either on the ScriptData object *or* on the ParameterItem object, update the bash file
        //
        //  you can change the parameter data inside this fuction, but what I tried to do was change only things that require
        //  global knowledge in this fuction (e.g. auto picking a short name needs knowledge of all the short names).  if you 
        //  just need information local to the parameter, change it in the property setters.
        //
        private void ParameterOrScriptData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!UpdateOnPropertyChanged)
            {
                return;
            }
            ValidationErrorList.Clear();
            if (e.PropertyName == "LongParameter")
            {
                ParameterItem item = sender as ParameterItem;
                if (item.ShortParameter == "") // dont' pick one if the user already did...
                {
                    for (int i = 0; i < item.LongParameter.Length; i++)
                    {

                        item.ShortParameter = item.LongParameter.Substring(i, 1);
                        if (item.ShortParameter == "")
                        {
                            continue;
                        }
                        if (ValidateParameters())
                        {
                            break;
                        }
                    }
                    if (!ValidateParameters())
                    {
                        item.ShortParameter = ""; // can't find a short name automatically                        
                    }
                }
                if (item.VariableName == "")
                {
                    string[] tokens = item.LongParameter.Split(new char[] { '-' });

                    item.VariableName = tokens[0].ToLower();
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        item.VariableName += tokens[i][0].ToString().ToUpper() + tokens[i].Substring(1);
                    }
                }
            }

            if (e.PropertyName != "BashScript" && e.PropertyName != "UserCode") // don't update the BashScript when we are updating the BashScript...
            {
                ToBash();
            }

        }

        private void Reset()
        {
            ParseErrorList.Clear();
            Parameters.Clear();
            ScriptName = "";
            Description = "";

        }

        public void StripLeadingAndTrailingSpaces()
        {
            foreach (ParameterItem parameter in Parameters)
            {
                parameter.Default = parameter.Default.Trim();
                parameter.Description = parameter.Description.Trim();
                parameter.LongParameter = parameter.LongParameter.Trim();
                parameter.ShortParameter = parameter.ShortParameter.Trim();
                parameter.ValueIfSet = parameter.ValueIfSet.Trim();
                parameter.VariableName = parameter.VariableName.Trim();
            }
        }

       

        /// <summary>
        ///     this is like compiler errors/warnings when generating a bash script
        /// </summary>
        /// <returns></returns>
        public bool ValidateParameters()
        {
            //verify short names are unique
            HashSet<string> shortNames = new HashSet<string>();
            HashSet<string> longNames = new HashSet<string>();
            ValidationErrorList.Clear();
            foreach (ParameterItem param in Parameters)
            {
                if (param.ShortParameter == "" && param.LongParameter == "")
                {
                    continue; // probably just getting started
                }

                if (!shortNames.Add(param.ShortParameter))
                {
                    ValidationErrorList.Add($"{param.ShortParameter} exists at least twice.  please fix it.");
                    break;
                }
                if (!longNames.Add(param.LongParameter))
                {
                    ValidationErrorList.Add($"{param.LongParameter} exists at least twice.  please fix it.");
                    break;
                }

                if (!param.RequiresInputString && param.ValueIfSet.Trim(new char[] { ' ' }) == "$2")
                {
                    ValidationErrorList.Add($"Parameter {param.LongParameter} has \"Require Input String\" set to False and the \"Value if Set\" to \"$2\".  \nThis combination is not allowed.");
                    break;
                }

            }

            if (Parameters.Count == 0)
            {
                ValidationErrorList.Add("We won't create a script with zero Parameters");
            }

            if (ScriptName.IndexOf(":") != -1)
            {
                ValidationErrorList.Add($"Script names cannot have : in them.");
            }


            return ValidationErrorList.Count == 0;

        }
       

        private ParameterItem FindParameterByVarName(string varName)
        {
            foreach (var param in Parameters)
            {
                if (param.VariableName == varName)
                {
                    return param;
                }
            }
            return null;
        }

        private ParameterItem FindParameterByLongName(string longName)
        {
            foreach (var param in Parameters)
            {
                if (param.LongParameter == longName)
                {
                    return param;
                }
            }
            return null;
        }



        /// <summary>
        ///     return a string between start and end from the string "from"
        /// </summary>
        /// <param name="from"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="value"></param>
        /// <returns></returns>

        private bool GetStringBetween(string from, string start, string end, out string value)
        {
            int startIndex = from.IndexOf(start);
            if (startIndex == -1)
            {
                value = $"Bash Wizard requires a {start} line and it could not be found";
                return false;
            }
            startIndex += start.Length;
            int endIndex = from.IndexOf(end, startIndex);
            if (endIndex == -1)
            {
                value = $"Bash Wizard requires a {end} line after the {start} line and it could not be found";
                return false;
            }

            value = from.Substring(startIndex, endIndex - startIndex);
            return true;

        }

        /// <summary>
        /// this so we don't have to use \t.  instead replace them with 4 spaces.
        /// 
        /// </summary>
        /// <param name="n" >The number of 'tabs' to return</param>
        /// <returns>A string that has 4*n spaces</returns>
        private string Tabs(int n)
        {
            return "".PadLeft(n * 4);
        }
        
        public static bool FunctionExists(string bashScript, string name)
        {
            if (bashScript == "")
            {
                return false;
            }

            if (bashScript.IndexOf($"function {name}() {{") != -1)
            {
                return true;
            }


            return false;
        }

        /// <summary>
        ///     go through all the parameters and find the one that has the longest "LongParameter" used for lining up the display output
        ///     
        /// </summary>
        /// <returns></returns>
        private int GetLongestLongParameter()
        {
            int max = 0;
            foreach (ParameterItem param in Parameters)
            {
                if (param.LongParameter.Length > max)
                {
                    max = param.LongParameter.Length;
                }
            }
            return max;
        }

        private void SetCreateLogDirectory(bool value)
        {
            ParameterItem logParameter = new ParameterItem()
            {
                LongParameter = "log-directory",
                ShortParameter = "l",
                Description = "directory for the log file.  the log file name will be based on the script name",
                VariableName = "logDirectory",
                Default = "\"./\"",
                RequiresInputString = true,
                RequiredParameter = false,
                ValueIfSet = "$2"
            };

            AddOptionParameter(logParameter, value);

        }

        private void SetAcceptsInputFile(bool newValue)
        {

            // i is the short name and input-file is the long name for the 
            ParameterItem param = new ParameterItem()
            {
                ShortParameter = "i",
                LongParameter = "input-file",
                VariableName = "inputFile",
                Description = "filename that contains the JSON values to drive the script.  command line overrides file",
                RequiresInputString = true,
                Default = "", // this needs to be empty because if it is set, the script will try to find the file...
                RequiredParameter = false,
                ValueIfSet = "$2"
            };

            AddOptionParameter(param, newValue);

        }

        private (bool ret, string msg) SetCreateVerifyDelete(bool addParameter)
        {
            string msg = "";
            if (!addParameter) // deselecting
            {
                string[] functions = new string[] { "onVerify", "onCreate", "onDelete" };
                string err = "";

                foreach (string f in functions)
                {
                    if (FunctionExists(UserCode, f))
                    {
                        err += f + "\n";
                    }
                }

                if (err != "")
                {

                    msg = $"You can unselected the Create, Verify, Delete pattern, but you have the following functions implemented:\n{err}\n\nManually fix the user code to not need these functions before removing this option.";
                    return (ret: false, msg: msg);
                    
                }
            }



            ParameterItem param = new ParameterItem()
            {
                LongParameter = "create",
                ShortParameter = "c",
                VariableName = "create",
                Description = "creates the resource",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, addParameter);
            param = new ParameterItem()
            {
                LongParameter = "verify",
                ShortParameter = "v",
                VariableName = "verify",
                Description = "verifies the script ran correctly",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, addParameter);
            param = new ParameterItem()
            {
                LongParameter = "delete",
                ShortParameter = "d",
                VariableName = "delete",
                Description = "deletes whatever the script created",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, addParameter);
            return (ret: true, msg: "");

        }

        private void AddOptionParameter(ParameterItem item, bool add)
        {
            

            ParameterItem param = null;
            foreach (ParameterItem p in Parameters)
            {
                if (p.VariableName == item.VariableName)
                {
                    param = p;
                    break;
                }
            }

            //
            //  need to have the right parameter for long line to work correctly -- make sure it is there, and if not, add it.
            if (add && param == null)
            {
                Parameters.Add(item);

            }
            else if (!add && param != null)
            {
                Parameters.Remove(param);
            }

        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!_updateProperties)
            {
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }


    public static class EmbeddedResource
    {
        public static string GetResourceFile(Assembly assembly, string resName)
        {
            try
            {

                string resourceName = "";
                string[] resourceNames = assembly.GetManifestResourceNames();
                foreach (string res in resourceNames)
                {
                    if (res.EndsWith(resName))
                    {
                        resourceName = res;
                        break;
                    }
                }

                if (resourceName == "")
                {
                    throw new Exception();
                }
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return result;
                }
            }
            catch
            {
                throw new Exception($"Failed to read Embedded Resource {resName}");
            }
        }
    }

}
