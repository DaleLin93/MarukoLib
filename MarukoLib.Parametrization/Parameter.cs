using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Lang.Exceptions;
using MarukoLib.Logging;
using MarukoLib.Parametrization.Data;
using MarukoLib.Parametrization.Presenters;
using MarukoLib.Persistence;

namespace MarukoLib.Parametrization
{

    public interface IDescriptor 
    {

        [CanBeNull] string Name { get; }

        [CanBeNull] string Description { get; }

    }

    public interface IParameterDescriptor : IContextProperty, IDescriptor, IRegistrable
    {

        [CanBeNull] string Unit { get; }

        [CanBeNull] object DefaultValue { get; }

        [CanBeNull] IEnumerable SelectableValues { get; }

        [NotNull] IReadonlyContext Metadata { get; }

        bool IsValid([CanBeNull] object value);

    }

    public interface IGroupDescriptor : IDescriptor
    {

        [NotNull] IReadOnlyCollection<IDescriptor> Items { get; }

    }

    public interface IRoutedParameter : IParameterDescriptor
    {

        [NotNull] IParameterDescriptor OriginalParameter { get; }

    }

    public sealed class ParameterGroup : IGroupDescriptor, IReadOnlyCollection<IDescriptor>
    {

        public ParameterGroup([NotNull] params IDescriptor[] items)
            : this(null, null, (IReadOnlyCollection<IDescriptor>)items) { }

        public ParameterGroup([NotNull] IReadOnlyCollection<IDescriptor> items)
            : this(null, null, items) { }

        public ParameterGroup([CanBeNull] string name, [NotNull] params IDescriptor[] items)
            : this(name, null, (IReadOnlyCollection<IDescriptor>)items) { }

        public ParameterGroup([CanBeNull] string name, [NotNull] IReadOnlyCollection<IDescriptor> items)
            : this(name, null, items) { }

        public ParameterGroup([CanBeNull] string name, [CanBeNull] string description, [NotNull] params IDescriptor[] items)
            : this(name, description, (IReadOnlyCollection<IDescriptor>)items) { }

        public ParameterGroup([CanBeNull] string name, [CanBeNull] string description, [NotNull] IReadOnlyCollection<IDescriptor> items)
        {
            Name = name;
            Description = description;
            Items = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyCollection<IDescriptor> Items { get; }

        public int Count => Items.Count;

        public bool IsEmpty => Count <= 0;

        public IEnumerator<IDescriptor> GetEnumerator() => Items.GetEnumerator();

        public override bool Equals(object obj)
        {
            if (null == obj) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ParameterGroup other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Items.GetHashCode();
            }
        }

        public override string ToString() => Name ?? "<ABSENT>";

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool Equals(IGroupDescriptor other) => string.Equals(Name, other.Name) && Equals(Items, other.Items);

    }

    public sealed class ParameterGroupCollection : IReadOnlyCollection<IGroupDescriptor>, IReadOnlyCollection<IDescriptor>
    {

        private readonly LinkedList<IGroupDescriptor> _groups = new LinkedList<IGroupDescriptor>();

        public int Count => _groups.Count;

        public IEnumerator<IGroupDescriptor> GetEnumerator() => _groups.GetEnumerator();

        public ParameterGroupCollection Add([NotNull] params IDescriptor[] descriptors) => Add(null, descriptors);

        public ParameterGroupCollection Add([CanBeNull] string groupName, [NotNull] params IDescriptor[] descriptors) => Add(groupName, null, descriptors);

        public ParameterGroupCollection Add([CanBeNull] string groupName, [CanBeNull] string groupDescription, [NotNull] params IDescriptor[] descriptors)
        {
            _groups.AddLast(new ParameterGroup(groupName, groupDescription, descriptors));
            return this;
        }

        IEnumerator<IDescriptor> IEnumerable<IDescriptor>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

    public abstract class OverridenParameter : IRoutedParameter
    {

        protected OverridenParameter(IParameterDescriptor parameter) => OriginalParameter = parameter ?? throw new ArgumentNullException(nameof(parameter));

        public IParameterDescriptor OriginalParameter { get; }

        public abstract Type ValueType { get; }

        public abstract string Id { get; }

        public abstract string Name { get; }

        public abstract string Unit { get; }

        public abstract string Description { get; }

        public abstract bool IsNullable { get; }

        public abstract object DefaultValue { get; }

        public abstract IEnumerable SelectableValues { get; }

