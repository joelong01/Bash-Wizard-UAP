using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace bashGeneratorSharedModels
{
    public class ConfigModel
    {

        public string ScriptName { get; set; } = null;
        public bool EchoInput { get; set; } = true;
        public bool CreateLogFile { get; set; } = true;
        public bool TeeToLogFile { get; set; } = true;
        public List<ParameterItem> Parameters { get; set; } = new List<ParameterItem>();

        public ConfigModel(string name, IEnumerable<ParameterItem> list, bool echoInput, bool createLogFile, bool timeScript)
        {
            ScriptName = name;
            if (list != null)
            {
                Parameters.AddRange(list);
            }
            EchoInput = echoInput;
            CreateLogFile = createLogFile;
            TeeToLogFile = timeScript;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static ConfigModel Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<ConfigModel>(json);
        }

        /// <summary>
        ///     we want something like
        ///      "createResourceGroup.sh": {
        ///       "Name": "createResourceGroup.sh",
        ///       "Passthrough": [
        ///           {
        ///               "location": "uswest2",
        ///               "delete": "false",
        ///               "create": "true"
        ///           }
        ///       ],
        ///       "Input": [
        ///           {
        ///               "confirm-on-delete": "true"
        ///           }
        ///        ]
        ///       },
        ///     
        /// </summary>
        /// <returns></returns>
        public string SerializeInputJson()
        {


            string nl = "\n";
            string indentOne = "  ";
            string indentTwo = "      ";
            string indentThree = "         ";
            StringBuilder sb = new StringBuilder($"{indentOne}\"{this.ScriptName}\": {{{nl}{indentTwo}\"Name\":\"{ScriptName}\",{nl}{indentTwo}\"Passthrough\": {nl}{indentTwo}{{{nl}");
            string input = "";
            string passthrough = "";
            char[] quotes = { '"'};
            char[] commadNewLine = { ',', '\n', ' ' };
            foreach (var param in Parameters)
            {
                string defValue = param.Default;
                defValue = defValue.TrimStart(quotes);
                defValue = defValue.TrimEnd(quotes);

                if (param.PassthroughParam)
                {                    
                    input += $"{indentThree}\"{param.LongParam}\": \"{defValue}\",{nl}";
                }
                else
                {
                    passthrough += $"{indentThree}\"{param.LongParam}\": \"{defValue}\",{nl}";
                }
            }
            //  delete trailing "," int the temp strings            
            passthrough = passthrough.TrimEnd(commadNewLine);
            input = input.TrimEnd(commadNewLine);
            sb.Append(input);

            sb.Append($"{nl}{indentTwo}}},{nl}");

            sb.Append($"{indentTwo}\"Input\": {{{nl}");
            sb.Append(passthrough);
            sb.Append($"{nl}{indentTwo}}}{nl}{indentTwo}{nl}{indentOne}}},");



            return sb.ToString();

        }

        private string ValidateParameters()
        {
            //verify short names are unique
            HashSet<string> shortNames = new HashSet<string>();
            HashSet<string> longNames = new HashSet<string>();
            foreach (var param in Parameters)
            {
                if (param.ShortParam == "" && param.LongParam == "")
                {
                    continue; // probably just getting started
                }

                if (!shortNames.Add(param.ShortParam))
                {
                    return $"{param.ShortParam} exists at least twice.  please fix it.";
                }
                if (!longNames.Add(param.LongParam))
                {
                    return $"{param.LongParam} exists at least twice.  please fix it.";
                }
            }

            if (TeeToLogFile && !CreateLogFile)
            {
                return "Add the Tee requires that \"Create Log File\" be selected";
            }


            return "";

        }

        public string ToBash()
        {
            string validateString = ValidateParameters();
            if (validateString != "")
            {
                return validateString;
            }
            StringBuilder sb = new StringBuilder(4096);
            string nl = "\n";
            sb.Append($"#!/bin/bash{nl}{nl}");
            sb.Append($"#---------- see https://github.com/joelong01/starterBash ----------------{nl}");

            sb.Append($"usage() {{{nl}");
            sb.Append($"\techo \"Usage: $0 ");
            foreach (var param in Parameters)
            {
                sb.Append($" -{param.ShortParam}|--{param.LongParam}");
            }

            sb.Append($"\" 1>&2 {nl} ");
            sb.Append($"\techo \"\"{nl}");
            string required = "";

            foreach (var param in Parameters)
            {
                if (param.Required)
                {
                    required = "(Required)";
                }
                else
                {
                    required = "(Optional)";
                }
                sb.Append($"\techo \" -{param.ShortParam} | --{param.LongParam,-30} {required,-15} {param.Description}\"{nl}");
            }
            sb.Append($"\techo \"\"{nl}");
            sb.Append($"\texit 1{nl}");
            sb.Append($"}}{nl}{nl}");

            // echoInput()

            sb.Append($"echoInput() {{ {nl}\techo \"{ScriptName}:\"{nl}");
            foreach (var param in Parameters)
            {
                sb.Append($"\techo \"\t{param.VarName,-30} ${param.VarName}\"{nl}");
            }
            sb.Append($"}}{nl}{nl}");

            // input variables
            sb.Append($"# input variables {nl}");
            foreach (var param in Parameters)
            {
                sb.Append($"declare {param.VarName}={param.Default}{nl}");
            }
            sb.Append($"{nl}");
            sb.Append($"# make sure this version of *nix supports the right getopt {nl}");
            sb.Append($"! getopt --test > /dev/null{nl}");
            sb.Append($"if [[ ${{PIPESTATUS[0]}} -ne 4 ]]; then{nl}");
            sb.Append($"\techo \"I’m sorry, 'getopt --test' failed in this environment.\"{nl}");
            sb.Append($"\texit 1{nl}");
            sb.Append($"fi{nl}{nl}");


            sb.Append("OPTIONS=");
            foreach (var param in Parameters)
            {
                sb.Append($"{param.ShortParam}");
                if (param.AcceptsValue)
                {
                    sb.Append(":");
                }
            }

            sb.Append($"{nl}");


            sb.Append("LONGOPTS=");
            foreach (var param in Parameters)
            {
                sb.Append($"{param.LongParam}");
                if (param.AcceptsValue)
                {
                    sb.Append(":");
                }
                sb.Append(",");
            }

            sb.Append($"{nl}");

            sb.Append($"# -use ! and PIPESTATUS to get exit code with errexit set{nl}");
            sb.Append($"# -temporarily store output to be able to check for errors{nl}");
            sb.Append($"# -activate quoting/enhanced mode (e.g. by writing out \"--options\"){nl}");
            sb.Append($"# -pass arguments only via   -- \"$@\"   to separate them correctly{nl}");
            sb.Append("! PARSED=$(getopt --options=$OPTIONS --longoptions=$LONGOPTS --name \"$0\" -- \"$@\")");
            sb.Append($"{nl}");
            sb.Append($"if [[ ${{PIPESTATUS[0]}} -ne 0 ]]; then{nl}");
            sb.Append($"\t# e.g. return value is 1{nl}");
            sb.Append($"\t# then getopt has complained about wrong arguments to stdout{nl}");
            sb.Append($"\techo \"you might be running bash on a Mac.  if so, run 'brew install gnu-getopt' to make the command line processing work.\"{nl}");
            sb.Append($"\tusage{nl}");
            sb.Append($"\texit 2{nl}");
            sb.Append($"fi{nl}{nl}");


            sb.Append($"# read getopt’s output this way to handle the quoting right:{nl}");
            sb.Append($"eval set -- \"$PARSED\"{nl}");
            sb.Append($"# now enjoy the options in order and nicely split until we see --{nl}");

            sb.Append($"while true; do{nl}");
            sb.Append($"\tcase \"$1\" in{nl}");
            bool atLeastOneRequiredParameter = false;
            foreach (var param in Parameters)
            {
                //
                //  might as well look for this here since we are iterating already
                //

                if (param.Required)
                {
                    atLeastOneRequiredParameter = true;
                }


                sb.Append($"\t\t-{param.ShortParam}|--{param.LongParam}){nl}");
                sb.Append($"\t\t\t{param.VarName}={param.SetVal}{nl}");
                sb.Append($"\t\t\tshift ");
                if (param.AcceptsValue)
                {
                    sb.Append($"2{nl}");
                }
                else
                {
                    sb.Append($"1{nl}");
                }
                sb.Append($"\t\t;;{nl}");
            }
            sb.Append($"\t\t--){nl}\t\t\tshift{nl}\t\t\tbreak{nl}\t\t;;{nl}\t\t*){nl}\t\t\techo \"Invalid option $1 $2\"{nl}\t\t\texit 3{nl}\t\t;;{nl}\tesac{nl}done{nl}{nl}");
            if (atLeastOneRequiredParameter)
            {

                string shortString = $"if ";
                foreach (var param in Parameters)
                {
                    if (param.Required)
                    {
                        shortString += $"[ -z \"${{{param.VarName}}}\" ] || ";
                    }
                }
                if (shortString.Length > 3)
                {
                    shortString = shortString.Substring(0, shortString.Length - " || ".Length);
                    sb.Append($"{shortString}");
                }


                sb.Append($"; then{nl}");
                sb.Append($"\techo \"\"{nl}");
                sb.Append($"\techo \"Required parameter missing! \"{nl}");
                sb.Append($"\techoInput #make it easy to see what is missing{nl}");
                sb.Append($"\techo \"\"{nl}");
                sb.Append($"\tusage{nl}");
                sb.Append($"\texit 2{nl}");
                sb.Append($"fi{nl}{nl}");
            }



            if (this.CreateLogFile)
            {
                sb.Append($"declare LOG_FILE=\"${{logFileDir}}{this.ScriptName}.log\"{nl}");
                sb.Append($"mkdir \"${{logFileDir}}\" 2>> /dev/null{nl}");
                sb.Append($"rm -f \"${{LOG_FILE}}\"  >> /dev/null{nl}");


            }


            if (TeeToLogFile)
            {
                sb.Append($"#creating a tee so that we capture all the output to the log file{nl}");
                sb.Append($"{{{nl}");
                sb.Append($"time=$(date +\"%m/%d/%y @ %r\"){nl}");
                sb.Append($"echo \"started: $time\"{nl}{nl}");
                if (this.EchoInput == true)
                {
                    sb.Append($"echoInput{nl}{nl}");
                    sb.Append($"{nl}");
                }
                sb.Append($"# --- YOUR SCRIPT STARTS HERE ---{nl}{nl}{nl}{nl}# --- YOUR SCRIPT ENDS HERE ---{nl}");
                sb.Append($"time=$(date +\"%m/%d/%y @ %r\"){nl}");
                sb.Append($"echo \"ended: $time\"{nl}");
                sb.Append($"}} | tee -a \"${{LOG_FILE}}\"");
            }



            if (!TeeToLogFile)
            {
                if (this.EchoInput == true)
                {
                    sb.Append($"echoInput{nl}{nl}");
                    sb.Append($"{nl}");
                }
                sb.Append($"# --- YOUR SCRIPT STARTS HERE ---{nl}");
            }


            return sb.ToString();
        }
    }
}
