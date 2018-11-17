using bashGeneratorSharedModels;
using System;
using System.IO;
using System.Linq;
using System.Reflection;


namespace bw
{
    internal class Program
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
                Console.WriteLine(e.Message);
                string usage = EmbeddedResource.GetResourceFile("usage.txt");
                Console.WriteLine(usage);
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
            Console.WriteLine(EmbeddedResource.GetResourceFile("sample.json"));
        }

        public static class EmbeddedResource
        {
            public static string GetResourceFile(string name)
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(name)); // ugh LINQ
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();
                        return result;
                    }
                }

                catch
                {
                    throw new Exception($"Failed to read Embedded Resource {name}");
                }
            }
        }
    }
}
