using sharedBashGen;
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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace starterBash
{




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
        public static readonly DependencyProperty CreateLogFileProperty = DependencyProperty.Register("CreateLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateLogFileChanged));
        public static readonly DependencyProperty TeeToLogFileProperty = DependencyProperty.Register("TeeToLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(true, TeeToLogFileChanged));
        public bool TeeToLogFile
        {
            get => (bool)GetValue(TeeToLogFileProperty);
            set => SetValue(TeeToLogFileProperty, value);
        }
        private static void TeeToLogFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetTeeToLogFile(depPropValue);
        }
        private void SetTeeToLogFile(bool value)
        {
            UpdateTextInfo(true);
        }

        public bool CreateLogFile
        {
            get => (bool)GetValue(CreateLogFileProperty);
            set => SetValue(CreateLogFileProperty, value);
        }
        private static void CreateLogFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetCreateLogLines(depPropValue);
        }
        private void SetCreateLogLines(bool value)
        {
            

            ParameterItem logParameter = null;
            foreach (var param in Parameters)
            {
                if (param.VarName == "logFileDir")
                {
                    logParameter = param;
                    break;
                }
            }

            //
            //  need to have the right parameter for long line to work correctly -- make sure it is there, and if not, add it.
            if (value && logParameter == null)
            {
                
                logParameter = new ParameterItem()
                {
                    LongParam = "log-directory",
                    ShortParam = "g",
                    Description = "directory for the log file.  the log file name will be based on the script name",
                    VarName = "logFileDir",
                    Default = "\"./\"",
                    AcceptsValue = true,
                    Required = false,
                    SetVal = "$2"
                };

                Parameters.Add(logParameter);
            }

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
            AsyncSave();
        }

        private void AsyncSave()
        {
            var ignored = CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    string toSave = Serialize();
                    if (_file != null)
                    {
                        await FileIO.WriteTextAsync(_file, toSave);
                    }
                }
                catch { }  // because we are doing this an an asyc way, it is very possible that the file is locked.  we'll just each the exception and it will (eventually) save
            });
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
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, EchoInput, CreateLogFile, TeeToLogFile);
            try
            {
                return model.ToBash();
            }
            catch (Exception e)
            {
                return $"Exception caught creating bash script:\n\n{e.Message}";
            }
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
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, EchoInput, CreateLogFile, TeeToLogFile);
            return model.Serialize();
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
                var result = ConfigModel.Deserialize(s);
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

        private async void OnNew(object sender, RoutedEventArgs e)
        {
            if (BashScript != "")
            {
                var dialog = new MessageDialog("Create a new bash script?")
                {
                    Title = "Starter Bash"
                };
                dialog.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                dialog.Commands.Add(new UICommand { Label = "No", Id = 1 });
                var ret = await dialog.ShowAsync();
                if ((int)ret.Id == 1)
                {
                    return;
                }
            }

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
