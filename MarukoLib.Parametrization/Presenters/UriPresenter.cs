using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Windows;
using MarukoLib.UI;
using Microsoft.Win32;

namespace MarukoLib.Parametrization.Presenters
{

    public class UriPresenter : IPresenter
    {

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly ISet<string> _supportedSchemes;

            private readonly bool _checkFileExistence;

            [NotNull] private readonly TextBox _uriTextBox;

            [CanBeNull] private readonly ComboBox _schemeComboBox;

            [CanBeNull] private readonly Button _browseButton;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] ISet<string> supportedSchemes, 
                [NotNull] TextBox uriTextBox, [CanBeNull] ComboBox schemeComboBox, [CanBeNull] Button browseButton)
            {
                _parameter = parameter;
                _supportedSchemes = supportedSchemes;
                _checkFileExistence = CheckFileExistenceProperty.Get(parameter.Metadata);

                _uriTextBox = uriTextBox;
                _schemeComboBox = schemeComboBox;
                _browseButton = browseButton;

                _uriTextBox.TextChanged += UriTextBox_OnUriTextChanged;
                if (_schemeComboBox != null)
                    _schemeComboBox.SelectionChanged += SchemeComboBox_OnSelectionChanged;
                if (_browseButton != null)
                    _browseButton.Click += BrowseButton_OnClick;
            }

            public bool IsEnabled
            {
                get => _uriTextBox.IsEnabled;
                set
                {
                    _uriTextBox.IsEnabled = value;
                    if (_schemeComboBox != null) _schemeComboBox.IsEnabled = value;
                    if (_browseButton != null) _browseButton.IsEnabled = value;
                }
            }

            public bool IsValid
            {
                get => _uriTextBox.Background != ViewConsts.InvalidColorBrush;
                set => _uriTextBox.Background = value ? Brushes.Transparent : ViewConsts.InvalidColorBrush;
            }

            public object Value
            {
                get
                {
                    var uriString = _uriTextBox.Text;
                    if (_schemeComboBox != null)
                    {
                        if (_schemeComboBox.SelectedValue == null)
                            throw new Exception("Scheme is not selected.");
                        uriString = _schemeComboBox.SelectedValue + uriString;
                    }
                    var uri = new Uri(uriString);
                    if (!IsSupportedScheme(uri.Scheme)) 
                        throw new NotSupportedException($"Invalid URI scheme: {uri.Scheme}.");
                    if (uri.IsFile && _checkFileExistence && !File.Exists(uri.LocalPath)) 
                        throw new FileNotFoundException(uri.LocalPath);
                    return _parameter.IsValidOrThrow(uri);
                }
                set
                {
                    if (!(value is Uri uri)) return;
                    lock (_updateLock)
                    {
                        if (_schemeComboBox != null)
                        {
                            if (!_schemeComboBox.FindAndSelectFirstByString($"{uri.Scheme.ToLowerInvariant()}://")) return;
                            _uriTextBox.Text = uri.PathAndQuery;
                        }
                        else
                            _uriTextBox.Text = uri.ToString();
                    }
                    RaiseValueChangedEvent();
                }
            }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private bool IsSupportedScheme([NotNull] string scheme) 
                => !_supportedSchemes.Any() || _supportedSchemes.Contains(scheme.ToLowerInvariant());

            private void UriTextBox_OnUriTextChanged(object sender, TextChangedEventArgs e) => RaiseValueChangedEvent(e);

            private void SchemeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => RaiseValueChangedEvent(e);

            private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
            {
                var initialDirectory = string.Empty;
                try
                {
                    if (Value is Uri uri && uri.IsFile)
                        initialDirectory = Path.GetDirectoryName(uri.LocalPath) ?? initialDirectory;
                }
                catch (Exception)
                {
                    /* ignored */
                }
                var dialog = new OpenFileDialog
                {
                    Title = $"Select File: {_parameter.Name}",
                    Multiselect = false,
                    CheckFileExists = _checkFileExistence,
                    Filter = FileFilterProperty.Get(_parameter.Metadata),
                    InitialDirectory = initialDirectory
                };
                if (!(bool) dialog.ShowDialog(Window.GetWindow((Button) sender))) return;
                Value = new Uri("file:\\" + dialog.FileName);
            }

        }

        public static readonly NamedProperty<string[]> SupportedSchemesProperty = new NamedProperty<string[]>("SupportedSchemes");

        public static readonly NamedProperty<bool> IsSchemeSelectableProperty = new NamedProperty<bool>("IsSchemeSelectable");

        public static readonly NamedProperty<bool> ShowFileSelectorProperty = PathPresenter.ShowSelectorProperty;

        public static readonly NamedProperty<string> FileFilterProperty = PathPresenter.FilterProperty;

        public static readonly NamedProperty<bool> CheckFileExistenceProperty = PathPresenter.CheckExistenceProperty;

        public static readonly UriPresenter Instance = new UriPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var schemes = new HashSet<string>(SupportedSchemesProperty
                .Get(param.Metadata, EmptyArray<string>.Instance)
                .Select(scheme => scheme.ToLowerInvariant()));
            if (schemes.Any() && IsSchemeSelectableProperty.Get(param.Metadata, true))
                return PresentComposite(param, schemes);
            return PresentPlainText(param, schemes);
        }

        internal ParameterViewModel PresentComposite(IParameterDescriptor param, ISet<string> schemes)
        {
            var comboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                ItemsSource = schemes.Select(scheme => $"{scheme}://"), 
                SelectedIndex = 0
            };
            var textBox = new TextBox {MaxLength = 256};
            var container = new Grid
            {
                Children = {comboBox, textBox},
                ColumnDefinitions =
                {
                    new ColumnDefinition {Width = GridLength.Auto},
                    new ColumnDefinition {Width = new GridLength(ViewConsts.MinorSpacing)},
                    new ColumnDefinition {Width = ViewConsts.Star1GridLength}
                }
            };
            Grid.SetColumn(comboBox, 0);
            Grid.SetColumn(textBox, 2);

            Button button = null;
            if ((!schemes.Any() || schemes.Contains("file")) && ShowFileSelectorProperty.Get(param.Metadata, true))
            {
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ViewConsts.MinorSpacing) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                button = new Button { Content = "...", HorizontalAlignment = HorizontalAlignment.Right, Width = 25 };
                container.Children.Add(button);
                Grid.SetColumn(button, 4);
            }
            return new ParameterViewModel(param, container, new Adapter(param, schemes, textBox, comboBox, button));
        }

        internal ParameterViewModel PresentPlainText(IParameterDescriptor param, ISet<string> schemes)
        {
            var textBox = new TextBox {MaxLength = 256};
            var container = new Grid {Children = {textBox}};

            Button button = null;
            if ((!schemes.Any() || schemes.Contains("file")) && ShowFileSelectorProperty.Get(param.Metadata, true))
            {
                container.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.Star1GridLength});
                container.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(ViewConsts.MinorSpacing)});
                container.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
                button = new Button {Content = "...", HorizontalAlignment = HorizontalAlignment.Right, Width = 25};
                container.Children.Add(button);
                Grid.SetColumn(textBox, 0);
                Grid.SetColumn(button, 2);
            }
            return new ParameterViewModel(param, container, new Adapter(param, schemes, textBox, null, button));
        }

    }
}