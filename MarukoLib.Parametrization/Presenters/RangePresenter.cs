using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Windows;

namespace MarukoLib.Parametrization.Presenters
{

    public class RangePresenter : IPresenter
    {

        private class Accessor : IParameterViewAccessor
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly Slider _slider;

            [NotNull] private readonly Func<double, string> _formatter;

            private double _selectionStartValue;

            public Accessor([NotNull] IParameterDescriptor parameter, [NotNull] Slider slider, [NotNull] Func<double, string> formatter)
            {
                _parameter = parameter;
                _slider = slider;
                _formatter = formatter;

                slider.GotMouseCapture += Slider_OnGotMouseCapture;
                slider.LostMouseCapture += Slider_OnLostMouseCapture;
                slider.MouseRightButtonDown += Slider_OnMouseRightButtonDown;
                _slider.ValueChanged += Slider_OnValueChanged;
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(new Range(_slider.SelectionStart, _slider.SelectionEnd)); 
                set
                {
                    if (value is Range interval)
                    {
                        _slider.SelectionStart = interval.MinValue;
                        _slider.SelectionEnd = interval.MaxValue;
                        _slider.Value = interval.MaxValue;
                        UpdateToolTip();
                    }
                }
            }

            private void UpdateToolTip() => _slider.ToolTip = $"{_formatter(_slider.SelectionStart)} ~ {_formatter(_slider.SelectionEnd)}";

            private void Slider_OnGotMouseCapture(object sender, MouseEventArgs e) => _selectionStartValue = _slider.Value;

            private void Slider_OnLostMouseCapture(object sender, MouseEventArgs e)
            {
                UpdateToolTip();
                ValueChanged?.Invoke(this, e);
            }

            private void Slider_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
            {
                _slider.SelectionStart = _slider.SelectionEnd = _slider.Value;
                UpdateToolTip();
            }

            private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (!_slider.IsMouseCaptureWithin) return;
                var v1 = _selectionStartValue;
                var v2 = _slider.Value;
                _slider.SelectionStart = Math.Min(v1, v2);
                _slider.SelectionEnd = Math.Max(v1, v2);
            }

        }

        /// <summary>
        /// Default Value: 0
        /// </summary>
        public static readonly NamedProperty<double> MinimumValueProperty = SliderNumberPresenter.MinimumValueProperty;

        /// <summary>
        /// Default Value: 100
        /// </summary>
        public static readonly NamedProperty<double> MaximumValueProperty = SliderNumberPresenter.MaximumValueProperty;

        /// <summary>
        /// Default Value: 1
        /// </summary>
        public static readonly NamedProperty<double> TickFrequencyProperty = SliderNumberPresenter.TickFrequencyProperty;

        /// <summary>
        /// Default Value: $"{num:G}"
        /// </summary>
        public static readonly NamedProperty<Func<double, string>> NumberFormatterProperty = SliderNumberPresenter.NumberFormatterProperty;

        /// <summary>
        /// Default Value: None
        /// </summary>
        public static readonly NamedProperty<TickPlacement> TickPlacementProperty = SliderNumberPresenter.TickPlacementProperty;

        public static readonly RangePresenter Instance = new RangePresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var numberFormatter = NumberFormatterProperty.Get(param.Metadata);
            var valueFormatter = string.IsNullOrWhiteSpace(param.Unit)? numberFormatter : (val => $"{numberFormatter(val)} {param.Unit}");
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.Star1GridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            var slider = new Slider
            {
                Minimum = MinimumValueProperty.Get(param.Metadata, 0),
                Maximum = MaximumValueProperty.Get(param.Metadata, 100),
                TickFrequency = TickFrequencyProperty.Get(param.Metadata),
                TickPlacement = TickPlacementProperty.Get(param.Metadata),
                AutoToolTipPlacement = AutoToolTipPlacement.BottomRight,
                IsSelectionRangeEnabled = true,
                IsSnapToTickEnabled = true
            };
            var minimumTextBlock = new TextBlock {Text = $"{numberFormatter(slider.Minimum)}"};
            var maximumTextBlock = new TextBlock {Text = $"{numberFormatter(slider.Maximum)}"};
            Grid.SetColumn(minimumTextBlock, 0);
            Grid.SetColumn(slider, 2);
            Grid.SetColumn(maximumTextBlock, 4);
            grid.Children.Add(minimumTextBlock);
            grid.Children.Add(slider);
            grid.Children.Add(maximumTextBlock);

            return new ParameterViewModel(param, grid, new Accessor(param, slider, valueFormatter), slider);
        }

    }

}