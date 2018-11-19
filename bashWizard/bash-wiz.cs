using bashGeneratorSharedModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace bash_gen
{
    internal class Program
    {
        private static readonly string usage =
 @" 
Usage:  

    bash-gen -f <config_file.JSON>
                        
    takes config_file in JSON format outputs a bash script

    example: bash-gen -f test.json > test.sh
        
    == or ==

    bash.gen -c 

    example: bash.gen -c > test.json";

        private static void Main(string[] args)
        {
            Console.WriteLine(Environment.CommandLine);

            string s = EmbeddedResource.GetResourceFile("Data/useage.txt");
            var configFile = "";
            try
            {

                var param = args[0];
                Console.WriteLine($"param: {param}");
                switch (param)
                {
                    case "-c":
                        CreateDefaultConfigFile();
                        return;
                    case "-f":
                        LoadAndCreateBash(args[1]);
                        return;
                    default:
                        ShowUsage();
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nException caught: {e.Message}\n");
                ShowUsage();
            }

            if (configFile == "")
            {
                Console.WriteLine("ConfigFile is null!");
                ShowUsage();
                return;
            }

            try
            {
                var model = ConfigModel.Deserialize(configFile);
                Console.WriteLine(model.ToBash());

            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception thrown: {e.Message}");
            }

        }

        private static void LoadAndCreateBash(string configFile)
        {
            string Json = System.IO.File.ReadAllText(configFile);
            var model = ConfigModel.Deserialize(Json);
            Console.WriteLine(model.ToBash());
        }

        private static void ShowUsage()
        {
            Console.WriteLine(usage);

        }

        private static void CreateDefaultConfigFile()
        {
            List<ParameterItem> list = new List<ParameterItem>();
            var item = new ParameterItem()
            {
                LongParam = "log-line",
                ShortParam = "g",
                Description = "log file directory",
                VarName = "logFileDir",
                Default = "./",
                AcceptsValue = true,
                Required = false,
                SetVal = "%2"
            };
            list.Add(item);
            item = new ParameterItem();
            list.Add(item);
            ConfigModel model = new ConfigModel("", list, true, true, true);
            Console.WriteLine(model.Serialize());

        }

        public static class EmbeddedResource
        {
            public static string GetResourceFile(string namespaceAndFileName)
            {
                try
                {
                    using (var stream = typeof(EmbeddedResource).GetTypeInfo().Assembly.GetManifestResourceStream(namespaceAndFileName))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }

                catch
                {
                    throw new Exception($"Failed to read Embedded Resource {namespaceAndFileName}");
                }
            }
        }
    }
}