        public abstract IReadonlyContext Metadata { get; }

        public abstract bool IsValid(object value);

        public sealed override bool Equals(object obj) => ReferenceEquals(this, obj);

        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        public sealed override int GetHashCode() => base.GetHashCode();

    }

    public sealed class MetadataOverridenParameter : OverridenParameter
    {

        public const string Keep = "{K-E-E-P}";

        public static readonly NamedProperty<string> NameProperty = new NamedProperty<string>("Name");

        public static readonly NamedProperty<string> UnitProperty = new NamedProperty<string>("Unit", null, true);

        public static readonly NamedProperty<string> DescriptionProperty = new NamedProperty<string>("Description", null, true);

        public static readonly NamedProperty<Type> ValueTypeProperty = new NamedProperty<Type>("ValueType");

        public static readonly NamedProperty<bool> IsNullableProperty = new NamedProperty<bool>("IsNullable");

        public static readonly NamedProperty<object> DefaultValueProperty = new NamedProperty<object>("DefaultValue", null, true);

        public static readonly NamedProperty<IEnumerable> SelectableValuesProperty = new NamedProperty<IEnumerable>("SelectableValues", null, true);

        public MetadataOverridenParameter([NotNull] IParameterDescriptor originalParameter,
            [CanBeNull] string name, [CanBeNull] string unit = Keep, [CanBeNull] string description = Keep,
            [CanBeNull] IReadonlyContext metadata = null) : base(originalParameter)
        {
            var context = new Context(4);
            if (name == null || !Equals(unit, Keep)) context[NameProperty] = name;
            if (!Equals(unit, Keep)) context[UnitProperty] = unit;
            if (!Equals(description, Keep)) context[DescriptionProperty] = description;
            Metadata = metadata == null ? (IReadonlyContext)context : new CompositeReadonlyContext(context, metadata);
        }

        public MetadataOverridenParameter([NotNull] IParameterDescriptor originalParameter, [NotNull] Type valueType, [CanBeNull] IReadonlyContext metadata)
            : this(originalParameter, valueType, TypeUtils.Default(valueType), valueType.IsEnum ? Enum.GetValues(valueType) : null, metadata) { }

        public MetadataOverridenParameter([NotNull] IParameterDescriptor originalParameter, [NotNull] Type valueType,
            [CanBeNull] object defaultValue, [CanBeNull] IEnumerable selectableValues, [CanBeNull] IReadonlyContext metadata)
            : base(originalParameter)
        {
            var context = new Context(4)
            {
                [ValueTypeProperty] = valueType,
                [DefaultValueProperty] = defaultValue,
                [SelectableValuesProperty] = selectableValues
            };
            Metadata = metadata == null ? (IReadonlyContext) context : new CompositeReadonlyContext(context, metadata);
        }

        public MetadataOverridenParameter([NotNull] IParameterDescriptor originalParameter, [NotNull] IReadonlyContext metadata)
            : base(originalParameter) => Metadata = metadata;

        public override string Id => OriginalParameter.Id;

        public override string Name => NameProperty.TryGet(Metadata, out var name) ? name : OriginalParameter.Name;

        public override string Unit => UnitProperty.TryGet(Metadata, out var unit) ? unit : OriginalParameter.Unit;

        public override string Description => DescriptionProperty.TryGet(Metadata, out var description) ? description : OriginalParameter.Description;

        public override Type ValueType => ValueTypeProperty.TryGet(Metadata, out var valueType) ? valueType : OriginalParameter.ValueType;

        public override bool IsNullable => IsNullableProperty.TryGet(Metadata, out var isNullable) ? isNullable : OriginalParameter.IsNullable;

        public override object DefaultValue => DefaultValueProperty.TryGet(Metadata, out var defaultValue) ? defaultValue : OriginalParameter.DefaultValue;

        public override IEnumerable SelectableValues => SelectableValuesProperty.TryGet(Metadata, out var selectableValues) ? selectableValues : OriginalParameter.SelectableValues;

        public override IReadonlyContext Metadata { get; }

        public override bool IsValid(object val) => IsNullable && val == null || ValueType.IsInstanceOfType(val);

    }

    public sealed class Parameter<T> : ContextProperty<T>, IParameterDescriptor
    {

        public sealed class Builder : IContextBuilder
        {

            public readonly ContextBuilder MetadataBuilder = new ContextBuilder();

            [NotNull] public string Id;

            [CanBeNull] public string Name;

