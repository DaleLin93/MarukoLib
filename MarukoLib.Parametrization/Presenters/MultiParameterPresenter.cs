using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Windows;

namespace MarukoLib.Parametrization.Presenters
{

    public abstract class MultiParameterPresenter : IPresenter
    {

        protected interface IAdapter
        {

            event EventHandler ValueChanged;

            object Value { get; set; }

            void SetEnabled(bool enabled);

            void SetValid(bool valid);

        }

        private class PopupAdapter : ControlStateHandler, IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly MultiParameterPresenter _presenter;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [CanBeNull] private object _value;

            public PopupAdapter([NotNull] MultiParameterPresenter presenter, [NotNull] IParameterDescriptor parameter, 
                [NotNull] ButtonBase button, [CanBeNull] object value) : base(button)
            {
                _presenter = presenter;
                _parameter = parameter;
                _value = value;

                button.Click += Button_OnClick;
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(_value);
                set
                {
                    _value = value;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public void Popup()
            {
                var subParams = _presenter.GetSubParameters(_parameter);
                var configWindow = new ParameterizedConfigWindow(_parameter.Name ?? "Parameter", subParams, EmptyContext.Instance) {Width = 400};
                var adapter = _presenter.GetAdapter(_parameter, configWindow.ConfigurationPanel.ParameterViewModels);
                adapter.Value = _parameter.IsValid(_value) ? _value : _parameter.DefaultValue;
                if (!configWindow.ShowDialog(out _)) return;
                Value = adapter.Value;
            }

            private void Button_OnClick(object sender, RoutedEventArgs e) => Popup();

        }

        private class InlineAdapter : ControlStateHandler, IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IAdapter _adapter;

            public InlineAdapter([NotNull] IAdapter adapter, [NotNull] FrameworkElement container) : base(container)
            {
                _adapter = adapter;
                _adapter.ValueChanged += Adapter_OnValueChanged;
            }

            public override bool IsEnabled
            {
                get => base.IsEnabled;
                set
                {
                    base.IsEnabled = value;
                    _adapter.SetEnabled(value);
                }
            }

            public override bool IsValid
            {
                get => base.IsValid;
                set
                {
                    base.IsValid = value;
                    _adapter.SetValid(value);
                }
            }

            public object Value
            {
                get => _adapter.Value;
                set => _adapter.Value = value;
            }

            private void Adapter_OnValueChanged(object sender, EventArgs e)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e);
            }

        }

        /// <summary>
        /// Config in a popup window.
        /// Default Value: false
        /// </summary>
        public static readonly ContextProperty<bool> PopupProperty = new NamedProperty<bool>("Popup", false);

        /// <summary>
        /// Default Value: Horizontal
        /// </summary>
        public static readonly NamedProperty<Orientation> LayoutOrientationProperty = new NamedProperty<Orientation>("LayoutOrientation", Orientation.Horizontal);

        /// <summary>
        /// Default Value: true
        /// </summary>
        public static readonly NamedProperty<bool> ParamLabelVisibilityProperty = new NamedProperty<bool>("ParamLabelVisibility", true);

        /// <summary>
        /// Property for sub-parameters.
        /// Default Value: 1*
        /// </summary>
        public static readonly NamedProperty<GridLength> ColumnWidthProperty = new NamedProperty<GridLength>("ColumnWidth", ViewConsts.Star1GridLength);

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var popup = PopupProperty.Get(param.Metadata);
            return popup ? PresentPopup(param) : PresentInline(param);
        }

        protected abstract IParameterDescriptor[] GetSubParameters(IParameterDescriptor parameter);

        protected abstract IAdapter GetAdapter(IParameterDescriptor parameter, ParameterViewModel[] subParamViewModels);

        internal ParameterViewModel PresentPopup(IParameterDescriptor param)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.Star1GridLength, MinWidth = 110, MaxWidth = 130 });

            var button = new Button { Content = "Configure →" };
            grid.Children.Add(button);
            Grid.SetColumn(button, 1);

            return new ParameterViewModel(param, grid, new PopupAdapter(this, param, button, null));
        }

        internal ParameterViewModel PresentInline(IParameterDescriptor param)
        {
            var subParameters = GetSubParameters(param);
            var subParameterCount = subParameters.Length;
            var subParamViewModels = new ParameterViewModel[subParameterCount];

            Panel container;
            if (subParameterCount == 0)
            {
                var grid = new Grid();
                grid.Children.Add(new TextBlock { Text = "<EMPTY>", Foreground = Brushes.DimGray });
                container = grid;
            }
            else
            {
                var labelVisible = ParamLabelVisibilityProperty.Get(param.Metadata);
                var nameTextBlocks = labelVisible ? new TextBlock[subParameterCount] : null;
                for (var i = 0; i < subParameterCount; i++)
                {
                    var subParam = subParameters[i];
                    subParamViewModels[i] = subParam.Present();
                    if (!labelVisible) continue;
                    var nameTextBlock = ViewHelper.CreateParamNameTextBlock(subParam);
                    nameTextBlock.FontSize = 8;
                    nameTextBlock.TextWrapping = TextWrapping.NoWrap;
                    nameTextBlock.TextAlignment = TextAlignment.Left;
                    nameTextBlocks[i] = nameTextBlock;
                }

                var orientation = LayoutOrientationProperty.Get(param.Metadata);
                switch (orientation)
                {
                    case Orientation.Horizontal:
                    {
                        var grid = new Grid();
                        if (labelVisible) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        grid.ColumnDefinitions.Add(new ColumnDefinition()); // first content column (at least one element is ensured).
                        for (var i = 1; i < subParameterCount; i++)
                        {
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.MinorSpacingGridLength });
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                        }
                        for (var i = 0; i < subParameterCount; i++)
                        {
                            var columnIndex = i * 2;
                            var subParam = subParameters[i];
                            var subParamViewModel = subParamViewModels[i];
                            if (labelVisible)
                            {
                                var nameTextBlock = nameTextBlocks[i];
                                grid.Children.Add(nameTextBlock);
                                Grid.SetRow(nameTextBlock, 0);
                                Grid.SetColumn(nameTextBlock, columnIndex);
                            }

                            grid.Children.Add(subParamViewModel.Element);
                            if (labelVisible) Grid.SetRow(subParamViewModel.Element, 1);
                            Grid.SetColumn(subParamViewModel.Element, columnIndex);

                            GridLength columnWidth;
                            if (ColumnWidthProperty.TryGet(subParam.Metadata, out var propertyValue))
                                columnWidth = propertyValue;
                            else if (subParamViewModel.Element is FrameworkElement fe && fe.HorizontalAlignment != HorizontalAlignment.Stretch)
                                columnWidth = GridLength.Auto;
                            else
                                columnWidth = ColumnWidthProperty.DefaultValue;
                            grid.ColumnDefinitions[columnIndex].Width = columnWidth;
                        }
                        container = grid;
                        break;
                    }
                    case Orientation.Vertical:
                    {
                        var stackPanel = new StackPanel();
                        for (var i = 0; i < subParameterCount; i++)
                        {
                            var subParamViewModel = subParamViewModels[i];
                            if (labelVisible)
                            {
                                var nameTextBlock = nameTextBlocks[i];
                                stackPanel.Children.Add(nameTextBlock);
                            }
                            stackPanel.Children.Add(subParamViewModel.Element);
                        }
                        container = stackPanel;
                        break;
                    }
                    default:
                        throw new NotSupportedException(orientation.ToString());
                }
            }
            return new ParameterViewModel(param, container, new InlineAdapter(GetAdapter(param, subParamViewModels), container));
        }

    }

}