using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Windows;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace MarukoLib.Parametrization.Presenters
{

    public class PositionPresenter : IPresenter
    {

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly IList _enumValues;

            [NotNull] private readonly Rectangle[] _checkBoxes;

            private bool _isEnabled = true, _isValid = true;

            private int _selectedIndex = -1;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] Rectangle[] checkBoxes, [NotNull] IList enumValues)
            {
                _parameter = parameter;
                _checkBoxes = checkBoxes;
                _enumValues = enumValues;

                foreach (var checkBox in _checkBoxes)
                    checkBox.MouseLeftButtonUp += CheckBox_OnMouseLeftButtonUp;
            }

            private static Brush GetCheckBoxFillColor(bool selected, bool enabled, bool valid)
            {
                if (enabled)
                {
                    if (valid) return selected ? Brushes.Gray : Brushes.White;
                    return selected ? Brushes.Red : Brushes.White;
                }
                if (valid) return selected ? Brushes.DimGray : Brushes.DarkGray;
                return selected ? Brushes.DarkRed : Brushes.DarkGray;
            }

            public bool Select(int index)
            {
                if (index < 0) index = -1;
                if (_selectedIndex == index) return false;
                for (var i = 0; i < _checkBoxes.Length; i++)
                    _checkBoxes[i].Fill = GetCheckBoxFillColor(i == index, _isEnabled, _isValid);
                _selectedIndex = index;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    if (_isEnabled == value) return;
                    _isEnabled = value;
                    for (var i = 0; i < _checkBoxes.Length; i++)
                    {
                        var checkbox = _checkBoxes[i];
                        checkbox.IsEnabled = value;
                        checkbox.Fill = GetCheckBoxFillColor(i == _selectedIndex, value, _isValid);
                    }
                }
            }

            public bool IsValid
            {
                get => _isValid;
                set
                {
                    if (_isValid == value) return;
                    _isValid = value;
                    for (var i = 0; i < _checkBoxes.Length; i++)
                        _checkBoxes[i].Fill = GetCheckBoxFillColor(i == _selectedIndex, _isEnabled, value); 
                }
            }

            public object Value
            {
                get
                {
                    object value = null;
                    if (_selectedIndex >= 0) value = _enumValues[_selectedIndex];
                    return _parameter.IsValidOrThrow(value);
                }
                set => Select((int) value);
            }

            private void CheckBox_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                if (((Rectangle) sender).Tag is int index)
                    Select(index);
            }

        }

        public static readonly NamedProperty<uint> CheckboxSizeProperty = new NamedProperty<uint>("CheckboxSize", 15);

        public static readonly NamedProperty<bool> Position1DLabelVisibilityProperty = new NamedProperty<bool>("Position1DLabelVisibility", true);

        public static readonly NamedProperty<PositionH1D> Position2DHorizontalAlignmentProperty = new NamedProperty<PositionH1D>("Position2DHorizontalAlignment", PositionH1D.Center);

        public static readonly PositionPresenter Instance = new PositionPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            if (param.ValueType == typeof(Position1D) || param.ValueType == typeof(PositionH1D) || param.ValueType == typeof(PositionV1D)) return Present1D(param);
            if (param.ValueType == typeof(Position2D)) return Present2D(param);
            throw new NotSupportedException($"Unsupported value type: {param.ValueType}");
        }

        public ParameterViewModel Present1D(IParameterDescriptor param)
        {
            var checkboxSize = CheckboxSizeProperty.Get(param.Metadata);
            var labelVisible = Position1DLabelVisibilityProperty.Get(param.Metadata);
            var enumNames = param.ValueType.GetEnumNames();
            var enumValues = param.ValueType.GetEnumValues();
            var checkboxes = new Rectangle[enumNames.Length];

            var grid = new Grid();
            if (labelVisible)
                grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto}); /* Name row */
            grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto}); /* Checkbox row */
            for (var i = 0; i < enumNames.Length; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.Star1GridLength});

                if (labelVisible)
                {
                    var nameTextBlock = new TextBlock { Text = enumNames[i], HorizontalAlignment = HorizontalAlignment.Center, FontSize = 9 };
                    grid.Children.Add(nameTextBlock);
                    Grid.SetRow(nameTextBlock, 0);
                    Grid.SetColumn(nameTextBlock, i);
                }

                var checkbox = checkboxes[i] = new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(2),
                    Width = checkboxSize, Height = checkboxSize,
                    Stroke = Brushes.Black,
                    Fill = Brushes.White,
                    Tag = i
                };
                grid.Children.Add(checkbox);
                if (labelVisible) Grid.SetRow(checkbox, 1);
                Grid.SetColumn(checkbox, i);
            }
            return new ParameterViewModel(param, grid, new Adapter(param, checkboxes, enumValues));
        }

        public ParameterViewModel Present2D(IParameterDescriptor param)
        {
            var checkboxSize = CheckboxSizeProperty.Get(param.Metadata);
            var enumNames = param.ValueType.GetEnumNames();
            var enumValues = param.ValueType.GetEnumValues();
            var checkboxes = new Rectangle[enumNames.Length];

            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 3)
            };

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (Position2DHorizontalAlignmentProperty.Get(param.Metadata))
            {
                case PositionH1D.Left:
                    grid.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case PositionH1D.Right:
                    grid.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                default:
                    grid.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
            }

            for (var i = 0; i < 3; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            }

            for (var i = 0; i < enumValues.Length; i++)
            {
                var rowIndex = i / 3;
                var colIndex = i % 3;
                var checkbox = checkboxes[i] = new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4),
                    Width = checkboxSize, Height = checkboxSize,
                    Stroke = Brushes.Black, Fill = Brushes.White,
                    ToolTip = enumNames[i]
                };
                grid.Children.Add(checkbox);
                Grid.SetRow(checkbox, rowIndex);
                Grid.SetColumn(checkbox, colIndex);
            }
            return new ParameterViewModel(param, grid, new Adapter(param, checkboxes, enumValues));
        }

    }

}