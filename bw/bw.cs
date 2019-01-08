using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using bashWizardShared;


namespace BashWizardConsole
{
    internal class BashWizard
    {



        private static void Main(string[] args)
        {

            Parameter[] parameters = new Parameter[]
            {
                //public Parameter(string name, string shortName, string longName, bool requiresInput, string value, bool required, string description)
                new Parameter("CreateSample", "-c", "--create-sample-json", false, "", false, "Creates a sample bash script"),
                new Parameter("ParseAndCreate", "-p", "--parse-and-create", false, "", false, "Parses the input file and creates the output file.  Typically used to update the to current BashWizard version"),
                new Parameter("InputFile", "-i", "--input-file", true, "", false,"Required when passing --parse-and-create.  Represents the input bash file"),
                new Parameter("OutputFile", "-o", "--output-file", true, "", true,"Required when passing --parse-and-create.  Represents the input bash file"),
                new Parameter("VSCodeDebugInfo", "-d", "--vs-code-debug-info", true, "", false, "Outputs the JSON config needed for the VS Code Bash Debug extention"),
                new Parameter("Help", "-h", "--help", false, "", false,"Prints the help")
            };

            InputValidation input = new InputValidation(parameters);
            try
            {
                input.SetAndValidate(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("");
                var bg = Console.BackgroundColor;
                var fg = Console.ForegroundColor;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(e.Message);
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
                EchoHelp(parameters);
                return;
            }

            if (input.IsFlagSet("CreateSample"))
            {
                CreateSample();
                Console.WriteLine("");

            }

            if (input.IsFlagSet("ParseAndCreate"))
            {
                
                string inputFile = input.GetValue("InputFile");
                string outputFile = input.GetValue("OutputFile");
                
                if (inputFile == "" || outputFile == "")
                {
                    EchoHelp(parameters);
                    return;
                }
                Console.WriteLine($"parsing {inputFile} to create {outputFile} ");
                try
                {
                    ScriptData scriptData = null;
                    using (StreamReader srIn = new StreamReader(new FileStream(inputFile, FileMode.Open)))
                    {
                        
                        var bashFile = srIn.ReadToEnd();
                        scriptData = ScriptData.FromBash(bashFile); // this will both parse it and generate a new file
                    }
                    
                    using (var srOut = new StreamWriter(new FileStream(outputFile, FileMode.Create)))
                    {
                        srOut.Write(scriptData.BashScript);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }


            }

            if (input.IsFlagSet("CreateBashScript"))
            {
                CreateBashScript(input.GetValue("CreateBashScript"));
                Console.WriteLine("");
            }

            if (input.IsFlagSet("MakeJsonInputParameters"))
            {
                CreateInputJson(input.GetValue("MakeJsonInputParameters"));
                Console.WriteLine("");
            }

            if (input.IsFlagSet("VSCodeDebugInfo"))
            {
                CreateVSCodeDebugInfo(input.GetValue("VSCodeDebugInfo"), input.GetValue("ScriptDirectory"));
                Console.WriteLine("");
            }

            if (input.IsFlagSet("Help"))
            {
                EchoHelp(parameters);
            }

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

        private static void EchoHelp(Parameter[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Usage:\n\n");
            var (maxLongParam, maxDescription) = GetLongestParameter(parameters);
            sb.Append($"\t{"Flag".PadRight(maxLongParam+10, ' ')}Required     {"Description".PadRight(maxDescription, ' ')}\n");
            sb.Append($"\t{"".PadRight(maxLongParam + 5, '=')}     ========     {"".PadRight(maxDescription, '=')}\n");
            foreach (var param in parameters)
            {
                sb.Append($"\t{param.ShortName} | {param.LongName.PadRight(maxLongParam + 5, ' ')}");
                sb.Append(param.Required ? "(yes)        " : "(no)         ");
                sb.Append($"{param.Description}\n");
            }
            sb.Append("\n");
                        
            Console.WriteLine(sb.ToString());
        }

        private static void CreateVSCodeDebugInfo(string configFile, string scriptDirectory)
        {
            //string Json = System.IO.File.ReadAllText(configFile);
            //var model = ScriptData.Deserialize(Json);
            //Console.WriteLine(model.VSCodeDebugInfo(scriptDirectory));
        }

        private static void CreateInputJson(string configFile)
        {
            //string Json = System.IO.File.ReadAllText(configFile);
            //var model = ScriptData.Deserialize(Json);
            //Console.WriteLine(model.SerializeInputJson());
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
            param = new ParameterItem
            {
                ShortParameter = "d",
                Description = "delete the resource group if it already exists",
                LongParameter = "delete",
                VariableName = "delete",
                Default = "false",
                RequiresInputString = false,
                RequiredParameter = false,
                ValueIfSet = "true"
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
            //Console.WriteLine(model.Serialize());

        }


    }
}

