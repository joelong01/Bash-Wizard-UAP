using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace bashGeneratorSharedModels
{
    public class ConfigModel
    {

        public string ScriptName { get; set; } = null;
        public bool EchoInput { get; set; } = true;
        public bool CreateLogFile { get; set; } = false;
        public bool TeeToLogFile { get; set; } = false;
        public bool AcceptInputFile { get; set; } = false;
        public List<ParameterItem> Parameters { get; set; } = new List<ParameterItem>();

        public ConfigModel(string name, IEnumerable<ParameterItem> list, bool echoInput, bool createLogFile, bool timeScript, bool acceptsInput)
        {
            ScriptName = name;
            if (list != null)
            {
                Parameters.AddRange(list);
            }
            EchoInput = echoInput;
            CreateLogFile = createLogFile;
            TeeToLogFile = timeScript;
            AcceptInputFile = acceptsInput;
        }

        public string Serialize()
        {
            StripLeadingAndTrailingSpaces();
            return JsonConvert.SerializeObject(this, Formatting.Indented);
            
        }

        public void StripLeadingAndTrailingSpaces()
        {
            foreach (var parameter in Parameters)
            {
                parameter.Default = parameter.Default.Trim();
                parameter.Description = parameter.Description.Trim();
                parameter.LongParameter = parameter.LongParameter.Trim();
                parameter.ShortParameter = parameter.ShortParameter.Trim();
                parameter.ValueIfSet = parameter.ValueIfSet.Trim();
                parameter.VariableName = parameter.VariableName.Trim();                
            }
        }

        public static ConfigModel Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<ConfigModel>(json);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string SerializeInputJson()
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
            foreach (var param in Parameters)
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

        public string ValidateParameters()
        {
            //verify short names are unique
            HashSet<string> shortNames = new HashSet<string>();
            HashSet<string> longNames = new HashSet<string>();
            foreach (var param in Parameters)
            {
                if (param.ShortParameter == "" && param.LongParameter == "")
                {
                    continue; // probably just getting started
                }

                if (!shortNames.Add(param.ShortParameter))
                {
                    return $"{param.ShortParameter} exists at least twice.  please fix it.";
                }
                if (!longNames.Add(param.LongParameter))
                {
                    return $"{param.LongParameter} exists at least twice.  please fix it.";
                }

                if (!param.RequiresInputString && param.ValueIfSet.Trim(new char[] { ' ' }) == "$2")
                {
                    return $"Parameter {param.LongParameter} has \"Require Input String\" set to False and the \"Value if Set\" to \"$2\".  \nThis combination is not allowed.";
                }
                
            }

            if (TeeToLogFile && !CreateLogFile)
            {
                return "Add the Tee requires that \"Create Log File\" be selected";
            }


            return "";

        }
        /// <summary>
        /// this so we don't have to use \t.  instead replace them with 4 spaces.
        /// 
        /// </summary>
        /// <param name="n" >The number of 'tabs' to return</param>
        /// <returns>A string that has 4*n spaces</returns>
        public string Tabs(int n)
        {
            return "".PadLeft(n * 4);
        }
        /// <summary>
        ///     Converts the parameters to a bash script.  The overall implementation strategy is to put as much as possible into "templates",
        ///     stored in resource files.  we then replace strings in the templates with strings we generate based on the parameters.  there are
        ///     three "phases": 1) loop through the parameters and build each string we need 2) fix up the strings we built - e.g. remove end of 
        ///     line characters and 3) use StringBuilder.Replace() to put the strings into the right place in the bash file.
        /// </summary>
        /// <returns></returns>
        public string ToBash()
        {

            string validateString = ValidateParameters();
            if (validateString != "")
            {
                return validateString;
            }

            string nl = "\n";

            StringBuilder sbBashScript = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "bashTemplate.sh"));
            StringBuilder logTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "logTemplate.sh"));
            StringBuilder parseInputTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "parseInputTemplate.sh"));
            StringBuilder requiredVariablesTemplate = new StringBuilder(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "requiredVariablesTemplate.sh"));
            StringBuilder usageLine = new StringBuilder($"{Tabs(1)}echo \"Usage: $0 ");
            StringBuilder usageInfo = new StringBuilder($"{Tabs(1)}echo \"\"\n");
            StringBuilder echoInput = new StringBuilder($"\"{ScriptName}:\"{nl}");
            StringBuilder shortOptions = new StringBuilder("");
            StringBuilder longOptions = new StringBuilder("");
            StringBuilder inputCase = new StringBuilder("");
            StringBuilder inputDeclarations = new StringBuilder("");
            StringBuilder parseInputFile = new StringBuilder("");
            StringBuilder requiredFilesIf = new StringBuilder("");
            StringBuilder loggingSupport = new StringBuilder("");


            //
            //   phase 1: loop through the parameters and build our strings
            foreach (var param in Parameters)
            {
                //
                //  first usage line
                string required = (param.RequiredParameter) ? "Required" : "Optional";
                usageLine.Append($"-{param.ShortParameter} | --{param.LongParameter} ");
                usageInfo.Append($"{Tabs(1)}echo \" -{param.ShortParameter} | --{param.LongParameter,-30} {required,-15} {param.Description}\"{nl}");

                //
                // the  echoInput function
                echoInput.Append($"{Tabs(1)}echo \"{Tabs(1)}{param.LongParameter,-30} ${param.VariableName}\"{nl}");

                //
                //  OPTIONS, LONGOPTS
                string colon = (param.RequiresInputString) ? ":" : "";
                shortOptions.Append($"{param.ShortParameter}{colon}");
                longOptions.Append($"{param.LongParameter}{colon},");

                // input Case
                inputCase.Append($"{Tabs(3)}-{param.ShortParameter}|--{param.LongParameter})\n");
                inputCase.Append($"{Tabs(4)}{param.VariableName}={param.ValueIfSet}\n");
                inputCase.Append((param.RequiresInputString) ? $"{Tabs(4)}shift 2\n" : $"{Tabs(4)}shift 1\n");
                inputCase.Append($"{Tabs(3)};;\n");

                // declare variables
                inputDeclarations.Append($"declare {param.VariableName}={param.Default}\n");
                if (this.AcceptInputFile && param.VariableName != "inputFile")
                {

                    // parse input file
                    parseInputFile.Append($"{Tabs(1)}{param.VariableName}=$(echo \"${{configSection}}\" | jq \'.[\"{param.LongParameter}\"]\' --raw-output)\n");

                }

                // if statement for the required files

                if (param.RequiredParameter)
                {
                    requiredFilesIf.Append($" [ -z \"${{{param.VariableName}}}\" ] ||");
                }


            }


            //
            //  phase 2 - fix up any of the string created above         

            usageLine.Append("\"");

            longOptions.Remove(longOptions.Length - 1, 1);
            inputCase.Remove(inputCase.Length - 1, 1);

            if (requiredFilesIf.Length > 0)
            {
                requiredFilesIf.Remove(requiredFilesIf.Length - 3, 3); // removes the " ||" at the end
                requiredVariablesTemplate.Replace("__REQUIRED_FILES_IF__", requiredFilesIf.ToString());
            }
            else
            {
                requiredVariablesTemplate.Clear();
            }

            if (this.CreateLogFile)
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

            if (parseInputFile.Length > 0)
            {
                parseInputTemplate.Replace("__SCRIPT_NAME__", this.ScriptName);
                parseInputTemplate.Replace("__FILE_TO_SETTINGS__", parseInputFile.ToString());
                sbBashScript.Replace("__PARSE_INPUT_FILE", parseInputTemplate.ToString());
            }
            else
            {
                sbBashScript.Replace("__PARSE_INPUT_FILE", "");
            }

            sbBashScript.Replace("__BEGIN_TEE__", this.TeeToLogFile ? EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "beginTee.sh") : "");


            sbBashScript.Replace("__REQUIRED_PARAMETERS__", requiredVariablesTemplate.ToString());
            sbBashScript.Replace("__LOGGING_SUPPORT_", logTemplate.ToString());
            sbBashScript.Replace("__ECHO_INPUT__", this.EchoInput ? "echoInput" : "");
            return sbBashScript.ToString();


        }


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
                foreach (var param in Parameters)
                {
                    sb.Append($"{Tabs(2)}\"--{param.LongParameter}\",{nl}{Tabs(2)}\"{param.Default.TrimStart(quotes).TrimEnd(quotes)}\",{nl}");
                }


                sb.Append($"{Tabs(1)}]{nl}");
                sb.Append($"}}");
            }
            catch(Exception e)
            {
                return $"Exception generating config\n\nException Info:\n===============\n{e.Message}";
            }

            return sb.ToString();
        }
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
