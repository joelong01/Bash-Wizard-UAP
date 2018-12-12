using bashGeneratorSharedModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
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
        private string _userCode = "";


        private bool _opening = false;
        public MainPage()
        {
            this.InitializeComponent();
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
        public static readonly DependencyProperty CreateLogFileProperty = DependencyProperty.Register("CreateLogFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateLogFileChanged));
        public static readonly DependencyProperty AcceptsInputFileProperty = DependencyProperty.Register("AcceptsInputFile", typeof(bool), typeof(MainPage), new PropertyMetadata(false, AcceptsInputFileChanged));
        public static readonly DependencyProperty ScriptNameProperty = DependencyProperty.Register("ScriptName", typeof(string), typeof(MainPage), new PropertyMetadata("", ScriptNameChanged));
        public static readonly DependencyProperty CreateVerifyDeleteProperty = DependencyProperty.Register("CreateVerifyDelete", typeof(bool), typeof(MainPage), new PropertyMetadata(false, CreateVerifyDeleteChanged));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(MainPage), new PropertyMetadata("", DescriptionChanged));
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        private static void DescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainPage depPropClass = d as MainPage;
            string depPropValue = (string)e.NewValue;
            depPropClass.SetDescription(depPropValue);
        }
        private void SetDescription(string value)
        {
            UpdateTextInfo(true);
        }


        public bool CreateVerifyDelete
        {
            get => (bool)GetValue(CreateVerifyDeleteProperty);
            set => SetValue(CreateVerifyDeleteProperty, value);
        }
        private static void CreateVerifyDeleteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainPage depPropClass = d as MainPage;
            bool depPropValue = (bool)e.NewValue;
            depPropClass?.SetCreateVerifyDelete(depPropValue);
        }
        private async void SetCreateVerifyDelete(bool value)
        {
            if (!value) // deselecting
            {
                string[] functions = new string[] { "onVerify", "onCreate", "onDelete" };
                string err = "";

                foreach (string f in functions)
                {
                    if (ConfigModel.FunctionExists(_userCode, f))
                    {
                        err += f + "\n";
                    }
                }

                if (err != "")
                {

                    MessageDialog dlg = new MessageDialog($"You can unselected the Create, Verify, Delete pattern, but you have the following functions implemented:\n{err}\n\nManually fix the user code to not need these functions before removing this option.");
                    await dlg.ShowAsync();
                    CreateVerifyDelete = true;
                    return;
                }
            }



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
            MainPage depPropClass = d as MainPage;
            string depPropValue = (string)e.NewValue;
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
            MainPage depPropClass = d as MainPage;
            bool depPropValue = (bool)e.NewValue;
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
            MainPage depPropClass = d as MainPage;
            bool depPropValue = (bool)e.NewValue;
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
            foreach (ParameterItem p in Parameters)
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
                ConfigModel model = CreateConfigModel();
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
                    ParameterItem param = Parameters[Parameters.Count - 1];
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

            string script = GenerateBash();
            string json = SerializeParameters().Replace("\n", "\n# "); // put the # back in front of the JSON

            BashScript = script + "# --- BEGIN BASH WIZARD JSON THIS TEXT EDITABLE IN THE BASH WIZARD---\n" + json;


            splitView.IsPaneOpen = false;
            InputSection = GenerateInputBash();

            //  Save().RunSynchronously();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0].GetType() == typeof(ParameterItem))
            {
                _selectedItem = e.AddedItems[0] as ParameterItem;
            }
        }
        //
        //  this gets called in multiple places and I got tired of having to update a bunch of lines everytime I added a property
        private ConfigModel CreateConfigModel()
        {
            List<ParameterItem> list = new List<ParameterItem>(Parameters);
            return new ConfigModel(ScriptName, list, CreateLogFile, AcceptsInputFile, CreateVerifyDelete, Description);
        }
        private string GenerateBash()
        {

            ConfigModel model = CreateConfigModel();
            try
            {
                string template = model.ToBash(_userCode);
                return template;
            }
            catch (Exception e)
            {
                return $"Exception caught creating bash script:\n\n{e.Message}";
            }
        }

        private string GenerateInputBash()
        {
            ConfigModel model = CreateConfigModel();
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
            return CreateConfigModel().Serialize();
        }



        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            try
            {
                // await GetFolders();

                FileOpenPicker picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List,

                };

                picker.FileTypeFilter.Add(".sh");
                _fileBashWizard = await picker.PickSingleFileAsync();
                _opening = true;
                string s = await FileIO.ReadTextAsync(_fileBashWizard);
                ApplicationView appView = ApplicationView.GetForCurrentView();
                appView.Title = $"{_fileBashWizard.Name}";
                ParseAndDeserialize(s, true);

            }
            catch (Exception ex)
            {
                BashScript = "Error: " + ex.Message;
                _fileBashWizard = null;
            }
            finally
            {
                if (_fileBashWizard != null)
                {
                    UpdateTextInfo(true);
                }
                _opening = false;
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


        /// <summary>
        ///     Loads a bash file, parses it, and loads BashWizard config
        ///     1. looks at version
        ///     2. parses out user code vs. wizard code
        ///     3. loads and parses the JSON section
        ///     4. 
        ///     general form of the file is
        ///     #!/bin/bash
        /// #---------- see https://github.com/joelong01/Bash-Wizard----------------
        /// # bashWizard version 0.900
        ///     --BASH WIZARD CODE --
        ///     # --- BEGIN USER CODE ---
        ///         --USER CODE --
        ///     # --- END USER CODE ---
        ///    --BASH WIZARD CODE --
        /// # --- BEGIN BASH WIZARD JSON THIS TEXT EDITABLE IN THE BASH WIZARD---
        ///     -- JSON CONFIG --
        /// 
        /// </summary>
        /// <param name="bashFile"></param>
        /// <param name="setJsonText"></param>
        private void ParseAndDeserialize(string bashFile, bool setJsonText)
        {
            bashFile = bashFile.Replace("\r", ""); // string out CR - 
            string versionLine = "# bashWizard version ";
            string[] commentDelimeters = null;
            int USER_CODE = 1;
            int BASH_WIZARD_JSON = 3;
            int index = bashFile.IndexOf(versionLine);
            string userBashVersion = "0.1";
            if (index > 0)
            {
                userBashVersion = bashFile.Substring(index + versionLine.Length, 5);
            }
            else
            {
                this.BashScript = "The Bash Wizard couldn't find the version of this file.";
                return;
            }

            if (userBashVersion != "0.900")
            {
                this.BashScript = "The Bash Wizard doesn't know how to open this file version.";
                return;


            }


            commentDelimeters = new string[] { "# --- BEGIN BASH WIZARD JSON THIS TEXT EDITABLE IN THE BASH WIZARD---", "# --- BEGIN USER CODE ---", "# --- END USER CODE ---" };



            try
            {
                //
                //  get the JSON

                string[] bashWizardTokens = bashFile.Split(commentDelimeters, StringSplitOptions.RemoveEmptyEntries);
                //
                //  bashWizardTokens[0]: beginning of the file.  should never be user modified
                //  bashWizardTokens[1]: the user code
                //  bashWizardTokens[2]: the end of the BashWizard generated code
                //  bashWizardTokens[3]: the BashWizard bashWizardTokens[0]:
                //
                if (bashWizardTokens.Length != commentDelimeters.Length + 1)
                {
                    string errMessage = "This is not a BashWizard file.  Coult not find the comment(s):\n";
                    foreach (string comment in commentDelimeters)
                    {
                        int loc = bashWizardTokens[0].IndexOf(comment);
                        if (loc == -1)
                        {
                            errMessage += errMessage + comment + "\n";
                        }
                    }
                    this.BashScript = errMessage;
                    return;
                }

                string json = bashWizardTokens[BASH_WIZARD_JSON].Replace("#", ""); // strip comments

                //
                //  now parse the sript side
                _userCode = bashWizardTokens[USER_CODE];


                ConfigModel result = ConfigModel.Deserialize(json);
                if (result != null)
                {
                    _opening = true;
                    Parameters.Clear();

                    foreach (ParameterItem param in result.Parameters)
                    {
                        Parameters.Add(param);
                        param.PropertyChanged += ParameterPropertyChanged;
                    }
                    this.ScriptName = result.ScriptName;
                    this.CreateLogFile = result.CreateLogFile;
                    this.AcceptsInputFile = result.AcceptInputFile;
                    this.CreateVerifyDelete = result.CreateVerifyDeletePattern;
                    this.Description = result.Description;
                    _opening = false;


                }
            }
            catch (Exception e)
            {
                BashScript = $"Exception thrown parsing JSON file.\n" + e.Message;
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
                MessageDialog dialog = new MessageDialog("Create a new bash script?")
                {
                    Title = "Starter Bash"
                };
                dialog.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                dialog.Commands.Add(new UICommand { Label = "No", Id = 1 });
                IUICommand ret = await dialog.ShowAsync();
                if ((int)ret.Id == 1)
                {
                    return;
                }
            }
            _userCode = "";
            AcceptsInputFile = false;
            CreateVerifyDelete = false;
            CreateLogFile = false;
            BashScript = "";
            Json = "";
            Parameters.Clear();
            ScriptName = "";
            Description = "";
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
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Bash Wizard Files", new List<string>() { ".sh" });
            savePicker.SuggestedFileName = $"{ScriptName}";
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
            try
            {
                if (_fileBashWizard != null)
                {
                    await FileIO.WriteTextAsync(_fileBashWizard, this.BashScript.Replace("\r", ""));
                }
            }
            catch (Exception e)
            {
                this.InputSection = "Exception saving file: " + e.Message;
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateTextInfo(true);
        }

        //private void Json_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    TextBox txtBox = sender as TextBox;

        //    this.ParseAndDeserialize(txtBox.Text, false);
        //}
        private static bool IsCtrlKeyPressed()
        {
            CoreVirtualKeyStates ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        /// <summary>
        ///     CTRL+P will Parse the JSON field and if sucessful update the rest of the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void Json_KeyUp(object sender, KeyRoutedEventArgs e)
        //{
        //    if (e.Key == VirtualKey.P && IsCtrlKeyPressed())
        //    {
        //        TextBox txtBox = sender as TextBox;

        //        this.ParseAndDeserialize(txtBox.Text, false);

        //    }
        //}


        private void OnCopyTopBash(object sender, RoutedEventArgs e)
        {

            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(BashScript);
            Clipboard.SetContent(dataPackage);
        }

        private void OnCopyJson(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(this.Json);
            Clipboard.SetContent(dataPackage);

        }

        private void LongName_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_LostFocus(sender, e);


        }

        private async void OnGetDebugConfig(object sender, RoutedEventArgs e)
        {
            ConfigModel model = CreateConfigModel();

            DebugConfig dbgWindow = new DebugConfig()
            {
                ConfigModel = model
            };

            await dbgWindow.ShowAsync(ContentDialogPlacement.Popup);
        }




        private void OnTest(object sender, RoutedEventArgs e)
        {
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
