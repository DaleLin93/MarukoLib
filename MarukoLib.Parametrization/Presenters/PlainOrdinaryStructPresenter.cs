using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Presenters
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class PlainOrdinaryStructPresenter : MultiParameterPresenter
    {

        internal enum TargetMemberType
        {
            Any,
            Field,
            Property
        }

        internal class TargetMember
        {

            private static readonly Regex AnyMemberTypeRegex = new Regex("\\A[a-zA-Z0-9$_]+\\z", RegexOptions.Compiled);

            private static readonly Regex ExactMemberTypeRegex = new Regex("\\A([AFP]):([a-zA-Z0-9$_])+\\z", RegexOptions.Compiled);

            public readonly TargetMemberType Type;

            public readonly string Name;

            public TargetMember(TargetMemberType type, string name)
            {
                Type = type;
                Name = name;
            }

            public static TargetMember Parse(string str)
            {
                var match = ExactMemberTypeRegex.Match(str);
                if (match.Success)
                {
                    TargetMemberType type;
                    switch (match.Groups[1].Value)
                    {
                        case "A":
                            type = TargetMemberType.Any;
                            break;
                        case "F":
                            type = TargetMemberType.Field;
                            break;
                        case "P":
                            type = TargetMemberType.Property;
                            break;
                        default:
                            throw new ArgumentException($"Unrecognized target member type '{match.Groups[1].Value}' in '{str}'.");
                    }
                    var name = match.Groups[2].Value;
                    return new TargetMember(type, name);
                }
                match = AnyMemberTypeRegex.Match(str);
                if (match.Success)
                    return new TargetMember(TargetMemberType.Any, str);
                throw new ArgumentException($"Unrecognized target member '{str}'.");
            }

        }

        internal abstract class MemberParameter : IParameterDescriptor
        {

            protected MemberParameter([NotNull] MemberInfo member, [NotNull] Type type) : this(member, type,
                member.Name.ToCamelCase(), member.Name, null, null, Activator.CreateInstance(type), type.IsEnum ? Enum.GetValues(type) : null) { }

            protected MemberParameter([NotNull] MemberInfo member, [NotNull] Type type, [NotNull] string id, [CanBeNull] string name, 
                [CanBeNull] string unit, [CanBeNull] string desc, [CanBeNull] object defaultValue, [CanBeNull] IEnumerable selectableValues)
            {
                Member = member;
                Id = id;
                Name = name ?? Member.Name;
                Unit = unit;
                Description = desc;
                ValueType = type;
                DefaultValue = defaultValue;
                SelectableValues = selectableValues;
            }

            [NotNull] public MemberInfo Member { get; }

            public string Id { get; }

            public string Name { get; }

            public string Unit { get; }

            public string Description { get; }

            public Type ValueType { get; }

            public bool IsNullable => ValueType.IsNullableType();

            public object DefaultValue { get; }

            public IEnumerable SelectableValues { get; }

            public IReadonlyContext Metadata { get; } = EmptyContext.Instance;

            public bool IsValid(object value) => IsNullable && value == null || ValueType.IsInstanceOfType(value);

            public override string ToString() => Id;

            internal abstract object GetMemberValue(object instance);

            internal abstract void SetMemberValue(object instance, object value);

        }

        internal class FieldParameter : MemberParameter
        {

            public FieldParameter([NotNull] FieldInfo field) : base(field, field.FieldType) => Field = field;

            public FieldInfo Field { get; }

            internal override object GetMemberValue(object instance) => Field.GetValue(instance);

            internal override void SetMemberValue(object instance, object value) => Field.SetValue(instance, value);

        }

        internal class PropertyParameter : MemberParameter
        {

            public PropertyParameter([NotNull] PropertyInfo property) : base(property, property.PropertyType) => Property = property;

            public PropertyInfo Property { get; }

            internal override object GetMemberValue(object instance) => Property.GetValue(instance);

            internal override void SetMemberValue(object instance, object value) => Property.SetValue(instance, value);

        }

        private class Adapter : IAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly IList<ParameterViewModel> _memberParamViewModels;

            public Adapter([NotNull] IParameterDescriptor parameter, 
                [NotNull] IList<ParameterViewModel> memberParamViewModels)
            {
                _parameter = parameter;
                _memberParamViewModels = memberParamViewModels;

                foreach (var memberParamViewModel in _memberParamViewModels)
                    memberParamViewModel.ValueChanged += MemberParamViewModel_OnValueChanged;
            }

            public object Value
            {
                get
                {
                    var value = _parameter.ValueType.InitClassOrStruct();
                    foreach (var viewModel in _memberParamViewModels)
                        ((MemberParameter)viewModel.Parameter).SetMemberValue(value, viewModel.Value);
                    return _parameter.IsValidOrThrow(value);
                }
                set
                {
                    if (!_parameter.ValueType.IsInstanceOfType(value)) return;
                    using (_updateLock.Ref())
                        foreach (var viewModel in _memberParamViewModels)
                            viewModel.Value = ((MemberParameter)viewModel.Parameter).GetMemberValue(value);
                    RaiseValueChangedEvent();
                }
            }

            public void SetEnabled(bool enabled)
            {
                foreach (var memberParamViewModel in _memberParamViewModels) 
                    memberParamViewModel.IsEnabled = enabled;
            }

            public void SetValid(bool valid) { }

            private void RaiseValueChangedEvent(EventArgs e = null)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e ?? EventArgs.Empty);
            }

            private void MemberParamViewModel_OnValueChanged(object sender, EventArgs e) => RaiseValueChangedEvent(e);

        }

        private static readonly IDictionary<Type, IList<MemberParameter>> TypeMemberParameters = new Dictionary<Type, IList<MemberParameter>>();

        private static readonly ContextProperty<IEnumerable<string>> TargetMembersProperty = new NamedProperty<IEnumerable<string>>("TargetMembers");

        public static readonly PlainOrdinaryStructPresenter Instance = new PlainOrdinaryStructPresenter();

        public static void SetTargetMembers([NotNull] Type type, [CanBeNull] IReadOnlyCollection<string> targetMembers)
        {
            if (targetMembers == null)
                TypeMemberParameters.Remove(type);
            else
                TypeMemberParameters[type] = GetMemberParameters(type, targetMembers.NotNull().Select(TargetMember.Parse));
        }

        private static IList<MemberParameter> GetMemberParameters(Type type, IEnumerable<TargetMember> targetMembers)
        {
            if (!type.IsValueType) throw new ArgumentException($"Value type of parameter must be a struct type, type: '{type}'.");
            var targetMemberDict = targetMembers.ToDictionary(tm => tm.Name, tm => tm.Type);
            var memberParameters = new List<MemberParameter>();
            foreach (var nameAndType in targetMemberDict)
            {
                var name = nameAndType.Key;
                var memberType = nameAndType.Value;
                MemberParameter parameter = null;
                if (memberType == TargetMemberType.Any || memberType == TargetMemberType.Field)
                {
                    var fieldInfo = type.GetField(name);
                    if (fieldInfo != null)
                    {
                        if (fieldInfo.IsInitOnly)
                            throw new ArgumentException($"Field '{name}' must not be readonly in type '{type}'");
                        parameter = new FieldParameter(fieldInfo);
                    }
                }
                if (parameter == null && (memberType == TargetMemberType.Any || memberType == TargetMemberType.Property))
                {
                    var propertyInfo = type.GetProperty(name);
                    if (propertyInfo != null)
                    {
                        if (propertyInfo.GetMethod == null || propertyInfo.SetMethod == null)
                            throw new ArgumentException($"Property '{name}' must have getter and setter in type '{type}'.");
                        parameter = new PropertyParameter(propertyInfo);
                    }
                }
                if (parameter == null) throw new ArgumentException($"No member found in type '{type}' with '{type}:{name}'.");
                memberParameters.Add(parameter);
            }
            if (memberParameters.Count == 0)
                throw new ArgumentException($"No target members found in type '{type}' with target member descriptors '{targetMemberDict.Select(kv => $"{kv.Value}:{kv.Key}").Join(";")}'.");
            return memberParameters;
        }

        protected override IParameterDescriptor[] GetSubParameters(IParameterDescriptor param)
        {
            IList<MemberParameter> memberParameters;
            if (TargetMembersProperty.TryGet(param.Metadata, out var targetMemberStrings))
                memberParameters = GetMemberParameters(param.ValueType, targetMemberStrings.Select(TargetMember.Parse));
            else if (!TypeMemberParameters.TryGetValue(param.ValueType, out memberParameters))
                throw new ArgumentException($"No target members configured for type '{param.ValueType}'.");
            return memberParameters.Cast<IParameterDescriptor>().ToArray();
        }

        protected override IAdapter GetAdapter(IParameterDescriptor parameter, ParameterViewModel[] subParamViewModels) => new Adapter(parameter, subParamViewModels);

    }

}
