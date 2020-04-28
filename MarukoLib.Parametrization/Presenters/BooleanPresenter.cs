using System;
using System.Windows;
using System.Windows.Controls;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Presenters
{

    public class BooleanPresenter : IPresenter
    {

        private class Accessor : IParameterViewAccessor
        {

            public event EventHandler ValueChanged;

            private readonly IParameterDescriptor _parameter;

            private readonly CheckBox _checkBox;

            public Accessor(IParameterDescriptor parameter, CheckBox checkBox)
            {
                _parameter = parameter;
                _checkBox = checkBox;
                checkBox.Checked += CheckBox_OnValueChanged;
                checkBox.Unchecked += CheckBox_OnValueChanged;
                checkBox.Indeterminate += CheckBox_OnValueChanged;
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(_checkBox.IsChecked);
                set
                {
                    var nullableBool = value as bool?;
                    _checkBox.IsChecked = !_checkBox.IsThreeState && !nullableBool.HasValue ? false : nullableBool;
                }
            }

            private void CheckBox_OnValueChanged(object sender, RoutedEventArgs e) => ValueChanged?.Invoke(this, e);

        }

        public static readonly NamedProperty<string> CheckboxTextProperty = new NamedProperty<string>("CheckBoxText");

        public static readonly NamedProperty<bool> AlignToRightProperty = new NamedProperty<bool>("AlignToRight", false);

        public static readonly BooleanPresenter Instance = new BooleanPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var checkBox = new CheckBox
            {
                HorizontalAlignment = AlignToRightProperty.Get(param.Metadata) ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsThreeState = param.ValueType.IsNullableType()
            };
            UIElement element = checkBox;
            if (CheckboxTextProperty.TryGet(param.Metadata, out var text))
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    FontSize = SystemFonts.SmallCaptionFontSize - 2,
                    Margin = new Thickness(3, 0, 3, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var grid = new Grid
                {
                    HorizontalAlignment = AlignToRightProperty.Get(param.Metadata) ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children = { textBlock, checkBox }
                };
                Grid.SetColumn(AlignToRightProperty.Get(param.Metadata) ? (UIElement) checkBox : textBlock, 1);
                element = grid;
            }
            return new ParameterViewModel(param, element, new Accessor(param, checkBox), checkBox);
        }

    }

}