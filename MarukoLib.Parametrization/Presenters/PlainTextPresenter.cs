using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Presenters
{

    public class PlainTextPresenter : IPresenter
    {

        private class Accessor : IParameterViewAccessor
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [CanBeNull] private readonly Regex _regex;

            [CanBeNull] private readonly ITypeConverter _textConverter;

            [NotNull] private readonly TextBox _textBox;

            public Accessor([NotNull] IParameterDescriptor parameter, [CanBeNull] Regex regex, 
                [CanBeNull] ITypeConverter textConverter, [NotNull] TextBox textBox)
            {
                _parameter = parameter;
                _regex = regex;
                _textConverter = textConverter;

                _textBox = textBox;

                _textBox.TextChanged += TextBox_OnTextChanged;
            }

            public object Value
            {
                get
                {
                    var text = _textBox.Text;
                    if (_regex != null && !_regex.IsMatch(text)) throw new Exception("input text not match the given pattern");
                    return _parameter.IsValidOrThrow(_textConverter == null 
                        ? _parameter.ParseValueFromString(text)
                        : _textConverter.ConvertForward(text));
                }
                set => _textBox.Text = _textConverter == null
                    ? _parameter.ConvertValueToString(value ?? "") 
                    : (string) _textConverter.ConvertBackward(value);
            }

            private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e) => ValueChanged?.Invoke(this, e);

        }

        public static readonly NamedProperty<Regex> PatternProperty = new NamedProperty<Regex>("Pattern");

        public static readonly NamedProperty<double> FontSizeProperty = new NamedProperty<double>("FontSize");

        public static readonly NamedProperty<Brush> ForegroundProperty = new NamedProperty<Brush>("Foreground");

        public static readonly NamedProperty<double> TextBoxHeightProperty = new NamedProperty<double>("TextBoxHeight", double.NaN);

        public static readonly NamedProperty<int> MaxLengthProperty = new NamedProperty<int>("MaxLength");

        public static readonly NamedProperty<bool> MultiLineProperty = new NamedProperty<bool>("MultiLine", false);

        public static readonly NamedProperty<int> MaxLinesProperty = new NamedProperty<int>("MaxLines", int.MaxValue);

        public static readonly NamedProperty<TextAlignment> TextAlignmentProperty = new NamedProperty<TextAlignment>("TextAlignment");

        public static readonly NamedProperty<TextWrapping> TextWrappingProperty = new NamedProperty<TextWrapping>("TextWrapping");

        public static readonly NamedProperty<Action<TextBox>> TextBoxConfiguratorProperty = new NamedProperty<Action<TextBox>>("TextBoxConfigurator");

        /// <summary>
        /// Text converter to convert text to expected value type.
        /// </summary>
        public static readonly NamedProperty<ITypeConverter> TextConverterProperty = new NamedProperty<ITypeConverter>("TextConverter");

        public static readonly PlainTextPresenter Instance = new PlainTextPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var regex = PatternProperty.Get(param.Metadata, null);
            var textConverter = TextConverterProperty.Get(param.Metadata, null)?.Validate(typeof(string), param.ValueType);
            var textBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MaxLength = MaxLengthProperty.Get(param.Metadata, param.ValueType == typeof(char) ? 1 : 128),
                AcceptsReturn = MultiLineProperty.Get(param.Metadata), 
                MaxLines = MaxLinesProperty.Get(param.Metadata)
            };
            if (FontSizeProperty.TryGet(param.Metadata, out var fontSize)) textBox.FontSize = fontSize;
            if (ForegroundProperty.TryGet(param.Metadata, out var foreground)) textBox.Foreground = foreground;
            if (TextBoxHeightProperty.TryGet(param.Metadata, out var height)) textBox.Height = height;
            if (TextAlignmentProperty.TryGet(param.Metadata, out var textAlignment)) textBox.TextAlignment = textAlignment;
            if (TextWrappingProperty.TryGet(param.Metadata, out var textWrapping)) textBox.TextWrapping = textWrapping;
            TextBoxConfiguratorProperty.Get(param.Metadata, null)?.Invoke(textBox);
            return new ParameterViewModel(param, textBox, new Accessor(param, regex, textConverter, textBox), textBox);
        }

    }

    public static class PlainTextPresenterExt
    {

        public static T UsePlainTextPresenter<T>([NotNull] this T contextBuilder,
            double? fontSize = null, Brush foreground = null, double? textBoxHeight = null,
            int? maxLength = null, bool? multiLines = null, int? maxLines = null,
            TextAlignment? textAlignment = null, TextWrapping? textWrapping = null,
            Action<TextBox> configurator = null, ITypeConverter textConverter = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(PlainTextPresenter.Instance);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.FontSizeProperty, fontSize);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.ForegroundProperty, foreground);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.TextBoxHeightProperty, textBoxHeight);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.MaxLengthProperty, maxLength);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.MultiLineProperty, multiLines);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.MaxLinesProperty, maxLines);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.TextAlignmentProperty, textAlignment);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.TextWrappingProperty, textWrapping);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.TextBoxConfiguratorProperty, configurator);
            contextBuilder.SetPropertyNotNull(PlainTextPresenter.TextConverterProperty, textConverter);
            return contextBuilder;
        }

    }

}