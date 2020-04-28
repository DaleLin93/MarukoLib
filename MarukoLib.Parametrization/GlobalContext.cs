using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Persistence;

namespace MarukoLib.Parametrization
{

    internal class ParameterRegistry : IRegistry<GlobalParameter>, IRegistry
    {

        private readonly object _lock = new object();

        private readonly IRegistry<GlobalParameter> _registry = new Registry<GlobalParameter>();

        private readonly IDictionary<string, ParameterGroup> _groups = new Dictionary<string, ParameterGroup>();

        public ParameterGroup[] Groups => _groups.Values.ToArray();

        public GlobalParameter[] Registered
        {
            get
            {
                lock (_lock)
                    return _registry.Registered;
            }
        }

        public void Register(GlobalParameter parameter)
        {
            lock (_lock)
            {
                _registry.Register(parameter);
                AddToGroup(parameter);
                GlobalContext.Reload(parameter.Parameter);
            }
        }

        public void Unregister(GlobalParameter parameter)
        {
            lock (_lock)
            {
                _registry.Unregister(parameter);
                RemoveFromGroup(parameter);
            }
        }

        public bool LookUp(string id, out GlobalParameter parameter)
        {
            lock (_lock)
                return _registry.LookUp(id, out parameter);
        }

        private void AddToGroup(GlobalParameter parameter)
        {
            var groupName = parameter.Group ?? string.Empty;
            var list = new LinkedList<IDescriptor>();
            if (_groups.TryGetValue(groupName, out var pg)) list.AddAll(pg.Items);
            list.AddLast(parameter.Parameter);
            _groups[groupName] = new ParameterGroup(groupName, list.AsReadonly());
        }

        private void RemoveFromGroup(GlobalParameter parameter)
        {
            var groupName = parameter.Group ?? string.Empty;
            if (!_groups.TryGetValue(parameter.Group ?? string.Empty, out var group)) return;
            var list = new LinkedList<IDescriptor>(group.Items);
            if (!list.Remove(parameter.Parameter)) return;
            if (list.IsEmpty())
                _groups.Remove(groupName);
            else 
                _groups[groupName] = new ParameterGroup(groupName, list.AsReadonly());
        }

        Type IRegistry.TargetType => typeof(GlobalParameter);

        IRegistrable[] IRegistry.Registered => Registered.Cast<IRegistrable>().ToArray();

        void IRegistry.Register(IRegistrable registrable) => Register((GlobalParameter)registrable);

        void IRegistry.Unregister(IRegistrable registrable) => Unregister((GlobalParameter)registrable);

        bool IRegistry.LookUp(string id, out IRegistrable registrable)
        {
            var flag = LookUp(id, out var parameter);
            registrable = parameter;
            return flag;
        }

    }

    public sealed class GlobalParameter : IRegistrable
    {

        public GlobalParameter([NotNull] IParameterDescriptor parameter, [CanBeNull] string group)
        {
            Parameter = parameter;
            Group = group;
        }

        public string Id => Parameter.Id;

        [NotNull] public IParameterDescriptor Parameter { get; }

        [CanBeNull] public string Group { get; }

    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GlobalParameterAttribute : Attribute
    {

        public GlobalParameterAttribute([CanBeNull] string group = null) => Group = group;

        [CanBeNull] public string Group { get; set; }

    }

    public static class GlobalContext
    {

        public static event EventHandler Changed;

        public static readonly Registries Registries = new Registries();

        private static readonly TransactionalContext Context = new TransactionalContext();

        private static readonly ParameterRegistry ParameterRegistry = new ParameterRegistry();

        private static IDictionary<string, string> _dict;

        static GlobalContext() => Registries.SetupRegistry(ParameterRegistry);

        public static IReadonlyContext Variables => Context;

        public static ParameterGroup[] ParameterDefinitions => ParameterRegistry.Groups;

        public static void Apply([CanBeNull] IReadonlyContext context)
        {
            if (context == null) return;
            var transaction = Context.CreateTransaction();
            foreach (var property in context.Properties)
                transaction[property] = context[property];
            transaction.Commit();
            Changed?.Invoke(null, EventArgs.Empty);
        }

        #region Save & Load

        public static void Save([NotNull] string filePath)
        {
            var dict = _dict ?? new Dictionary<string, string>();
            dict.PutAll(Serialize());
            dict.JsonSerializeToFile(filePath, JsonUtils.Pretty);
            _dict = dict;
        }

        public static void Load([NotNull] string filePath)
            => Deserialize(_dict = JsonUtils.DeserializeFromFile<IDictionary<string, string>>(filePath) ?? new Dictionary<string, string>());

        #endregion

        #region Serialization & Deserialization

        [NotNull]
        public static IDictionary<string, string> Serialize() 
            => ParameterDefinitions.GetAllParameters().SerializeArgs(Variables) ?? throw new Exception("unreachable");

        public static void Deserialize([CanBeNull] IDictionary<string, string> input)
        {
            if (input == null) return;
            Apply(ParameterDefinitions.GetAllParameters().DeserializeArgs(input));
        }

        #endregion

        internal static void Reload([NotNull] IParameterDescriptor parameter)
        {
            if (_dict == null) return;
            Apply(new[] {parameter}.DeserializeArgs(_dict));
        }

    }

}
