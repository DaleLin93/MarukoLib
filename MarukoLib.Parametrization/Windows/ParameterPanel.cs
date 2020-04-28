using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.Parametrization.Presenters;
using MarukoLib.Windows;

namespace MarukoLib.Parametrization.Windows
{

    public class ParameterPanel : StackPanel
    {

        private struct GroupMeta
        {

            internal readonly GroupViewModel Group;

            internal readonly StackPanel Container;

            internal readonly IEnumerator<IDescriptor> Items;

            public GroupMeta(GroupViewModel group, StackPanel container, IEnumerator<IDescriptor> items)
            {
                Group = group;
                Container = container;
                Items = items;
            }

        }

        public static readonly ContextProperty<bool> ParameterResettableProperty = new ContextProperty<bool>(true);

        public static readonly ContextProperty<HorizontalAlignment> NameTextHorizontalAlignProperty =
            new ContextProperty<HorizontalAlignment>(HorizontalAlignment.Left);

        public static readonly ContextProperty<VerticalAlignment> NameTextVerticalAlignProperty =
            new ContextProperty<VerticalAlignment>(VerticalAlignment.Center);

        public event EventHandler<LayoutChangedEventArgs> LayoutChanged;

        public event EventHandler<ContextChangedEventArgs> ContextChanged;

        private readonly ReferenceCounter _updateLock = new ReferenceCounter();

        private readonly IDictionary<IGroupDescriptor, GroupViewModel> _groupViewModels =
            new Dictionary<IGroupDescriptor, GroupViewModel>();

        private readonly IDictionary<IParameterDescriptor, ParamRowViewModel> _paramRowViewModels =
            new Dictionary<IParameterDescriptor, ParamRowViewModel>();

        private bool _canCollapse = true;

        private Context _context = new Context(64);

        public ParameterPanel() => VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);

        [CanBeNull] public GroupViewModel this[IGroupDescriptor group] => _groupViewModels.TryGetValue(group, out var viewModel) ? viewModel : null;

        [CanBeNull] public ParamRowViewModel this[IParameterDescriptor parameter] => _paramRowViewModels.TryGetValue(parameter, out var viewModel) ? viewModel : null;

        public bool CanCollapse
        {
            get => _canCollapse; 
            set
            {
                _canCollapse = value;
                foreach (var group in _groupViewModels.Values)
                {
                    var canCollapse = value && (Adapter?.CanCollapse(group.Group, group.Depth) ?? false);
                    group.GroupHeader.IsExpandable = canCollapse;
                    if (!canCollapse && !group.GroupHeader.IsExpanded) group.GroupHeader.IsExpanded = true;
                }
            }
        }

        [CanBeNull] public IParameterPresentAdapter Adapter { get; private set; }

        [CanBeNull] public IReadOnlyCollection<IDescriptor> Descriptors { get; private set; }

        [NotNull] public IReadonlyContext Context
        {
            get
            {
                var context = new Context();
                foreach (var entry in _paramRowViewModels)
                    context[entry.Key] = _context.TryGet(entry.Key, out var val) 
                        ? val : entry.Value.Parameter.DefaultValue;
                return context;
            }
            set
            {
                if (_paramRowViewModels.Any())
                    using (_updateLock.Ref())
                        foreach (var entry in _paramRowViewModels)
                        {
                            if (!value.TryGet(entry.Key, out var val) || !entry.Key.IsValid(val))
                                val = entry.Value.Parameter.DefaultValue;
                            entry.Value.ParameterViewModel.Value = val;
                        }
                Refresh();
            }
        }

        internal ParameterViewModel[] ParameterViewModels => _paramRowViewModels.Values.Select(viewModel => viewModel.ParameterViewModel).ToArray();

        public void SetDescriptors(IParameterPresentAdapter adapter, IEnumerable<IDescriptor> descriptors)
        {
            Adapter = adapter;
            Descriptors = descriptors?.ToArray() ?? EmptyArray<IDescriptor>.Instance;
            InitializeConfigurationPanel();
        }

        public void SetParameter(IParameterDescriptor parameter, object value, bool quietly = true)
        {
            using (_updateLock.Ref())
                if (_paramRowViewModels.TryGetValue(parameter, out var viewModel))
                    viewModel.ParameterViewModel.Value = value;
            if (!quietly) OnParamsUpdated(false);
        }

        public void ApplyContext(IReadonlyContext context, bool quietly = true)
        {
            using (_updateLock.Ref())
                foreach (var param in context.Properties.OfType<IParameterDescriptor>())
                    if (_paramRowViewModels.TryGetValue(param, out var viewModel))
                        if (context.TryGet(param, out var value))
                            viewModel.ParameterViewModel.Value = value;
            if (!quietly) OnParamsUpdated(false);
        }

        public void Refresh()
        {
            var context = new Context();
            foreach (var entry in _paramRowViewModels)
                try { context[entry.Key] = entry.Value.ParameterViewModel.Value; }
                catch (Exception) { /* ignored */ }
            _context = context;
            OnParamsUpdated();
        }

        public void ResetToDefault() => Context = EmptyContext.Instance;

        public IEnumerable<IParameterDescriptor> GetInvalidParams() => 
            from pvm in _paramRowViewModels.Values
            where !pvm.ParameterViewModel.IsValid
            select pvm.Parameter;

