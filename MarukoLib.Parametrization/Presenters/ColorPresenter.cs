using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Windows;
using MarukoLib.UI;
using Color = System.Drawing.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MarukoLib.Parametrization.Presenters
{

    public class ColorPresenter : IPresenter
    {

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            private readonly IParameterDescriptor _parameter;

            private readonly bool _isSdColor;

            private readonly Rectangle _rect;

            private readonly TextBlock _textBlock;

            public Adapter(IParameterDescriptor parameter, bool isSdColor, Rectangle rect, TextBlock textBlock)
            {
                _parameter = parameter;
                _isSdColor = isSdColor;
                _rect = rect;
                _textBlock = textBlock;
                _rect.MouseEnter += Rect_OnMouseEnter;
                _rect.MouseLeave += Rect_OnMouseLeave;
                _rect.MouseLeftButtonUp += Rect_OnMouseLeftButtonUp;
            }

            public bool IsEnabled
            {
                get => _rect.IsEnabled;
                set => _rect.IsEnabled = value;
            }

            public bool IsValid { get; set; }

            public object Value
            {
                get
                {
                    var color = ((SolidColorBrush)_rect.Fill).Color;
                    return _parameter.IsValidOrThrow(_isSdColor ? color.ToSdColor() : (object)color);
                }
                set
                {
                    var color = (_isSdColor ? ((Color?)value)?.ToSwmColor() : (System.Windows.Media.Color?)value) ?? System.Windows.Media.Color.FromScRgb(0, 0, 0, 0);
                    _rect.Fill = new SolidColorBrush(color);
                    if (_textBlock != null)
                    {
                        _textBlock.Text = $"ARGB({color.A}, {color.R}, {color.G}, {color.B})";
                        _textBlock.Foreground = new SolidColorBrush(color.Inverted());
                    }
                }
            }

            private void Rect_OnMouseEnter(object sender, MouseEventArgs e) => _textBlock.Visibility = Visibility.Visible;

            private void Rect_OnMouseLeave(object sender, MouseEventArgs e) => _textBlock.Visibility = Visibility.Hidden;

            private void Rect_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                using (var dialog = new ColorDialog {Color = ((_rect.Fill as SolidColorBrush)?.Color ?? Colors.Red).ToSdColor()})
                {
                    if (DialogResult.OK != dialog.ShowDialog()) return;
                    _rect.Fill = new SolidColorBrush(dialog.Color.ToSwmColor());
                }
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }

        }

        /// <summary>
        /// Default Value: true
        /// </summary>
        public static readonly NamedProperty<bool> ShowArgbProperty = new NamedProperty<bool>("ShowArgb", true);

        public static readonly ColorPresenter Instance = new ColorPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var isSdColor = param.ValueType == typeof(Color) || param.ValueType == typeof(Color?);
            var rectangle = new Rectangle {Stroke = (Brush) ViewHelper.GetResource("SeparatorFillBrush"), MinHeight = 15};
            Grid grid = null;
            TextBlock textBlock = null;
            if (ShowArgbProperty.Get(param.Metadata))
            {
                grid = new Grid();
                grid.Children.Add(rectangle);
                textBlock = new TextBlock
                {
                    FontSize = 8,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Visibility = Visibility.Hidden,
                    IsHitTestVisible = false
                };
                grid.Children.Add(textBlock);
            }
            return new ParameterViewModel(param, grid ?? (UIElement)rectangle, new Adapter(param, isSdColor, rectangle, textBlock));
        }

    }

}