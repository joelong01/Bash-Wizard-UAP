using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using bashWizardShared;


namespace BashWizardConsole
{
    internal class BashWizard
    {
        private static readonly string[] examples =
        {
            "cat foo.json | bw > foo.batch #takes the file foo.json, passes it to bw which will output a foo.batch file with those parameters",
            "curl myBaseParams.json | bw --merge-params ./localParams.Json > foo.sh # download myBaseParams.Json, parses it and merges in the parameters in localParams.json and then outputs it to foo.sh",
            "cat foo.sh | bw # parses foo.sh and echos to stdout an updated foo.sh script"

        };


        private static void Main(string[] args)
        {
            Parameter[] parameters = new Parameter[]
               {
                //public Parameter(string name, string shortName, string longName, bool requiresInput, string value, bool required, string description)
                new Parameter("CreateSample",       "-c", "--create-sample-json", false, "", false, "Creates a sample bash script"),
                new Parameter("ParseAndCreate",     "-p", "--parse-and-create", false, "", false, "Parses the input file and creates the output file.  Typically used to update the to current BashWizard version"),
                new Parameter("InputFile",          "-i", "--input-file", true, "", false,"Required when passing --parse-and-create.  Represents the input bash file"),
                new Parameter("OutputFile",         "-o", "--output-file", true, "", false,"Required when passing --parse-and-create.  Represents the input bash file"),
                new Parameter("VSCodeDebugInfo",    "-d", "--vs-code-debug-info", false, "", false, "Outputs the JSON config needed for the VS Code Bash Debug extension"),
                new Parameter("OutputJson",         "-j", "--json", false, "", false, "Outputs the JSON file given the bash file.  e.g. \"cat foo.sh | bw -j\", \" cat foo.sh | bw -j > foo.json\", or \"cat foo.sh | bw -j -o foo.json\""),
                new Parameter("MakeJsonInputParameters", "-k", "--output-json-input-config", false, "", false, "outputs the json file that has all of the input variables to set"),
                new Parameter("Help",               "-h", "--help", false, "", false,"Prints the help")
               };


            //
            //  store the console colors so we can write errors in red on black and reset them to what the user has            
            var bg = Console.BackgroundColor;
            var fg = Console.ForegroundColor;
            InputValidation input = null;
            try
            {

                input = ParseCommandLine(args, parameters); // this can throw

                //
                //  you can pass a file in via StdIn
                string stdin = "";
                if (Console.IsInputRedirected)
                {
                    using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                    {
                        stdin = reader.ReadToEnd();

                    }
                }


                if (args.Length == 0)
                {
                    // e.g. "bw foo.json" or "bw foo.sh"
                    ParseAndCreate(stdin, "");

                    return; // because there are no parameters, there is nothing else to do
                }

                //
                //  we need this for many of the cases below
                string inputContents = "";
                string outputFileName = "";

                //
                //  now go through each set flag and do what the command line asks us to do
                foreach (var kvp in input.SetFlags)
                {
                    switch (kvp.Key) // Key == the "Name" property of the Parameter object
                    {
                        case "CreateSample":
                            CreateSample();
                            break;
                        case "ParseAndCreate":
                            //  e.g. 
                            //  bw --input-file foo.json
                            //  bw --input-file foo.sh
                            //  bw --input-file foo.json --output-file foo.sh
                            //  cat foo.sh | bw -o foo.sh
                            //  curl <...> | bw -o foo.sh
                            //  etc.
                            (inputContents, outputFileName) = GetInputAndOutputValues(input, stdin);
                            ParseAndCreate(inputContents, outputFileName);
                            break;
                        case "OutputJson":
                            // \"cat foo.sh | bw -j\", \" cat foo.sh | bw -j > foo.json\", or \"cat foo.sh | bw -j -o foo.json\""
                            (inputContents, outputFileName) = GetInputAndOutputValues(input, stdin);
                            OutputJson(inputContents, outputFileName);
                            
                            break;
                        case "InputFile":
                        case "OutputFile":
                            break;
                        case "VSCodeDebugInfo":
                            //  -d...examples:
                            //  bw -d foo.bash
                            //  bw -d foo.json
                            //  curl www.foo.com/file | bw -d
                            //  bw -d foo.bash -o foo_debug.json

                            (inputContents, outputFileName) = GetInputAndOutputValues(input, stdin);

                            CreateVSCodeDebugInfo(inputContents, outputFileName);
                            break;
                        case "MakeJsonInputParameters":
                            (inputContents, outputFileName) = GetInputAndOutputValues(input, stdin);
                            CreateInputJson(inputContents, outputFileName);
                            break;

                        case "Help":
                            Console.WriteLine(UsageString(parameters));
                            break;
                        default:
                            throw new Exception($"{kvp.Key} is an unexpected and unsupported Name for a Parameter");
                    }
                }

            }
            catch (Exception e)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(e.Message);

            }
            finally
            {
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
            }
        }


