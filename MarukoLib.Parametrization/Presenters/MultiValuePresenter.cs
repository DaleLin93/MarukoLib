using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Windows;

namespace MarukoLib.Parametrization.Presenters
{

    public class MultiValuePresenter : IPresenter
    {

        private class Adapter : IParameterViewAdapter
        {

            private class ElementViewModel
            {

                [NotNull] internal readonly ParameterViewModel ParameterViewModel;

                [NotNull] internal readonly UIElement RootElement;

                public ElementViewModel([NotNull] ParameterViewModel parameterViewModel, [NotNull] UIElement rootElement)
                {
                    ParameterViewModel = parameterViewModel;
                    RootElement = rootElement;
                }

            }

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly List<ElementViewModel> _elementViewModels = new List<ElementViewModel>();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly IParameterDescriptor _elementParameter;

            [NotNull] private readonly IPresenter _elementPresenter;

            [NotNull] private readonly UIElement _container;

            [NotNull] private readonly StackPanel _itemsContainer;

            [CanBeNull] private readonly PlusButton _plusButton;

            private readonly bool _distinct, _fixed;

            private readonly int _maxElementNum;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] IParameterDescriptor elementParameter,
                [NotNull] UIElement container, [NotNull] StackPanel itemsContainer, [CanBeNull] PlusButton plusButton,
                bool distinct, bool @fixed, int maxElementNum)
            {
                _parameter = parameter;
                _elementParameter = elementParameter;
                _elementPresenter = elementParameter.GetPresenter();
                _container = container;
                _itemsContainer = itemsContainer;
                _plusButton = plusButton;
                _distinct = distinct;
                _fixed = @fixed;
                _maxElementNum = maxElementNum;

                if (plusButton != null)
                    plusButton.Triggered += PlusButton_OnTriggered;
                if (_fixed)
                    for (var i = 0; i < maxElementNum; i++)
                    {
                        var viewModel = _elementPresenter.Present(_elementParameter);
                        viewModel.ValueChanged += ElementViewModel_OnValueChanged;
                        _itemsContainer.Children.Add(viewModel.Element);
                        _elementViewModels.Add(new ElementViewModel(viewModel, viewModel.Element));

                    }
            }

            public bool IsEnabled
            {
                get => _container.IsEnabled;
                set => _container.IsEnabled = value;
            }

            public bool IsValid { get; set; }

            public object Value
            {
                get
                {
                    IList result = Array.CreateInstance(_elementParameter.ValueType, _elementViewModels.Count);

                    /* Get values from element view models and set to result list. */
                    for (var i = 0; i < _elementViewModels.Count && i < _maxElementNum; i++)
                        result[i] = _elementViewModels[i].ParameterViewModel.Value;

                    /* Check value duplication in result list if distinction is required. */
                    if (_distinct) 
                        for (var i = 1; i < result.Count; i++)
                        {
                            var primary = result[i];
                            for (var j = 0; j < i; j++)
                                if (Equals(primary, result[i]))
                                    throw new Exception($"The value is duplicated: {primary}");
                        }
                    return _parameter.IsValidOrThrow(result);
                }
                set
                {
                    if (value is IList list)
                    {
                        if (!_fixed)
                        {
                            while (_elementViewModels.Count < list.Count && _elementViewModels.Count < _maxElementNum) AddRow();
                            while (_elementViewModels.Count > list.Count || _elementViewModels.Count > _maxElementNum) RemoveLastRow();
                        }
                        for (var i = 0; i < list.Count && i < _elementViewModels.Count; i++)
                            _elementViewModels[i].ParameterViewModel.Value = list[i];
                    }
                }
            }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private void OnElementListUpdated()
            {
                UpdatePlusButtonState();
                RaiseValueChangedEvent();
            }

            private void UpdatePlusButtonState()
            {
                if (_plusButton != null)
                    _plusButton.IsEnabled = _elementViewModels.Count < _maxElementNum;
            }