            [CanBeNull] public string Unit;

            [CanBeNull] public string Description;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public bool Nullable = typeof(T).IsNullableType();

            [CanBeNull] public Supplier<T> DefaultValueSupplier;

            [CanBeNull] public Supplier<IEnumerable<T>> SelectableValuesSupplier;

            [CanBeNull] public Predicate<T> Validator;

            public Builder([NotNull] string name) : this(ParameterUtils.GenerateIdByName(name), name) { }

            public Builder([NotNull] string id, [CanBeNull] string name)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Name = name;
            }

            public Builder SetId([NotNull] string identifier)
            {
                Id = identifier;
                return this;
            }

            public Builder SetName([CanBeNull] string name)
            {
                Name = name;
                return this;
            }

            public Builder SetUnit([CanBeNull] string unit)
            {
                Unit = unit;
                return this;
            }

            public Builder SetDescription([CanBeNull] string description)
            {
                Description = description;
                return this;
            }

            public Builder SetNullable(bool nullable)
            {
                Nullable = nullable;
                return this;
            }

            public Builder SetDefaultValue(T defaultValue)
            {
                DefaultValueSupplier = () => defaultValue;
                return this;
            }

            public Builder SetDefaultValue([CanBeNull] Supplier<T> defaultValueSupplier)
            {
                DefaultValueSupplier = defaultValueSupplier;
                return this;
            }

