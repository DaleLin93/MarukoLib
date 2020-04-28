using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using JetBrains.Annotations;
using MarukoLib.IO;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Windows;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = MarukoLib.Parametrization.Data.Path;
using TextBox = System.Windows.Controls.TextBox;

namespace MarukoLib.Parametrization.Presenters
{

    public class PathPresenter : IPresenter
    {

        public enum PathType
        {
            File, Directory
        }

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly TextBox _pathTextBox;

            [CanBeNull] private readonly Button _browseButton;

            private readonly PathType _pathType;

            private readonly bool _checkExistence;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] TextBox pathTextBox, [CanBeNull] Button browseButton, 
                PathType pathType, bool checkExistence)
            {
                _parameter = parameter;
                _pathTextBox = pathTextBox;
                _browseButton = browseButton;
                _pathType = pathType;
                _checkExistence = checkExistence;

                _pathTextBox.TextChanged += PathTextBox_OnTextChanged;
            }

            public bool IsEnabled
            {
                get => _pathTextBox.IsEnabled;
                set
                {
                    _pathTextBox.IsEnabled = value;
                    if (_browseButton != null) _browseButton.IsEnabled = value;
                }
            }

            public bool IsValid
            {
                get => _pathTextBox.Background != ViewConsts.InvalidColorBrush;
                set => _pathTextBox.Background = value ? Brushes.Transparent : ViewConsts.InvalidColorBrush;
            }

            public object Value
            {
                get
                {
                    var path = string.IsNullOrWhiteSpace(_pathTextBox.Text) ? (Path?)null : new Path(_pathTextBox.Text);
                    if (path != null && _checkExistence)
                    {
                        switch (_pathType)
                        {
                            case PathType.File:
                                if (!File.Exists(path.Value.Value)) throw new Exception("File not exists");
                                break;
                            case PathType.Directory:
                                if (!Directory.Exists(path.Value.Value)) throw new Exception("Directory not exists");
                                break;
                            default:
                                throw new NotSupportedException($"unsupported path type: {_pathType}" );
                        }
                    }
                    return _parameter.IsValidOrThrow(path);
                }
                set
                {
                    switch (value)
                    {
                        case null:
                            _pathTextBox.Text = string.Empty;
                            break;
                        case Path path:
                            _pathTextBox.Text = path.Value;
                            break;
                        case string str:
                            _pathTextBox.Text = str;
                            break;
                        default:
                            _pathTextBox.Text = _pathTextBox.Text;
                            break;
                    }
                }
            }

            private void PathTextBox_OnTextChanged(object sender, TextChangedEventArgs e) => ValueChanged?.Invoke(this, e);

        }

        public static readonly NamedProperty<string> FilterProperty = new NamedProperty<string>("Filter", FileUtils.AllFileFilterPattern);

        public static readonly NamedProperty<PathType> PathTypeProperty = new NamedProperty<PathType>("PathType", PathType.File);

        public static readonly NamedProperty<bool> ShowSelectorProperty = new NamedProperty<bool>("ShowSelector", true);

        public static readonly NamedProperty<bool> CheckExistenceProperty = new NamedProperty<bool>("CheckExistence", true);

        public static readonly PathPresenter Instance = new PathPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var pathType = PathTypeProperty.Get(param.Metadata);
            var checkExistence = CheckExistenceProperty.Get(param.Metadata);

            var textBox = new TextBox {MaxLength = 256};
            var container = new Grid {Children = {textBox}};

            Button button = null;
            if (ShowSelectorProperty.Get(param.Metadata))
            {
                button = new Button {Content = "...", HorizontalAlignment = HorizontalAlignment.Right, Width = 25};
                textBox.Margin = new Thickness {Right = ViewConsts.MinorSpacing + button.Width};
                button.Click += (sender, args) =>
                {
                    switch (pathType)
                    {
                        case PathType.File:
                            var openFileDialog = new OpenFileDialog
                            {
                                Title = $"Select File: {param.Name}",
                                Multiselect = false,
                                CheckFileExists = checkExistence,
                                Filter = FilterProperty.Get(param.Metadata)
                            };
                            if (!textBox.Text.IsBlank()) openFileDialog.InitialDirectory = new FileInfo(textBox.Text).Directory?.FullName ?? "";
                            if ((bool) openFileDialog.ShowDialog(Window.GetWindow(button))) textBox.Text = openFileDialog.FileName;
                            break;
                        case PathType.Directory:
                            using (var dialog = new FolderBrowserDialog())
                            {
                                if (!textBox.Text.IsBlank()) dialog.SelectedPath = textBox.Text;
                                var result = dialog.ShowDialog();
                                if (result == DialogResult.OK) textBox.Text = dialog.SelectedPath;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                };
                container.Children.Add(button);
            }
            return new ParameterViewModel(param, container, new Adapter(param, textBox, button, pathType, checkExistence));
        }

    }
}