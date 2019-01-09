using System;
using System.Reflection;
using System.Text;

namespace bashWizardShared
{
    public partial class ScriptData
    {
        /// <summary>
        ///     the parameters we support that add built in functionality
        /// </summary>
        public enum BashWizardParameter { LoggingSupport, InputFile, CreateVerifyDelete };

        /// <summary>
        ///     given a BashWizardParameter turn the functionality on or off
        /// </summary>
        /// <param name="paramName">a BashWizard enum value of the property to set </param>
        /// /// <param name="set">turn the feature on or off </param>
        /// <returns>true if the API was able to do the action specified, otherwise false </returns>
        public (bool retVal, string msg) SetBuiltInParameter(BashWizardParameter paramName, bool set)
        {
            bool ret = false;
            string msg = "";
            switch (paramName)
            {
                case BashWizardParameter.LoggingSupport:
                    SetCreateLogDirectory(set);
                    ret = true;
                    break;
                case BashWizardParameter.InputFile:
                    SetAcceptsInputFile(set);
                    ret = true;
                    break;
                case BashWizardParameter.CreateVerifyDelete:
                    (ret, msg) = SetCreateVerifyDelete(set);
                    break;
                default:
                    break;
            }
            //
            //  you don't call ToBash() because modifying the Parameters collection will update the bash script
            //  

            return (ret, msg);
        }

