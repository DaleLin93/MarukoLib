using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Windows;
using MarukoLib.Persistence;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;

namespace MarukoLib.Parametrization.Presenters
{

    public interface IParameterViewAccessor : IValueAccessor<object>
    {

        event EventHandler ValueChanged;

    }

    public interface IParameterViewStateHandler
    {

        bool IsEnabled { get; set; }

        bool IsValid { get; set; }

    }

    public interface IParameterViewAdapter : IParameterViewAccessor, IParameterViewStateHandler { }

    public class ControlStateHandler : IParameterViewStateHandler
    {

        [NotNull] private readonly FrameworkElement _element;

        [NotNull] private System.Windows.Media.Brush _defaultBackgroundBrush;

        [NotNull] private System.Windows.Media.Brush _invalidBackgroundBrush;

        public ControlStateHandler([NotNull] FrameworkElement element)
        {
            _element = element;
            _defaultBackgroundBrush = Brushes.Transparent;
            _invalidBackgroundBrush = element is Panel ? ViewConsts.LightInvalidColorBrush : ViewConsts.InvalidColorBrush;
        }

        public virtual bool IsEnabled
        {
            get => _element.IsEnabled;
            set => _element.IsEnabled = value;
        }

        public virtual bool IsValid
        {
            get => !ReferenceEquals(_element.GetValue(Panel.BackgroundProperty), _invalidBackgroundBrush);
            set => _element.SetValue(Panel.BackgroundProperty, value ? _defaultBackgroundBrush : _invalidBackgroundBrush);
        }

        [NotNull]
        public System.Windows.Media.Brush DefaultBackgroundBrush
        {
            get => _defaultBackgroundBrush;
            set
            {
                if (IsValid) _element.SetValue(Panel.BackgroundProperty, value);
                _defaultBackgroundBrush = value;
            }
        }

        [NotNull]
        public System.Windows.Media.Brush InvalidBackgroundBrush
        {
            get => _invalidBackgroundBrush;
            set
            {
                if (!IsValid) _element.SetValue(Panel.BackgroundProperty, value);
                _invalidBackgroundBrush = value;
            }
        }

    }
    
    public sealed class ParameterViewModel : IParameterViewAdapter
    {

        public event EventHandler ValueChanged;

        [NotNull] public readonly IParameterDescriptor Parameter;

        [NotNull] public readonly UIElement Element;

        [NotNull] private readonly IParameterViewAccessor _accessor;

        [CanBeNull] private readonly IParameterViewStateHandler _stateHandler;

        public ParameterViewModel([NotNull] IParameterDescriptor parameter, [NotNull] UIElement element, [NotNull] IParameterViewAdapter adapter)
            : this(parameter, element, adapter, adapter) { }

        public ParameterViewModel([NotNull] IParameterDescriptor parameter, [NotNull] UIElement element,
            [NotNull] IParameterViewAccessor accessor, [CanBeNull] Control control = null)
            : this(parameter, element, accessor, control == null ? null : new ControlStateHandler(control)) { }

        public ParameterViewModel([NotNull] IParameterDescriptor parameter, [NotNull] UIElement element,
            [NotNull] IParameterViewAccessor accessor, [CanBeNull] IParameterViewStateHandler stateHandler = null)
        {
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            Element = element ?? throw new ArgumentNullException(nameof(element));
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _stateHandler = stateHandler;
            _accessor.ValueChanged += Accessor_OnValueChanged;
        }

        public string Name => Parameter.Name;

        public bool IsEnabled
        {
            get => _stateHandler?.IsEnabled ?? true;
            set
            {
                if (_stateHandler == null) return;
                _stateHandler.IsEnabled = value;
            }
        }

        public bool IsValid
        {
            get => _stateHandler?.IsValid ?? true;
            set
            {
                if (_stateHandler == null) return;
                _stateHandler.IsValid = value;
            }
        }

        public object Value
        {
            get
            {
                object value;
                try
                {
                    value = _accessor.Value;
                    IsValid = true;
                }
                catch (Exception)
                {
                    IsValid = false;
                    throw;
                }

                return value;
            }
            set => _accessor.Value = value;
        }

        public void SetDefault() => _accessor.Value = Parameter.DefaultValue;

        private void Accessor_OnValueChanged(object sender, EventArgs e) => ValueChanged?.Invoke(this, e);

    }

    public interface IPresenter
    {

