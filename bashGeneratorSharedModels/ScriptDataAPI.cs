using System;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace bashWizardShared
{
    public partial class ScriptData
    {
        //
        //  Error Messages constants used when parsing the Bash file
        private const string unMergedGitFile = "Bash Script has \"<<<<<<< HEAD\" string in it, indicating an un-merged GIT file.  fix merge before opening.";
        private const string noNewLines = "There are no new lines in this file -- please fix this and try again.";
        private const string missingComments = "Missing the comments around the user's code.  User Code starts after \"# --- BEGIN USER CODE ---\" and ends before \"# --- END USER CODE ---\" ";
        private const string addingComments = "Adding comments and treating the whole file as user code";
        private const string missingOneUserComment = "Missing one of the comments around the user's code.  User Code starts after \"# --- BEGIN USER CODE ---\" and ends before \"# --- END USER CODE ---\" ";
        private const string pleaseFix = "Please fix and retry.";
        private const string tooManyUserComments = "There is more than one \"# --- BEGIN USER CODE ---\" or more than one \"# --- END USER CODE ---\" comments in this file.  Please fix and try again.";
        private const string missingVersionInfo = "couldn't find script version information";


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
                this.BashScript = "# --- BEGIN USER CODE ---\n" + this.UserCode + "\n# --- END USER CODE ---";
                return true;

            }

            ValidateParameters();

            string nl = "\n";

            StringBuilder sbBashScript = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "bashTemplate.sh"));
            sbBashScript.Replace("__VERSION__", this.Version);
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
            this.JSON = JsonConvert.SerializeObject(this, Formatting.Indented);
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

        public static ScriptData FromJson(string json, string userCode)
        {
            ScriptData scriptData = new ScriptData();
            bool oldGenerateBashScript = scriptData.GenerateBashScript;
            try
            {


                scriptData = JsonConvert.DeserializeObject<ScriptData>(json);
                //
                //  Serialize is OptIn, deserialize is not.  so these will be reset
                // scriptData.JSON = json;
                scriptData.UserCode = userCode;  
                scriptData.GenerateBashScript = true;
                scriptData.UpdateOnPropertyChanged = false;
                scriptData.ToBash(); //this will set the JSON
                return scriptData;
            }
            catch (Exception e)
            {
                //
                //  the user should expect to have the JSON still there and not lose their code
                //  when they type incorrect JSON - setting these here allows them to fix the error
                //  and get back to the state they were in.
                scriptData.JSON = json;
                scriptData.UserCode = userCode;
                scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, "Exception caught when parsing JSON."));
                scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, $"ExceptionInfo: {e.Message}"));
                return scriptData;
            }
            finally
            {
                scriptData.GenerateBashScript = oldGenerateBashScript;
                scriptData.UpdateOnPropertyChanged = true;
            }
        }

        /// <summary>
        ///     Given a bash file, create a ScriptData object.  This is the "parse a bash script" function
        /// </summary>
        /// <param name="bash"></param>
      
        public static ScriptData FromBash(string input)
        {
            ScriptData scriptData = new ScriptData();
            bool oldGenerateBashScript = scriptData.GenerateBashScript;
            try
            {

                scriptData.ClearParseErrors();
                scriptData.UpdateOnPropertyChanged = false; // this flag stops the NotifyPropertyChanged events from firing
                scriptData.GenerateBashScript = false;  // this flag tells everything that we are in the process of parsing
                scriptData.BashScript = input;
                //
                //  make sure that we deal with the case of getting a file with EOL == \n\r.  we only want \n
                //  I've also had scenarios where I get only \r...fix those too.
                if (input.IndexOf("\n") != -1)
                {
                    //
                    //  we have some new lines, just kill the \r
                    if (input.IndexOf("\r") != -1)
                    {
                        input = input.Replace("\r", string.Empty);

                    }
                }
                else if (input.IndexOf("\r") != -1)
                {
                    // no \n, but we have \r
                    input = input.Replace("\r", "\n");
                }
                else
                {
                    // no \r and no \n
                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, noNewLines));
                    return scriptData;
                }


                //
                // make sure the file doesn't have GIT merge conflicts
                if (input.IndexOf("<<<<<<< HEAD") != -1)
                {
                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, unMergedGitFile));
                    return scriptData;
                }

                /*
                        The general layout of the file is 
                        
                        #!/bin/bash
                        # bashWizard version <version>
                        <BashWizard Functions>
                        # --- BEGIN USER CODE ---

                        # --- END USER CODE ---
                        <optional BashWizard code>
                        
                        the general parse strategy is to separate the user code and then to parse the Bash Wizard Functions to find all the parameter information
                        we *never* want to touch the user code 

                 */

                string[] userComments = new string[] { "# --- BEGIN USER CODE ---", "# --- END USER CODE ---" };
                string[] sections = input.Split(userComments, StringSplitOptions.RemoveEmptyEntries);
                string bashWizardCode = "";
                switch (sections.Length)
                {
                    case 0:

                        //
                        //  this means we couldn't find any of the comments -- treat this as a non-BW file
                        scriptData.UserCode = input.Trim(); // make it all user code                        
                        scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, missingComments));
                        scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, addingComments));
                        return scriptData;
                    case 1:
                        scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, missingOneUserComment));
                        scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, pleaseFix));
                        return scriptData;
                    case 2:
                    case 3:
                        bashWizardCode = sections[0];
                        scriptData.UserCode = sections[1].Trim();
                        // ignore section[2], it is code after the "# --- END USER CODE ---" that will be regenerated
                        break;
                    default:
                        scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, tooManyUserComments));
                        return scriptData;
                }

                //
                //  first look for the bash version
                string versionLine = "# bashWizard version ";
                int index = bashWizardCode.IndexOf(versionLine);
                double userBashVersion = 0.1;
                string[] lines = null;
                string line = "";
                bool foundDescription = false;
                if (index > 0)
                {
                    bool ret = double.TryParse(bashWizardCode.Substring(index + versionLine.Length, 5), out userBashVersion);
                    if (!ret)
                    {
                        ret = double.TryParse(bashWizardCode.Substring(index + versionLine.Length, 3), out userBashVersion); // 0.9 is a version i have out there...
                        if (!ret)
                        {
                            scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, missingVersionInfo));
                        }
                    }
                }


                //
                //  find the usage() function and parse it out - this gives us the 4 properties in the ParameterItem below
                if (scriptData.GetStringBetween(bashWizardCode, "usage() {", "}", out string bashFragment) == false)
                {
                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, bashFragment));

                }
                else
                {
                    bashFragment = bashFragment.Replace("echoWarning", "echo");
                    bashFragment = bashFragment.Replace("\n", "");
                    lines = bashFragment.Split(new string[] { "echo ", "\"" }, StringSplitOptions.RemoveEmptyEntries);
                    line = "";

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


                        if (!foundDescription)
                        {
                            /*
                              it look like:

                             function usage() {
                             *  echoWarning
                             *  echo "<description>"
                             *  ...
                             * 
                             * }
                             *
                             * but the echoWarning isn't always there -- only if the --input-file option was specified.
                             * 
                             */
                            if (line.StartsWith("Parameters can be passed in the command line or in the input file."))
                            {
                                continue;
                            }
                            //
                            //  if the description is black, the next line echo's the usage -- so if we do NOT find the Usage statement
                            //  we have found the description.  and in any case, if the Description isn't there by now, it isn't there
                            //  so always set the flag saying we found it.

                            if (!line.StartsWith("Usage: $0"))
                            {
                                scriptData.Description = line.TrimEnd();
                            }

                            foundDescription = true;
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
                //  parse the echoInput() function to get script name - don't fail parsing on this one
                bashFragment = "";
                if (scriptData.GetStringBetween(bashWizardCode, "echoInput() {", "parseInput()", out bashFragment))
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
                if (scriptData.GetStringBetween(bashWizardCode, "eval set -- \"$PARSED\"", "--)", out bashFragment) == false)
                {
                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, bashFragment));

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
                                scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the parseInput() function to get the variable names, encountered the line {lines[index + 1].Trim()} which doesn't parse.  It should look like varName=$2 or the like."));

                            }
                            string[] nameTokens = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            if (nameTokens.Length != 2) // the first is the short param, second long param, and third is empty
                            {
                                scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the parseInput() function to get the variable names, encountered the line {lines[index].Trim()} which doesn't parse.  It should look like \"-l | --long-name)\" or the like."));
                            }
                            // nameTokens[1] looks like "--long-param)
                            string longParam = nameTokens[1].Substring(3, nameTokens[1].Length - 4);
                            ParameterItem param = scriptData.FindParameterByLongName(longParam);
                            if (param == null)
                            {
                                scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the parseInput() function to get the variable names, found a long parameter named {longParam} which was not found in the usage() function"));
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
                                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the parseInput() function to see if {param.VariableName} requires input, found this line: {lines[index + 1]} which does not parse.  it should either be \"shift 1\" or \"shift 2\""));
                                }
                            }
                            index += 2;
                        }
                    }
                }
                // the last bit of info to figure out is the default value -- find these with a comment
                if (scriptData.GetStringBetween(bashWizardCode, "# input variables", "parseInput", out bashFragment) == false)
                {
                    scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Fatal, bashFragment));
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
                            scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the variable declarations between the \"# input variables\" comment and the \"parseInput\" calls, the line {line} was encountered that didn't parse.  it should be in the form of varName=Default"));

                        }
                        string varName = varTokens[0].Trim();
                        ParameterItem param = scriptData.FindParameterByVarName(varName);
                        if (param == null)
                        {
                            scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Warning, $"When parsing the variable declarations between the \"# input variables\" comment and the \"parseInput\" calls, found a variable named {varName} which was not found in the usage() function"));
                            scriptData.ParseErrors.Add(new ParseErrorInfo(ErrorLevel.Information, $"\"{line}\" will be removed from the script.  If you want to declare it, put the declaration inside the user code comments"));

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
                scriptData.GenerateBashScript = oldGenerateBashScript;
                scriptData.NotifyPropertyChanged("BashScript");

                //
                //  now go from Parameters back to bash script, unless there are fatal errors
                //  if there are, it will stay as the input set at the top of the FromBash() function
                if (!scriptData.HasFatalErrors)
                {
                    scriptData.ToBash();
                }
            }



        }
    }
}