        /// <summary>
        ///     Converts the parameters to a bash script.  
        /// </summary>
        /// <remarks>
        ///     The overall implementation strategy is to put as much as possible into "templates",
        ///     stored in resource files.  we then replace strings in the templates with strings we generate based on the parameters.  there are
        ///     three "phases": 1) loop through the parameters and build each string we need 2) fix up the strings we built - e.g. remove end of 
        ///     line characters and 3) use StringBuilder.Replace() to put the strings into the right place in the bash file.
        /// </remarks>
        /// <returns>true on success, false on failure</returns>
        public bool ToBash()
        {
            if (!GenerateBashScript)
            {
                return false;
            }

            if (this.Parameters.Count == 0)
            {
                //
                //  if there are no parameters, just mark it as user code
                GenerateBashScript = false;
                this.BashScript = "# --- BEGIN USER CODE ---\n" + this.UserCode + "\n# --- END USER CODE ---";
                GenerateBashScript = true;
                return true;

            }


            if (!ValidateParameters())
            {
                this.BashScript = ValidationErrors;
                return false;
            }


            string nl = "\n";

            StringBuilder sbBashScript = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "bashTemplate.sh"));
            StringBuilder logTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "logTemplate.sh"));
            StringBuilder parseInputTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "parseInputTemplate.sh"));
            StringBuilder requiredVariablesTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "requiredVariablesTemplate.sh"));
            StringBuilder verifyCreateDeleteTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "verifyCreateDeleteTemplate.sh"));
            StringBuilder endLogTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "endLogTemplate.sh"));
            StringBuilder usageLine = new StringBuilder($"{Tabs(1)}echo \"{this.Description}\"\n{Tabs(1)}echo \"\"\n{Tabs(1)}echo \"Usage: $0 ");
            StringBuilder usageInfo = new StringBuilder($"{Tabs(1)}echo \"\"\n");
            StringBuilder echoInput = new StringBuilder($"\"{ScriptName}:\"{nl}");
            StringBuilder shortOptions = new StringBuilder("");
            StringBuilder longOptions = new StringBuilder("");
            StringBuilder inputCase = new StringBuilder("");
            StringBuilder inputDeclarations = new StringBuilder("");
            StringBuilder parseInputFile = new StringBuilder("");
            StringBuilder requiredFilesIf = new StringBuilder("");
            StringBuilder loggingSupport = new StringBuilder("");
            int longestLongParameter = GetLongestLongParameter() + 4;
            //
            //   phase 1: loop through the parameters and build our strings
            foreach (ParameterItem param in Parameters)
            {
                //
                //  first usage line
                string required = (param.RequiredParameter) ? "Required" : "Optional";
                usageLine.Append($"-{param.ShortParameter}|--{param.LongParameter} ");
                usageInfo.Append($"{Tabs(1)}echo \" -{param.ShortParameter} | --{param.LongParameter.PadRight(longestLongParameter)}{Tabs(1)}{required}{Tabs(1)}{param.Description}\"{nl}");

                //
                // the  echoInput function
                echoInput.Append($"{Tabs(1)}echo -n \"{Tabs(1)}{param.LongParameter.PadRight(longestLongParameter, '.')} \"{nl}");
                echoInput.Append($"{Tabs(1)}echoInfo \"${param.VariableName}\"{nl}");

                //
                //  OPTIONS, LONGOPTS
                string colon = (param.RequiresInputString) ? ":" : "";
                shortOptions.Append($"{param.ShortParameter}{colon}");
                longOptions.Append($"{param.LongParameter}{colon},");

                // input Case
                inputCase.Append($"{Tabs(2)}-{param.ShortParameter} | --{param.LongParameter})\n");
                inputCase.Append($"{Tabs(3)}{param.VariableName}={param.ValueIfSet}\n");
                inputCase.Append((param.RequiresInputString) ? $"{Tabs(3)}shift 2\n" : $"{Tabs(3)}shift 1\n");
                inputCase.Append($"{Tabs(3)};;\n");

                // declare variables
                inputDeclarations.Append($"declare {param.VariableName}={param.Default}\n");
                if (this.AcceptsInputFile && param.VariableName != "inputFile")
                {

                    // parse input file
                    parseInputFile.Append($"{Tabs(1)}{param.VariableName}=$(echo \"${{configSection}}\" | jq \'.[\"{param.LongParameter}\"]\' --raw-output)\n");

                }

                // if statement for the required files

                if (param.RequiredParameter)
                {
                    requiredFilesIf.Append($"[ -z \"${{{param.VariableName}}}\" ] || ");
                }


            }


            //
            //  phase 2 - fix up any of the string created above         

            usageLine.Append("\"");

            longOptions.Remove(longOptions.Length - 1, 1);
            inputCase.Remove(inputCase.Length - 1, 1);
            usageInfo.Remove(usageInfo.Length - 1, 1);

            if (requiredFilesIf.Length > 0)
            {
                requiredFilesIf.Remove(requiredFilesIf.Length - 4, 4); // removes the " || " at the end
                requiredVariablesTemplate.Replace("__REQUIRED_FILES_IF__", requiredFilesIf.ToString());
            }
            else
            {
                requiredVariablesTemplate.Clear();
            }

            if (this.LoggingSupport)
            {
                logTemplate.Replace("__LOG_FILE_NAME__", this.ScriptName + ".log");

            }
            else
            {
                logTemplate.Clear();
            }

            //
            //  phase 3 - replace the strings in the templates
            sbBashScript.Replace("__USAGE_LINE__", usageLine.ToString());
            sbBashScript.Replace("__USAGE__", usageInfo.ToString());
            sbBashScript.Replace("__ECHO__", echoInput.ToString());
            sbBashScript.Replace("__SHORT_OPTIONS__", shortOptions.ToString());
            sbBashScript.Replace("__LONG_OPTIONS__", longOptions.ToString());
            sbBashScript.Replace("__INPUT_CASE__", inputCase.ToString());
            sbBashScript.Replace("__INPUT_DECLARATION__", inputDeclarations.ToString());

            string inputOverridesRequired = (this.AcceptsInputFile) ? "echoWarning \"Parameters can be passed in the command line or in the input file.  The command line overrides the setting in the input file.\"" : "";
            sbBashScript.Replace("__USAGE_INPUT_STATEMENT__", inputOverridesRequired);

            if (parseInputFile.Length > 0)
            {
                parseInputTemplate.Replace("__SCRIPT_NAME__", this.ScriptName);
                parseInputTemplate.Replace("__FILE_TO_SETTINGS__", parseInputFile.ToString());
                sbBashScript.Replace("__PARSE_INPUT_FILE__", parseInputTemplate.ToString());
            }
            else
            {
                sbBashScript.Replace("__PARSE_INPUT_FILE__", "");
            }

            sbBashScript.Replace("__REQUIRED_PARAMETERS__", requiredVariablesTemplate.ToString());
            sbBashScript.Replace("__LOGGING_SUPPORT_", logTemplate.ToString());
            sbBashScript.Replace("__END_LOGGING_SUPPORT__", this.LoggingSupport ? endLogTemplate.ToString() : "");

            if (this.CreateVerifyDelete)
            {
                if (!ScriptData.FunctionExists(this.UserCode, "onVerify") && !ScriptData.FunctionExists(this.UserCode, "onDelete") && !ScriptData.FunctionExists(this.UserCode, "onCreate"))
                {
                    //
                    //  if they don't have the functions, add the template code
                    sbBashScript.Replace("__USER_CODE_1__", verifyCreateDeleteTemplate.ToString());
                }
            }
            //
            // put the user code where it belongs -- it might contain the functions already
            sbBashScript.Replace("__USER_CODE_1__", this.UserCode);
            sbBashScript.Replace("\r", string.Empty);
            this.BashScript = sbBashScript.ToString();
            ValidationErrorList.Clear();
            return true;


        }

        /// <summary>
        ///     this generates the JSON that this script needs for an input file
        /// </summary>
        /// <returns></returns>
        public string GetInputJson()
        {

            //       we want something like
            //   "__SCRIPT__NAME__ : {
            //      "longParameter": "Default"
            //  }


            string nl = "\n";


            StringBuilder sb = new StringBuilder($"{Tabs(1)}\"{ScriptName}\": {{{nl}");

            string paramKeyValuePairs = "";
            char[] quotes = { '"' };
            char[] commadNewLine = { ',', '\n', ' ' };
            foreach (ParameterItem param in Parameters)
            {
                string defValue = param.Default;
                defValue = defValue.TrimStart(quotes);
                defValue = defValue.TrimEnd(quotes);
                defValue = defValue.Replace("\\", "\\\\");
                paramKeyValuePairs += $"{Tabs(2)}\"{param.LongParameter}\": \"{defValue}\",{nl}";

            }
            //  delete trailing "," "\n" and spaces
            paramKeyValuePairs = paramKeyValuePairs.TrimEnd(commadNewLine);
            sb.Append(paramKeyValuePairs);

            sb.Append($"{nl}{Tabs(1)}}}");


            return sb.ToString();

        }

        /// <summary>
        ///     generate the JSON needed for VS Code debug config when using the Bash Debug extension
        /// </summary>
        /// <param name="scriptDirectory"></param>
        /// <returns></returns>
        public string VSCodeDebugInfo(string scriptDirectory)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                string scriptDir = scriptDirectory;
                string scriptName = this.ScriptName;
                char[] slashes = new char[] { '/', '\\' };
                char[] quotes = new char[] { '\"', '\'' };
                scriptDir = scriptDir.TrimEnd(slashes).TrimStart(new char[] { '.', '/' }).TrimEnd(slashes);
                scriptName = scriptName = scriptName.TrimStart(slashes);
                string nl = "\n";
                sb.Append($"{{{nl}");
                sb.Append($"{Tabs(1)}\"type\": \"bashdb\",{nl}");
                sb.Append($"{Tabs(1)}\"request\": \"launch\",{nl}");
                sb.Append($"{Tabs(1)}\"name\": \"Debug {this.ScriptName}\",{nl}");
                sb.Append($"{Tabs(1)}\"cwd\": \"${{workspaceFolder}}\",{nl}");

                sb.Append($"{Tabs(1)}\"program\": \"${{workspaceFolder}}/{scriptDir}/{scriptName}\",{nl}");
                sb.Append($"{Tabs(1)}\"args\": [{nl}");
                foreach (ParameterItem param in Parameters)
                {
                    sb.Append($"{Tabs(2)}\"--{param.LongParameter}\",{nl}{Tabs(2)}\"{param.Default.TrimStart(quotes).TrimEnd(quotes)}\",{nl}");
                }


                sb.Append($"{Tabs(1)}]{nl}");
                sb.Append($"}}");
            }
            catch (Exception e)
            {
                return $"Exception generating config\n\nException Info:\n===============\n{e.Message}";
            }

            return sb.ToString();
        }



        /// <summary>
        ///     Given a bash file, create a ScriptData object.  This is the "parse a bash script" function
        /// </summary>
        /// <param name="bash"></param>
        public static ScriptData FromBash(string bash)
        {
            ScriptData scriptData = new ScriptData();

            try
            {

                scriptData.UpdateOnPropertyChanged = false; // this flag stops the NotifyPropertyChanged events from firing
                scriptData.GenerateBashScript = true;  // this flag tells everything that we are in the process of parsing
                bash = bash.Replace("\r", string.Empty);

                //
                //  first look for the bash version
                string versionLine = "# bashWizard version ";
                int index = bash.IndexOf(versionLine);
                double userBashVersion = 0.1;
                string[] lines = null;
                string line = "";
                int count = 0;
                if (index > 0)
                {
                    double.TryParse(bash.Substring(index + versionLine.Length, 5), out userBashVersion);
                }
                else
                {
                    //
                    //  see if it is a BashWizard by looking for the old comments


                    if (scriptData.GetStringBetween(bash, "# --- END OF BASH WIZARD GENERATED CODE ---", "# --- YOUR SCRIPT ENDS HERE ---", out string code) == false)
                    {
                        scriptData.ParseErrorList.Add("The Bash Wizard couldn't find the version of this file and it doesn't have the old comment delimiters.  Not a Bash Wizard file.");
                    }
                    else
                    {
                        scriptData.UserCode = code.Trim();
                    }

                }

                if (scriptData.UserCode == "") // not an old style script...
                {
                    bool ret = scriptData.GetStringBetween(bash, "# --- BEGIN USER CODE ---", "# --- END USER CODE ---", out string userCode); // if this is the second time through with one big bash file, this strips the comments so we don't keep adding them
                    if (!ret)
                    {
                        scriptData.UserCode = bash.Trim(); // make it all user code                        
                        scriptData.ParseErrorList.Add("Missing the comments around the user's code.  User Code starts after \"# --- BEGIN USER CODE ---\" and ends before \"# --- END USER CODE ---\" ");
                        scriptData.ParseErrorList.Add("Adding comments and treating the whole file as user code");
                        return scriptData;
                    }
                    
                    scriptData.UserCode = userCode.Trim();
                    
                    
                }

                //
                //  find the usage() function and parse it out - this gives us the 4 properties in the ParameterItem below
                if (scriptData.GetStringBetween(bash, "usage() {", "}", out string bashFragment) == false)
                {
                    scriptData.ParseErrorList.Add(bashFragment);

                }
                else
                {
                    bashFragment = bashFragment.Replace("echoWarning", "echo");
                    bashFragment = bashFragment.Replace("\n", "");
                    lines = bashFragment.Split(new string[] { "echo ", "\"" }, StringSplitOptions.RemoveEmptyEntries);
                    line = "";
                    count = 0;
                    foreach (string l in lines)
                    {
                        line = l.Trim();
                        if (line == "")
                        {
                            continue;
                        }
                        if (line == "exit 1")
                        {
                            break;
                        }

                        count++;
                        if (count == 2)
                        {
                            //
                            //  we write a Warning line, then the description, then instructions
                            //  strip trailing quote and spaces

                            if (!line.StartsWith("Usage: $0")) // to protect from blank Descriptions
                            {
                                scriptData.Description = line.TrimEnd();
                            }
                            continue;
                        }

                        if (line.Substring(0, 1) == "-") // we have a parameter!
                        {
                            string[] paramTokens = line.Split(new string[] { " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
                            string description = "";
                            for (int i = 3; i < paramTokens.Length; i++)
                            {
                                description += paramTokens[i] + " ";
                            }
                            description = description.Trim();
                            ParameterItem parameterItem = new ParameterItem()
                            {
                                ShortParameter = paramTokens[0].Trim(),
                                LongParameter = paramTokens[1].Trim(),
                                RequiredParameter = (paramTokens[2].Trim() == "Required") ? true : false,
                                Description = description
                            };


                            scriptData.Parameters.Add(parameterItem);
                        }
                    }
                }

                //
                //  parse the echoInput() function to get script name - dont' fail parsing on this one
                bashFragment = "";
                if (scriptData.GetStringBetween(bash, "echoInput() {", "parseInput()", out bashFragment))
                {
                    lines = bashFragment.Split('\n');
                    foreach (string l in lines)
                    {
                        line = l.Trim();
                        if (line == "")
                        {
                            continue;
                        }
                        //
                        //  the line is in the form of: "echo "<scriptName>:"
                        if (scriptData.GetStringBetween(line, "echo \"", ":", out string name))
                        {
                            scriptData.ScriptName = name;
                        }
                        break;
                    }
                }


                //
                //  next parse out the "parseInput" function to get "valueWhenSet" and the "VariableName"

                bashFragment = "";
                if (scriptData.GetStringBetween(bash, "eval set -- \"$PARSED\"", "--)", out bashFragment) == false)
                {
                    scriptData.ValidationErrorList.Add(bashFragment);

                }
                else
                {

                    lines = bashFragment.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (index = 0; index < lines.Length; index++)
                    {
                        line = lines[index].Trim();
                        if (line == "")
                        {
                            continue;
                        }

                        if (line.Substring(0, 1) == "-") // we have a parameter!
                        {
                            string[] paramTokens = lines[index + 1].Trim().Split(new char[] { '=' });
                            if (paramTokens.Length != 2)
                            {
                                scriptData.ParseErrorList.Add($"When parsing the parseInput() function to get the variable names, encountered the line {lines[index + 1].Trim()} which doesn't parse.  It should look like varName=$2 or the like.");

                            }
                            string[] nameTokens = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            if (nameTokens.Length != 2) // the first is the short param, second long param, and third is empty
                            {
                                scriptData.ParseErrorList.Add($"When parsing the parseInput() function to get the variable names, encountered the line {lines[index].Trim()} which doesn't parse.  It should look like \"-l | --long-name)\" or the like.");
                            }
                            // nameTokens[1] looks like "--long-param)
                            string longParam = nameTokens[1].Substring(3, nameTokens[1].Length - 4);
                            ParameterItem param = scriptData.FindParameterByLongName(longParam);
                            if (param == null)
                            {
                                scriptData.ParseErrorList.Add($"When parsing the parseInput() function to get the variable names, found a long parameter named {longParam} which was not found in the usage() function");
                            }
                            else
                            {
                                param.VariableName = paramTokens[0].Trim();
                                param.ValueIfSet = paramTokens[1].Trim();
                                if (lines[index + 2].Trim() == "shift 1")
                                {
                                    param.RequiresInputString = false;
                                }
                                else if (lines[index + 2].Trim() == "shift 2")
                                {
                                    param.RequiresInputString = true;
                                }
                                else
                                {
                                    scriptData.ParseErrorList.Add($"When parsing the parseInput() function to see if {param.VariableName} requires input, found this line: {lines[index + 1]} which does not parse.  it should either be \"shift 1\" or \"shift 2\"");
                                }
                            }
                            index += 2;
                        }
                    }
                }
                // the last bit of info to suss out is the default value -- find these with a comment
                if (scriptData.GetStringBetween(bash, "# input variables", "parseInput", out bashFragment) == false)
                {
                    scriptData.ParseErrorList.Add(bashFragment);
                }
                else
                {
                    // throw away the "declare "
                    bashFragment = bashFragment.Replace("declare ", "");
                    lines = bashFragment.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string l in lines)
                    {
                        line = l.Trim();
                        if (line == "")
                        {
                            continue;
                        }
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        string[] varTokens = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        if (varTokens.Length == 0 || varTokens.Length > 2)
                        {
                            scriptData.ParseErrorList.Add($"When parsing the variable declarations between the \"# input variables\" comment and the \"parseInput\" calls, the line {line} was encountered that didn't parse.  it should be in the form of varName=Default");

                        }
                        string varName = varTokens[0].Trim();
                        ParameterItem param = scriptData.FindParameterByVarName(varName);
                        if (param == null)
                        {
                            scriptData.ParseErrorList.Add($"When parsing the variable declarations between the \"# input variables\" comment and the \"parseInput\" calls, found a variable named {varName} which was not found in the usage() function");

                        }
                        else
                        {
                            param.Default = varTokens.Length == 2 ? varTokens[1].Trim() : "";  // in bash "varName=" is a valid declaration
                        }

                    }
                }


                return scriptData;
            }
            finally
            {
                //
                //  need to update everything that might have been changed by the parse
                scriptData.UpdateOnPropertyChanged = true; // force events to fire
                scriptData.NotifyPropertyChanged("Description");
                scriptData.NotifyPropertyChanged("ScriptName");


                //  "BashScript" also updates the ToggleButtons
                scriptData.GenerateBashScript = true; // setting this here makes it so we don't generate the script when we change the Description and the Name
                scriptData.NotifyPropertyChanged("BashScript");




            }



        }
    }
}
