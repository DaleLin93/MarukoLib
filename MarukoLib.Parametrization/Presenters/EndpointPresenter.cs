using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Windows;
using MarukoLib.UI;
using Border = System.Windows.Controls.Border;
using IPAddress = System.Net.IPAddress;

namespace MarukoLib.Parametrization.Presenters
{

    public class EndpointPresenter : IPresenter
    {

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [CanBeNull] private readonly Border _border;

            [NotNull] private readonly TextBox[] _addressTextBoxes;

            [CanBeNull] private readonly TextBox _portTextBox;

            public Adapter([NotNull] IParameterDescriptor parameter, [CanBeNull] Border border, 
                [NotNull] TextBox[] addressTextBoxes, [CanBeNull] TextBox portTextBox)
            {
                _parameter = parameter;

                _border = border;
                _addressTextBoxes = addressTextBoxes;
                _portTextBox = portTextBox;

                foreach (var textBox in _addressTextBoxes)
                    textBox.TextChanged += TextBox_OnTextChanged;
                if (_portTextBox != null)
                    _portTextBox.TextChanged += TextBox_OnTextChanged;
            }

            public object Value
            {
                get
                {
                    string addressStr = null;
                    if (_addressTextBoxes.Length == 1)
                        addressStr = _addressTextBoxes[0].Text.Trim2Null();
                    else
                    {
                        var addressParts = _addressTextBoxes.Select(textBox => textBox.Text)
                            .Select(StringUtils.Trim2Null)
                            .ToArray();
                        if (!addressParts.All(string.IsNullOrWhiteSpace))
                        {
                            if (addressParts.Any(string.IsNullOrWhiteSpace))
                                throw new Exception("Malformed IP address");
                            addressStr = addressParts.Join(".");
                        }
                    }
                    if (addressStr == null) return _parameter.IsValidOrThrow(null);
                    if (!IPAddress.TryParse(addressStr, out var ipAddress))
                        throw new Exception($"Malformed IP address: '{addressStr}'.");
                    if (_portTextBox == null)
                        return _parameter.IsValidOrThrow(_parameter.ValueType == typeof(IPAddress) ? (object) ipAddress : new Endpoint(ipAddress));
                    if (string.IsNullOrWhiteSpace(_portTextBox.Text) || !ushort.TryParse(_portTextBox.Text, out var port))
                        throw new Exception($"Invalid port for IP endpoint: '{_portTextBox.Text}'.");
                    var ipEndPoint = new IPEndPoint(ipAddress, port);
                    return _parameter.IsValidOrThrow(_parameter.ValueType == typeof(IPEndPoint) ? (object) ipEndPoint : new Endpoint(ipEndPoint));
                }
                set
                {
                    var address = value as IPAddress;
                    int? port = null;
                    switch (value)
                    {
                        case IPEndPoint endPoint:
                            address = endPoint.Address;
                            port = endPoint.Port;
                            break;
                        case Endpoint endpoint:
                        {
                            if (IPAddress.TryParse(endpoint.Address, out var ipAddress))
                                address = ipAddress;
                            port = endpoint.Port;
                            break;
                        }
                    }

                    lock(_updateLock.Ref())
                    {
                        if (address == null)
                            foreach (var textBox in _addressTextBoxes)
                                textBox.Text = string.Empty;
                        else
                        {
                            if (_addressTextBoxes.Length == 1)
                                _addressTextBoxes[0].Text = address.ToString();
                            else
                            {
                                if (address.AddressFamily != AddressFamily.InterNetwork)
                                    throw new Exception($"Address family not supported: '{address}'.");
                                var bytes = address.GetAddressBytes();
                                for (var i = 0; i < bytes.Length; i++)
                                    _addressTextBoxes[i].Text = bytes[i].ToString();
                            }
                        }
                        if (_portTextBox != null)
                            _portTextBox.Text = port?.ToString() ?? string.Empty;
                    }
                    RaiseValueChangedEvent(EventArgs.Empty);
                }
            }

            public bool IsEnabled
            {
                get => _addressTextBoxes[0].IsEnabled;
                set
                {
                    foreach (var addressTextBox in _addressTextBoxes)
                        addressTextBox.IsEnabled = value;
                    if (_portTextBox != null)
                        _portTextBox.IsEnabled = value;
                }
            }

