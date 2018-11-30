using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using bashGeneratorSharedModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BashWizard
{




    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        public ObservableCollection<ParameterItem> Parameters { get; set; } = new ObservableCollection<ParameterItem>();
        private ParameterItem _selectedItem = null;
        private StorageFile _fileBashWizard = null;
        private StorageFile _fileBashScript = null;
        private bool _opening = false;
        private string _endScript = "";
        public MainPage()
        {
            this.InitializeComponent();
            _endScript = EmbeddedResource.GetResourceFile(Assembly.GetExecutingAssembly(), "EndOfScript.txt");
            Parameters.CollectionChanged += Parameters_CollectionChanged;
        }
        /// <summary>
        ///     I want to update the bash script when the collection is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateTextInfo(true);
        }

        public static readonly DependencyProperty BashScriptProperty = DependencyProperty.Register("BashScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty JsonProperty = DependencyProperty.Register("Json", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty EndScriptProperty = DependencyProperty.Register("EndScript", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty InputSectionProperty = DependencyProperty.Register("InputSection", typeof(string), typeof(MainPage), new PropertyMetadata(""));
        public static readonly DependencyProperty EchoInputProperty = DependencyProperty.Register("EchoInput", typeof(bool), typeof(MainPage), new PropertyMetadata(true, EchoInputChanged));
        public static readonly DependencyProperty CreateLogFileProperty = DependencyProperty.Register("CreateLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateLogFileChanged));
        public static readonly DependencyProperty TeeToLogFileProperty = DependencyProperty.Register("TeeToLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, TeeToLogFileChanged));
        public static readonly DependencyProperty AcceptsInputFileProperty = DependencyProperty.Register("AcceptsInputFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, AcceptsInputFileChanged));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata("", ScriptNameChanged));

        public string ScriptName
        {
            get => (string)GetValue(ScriptNameProperty);
            set => SetValue(ScriptNameProperty, value);
        }
        private static void ScriptNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (string)e.NewValue;
            depPropClass?.SetScriptName(depPropValue);
        }
        private void SetScriptName(string value)
        {
            UpdateTextInfo(true);
        }

        public string InputSection
        {
            get => (string)GetValue(InputSectionProperty);
            set => SetValue(InputSectionProperty, value);
        }
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
                if (param.ShortParameter == "i" && param.LongParameter == "input-file")
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
                        ShortParameter = "i",
                        LongParameter = "input-file",
                        VariableName = "inputFile",
                        Description = "filename that contains the JSON values to drive the script.  command line overrides file",
                        RequiresInputString = true,
                        Default = "", // this needs to be empty because if it is set, the script will try to find the file...
                        RequiredParameter = false,
                        ValueIfSet = "$2"
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

            if (_opening)
            {
                return;
            }

            ParameterItem logParameter = null;
            foreach (var param in Parameters)
            {
                if (param.VariableName == "logDirectory")
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
                    LongParameter = "log-directory",
                    ShortParameter = "l",
                    Description = "directory for the log file.  the log file name will be based on the script name",
                    VariableName = "logDirectory",
                    Default = "\"./\"",
                    RequiresInputString = true,
                    RequiredParameter = false,
                    ValueIfSet = "$2"
                };

                Parameters.Add(logParameter);
                logParameter.PropertyChanged += ParameterPropertyChanged;
            }

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
            if (e.PropertyName == "LongParameter")
            {
                ParameterItem item = sender as ParameterItem;
                ConfigModel model = new ConfigModel(ScriptName, Parameters, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);
                if (item.ShortParameter == "") // dont' pick one if the user already did...
                {
                    for (int i = 0; i < item.LongParameter.Length; i++)
                    {

                        item.ShortParameter = item.LongParameter.Substring(i, 1);
                        if (item.ShortParameter == "")
                        {
                            continue;
                        }
                        if (model.ValidateParameters() == "")
                        {
                            break;
                        }
                    }
                    if (model.ValidateParameters() != "")
                    {
                        BashScript = "pick a short name that works...";
                    }
                }
                if (item.VariableName == "")
                {
                    string[] tokens = item.LongParameter.Split(new char[] { '-' });

                    item.VariableName = tokens[0].ToLower();
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        item.VariableName += tokens[i][0].ToString().ToUpper() + tokens[i].Substring(1);
                    }
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

            if (Parameters.Count == 0)
            {
                return;
            }

            BashScript = GenerateBash();
            if (setJsonText)
            {
                Json = SerializeParameters();
            }

            splitView.IsPaneOpen = false;

            InputSection = GenerateInputBash();

            AsyncSave();
        }

        private void AsyncSave()
        {
            if (_fileBashWizard == null)
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
                    if (_fileBashWizard != null)
                    {
                        await FileIO.WriteTextAsync(_fileBashWizard, toSave);
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
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);
            try
            {
                return model.SerializeInputJson();
            }
            catch (Exception e)
            {
                return $"Exception caught creating bash script:\n\n{e.Message}";
            }
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

            picker.FileTypeFilter.Add(".bw");

            _fileBashWizard = await picker.PickSingleFileAsync();
            if (_fileBashWizard != null)
            {
                try
                {
                    _opening = true;
                    string s = await FileIO.ReadTextAsync(_fileBashWizard);
                    ApplicationView appView = ApplicationView.GetForCurrentView();
                    appView.Title = $"{_fileBashWizard.Name}";
                    Deserialize(s, true);
                }
                catch
                {
                    BashScript = "Error opening file";
                    _fileBashWizard = null;
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
                    _opening = true;
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

                    _opening = false;
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
            _fileBashWizard = null;
        }

        private async void OnSaveAs(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Bash Wizard Files", new List<string>() { ".bw" });
            savePicker.SuggestedFileName = $"{ScriptName}.bw";
            _fileBashWizard = await savePicker.PickSaveFileAsync();


            await Save();

        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (_fileBashWizard == null)
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

            if (_fileBashWizard != null)
            {
                await FileIO.WriteTextAsync(_fileBashWizard, toSave);
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //  UpdateTextInfo(true);
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

        private async void OnGetDebugConfig(object sender, RoutedEventArgs e)
        {
            ConfigModel model = new ConfigModel(ScriptName, Parameters, EchoInput, CreateLogFile, TeeToLogFile, AcceptsInputFile);

            var dbgWindow = new DebugConfig()
            {
                ConfigModel = model
            };

            await dbgWindow.ShowAsync(ContentDialogPlacement.Popup);
        }

        private async void OnUpdateShellScript(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Shell Script Files", new List<string>() { ".sh" });
            savePicker.SuggestedFileName = ScriptName;
            _fileBashScript = await savePicker.PickSaveFileAsync();
            if (_fileBashScript != null)
            {
                try
                {


                    string file = await FileIO.ReadTextAsync(_fileBashScript);
                    file = file.Replace("\r", "");
                    string[] lines = file.Split(new char[] { '\n' }); // assumes Unix style file
                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.BashScript.Replace("\r", ""));
                    bool hitMarker = false;
                    foreach (var line in lines)
                    {
                        if (hitMarker)
                        {
                            sb.Append($"{line}\n");

                        }
                        else if (line.Trim() == "# --- END OF BASH WIZARD GENERATED CODE ---")
                        {
                            hitMarker = true;
                        }
                    }

                    if (hitMarker)
                    {
                        if (_fileBashScript != null)
                        {
                            sb.Replace("\t", "    ");
                            await FileIO.WriteTextAsync(_fileBashScript, sb.ToString());
                        }
                    }
                }
                catch
                {
                    BashScript = "Error opening file";
                    _fileBashScript = null;
                }
                finally
                {
                    _opening = false;
                    this.ScriptName = _fileBashScript.Name;

                }

            }




        }
    }
}
