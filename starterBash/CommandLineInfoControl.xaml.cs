using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace starterBash
{


    public sealed partial class CommandLineInfoControl : UserControl, INotifyPropertyChanged
    {

        public CommandLineInfoControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty ParameterInfoProperty = DependencyProperty.Register("ParameterInfo", typeof(CommandLineInfo), typeof(CommandLineInfoControl), new PropertyMetadata("", ParameterInfoChanged));

        public event PropertyChangedEventHandler PropertyChanged;

        public CommandLineInfo ParameterInfo
        {
            get => (CommandLineInfo)GetValue(ParameterInfoProperty);
            set => SetValue(ParameterInfoProperty, value);
        }
        private static void ParameterInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var depPropClass = d as CommandLineInfoControl;
            var depPropValue = (CommandLineInfo)e.NewValue;
            CommandLineInfo oldVal = null;
            if (e.OldValue.GetType() == typeof(CommandLineInfo))
            {
                oldVal = e.OldValue as CommandLineInfo;
            }
            depPropClass?.SetParameterInfo(depPropValue, oldVal);
        }
        private void SetParameterInfo(CommandLineInfo newValue, CommandLineInfo oldValue)
        {
            if (oldValue != null)
            {
                oldValue.PropertyChanged -= CommandLinePropertyChanged;
            }

            newValue.PropertyChanged += CommandLinePropertyChanged;

        }

        private void CommandLinePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        public override string ToString()
        {
            return ParameterInfo.Serialize();
        }



    }


}
