using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Windows;

namespace MarukoLib.Parametrization.Presenters
{

    public class OptionalPresenter : IPresenter
    {

        private class OptionalAccessor
        {

            private static readonly IDictionary<Type, OptionalAccessor> Accessors = new Dictionary<Type, OptionalAccessor>();

            [NotNull] internal readonly Type Type;

            [NotNull] internal readonly Type ValueType;

            [NotNull] internal readonly ConstructorInfo Constructor;

            [NotNull] internal readonly PropertyInfo HasValueProperty, ValueProperty;

            public OptionalAccessor(Type optionalType)
            {
                Type = optionalType ?? throw new ArgumentNullException(nameof(optionalType));
                ValueType = optionalType.GetGenericType(typeof(Optional<>));
                Constructor = optionalType.GetConstructor(new[] { typeof(bool), ValueType }) ?? throw new Exception("cannot found optional constructor");
                HasValueProperty = optionalType.GetProperty(nameof(Optional<object>.HasValue)) ?? throw new Exception("cannot found 'HasValue' property");
                ValueProperty = optionalType.GetProperty(nameof(Optional<object>.Value)) ?? throw new Exception("cannot found 'Value' property");
            }

            public static OptionalAccessor OfType(Type type)
            {
                lock (Accessors)
                {
                    if (Accessors.TryGetValue(type, out var accessor)) return accessor;
                    return Accessors[type] = new OptionalAccessor(type);
                }
            }

            public object CreateInstance(bool has, object value) => Constructor.Invoke(new[] { has, value });

            public bool TryReadValue(object optional, out bool has, out object value)
            {
                if (Type.IsInstanceOfType(optional))
                {
                    has = (bool)HasValueProperty.GetValue(optional);
                    value = ValueProperty.GetValue(optional);
                    return true;
                }
                has = false;
                value = null;
                return false;
            }

        }

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly OptionalAccessor _accessor;

            [NotNull] private readonly Grid _container;

            [NotNull] private readonly CheckBox _checkBox;

            [NotNull] private readonly ParameterViewModel _wrapped;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] OptionalAccessor accessor,
                [NotNull] Grid container, [NotNull] CheckBox checkBox, [NotNull] ParameterViewModel wrapped)
            {
                _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
                _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
                _container = container ?? throw new ArgumentNullException(nameof(container));
                _checkBox = checkBox ?? throw new ArgumentNullException(nameof(checkBox));
                _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));

                _checkBox.Checked += CheckBox_OnValueChanged;
                _checkBox.Unchecked += CheckBox_OnValueChanged;
                _wrapped.ValueChanged += WrappedParameter_OnValueChanged;
            }

            public bool IsEnabled
            {
                get => _container.IsEnabled;
                set => _container.IsEnabled = value;
            }

            public bool IsValid
            {
                get => _wrapped.IsValid;
                set => _wrapped.IsValid = value;
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(_accessor.CreateInstance(_checkBox.IsChecked ?? false, _wrapped.Value));
                set
                {
                    if (_accessor.TryReadValue(value, out var hasValue, out var optionalValue))
                    {
                        _checkBox.IsChecked = hasValue;
                        _wrapped.Value = optionalValue;
                    }
                }
            }

            private void CheckBox_OnValueChanged(object sender, RoutedEventArgs e)
            {
                _wrapped.Element.IsEnabled = ((CheckBox)sender).IsChecked ?? false;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }

            private void WrappedParameter_OnValueChanged(object sender, EventArgs e) => ValueChanged?.Invoke(this, e);

        }

        public static readonly NamedProperty<object> CheckBoxContentProperty = new NamedProperty<object>("CheckBoxContent");

        public static readonly NamedProperty<IReadonlyContext> ValueTypePresentingContextProperty = new NamedProperty<IReadonlyContext>("ValueTypePresentingContext", EmptyContext.Instance);

        public static readonly OptionalPresenter Instance = new OptionalPresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var accessor = OptionalAccessor.OfType(param.ValueType);
            var valueTypeContext = ValueTypePresentingContextProperty.Get(param.Metadata);
            var valueTypeParam = new MetadataOverridenParameter(param, accessor.ValueType, valueTypeContext);
            var wrapped = valueTypeParam.Present();

            var container = new Grid();
            container.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
            container.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.MinorSpacingGridLength});
            container.ColumnDefinitions.Add(new ColumnDefinition {Width = ViewConsts.Star1GridLength});
            var checkbox = new CheckBox {IsChecked = true, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center};
            if (CheckBoxContentProperty.TryGet(param.Metadata, out var checkBoxContent)) checkbox.Content = checkBoxContent;

            container.Children.Add(checkbox);
            Grid.SetColumn(checkbox, 0);
            container.Children.Add(wrapped.Element);
            Grid.SetColumn(wrapped.Element, 2);
            return new ParameterViewModel(param, container, new Adapter(param, accessor, container, checkbox, wrapped));
        }
    }
}