            [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
            public Builder SetSelectableValues([CanBeNull] IEnumerable<T> selectableValues, bool setFirstAsDefault = false)
            {
                SelectableValuesSupplier = selectableValues == null ? (Supplier<IEnumerable<T>>)null : () => selectableValues;
                if (setFirstAsDefault) SetDefaultValue(selectableValues == null ? default : selectableValues.FirstOrDefault());
                return this;
            }

            [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
            public Builder SetSelectableValues([CanBeNull] Supplier<IEnumerable<T>> selectableValuesSupplier, bool setFirstAsDefault = false)
            {
                SelectableValuesSupplier = selectableValuesSupplier;
                if (setFirstAsDefault)
                {
                    if (selectableValuesSupplier == null)
                        SetDefaultValue(() => default);
                    else
                        SetDefaultValue(() => selectableValuesSupplier().FirstOrDefault());
                }
                return this;
            }

            public Builder SetValidator([CanBeNull] Predicate<T> validator)
            {
                Validator = validator;
                return this;
            }

            public Builder SetMetadata([CanBeNull] IReadonlyContext metadata)
            {
                MetadataBuilder.Clear();
                if (metadata != null)
                    MetadataBuilder.SetProperties(metadata);
                return this;
            }

            public Builder SetRawMetadata([NotNull] IContextProperty property, object value)
            {
                MetadataBuilder.Set(property, value);
                return this;
            }

            public Builder SetMetadata<TP>([NotNull] ContextProperty<TP> property, TP value)
            {
                MetadataBuilder.Set(property, value);
                return this;
            }

            public Builder SetMetadata([NotNull] Action<ContextBuilder> action)
            {
                action(MetadataBuilder);
                return this;
            }

            public Builder Config([NotNull] Action<Builder> action)
            {
                action(this);
                return this;
            }

            public Parameter<T> Build() => new Parameter<T>(this);

            int IContextBuilder.Count => MetadataBuilder.Count;

            bool IContextBuilder.TryGet(IContextProperty property, out object value) => MetadataBuilder.TryGet(property, out value);

            IContextBuilder IContextBuilder.Set(IContextProperty property, object value) => MetadataBuilder.Set(property, value);

            IContextBuilder IContextBuilder.Delete(IContextProperty property) => MetadataBuilder.Delete(property);

            IContextBuilder IContextBuilder.Clear() => MetadataBuilder.Clear();

            IContext IContextBuilder.Build() => MetadataBuilder.Build();

        }

        public Parameter(string name, T defaultValue = default, IEnumerable<T> selectableValues = null)
            : this(ParameterUtils.GenerateIdByName(name), name, null, null, defaultValue, selectableValues) { }

        public Parameter(string name, string description, T defaultValue = default, IEnumerable<T> selectableValues = null)
            : this(ParameterUtils.GenerateIdByName(name), name, null, description, defaultValue, selectableValues) { }

        public Parameter(string name, string unit, string description, T defaultValue = default, IEnumerable<T> selectableValues = null)
            : this(ParameterUtils.GenerateIdByName(name), name, unit, description, defaultValue, selectableValues) { }

        public Parameter(string id, string name, string unit, string description, T defaultValue = default, IEnumerable<T> selectableValues = null)
            : this(CreateBuilderWithId(id, name, defaultValue).SetUnit(unit).SetDescription(description).SetSelectableValues(selectableValues, false)) { }

        public Parameter(string name, Predicate<T> validator, T defaultValue = default)
            : this(ParameterUtils.GenerateIdByName(name), name, null, null, validator, defaultValue) { }

        public Parameter(string name, string unit, string description, Predicate<T> validator, T defaultValue = default)
            : this(ParameterUtils.GenerateIdByName(name), name, unit, description, validator, defaultValue) { }

        public Parameter(string id, string name, string unit, string description, Predicate<T> validator, T defaultValue = default)
            : this(CreateBuilderWithId(id, name, defaultValue).SetUnit(unit).SetDescription(description).SetValidator(validator)) { }

        private Parameter(Builder builder) : base(null, builder.Nullable)
        {
            Id = builder.Id.ToCamelCase();
            Name = builder.Name;
            Unit = builder.Unit;
            Description = builder.Description;
            DefaultValueSupplier = builder.DefaultValueSupplier;
            SelectableValuesSupplier = builder.SelectableValuesSupplier;
            Validator = builder.Validator;
            Metadata = builder.MetadataBuilder.BuildReadonly();

            var defaultValue = DefaultValue;
            if (defaultValue != null && !IsValid(defaultValue)) 
                throw new ProgrammingException($"Invalid default value: parameter {Id}");
        }

        public static Builder CreateBuilder(string name, T defaultValue = default) => new Builder(name).SetDefaultValue(defaultValue);

        public static Builder CreateBuilderWithId(string id, string name, T defaultValue = default) => new Builder(id, name).SetDefaultValue(defaultValue);

        public static Parameter<T> OfEnum(string name, T defaultValue) => OfEnum(ParameterUtils.GenerateIdByName(name), name, null, null, defaultValue);

        public static Parameter<T> OfEnum(string name, string unit = null, string description = null) => OfEnum(ParameterUtils.GenerateIdByName(name), name, unit, description);

        public static Parameter<T> OfEnum(string id, string name, string unit, string description)
        {
            var values = EnumUtils.GetEnumValues<T>();
            if (values.Length == 0) throw new ArgumentException("enum type has no values");
            return new Parameter<T>(id, name, unit, description, values[0], values);
        }

        public static Parameter<T> OfEnum(string id, string name, string unit, string description, T defaultValue)
        {
            var values = EnumUtils.GetEnumValues<T>();
            if (values.Length == 0)
                throw new ArgumentException("enum type has no values");
            return new Parameter<T>(id, name, unit, description, defaultValue, values);
        }

        public string Id { get; }

        public string Name { get; }

        public string Unit { get; }

        public string Description { get; }

        public override bool HasDefaultValue => true;

        [CanBeNull] public override T DefaultValue => DefaultValueSupplier == null ? default : DefaultValueSupplier();

        [CanBeNull] public IEnumerable<T> SelectableValues => SelectableValuesSupplier?.Invoke();

        [CanBeNull] public Predicate<T> Validator { get; }

        public IReadonlyContext Metadata { get; }

        [CanBeNull] private Supplier<T> DefaultValueSupplier { get; }

        [CanBeNull] private Supplier<IEnumerable<T>> SelectableValuesSupplier { get; }

        public bool IsValid(object val)
        {
            if (IsNullable && val == null || val is T)
                return Validator?.Invoke((T) val) ?? true;
            return false;
        }

        public TOut Get<TOut>(IReadonlyContext context, Func<T, TOut> mappingFunc) => mappingFunc(Get(context));

        public override string ToString() => Id;

        object IParameterDescriptor.DefaultValue => DefaultValue;

        IEnumerable IParameterDescriptor.SelectableValues => SelectableValues;

    }

    #region Auto Parameterization

    /// <summary>
    /// The adapter for auto parameter which Will be used to verify the availability of parameters.
    /// </summary>
    public interface IAutoParamAdapter
    {

        bool IsValid(MemberInfo member, object value);

    }

