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
        private StorageFile _fileBashWizard = null;

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
            try
            {


                FileOpenPicker picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List,

                };

                picker.FileTypeFilter.Add(".sh");
                _fileBashWizard = await picker.PickSingleFileAsync();
                if (_fileBashWizard != null)
                {
                    ScriptData = new ScriptData();
                    IBuffer buffer = await FileIO.ReadBufferAsync(_fileBashWizard);
                    DataReader reader = DataReader.FromBuffer(buffer);
                    byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(fileContent);
                    string s = Encoding.UTF8.GetString(fileContent, 0, fileContent.Length);
                    ApplicationView appView = ApplicationView.GetForCurrentView();
                    appView.Title = $"{_fileBashWizard.Name}";
                    ScriptData.ScriptName = _fileBashWizard.Name;
                    this.ScriptData = ScriptData.FromBash(s);
                }

            }
            catch (Exception ex)
            {
                ScriptData.BashScript = "Error: " + ex.Message;
                _fileBashWizard = null;
            }
            finally
            {

                if (_fileBashWizard != null)
                {
                    ScriptData.ToBash();
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
            if (ScriptData.ScriptName == "")
            {
                ScriptData.BashScript = "You must specify a script name";
                return;
            }
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation =
                PickerLocationId.DocumentsLibrary
            };
            savePicker.FileTypeChoices.Add("Shell Scripts", new List<string>() { ".sh" });
            savePicker.SuggestedFileName = $"{ScriptData.ScriptName}";
            _fileBashWizard = await savePicker.PickSaveFileAsync();
            if (_fileBashWizard != null)
            {
                if (ScriptData.ScriptName == null)
                {
                    ScriptData.ScriptName = _fileBashWizard.Name;
                }
            }
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
                    if (ScriptData.ScriptName == "")
                    {

                    }
                    await FileIO.WriteTextAsync(_fileBashWizard, ScriptData.BashScript.Replace("\r", ""));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception saving file: " + e.Message);
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




        private void OnParseBashScript(object sender, RoutedEventArgs e)
        {



            string bash = ScriptData.BashScript;
            Reset();
            this.ScriptData = ScriptData.FromBash(bash);
            //if (ScriptData.FromBash(bash) == false)
            //{
            //    ScriptData.BashScript = ScriptData.ParseErrors;
            //}
            //else
            //{
            //    ScriptData.ToBash();
            //}
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
            dialog.PrimaryButtonClick += delegate {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(tb.Text);
                Clipboard.SetContent(dataPackage);
            };
            dialog.SecondaryButtonText = "Close";
            dialog.SecondaryButtonClick += delegate {
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
