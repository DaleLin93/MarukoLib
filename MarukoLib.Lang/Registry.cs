using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public interface IRegistrable
    {

        [NotNull] string Id { get; }

    }

    public interface IRegistry 
    {

        [NotNull] Type TargetType { get; }

        [NotNull] IRegistrable[] Registered { get; }

        void Register([NotNull] IRegistrable registrable);

        void Unregister([NotNull] IRegistrable registrable);

        /// <summary>
        /// Look up registrable by ID.
        /// </summary>
        /// <returns>true if specific registrable is found, and value will be non-null.</returns>
        bool LookUp([CanBeNull] string id, out IRegistrable registrable);

    }

    public interface IRegistry<T> where T : IRegistrable
    {

        [NotNull] T[] Registered { get; }

        void Register([NotNull] T registrable);

        void Unregister([NotNull] T registrable);

        /// <summary>
        /// Look up registrable by ID.
        /// </summary>
        /// <returns>true if specific registrable is found, and value will be non-null.</returns>
        bool LookUp([CanBeNull] string id, out T registrable);

    }

    public sealed class Registry : IRegistry
    {

        private readonly IDictionary<string, IRegistrable> _registered = new Dictionary<string, IRegistrable>();

        public Registry(Type type)
        {
            if (!typeof(IRegistrable).IsAssignableFrom(type)) throw new ArgumentException($"target type is not assignable from {typeof(IRegistrable)}");
            TargetType = type;
        }

        public Type TargetType { get; }

        public IRegistrable[] Registered
        {
            get
            {
                lock (_registered)
                    return _registered.Values.ToArray();
            }
        }

        public void Register(IRegistrable registrable)
        {
            CheckType(registrable);
            var id = registrable.Id;
            lock (_registered)
                if (_registered.ContainsKey(id))
                    throw new ArgumentException($"identifier already used: {id}");
                else
                    _registered[id] = registrable;
        }

        public void Unregister(IRegistrable registrable)
        {
            CheckType(registrable);
            var id = registrable.Id;
            lock (_registered)
                if (!_registered.ContainsKey(id) || !ReferenceEquals(_registered[id], registrable))
                    throw new ArgumentException($"unregistered: {id}");
                else
                    _registered.Remove(id);
        }

        public bool LookUp(string id, out IRegistrable registrable)
        {
            if (id != null)
                lock (_registered)
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (_registered.TryGetValue(id, out registrable))
                        return true;
            registrable = default;
            return false;
        }

        internal void CheckType(IRegistrable registrable)
        {
            if (registrable == null) throw new ArgumentNullException(nameof(registrable));
            if (!TargetType.IsInstanceOfType(registrable)) throw new ArgumentException($"type not match, required: {TargetType}, given: {registrable.GetType()}");
        }

    }

    public sealed class ComplexRegistry : IRegistry
    {

        public ComplexRegistry(IRegistry parent)
        {
            Parent = parent;
            Registry = new Registry(parent.TargetType);
            TargetType = parent.TargetType;
        }

        public IRegistry Parent { get; }

        public IRegistry Registry { get; }

        public Type TargetType { get; }

        public IRegistrable[] Registered
        {
            get
            {
                var dict = new Dictionary<string, IRegistrable>();
                foreach (var registrable in Parent.Registered)
                    dict[registrable.Id] = registrable;
                foreach (var registrable in Registry.Registered)
                    dict[registrable.Id] = registrable;
                return dict.Values.ToArray();
            }
        }

        public void Register(IRegistrable registrable)
        {
            if (Parent.LookUp(registrable.Id, out _)) throw new AccessViolationException();
            Registry.Register(registrable);
        }

        public void Unregister(IRegistrable registrable)
        {
            if (Parent.LookUp(registrable.Id, out _)) throw new AccessViolationException();
            Registry.Unregister(registrable);
        }

        public bool LookUp(string id, out IRegistrable registrable)
        {
            if (Registry.LookUp(id, out registrable)) return true;
            if (Parent.LookUp(id, out registrable)) return true;
            registrable = default;
            return false;
        }

    }

    public sealed class Registry<T> : IRegistry, IRegistry<T> where T : IRegistrable
    {

        public Registry() : this(new Registry(typeof(T))) { }

        public Registry(IRegistry baseRegistry)
        {
            if (baseRegistry.TargetType != typeof(T)) throw new ArgumentException("type not match");
            BaseRegistry = baseRegistry;
        }

        public IRegistry BaseRegistry { get; }

        public Type TargetType => typeof(T);

        public T[] Registered => BaseRegistry.Registered.OfType<T>().ToArray();

        public void Register(T registrable) => BaseRegistry.Register(registrable);

        public void Unregister(T registrable) => BaseRegistry.Unregister(registrable);

        public bool LookUp(string id, out T registrable)
        {
            var flag = BaseRegistry.LookUp(id, out var t);
            // ReSharper disable once AssignNullToNotNullAttribute
            registrable = flag ? (T)t : default;
            return flag && registrable != null;
        }

        IRegistrable[] IRegistry.Registered => BaseRegistry.Registered;

        void IRegistry.Register(IRegistrable registrable) => BaseRegistry.Register(registrable);

        void IRegistry.Unregister(IRegistrable registrable) => BaseRegistry.Unregister(registrable);

        bool IRegistry.LookUp(string id, out IRegistrable registrable) => BaseRegistry.LookUp(id, out registrable);

    }

    public class Registries
    {

        private readonly IDictionary<Type, IRegistry> _registered = new Dictionary<Type, IRegistry>();

        public T[] GetRegistered<T>() where T : IRegistrable => GetRegistry<T>().Registered;

        public IRegistrable[] GetRegistered(Type type) => GetRegistry(type).Registered;

        public IRegistry<T> GetRegistry<T>() where T : IRegistrable => new Registry<T>(GetRegistry(typeof(T)));

        public IRegistry GetRegistry(Type type)
        {
            if (!typeof(IRegistrable).IsAssignableFrom(type))
                throw new ArgumentException($"target type is not assignable from {typeof(IRegistrable)}");
            IRegistry registry;
            lock (_registered)
                if (!_registered.TryGetValue(type, out registry))
                    registry = _registered[type] = new Registry(type);
            return registry;
        }

        public void SetupRegistry([NotNull] IRegistry registry, bool move = true)
        {
            var type = registry.TargetType;
            lock (_registered)
            {
                if (move && _registered.TryGetValue(type, out var existed))
                {
                    var array = existed.Registered;
                    existed.UnregisterAll(array);
                    registry.RegisterAll(array);
                }
                _registered[type] = registry;
            }
        }

        public void Register<T>(T value) where T : IRegistrable => GetRegistry(typeof(T)).Register(value);

        public void Unregister<T>(T value) where T : IRegistrable => GetRegistry(typeof(T)).Unregister(value);

        public void RegisterAll<T>(IEnumerable<T> values) where T : IRegistrable => GetRegistry(typeof(T)).RegisterAll(values);

        public void UnregisterAll<T>(IEnumerable<T> values) where T : IRegistrable => GetRegistry(typeof(T)).UnregisterAll(values);

    }

    public static class RegistryExt
    {

        public static Registry<T> OfType<T>(this IRegistry registry) where T : IRegistrable => new Registry<T>(registry);

        public static void RegisterAll<T>(this IRegistry registry, IEnumerable<T> registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Register(registrable);
        }

        public static void RegisterAll<T>(this IRegistry<T> registry, params T[] registrables) where T : IRegistrable => RegisterAll(registry, (IEnumerable<T>)registrables);

        public static void RegisterAll<T>(this IRegistry<T> registry, IEnumerable<T> registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Register(registrable);
        }

        public static void UnregisterAll<T>(this IRegistry registry, IEnumerable<T> registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Unregister(registrable);
        }

        public static void UnregisterAll<T>(this IRegistry<T> registry, params T[] registrables) where T : IRegistrable => UnregisterAll(registry, (IEnumerable<T>)registrables);

        public static void UnregisterAll<T>(this IRegistry<T> registry, IEnumerable<T> registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Unregister(registrable);
        }

    }

}
