using System;
using System.Collections.Generic;

namespace MarukoLib.Lang.Collections
{

    public class CollectingObserver<T> : IObserver<T>
    {

        public CollectingObserver() : this(new LinkedList<T>()) { }

        public CollectingObserver(ICollection<T> collection) => Collection = collection;

        public ICollection<T> Collection { get; }

        public void OnNext(T value) => Collection.Add(value);

        public void OnError(Exception error) { }

        public void OnCompleted() { }

        public void Reset() => Collection.Clear();

    }

}
