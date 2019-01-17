using System;
using System.Collections.Generic;

namespace BashWizardConsole
{
    public class Parameter
    {
        public Parameter(string name, string shortName, string longName, bool requiresInput, string value, bool required, string description)
        {
            Name = name;
            ShortName = shortName;
            LongName = longName;
            RequiresInput = requiresInput;
            Value = value;
            Description = description;
            Required = required;

        }

        public bool RequiresInput { get; set; } = false;
        public string ShortName { get; set; } = "";
        public string LongName { get; set; } = "";
        public string Value { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Required { get; set; } = false;

        public override string ToString()
        {
            return $"{ShortName}|{LongName}: [RequiresInput={RequiresInput}] [Value={Value}]";
        }


    }
    public class InputValidation
    {

        public Dictionary<string, Parameter> ValidFlags { get; set; } = new Dictionary<string, Parameter>();
        public Dictionary<string, Parameter> SetFlags { get; set; } = new Dictionary<string, Parameter>();
        //
        //  we pass in a list of possible parameters and put them into a dictionary.  that way if an invalid
        //  parameter is passed in, we can tell because it isn't in the dictionary
        public InputValidation(Parameter[] parameters)
        {
            foreach (var f in parameters)
            {
                ValidFlags[f.Name] = f; // you can access it by name
                ValidFlags[f.ShortName] = f;
                ValidFlags[f.LongName] = f;
            }
        }

        public List<string> SetAndValidate(string[] inputs)
        {
            int i = 0;
            List<string> errors = new List<string>();
            if (inputs.Length == 0)
            {
                return errors; // which will be empty
            }
            
            for (i = 0; i < inputs.Length; i++)
            {
                bool exists = ValidFlags.TryGetValue(inputs[i], out Parameter parameter);
                if (!exists)
                {
                    //
                    //  I always hate it when I pass in a bunch of parameters and have more than 1 wrong.
                    //  you get an error for the first one, fix it, try again and get the error for the second.
                    //  and so on...so we'll go through all of them and tell the user *all* the params that are bad

                    errors.Add($"Bad Parameter {inputs[i]}");
                }
                else
                {

                    if (parameter.RequiresInput)
                    {
                        i++;
                        parameter.Value = inputs[i];
                    }
                    else
                    {
                        parameter.Value = "true"; // would have been nice to make this a template, but you can't have a type <T> in a Dictionary...
                    }

                    SetFlags[parameter.Name] = parameter; // I loop over these in main(), so it is important that it only have the name                  
                }

            }

            return errors;

        }

      
    }
}
