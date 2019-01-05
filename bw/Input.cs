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
        
    }
    public class InputValidation
    {

        public Dictionary<string, Parameter> Flags { get; set; } = new Dictionary<string, Parameter>();
        public InputValidation(Parameter[] parameters)
        {
            foreach (var f in parameters)
            {
                Flags[f.Name] = f; // you can access it by name
                Flags[f.ShortName] = f;
                Flags[f.LongName] = f;
            }
        }

        public void SetAndValidate(string[] inputs)
        {
            int i = 0;
            if (inputs.Length == 0)
            {
                throw new Exception("bad input");
            }
            try
            {
                for (i = 0; i < inputs.Length; i++)
                {
                    Parameter parameter = Flags[inputs[i]];
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
                    }
                }
            }
            catch
            {
                throw new Exception($"Bad Parameter {inputs[i]}");
            }


        }

        public bool IsFlagSet(string flag)
        {
            if (Flags.TryGetValue(flag, out Parameter parameter))
            {
                if (parameter.Value != "")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public string GetValue(string flag)
        {
            Parameter parameter = Flags[flag];
            return parameter.Value;
        }

    }
}