    /// <summary>
    /// The field or property attribute to declare an auto parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AutoParamAttribute : Attribute
    {

        private static readonly IDictionary<Type, IAutoParamAdapter> Adapters = new Dictionary<Type, IAutoParamAdapter>();

        public AutoParamAttribute() { }

        public AutoParamAttribute([CanBeNull] string name) => Name = name;

        public AutoParamAttribute([CanBeNull] string id, [CanBeNull] string name) : this(id, name, null) { }

        public AutoParamAttribute([CanBeNull] string id, [CanBeNull] string name, [CanBeNull] string group)
        {
            Id = id;
            Name = name;
            Group = group;
        }

        [CanBeNull]
        private static IAutoParamAdapter GetAdapter([CanBeNull] Type adapterType)
        {
            if (adapterType == null) return null;
            if (Adapters.TryGetValue(adapterType, out var adapter)) return adapter;
            return Adapters[adapterType] = (IAutoParamAdapter)Activator.CreateInstance(adapterType);
        }

        [CanBeNull] public string Id { get; set; }

        [CanBeNull] public string Name { get; set; }

        [CanBeNull] public string Group { get; set; }

        [CanBeNull] public string Unit { get; set; }

        [CanBeNull] public string Desc { get; set; }

        [CanBeNull] public Type AdapterType { get; set; }

        [CanBeNull] public IAutoParamAdapter Adapter => GetAdapter(AdapterType);

    }

    public abstract class AutoParameter : IParameterDescriptor
    {

        public class Field : AutoParameter
        {

            private readonly FieldInfo _field;

            public Field([NotNull] FieldInfo field, [NotNull] AutoParamAttribute attribute)
                : this(field, attribute, Activator.CreateInstance(field.FieldType)) { }

            public Field([NotNull] FieldInfo field, [NotNull] AutoParamAttribute attribute, [CanBeNull] object defaultValue)
                : this(field, attribute, defaultValue, field.FieldType.IsEnum ? Enum.GetValues(field.FieldType) : null) { }

            public Field([NotNull] FieldInfo field, [NotNull] AutoParamAttribute attribute,
                [CanBeNull] object defaultValue, [CanBeNull] IEnumerable selectableValues)
                : base(field, field.FieldType, attribute, defaultValue, selectableValues) => _field = field;

            internal override object GetMemberValue(object instance) => _field.GetValue(instance);

            internal override void SetMemberValue(object instance, object value) => _field.SetValue(instance, value);

        }

        public class Property : AutoParameter
        {

            private readonly PropertyInfo _property;

            public Property([NotNull] PropertyInfo property, [NotNull] AutoParamAttribute attribute)
                : this(property, attribute, Activator.CreateInstance(property.PropertyType)) { }

            public Property([NotNull] PropertyInfo property, [NotNull] AutoParamAttribute attribute, [CanBeNull] object defaultValue)
                : this(property, attribute, defaultValue, property.PropertyType.IsEnum ? Enum.GetValues(property.PropertyType) : null) { }

            public Property([NotNull] PropertyInfo property, [NotNull] AutoParamAttribute attribute,
                [CanBeNull] object defaultValue, [CanBeNull] IEnumerable selectableValues) 
                : base(property, property.PropertyType, attribute, defaultValue, selectableValues) => _property = property;

            internal override object GetMemberValue(object instance) => _property.GetValue(instance);

            internal override void SetMemberValue(object instance, object value) => _property.SetValue(instance, value);

        }

        protected AutoParameter([NotNull] MemberInfo member, [NotNull] Type type, [NotNull] AutoParamAttribute attribute,
            [CanBeNull] object defaultValue, [CanBeNull] IEnumerable selectableValues)
        {
            Member = member;
            ValueType = type;
            Attribute = attribute;
            DefaultValue = defaultValue;
            SelectableValues = selectableValues;
        }

        [NotNull] public MemberInfo Member { get; }

        [NotNull] public AutoParamAttribute Attribute { get; }

        [NotNull] public string Name => Attribute.Name ?? Member.Name;

        [CanBeNull] public string Group => Attribute.Group;

        public string Id => Attribute.Id ?? ParameterUtils.GenerateIdByName(Name);

        public string Unit => Attribute.Unit;

        public string Description => Attribute.Desc;

        public Type ValueType { get; }

        public bool IsNullable => ValueType.IsNullableType();

        public object DefaultValue { get; }

        public IEnumerable SelectableValues { get; }

        public IReadonlyContext Metadata { get; set; } = EmptyContext.Instance;

        public bool IsValid(object value) => IsNullable && value == null || ValueType.IsInstanceOfType(value) && (Attribute.Adapter?.IsValid(Member, value) ?? true);

