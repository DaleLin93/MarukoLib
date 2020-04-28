using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using JetBrains.Annotations;
using MarukoLib.Graphics;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.UI;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Panel = System.Windows.Controls.Panel;

namespace MarukoLib.Parametrization.Windows
{

    public static class ViewHelper
    {

        [GlobalParameter("UI")]
        public static readonly Parameter<bool> DisableUiAnimation = new Parameter<bool>("Disable UI Animation", false);

        public static readonly ResourceDictionary Resources = new ResourceDictionary {Source = new Uri(ViewConsts.SharedResourceDictionaryUri)};
        
        public static object GetResource(string name)
        {
            var res = Resources[name];
            if (res == DependencyProperty.UnsetValue) throw new ProgrammingException($"Resource not found by name: '{name}'");
            return res;
        }

        public static TextBlock CreateDefaultComboBoxItem(string text = ViewConsts.NotSelectedComboBoxItemText,
            TextAlignment alignment = TextAlignment.Left) => new TextBlock
        {
            Text = text,
            FontStyle = FontStyles.Italic,
            Foreground = Brushes.DimGray,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = alignment
            };

        public static void ApplyPropertyAnimated<T>([NotNull] this FrameworkElement element, [NotNull] DependencyProperty property, 
            [NotNull] Func<T, T, AnimationTimeline> animationCreator, T value, bool animation = true) 
            => ApplyPropertyAnimated(element, property, animationCreator, (T) element.GetValue(property), value, animation);

        public static void ApplyPropertyAnimated<T>([NotNull] this FrameworkElement element, [NotNull] DependencyProperty property,
            [NotNull] Func<T, T, AnimationTimeline> animationCreator, T oldValue, T newValue, bool animation = true)
        {
            if (animation)
                element.BeginAnimation(property, animationCreator(oldValue, newValue), HandoffBehavior.SnapshotAndReplace);
            else
            {
                element.BeginAnimation(property, null);
                element.SetValue(property, newValue);
            }
        }

        public static void UpdateWindowSize(this Window window, double newHeight, double minWidth, bool animation = true)
        {
            animation = animation && !DisableUiAnimation.Get(GlobalContext.Variables);
            var point = window.PointToScreen(new Point(window.ActualWidth / 2, window.ActualHeight / 2));
            var screen = Screen.FromPoint(point.RoundToSdPoint());
            var scaleFactor = DpiUtils.Scale;
            var maxWidth = screen.WorkingArea.Width / scaleFactor;
            var maxHeight = screen.WorkingArea.Height / scaleFactor;
            minWidth = Math.Min(maxWidth, minWidth);
            newHeight = Math.Min(maxHeight, newHeight);
            var winWidth = window.Width;
            var winHeight = window.Height;
            var winRightEx = window.Left + minWidth + (window.ActualWidth - window.Width) - screen.WorkingArea.Right / scaleFactor;
            var winBottomEx = window.Top + newHeight + (window.ActualHeight - window.Height) - screen.WorkingArea.Bottom / scaleFactor;
            if (minWidth - winWidth > 1.0) window.ApplyPropertyAnimated(FrameworkElement.WidthProperty, CreateDoubleAnimation, winWidth, minWidth, animation);
            if (Math.Abs(newHeight - winHeight) > 1.0) window.ApplyPropertyAnimated(FrameworkElement.HeightProperty, CreateDoubleAnimation, winHeight, newHeight, animation);
            if (winRightEx > 0) window.ApplyPropertyAnimated(Window.LeftProperty, CreateDoubleAnimation, window.Left, Math.Max(0, window.Left - winRightEx), animation);
            if (winBottomEx > 0) window.ApplyPropertyAnimated(Window.TopProperty, CreateDoubleAnimation, window.Top, Math.Max(0, window.Top - winBottomEx), animation);
        }

        public static DoubleAnimation CreateDoubleAnimation(double from, double to) => CreateDoubleAnimationEx(from, to);

        public static DoubleAnimation CreateDoubleAnimationEx(double from, double to, FillBehavior? fillBehavior = null,
            Action completeAction = null, Duration? duration = null, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation(from, to, duration ?? ViewConsts.DefaultAnimationDuration)
            {
                FillBehavior = fillBehavior ?? FillBehavior.HoldEnd,
                EasingFunction = easingFunction ?? ViewConsts.DefaultEasingFunction
            };
            if (completeAction != null) animation.Completed += (sender, e) => completeAction();
            return animation;
        }