            public bool IsValid
            {
                get => (_border == null ? _addressTextBoxes[0].Background : _border.Background) != ViewConsts.InvalidColorBrush;
                set
                {
                    var brush = value ? Brushes.Transparent : ViewConsts.InvalidColorBrush;
                    if (_border != null)
                    {
                        _border.Background = brush;
                        return;
                    }
                    foreach (var addressTextBox in _addressTextBoxes)
                        addressTextBox.Background = brush;
                    if (_portTextBox != null)
                        _portTextBox.Background = brush;
                }
            }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e) => RaiseValueChangedEvent(e);

        }

        private const int IpV6MaxLen = 46;

        private const string DecimalChars = "0123456789";

        private const string HexChars = DecimalChars + "ABCDEFabcdef";

        private const string IpV4AddressChars = DecimalChars + ".";

        private const string IpV6AddressChars = HexChars + ":/";

        private const string IpAddressChars = IpV4AddressChars + IpV6AddressChars;

        public static readonly NamedProperty<bool> AcceptIpV6Property = new NamedProperty<bool>("AcceptIPV6", false);

        public static readonly NamedProperty<bool> AcceptPortProperty = new NamedProperty<bool>("AcceptPort");

        public static readonly EndpointPresenter Instance = new EndpointPresenter();

        private static readonly Thickness TextBlockMargin = new Thickness(2, 0, 2, 0);

        private static Border CreateBorder(UIElement child) => new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = SystemColors.ActiveBorderBrush,
            Child = child
        };

        private static TextBox CreatePortTextBox(bool borderless)
        {
            var textBox = new TextBox {MaxLength = 5, TextAlignment = TextAlignment.Center};
            if (borderless) textBox.BorderThickness = new Thickness(0);
            textBox.SetupInputValidation(DecimalChars);
            return textBox;
        }

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            if (param.ValueType != typeof(IPAddress) && param.ValueType != typeof(IPEndPoint)
                                                     && param.ValueType != typeof(Endpoint) 
                                                     && param.ValueType != typeof(Endpoint?))
                throw new ArgumentException($"Value type not supported: '{param.ValueType}'.");
            if (param.ValueType == typeof(IPAddress) && AcceptPortProperty.TryGet(param.Metadata, out var p) && p)
                throw new ArgumentException("Port is only for endpoints.");
            var port = param.ValueType != typeof(IPAddress) && AcceptPortProperty.Get(param.Metadata, true);
            return AcceptIpV6Property.Get(param.Metadata) ? PresentV6(param, port) : PresentV4(param, port);
        }

        public ParameterViewModel PresentV4(IParameterDescriptor param, bool port)
        {
            var grid = new Grid();
            var border = CreateBorder(grid);
            for (var i = 0; i < 7; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = i % 2 == 0 ? ViewConsts.Star1GridLength : GridLength.Auto});
            var addressTextBoxes = new TextBox[4];
            for (var i = 0; i < addressTextBoxes.Length; i++)
            {
                var textBox = new TextBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    MaxLength = 3,
                    BorderThickness = new Thickness(0)
                };
                textBox.SetupInputValidation(DecimalChars);
                grid.AddWithColumn(addressTextBoxes[i] = textBox, i * 2);
            }
            for (var i = 0; i < 3; i++)
                grid.AddWithColumn(new TextBlock {Text = ".", Margin = TextBlockMargin, FontWeight = FontWeights.DemiBold}, i * 2 + 1);
            TextBox portTextBox = null;
            if (port)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1.25, GridUnitType.Star)});
                grid.AddWithColumn(new TextBlock {Text = ":", Margin = TextBlockMargin, FontWeight = FontWeights.DemiBold}, 7);
                grid.AddWithColumn(portTextBox = CreatePortTextBox(true), 8);
            }
            return new ParameterViewModel(param, border, new Adapter(param, border, addressTextBoxes, portTextBox));
        }

        public ParameterViewModel PresentV6(IParameterDescriptor param, bool port)
        {
            var addressTextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                MaxLength = IpV6MaxLen
            };
            addressTextBox.SetupInputValidation(IpAddressChars);
            if (!port) return new ParameterViewModel(param, addressTextBox, new Adapter(param, null, new[] {addressTextBox}, null));
            var portTextBox = CreatePortTextBox(false);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ViewConsts.Star1GridLength });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) });
            grid.AddWithColumn(new TextBlock {Text = "[", Margin = new Thickness(0, 0, 2, 0), FontWeight = FontWeights.DemiBold}, 0);
            grid.AddWithColumn(addressTextBox, 1);
            grid.AddWithColumn(new TextBlock {Text = "]:", Margin = TextBlockMargin, FontWeight = FontWeights.DemiBold}, 2);
            grid.AddWithColumn(portTextBox, 3);
            return new ParameterViewModel(param, grid, new Adapter(param, null, new[] { addressTextBox }, portTextBox));
        }

    }

    public static class IpAddressPresenterExt
    {

        public static T UseIpAddressPresenter<T>([NotNull] this T contextBuilder, bool? acceptIpV6 = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(EndpointPresenter.Instance);
            contextBuilder.SetPropertyNotNull(EndpointPresenter.AcceptIpV6Property, acceptIpV6);
            return contextBuilder;
        }

    }

}