            private void AddRow()
            {
                if (_elementViewModels.Count >= _maxElementNum) return;
                var elementViewModel = _elementPresenter.Present(_elementParameter);
                elementViewModel.ValueChanged += ElementViewModel_OnValueChanged;

                /* Row grid container, with actual parameter view and minus button */
                var grid = new Grid { Margin = new Thickness { Top = 2, Bottom = 2 } };
                var element = new ElementViewModel(elementViewModel, grid);

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.Star1GridLength });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.MinorSpacingGridLength });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                grid.Children.Add(elementViewModel.Element);
                Grid.SetColumn(elementViewModel.Element, 0);

                var minusButton = new MinusButton(15, 15) {Tag = element};
                minusButton.Triggered += MinusButton_OnTriggered;
                grid.Children.Add(minusButton);
                Grid.SetColumn(minusButton, 2);

                _elementViewModels.Add(element);
                _itemsContainer.Children.Add(grid);
                OnElementListUpdated();
            }

            private void RemoveLastRow()
            {
                var index = _elementViewModels.Count - 1;
                var tuple = _elementViewModels[index];
                _elementViewModels.Remove(tuple);
                _itemsContainer.Children.Remove(tuple.RootElement);
                OnElementListUpdated();
            }

            private void ElementViewModel_OnValueChanged(object sender, EventArgs e) => RaiseValueChangedEvent(e);

            private void PlusButton_OnTriggered(object sender, EventArgs e) => AddRow();

            private void MinusButton_OnTriggered(object sender, EventArgs e)
            {
                if (!((sender as FrameworkElement)?.Tag is ElementViewModel elementViewModel)) return;
                _elementViewModels.Remove(elementViewModel);
                _itemsContainer.Children.Remove(elementViewModel.RootElement);
                OnElementListUpdated();
            }

        }

        private class MinusButton : Grid
        {

            public event EventHandler Triggered;

            public MinusButton(int width, int height)
            {
                Rectangle backgroundRect;
                Children.Add(backgroundRect = new Rectangle
                {
                    Fill = Brushes.DarkGray,
                    Width = width,
                    Height = height,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                backgroundRect.RadiusX = backgroundRect.RadiusY = Math.Min(width, height) / 2.0;
                Children.Add(new Rectangle
                {
                    Fill = Brushes.White,
                    Width = Math.Min(width, height) * 2 / 3.0,
                    Height = Math.Min(width, height) * 2 / 9.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsHitTestVisible = false
                });
                backgroundRect.MouseEnter += (s0, e0) => ((Rectangle)s0).Fill = Brushes.IndianRed;
                backgroundRect.MouseLeave += (s0, e0) => ((Rectangle)s0).Fill = Brushes.DarkGray;
                backgroundRect.MouseUp += (s1, e1) => Triggered?.Invoke(this, EventArgs.Empty);
            }

        }

        private class PlusButton : Grid
        {

            public event EventHandler Triggered;

            public PlusButton(int height)
            {
                Rectangle backgroundRect;
                Children.Add(backgroundRect = new Rectangle
                {
                    Fill = Brushes.DimGray,
                    StrokeThickness = 1,
                    Height = height,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                });
                backgroundRect.RadiusX = backgroundRect.RadiusY = height / 2.0;
                Children.Add(new Rectangle
                {
                    Fill = Brushes.White,
                    Width = height * 2 / 3.0,
                    Height = height * 2 / 9.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsHitTestVisible = false
                });
                Children.Add(new Rectangle
                {
                    Fill = Brushes.White,
                    Width = height * 2 / 9.0,
                    Height = height * 2 / 3.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsHitTestVisible = false
                });
                backgroundRect.MouseEnter += (s0, e0) => ((Rectangle)s0).Fill = Brushes.MediumAquamarine;
                backgroundRect.MouseLeave += (s0, e0) => ((Rectangle)s0).Fill = Brushes.DimGray;
                backgroundRect.MouseUp += (s1, e1) => Triggered?.Invoke(this, EventArgs.Empty);
            }

        }

        public static readonly NamedProperty<bool> DistinctProperty = new NamedProperty<bool>("Distinct", false);

        public static readonly NamedProperty<int> FixedElementCountProperty = new NamedProperty<int>("FixedElementCount", -1);

        public static readonly NamedProperty<ushort> MaximumElementCountProperty = new NamedProperty<ushort>("MaximumElementCount", ushort.MaxValue);

        public static readonly NamedProperty<IReadonlyContext> ElementContextProperty = new NamedProperty<IReadonlyContext>("ElementContext", EmptyContext.Instance);

        public static readonly MultiValuePresenter Instance = new MultiValuePresenter();

        private static Type GetElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (typeof(IList<>).IsAssignableFrom(type)) return type.GetGenericType(typeof(IList<>)) ?? throw new ArgumentException("Generic type not defined");
            throw new ArgumentException("Array or IList<> type required");
        }

        [SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var distinct = DistinctProperty.Get(param.Metadata);
            var fixedCount = FixedElementCountProperty.Get(param.Metadata);
            var @fixed = fixedCount >= 0;
            var maxElementCount = @fixed ? fixedCount : MaximumElementCountProperty.Get(param.Metadata);
            var elementType = GetElementType(param.ValueType);
            if (elementType.IsPrimitive) return PlainTextPresenter.Instance.Present(param);

            var elementParameter = new MetadataOverridenParameter(param, elementType, ElementContextProperty.Get(param.Metadata));

            /* Outer grid container, with rounded rect background */
            var container = new Grid {Margin = new Thickness(0, 3, 0, 3)};
            container.Children.Add(new Rectangle
            {
                RadiusX = 6, RadiusY = 6,
                Stroke = Brushes.SlateGray,
                StrokeDashArray = new DoubleCollection(new []{3.0, 3.0}),
                StrokeThickness = 1
            });

            /* Middle stack container, with element stack panel and plus button */
            var stackPanel = new StackPanel{Margin = new Thickness(7)};
            container.Children.Add(stackPanel);

            /* Inner stack container, with elements */
            StackPanel itemsContainer;
            if (!@fixed) stackPanel.Children.Add(itemsContainer = new StackPanel());
            else itemsContainer = stackPanel;

            PlusButton plusButton = null;
            if (!@fixed)
            {
                plusButton = new PlusButton(15) { Margin = new Thickness(0, ViewConsts.MinorSpacing, 0, 0) };
                stackPanel.Children.Add(plusButton);
            }

            return new ParameterViewModel(param, container, new Adapter(param, elementParameter, container, itemsContainer, plusButton, distinct, @fixed, maxElementCount));
        }

    }

    public static class MultiValuePresenterExt
    {

        public static T UseMultiValuePresenter<T>([NotNull] this T contextBuilder, bool? distinct = null, 
            ushort? maxCount = null, IReadonlyContext elementContext = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(MultiValuePresenter.Instance);
            contextBuilder.SetPropertyNotNull(MultiValuePresenter.DistinctProperty, distinct);
            contextBuilder.SetPropertyNotNull(MultiValuePresenter.MaximumElementCountProperty, maxCount);
            contextBuilder.SetPropertyNotNull(MultiValuePresenter.ElementContextProperty, elementContext);
            return contextBuilder;
        }

        public static T UseFixedMultiValuePresenter<T>([NotNull] this T contextBuilder, int fixedCount, 
            bool? distinct = null, IReadonlyContext elementContext = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(MultiValuePresenter.Instance);
            contextBuilder.Set(MultiValuePresenter.FixedElementCountProperty, fixedCount);
            contextBuilder.SetPropertyNotNull(MultiValuePresenter.DistinctProperty, distinct);
            contextBuilder.SetPropertyNotNull(MultiValuePresenter.ElementContextProperty, elementContext);
            return contextBuilder;
        }

    }

}