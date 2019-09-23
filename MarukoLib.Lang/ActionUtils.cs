using System;

namespace MarukoLib.Lang
{

    public static class ActionUtils
    {

        public static readonly Action NoOp = () => { };

        public static void SafelyInvoke(this Action action, Action<Exception> exceptionConsumer = null) => SafelyInvoke<Exception>(action, exceptionConsumer);

        public static void SafelyInvoke<T>(this Action action, Action<T> exceptionConsumer = null) where T : Exception
        {
            try { action.Invoke(); } catch (T e) { exceptionConsumer?.Invoke(e); }
        }

    }

}
