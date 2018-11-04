using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace starterBash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public ObservableCollection<CommandLineInfoControl> ControlList = new ObservableCollection<CommandLineInfoControl>();
        private CommandLineInfoControl _selectedItem = null;
        
        public MainPage()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BashScriptProperty = DependencyProperty.Register("BashScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata(""));
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

        private void OnUpdate(object sender, RoutedEventArgs e)
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

            BashScript = s;
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
                Required=true
                
            };

            ctrl.ParameterInfo = info;

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


            ControlList.Add(ctrl);

            ScriptName = "./createResourceGroup.sh";

        }
    }
}
