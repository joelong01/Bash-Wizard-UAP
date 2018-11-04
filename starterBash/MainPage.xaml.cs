using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace starterBash
{

    public class MainPageModel
    {
        public string ScriptName { get; set; } = null;
        public List<CommandLineInfo> Parameters { get; set; } = new List<CommandLineInfo>();
        public MainPageModel(string name, ObservableCollection<CommandLineInfoControl> list)
        {
            ScriptName = name;
            if (list != null)
            {
                foreach (var c in list)
                {
                    Parameters.Add(c.ParameterInfo);
                }
            }
        }
    }


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed partial class MainPage : Page
    {
        [JsonProperty]
        public ObservableCollection<CommandLineInfoControl> ControlList { get; set; } = new ObservableCollection<CommandLineInfoControl>();
        private CommandLineInfoControl _selectedItem = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BashScriptProperty = DependencyProperty.Register("BashScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty JsonProperty = DependencyProperty.Register("Json", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public string Json
        {
            get => (string)GetValue(JsonProperty);
            set => SetValue(JsonProperty, value);
        }

        [JsonProperty]
        public string ScriptName
        {
            get => (string)GetValue(ScriptNameProperty);
            set => SetValue(ScriptNameProperty, value);
        }
        public string BashScript
        {
            get => (string)GetValue(BashScriptProperty);
            set => SetValue(BashScriptProperty, value);
        }




        private void OnAddParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            CommandLineInfoControl ctrl = new CommandLineInfoControl();
            ControlList.Add(ctrl);
            ctrl.PropertyChanged += ParameterPropertyChanged;


        }

        private void ParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            BashScript = GenerateBash();
            Json = Serialize();
        }

        private void OnDeleteParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                ControlList.Remove(_selectedItem);
                _selectedItem = null;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0].GetType() == typeof(CommandLineInfoControl))
            {
                _selectedItem = e.AddedItems[0] as CommandLineInfoControl;
            }
        }

        private string GenerateBash()
        {
            string s = "";
            string nl = "\n";
            s += "#!/bin/bash" + nl + nl;
            s += "usage() {" + nl;
            s += "\techo \"Usage: $0 ";
            foreach (var param in ControlList)
            {
                s += $" -{param.ParameterInfo.ShortParam}|--{param.ParameterInfo.LongParam}";
            }

            s += $"\" 1>&2 {nl} ";
            foreach (var param in ControlList)
            {
                s += $"\techo \" -{param.ParameterInfo.ShortParam} | --{param.ParameterInfo.LongParam,-30} {param.ParameterInfo.Description}\"{nl}";
            }

            s += $"\texit 1{nl}";
            s += $"}}{nl}{nl}";
            s += $"# input variables {nl}";
            foreach (var param in ControlList)
            {
                s += $"declare {param.ParameterInfo.VarName}={param.ParameterInfo.Default}{nl}";
            }
            s += nl;
            s += $"# make sure this version of *nix supports the right getopt {nl}";
            s += $"! getopt --test > /dev/null{nl}";
            s += $"if [[ ${{ PIPESTATUS[0]}} -ne 4 ]]; then{nl}";
            s += $"\techo \"I’m sorry, 'getopt --test' failed in this environment.\"{nl}";
            s += $"\texit 1{nl}";
            s += $"fi{nl}{nl}";


            s += "OPTIONS=";
            foreach (var param in ControlList)
            {
                s += $"{param.ParameterInfo.ShortParam}";
                if (param.ParameterInfo.AcceptsValue)
                {
                    s += ":";
                }
            }

            s += nl;


            s += "LONGOPTS=";
            foreach (var param in ControlList)
            {
                s += $"{param.ParameterInfo.LongParam}";
                if (param.ParameterInfo.AcceptsValue)
                {
                    s += ":";
                }
                s += ",";
            }

            s += nl;

            s += $"# -use ! and PIPESTATUS to get exit code with errexit set{nl}";
            s += $"# -temporarily store output to be able to check for errors{nl}";
            s += $"# -activate quoting/enhanced mode (e.g. by writing out “--options”){nl}";
            s += $"# -pass arguments only via   -- \"$@\"   to separate them correctly{nl}";
            s += $"!PARSED =$(getopt--options =$OPTIONS--longoptions =$LONGOPTS--name \"$0\"-- \"$@\"){nl}";
            s += $"if [[ ${{PIPESTATUS[0]}} -ne 0 ]]; then{nl}";
            s += $"\t# e.g. return value is 1{nl}";
            s += $"\t# then getopt has complained about wrong arguments to stdout{nl}";
            s += $"\techo \"you might be running bash on a Mac.  if so, run 'brew install gnu-getopt' to make the command line processing work.\"{nl}";
            s += $"\tusage{nl}";
            s += $"\texit 2{nl}";
            s += $"fi{nl}{nl}";


            s += $"# read getopt’s output this way to handle the quoting right:{nl}";
            s += $"eval set -- \"$PARSED\"{nl}";
            s += $"# now enjoy the options in order and nicely split until we see --{nl}";

            s += $"while true; do{nl}";
            s += $"\tcase \"$1\" in{nl}";
            foreach (var param in ControlList)
            {

                s += $"\t\t-{param.ParameterInfo.ShortParam}|--{param.ParameterInfo.LongParam}){nl}";
                s += $"\t\t{param.ParameterInfo.VarName}=\"{param.ParameterInfo.SetVal}\"{nl}";
                s += $"\t\tshift ";
                if (param.ParameterInfo.AcceptsValue)
                {
                    s += $"2{nl}";
                }
                else
                {
                    s += $"1{nl}";
                }
                s += $"\t;;{nl}";
            }
            s += $"\t--){nl}\t\tshift{nl}\t\tbreak{nl}\t;;{nl}\t\t*){nl}\t\techo \"Invalid option $1 $2\"{nl}\t\texit 3{nl}\t;;{nl}\tesac{nl}done{nl}{nl}if{nl}";
            string shortString = "";
            foreach (var param in ControlList)
            {
                if (param.ParameterInfo.Required)
                {
                    shortString += $"[-z \"${{{param.ParameterInfo.VarName}}}\" ] || ";
                }
            }
            if (shortString.Length > 3)
            {
                shortString = shortString.Substring(0, shortString.Length - " || ".Length);
                s += $"{shortString}";
            }


            s += $"]; then{nl}";
            s += $"\tusage{nl}";
            s += $"\texit 2{nl}";
            s += $"fi{nl}{nl}";

            s += $"# ================ END OF STARTERBASH.EXE GENERATED CODE ================{nl}{nl}";

            s += $"# start writing you script!{nl}{nl}";

            return s;
        }

        private void OnUpdate(object sender, RoutedEventArgs e)
        {


            BashScript = GenerateBash();
        }

        private void OnTest(object sender, RoutedEventArgs e)
        {
            CommandLineInfoControl ctrl = new CommandLineInfoControl();
            CommandLineInfo info = new CommandLineInfo
            {
                ShortParam = "r",
                Description = "Azure Resource Group",
                VarName = "resourceGroup",
                Default = "",
                AcceptsValue = true,
                LongParam = "rource-group",
                Required = true

            };

            ctrl.ParameterInfo = info;
            ctrl.PropertyChanged += ParameterPropertyChanged;
            ControlList.Add(ctrl);
            

            ctrl = new CommandLineInfoControl();
            info = new CommandLineInfo
            {
                ShortParam = "l",
                Description = "the location of the VMs",
                LongParam = "location",
                VarName = "location",
                Default = "westus2",
                AcceptsValue = true,
                Required = true

            };

            ctrl.ParameterInfo = info;
            ctrl.PropertyChanged += ParameterPropertyChanged;
            ControlList.Add(ctrl);

            ctrl = new CommandLineInfoControl();
            info = new CommandLineInfo
            {
                ShortParam = "d",
                Description = "delete the resource group if it already exists",
                LongParam = "delete",
                VarName = "delete",
                Default = "false",
                AcceptsValue = false,
                Required = false,
                SetVal = "true"
            };

            ctrl.ParameterInfo = info;
            ctrl.PropertyChanged += ParameterPropertyChanged;
            ControlList.Add(ctrl);

            ScriptName = "./createResourceGroup.sh";
            Json = Serialize();
            BashScript = GenerateBash();
        }

        private string Serialize()
        {
            MainPageModel model = new MainPageModel(ScriptName, ControlList);
            return JsonConvert.SerializeObject(model, Formatting.Indented);
        }

        private void Deserialize()
        {

        }

        private void OnSerialize(object sender, RoutedEventArgs e)
        {
            //MainPageModel model = new MainPageModel(ScriptName, ControlList);
            //Json = JsonConvert.SerializeObject(model, Formatting.Indented);

            //var result = JsonConvert.DeserializeObject<MainPageModel>(BashScript);
            //foreach (var c in result.ControlList)
            //{
            //    this.ControlList.Add(c);
            //}

            //ScriptName = result.ScriptName;
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("BASH parameters", new List<string>() { ".param" });
            savePicker.SuggestedFileName = $"{ScriptName}.param";
            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            string toSave = Serialize();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, toSave);

            }
        }

        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List

            };

            picker.FileTypeFilter.Add(".param");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                this.Json = await FileIO.ReadTextAsync(file);

                ControlList.Clear();
                var result = JsonConvert.DeserializeObject<MainPageModel>(this.Json);
                if (result != null)
                {
                    foreach (var c in result.Parameters)
                    {
                        var ctrl = new CommandLineInfoControl()
                        {
                            ParameterInfo = c
                        };

                        ControlList.Add(ctrl);
                    }
                    ScriptName = result.ScriptName;
                }
            }

        }

        private void Text_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }


        //var result = JsonConvert.DeserializeObject<JArray> (BashScript);
        //foreach (var item in result)
        //{
        //    CommandLineInfo info = JsonConvert.DeserializeObject<CommandLineInfo>(item.ToString());
        //    CommandLineInfoControl ctrl = new CommandLineInfoControl
        //    {
        //        ParameterInfo = info
        //    };
        //    ControlList.Add(ctrl);
        //}


    }
}