        /// <summary>
        ///  take the input and stdin and parse and return the contents of the input and the output filename
        ///  if output is "stdin" change it to "" as that is what we use to decide to Console.Writeline instead
        ///  of writing the text file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="stdin"></param>
        /// <returns></returns>
        private static (string inputContents, string outputFileName) GetInputAndOutputValues(InputValidation input, string stdin)
        {

            //
            //  now go through each set flag and do what the command line asks us to do

            string inputContents = "";
            string outputFileName = "";

            //
            //  if an input File has been specified, use it.  otherwise use what is in stdin
            // always get contents to make the functions consistent when input is piped in
            if (input.SetFlags.TryGetValue("InputFile", out Parameter param))
            {
                inputContents = File.ReadAllText(param.Value); // this can throw

            }
            else if (stdin != "")
            {
                inputContents = stdin;
            }
            else
            {
                //
                //  no specified filename for input and nothing piped in -- we have an error
                throw new Exception("Missing input data.  Either pass in a filename or pipe file contents e.g. cat file | bw <options>");
            }

            if (input.SetFlags.TryGetValue("OutputFile", out param))
            {
                outputFileName = param.Value;
                if (outputFileName.ToLower() == "stdin")
                {
                    outputFileName = "";
                }
            }

            return (inputContents, outputFileName);
        }

        /// <summary>
        ///     given file contents, output the JSON for the parameters
        /// </summary>
        /// <param name="inputContents"></param>
        /// <param name="outputFileName"></param>
        private static void OutputJson(string inputContents, string outputFileName)
        {
            var scriptData = ScriptData.FromBash(inputContents);
            if (scriptData.ParseErrors.Count > 0)
            {
                foreach (var error in scriptData.ParseErrors)
                {
                    EchoError($"Error: {error.Message}");
                }
                throw new Exception("Please fix the errors and try again");
            }
            if (outputFileName == "")
            {
                Console.WriteLine(scriptData.ToJson());
            }
            else
            {
                File.WriteAllText(outputFileName, scriptData.ToJson());
            }
        }

        /// <summary>
        ///     Parse the contents and write the new bash file.
        /// </summary>
        /// <param name="inputContents">The Bash File contents</param>
        /// <param name="outputFileName">The name of the file to write the new script to.  can be null for stdout</param>
        private static void ParseAndCreate(string inputContents, string outputFileName)
        {
            var scriptData = FromFileContents(inputContents);
            if (outputFileName == "")
            {
                Console.WriteLine(scriptData.BashScript);
            }
            else
            {
                File.WriteAllText(outputFileName, scriptData.BashScript);
            }
        }
        /// <summary>
        ///     Output the JSON needed for the VS Code debugger extension
        ///     input contents can be either a JSON file or a Bash Wizard script
        ///     output can be stdin or a file name
        /// </summary>
        /// <param name="inputContents">The Bash or JSON File contents</param>
        /// <param name="outputFileName">The name of the file to write the new script to.  can be null for stdout</param>
        private static void CreateVSCodeDebugInfo(string inputContents, string outputFileName)
        {
            var scriptData = FromFileContents(inputContents);
            if (outputFileName == "")
            {
                Console.WriteLine(scriptData.VSCodeDebugInfo(""));
            }
            else
            {
                File.WriteAllText(outputFileName, scriptData.VSCodeDebugInfo(""));
            }
        }

        /// <summary>
        ///     outputs a JSON file that can be used with the --input-file standard parameter of Bash Wizard scripts
        /// </summary>
        /// <param name="inputContents">The Bash or JSON File contents</param>
        /// <param name="outputFileName">The name of the file to write the new script to.  can be null for stdout</param>
        private static void CreateInputJson(string inputContents, string outputFileName)
        {
            var scriptData = FromFileContents(inputContents);
            if (outputFileName == "")
            {
                Console.WriteLine(scriptData.GetInputJson());
            }
            else
            {
                File.WriteAllText(outputFileName, scriptData.GetInputJson());
            }
        }

