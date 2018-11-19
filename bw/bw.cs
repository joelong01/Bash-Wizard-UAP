using bashGeneratorSharedModels;
using System;
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
                new Parameter("MakeJsonInputParameters", "-i", "--create-input-json", true, "")
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
                string usage = EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "usage.txt");
                Console.WriteLine("");
                Console.WriteLine(usage);
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
            Console.WriteLine(EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "sample.json"));
        }

       
    }
}
