using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using MarukoLib.Lang.Exceptions;

namespace MarukoLib.Lang
{

    public interface IContextProperty
    {

        Type ValueType { get; }

    }

    public class ContextProperty<T> : IContextProperty, IEquatable<ContextProperty<T>>
    {

        public static bool operator ==(ContextProperty<T> left, ContextProperty<T> right) => Equals(left, right);

        public static bool operator !=(ContextProperty<T> left, ContextProperty<T> right) => !Equals(left, right);

        public ContextProperty() { }

        public ContextProperty(T defaultValue)
        {
            HasDefaultValue = true;
            DefaultValue = defaultValue;
        }

        public Type ValueType => typeof(T);

        public virtual bool HasDefaultValue { get; } = false;

        public virtual T DefaultValue { get; } = default;

        public bool TryGet(IReadonlyContext context, out T result)
        {
            if (!context.TryGet(this, out var resultObj))
            {
                result = default;
                return false;
            }
            result = (T)resultObj;
            return true;
        }

        public T Get(IReadonlyContext context)
        {
            if (!context.TryGet(this, out var result))
                return HasDefaultValue ? DefaultValue : throw new KeyNotFoundException("Cannot found property");
            return (T)result;
        }

        public T GetOrDefault(IReadonlyContext context, T defaultValue = default) =>
            TryGet(context, out var result) ? result : defaultValue;

        public void Set(IContext context, T value) => context.Set(this, value);

        public bool Equals(ContextProperty<T> that) => ReferenceEquals(this, that);

        public sealed override bool Equals(object obj) => ReferenceEquals(this, obj);

        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        public sealed override int GetHashCode() => base.GetHashCode();

    }

    public class ContextProperty : ContextProperty<object>
    {

        public ContextProperty() { }

        public ContextProperty(object defaultValue) : base(defaultValue) { }

    }

    public class NamedProperty<T> : ContextProperty<T>
    {

        public NamedProperty(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public NamedProperty(string name, T defaultValue) : base(defaultValue) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public string Name { get; }

        public override string ToString() => Name;

    }

    public interface IReadonlyContext
    {

        object this[IContextProperty property] { get; }

        IReadOnlyCollection<IContextProperty> Properties { get; }

        bool TryGet(IContextProperty property, out object result);

    }

    public interface IContext : IReadonlyContext
    {

        new object this[IContextProperty property] { get; set; }

        void Set(IContextProperty property, object value);

        void Delete(IContextProperty property);

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

        private readonly object _lock = new object();

        private readonly IDictionary<IContextProperty, object> _dict;

        public Context() : this(16) { }

        public Context(int initialCapacity) : this(new Dictionary<IContextProperty, object>(initialCapacity)) { }

        public Context(IReadonlyContext context) : this(context.ToDictionary()) { }

        private Context(IDictionary<IContextProperty, object> dictionary) => _dict = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        
        public static Context CopyOf(IDictionary dictionary)
        {
            var dict = new Dictionary<IContextProperty, object>();
            foreach (DictionaryEntry entry in dictionary)
                if (entry.Key is IContextProperty contextProperty && IsTypeMatched(contextProperty.ValueType, entry.Value))
                    dict[contextProperty] = entry.Value;
            return new Context(dict);
        }

        public static Context CopyOf<TK, TV>(IDictionary<TK, TV> dictionary)
        {
            var dict = new Dictionary<IContextProperty, object>();
            foreach (var entry in dictionary)
                if (entry.Key is IContextProperty contextProperty && IsTypeMatched(contextProperty.ValueType, entry.Value))
                    dict[contextProperty] = entry.Value;
            return new Context(dict);
        }

        private static bool IsTypeMatched(Type type, object value) => type.IsInstanceOfType(value) || (type.IsClass || type.IsNullableType()) && value == null; 

        public override IReadOnlyCollection<IContextProperty> Properties
        {
            get
            {
                lock (_lock)
                    return _dict.Keys.ToArray();
            }
        }

        public override bool TryGet(IContextProperty property, out object result)
        {
            lock (_lock)
                if (_dict.ContainsKey(property))
                {
                    result = _dict[property];
                    return IsTypeMatched(property.ValueType, result);
                }
            result = default;
            return false;
        }

        public override void Set(IContextProperty property, object value)
        {
            if (!IsTypeMatched(property.ValueType, value))
                throw new ArgumentException("value type mismatched");
            lock (_lock)
                _dict[property] = value;
        }

        public override void Delete(IContextProperty property)
        {
            lock (_lock)
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
                _observers.Add(observer);
            return new DelegatedDisposable(() =>
            {
                lock (_observers)
                    _observers.Remove(observer);
            }, false);
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
                return set;
            }
        }

        public Transaction CreateTransaction() => new Transaction(this);

        public TransactionManager CreateTransactionManager() => new TransactionManager(this);

        public void Commit(Transaction transaction)
        {
            if (transaction.Context != this) throw new ArgumentException();
            lock (_lock)
            {
                lock (transaction.Lock)
                {
                    if (transaction.IsCompleted) throw new StateException();
                    transaction.IsCommitted = true;
                    transaction.IsCompleted = true;
                }
                Refresh();
            }
        }

        public void Rollback(Transaction transaction)
        {
            if (transaction.Context != this) throw new ArgumentException();
            lock (_lock)
            {
                lock (transaction.Lock)
                {
                    if (transaction.IsCompleted) throw new StateException();
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

        public void Set(Transaction transaction, IContextProperty property, object value)
        {
            if (transaction.Context != this) throw new ArgumentException();
            if (!property.ValueType.IsInstanceOfType(value)) throw new ArgumentException();
            lock (_lock)
            lock (transaction.Lock)
            {
                if (transaction.IsCompleted) throw new StateException();
                _onTheFlyChanges.AddLast(new Tuple<Transaction, IContextProperty, object>(transaction, property, value));
                _dictOnTheFly[property] = value;
            }
        }

        public void Remove(Transaction transaction, IContextProperty property)
        {
            if (transaction.Context != this) throw new ArgumentException();
            if (transaction.IsCompleted) throw new StateException();
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

        public static T GetOrDefault<T>(this IReadonlyContext context, IContextProperty property, T defaultVal = default) => context.TryGet(property, out var obj) && obj is T t ? t : defaultVal;

        public static bool Contains(this IReadonlyContext context, IContextProperty property) => context.TryGet(property, out _);

        public static void Copy(this IReadonlyContext source, IContext destination)
        {
            foreach (var property in source.Properties)
                if (source.TryGet(property, out var value))
                    destination.Set(property, value); 
        }

        public static IDictionary<IContextProperty, object> ToDictionary(this IReadonlyContext context)
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

}
