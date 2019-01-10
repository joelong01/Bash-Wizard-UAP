using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using bashWizardShared;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BashWizard
{




    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {


        private ParameterItem _selectedItem = null;

        private StorageFile _bashFile = null;
        private StorageFile _jsonFile = null;

        public static readonly DependencyProperty ScriptDataProperty = DependencyProperty.Register("ScriptData", typeof(ScriptData), typeof(MainPage), null);
        public ScriptData ScriptData
        {
            get => (ScriptData)GetValue(ScriptDataProperty);
            set => SetValue(ScriptDataProperty, value);
        }

        public MainPage()
        {

            this.InitializeComponent();

            ScriptData = new ScriptData
            {
                BashScript = "There are three things to do: \n1. Open a Bash File \n2. Start creating a bash file. \n3. Paste a Bash script here and I'll parse it for you!",

            };

            Debug.WriteLine($"ScriptData.Hash: {ScriptData.GetHashCode()}");

        }






        private void OnAddParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ParameterItem param = new ParameterItem();
            ScriptData.Parameters.Add(param);
            ListBox_Parameters.ScrollIntoView(param);
            ListBox_Parameters.SelectedItem = param;
            splitView.IsPaneOpen = false;

        }



        private void OnDeleteParameter(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                ScriptData.Parameters.Remove(_selectedItem);
                _selectedItem = null;
                if (ScriptData.Parameters.Count > 0)
                {
                    ParameterItem param = ScriptData.Parameters[ScriptData.Parameters.Count - 1];
                    ListBox_Parameters.ScrollIntoView(param);
                    ListBox_Parameters.SelectedItem = param;

                }
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

        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            StorageFile file = null;
            try
            {


                FileOpenPicker picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List,

                };

                if (JSONShown)
                {
                    picker.FileTypeFilter.Add(".json");
                    picker.FileTypeFilter.Add(".bw");
                    _jsonFile = await picker.PickSingleFileAsync();
                    file = _jsonFile;
                }
                else
                {

                    picker.FileTypeFilter.Add(".sh");
                    _bashFile = await picker.PickSingleFileAsync();
                    file = _bashFile;
                }

                if (file != null)
                {
                    ScriptData = new ScriptData();
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    DataReader reader = DataReader.FromBuffer(buffer);
                    byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(fileContent);
                    string s = Encoding.UTF8.GetString(fileContent, 0, fileContent.Length);
                    ApplicationView appView = ApplicationView.GetForCurrentView();
                    appView.Title = $"{file.Name}";
                    ScriptData.ScriptName = file.Name;
                    if (s[0] == '{' && !JSONShown)
                    {
                        //
                        //  looks like they opened a JSON file instead of a BASH Script...
                        // 
                        _jsonFile = file;
                        _bashFile = null;
                        this.ScriptData = ScriptData.FromJson(s, "");
                    }
                    else if (s[0] != '#' && !JSONShown)
                    {
                        //
                        //  JSON is shown and they opened a Bash Script....
                        _bashFile = file;
                        _jsonFile = null;
                        this.ScriptData = ScriptData.FromBash(s);

                    }
                    else
                    {
                        this.ScriptData = JSONShown ? ScriptData.FromJson(s, "") : this.ScriptData = ScriptData.FromBash(s);
                    }
                }

            }
            catch (Exception ex)
            {
                ScriptData.ParseErrors.Add($"Error loading file {((file == null) ? "" : file.Name)}");
                ScriptData.ParseErrors.Add($"Exception: {ex.Message}");
                _bashFile = null;
            }
            finally
            {



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
            ScriptData.Parameters.Add(param);

        }



        private async void OnNew(object sender, RoutedEventArgs e)
        {
            if (ScriptData.BashScript != "")
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
            Reset();
        }

        private void Reset()
        {
            ScriptData = new ScriptData();
        }

        private async void OnSaveAs(object sender, RoutedEventArgs e)
        {
            ScriptData.ToBash();
            StorageFile file = null;
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"{ScriptData.ScriptName}"
            };
            if (JSONShown)
            {
                savePicker.FileTypeChoices.Add("JSON files", new List<string>() { ".json" });
                _jsonFile = await savePicker.PickSaveFileAsync();
                file = _jsonFile;
            }
            else
            {
                savePicker.FileTypeChoices.Add("Shell Scripts", new List<string>() { ".sh" });
                _bashFile = await savePicker.PickSaveFileAsync();
                file = _bashFile;
            }


            if (file != null)
            {
                if (ScriptData.ScriptName == null)
                {
                    ScriptData.ScriptName = file.Name;
                }
            }
            await Save();

        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {



            if (_jsonFile == null && JSONShown)
            {
                OnSaveAs(sender, e);
                return;
            }
            else if (_bashFile == null && !JSONShown)
            {
                OnSaveAs(sender, e);
                return;
            }

            await Save();
            

        }

        private async Task Save()
        {
            try
            {
                if (_jsonFile != null)
                {
                    await FileIO.WriteTextAsync(_jsonFile, ScriptData.BashScript.Replace("\r", "\n"));
                    Debug.WriteLine($"saving JSON file {_jsonFile.Name}");
                }

                if (_bashFile != null)
                {

                    Debug.WriteLine($"saving bash file {_bashFile.Name}");
                    await FileIO.WriteTextAsync(_bashFile, ScriptData.BashScript.Replace("\r", "\n"));
                }

            }
            catch (Exception e)
            {
                ScriptData.ParseErrors.Add("Error saving file");
                ScriptData.ParseErrors.Add("Exception saving file: " + e.Message);
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ScriptData.ValidateParameters() == false)
            {
                ScriptData.BashScript = ScriptData.ValidationErrors;
            }

        }







        private void LongName_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_LostFocus(sender, e);


        }

        private async void OnGetDebugConfig(object sender, RoutedEventArgs e)
        {


            DebugConfig dbgWindow = new DebugConfig()
            {
                ConfigModel = ScriptData
            };

            await dbgWindow.ShowAsync(ContentDialogPlacement.Popup);
        }




        /// <summary>
        ///     Refresh menu item.
        ///     check to see which PivotItem is active.  if it is the JSON one, then parse the JSON and create the bash script.
        ///     otherwise parse the bash script
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;

                if (JSONShown)
                {
                    this.ScriptData = ScriptData.FromJson(this.ScriptData.JSON, this.ScriptData.UserCode);
                    return;
                }
                else
                {

                    string bash = ScriptData.BashScript;
                    this.ScriptData = ScriptData.FromBash(bash);
                }
            }
            finally
            {
                ((Button)sender).IsEnabled = true;
            }
        }

        /// <summary>
        ///     returns True if the JSON TextBox is the current Pivot Item, otherwise returns False
        /// </summary>
        private bool JSONShown
        {
            get
            {
                if (!(_Pivot.SelectedItem is PivotItem itm))
                {
                    throw new Exception("There is no way that this could not be a PivotItem!");
                }

                if (itm.Name == "PI_JSON")
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        ///     Called when the user clicks on the toolbar menu.  We simply toggle the setting which will add/remove
        ///     the parameter and update the script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnToggleAddLoggingSupport(object sender, RoutedEventArgs e)
        {
            try
            {
                ScriptData.SetBuiltInParameter(ScriptData.BashWizardParameter.LoggingSupport, !ScriptData.LoggingSupport);
            }
            finally
            {
                splitView.IsPaneOpen = false;
            }
        }

        private void OnToggleAcceptsInputFile(object sender, RoutedEventArgs e)
        {
            try
            {
                ScriptData.SetBuiltInParameter(ScriptData.BashWizardParameter.InputFile, !ScriptData.AcceptsInputFile);
            }
            finally
            {
                splitView.IsPaneOpen = false;
            }
        }

        private void OnToggleCreateVerifyDeletePattern(object sender, RoutedEventArgs e)
        {
            try
            {
                ScriptData.SetBuiltInParameter(ScriptData.BashWizardParameter.CreateVerifyDelete, !ScriptData.CreateVerifyDelete);
            }
            finally
            {
                splitView.IsPaneOpen = false;
            }
        }

        private void OnCopyBashScript(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetText(ScriptData.BashScript);
            Clipboard.SetContent(dataPackage);
        }

        private async void OnShowInputJson(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Bash Wizard: JSON Input"

            };
            var panel = new StackPanel();
            var tb = new TextBox()
            {
                AcceptsReturn = true,
                IsReadOnly = false,
                FontFamily = new Windows.UI.Xaml.Media.FontFamily("Courier New"),
                FontSize = 12,
                Text = this.ScriptData.GetInputJson()
            };
            panel.Children.Add(tb);
            dialog.Content = panel;
            dialog.PrimaryButtonText = "Copy";
            dialog.IsPrimaryButtonEnabled = true;
            dialog.PrimaryButtonClick += delegate
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(tb.Text);
                Clipboard.SetContent(dataPackage);
            };
            dialog.SecondaryButtonText = "Close";
            dialog.SecondaryButtonClick += delegate
            {
                dialog.Hide();
            };

            var result = await dialog.ShowAsync();
        }

        private void OnShowWarnings(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleButton btn = sender as ToggleButton;
                if (txt_Bash.Visibility == Visibility.Visible)
                {
                    txt_Bash.Visibility = Visibility.Collapsed;
                    txt_Warnings.Visibility = Visibility.Visible;
                    btn.IsChecked = true;

                }
                else
                {
                    txt_Bash.Visibility = Visibility.Visible;
                    txt_Warnings.Visibility = Visibility.Collapsed;
                    btn.IsChecked = false;
                }

            }
            finally
            {
                splitView.IsPaneOpen = false;
            }
        }
    }
}
