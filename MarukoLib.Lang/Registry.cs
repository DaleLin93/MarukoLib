﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public interface IRegistrable
    {

        [NotNull] string Identifier { get; }

    }

    public interface IRegistry
    {

        Type TargetType { get; }

        IRegistrable[] Registered { get; }

        void Register(IRegistrable registrable);

        void Unregister(IRegistrable registrable);

        /// <summary>
        /// Look up registrable by ID.
        /// </summary>
        /// <returns>true if specific registrable is found, and value will be non-null.</returns>
        bool LookUp(string id, out IRegistrable registrable);

    }

    public interface IRegistry<T> where T : IRegistrable
    {

        T[] Registered { get; }

        void Register(T registrable);

        void Unregister(T registrable);

        /// <summary>
        /// Look up registrable by ID.
        /// </summary>
        /// <returns>true if specific registrable is found, and value will be non-null.</returns>
        bool LookUp(string id, out T registrable);

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
            var id = registrable.Identifier ?? throw new ArgumentException("Id of registrable cannot be null");
            lock (_registered)
                if (_registered.ContainsKey(id))
                    throw new ArgumentException($"identifier already used: {id}");
                else
                    _registered[id] = registrable;
        }

        public void Unregister(IRegistrable registrable)
        {
            CheckType(registrable);
            var id = registrable.Identifier ?? throw new ArgumentException("Id of registrable cannot be null");
            lock (_registered)
                if (!_registered.ContainsKey(id) || !ReferenceEquals(_registered[id], registrable))
                    throw new ArgumentException($"unregistered: {id}");
                else
                    _registered.Remove(id);
        }

        public bool LookUp(string id, out IRegistrable registrable)
        {
            var contains = false;
            registrable = default;
            if (id != null)
                lock (_registered)
                    if (contains = _registered.ContainsKey(id))
                        registrable = _registered[id];
            return contains;
        }

        internal void CheckType(IRegistrable registrable)
        {
            if (registrable == null) throw new ArgumentNullException(nameof(registrable));
            if (!TargetType.IsInstanceOfType(registrable)) throw new ArgumentException($"type not match, required: {TargetType}, given: {registrable.GetType()}");
        }

    }

    public sealed class Registry<T> : IRegistry, IRegistry<T> where T : IRegistrable
    {

        public Registry() : this(new Registry(typeof(T))) { }

        public Registry(Registry baseRegistry)
        {
            if (baseRegistry.TargetType != typeof(T)) throw new ArgumentException("type not match");
            BaseRegistry = baseRegistry;
        }

        public Registry BaseRegistry { get; }

        public Type TargetType => typeof(T);

        public T[] Registered => BaseRegistry.Registered.OfType<T>().ToArray();

        public void Register(T registrable) => BaseRegistry.Register(registrable);

        public void Unregister(T registrable) => BaseRegistry.Unregister(registrable);

        public bool LookUp(string id, out T registrable)
        {
            var flag = BaseRegistry.LookUp(id, out var t);
            registrable = flag ? (T) t : default;
            return flag;
        }

        IRegistrable[] IRegistry.Registered => BaseRegistry.Registered;

        void IRegistry.Register(IRegistrable registrable) => BaseRegistry.Register(registrable);

        void IRegistry.Unregister(IRegistrable registrable) => BaseRegistry.Unregister(registrable);

        bool IRegistry.LookUp(string id, out IRegistrable registrable) => BaseRegistry.LookUp(id, out registrable);

    }

    public sealed class Registries
    {

        private readonly IDictionary<Type, Registry> _registered = new Dictionary<Type, Registry>();

        public Registry<T> Registry<T>() where T : IRegistrable => new Registry<T>(Registry(typeof(T)));

        public Registry Registry(Type type)
        {
            if (!typeof(IRegistrable).IsAssignableFrom(type))
                throw new ArgumentException($"target type is not assignable from {typeof(IRegistrable)}");
            Registry registry;
            lock (_registered)
                if (_registered.ContainsKey(type))
                    registry = _registered[type];
                else
                    registry = _registered[type] = new Registry(type);
            return registry;
        }

    }

    public static class RegistryExt
    {

        public static void RegisterAll<T>(this Registry<T> registry, params T[] registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Register(registrable);
        }

        public static void UnregisterAll<T>(this Registry<T> registry, params T[] registrables) where T : IRegistrable
        {
            foreach (var registrable in registrables)
                registry.Unregister(registrable);
        }

    }

}