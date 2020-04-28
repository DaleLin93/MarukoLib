using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using MarukoLib.Lang.Exceptions;

namespace MarukoLib.Lang
{

    public interface IContextProperty
    {

        [NotNull] Type ValueType { get; }

        bool IsNullable { get; }

    }

    public interface IReadonlyContext
    {

        [CanBeNull] object this[[NotNull] IContextProperty property] { get; }

        [NotNull] IReadOnlyCollection<IContextProperty> Properties { get; }

        bool TryGet([NotNull] IContextProperty property, [CanBeNull] out object result);

    }

    public interface IContext : IReadonlyContext
    {

        [CanBeNull] new object this[[NotNull] IContextProperty property] { get; set; }

        void Set([NotNull] IContextProperty property, [CanBeNull] object value);

        void Delete([NotNull] IContextProperty property);

    }

    public class ContextProperty<T> : IContextProperty, IEquatable<ContextProperty<T>>
    {

        public static bool operator ==(ContextProperty<T> left, ContextProperty<T> right) => Equals(left, right);

        public static bool operator !=(ContextProperty<T> left, ContextProperty<T> right) => !Equals(left, right);

        [CanBeNull] private readonly IContainer<T> _defaultValue;

        public ContextProperty() : this(null) { }

        public ContextProperty(T defaultValue) : this(new Immutable<T>(defaultValue), false) { } 

        public ContextProperty([CanBeNull] IContainer<T> defaultValue, bool nullable = false)
        {
            if (!nullable && defaultValue != null && defaultValue.Value == null)
                throw new ArgumentException("Default value cannot be null");
            _defaultValue = defaultValue?.ToImmutable();
            IsNullable = nullable;
        }

        public static ContextProperty<T> NotNull() => new ContextProperty<T>(null, false);

        public static ContextProperty<T> NotNull(T defaultValue) => new ContextProperty<T>(new Immutable<T>(defaultValue), false);

        public static ContextProperty<T> Nullable() => new ContextProperty<T>(null, true);

        public static ContextProperty<T> Nullable(T defaultValue) => new ContextProperty<T>(new Immutable<T>(defaultValue), true);

        public Type ValueType => typeof(T);

        public virtual bool IsNullable { get; }

        public virtual bool HasDefaultValue => _defaultValue != null;

        public virtual T DefaultValue => (_defaultValue ?? throw new Exception("Missing default value")).Value;