        private void InitializeConfigurationPanel()
        {
            Children.Clear();
            _groupViewModels.Clear();
            _paramRowViewModels.Clear();

            var paramKeySet = new HashSet<string>();
            var stack = new Stack<GroupMeta>();
            stack.Push(new GroupMeta(null, this, (Descriptors ?? EmptyArray<IDescriptor>.Instance).GetEnumerator()));
            do
            {
                var groupMeta = stack.Peek();
                if (!groupMeta.Items.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                var currentItem = groupMeta.Items.Current;
                switch (currentItem)
                {
                    case null:
                        continue;
                    case IParameterDescriptor parameter:
                    {
                            if (_paramRowViewModels.ContainsKey(parameter)) throw new ProgrammingException($"Parameter duplicated: {parameter.Id}");
                            if (!paramKeySet.Add(parameter.Id)) throw new ProgrammingException($"Parameter key duplicated: {parameter.Id}");
                            var parameterViewModel = parameter.Present();
                            var canReset = ParameterResettableProperty.Get(parameter.Metadata);
                            var nameTextBlock = ViewHelper.CreateParamNameTextBlock(parameter, canReset);
                            nameTextBlock.HorizontalAlignment = NameTextHorizontalAlignProperty.Get(parameter.Metadata);
                            nameTextBlock.VerticalAlignment = NameTextVerticalAlignProperty.Get(parameter.Metadata);
                            if (canReset)
                            {
                                nameTextBlock.Tag = parameterViewModel;
                                nameTextBlock.MouseLeftButtonDown += ParameterNameTextBlock_OnMouseLeftButtonDown;
                            }
                            var rowGrid = groupMeta.Container.AddLabeledRow(nameTextBlock, parameterViewModel.Element);
                            var paramViewModel = new ParamRowViewModel(groupMeta.Group, rowGrid, nameTextBlock, parameterViewModel);
                            paramViewModel.AnimationCompleted += (sender, e) => LayoutChanged?.Invoke(this, LayoutChangedEventArgs.NonInitialization);
                            _paramRowViewModels[parameter] = paramViewModel;
                            break;
                    }
                    case IGroupDescriptor group:
                        if (_groupViewModels.ContainsKey(group)) throw new ProgrammingException($"Invalid paradigm, parameter group duplicated: {group.Name}");
                        var depth = stack.Count - 1;
                        var canCollapse = CanCollapse && (Adapter?.CanCollapse(group, depth) ?? false);
                        var groupViewModel = ViewHelper.CreateGroupViewModel(group, depth, canCollapse);
                        groupViewModel.AnimationCompleted += (sender, e) => LayoutChanged?.Invoke(this, LayoutChangedEventArgs.NonInitialization);
                        groupMeta.Container.Children.Add(groupViewModel.GroupPanel);
                        stack.Push(new GroupMeta(groupViewModel, groupViewModel.ItemsPanel, groupViewModel.Group.Items.GetEnumerator()));
                        _groupViewModels[group] = groupViewModel;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported descriptor: '{currentItem.Name}', type: '{currentItem.GetType().Name}'");
                }
            } while (stack.Any());

            /* Update parameters and listen value changed events. */
            foreach (var paramRowViewModel in _paramRowViewModels.Values)
            {
                var parameterViewModel = paramRowViewModel.ParameterViewModel;
                var parameter = parameterViewModel.Parameter;
                using (_updateLock.Ref())
                    try { parameterViewModel.Value = _context.TryGet(parameter, out var val) ? val : parameter.DefaultValue; }
                    catch (Exception) { /* ignored */ }
                try { _context[parameter] = parameterViewModel.Value; }
                catch (Exception) { /* ignored */ }
                parameterViewModel.ValueChanged += ParameterViewModel_OnValueChanged;
            }
            OnParamsUpdated(true);
            LayoutChanged?.Invoke(this, LayoutChangedEventArgs.Initialization);
        }

        private void OnParamChanged(ParameterViewModel parameterViewModel)
        {
            if (_updateLock.IsReferred) return;
            try { _context[parameterViewModel.Parameter] = parameterViewModel.Value; }
            catch (Exception) { /* ignored */ }
            OnParamsUpdated();
        }

        private void OnParamsUpdated(bool initializing = false) 
        {
            if (_updateLock.IsReferred) return;
            if (GetInvalidParams().Any()) return;
            UpdateParamVisibility(_context, initializing);
            UpdateParamAvailability(_context);
            ContextChanged?.Invoke(this, new ContextChangedEventArgs(_context));
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool UpdateParamVisibility(IReadonlyContext context, bool initializing)
        {
            var adapter = Adapter;
            if (adapter == null) return false;
            var visibilityChanged = false;
            foreach (var pViewModel in _paramRowViewModels.Values)
            {
                var visible = adapter.IsVisible(context, pViewModel.Parameter);
                if (visible == pViewModel.IsVisible) continue;
                pViewModel.SetVisible(visible, (pViewModel.Group?.IsVisible ?? true) && !initializing);
                visibilityChanged = true;
            }
            foreach (var gViewModel in _groupViewModels.Values)
            {
                var visible = adapter.IsVisible(context, gViewModel.Group);
                if (visible == gViewModel.IsVisible) continue;
                gViewModel.SetVisible(visible, !initializing);
                visibilityChanged = true;
            }
            return visibilityChanged;
        }

        private void UpdateParamAvailability(IReadonlyContext @params)
        {
            var adapter = Adapter;
            if (adapter == null) return;
            foreach (var paramHolder in _paramRowViewModels.Values)
                paramHolder.ParameterViewModel.IsEnabled = adapter.IsEnabled(@params, paramHolder.Parameter);
        }

        private void ParameterViewModel_OnValueChanged(object sender, EventArgs e) => OnParamChanged((ParameterViewModel)sender);

        private void ParameterNameTextBlock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2 || !(sender is FrameworkElement element) || !(element.Tag is ParameterViewModel parameter)) return;
            if (MsgBoxUtils.QuestionYesNo($"Set param '{parameter.Name}' to default?", "Set to default", MessageBoxResult.No).IsYes())
                using (_updateLock.Ref())
                    parameter.SetDefault();
            OnParamChanged(parameter);
        }

    }

}