        [NotNull] ParameterViewModel Present([NotNull] IParameterDescriptor parameter);

    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class PresenterAttribute : Attribute
    {

        private static readonly IDictionary<Type, IPresenter> Presenters = new Dictionary<Type, IPresenter>();

        public PresenterAttribute(Type presenterType) => Presenter = Initiate(presenterType);

        [CanBeNull]
        public static PresenterAttribute GetFrom([NotNull] Type type)
        {
            while (type.IsNullableType(out var underlyingType))
                type = underlyingType;
            return type.GetCustomAttribute<PresenterAttribute>();
        }

        private static IPresenter Initiate(Type presenterType)
        {
            IPresenter presenter;
            lock (Presenters)
                if (!Presenters.TryGetValue(presenterType, out presenter))
                {
                    if (!typeof(IPresenter).IsAssignableFrom(presenterType)) 
                        throw new ArgumentException($"'{nameof(presenterType)}' must be a subclass of {nameof(IPresenter)}");
                    Presenters[presenterType] = presenter = (IPresenter) Activator.CreateInstance(presenterType);
                }
            return presenter;
        }

        public IPresenter Presenter { get; }

    }

    public static class Presenters
    {

        public const string NullPlaceholder = "{*NULL*}";

        [CanBeNull]
        public delegate IPresenter PresenterSelector([NotNull] IParameterDescriptor parameter);

        public static ContextProperty<IPresenter> PresenterProperty = new ContextProperty<IPresenter>();

        private static readonly IDictionary<Type, IPresenter> TypePresenters = new Dictionary<Type, IPresenter>
        {
            {typeof(string), PlainTextPresenter.Instance},
            {typeof(bool), BooleanPresenter.Instance},
            {typeof(Color), ColorPresenter.Instance},
            {typeof(System.Drawing.Color), ColorPresenter.Instance},
            {typeof(DateTime), DateTimePresenter.Instance},
            //{typeof(MarkerDefinition), MarkerDefinitionPresenter.Instance},

            {typeof(Uri), UriPresenter.Instance},
            {typeof(IPAddress), EndpointPresenter.Instance},
            {typeof(IPEndPoint), EndpointPresenter.Instance},

            {typeof(Position1D), PositionPresenter.Instance},
            {typeof(PositionH1D), PositionPresenter.Instance},
            {typeof(PositionV1D), PositionPresenter.Instance},
            {typeof(Position2D), PositionPresenter.Instance},

        };

        static Presenters()
        {
            var pointTypes = new[] {typeof(System.Drawing.Point), typeof(PointF), typeof(Point)};
            var sizeTypes = new[] {typeof(System.Drawing.Size), typeof(SizeF), typeof(Size)};
            var rectTypes = new[] {typeof(System.Drawing.Rectangle), typeof(RectangleF), typeof(Rect)};
            foreach (var pointType in pointTypes)
            {
                PlainOrdinaryStructPresenter.SetTargetMembers(pointType, new[] {"X", "Y"});
                TypePresenters[pointType] = PlainOrdinaryStructPresenter.Instance;
            }
            foreach (var sizeType in sizeTypes)
            {
                PlainOrdinaryStructPresenter.SetTargetMembers(sizeType, new[] {"Width", "Height"});
                TypePresenters[sizeType] = PlainOrdinaryStructPresenter.Instance;
            }
            foreach (var rectType in rectTypes)
            {
                PlainOrdinaryStructPresenter.SetTargetMembers(rectType, new[] {"X", "Y", "Width", "Height"});
                TypePresenters[rectType] = PlainOrdinaryStructPresenter.Instance;
            }
        }

        private static readonly IList<PresenterSelector> InternalPresenterSelectors = new List<PresenterSelector>
        {
            parameter => PresenterProperty.TryGet(parameter.Metadata, out var presenter) ? presenter : null,
            parameter => PresenterAttribute.GetFrom(parameter.ValueType)?.Presenter,
            ConditionalPresenterSelector(TypeConvertedPresenter.HasTypeConverter, TypeConvertedPresenter.Instance),
            ConditionalPresenterSelector(ParameterDescriptorExt.IsSelectable, SelectablePresenter.Instance),
            ConditionalPresenterSelector(ParameterDescriptorExt.IsMultiValue, MultiValuePresenter.Instance),
            parameter => GetPresenterByType(parameter.ValueType),
            parameter => PlainTextPresenter.Instance
        };

        private static readonly LinkedList<PresenterSelector> CustomPresenterSelectors = new LinkedList<PresenterSelector>();

        public static void SetTypePresenter([NotNull] Type type, [CanBeNull] IPresenter presenter)
        {
            if (presenter == null)
                TypePresenters.Remove(type);
            else
                TypePresenters[type] = presenter;
        }

        public static void RegisterPresenterSelector([NotNull] PresenterSelector selector) => CustomPresenterSelectors.AddFirst(selector);

        public static void UnregisterPresenterSelector([NotNull] PresenterSelector selector) => CustomPresenterSelectors.Remove(selector);
        
        public static T SetPresenter<T>([NotNull] this T contextBuilder, [NotNull] IPresenter presenter, bool @override = false) where T : IContextBuilder
        {
            if (!@override && contextBuilder.TryGet(PresenterProperty, out var p) && p != presenter)
                throw new ProgrammingException("Presenter is already set.");
            contextBuilder.Set(PresenterProperty, presenter);
            return contextBuilder;
        }