        /// <summary>
        /// Try get value that contained in given context.
        /// </summary>
        public bool TryGet([NotNull] IReadonlyContext context, out T result)
        {
            if (context.TryGet(this, out var resultObj) && resultObj is T t)
            {
                result = t;
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Get the value that contained in given context or use default value of this property.
        /// </summary>
        /// <exception cref="KeyNotFoundException">This property is not existed in given context, and not available default value for this property.</exception>
        public T Get([NotNull] IReadonlyContext context)
        {
            if (!context.TryGet(this, out var result))
                return HasDefaultValue ? DefaultValue : throw new KeyNotFoundException("Cannot found property");
            return (T)result;
        }

        /// <summary>
        /// Get the value that contained in given context of this property or return the given default value.
        /// </summary>
        /// <exception cref="KeyNotFoundException">This property is not existed in given context, and not available default value for this property.</exception>
        public T Get([NotNull] IReadonlyContext context, T defaultValue) =>
            TryGet(context, out var result) ? result : defaultValue;

        public void Set([NotNull] IContext context, T value) => context.Set(this, value);

        public bool Equals(ContextProperty<T> that) => ReferenceEquals(this, that);

        public sealed override bool Equals(object obj) => ReferenceEquals(this, obj);

        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        public sealed override int GetHashCode() => base.GetHashCode();

    }

    public class NamedProperty<T> : ContextProperty<T>
    {

        public NamedProperty(string name) : this(name, null) { }

        public NamedProperty(string name, T defaultValue) : this(name, new Immutable<T>(defaultValue)) { }

        public NamedProperty(string name, IContainer<T> defaultValue, bool nullable = false) : base(defaultValue, nullable) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public static NamedProperty<T> NotNull(string name) => new NamedProperty<T>(name, null, false);

        public static NamedProperty<T> NotNull(string name, T defaultValue) => new NamedProperty<T>(name, new Immutable<T>(defaultValue), false);

        public static NamedProperty<T> Nullable(string name) => new NamedProperty<T>(name, null, true);

        public static NamedProperty<T> Nullable(string name, T defaultValue) => new NamedProperty<T>(name, new Immutable<T>(defaultValue), true);

        public string Name { get; }

        public override string ToString() => Name;

    }

    public abstract class AbstractReadonlyContext : IReadonlyContext
    {

        public object this[IContextProperty key] => TryGet(key ?? throw new ArgumentNullException(nameof(key)), out var result) ? result : throw new KeyNotFoundException(key.ToString());

        public abstract IReadOnlyCollection<IContextProperty> Properties { get; }

        public abstract bool TryGet(IContextProperty property, out object result);

    }

    public sealed class EmptyContext : AbstractReadonlyContext
    {

        public static readonly EmptyContext Instance = new EmptyContext();

        private EmptyContext() { }

        public override IReadOnlyCollection<IContextProperty> Properties => EmptyArray<IContextProperty>.Instance;

        public override bool TryGet(IContextProperty property, out object result)
        {
            result = null;
            return false;
        }

    }

    public sealed class ReadonlyContext : AbstractReadonlyContext
    {

        private readonly IReadOnlyDictionary<IContextProperty, object> _dict;

        public ReadonlyContext([NotNull] IReadonlyContext context, [CanBeNull] IEnumerable<IContextProperty> properties = null)
            : this(context.ToDictionary(), properties) { }

        public ReadonlyContext([NotNull] IReadOnlyDictionary<IContextProperty, object> dictionary, [CanBeNull] IEnumerable<IContextProperty> properties = null)
            : this(dictionary.OfKeys(properties), false) { }

        internal ReadonlyContext([NotNull] IReadOnlyDictionary<IContextProperty, object> dictionary, bool copy)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            _dict = copy ? dictionary.Copy() : dictionary;
        }

        public override IReadOnlyCollection<IContextProperty> Properties => _dict.Keys.AsReadonlyCollection(_dict.Count);

        public override bool TryGet(IContextProperty property, out object result)
        {
            if (_dict.ContainsKey(property))
            {
                result = _dict[property];
                return property.IsValidValue(result);
            }
            result = default;
            return false;
        }

    }

    public sealed class CompositeReadonlyContext : AbstractReadonlyContext
    {

        [NotNull] private readonly IReadOnlyCollection<IReadonlyContext> _contexts;

        public CompositeReadonlyContext([NotNull] params IReadonlyContext[] contexts) 
            : this((IReadOnlyCollection<IReadonlyContext>)contexts) { }

        public CompositeReadonlyContext([NotNull] IReadOnlyCollection<IReadonlyContext> contexts)
            => _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));

        public override IReadOnlyCollection<IContextProperty> Properties 
            => _contexts.SelectMany(ctx => ctx.Properties).Distinct().ToLinkedList().AsReadonly();

