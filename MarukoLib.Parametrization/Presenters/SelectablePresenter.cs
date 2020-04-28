using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.Parametrization.Windows;
using MarukoLib.UI;

namespace MarukoLib.Parametrization.Presenters
{

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    public class SelectablePresenter : IPresenter
    {

        private class ComboBoxAdapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [CanBeNull] private readonly ITypeConverter _textConverter;

            [NotNull] private readonly ComboBox _comboBox;

            [CanBeNull] private readonly Button _refreshButton;

            [CanBeNull] private TextBox _textBox;

            [CanBeNull] private Border _textBoxBorder;

            private bool _isValid = true;

            public ComboBoxAdapter([NotNull] IParameterDescriptor parameter, [CanBeNull] ITypeConverter textConverter,
                [NotNull] ComboBox comboBox, [CanBeNull] Button refreshButton)
            {
                _parameter = parameter;
                _textConverter = textConverter;

                _comboBox = comboBox;
                _refreshButton = refreshButton;

                _comboBox.SelectionChanged += ComboBox_OnSelectionChanged;
                if (comboBox.IsEditable)
                {
                    if (textConverter == null) throw new ArgumentNullException(nameof(textConverter));
                    comboBox.Loaded += ComboBox_OnLoaded;
                    comboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(ComboBoxTextBox_OnTextChanged));
                }
                if (_refreshButton != null) _refreshButton.Click += RefreshButton_OnClick;
            }

            public bool IsEnabled
            {
                get => _comboBox.IsEnabled;
                set => _comboBox.IsEnabled = value;
            }

            public bool IsValid
            {
                get => _isValid;
                set
                {
                    if (_isValid == value) return;
                    _isValid = value;
                    var brush = value ? Brushes.Transparent : ViewConsts.InvalidColorBrush;
                    if (_comboBox.IsEditable)
                    {
                        if (_textBoxBorder != null)
                            _textBoxBorder.Background = brush;
                        else if (_textBox != null)
                            _textBox.Background = brush;
                    }
                    else
                        _comboBox.Background = brush;
                }
            }

            public object Value
            {
                get
                {
                    object value;
                    if (_comboBox.IsEditable)
                    {
                        var text = _comboBox.Text;
                        value = ReferenceEquals(NullValue, text) ? null : _textConverter?.ConvertForward(_comboBox.Text);
                    }
                    else
                    {
                        value = ((NamedObject)_comboBox.SelectedValue)?.Value;
                        if (ReferenceEquals(NullValue, value)) value = null;
                    }
                    return _parameter.IsValidOrThrow(value);
                }
                set
                {
                    using (_updateLock.Ref())
                    {
                        string text;
                        if (_textConverter != null)
                            text = (string)_textConverter.ConvertBackward(value);
                        else if(value is INamed named)
                            text = named.Name;
                        else
                            text = _parameter.ConvertValueToString(value);
                        text = text ?? NullValue;
                        if (_comboBox.IsEditable)
                            _comboBox.Text = text;
                        else
                            _comboBox.FindAndSelectFirstByString(text);
                    } 
                    RaiseValueChangedEvent();
                }
            }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private void ComboBox_OnLoaded(object sender, RoutedEventArgs args)
            {
                var comboBox = (ComboBox)sender;
                comboBox.Loaded -= ComboBox_OnLoaded;
                if ((_textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox) == null) return;
                _textBox.Background = Brushes.Transparent;
                if ((_textBoxBorder = _textBox.Parent as Border) == null) return;
                _textBoxBorder.Background = Brushes.Transparent;
            }

            private void ComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => RaiseValueChangedEvent(e);

            private void ComboBoxTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => RaiseValueChangedEvent(e);