        public override string ToString() => Id;

        internal abstract object GetMemberValue(object instance);

        internal abstract void SetMemberValue(object instance, object value);

    }

    #endregion

    public static class ParameterDescriptorExt
    {

        public static IParameterDescriptor GetOriginalParameter(this IParameterDescriptor parameter, bool recursively = true)
        {
            var param = parameter;
            while (param is IRoutedParameter routedParameter)
            {
                param = routedParameter.OriginalParameter;
                if (!recursively) return param;
            }
            return param;
        }

        public static object IsValidOrThrow(this IParameterDescriptor parameter, object value)
        {
            if(!parameter.IsValid(value)) throw new ArgumentException($"Value is invalid, parameter: {parameter.Name}, value: {value}");
            return value;
        }

        public static bool IsSelectable(this IParameterDescriptor parameter) => parameter.SelectableValues != null;

        public static bool IsMultiValue(this IParameterDescriptor parameter) => parameter.ValueType.IsArray && parameter.ValueType.GetArrayRank() == 1;

    }

    public static class ParameterGroupExt
    {

        public static IReadOnlyCollection<IParameterDescriptor> GetParameters(this IGroupDescriptor group) => group.Items.OfType<IParameterDescriptor>().ToList();

        public static IReadOnlyCollection<IGroupDescriptor> GetGroups(this IGroupDescriptor group) => group.Items.OfType<IGroupDescriptor>().ToList();

        public static IEnumerable<IParameterDescriptor> GetAllParameters(this IEnumerable<IDescriptor> descriptors) => descriptors.SelectMany(GetAllParameters);

        public static IEnumerable<IParameterDescriptor> GetAllParameters(this IDescriptor descriptor)
        {
            switch (descriptor)
            {
                case IParameterDescriptor parameter:
                    yield return parameter;
                    break;
                case IGroupDescriptor group:
                {
                    foreach (var child in group.Items)
                    foreach (var p in GetAllParameters(child))
                        yield return p;
                    break;
                }
            }
        }

        public static IEnumerable<IGroupDescriptor> GetAllGroups(this IEnumerable<IDescriptor> descriptors) =>
            descriptors.SelectMany(descriptor => GetAllGroups(descriptor, true));

        public static IEnumerable<IGroupDescriptor> GetAllGroups(this IDescriptor descriptor, bool includeSelf = true)
        {
            if (descriptor is IGroupDescriptor group)
            {
                if (includeSelf) yield return group;
                foreach (var child in GetGroups(group))
                foreach (var childGroup in GetAllGroups(child, false))
                    yield return childGroup;
            }
        }

    }

    public static class ParameterPersistenceExt
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ParameterPersistenceExt));

        public static ContextProperty<ITypeConverter> PersistentTypeConverterProperty = new ContextProperty<ITypeConverter>();

        public static T SetPersistentTypeConverter<T>([NotNull] this T contextBuilder, [NotNull] ITypeConverter converter) where T : IContextBuilder
        {
            contextBuilder.Set(PersistentTypeConverterProperty, converter);
            return contextBuilder;
        }

        public static bool TryGetPersistentTypeConverter([NotNull] this IParameterDescriptor parameter, out ITypeConverter converter)
        {
            var flag = PersistentTypeConverterProperty.TryGet(parameter.Metadata, out converter) && converter != null;
            if (!flag) return false;
            try
            {
                converter = converter.Validate(parameter.ValueType, null);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid type converter ({converter.InputType.Name}<==>{converter.OutputType.Name}) for parameter '{parameter.Id}'.", e);
            }
            return true;
        }

        [CanBeNull]
        public static IDictionary<string, string> SerializeArgs([CanBeNull] this IEnumerable<IParameterDescriptor> parameters, [CanBeNull] IReadonlyContext context)
        {
            if (parameters == null || context == null) return null;
            var @params = new Dictionary<string, string>();
            foreach (var p in parameters)
                if (context.TryGet(p, out var val))
                    try { @params[p.Id] = p.SerializeValue(val); }
                    catch (Exception e) { Logger.Warn("SerializeArgs", e, "param", p.Id, "value", val); }
            return @params;
        }

