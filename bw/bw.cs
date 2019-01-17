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
            "dotnet ../Binaries/bw.dll -c",
            "dotnet ../Binaries/bw.dll -c > foo.json",
            "cat foo.json | dotnet ../Binaries/bw.dll > foo.sh #takes the file foo.json, passes it to bw which will output a foo.batch file with those parameters",
            "curl myBaseParams.json | dotnet ../Binaries/bw.dll --merge-params ./localParams.Json > foo.sh # download myBaseParams.Json, parses it and merges in the parameters in localParams.json and then outputs it to foo.sh",
            "cat foo.sh | dotnet ../Binaries/bw.dll # parses foo.sh and echos to stdout an updated foo.sh script"

        };


        private static void Main(string[] args)
        {
            Parameter[] parameters = new Parameter[]
               {
                //public Parameter(string name, string shortName, string longName, bool requiresInput, string value, bool required, string description)
                new Parameter("CreateSample",       "-c", "--create-sample-json", false, "", true, "Creates a sample bash script"),
                new Parameter("InputFile",          "-i", "--input-file", true, "", false,"Represents the input bash or JSON file.  if not specified, STDIN is assumed"),
                new Parameter("OutputFile",         "-o", "--output-file", true, "", false,"Represents the output bash or JSON file.  If not specified, STDOUT is assumed"),
                new Parameter("ParseAndCreate",     "-p", "--parse-and-create", false, "", false, "Parses the input file and creates a bash file specified --output-file (-o)"),
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

        /// <summary>
        ///     print one column
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="columns"></param>
        /// <param name="columnToAdd"></param>
        /// <param name="toAdd"></param>
        /// <param name="pad"></param>
        /// <param name="left"></param>
        private static void PrintColumn(StringBuilder sb, int[] columns, int columnToAdd, string toAdd, char pad, bool left)
        {
            if (left)
            {
                sb.Append($"{toAdd.PadLeft(columns[columnToAdd], pad)}");
            }
            else
            {
                sb.Append($"{toAdd.PadRight(columns[columnToAdd], pad)}");
            }
        }
        /// <summary>
        ///     pass in arrays of info to print out the columns
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="values"></param>
        /// <param name="columns"></param>
        /// <param name="flags"></param>
        /// <param name="pad"></param>
        private static void AppendColumns(StringBuilder sb, string[] values, int[] columns, bool[] flags, char[] pad )
        {
            int count = values.Length;
            if (columns.Length != count || flags.Length != count)
            {
                throw new ArgumentException("array lengths must match");
            }
            
            for (int i=0; i<count; i++)
            {
                if (flags[i])
                {
                    sb.Append($"{values[i].PadLeft(columns[i], pad[i])}");
                }
                else
                {
                    sb.Append($"{values[i].PadRight(columns[i], pad[i])}");
                }
            }

        }

        private static string UsageString(Parameter[] parameters)
        {
            // Column layout: 
            //    Flag              Required    Description
            //....==============....========....============================================================================================
            // C0   C1           C2     C3    C4    C5
            //   
            //

            
            StringBuilder sb = new StringBuilder();

            string[] header = new string[] { "Flag", "Required", "Description" };
            var (maxLongParam, maxDescription) = GetLongestParameter(parameters);
         
            int[] columnWidths = new int[] { 4, maxLongParam + 5, 4, 8, 4, maxDescription > 80 ? 80 : maxDescription };
            bool[] boolPadFlags = new bool[] { true, false, true, true, true, false };
            string[] columnHeaders = new string[] { "", "Flag", "", "Required", "", "Description" };
            char[] defaultPadding = new char[] { ' ', ' ', ' ', ' ', ' ', ' ' };
            AppendColumns(sb, columnHeaders, columnWidths, boolPadFlags, defaultPadding);
            sb.Append("\n");
            AppendColumns(sb, new string[] {"","","","", "", "" }, columnWidths, boolPadFlags, new char[] { ' ', '=', ' ', '=', ' ', '=' });
            sb.Append("\n");


            foreach (var param in parameters)
            {
                PrintColumn(sb, columnWidths, 0, "", ' ', true);
                PrintColumn(sb, columnWidths, 1, $"{param.ShortName} | {param.LongName}", ' ', false);
                PrintColumn(sb, columnWidths, 2, "", ' ', true);
                PrintColumn(sb, columnWidths, 3, param.Required ? "(yes)" : "(no)", ' ', false);
                PrintColumn(sb, columnWidths, 4, "", ' ', true);
               
                
               
                if (param.Description.Length <= columnWidths[5])
                {
                    PrintColumn(sb, columnWidths, 5, param.Description, ' ', false);
                    sb.Append($"\n");
                }
                else
                {
                    int len = param.Description.Length;
                    int currentPosition = 0;
                    int columnWidth = columnWidths[5];
                    int startPos = columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3] + columnWidths[4];
                    while(true)
                    {
                        int toCopy = columnWidth;
                        if (columnWidth + currentPosition > param.Description.Length)
                        {
                            toCopy = param.Description.Length - currentPosition;
                        }
                        
                        string toWrite = param.Description.Substring(currentPosition, toCopy);
                        PrintColumn(sb, columnWidths, 5, toWrite, ' ', false);
                        sb.Append($"\n");                        
                        currentPosition += toWrite.Length;
                        if (currentPosition < param.Description.Length)
                        {
                            sb.Append("".PadLeft(startPos, ' '));
                        }
                        else
                        {
                            break;
                        }
                    } 
                }
            }
    

            sb.Append("\n");
            sb.Append("Examples\n");
            sb.Append("========\n");
            foreach (var s in examples)
            {
                sb.Append($"{s}\n");                
            }
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
            Console.WriteLine(model.ToJson());


        }


    }
}