            private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
            {
                using (_updateLock.Ref())
                {
                    var selected = _comboBox.SelectedItem;
                    _comboBox.ItemsSource = GetNamedValues(_parameter, _textConverter);
                    if (selected != null) Value = selected;
                }
                RaiseValueChangedEvent();
            }

        }

        private class RadioGroupAdapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [CanBeNull] private readonly ITypeConverter _textConverter;

            [NotNull] private readonly Rectangle _background;

            [NotNull] private readonly UIElement _container;

            [NotNull] private readonly IList<RadioButton> _radioButtons;

            public RadioGroupAdapter([NotNull] IParameterDescriptor parameter, [CanBeNull] ITypeConverter textConverter, 
                [NotNull] Rectangle background, [NotNull] UIElement container, [NotNull] IList<RadioButton> radioButtons)
            {
                _parameter = parameter;
                _textConverter = textConverter;

                _background = background;
                _container = container;
                _radioButtons = radioButtons;

                foreach (var radioButton in radioButtons)
                    radioButton.Checked += RadioButton_OnChecked;
            }

            public bool IsEnabled
            {
                get => _container.IsEnabled;
                set => _container.IsEnabled = value;
            }

            public bool IsValid
            {
                get => _background.Fill != ViewConsts.InvalidColorBrush;
                set => _background.Fill = value ? Brushes.Transparent : ViewConsts.InvalidColorBrush;
            }

            public object Value
            {
                get
                {
                    var value = _radioButtons.First(rb => rb.IsChecked ?? false).Tag;
                    if (ReferenceEquals(NullValue, value)) value = null;
                    return _parameter.IsValidOrThrow(value);
                }
                set
                {
                    using (_updateLock.Ref())
                    {
                        string text;
                        if (_textConverter != null)
                            text = (string) _textConverter.ConvertBackward(value);
                        else if(value is INamed named)
                            text = named.Name;
                        else 
                            text = _parameter.ConvertValueToString(value);
                        text = text ?? NullValue;
                        var @checked = false;
                        foreach (var radioButton in _radioButtons)
                        {
                            var equal = Equals(radioButton.Content, text);
                            if (equal) @checked = true;
                            radioButton.IsChecked = equal;
                        }
                        if (!@checked && _radioButtons.Any()) _radioButtons[0].IsChecked = true;
                    }
                    RaiseValueChangedEvent();
                }
            }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private void RadioButton_OnChecked(object sender, RoutedEventArgs e) => RaiseValueChangedEvent(e);

        }

        private const string NullValue = "<NULL>";

        public static readonly NamedProperty<Func<IParameterDescriptor, IEnumerable>> SelectableValuesFuncProperty =
            new NamedProperty<Func<IParameterDescriptor, IEnumerable>>("SelectableValuesFunc");

        /// <summary>
        /// Add refresh button refresh selectable values at any time.
        /// Only supported for presentation style of combo box.
        /// Default value: <code>false</code>
        /// </summary>
        public static readonly NamedProperty<bool> RefreshableProperty = new NamedProperty<bool>("Refreshable", false);

        /// <summary>
        /// Make combo box editable to customize value.
        /// Only supported for presentation style of combo box and for string as value type (or string convertible by using text converter).
        /// Default value: <code>false</code>
        /// </summary>
        public static readonly NamedProperty<bool> CustomizableProperty = new NamedProperty<bool>("Customizable", false);

        /// <summary>
        /// Text converter to convert customized text to expected value type.
        /// </summary>
        public static readonly NamedProperty<ITypeConverter> TextConverterProperty = PlainTextPresenter.TextConverterProperty;

        /// <summary>
        /// Use radio button group to select value instead of a combo box.
        /// Default value: <code>false</code>
        /// </summary>
        public static readonly NamedProperty<bool> UseRadioGroupProperty = new NamedProperty<bool>("UseRadioGroup", false);

        public static readonly NamedProperty<string> DisplayTextOfNullValueProperty = 
            new NamedProperty<string>("DisplayTextOfNullValue", NullValue);

        public static readonly NamedProperty<Orientation> RadioGroupOrientationProperty =
            new NamedProperty<Orientation>("RadioGroupOrientation", Orientation.Horizontal);

        public static readonly SelectablePresenter Instance = new SelectablePresenter();

        private static IEnumerable<NamedObject> GetNamedValues([NotNull] IParameterDescriptor param, [CanBeNull] ITypeConverter textConverter)
        {
            IEnumerable items;
            if (SelectableValuesFuncProperty.TryGet(param.Metadata, out var selectableValuesFunc))
                items = selectableValuesFunc(param);
            else if (param.IsSelectable())
                items = param.SelectableValues;
            else if (param.ValueType.IsEnum)
                items = Enum.GetValues(param.ValueType);
            else
                throw new ProgrammingException("Parameter.SelectableValues or SelectablePresenter.SelectableValuesFuncProperty must be assigned");
            var list = new LinkedList<NamedObject>();
            if (items != null)
            {
                if (items is IDictionary dictionary)
                    foreach (var key in dictionary.Keys) // Assumes that keys of the dictionary are names.
                        list.AddLast(new NamedObject(key.ConvertToString(), dictionary[key]));
                else
                    foreach (var item in items)
                        if (item != null)
                            switch (item)
                            {
                                case NamedObject namedObject: // Assumes that the value are store in named object.
                                    list.AddLast(namedObject);
                                    break;
                                case INamed named: // Assumes that the value are store in named object.
                                    list.AddLast(new NamedObject(named.Name, named));
                                    break;
                                default:
                                    var name = textConverter == null 
                                        ? param.ConvertValueToString(item) 
                                        : (string) textConverter.ConvertBackward(item);
                                    list.AddLast(new NamedObject(name, item));
                                    break;
                            }
            }
            if (param.IsNullable) list.AddFirst(new NamedObject(DisplayTextOfNullValueProperty.Get(param.Metadata), NullValue));
            return list;
        }

        public ParameterViewModel Present(IParameterDescriptor param) 
            => UseRadioGroupProperty.Get(param.Metadata) ? PresentRadioButtons(param) : PresentComboBox(param);

        public ParameterViewModel PresentComboBox(IParameterDescriptor param)
        {
            var refreshable = RefreshableProperty.Get(param.Metadata);
            var customizable = CustomizableProperty.Get(param.Metadata);
            var textConverter = TextConverterProperty.Get(param.Metadata, null)?.Validate(typeof(string), param.ValueType);
            if (customizable)
            {
                if (!param.ValueType.IsBasicType() && textConverter == null)
                    throw new ProgrammingException($"Customizable feature is not supported for type '{param.ValueType}' without text converter.");
                if (textConverter == null)
                    textConverter = param.ValueType == typeof(string)
                        ? TypeConverter.Identity(typeof(string), typeof(string))
                        : TypeConverter.SystemConvert(typeof(string), param.ValueType);
            } 

            var comboBox = new ComboBox {ItemsSource = GetNamedValues(param, textConverter), IsEditable = customizable};

            if (!refreshable) return new ParameterViewModel(param, comboBox, new ComboBoxAdapter(param, textConverter, comboBox, null));

            /* Create 3 columns grid for 'ComboBox-Spacing-RefreshButton' */
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.Star1GridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});

            /* Add ComboBox at 1st column */
            grid.Children.Add(comboBox);
            Grid.SetColumn(comboBox, 0);

            /* Add RefreshButton at 3rd column */
            var refreshValuesBtn = new Button
            {
                ToolTip = "Refresh Values",
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = ViewConsts.DefaultRowHeight,
                Content = new Image {Margin = new Thickness(2), Source = new BitmapImage(new Uri(ViewConsts.ResetImageUri, UriKind.Absolute))}
            };
            grid.Children.Add(refreshValuesBtn);
            Grid.SetColumn(refreshValuesBtn, 2);

            return new ParameterViewModel(param, grid, new ComboBoxAdapter(param, textConverter, comboBox, refreshValuesBtn));
        }

        public ParameterViewModel PresentRadioButtons(IParameterDescriptor param)
        {
            if (RefreshableProperty.Get(param.Metadata)) throw new ProgrammingException("Refreshable feature not supported for radio button group style.");
            if (CustomizableProperty.Get(param.Metadata)) throw new ProgrammingException("Customizable feature not supported for radio button group style.");
            var textConverter = TextConverterProperty.Get(param.Metadata, null)?.Validate(typeof(string), param.ValueType);
            var guid = Guid.NewGuid().ToString();
            var radioButtons = (from item in GetNamedValues(param, textConverter)
                select new RadioButton
                {
                    GroupName = guid,
                    Content = item.Name,
                    Margin = new Thickness
                    {
                        Top = ViewConsts.MinorSpacing / 2.0,
                        Bottom = ViewConsts.MinorSpacing / 2.0,
                        Left = ViewConsts.MinorSpacing / 2.0,
                        Right = ViewConsts.MinorSpacing
                    },
                    Tag = item.Value
                }).ToList();
            var grid = new Grid {Margin = new Thickness(0, ViewConsts.MinorSpacing / 2.0, 0, 0)};

            var background = new Rectangle
            {
                RadiusX = 5, RadiusY = 5,
                Stroke = new SolidColorBrush(Color.FromScRgb(1, 0.2F, 0.2F, 0.2F)), 
                StrokeThickness = 1
            };
            grid.Children.Add(background);

            var container = RadioGroupOrientationProperty.Get(param.Metadata) == Orientation.Horizontal ? new WrapPanel() : (Panel)new StackPanel();
            container.Margin = new Thickness(ViewConsts.MinorSpacing);
            foreach (var radioButton in radioButtons)
                container.Children.Add(radioButton);
            grid.Children.Add(container);
            return new ParameterViewModel(param, grid, new RadioGroupAdapter(param, textConverter, background, container, radioButtons));
        }

    }

    public static class SelectablePresenterExt
    {

        public static T UseSelectablePresenter<T>([NotNull] this T contextBuilder,
            Func<IParameterDescriptor, IEnumerable> selectableValueFunc = null, bool? refreshable = null, bool? customizable = null,
            ITypeConverter textConverter = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(SelectablePresenter.Instance);
            contextBuilder.SetPropertyNotNull(SelectablePresenter.SelectableValuesFuncProperty, selectableValueFunc);
            contextBuilder.SetPropertyNotNull(SelectablePresenter.RefreshableProperty, refreshable);
            contextBuilder.SetPropertyNotNull(SelectablePresenter.CustomizableProperty, customizable);
            contextBuilder.SetPropertyNotNull(SelectablePresenter.TextConverterProperty, textConverter);
            return contextBuilder;
        }

    }

}