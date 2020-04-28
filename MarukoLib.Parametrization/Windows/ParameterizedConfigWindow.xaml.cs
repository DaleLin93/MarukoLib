using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using JetBrains.Annotations;
using MarukoLib.Graphics;
using MarukoLib.Lang;
using MarukoLib.UI;
using MarukoLib.Windows;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MarukoLib.Parametrization.Windows
{

    /// <inheritdoc cref="Window" />
    /// <summary>
    /// Interaction logic for ParameterizedConfigWindow.xaml
    /// </summary>
    public partial class ParameterizedConfigWindow
    {

        public event EventHandler<ContextChangedEventArgs> ContextChanged;

        private bool _needAutoUpdateWindowSize;

        public ParameterizedConfigWindow([NotNull] string title, [NotNull] IEnumerable<IDescriptor> descriptors,
            IReadonlyContext context = null, IParameterPresentAdapter adapter = null)
        {
            InitializeComponent();
            ConfigurationPanel.SetDescriptors(adapter, descriptors);
            ConfigurationPanel.Context = context ?? EmptyContext.Instance;
            Title = title;
        }

        public bool IsOkButtonVisible
        {
            get => ActionPanel.IsVisible;
            set => ActionPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Update(IParameterDescriptor parameter, object value, bool quietly = true) => ConfigurationPanel.SetParameter(parameter, value, quietly);

        public void Update(IReadonlyContext context, bool quietly = true) => ConfigurationPanel.ApplyContext(context, quietly);

        public bool ShowDialog(out IReadonlyContext args)
        {
            args = EmptyContext.Instance;
            var dialogResult = ShowDialog() ?? false;
            if (!dialogResult) return false;
            args = ConfigurationPanel.Context;
            return true;
        }

        private void Confirm()
        {
            var invalidParams = ConfigurationPanel.GetInvalidParams().ToLinkedList();
            if (!invalidParams.IsEmpty())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("The following parameters are invalid");
                foreach (var param in invalidParams) stringBuilder.Append("\n - ").Append(param.Name);
                MsgBoxUtils.WarningOk(stringBuilder.ToString());
                return;
            }
            DialogResult = true;
            Close();
        }

        private void UpdateWindowSize(bool preferAnimation)
        {
            var contentHeight = StackPanel.ActualHeight;
            var minWidth = ConfigurationPanel.Adapter?.DesiredWidth ?? 0;
            this.UpdateWindowSize(contentHeight + 50 + (ActualHeight - StackPanel.ActualHeight), minWidth, preferAnimation);
        }

        private void Window_OnLoaded(object sender, EventArgs e) => UpdateWindowSize(false);

        private void Window_OnLayoutUpdated(object sender, EventArgs e)
        {
            if (!IsVisible || !_needAutoUpdateWindowSize) return;
            var point = PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2));
            var screen = Screen.FromPoint(point.RoundToSdPoint());
            var scaleFactor = DpiUtils.Scale;
            var maxHeight = screen.WorkingArea.Height / scaleFactor;
            var contentHeight = StackPanel.Children.OfType<FrameworkElement>().Sum(el => el.ActualHeight);
            Height = MaxHeight = Math.Min(contentHeight + 20 + (ActualHeight - ScrollView.ActualHeight), maxHeight);
            var offset = screen.WorkingArea.Bottom / scaleFactor - (Top + ActualHeight);
            if (offset < 0) Top += offset;
            _needAutoUpdateWindowSize = false;
        }

        private void Window_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IsOkButtonVisible && e.KeyStates == Keyboard.GetKeyStates(Key.Return) && Keyboard.Modifiers == ModifierKeys.Alt) Confirm();
        }

        private void ConfigurationPanel_OnLayoutChanged(object sender, LayoutChangedEventArgs e)
        {
            if (e.IsInitialization)
            {
                ScrollView.InvalidateScrollInfo();
                ScrollView.ScrollToTop();
            }
            _needAutoUpdateWindowSize = true;
        }

        private void ConfigurationPanel_OnContextChanged(object sender, ContextChangedEventArgs e) => ContextChanged?.Invoke(sender, e);

        private void OkBtn_OnClick(object sender, RoutedEventArgs e) => Confirm();

    }
}