        /// <summary>
        ///     given input contents, parse it, and return a ScriptData object
        ///     this can throw
        /// </summary>
        /// <param name="inputContents">The Bash File or JSON file contents</param>
        /// <returns></returns>
        private static ScriptData FromFileContents(string inputContents)
        {

            ScriptData scriptData = null;

            // is it bash, json, or trash?
            // bw scripts start with # and JSON with { 
            switch (inputContents[0])
            {
                case '#':
                    {
                        // treat like a Bash Wizard shell script - parse it and echo out a new bash script
                        scriptData = ScriptData.FromBash(inputContents);

                    }
                    break;
                case '{':
                    {
                        // treat like a JSON file - echo out a new bash script based on the JSON
                        scriptData = ScriptData.FromJson(inputContents, "");

                    }
                    break;
                default:
                    throw new Exception($"The input is not a JSON file or a Bash Wizard File because the first character is {inputContents[0]} instead of a # or a {{");

            }
            return scriptData;
        }

        private static InputValidation ParseCommandLine(string[] args, Parameter[] parameters)
        {

            InputValidation input = new InputValidation(parameters);
            List<string> errors = input.SetAndValidate(args);
            if (errors.Count > 0)
            {
                //
                //  if there is an error, we'll echo the Usage and what we parsed 
                string s = UsageString(parameters) + "\n";
                s += ParsedCommandLine(input) + "\n";
                foreach (var e in errors)
                {
                    s += e + "\n";
                }
                throw new Exception(s);
            }
            return input;
        }

        private static void EchoError(string toEcho)
        {
            var bg = Console.BackgroundColor;
            var fg = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.Write(toEcho);
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;

        }

        private static void LoadAndSave(string input, string output)
        {
            Console.WriteLine($"updating {input}");
            string oldBash = System.IO.File.ReadAllText(input);
            ScriptData data = ScriptData.FromBash(oldBash);
            File.WriteAllText(output, data.BashScript);

        }

        private static (int maxLongParam, int maxDescription) GetLongestParameter(Parameter[] parameters)
        {
            int maxLongParam = 0;
            int maxDescription = 0;
            foreach (var param in parameters)
            {
                if (param.LongName.Length > maxLongParam)
                {
                    maxLongParam = param.LongName.Length;
                }

                if (param.Description.Length > maxDescription)
                {
                    maxDescription = param.Description.Length;
                }
            }
            return (maxLongParam, maxDescription);
        }

        private static string UsageString(Parameter[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Usage:\n\n");
            var (maxLongParam, maxDescription) = GetLongestParameter(parameters);
            sb.Append($"\t{"Flag".PadRight(maxLongParam + 10, ' ')}Required     {"Description".PadRight(maxDescription, ' ')}\n");
            sb.Append($"\t{"".PadRight(maxLongParam + 5, '=')}     ========     {"".PadRight(maxDescription, '=')}\n");
            foreach (var param in parameters)
            {
                sb.Append($"\t{param.ShortName} | {param.LongName.PadRight(maxLongParam + 5, ' ')}");
                sb.Append(param.Required ? "(yes)        " : "(no)         ");
                sb.Append($"{param.Description}\n");
            }
            sb.Append("\n");

            return sb.ToString();
        }

        private static string ParsedCommandLine(InputValidation input)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Parsed Command Line:\n\n");
            foreach (var kvp in input.ValidFlags)
            {
                sb.Append($"\t{kvp.Value.ShortName} | {kvp.Value.LongName.PadRight(15, '.')} {kvp.Value.Value}\n");
            }
            sb.Append("\n");

            return sb.ToString();
        }

  

     

        private static void CreateBashScript(string configFile)
        {
            //string Json = System.IO.File.ReadAllText(configFile);
            //var model = ScriptData.Deserialize(Json);
            //Console.WriteLine(model.ToBash());
        }

        private static void CreateSample()
        {
            List<ParameterItem> paramList = new List<ParameterItem>();
            ParameterItem param = new ParameterItem
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
            paramList.Add(param);
            param = new ParameterItem
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
            paramList.Add(param);
     

            param = new ParameterItem()
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
            paramList.Add(param);
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
            paramList.Add(param);

            ScriptData model = new ScriptData("test.sh", paramList, true, true, true, "Sample test script", "");
            Console.Write(model.ToJson());

        }


    }
}

