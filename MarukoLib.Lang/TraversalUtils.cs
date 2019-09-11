using System;
using System.Collections.Generic;
using System.Linq;

namespace MarukoLib.Lang
{

    public static class TraversalUtils
    {

        public static void DepthFirstSearch<T>(this IEnumerable<T> values, Func<T, IEnumerable<T>> childrenFunc, Action<T> visitor)
        {
            var stack = new Stack<IEnumerator<T>>();
            stack.Push(values.GetEnumerator());
            do
            {
                var enumerator = stack.Peek();
                if (!enumerator.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                var current = enumerator.Current;
                visitor(current);
                var children = childrenFunc(current);
                if (children != null) stack.Push(children.GetEnumerator());
            } while (stack.Any());
        }

        public static void BreadthFirstSearch<T>(this IEnumerable<T> values, Func<T, IEnumerable<T>> childrenFunc, Action<T> visitor)
        {
            var queue = new Queue<T>(values);
            while (queue.Any())
            {
                var item = queue.Dequeue();
                visitor(item);
                var children = childrenFunc(item);
                foreach (var child in children)
                    queue.Enqueue(child);
            }
        }

    }
}
