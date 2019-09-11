using System;
using System.Threading;
using System.Threading.Tasks;
using MarukoLib.Logging;

namespace MarukoLib.Threading
{

    public static class ThreadUtils
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ThreadUtils));

        public static Task RunAsync(this Action action, TimeSpan? delay = null, Action clean = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (delay != null)
                        Thread.Sleep(delay.Value);
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error("RunAsync - error while action execution", e);
                }
                finally
                {
                    clean?.Invoke();
                }
            });
        }

        public static Task RunAsync<T>(this Action<T> action, T param, TimeSpan? delay = null, Action clean = null) => RunAsync(() => action(param), delay, clean);

        public static Task RunAsync<T1, T2>(this Action<T1, T2> action, T1 param1, T2 param2, TimeSpan? delay = null, Action clean = null) => RunAsync(() => action(param1, param2), delay, clean);

        public static Task<T> RunAsync<T>(this Func<T> func, TimeSpan? delay = null, Action clean = null)
        {
            if (func == null)
                throw new ArgumentException("func cannot be null");
            return Task.Run(() =>
            {
                try
                {
                    if (delay != null)
                    {
                        Thread.Sleep(delay.Value);
                    }
                    return func.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error("RunAsync - error while function execution", e);
                    return default;
                }
                finally
                {
                    clean?.Invoke();
                }
            });
        }

        public static Task<TR> RunAsync<TR, TP>(this Func<TP, TR> func, TP param, TimeSpan? delay = null, Action clean = null)
        {
            return RunAsync(() => func(param), delay, clean);
        }

        public static Task<TR> RunAsync<TR, TP1, TP2>(this Func<TP1, TP2, TR> func, TP1 param1, TP2 param2, TimeSpan? delay = null, Action clean = null)
        {
            return RunAsync(() => func(param1, param2), delay, clean);
        }

        public static void Await(this Task task, int timeout = -1)
        {
            if (!task.Wait(timeout))
                throw new TimeoutException();
        }

        public static TR Await<TR>(this Task<TR> task, int timeout = -1)
        {
            if (!task.Wait(timeout))
                throw new TimeoutException();
            return task.Result;
        }

    }
}