        public override bool TryGet(IContextProperty property, out object result)
        {
            foreach (var context in _contexts)
                if (context.TryGet(property, out result))
                    return true;
            result = null;
            return false;
        }

    }

    public abstract class AbstractContext : IContext
    {

        public object this[IContextProperty key]
        {
            get => TryGet(key ?? throw new ArgumentNullException(nameof(key)), out var result) ? result : throw new KeyNotFoundException(key.ToString());
            set => Set(key ?? throw new ArgumentNullException(nameof(key)), value);
        }

        public abstract IReadOnlyCollection<IContextProperty> Properties { get; }

        public abstract bool TryGet(IContextProperty property, out object result);

        public abstract void Set(IContextProperty property, object value);

        public abstract void Delete(IContextProperty property);

    }

    public sealed class Context : AbstractContext, IDictionary<IContextProperty, object>
    {

        private readonly IDictionary<IContextProperty, object> _dict;

        public Context() : this(16) { }

        public Context(int initialCapacity) : this(new Dictionary<IContextProperty, object>(initialCapacity), false) { }

        public Context(IReadonlyContext context) : this(context.ToDictionary(), false) { }

        public Context(IDictionary<IContextProperty, object> dictionary) : this(dictionary, true) { }

        internal Context(IDictionary<IContextProperty, object> dictionary, bool copy)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            _dict = copy ? new Dictionary<IContextProperty, object>(dictionary) : dictionary;
        }

        public static Context CopyOf(IDictionary dictionary)
        {
            var dict = new Dictionary<IContextProperty, object>();
            foreach (DictionaryEntry entry in dictionary)
                if (entry.Key is IContextProperty property && property.IsValidValue(entry.Value))
                    dict[property] = entry.Value;
            return new Context(dict, false);
        }

        public static Context CopyOf<TK, TV>(IDictionary<TK, TV> dictionary)
        {
            var dict = new Dictionary<IContextProperty, object>();
            foreach (var entry in dictionary)
                if (entry.Key is IContextProperty property && property.IsValidValue(entry.Value))
                    dict[property] = entry.Value;
            return new Context(dict, false);
        }

        public override IReadOnlyCollection<IContextProperty> Properties => _dict.Keys.AsReadonly();

        public override bool TryGet(IContextProperty property, out object result)
        {
            if (_dict.ContainsKey(property))
            {
                result = _dict[property];
                return property.IsValidValue(result);
            }
            result = default;
            return false;
        }

        public override void Set(IContextProperty property, object value)
        {
            if (!property.IsValidValue(value))
                throw new ArgumentException($"Value type mismatched, expected type: {property.ValueType}, type of given value: {value?.GetType().ToString() ?? "NULL"}");
            _dict[property] = value;
        }

        public override void Delete(IContextProperty property)
        {
            if (_dict.ContainsKey(property))
                _dict.Remove(property);
        }

        ICollection<IContextProperty> IDictionary<IContextProperty, object>.Keys => _dict.Keys;

        ICollection<object> IDictionary<IContextProperty, object>.Values => _dict.Values;

        IEnumerator<KeyValuePair<IContextProperty, object>> IEnumerable<KeyValuePair<IContextProperty, object>>.GetEnumerator() => _dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _dict).GetEnumerator();

        void ICollection<KeyValuePair<IContextProperty, object>>.Add(KeyValuePair<IContextProperty, object> item) => _dict.Add(item);

        void ICollection<KeyValuePair<IContextProperty, object>>.Clear() => _dict.Clear();

        bool ICollection<KeyValuePair<IContextProperty, object>>.Contains(KeyValuePair<IContextProperty, object> item) => _dict.Contains(item);

        void ICollection<KeyValuePair<IContextProperty, object>>.CopyTo(KeyValuePair<IContextProperty, object>[] array, int arrayIndex) => _dict.CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<IContextProperty, object>>.Remove(KeyValuePair<IContextProperty, object> item) => _dict.Remove(item);

        int ICollection<KeyValuePair<IContextProperty, object>>.Count => _dict.Count;

        bool ICollection<KeyValuePair<IContextProperty, object>>.IsReadOnly => _dict.IsReadOnly;

        bool IDictionary<IContextProperty, object>.ContainsKey(IContextProperty key) => _dict.ContainsKey(key);

        void IDictionary<IContextProperty, object>.Add(IContextProperty key, object value) => _dict.Add(key, value);

        bool IDictionary<IContextProperty, object>.Remove(IContextProperty key) => _dict.Remove(key);

        bool IDictionary<IContextProperty, object>.TryGetValue(IContextProperty key, out object value) => _dict.TryGetValue(key, out value);

    }

    public sealed class SynchronizedContext : AbstractContext
    {

        private readonly object _lock = new object();

        private readonly IContext _context;

        public SynchronizedContext([NotNull] IContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        public override IReadOnlyCollection<IContextProperty> Properties
        {
            get
            {
                lock (_lock)
                    return _context.Properties.ToArray();
            }
        }

        public override bool TryGet(IContextProperty property, out object result)
        {
            lock (_lock)
                return _context.TryGet(property, out result);
        }

        public override void Set(IContextProperty property, object value)
        {
            lock (_lock)
                _context.Set(property, value);
        }

        public override void Delete(IContextProperty property)
        {
            lock (_lock)
                _context.Delete(property);
        }

    }

    public sealed class ObservableContext : IContext, IObservable<IContextProperty>
    {

        private readonly IContext _context;

        private readonly ICollection<IObserver<IContextProperty>> _observers = new LinkedList<IObserver<IContextProperty>>();

        public ObservableContext() : this(new Context()) { }

        public ObservableContext(IContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        public object this[IContextProperty key]
        {
            get => TryGet(key ?? throw new ArgumentNullException(nameof(key)), out var result) ? result : throw new KeyNotFoundException(key.ToString());
            set => Set(key ?? throw new ArgumentNullException(nameof(key)), value);
        }

        public IReadOnlyCollection<IContextProperty> Properties => _context.Properties;

        public IDisposable Subscribe(IObserver<IContextProperty> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            lock (_observers)
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
            return new DelegatedDisposable(() => Unsubscribe(observer), false);
        }

        public bool Unsubscribe(IObserver<IContextProperty> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            lock (_observers) return _observers.Remove(observer);
        }

        public bool TryGet(IContextProperty property, out object result) => _context.TryGet(property, out result);

        public void Set(IContextProperty property, object value)
        {
            _context.Set(property, value);
            NotifyPropertyChange(property);
        }

        public void Delete(IContextProperty property)
        {
            _context.Delete(property);
            NotifyPropertyChange(property);
        }

        private void NotifyPropertyChange(IContextProperty property)
        {
            foreach (var observer in _observers)
                try
                {
                    observer.OnNext(property);
                }
                catch (Exception)
                {
                    // ignored
                }
        }

    }

    public sealed class TransactionalContext : AbstractReadonlyContext
    {

        public sealed class Transaction : AbstractContext
        {

            internal object Lock = new object();

            public Transaction(TransactionalContext context) => Context = context;

            ~Transaction()
            {
                if (!IsCompleted)
                    Commit();
            }

            public TransactionalContext Context { get; }

            public bool IsCommitted { get; internal set; }

            public bool IsCompleted { get; internal set; }

            public void Commit() => Context.Commit(this);

            public void Rollback() => Context.Rollback(this);

            public override IReadOnlyCollection<IContextProperty> Properties => Context.Properties;

            public override bool TryGet(IContextProperty property, out object result) => Context.TryGet(property, out result);

            public override void Set(IContextProperty property, object value) => Context.Set(this, property, value);

            public override void Delete(IContextProperty property) => Context.Remove(this, property);

        }

        public sealed class TransactionManager : AbstractContext
        {

            private readonly object _lock = new object();

            private Transaction _transaction;

            public TransactionManager(TransactionalContext context) => Context = context;

            public TransactionalContext Context { get; }

            public Transaction Transaction
            {
                get
                {
                    lock (_lock)
                        return _transaction?.IsCompleted ?? true ? _transaction = Context.CreateTransaction() : _transaction;
                }
            }

            public void Commit() => Context.Commit(_transaction);

            public void Rollback() => Context.Rollback(_transaction);

            public override IReadOnlyCollection<IContextProperty> Properties => Context.Properties;

            public override bool TryGet(IContextProperty property, out object result) => Context.TryGet(property, out result);

            public override void Set(IContextProperty property, object value) => Context.Set(Transaction, property, value);

            public override void Delete(IContextProperty property) => Context.Remove(Transaction, property);

        }

        private static readonly object RemovedValue = new object();

        private readonly object _lock = new object();

        private readonly IDictionary _dictCommitted = new Dictionary<object, object>();

        private readonly LinkedList<Tuple<Transaction, IContextProperty, object>> _onTheFlyChanges = new LinkedList<Tuple<Transaction, IContextProperty, object>>();

        private readonly IDictionary _dictOnTheFly = new Dictionary<object, object>();

        private static void UpdatePropertyValue(IDictionary dict, IContextProperty property, object value, bool remove)
        {
            if (remove && value == RemovedValue)
                dict.Remove(property);
            else
                dict[property] = value;
        }

        public override IReadOnlyCollection<IContextProperty> Properties
        {
            get
            {
                var set = new HashSet<IContextProperty>();
                lock (_lock)
                {
                    var enumerator = _dictOnTheFly.GetEnumerator();
                    while (enumerator.MoveNext())
                        if (enumerator.Key is IContextProperty property && enumerator.Value != RemovedValue)
                            set.Add(property);
                    foreach (var property in _dictCommitted.Keys.OfType<IContextProperty>())
                        set.Add(property);
                }
                return set.AsReadonly();
            }
        }

        public Transaction CreateTransaction() => new Transaction(this);

        public TransactionManager CreateTransactionManager() => new TransactionManager(this);

        public void Commit(Transaction transaction)
        {
            if (transaction.Context != this) throw new ArgumentException(nameof(transaction));
            lock (_lock)
            {
                lock (transaction.Lock)
                {
                    if (transaction.IsCompleted) throw new InvalidOperationException();
                    transaction.IsCommitted = true;
                    transaction.IsCompleted = true;
                }
                Refresh();
            }
        }

        public void Rollback(Transaction transaction)
        {
            if (transaction.Context != this) throw new ArgumentException(nameof(transaction));
            lock (_lock)
            {
                lock (transaction.Lock)
                {
                    if (transaction.IsCompleted) throw new InvalidOperationException();
                    transaction.IsCompleted = true;
                }
                Refresh();
            }
        }

        public override bool TryGet(IContextProperty property, out object result)
        {
            lock (_lock)
            {
                if (_dictOnTheFly.Contains(property))
                {
                    result = _dictOnTheFly[property];
                    return result != RemovedValue;
                }
                if (_dictCommitted.Contains(property))
                {
                    result = _dictCommitted[property];
                    return true;
                }
            }
            result = default;
            return false;
        }

        public void Set([NotNull] Transaction transaction, [NotNull] IContextProperty property, [CanBeNull] object value)
        {
            if (transaction.Context != this) throw new ArgumentException(nameof(transaction));
            if (!property.IsValidValue(value)) throw new ArgumentException(nameof(value));
            lock (_lock)
            lock (transaction.Lock)
            {
                if (transaction.IsCompleted) throw new InvalidOperationException();
                _onTheFlyChanges.AddLast(new Tuple<Transaction, IContextProperty, object>(transaction, property, value));
                _dictOnTheFly[property] = value;
            }
        }

        public void Remove([NotNull] Transaction transaction, [NotNull] IContextProperty property)
        {
            if (transaction.Context != this) throw new ArgumentException(nameof(transaction));
            if (transaction.IsCompleted) throw new InvalidOperationException();
            lock (_lock)
            {
                lock (transaction.Lock)
                {
                    if (!_onTheFlyChanges.IsEmpty())
                    {
                        var node = _onTheFlyChanges.First;
                        do
                        {
                            var tuple = node.Value;
                            var nextNode = node.Next;
                            if (tuple.Item1 == transaction && Equals(tuple.Item2, property))
                                _onTheFlyChanges.Remove(node);
                            node = nextNode;
                        } while (node != null);
                    }
                    _onTheFlyChanges.AddLast(new Tuple<Transaction, IContextProperty, object>(transaction, property, RemovedValue));
                }
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!Monitor.IsEntered(_lock)) throw new StateException();
            _dictOnTheFly.Clear();
            if (_onTheFlyChanges.IsEmpty()) return;
            var node = _onTheFlyChanges.First;
            do
            {
                var tuple = node.Value;
                var nextNode = node.Next;
                lock (tuple.Item1.Lock)
                {
                    if (!tuple.Item1.IsCompleted) break;
                    _onTheFlyChanges.Remove(node);
                    if (tuple.Item1.IsCommitted) UpdatePropertyValue(_dictCommitted, tuple.Item2, tuple.Item3, true);
                }
                node = nextNode;
            } while (node != null);

            if (_onTheFlyChanges.IsEmpty()) return;
            node = _onTheFlyChanges.First;
            do
            {
                var tuple = node.Value;
                var nextNode = node.Next;
                lock (tuple.Item1.Lock)
                    if (!tuple.Item1.IsCompleted || tuple.Item1.IsCommitted)
                        UpdatePropertyValue(_dictOnTheFly, tuple.Item2, tuple.Item3, false);
                _dictOnTheFly[tuple.Item2] = tuple.Item3;
                node = nextNode;
            } while (node != null);
        }

    }

    public class ContextObject : AbstractContext
    {

        protected readonly IContext Context;

        public ContextObject() : this(new Context()) { }

        public ContextObject(IContext context) => Context = context ?? throw new ArgumentNullException(nameof(context));

        public override IReadOnlyCollection<IContextProperty> Properties => Context.Properties;

        public override bool TryGet(IContextProperty property, out object result) => Context.TryGet(property, out result);

        public override void Set(IContextProperty property, object value) => Context.Set(property, value);

        public override void Delete(IContextProperty property) => Context.Delete(property);

    }

    public static class ContextExt
    {

        public static bool IsValidValue(this IContextProperty property, object value)
            => value == null ? property.IsNullable : property.ValueType.IsInstanceOfType(value);

        public static T GetOrDefault<T>(this IReadonlyContext context, IContextProperty property, T defaultVal = default)
            => context.TryGet(property, out var obj) && obj is T t ? t : defaultVal;

        public static bool Contains(this IReadonlyContext context, IContextProperty property) => context.TryGet(property, out _);

        public static void Copy(this IReadonlyContext source, IContext destination)
        {
            foreach (var property in source.Properties)
                if (source.TryGet(property, out var value))
                    destination.Set(property, value);
        }

        public static Dictionary<IContextProperty, object> ToDictionary(this IReadonlyContext context)
        {
            var dict = new Dictionary<IContextProperty, object>();
            foreach (var property in context.Properties)
                if (context.TryGet(property, out var value))
                    dict[property] = value;
            return dict;
        }

        public static void Set(this IContext self, IReadonlyContext context)
        {
            foreach (var entry in context.ToDictionary())
                self.Set(entry.Key, entry.Value);
        }

        public static void Clear(this IContext context)
        {
            foreach (var property in context.Properties)
                context.Delete(property);
        }

    }

    public interface IContextBuilder
    {

        int Count { get; }

        bool TryGet([NotNull] IContextProperty property, out object value);

        [NotNull] IContextBuilder Set([NotNull] IContextProperty property, [CanBeNull] object value);

        [NotNull] IContextBuilder Delete([NotNull] IContextProperty property);

        [NotNull] IContextBuilder Clear();

        [NotNull] IContext Build();

    }

    public sealed class ContextBuilder : IContextBuilder
    {

        private readonly Dictionary<IContextProperty, object> _dict;

        public ContextBuilder() : this(16) { }

        public ContextBuilder(int initialCapacity) => _dict = new Dictionary<IContextProperty, object>(initialCapacity);

        public ContextBuilder([NotNull] IReadonlyContext context)
        {
            _dict = new Dictionary<IContextProperty, object>();
            this.SetProperties(context);
        }

        public int Count => _dict.Count;

        public bool TryGet(IContextProperty property, out object value) => _dict.TryGetValue(property, out value);

        public IContextBuilder Set(IContextProperty property, object value)
        {
            if (!property.IsValidValue(value)) throw new ArgumentException("The given value is invalid");
            _dict[property] = value;
            return this;
        }

        public IContextBuilder Delete(IContextProperty property)
        {
            _dict.Remove(property);
            return this;
        }

        public IContextBuilder Clear()
        {
            _dict.Clear();
            return this;
        }

        public IContext Build() => new Context(_dict);

    }

    public static class ContextBuilderExt
    {

        public static IContextBuilder SetProperties([NotNull] this IContextBuilder builder, [NotNull] IReadonlyContext context)
        {
            foreach (var property in context.Properties)
                if (context.TryGet(property, out var val))
                    builder.Set(property, val);
            return builder;
        }

        public static IContextBuilder SetPropertyNotNull([NotNull] this IContextBuilder builder, [NotNull] IContextProperty property, [CanBeNull] object value)
        {
            if (value != null) builder.Set(property, value);
            return builder;
        }

        public static IContextBuilder SetTypedProperty<TP>([NotNull] this IContextBuilder builder, [NotNull] ContextProperty<TP> property, TP value)
            => builder.Set(property, value);

        public static IReadonlyContext BuildReadonly([NotNull] this IContextBuilder builder) 
            => builder.Count > 0 ? (IReadonlyContext) builder.Build() : EmptyContext.Instance;

    }

}
