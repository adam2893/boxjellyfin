using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace JellyfinXbox.Views
{
    public class InfoRow : Control
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(InfoRow), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public InfoRow()
        {
            DefaultStyleKey = typeof(InfoRow);
        }
    }
}
