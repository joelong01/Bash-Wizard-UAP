using bashGeneratorSharedModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BashWizard
{
    public sealed partial class DebugConfig : ContentDialog
    {
        private ConfigModel _ConfigModel = null;
        public ConfigModel ConfigModel
        {
            get => _ConfigModel;
            set
            {
                if (value == null)
                {
                    this.Config = "";
                    _ConfigModel = null;

                }
                else if (_ConfigModel != value)
                {
                    _ConfigModel = value;
                    this.Config = _ConfigModel.VSCodeDebugInfo(this.FileLocation);
                }
            }
        }

        public DebugConfig()
        {
            this.InitializeComponent();
        }
        public static readonly DependencyProperty ConfigProperty = DependencyProperty.Register("Config", typeof(string), typeof(DebugConfig), new PropertyMetadata(""));
        public static readonly DependencyProperty FileLocationProperty = DependencyProperty.Register("FileLocation", typeof(string), typeof(DebugConfig), new PropertyMetadata("./BashScripts/", FileLocationChanged));
        public string FileLocation
        {
            get => (string)GetValue(FileLocationProperty);
            set => SetValue(FileLocationProperty, value);
        }
        private static void FileLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as DebugConfig;
            var depPropValue = (string)e.NewValue;
            depPropClass?.SetFileLocation(depPropValue);
        }
        private void SetFileLocation(string value)
        {
            if (Config != null)
            {
                this.Config = _ConfigModel.VSCodeDebugInfo(this.FileLocation);
            }
        }

        public string Config
        {
            get => (string)GetValue(ConfigProperty);
            set => SetValue(ConfigProperty, value);
        }



        private void OnClose(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void OnCopy(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(this.Config);
            Clipboard.SetContent(dataPackage);
            args.Cancel = true;
        }
    }
}
