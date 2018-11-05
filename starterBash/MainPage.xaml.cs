using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        public List<ParameterItem> Parameters { get; set; } = new List<ParameterItem>();
        public MainPageModel(string name, ObservableCollection<ParameterItem> list)
        {
            ScriptName = name;
            if (list != null)
            {
                Parameters.AddRange(list);
            }
        }
    }


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        public ObservableCollection<ParameterItem> Parameters { get; set; } = new ObservableCollection<ParameterItem>();
        private ParameterItem _selectedItem = null;

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
            ParameterItem param = new ParameterItem();
            Parameters.Add(param);
            param.PropertyChanged += ParameterPropertyChanged;


        }

        private void ParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateTextInfo();
        }

        private void OnDeleteParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                Parameters.Remove(_selectedItem);
                _selectedItem = null;
                UpdateTextInfo();
            }
        }

        private void UpdateTextInfo()
        {
            BashScript = GenerateBash();
            Json = Serialize();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0].GetType() == typeof(ParameterItem))
            {
                _selectedItem = e.AddedItems[0] as ParameterItem;
            }
        }
        /// <summary>
        ///     put all data validation here
        /// </summary>
        private string ValidateParameters()
        {
            //verify short names are unique
            HashSet<string>  shortNames = new HashSet<string>();
            HashSet<string> longNames = new HashSet<string>();
            foreach (var param in Parameters)
            {
                if (!shortNames.Add(param.ShortParam))
                {
                    return $"{param.ShortParam} exists at least twice.  please fix it.";
                }
                if (!longNames.Add(param.LongParam))
                {
                    return $"{param.LongParam} exists at least twice.  please fix it.";
                }
            }

            return "";

        }
        private string GenerateBash()
        {
            string validateString = ValidateParameters();
            if (validateString != "")
            {
                return validateString;
            }
            StringBuilder sb = new StringBuilder(4096);
            string nl = "\n";
            sb.Append($"#!/bin/bash{nl}{nl}");
            sb.Append($"usage() {{{nl}");
            sb.Append($"\techo \"Usage: $0 ");
            foreach (var param in Parameters)
            {
                sb.Append($" -{param.ShortParam}|--{param.LongParam}");
            }

            sb.Append($"\" 1>&2 {nl} ");
            foreach (var param in Parameters)
            {
                sb.Append($"\techo \" -{param.ShortParam} | --{param.LongParam,-30} {param.Description}\"{nl}");
            }

            sb.Append($"\texit 1{nl}");
            sb.Append($"}}{nl}{nl}");
            sb.Append($"# input variables {nl}");
            foreach (var param in Parameters)
            {
                sb.Append($"declare {param.VarName}={param.Default}{nl}");
            }
            sb.Append($"{nl}");
            sb.Append($"# make sure this version of *nix supports the right getopt {nl}");
            sb.Append($"! getopt --test > /dev/null{nl}");
            sb.Append($"if [[ ${{PIPESTATUS[0]}} -ne 4 ]]; then{nl}");
            sb.Append($"\techo \"I’m sorry, 'getopt --test' failed in this environment.\"{nl}");
            sb.Append($"\texit 1{nl}");
            sb.Append($"fi{nl}{nl}");


            sb.Append("OPTIONS=");
            foreach (var param in Parameters)
            {
                sb.Append($"{param.ShortParam}");
                if (param.AcceptsValue)
                {
                    sb.Append(":");
                }
            }

            sb.Append($"{nl}");


            sb.Append("LONGOPTS=");
            foreach (var param in Parameters)
            {
                sb.Append($"{param.LongParam}");
                if (param.AcceptsValue)
                {
                    sb.Append(":");
                }
                sb.Append(",");
            }

            sb.Append($"{nl}");

            sb.Append($"# -use ! and PIPESTATUS to get exit code with errexit set{nl}");
            sb.Append($"# -temporarily store output to be able to check for errors{nl}");
            sb.Append($"# -activate quoting/enhanced mode (e.g. by writing out “--options”){nl}");
            sb.Append($"# -pass arguments only via   -- \"$@\"   to separate them correctly{nl}");
            sb.Append($"! PARSED=$(getopt--options =$OPTIONS--longoptions =$LONGOPTS--name \"$0\"-- \"$@\"){nl}");
            sb.Append($"if [[ ${{PIPESTATUS[0]}} -ne 0 ]]; then{nl}");
            sb.Append($"\t# e.g. return value is 1{nl}");
            sb.Append($"\t# then getopt has complained about wrong arguments to stdout{nl}");
            sb.Append($"\techo \"you might be running bash on a Mac.  if so, run 'brew install gnu-getopt' to make the command line processing work.\"{nl}");
            sb.Append($"\tusage{nl}");
            sb.Append($"\texit 2{nl}");
            sb.Append($"fi{nl}{nl}");


            sb.Append($"# read getopt’s output this way to handle the quoting right:{nl}");
            sb.Append($"eval set -- \"$PARSED\"{nl}");
            sb.Append($"# now enjoy the options in order and nicely split until we see --{nl}");

            sb.Append($"while true; do{nl}");
            sb.Append($"\tcase \"$1\" in{nl}");
            foreach (var param in Parameters)
            {

                sb.Append($"\t\t-{param.ShortParam}|--{param.LongParam}){nl}");
                sb.Append($"\t\t{param.VarName}=\"{param.SetVal}\"{nl}");
                sb.Append($"\t\tshift ");
                if (param.AcceptsValue)
                {
                    sb.Append($"2{nl}");
                }
                else
                {
                    sb.Append($"1{nl}");
                }
                sb.Append($"\t;;{nl}");
            }
            sb.Append($"\t--){nl}\t\tshift{nl}\t\tbreak{nl}\t;;{nl}\t\t*){nl}\t\techo \"Invalid option $1 $2\"{nl}\t\texit 3{nl}\t;;{nl}\tesac{nl}done{nl}{nl}if{nl}");
            string shortString = "";
            foreach (var param in Parameters)
            {
                if (param.Required)
                {
                    shortString += $"[ -z \"${{{param.VarName}}}\" ] || ";
                }
            }
            if (shortString.Length > 3)
            {
                shortString = shortString.Substring(0, shortString.Length - " || ".Length);
                sb.Append($"{shortString}");
            }


            sb.Append($"; then{nl}");
            sb.Append($"\tusage{nl}");
            sb.Append($"\texit 2{nl}");
            sb.Append($"fi{nl}{nl}");

            sb.Append($"# ================ END OF STARTERBASH.EXE GENERATED CODE ================{nl}");
            

            return sb.ToString();
        }

        private void OnUpdate(object sender, RoutedEventArgs e)
        {

            UpdateTextInfo();
        }

        private void OnTest(object sender, RoutedEventArgs e)
        {

            ParameterItem param = new ParameterItem
            {
                ShortParam = "r",
                Description = "Azure Resource Group",
                VarName = "resourceGroup",
                Default = "",
                AcceptsValue = true,
                LongParam = "rource-group",
                Required = true

            };


            param.PropertyChanged += ParameterPropertyChanged;
            Parameters.Add(param);



            param = new ParameterItem
            {
                ShortParam = "l",
                Description = "the location of the VMs",
                LongParam = "location",
                VarName = "location",
                Default = "westus2",
                AcceptsValue = true,
                Required = true

            };

            param.PropertyChanged += ParameterPropertyChanged;
            Parameters.Add(param);


            param = new ParameterItem
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

            param.PropertyChanged += ParameterPropertyChanged;
            Parameters.Add(param);

            ScriptName = "./createResourceGroup.sh";
            UpdateTextInfo();
        }

        private string Serialize()
        {
            MainPageModel model = new MainPageModel(ScriptName, Parameters);
            return JsonConvert.SerializeObject(model, Formatting.Indented);
        }

        private void Deserialize()
        {

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

                Parameters.Clear();
                var result = JsonConvert.DeserializeObject<MainPageModel>(this.Json);
                if (result != null)
                {
                    foreach (var param in result.Parameters)
                    {

                        Parameters.Add(param);
                        param.PropertyChanged += ParameterPropertyChanged;
                    }


                }

                UpdateTextInfo();
            }



        }

        private void Text_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

    }
}