        [NotNull]
        public static ParameterViewModel Present([NotNull] this IParameterDescriptor param) => GetPresenter(param).Present(param);

        [NotNull, SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
        public static IPresenter GetPresenter([NotNull] this IParameterDescriptor param)
        {
            IPresenter presenter;
            foreach (var selectors in new []{(IEnumerable<PresenterSelector>)InternalPresenterSelectors, CustomPresenterSelectors})
                foreach (var selector in selectors)
                    if ((presenter = selector(param)) != null)
                        return presenter;
            throw new NotSupportedException($"presenter not found for type '{param.ValueType}'");
        }

        [CanBeNull, SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
        public static IPresenter GetPresenterByType([NotNull] this Type type)
        {
            if (type.IsNullableType(out var underlyingType)) type = underlyingType;
            if (TypePresenters.TryGetValue(type, out var presenter)) return presenter;
            if (type.IsPrimitive || type.IsEnum) return PlainTextPresenter.Instance;
            if (typeof(IParameterizedObject).IsAssignableFrom(type)) return ParameterizedObjectPresenter.Instance;
            return null;
        }

        [CanBeNull]
        public static object ParseValueFromString([NotNull] this IParameterDescriptor parameter, [NotNull] string strVal) 
            => Equals(NullPlaceholder, strVal) ? null : ParseValueFromString(parameter.ValueType, strVal);

        [CanBeNull, SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public static object ParseValueFromString([NotNull] Type type, [NotNull] string strVal)
        {
            if (Equals(NullPlaceholder, strVal)) return null;
            if (type.IsArray)
                if (type.GetArrayRank() == 1)
                {
                    strVal = strVal.Trim();
                    var substrings = strVal.Split(' ').Where(str => !str.IsBlank()).ToArray();
                    var array = Array.CreateInstance(type.GetElementType(), substrings.Length);
                    for (var i = 0; i < substrings.Length; i++)
                        array.SetValue(ParseValueFromString(type.GetElementType(), substrings[i]), i);
                    return array;
                }
                else
                    throw new NotSupportedException("Only 1D-array was supported");

            if (type == typeof(string)) return strVal;

            var nullableType = type.IsNullableType(out var underlyingType);
            var actualType = nullableType ? underlyingType : type;

            if (actualType.IsEnum)
            {
                var enumValues = Enum.GetValues(actualType);
                foreach (var enumValue in enumValues)
                    if (Equals(enumValue.ToString(), strVal))
                        return enumValue;
                throw new ArgumentException($"{actualType.Name} value not found by name: '{strVal}'");
            }

            if (strVal.IsEmpty())
                if (nullableType)
                    return null;
                else
                    throw new ArgumentException($"Cannot convert empty string to type: {type.FullName}");

            if (actualType == typeof(char)) return strVal[0];
            if (actualType == typeof(byte)) return byte.Parse(strVal);
            if (actualType == typeof(sbyte)) return sbyte.Parse(strVal);
            if (actualType == typeof(short)) return short.Parse(strVal);
            if (actualType == typeof(ushort)) return ushort.Parse(strVal);
            if (actualType == typeof(int)) return int.Parse(strVal);
            if (actualType == typeof(uint)) return uint.Parse(strVal);
            if (actualType == typeof(ulong)) return ulong.Parse(strVal);
            if (actualType == typeof(float)) return float.Parse(strVal);
            if (actualType == typeof(double)) return double.Parse(strVal);
            if (actualType == typeof(decimal)) return decimal.Parse(strVal);
            throw new ArgumentException($"Type is not supported, type: {type.FullName}");
        }

        [NotNull]
        public static string ConvertToString([CanBeNull] this object value) 
            => value == null ? NullPlaceholder : ConvertValueToString(value.GetType(), value);

        [NotNull]
        public static string ConvertValueToString([NotNull] this IParameterDescriptor parameter, [CanBeNull] object val) 
            => val == null ? NullPlaceholder : ConvertValueToString(parameter.ValueType, val);

        [NotNull]
        public static string ConvertValueToString([NotNull] this Type type, [NotNull] object value)
        {
            if (type.IsArray)
            {
                if (type.GetArrayRank() == 1 && (type.GetElementType()?.IsPrimitive ?? false))
                {
                    var stringBuilder = new StringBuilder();
                    var array = (Array)value;
                    for (var i = 1; i <= array.Length; i++)
                    {
                        stringBuilder.Append(array.GetValue(i - 1));
                        if (i != array.Length) stringBuilder.Append(' ');
                    }
                    return stringBuilder.ToString();
                }
                throw new NotSupportedException();
            }
            if (value is IDescribable describable) return describable.GetShortDescription();
            return value.ToString();
        }

        private static PresenterSelector ConditionalPresenterSelector([NotNull] Predicate<IParameterDescriptor> predicate, [NotNull] IPresenter presenter) 
            => parameter => predicate(parameter) ? presenter : null;

    }

}
