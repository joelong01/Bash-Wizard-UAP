using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace starterBash
{

    public class MainPageModel
    {

        public string ScriptName { get; set; } = null;
        public bool EchoInput { get; set; } = true;
        public bool CreateLogLines { get; set; } = true;
        public List<ParameterItem> Parameters { get; set; } = new List<ParameterItem>();

        public MainPageModel(string name, ObservableCollection<ParameterItem> list, bool echoInput, bool createLogLines)
        {
            ScriptName = name;
            if (list != null)
            {
                Parameters.AddRange(list);
            }
            EchoInput = echoInput;
            CreateLogLines = createLogLines;
        }
    }


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        public ObservableCollection<ParameterItem> Parameters { get; set; } = new ObservableCollection<ParameterItem>();
        private ParameterItem _selectedItem = null;
        private StorageFile _file = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BashScriptProperty = DependencyProperty.Register("BashScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty JsonProperty = DependencyProperty.Register("Json", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty EchoInputProperty = DependencyProperty.Register("EchoInput", typeof(bool), typeof(MainPage), new PropertyMetadata(true, EchoInputChanged));
        public static readonly DependencyProperty CreateLogLinesProperty = DependencyProperty.Register("CreateLogLines", typeof(bool), typeof(MainPage), new PropertyMetadata(true, CreateLogLinesChanged));
        public bool CreateLogLines
        {
            get => (bool)GetValue(CreateLogLinesProperty);
            set => SetValue(CreateLogLinesProperty, value);
        }
        private static void CreateLogLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetCreateLogLines(depPropValue);
        }
        private void SetCreateLogLines(bool value)
        {
            UpdateTextInfo(true);
        }


        public bool EchoInput
        {
            get => (bool)GetValue(EchoInputProperty);
            set => SetValue(EchoInputProperty, value);
        }
        private static void EchoInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetEchoInput(depPropValue);
        }
        private void SetEchoInput(bool value)
        {
            UpdateTextInfo(true);
        }



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
            ListBox_Parameters.ScrollIntoView(param);
            ListBox_Parameters.SelectedItem = param;

            splitView.IsPaneOpen = false;

        }

        private void ParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateTextInfo(true);
        }

        private void OnDeleteParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                Parameters.Remove(_selectedItem);
                _selectedItem = null;
                UpdateTextInfo(true);
                if (Parameters.Count > 0)
                {
                    var param = Parameters[Parameters.Count - 1];
                    ListBox_Parameters.ScrollIntoView(param);
                    ListBox_Parameters.SelectedItem = param;

                }
            }

            splitView.IsPaneOpen = false;
        }



        private void UpdateTextInfo(bool setJsonText)
        {
            BashScript = GenerateBash();
            if (setJsonText)
            {
                Json = Serialize();
            }

            splitView.IsPaneOpen = false;
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
            HashSet<string> shortNames = new HashSet<string>();
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
            sb.Append($"\techo \"\"{nl}");
            string required = "";

            foreach (var param in Parameters)
            {
                if (param.Required)
                {
                    required = "(Required)";
                }
                else
                {
                    required = "(Optional)";
                }
                sb.Append($"\techo \" -{param.ShortParam} | --{param.LongParam,-30} {required,-15} {param.Description}\"{nl}");
            }
            sb.Append($"\techo \"\"{nl}");
            sb.Append($"\texit 1{nl}");
            sb.Append($"}}{nl}{nl}");

            // echoInput()

            sb.Append($"echoInput() {{ {nl}\techo \"{ScriptName}:\"{nl}");
            foreach (var param in Parameters)
            {
                sb.Append($"\techo \"\t{param.VarName,-30} ${param.VarName}\"{nl}");
            }
            sb.Append($"}}{nl}{nl}");

            // input variables
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
            sb.Append($"# -activate quoting/enhanced mode (e.g. by writing out \"--options\"){nl}");
            sb.Append($"# -pass arguments only via   -- \"$@\"   to separate them correctly{nl}");
            sb.Append("! PARSED=$(getopt --options=$OPTIONS --longoptions=$LONGOPTS --name \"$0\" -- \"$@\")");
            sb.Append($"{nl}");
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
                sb.Append($"\t\t\t{param.VarName}={param.SetVal}{nl}");
                sb.Append($"\t\t\tshift ");
                if (param.AcceptsValue)
                {
                    sb.Append($"2{nl}");
                }
                else
                {
                    sb.Append($"1{nl}");
                }
                sb.Append($"\t\t;;{nl}");
            }
            sb.Append($"\t\t--){nl}\t\t\tshift{nl}\t\t\tbreak{nl}\t\t;;{nl}\t\t*){nl}\t\t\techo \"Invalid option $1 $2\"{nl}\t\t\texit 3{nl}\t\t;;{nl}\tesac{nl}done{nl}{nl}if{nl}");
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
            sb.Append($"\techo \"\"{nl}");
            sb.Append($"\techo \"Required parameter missing! \"{nl}");
            sb.Append($"\techoInput #make it easy to see what is missing{nl}");
            sb.Append($"\techo \"\"{nl}");
            sb.Append($"\tusage{nl}");
            sb.Append($"\texit 2{nl}");
            sb.Append($"fi{nl}{nl}");

            if (this.EchoInput == true)
            {
                sb.Append($"echoInput{nl}{nl}");
            }

            /*
             *declare LOG_FILE="${logFileDir}createResourceGroup.log"
            mkdir $logFileDir  2>> /dev/null
            rm -f $LOG_FILE  >> /dev/null


            time=$(date +"%m/%d/%y @ %r")
            echo "started: $time" >> $LOG_FILE 
             * 
             * 
             */
            if (this.CreateLogLines)
            {
                sb.Append($"declare LOG_FILE=\"${{logFileDir}}{this.ScriptName}.log\"{nl}");
                sb.Append($"mkdir $logFileDir  2>> /dev/null{nl}");
                sb.Append($"rm -f $LOG_FILE  >> /dev/null{nl}");
                sb.Append($"time=$(date +\"%m/%d/%y @ %r\"){nl}");
                sb.Append($"echo \"started: $time\" >> $LOG_FILE{nl}");
            }
            sb.Append($"#---------- see https://github.com/joelong01/starterBash ----------------{nl}");
            sb.Append($"# ================ END OF STARTERBASH.EXE GENERATED CODE ================{nl}{nl}");
            return sb.ToString();
        }

        private void OnUpdate(object sender, RoutedEventArgs e)
        {

            UpdateTextInfo(true);
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
            UpdateTextInfo(true);
        }

        private string Serialize()
        {
            MainPageModel model = new MainPageModel(ScriptName, Parameters, EchoInput, CreateLogLines);
            return JsonConvert.SerializeObject(model, Formatting.Indented);
        }




        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List

            };

            picker.FileTypeFilter.Add(".param");
            _file = await picker.PickSingleFileAsync();
            if (_file != null)
            {
                string s = await FileIO.ReadTextAsync(_file);

                Deserialize(s, true);
            }



        }

        private void Deserialize(string s, bool setJsonText)
        {
            if (setJsonText)
            {
                this.Json = s;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<MainPageModel>(s);
                if (result != null)
                {
                    Parameters.Clear();

                    foreach (var param in result.Parameters)
                    {

                        Parameters.Add(param);
                        param.PropertyChanged += ParameterPropertyChanged;
                    }

                    this.ScriptName = result.ScriptName;
                    this.EchoInput = result.EchoInput;
                    UpdateTextInfo(setJsonText);
                }
            }
            catch (Exception)
            {
                BashScript = $"Exception thrown parsing JSON file.";
            }


        }

        private void Text_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = !splitView.IsPaneOpen;
        }

        private void OnAddParameter(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ParameterItem param = new ParameterItem();
            Parameters.Add(param);
            param.PropertyChanged += ParameterPropertyChanged;
        }

        private void OnNew(object sender, RoutedEventArgs e)
        {
            BashScript = "";
            Json = "";
            Parameters.Clear();
            ScriptName = "";
            _file = null;
        }

        private async void OnSaveAs(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("BASH parameters", new List<string>() { ".param" });
            savePicker.SuggestedFileName = $"{ScriptName}.param";
            _file = await savePicker.PickSaveFileAsync();
            await Save();

        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (_file == null)
            {
                OnSaveAs(sender, e);
            }
            else
            {
                await Save();
            }

        }

        private async Task Save()
        {
            string toSave = Serialize();
            if (_file != null)
            {
                await FileIO.WriteTextAsync(_file, toSave);
            }
            UpdateTextInfo(true);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateTextInfo(true);
        }

        private void Json_LostFocus(object sender, RoutedEventArgs e)
        {
            var txtBox = sender as TextBox;

            this.Deserialize(txtBox.Text, false);
        }
        private static bool IsCtrlKeyPressed()
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        /// <summary>
        ///     CTRL+P will Parse the JSON field and if sucessful update the rest of the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Json_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.P && IsCtrlKeyPressed())
            {
                var txtBox = sender as TextBox;

                this.Deserialize(txtBox.Text, false);

            }
        }


    }
}
