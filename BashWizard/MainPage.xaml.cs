using bashGeneratorSharedModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace bashWizard
{




    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        public ObservableCollection<ParameterItem> Parameters { get; set; } = new ObservableCollection<ParameterItem>();
        private ParameterItem _selectedItem = null;
        private StorageFile _fileBashScript = null;
        private bool _opening = false;
        private string _endScript = "";
        public MainPage()
        {
            this.InitializeComponent();
            _endScript = EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "EndOfScript.txt");
        }

        public static readonly DependencyProperty BashScriptProperty = DependencyProperty.Register("BashScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty JsonProperty = DependencyProperty.Register("Json", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty EchoInputProperty = DependencyProperty.Register("EchoInput", typeof(bool), typeof(MainPage), new PropertyMetadata(true, EchoInputChanged));
        public static readonly DependencyProperty CreateLogFileProperty = DependencyProperty.Register("CreateLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateLogFileChanged));
        public static readonly DependencyProperty TeeToLogFileProperty = DependencyProperty.Register("TeeToLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, TeeToLogFileChanged));
        public static readonly DependencyProperty AcceptsInputFileProperty = DependencyProperty.Register("AcceptsInputFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, AcceptsInputFileChanged));
        public static readonly DependencyProperty EndScriptProperty = DependencyProperty.Register("EndScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public string EndScript
        {
            get => (string)GetValue(EndScriptProperty);
            set => SetValue(EndScriptProperty, value);
        }
        public bool AcceptsInputFile
        {
            get => (bool)GetValue(AcceptsInputFileProperty);
            set => SetValue(AcceptsInputFileProperty, value);
        }
        private static void AcceptsInputFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetAcceptsInputFile(depPropValue);
        }
        private void SetAcceptsInputFile(bool newValue)
        {


            // i is the short name and input-file is the long name for the 
            ParameterItem acceptsInputParam = null;
            //
            //  see if we already have the parameter
            foreach (var param in Parameters)
            {
                if (param.ShortParam == "i" && param.LongParam == "input-file")
                {
                    acceptsInputParam = param;
                    break;
                }
            }
            if (newValue)
            {
                if (acceptsInputParam == null)
                {
                    acceptsInputParam = new ParameterItem()
                    {
                        ShortParam = "i",
                        LongParam = "input-file",
                        VarName = "inputFile",
                        Description = "filename that contains the JSON values to drive the script.  command line overrides file",
                        AcceptsValue = true,
                        Default = $"{ScriptName}.input.json",
                        Required = false,
                        SetVal = "$2"
                    };

                    Parameters.Insert(0, acceptsInputParam);
                }
            }
            else
            {
                if (acceptsInputParam != null)
                {
                    Parameters.Remove(acceptsInputParam);
                }
            }


            UpdateTextInfo(true);
        }




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
            if (value)
            {
                EndScript = _endScript;
            }
            else
            {
                EndScript = "";
            }

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
            depPropClass?.SetCreateLogDir(depPropValue);
        }
        private void SetCreateLogDir(bool value)
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
                    ShortParam = "l",
                    Description = "directory for the log file.  the log file name will be based on the script name",
                    VarName = "logDirectory",
                    Default = "\"./\"",
                    AcceptsValue = true,
                    Required = false,
                    SetVal = "$2"
                };

                Parameters.Add(logParameter);
                logParameter.PropertyChanged += ParameterPropertyChanged;
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
            if (e.PropertyName == "LongParam")
            {
                ParameterItem item = sender as ParameterItem;
                ConfigModel model = new ConfigModel(ScriptName, Parameters, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);
                for (int i = 0; i < item.LongParam.Length; i++)
                {
                    item.ShortParam = item.LongParam.Substring(i, 1);
                    if (model.ValidateParameters() == "")
                    {
                        break;
                    }
                }
                if (model.ValidateParameters() != "")
                {
                    BashScript = "pick a short name that works...";
                }
                string[] tokens = item.LongParam.Split(new char[] { '-' });

                item.VarName = tokens[0].ToLower();
                for (int i = 1; i < tokens.Length; i++)
                {
                    item.VarName += tokens[i][0].ToString().ToUpper() + tokens[i].Substring(1);
                }
            }
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
            if (_opening)
            {
                return;
            }

            BashScript = GenerateBash();
            if (setJsonText)
            {
                Json = SerializeParameters();
            }

            splitView.IsPaneOpen = false;
            AsyncSave();
        }

        private void AsyncSave()
        {
            if (_fileBashScript == null)
            {
                return;
            }

            if (_opening)
            {
                return;
            }

            var ignored = CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    string toSave = SerializeParameters();
                    if (_fileBashScript != null)
                    {
                        await FileIO.WriteTextAsync(_fileBashScript, toSave);
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

        private string GenerateBash()
        {
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);
            try
            {
                return model.ToBash();
            }
            catch (Exception e)
            {
                return $"Exception caught creating bash script:\n\n{e.Message}";
            }
        }

        private string GenerateInputBash()
        {
            return "hnmnmm";
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

        private string SerializeParameters()
        {
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);
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
            _fileBashScript = await picker.PickSingleFileAsync();
            if (_fileBashScript != null)
            {
                try
                {
                    _opening = true;
                    string s = await FileIO.ReadTextAsync(_fileBashScript);
                    Deserialize(s, true);
                }
                catch
                {
                    BashScript = "Error opening file";
                    _fileBashScript = null;
                }
                finally
                {
                    _opening = false;
                    UpdateTextInfo(true);
                }

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
                    this.CreateLogFile = result.CreateLogFile;
                    this.TeeToLogFile = result.TeeToLogFile;
                    this.AcceptsInputFile = result.AcceptInputFile;

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
            _fileBashScript = null;
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
            _fileBashScript = await savePicker.PickSaveFileAsync();


            await Save();

        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (_fileBashScript == null)
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
            string toSave = SerializeParameters();

            if (_fileBashScript != null)
            {
                await FileIO.WriteTextAsync(_fileBashScript, toSave);
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


        private void OnCopyTopBash(object sender, RoutedEventArgs e)
        {

            var dataPackage = new DataPackage();
            dataPackage.SetText(BashScript);
            Clipboard.SetContent(dataPackage);
        }

        private void OnCopyJson(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(this.Json);
            Clipboard.SetContent(dataPackage);

        }

        private void LongName_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_LostFocus(sender, e);


        }
    }
}
