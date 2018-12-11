using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using bashGeneratorSharedModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
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
        private StorageFolder _bashWizardFolder = null;
        private StorageFolder _bashScriptFolder = null;
        private Dictionary<string, string> _userBashDict = null;
        private bool _opening = false;
        public MainPage()
        {
            this.InitializeComponent();
            Parameters.CollectionChanged += Parameters_CollectionChanged;
            _userBashDict = new Dictionary<string, string>
            {
                ["__USER_CODE_1__"] = "",
                ["__USER_CODE_2__"] = "",
                ["__USER_DELETE_CODE__"] = "",
                ["__USER_VERIFY_CODE__"] = "",
                ["__USER_CREATE_CODE__"] = ""
            };
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
        public static readonly DependencyProperty CreateLogFileProperty = DependencyProperty.Register("CreateLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateLogFileChanged));
        public static readonly DependencyProperty AcceptsInputFileProperty = DependencyProperty.Register("AcceptsInputFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, AcceptsInputFileChanged));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata("", ScriptNameChanged));
        public static readonly DependencyProperty CreateVerifyDeleteProperty = DependencyProperty.Register("CreateVerifyDelete", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateVerifyDeleteChanged));

        public bool CreateVerifyDelete
        {
            get => (bool)GetValue(CreateVerifyDeleteProperty);
            set => SetValue(CreateVerifyDeleteProperty, value);
        }
        private static void CreateVerifyDeleteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as MainPage;
            var depPropValue = (bool)e.NewValue;
            depPropClass?.SetCreateVerifyDelete(depPropValue);
        }
        private void SetCreateVerifyDelete(bool value)
        {

            ParameterItem param = new ParameterItem()
            {
                LongParameter = "create",
                ShortParameter = "c",
                VariableName = "create",
                Description = "creates the resource",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, value);
            param = new ParameterItem()
            {
                LongParameter = "verify",
                ShortParameter = "v",
                VariableName = "verify",
                Description = "verifies the script ran correctly",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, value);
            param = new ParameterItem()
            {
                LongParameter = "delete",
                ShortParameter = "d",
                VariableName = "delete",
                Description = "deletes whatever the script created",
                RequiresInputString = false,
                Default = "false",
                RequiredParameter = false,
                ValueIfSet = "true"
            };
            AddOptionParameter(param, value);

        }


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
            ParameterItem param = new ParameterItem()
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

            AddOptionParameter(param, newValue);

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
            ParameterItem logParameter = new ParameterItem()
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

            AddOptionParameter(logParameter, value);

        }

        private void AddOptionParameter(ParameterItem item, bool add)
        {
            if (_opening)
            {
                return;
            }

            ParameterItem param = null;
            foreach (var p in Parameters)
            {
                if (p.VariableName == item.VariableName)
                {
                    param = p;
                    break;
                }
            }

            //
            //  need to have the right parameter for long line to work correctly -- make sure it is there, and if not, add it.
            if (add && param == null)
            {
                Parameters.Add(item);
                item.PropertyChanged += ParameterPropertyChanged;
            }
            else if (!add && param != null)
            {
                param.PropertyChanged -= ParameterPropertyChanged;
                Parameters.Remove(param);
            }

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
                ConfigModel model = new ConfigModel(ScriptName, Parameters, CreateLogFile, AcceptsInputFile, CreateVerifyDelete);
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

            if (_fileBashWizard != null)
            {
                FileIO.WriteTextAsync(_fileBashWizard, this.Json).AsTask().RunSynchronously();
            }
            if (_fileBashScript != null)
            {
                FileIO.WriteTextAsync(_fileBashScript, this.BashScript).AsTask().RunSynchronously();
            }

            //var ignored = CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //{
            //    try
            //    {
            //        await Save();
            //        this.InputSection = "Async Save worked.";
            //    }
            //    catch { }  // because we are doing this an an asyc way, it is very possible that the file is locked.  we'll just each the exception and it will (eventually) save
            //});

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
            ConfigModel model = new ConfigModel(ScriptName, list, CreateLogFile, AcceptsInputFile, CreateVerifyDelete);
            try
            {
                string template = model.ToBash();
                StringBuilder sb = new StringBuilder(template);
                foreach (var kvp in _userBashDict)
                {
                    sb.Replace(kvp.Key, kvp.Value);
                }

                return sb.ToString();                
            }
            catch (Exception e)
            {
                return $"Exception caught creating bash script:\n\n{e.Message}";
            }
        }

        private string GenerateInputBash()
        {
            var list = new List<ParameterItem>(Parameters);
            ConfigModel model = new ConfigModel(ScriptName, list, CreateLogFile, AcceptsInputFile, CreateVerifyDelete);
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
            ConfigModel model = new ConfigModel(ScriptName, list, CreateLogFile, AcceptsInputFile, CreateVerifyDelete);
            return model.Serialize();
        }

        private async Task GetFolders()
        {
            _bashWizardFolder = await GetSaveFolder("Bash Wizard files", "BashWizardFolder", ".bw");
            _bashScriptFolder = await GetSaveFolder("Bash Scripts", "BashScriptFolder", ".sh");
        }

        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            try
            {
                await GetFolders();

                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List,

                };

                picker.FileTypeFilter.Add(".bw");
                _fileBashWizard = await picker.PickSingleFileAsync();
            }
            catch (Exception ex)
            {
                BashScript = "Error: " + ex.Message;
                _fileBashWizard = null;
            }
            if (_fileBashWizard != null)
            {
                try
                {
                    _opening = true;
                    string s = await FileIO.ReadTextAsync(_fileBashWizard);
                    ApplicationView appView = ApplicationView.GetForCurrentView();
                    appView.Title = $"{_fileBashWizard.Name}";
                    Deserialize(s, true);
                    BashScript = "Error opening file";
                    _fileBashWizard = null;
                    _opening = true;
                    _fileBashScript = await _bashScriptFolder.CreateFileAsync(this.ScriptName, CreationCollisionOption.OpenIfExists);
                    this.BashScript = await FileIO.ReadTextAsync(_fileBashScript);
                    await ParseUserScript(this.BashScript);
                }
                catch (Exception except)
                {
                    this.BashScript = "Error opening file:" + except.Message;
                    _fileBashScript = null;
                }
                finally
                {
                    _opening = false;
                    UpdateTextInfo(true);
                }

            }



        }

        /// <summary>
        ///     this function will either return the storage folder named "token" (we'll use "BashWizardFolder" and "BashScriptFolder") or prompt 
        ///     the user to find the folder for us. we need to do this once for each user.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<StorageFolder> GetSaveFolder(string type, string token, string extension)
        {

            StorageFolder folder = null;
            try
            {
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
                {

                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                    return folder;
                }
            }
            catch { }

            string content = $"After clicking on \"Close\" pick the default location for all your {type}.\nYou will only have to do this once.";
            MessageDialog dlg = new MessageDialog(content, "Bash Wizard");
            try
            {
                await dlg.ShowAsync();

                FolderPicker picker = new FolderPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };

                picker.FileTypeFilter.Add(extension);

                folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder);
                }
                else
                {
                    folder = ApplicationData.Current.LocalFolder;
                }


                return folder;
            }
            catch (Exception except)
            {
                Debug.WriteLine(except.ToString());
            }

            return null;
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
                    this.CreateLogFile = result.CreateLogFile;
                    this.AcceptsInputFile = result.AcceptInputFile;
                    this.CreateVerifyDelete = result.CreateVerifyDeletePattern;
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
            UpdateTextInfo(true);
            if (this.ScriptName == "")
            {
                this.BashScript = "You must specify a script name";
                return;
            }
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Bash Wizard Files", new List<string>() { ".bw" });
            savePicker.SuggestedFileName = $"{ScriptName}.bw";
            await GetFolders();
            _fileBashWizard = await savePicker.PickSaveFileAsync();
            string fqn = Path.Combine(_bashScriptFolder.Path);
            if (File.Exists(fqn))
            {
                ContentDialog dlg = new ContentDialog()
                {
                    Title = "Bash Wizard",
                    Content = $"\n{fqn} already exists.\nReplace it?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No"
                };

                dlg.SecondaryButtonClick += (o, i) =>
                {
                    return;
                };


                await dlg.ShowAsync();

            }

            _fileBashScript = await _bashScriptFolder.CreateFileAsync(this.ScriptName, CreationCollisionOption.ReplaceExisting);

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
            try
            {                
                if (_fileBashWizard != null)
                {
                    await FileIO.WriteTextAsync(_fileBashWizard, this.Json);
                }
                if (_fileBashScript != null)
                {
                    await FileIO.WriteTextAsync(_fileBashScript, this.BashScript);
                }
            }
             catch (Exception e)
            {
                this.InputSection = "Exception saving file: " + e.Message;
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
            ConfigModel model = new ConfigModel(ScriptName, Parameters, CreateLogFile, AcceptsInputFile, CreateVerifyDelete);

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

        /// <summary>
        ///     Parse the script into a dictionary that we will use to hold the strings (keys and values) 
        ///     that are needed to replace tokens in the bash templates created by the Model
        /// </summary>
        /// <param name="userBash"></param>
        /// <returns></returns>
        private async Task ParseUserScript(string userBash)
        {

            userBash = userBash.Replace("\r", "");
            string versionLine = "# bashWizard version ";
            int index = userBash.IndexOf(versionLine);
            string userBashVersion = "0.1";
            if (index > 0)
            {
                userBashVersion = userBash.Substring(index + versionLine.Length, 5);
            }

            if (userBashVersion == "0.1")
            {
                //
                //   old style script
                //

                string[] userBashTokens = userBash.Split(new string[] { "# --- END OF BASH WIZARD GENERATED CODE ---", " # --- YOUR SCRIPT ENDS HERE ---" }, StringSplitOptions.RemoveEmptyEntries);
                if (userBashTokens.Length != 3)
                {
                    MessageDialog dlg = new MessageDialog("I can't auto convert a script of this version");
                    await dlg.ShowAsync();
                    return;
                }

                _userBashDict["__USER_CODE_1__"] = userBashTokens[1];


            }
            else if (userBashVersion == "0.900")
            {
                string[] userBashTokens = userBash.Split(new string[] { "# --- USER CODE STARTS HERE ---", "# --- USER CODE ENDS HERE ---" }, StringSplitOptions.RemoveEmptyEntries);
                if (userBashTokens.Length == 7 && this.CreateVerifyDelete) // i'll let them delete the last two
                {
                    _userBashDict["__USER_CODE_1__"] = "";
                    _userBashDict["__USER_CODE_2__"] = "";
                    _userBashDict["__USER_DELETE_CODE__"] = userBashTokens[5];
                    _userBashDict["__USER_VERIFY_CODE__"] = userBashTokens[3];
                    _userBashDict["__USER_CREATE_CODE__"] = userBashTokens[1];
                } else if (userBashTokens.Length != 10)
                {
                    MessageDialog dlg = new MessageDialog("the scriopt file has modified BashWizard comments and can't be auto-merged.  Please manually merge your changes.");
                    await dlg.ShowAsync();
                }
                else
                {

                    _userBashDict["__USER_CODE_1__"] = userBashTokens[9];
                    _userBashDict["__USER_CODE_2__"] = userBashTokens[7];
                    _userBashDict["__USER_DELETE_CODE__"] = userBashTokens[5];
                    _userBashDict["__USER_VERIFY_CODE__"] = userBashTokens[3];
                    _userBashDict["__USER_CREATE_CODE__"] = userBashTokens[1];
                }
            }


        }

        private async void OnTest(object sender, RoutedEventArgs e)
        {
            await Task.Delay(0);
            //StringBuilder newBash = new StringBuilder(GenerateBash().Replace("\r", ""));

            //Dictionary<string, string> userBashDict = await ParseUserScript(this.BashScript);

            //foreach (var kvp in userBashDict)
            //{
            //    newBash.Replace(kvp.Key, kvp.Value);
            //}

            //this.Json = newBash.ToString();
        }
    }
}
