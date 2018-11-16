using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace bashGeneratorSharedModels
{
    public class InputParameter
    {


        public InputParameter(string longParam, string inputValue, bool passthroughParam)
        {
            this.Key = longParam;
            this.Value = inputValue;
            this.Passthrough = passthroughParam;
        }

        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Passthrough { get; set; } = true;

    }

    public class InputModel
    {
        public string Name { get; set; } = "";
        public List<InputParameter> Properties { get; set; } = new List<InputParameter>();
        public InputModel(string name, List<ParameterItem> parameters)
        {
            Name = name;
            foreach (var item in parameters)
            {
                if (item.SetInInputFile)
                {
                    var prop = new InputParameter(item.LongParam, item.InputValue, item.PassthroughParam);
                    Properties.Add(prop);
                }
            }
        }

        public string ToBash()
        {
            return "";
        }

        public string Serialize()
        {
            string props =  JsonConvert.SerializeObject(Properties, Formatting.Indented);

            string s = $"\"{Name}\": {{\n\"Properties\": {props}\n}},\n";
            return s;

        }

        public static ConfigModel Deserialize(string json)
        {
            JObject jObject = JObject.Parse(json);
            
            return JsonConvert.DeserializeObject<ConfigModel>(json);
        }
    }
}
