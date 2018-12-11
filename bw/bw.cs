using bashGeneratorSharedModels;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace BashWizardConsole
{
    internal class BashWizard
    {



        private static void Main(string[] args)
        {

            Parameter[] parameters = new Parameter[]
            {
                new Parameter("CreateBashScript", "-f", "--input-file", true, ""),
                new Parameter("CreateSample", "-c", "--create-sample-json", false, ""),
                new Parameter("MakeJsonInputParameters", "-i", "--create-input-json", true, ""),
                new Parameter("VSCodeDebugInfo", "-d", "--vs-code-debug-info", true, ""),
                new Parameter("ScriptDirectory", "-r", "--script-directory", true, "./Scripts"),
                new Parameter("Help", "-h", "--help", false, "")
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
                EchoHelp();
                return;
            }

            if (input.IsFlagSet("CreateSample"))
            {
                CreateSample();
                Console.WriteLine("");

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
                EchoHelp();
            }

        }

        private static void EchoHelp()
        {
            string usage = EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "usage.txt");
            Console.WriteLine("");
            Console.WriteLine(usage);
        }

        private static void CreateVSCodeDebugInfo(string configFile, string scriptDirectory)
        {
            string Json = System.IO.File.ReadAllText(configFile);
            var model = ConfigModel.Deserialize(Json);
            Console.WriteLine(model.VSCodeDebugInfo(scriptDirectory));
        }

        private static void CreateInputJson(string configFile)
        {
            string Json = System.IO.File.ReadAllText(configFile);
            var model = ConfigModel.Deserialize(Json);
            Console.WriteLine(model.SerializeInputJson());
        }

        private static void CreateBashScript(string configFile)
        {
            string Json = System.IO.File.ReadAllText(configFile);
            var model = ConfigModel.Deserialize(Json);
            Console.WriteLine(model.ToBash());
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

            ConfigModel model = new ConfigModel("test.sh", paramList,  true, true, true);
            Console.WriteLine(model.Serialize());
         
        }

       
    }
}

