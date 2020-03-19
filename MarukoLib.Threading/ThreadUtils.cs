using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarukoLib.Threading
{

    public static class ThreadUtils
    {

        public static Task RunAsync([NotNull] this Action action, [CanBeNull] TimeSpan? delay = null, [CanBeNull] Action clean = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return Task.Run(() =>
            {
                try
                {
                    if (delay != null) Thread.Sleep(delay.Value);
                    action.Invoke();
                }
                finally
                {
                    clean?.Invoke();
                }
            });
        }

        public static Task RunAsync<T>([NotNull] this Action<T> action, [CanBeNull] T param, [CanBeNull] TimeSpan? delay = null, [CanBeNull] Action clean = null)
            => RunAsync(() => action(param), delay, clean);

        public static Task RunAsync<T1, T2>([NotNull] this Action<T1, T2> action, [CanBeNull] T1 param1, [CanBeNull] T2 param2, [CanBeNull] TimeSpan? delay = null, [CanBeNull] Action clean = null)
            => RunAsync(() => action(param1, param2), delay, clean);

        public static Task<T> RunAsync<T>([NotNull] this Func<T> func, [CanBeNull] TimeSpan? delay = null, [CanBeNull] Action clean = null)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return Task.Run(() =>
            {
                try
                {
                    if (delay != null) Thread.Sleep(delay.Value);
                    return func.Invoke();
                }
                finally
                {
                    clean?.Invoke();
                }
            });
        }

        public static Task<TR> RunAsync<TR, TP>(this Func<TP, TR> func, TP param, TimeSpan? delay = null, Action clean = null) => RunAsync(() => func(param), delay, clean);

        public static Task<TR> RunAsync<TR, TP1, TP2>(this Func<TP1, TP2, TR> func, TP1 param1, TP2 param2, TimeSpan? delay = null, Action clean = null) => RunAsync(() => func(param1, param2), delay, clean);

        public static void Await(this Task task, int timeout = -1)
        {
            if (!task.Wait(timeout))
                throw new TimeoutException($"The operation failed due to a timeout, timeout: {timeout}");
            if (task.Exception != null)
                throw new AggregateException(task.Exception);
        }

        public static TR Await<TR>(this Task<TR> task, int timeout = -1)
        {
            if (!task.Wait(timeout))
                throw new TimeoutException($"The operation failed due to a timeout, timeout: {timeout}");
            if (task.Exception != null)
                throw new AggregateException(task.Exception);
            return task.Result;
        }

    }
}
