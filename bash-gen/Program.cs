using sharedBashGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace bash_gen
{

    class Program
    {

        static readonly string usage =
 @" 
Usage:  

    bash-gen -f <config_file.JSON>
                        
    takes config_file in JSON format outputs a bash script

    example: bash-gen -f test.json > test.sh
        
    == or ==

    bash.gen -c 

    example: bash.gen -c > test.json";


        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CommandLine);
            var configFile = "";
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var param = args[i];
                    Console.WriteLine($"param: {param}");
                    switch (param)
                    {
                        case "-c":
                            CreateDefaultConfigFile();
                            return;
                        case "-f":
                            LoadAndCreateBash(args[i + 1]);
                            return;
                        default:
                            ShowUsage();
                            return;
                    }
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
            catch(Exception e)
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
    }
}