        [CanBeNull]
        public static IContext DeserializeArgs([CanBeNull] this IEnumerable<IParameterDescriptor> parameters, [CanBeNull] IDictionary<string, string> input)
        {
            if (parameters == null || input == null) return null;
            var context = new Context();
            foreach (var p in parameters)
                if (input.ContainsKey(p.Id))
                    try { context.Set(p, p.DeserializeValue(input[p.Id])); }
                    catch (Exception e) { Logger.Warn("DeserializeArgs", e, "param", p.Id, "value", input[p.Id]); }
            return context;
        }

        [CanBeNull]
        public static string SerializeValue([NotNull] this IParameterDescriptor parameter, [CanBeNull] object value)
        {
            if (value == null) return null;
            if (TryGetPersistentTypeConverter(parameter, out var converter))
                return SerializeValue(converter.OutputType, converter.ConvertForward(value));
            if (typeof(IParameterizedObject).IsAssignableFrom(parameter.ValueType))
            {
                var factory = parameter.GetParameterizedObjectFactory();
                var context = factory.Parse(parameter, (IParameterizedObject)value);
                var output = new Dictionary<string, string>();
                foreach (var p in factory.GetParameters(parameter))
                    if (context.TryGet(p, out var val))
                        output[p.Id] = SerializeValue(p, val);
                return JsonUtils.Serialize(output);
            }
            return SerializeValue(parameter.ValueType, value);
        }

        [CanBeNull]
        public static object DeserializeValue([NotNull] this IParameterDescriptor parameter, [CanBeNull] string value)
        {
            if (TryGetPersistentTypeConverter(parameter, out var converter))
                return converter.ConvertBackward(DeserializeValue(converter.OutputType, value));
            if (typeof(IParameterizedObject).IsAssignableFrom(parameter.ValueType))
            {
                var factory = parameter.GetParameterizedObjectFactory();
                var context = new Context();
                var strParams = JsonUtils.Deserialize<Dictionary<string, string>>(value);
                foreach (var p in factory.GetParameters(parameter))
                    if (strParams.TryGetValue(p.Id, out var val))
                        context.Set(p, DeserializeValue(p, val));
                return factory.Create(parameter, context);
            }
            return value == null ? null : DeserializeValue(parameter.ValueType, value);
        }

        [CanBeNull]
        public static string SerializeValue([NotNull] this Type type, [CanBeNull] object value) 
            => type == typeof(string) ? (string) value : JsonUtils.Serialize(value);

