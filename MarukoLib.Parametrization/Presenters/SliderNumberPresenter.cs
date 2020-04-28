using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.Parametrization.Windows;

namespace MarukoLib.Parametrization.Presenters
{

    /// <summary>
    /// Notice: MinimumValueProperty & MaximumValueProperty must be set to use this presenter.
    /// </summary>
    public class SliderNumberPresenter : IPresenter
    {

        private class Accessor : IParameterViewAccessor
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly Slider _slider;

            public Accessor([NotNull] IParameterDescriptor parameter, [NotNull] Slider slider)
            {
                _parameter = parameter;
                _slider = slider;

                _slider.ValueChanged += Slider_OnValueChanged;
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(Convert.ChangeType(_slider.Value, _parameter.ValueType));
                set => _slider.Value = Convert.ToDouble(value);
            }

            private void Slider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ValueChanged?.Invoke(this, e);

        }

        /// <summary>
        /// Required
        /// </summary>
        public static readonly NamedProperty<double> MinimumValueProperty = new NamedProperty<double>("MaximumValue");

        /// <summary>
        /// Required
        /// </summary>
        public static readonly NamedProperty<double> MaximumValueProperty = new NamedProperty<double>("MaximumValue");

        /// <summary>
        /// Default Value: 1
        /// </summary>
        public static readonly NamedProperty<double> TickFrequencyProperty = new NamedProperty<double>("TickFrequency", 1);

        /// <summary>
        /// Default Value: None
        /// </summary>
        public static readonly NamedProperty<TickPlacement> TickPlacementProperty = new NamedProperty<TickPlacement>("TickPlacement", TickPlacement.None);

        /// <summary>
        /// Default Value: $"{num:G}"
        /// </summary>
        public static readonly NamedProperty<Func<double, string>> NumberFormatterProperty = new NamedProperty<Func<double, string>>("NumberFormatter", num => $"{num:G}");

        public static readonly SliderNumberPresenter Instance = new SliderNumberPresenter();
        
        public ParameterViewModel Present(IParameterDescriptor param)
        {
            if (!param.Metadata.Contains(MinimumValueProperty) || !param.Metadata.Contains(MaximumValueProperty))
                throw new ProgrammingException($"Missing 'MinimumValue' or 'MaximumValue' for parameter '{param.Name}'");
            var numberFormatter = NumberFormatterProperty.Get(param.Metadata);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.Star1GridLength });
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            var slider = new Slider
            {
                Minimum = MinimumValueProperty.Get(param.Metadata),
                Maximum = MaximumValueProperty.Get(param.Metadata),
                TickFrequency = TickFrequencyProperty.Get(param.Metadata),
                TickPlacement = TickPlacementProperty.Get(param.Metadata),
                AutoToolTipPlacement = AutoToolTipPlacement.BottomRight,
                IsSnapToTickEnabled = true
            };
            var minimumTextBlock = new TextBlock { Text = $"{numberFormatter(slider.Minimum)}" };
            var maximumTextBlock = new TextBlock { Text = $"{numberFormatter(slider.Maximum)}" };
            Grid.SetColumn(minimumTextBlock, 0);
            Grid.SetColumn(slider, 2);
            Grid.SetColumn(maximumTextBlock, 4);
            grid.Children.Add(minimumTextBlock);
            grid.Children.Add(slider);
            grid.Children.Add(maximumTextBlock);

            return new ParameterViewModel(param, grid, new Accessor(param, slider), slider);
        }

    }

    public static class SliderNumberPresenterExt
    {

        public static T UseSliderNumberPresenter<T>([NotNull] this T contextBuilder, double minimum, double maximum, 
            double? interval = null, TickPlacement? placement = null, Func<double, string> formatter = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(SliderNumberPresenter.Instance);
            contextBuilder.Set(SliderNumberPresenter.MinimumValueProperty, minimum);
            contextBuilder.Set(SliderNumberPresenter.MaximumValueProperty, maximum);
            contextBuilder.SetPropertyNotNull(SliderNumberPresenter.TickFrequencyProperty, interval);
            contextBuilder.SetPropertyNotNull(SliderNumberPresenter.TickPlacementProperty, placement);
            contextBuilder.SetPropertyNotNull(SliderNumberPresenter.NumberFormatterProperty, formatter);
            return contextBuilder;
        }

    }

}