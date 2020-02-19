using System;

namespace MarukoLib.Lang
{

    public static class ActionUtils
    {

        public static readonly Action NoOp = () => { };

        public static void InvokeQuietly(this Action action, Action<Exception> exceptionConsumer = null) => InvokeQuietly<Exception>(action, exceptionConsumer);

        public static void InvokeQuietly<TEx>(this Action action, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            try { action.Invoke(); } catch (TEx e) { exceptionConsumer?.Invoke(e); }
        }

    }

}