        [CanBeNull]
        public static object DeserializeValue([NotNull] this Type type, [CanBeNull] string value) 
            => type == typeof(string) ? value : JsonUtils.Deserialize(value, type);

    }

    public static class ParameterBuilderExt
    {

        [NotNull]
        public static Parameter<T>.Builder SetTypeConverters<T>([NotNull] this Parameter<T>.Builder builder, [NotNull] ITypeConverter typeConverter)
        {
            if (!typeConverter.IsMatch(typeof(T), null)) throw new ArgumentException("Invalid type converter");
            return builder.UseTypeConvertedPresenter(typeConverter).SetRawMetadata(ParameterPersistenceExt.PersistentTypeConverterProperty, typeConverter);
        }

        [NotNull]
        public static Parameter<ArrayQuery>.Builder SetDefaultQuery([NotNull] this Parameter<ArrayQuery>.Builder builder, [NotNull] string query)
        {
            var converter = ArrayQuery.TypeConverter;
            return builder.SetDefaultValue(converter.ConvertBackward(query)).SetTypeConverters(converter);
        }

        [NotNull]
        public static Parameter<ArrayQuery<T>>.Builder SetDefaultQuery<T>([NotNull] this Parameter<ArrayQuery<T>>.Builder builder, [NotNull] string query,
            [CanBeNull] ITypeConverter<double, T> numberConverter = null)
        {
            var converter = ArrayQuery<T>.CreateTypeConverter(numberConverter ?? TypeConverters.GetConverter<double, T>());
            return builder.SetDefaultValue(converter.ConvertBackward(query)).SetTypeConverters(converter);
        }

        [NotNull]
        public static Parameter<MatrixQuery>.Builder SetDefaultQuery([NotNull] this Parameter<MatrixQuery>.Builder builder, [NotNull] string query)
        {
            var converter = MatrixQuery.TypeConverter;
            return builder.SetDefaultValue(converter.ConvertBackward(query)).SetTypeConverters(converter);
        }

        [NotNull]
        public static Parameter<MatrixQuery<T>>.Builder SetDefaultQuery<T>([NotNull] this Parameter<MatrixQuery<T>>.Builder builder, [NotNull] string query,
            [CanBeNull] ITypeConverter<double, T> numberConverter = null)
        {
            var converter = MatrixQuery<T>.CreateTypeConverter(numberConverter ?? TypeConverters.GetConverter<double, T>());
            return builder.SetDefaultValue(converter.ConvertBackward(query)).SetTypeConverters(converter);
        }

        [NotNull]
        public static Parameter<T>.Builder SetKeyedSelectableValues<T>([NotNull] this Parameter<T>.Builder builder,
            [NotNull] IReadOnlyCollection<T> values, bool setFirstAsDefault = false) where T : INamed
        {
            if (values.Count == 0) throw new ArgumentException("No selectable values.");
            var typeConverter = TypeConverters.CreateNamedConverter(values, out var dict).Inverse();
            builder.SetRawMetadata(ParameterPersistenceExt.PersistentTypeConverterProperty, typeConverter);
            builder.UseSelectablePresenter(p => dict, null, null, typeConverter);
            if (setFirstAsDefault) builder.SetDefaultValue(values.First());
            return builder;
        }

        [NotNull]
        public static Parameter<T>.Builder SetKeyedSelectableValues<T>([NotNull] this Parameter<T>.Builder builder, 
            [NotNull] IReadOnlyDictionary<string, T> dict, bool setFirstAsDefault = false) 
            => SetKeyedSelectableValues(builder, dict.Select(pair => new Tuple<string, T>(pair.Key, pair.Value)).ToArray(), setFirstAsDefault);

        [NotNull]
        public static Parameter<T>.Builder SetKeyedSelectableValues<T>([NotNull] this Parameter<T>.Builder builder,
            [NotNull] IReadOnlyCollection<Tuple<string, T>> values, bool setFirstAsDefault = false)
        {
            if (values.Count == 0) throw new ArgumentException("No selectable values.");
            var biDiTypeConverter = TypeConverters.CreateBiDirectionConverter(values, out var dict, out _).Inverse();
            builder.SetRawMetadata(ParameterPersistenceExt.PersistentTypeConverterProperty, biDiTypeConverter);
            builder.UseSelectablePresenter(p => dict, null, null, biDiTypeConverter);
            if (setFirstAsDefault) builder.SetDefaultValue(values.First().Item2);
            return builder;
        }

        [NotNull]
        public static Parameter<T>.Builder SetKeyedSelectableValues<T>([NotNull] this Parameter<T>.Builder builder,
            [NotNull] IReadOnlyCollection<T> values, [NotNull] ITypeConverter typeConverter, bool setFirstAsDefault = false)
        {
            if (values.Count == 0) throw new ArgumentException("No selectable values.");
            typeConverter = typeConverter.Validate(typeof(T), typeof(string));
            builder.SetRawMetadata(ParameterPersistenceExt.PersistentTypeConverterProperty, typeConverter);
            var dict = new Dictionary<string, T>();
            foreach (var value in values)
                dict[(string) typeConverter.ConvertForward(value)] = value;
            builder.UseSelectablePresenter(p => dict, null, null, typeConverter.Inverse());
            if (setFirstAsDefault) builder.SetDefaultValue(values.First());
            return builder;
        }

        [NotNull]
        public static Parameter<T>.Builder SetSelectableValuesForEnum<T>([NotNull] this Parameter<T>.Builder builder, 
            bool setFirstAsDefault = false) where T : Enum
        {
            var values = EnumUtils.GetEnumValues<T>();
            if (values.Length == 0) throw new ArgumentException("Enum type has no values");
            return builder.SetSelectableValues(values, setFirstAsDefault);
        }

        [NotNull]
        public static Parameter<T>.Builder SetSelectableValuesForEnum<T>([NotNull] this Parameter<T>.Builder builder, 
            [NotNull] Func<T, string> func, bool setFirstAsDefault = false) where T : Enum
        {
            var values = EnumUtils.GetEnumValues<T>();
            if (values.Length == 0) throw new ArgumentException("Enum type has no values");
            return builder.SetKeyedSelectableValues(values.ToDictionary(func), setFirstAsDefault);
        }

    }

    public static class ParameterUtils
    {

        [NotNull]
        public static string GenerateIdByName([NotNull] string paramName)
        {
            var chars = paramName
                .Where(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                .ToArray();
            if (chars.IsEmpty()) throw new ArgumentException($"The generated id of parameter is empty for name: '{paramName}'");
            chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }

    }

}