        public static GroupHeader CreateGroupHeader(string header, string description, bool collapsable = false)
        {
            var tooltipBuilder = new StringBuilder(64).Append(header);
            if (!string.IsNullOrEmpty(description)) tooltipBuilder.AppendIfEmpty('\n').Append(description);
            if (collapsable) tooltipBuilder.AppendIfEmpty('\n').Append("(Click to Collapse/Expand)");
            return new GroupHeader {Header = header, Description = tooltipBuilder.ToString(), IsExpandable = collapsable};
        }

        private static TextBlock CreateLabelTextBlock(string text, string tooltip = null) =>
            new TextBlock {Style = (Style) GetResource("LabelText"), Text = text, ToolTip = tooltip};

        public static TextBlock CreateParamNameTextBlock(IParameterDescriptor param, bool dbClick2Reset = false)
        {
            var tooltipBuilder = new StringBuilder(64)
                .Append("Id: ").Append(param.Id)
                .Append("\nType: ").Append(param.ValueType.GetFriendlyName());
            if (!string.IsNullOrEmpty(param.Description)) tooltipBuilder.Append('\n').Append(param.Description);
            if (dbClick2Reset) tooltipBuilder.Append("\n(Double-Click to Reset)");
            return CreateLabelTextBlock(param.Name + (param.Unit == null ? "" : $" ({param.Unit})"), tooltipBuilder.ToString());
        }

        public static DockPanel CreateGroupPanel([NotNull] GroupHeader groupHeader, int depth = 0)
        {
            var dockPanel = new DockPanel();
            if (depth > 0)
            {
                var groupIntendRectangle =  new Rectangle
                {
                    Width = 2,
                    Margin = new Thickness {Left = ViewConsts.Intend},
                    Fill = GetResource("LightSeparatorColorBrush") as Brush,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                dockPanel.Children.Add(groupIntendRectangle);
                DockPanel.SetDock(groupIntendRectangle, Dock.Left);
            }
            dockPanel.Children.Add(groupHeader);
            DockPanel.SetDock(groupHeader, Dock.Top);
            return dockPanel;
        }

        public static StackPanel AddGroupStackPanel(this Panel parent, string header, string description = null)
        {
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(CreateGroupHeader(header, description));
            parent.Children.Add(stackPanel);
            return stackPanel;
        } 
            
        public static DockPanel AddGroupDockPanel(this Panel parent, [NotNull] GroupHeader groupHeader, int depth = 0)
        {
            var dockPanel = CreateGroupPanel(groupHeader, depth);
            parent.Children.Add(dockPanel);
            return dockPanel;
        }

        public static GroupViewModel CreateGroupViewModel(IGroupDescriptor group, int depth = 0, bool click2Collapse = false, Func<bool> collapseControl = null)
        {
            var groupHeader = CreateGroupHeader(group.Name, group.Description, click2Collapse);
            var dockPanel = CreateGroupPanel(groupHeader, depth);
            var itemsPanel = new StackPanel();
            dockPanel.Children.Add(itemsPanel);
            var viewModel = new GroupViewModel(group, groupHeader, dockPanel, itemsPanel, depth);
            if (click2Collapse) groupHeader.MouseLeftButtonUp += (sender, e) =>
            {
                var collapse = !viewModel.IsCollapsed;
                if (collapse && collapseControl != null && !collapseControl()) return;
                viewModel.SetCollapsed(collapse);
            };
            return viewModel;
        }

        public static LabeledRow CreateLabeledRow(string label, UIElement contentPart, uint rowHeight = 0) 
            => CreateLabeledRow(label == null ? null : CreateLabelTextBlock(label), contentPart, rowHeight);

        public static LabeledRow CreateLabeledRow(TextBlock labelPart, UIElement contentPart, uint rowHeight = 0)
        {
            var row = new LabeledRow(labelPart, contentPart);
            if (rowHeight > 0) row.Height = rowHeight;
            return row;
        }

        public static LabeledRow AddLabeledRow(this Panel parent, string label, UIElement contentPart, uint rowHeight = 0) =>
            AddLabeledRow(parent, label == null ? null : CreateLabelTextBlock(label), contentPart, rowHeight);

        public static LabeledRow AddLabeledRow(this Panel parent, TextBlock labelPart, UIElement contentPart, uint rowHeight = 0)
        {
            var row = CreateLabeledRow(labelPart, contentPart, rowHeight);
            parent.Children.Add(row);
            return row;
        }

    }

}
