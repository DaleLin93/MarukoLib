using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace MarukoLib.Lang
{

    public static class ActionUtils
    {

        public static readonly Action NoOp = () => { };

        public static void InvokeQuietly([CanBeNull] this Action action, Action<Exception> exceptionConsumer = null)
            => InvokeQuietly<Exception>(action, exceptionConsumer);

        public static void InvokeQuietly<TEx>([CanBeNull] this Action action, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            try { action?.Invoke(); } catch (TEx e) { exceptionConsumer?.Invoke(e); }
        }

        public static void InvokeQuietly<T>([CanBeNull] this Action<T> action, T param, Action<Exception> exceptionConsumer = null)
            => InvokeQuietly<T, Exception>(action, param, exceptionConsumer);

        public static void InvokeQuietly<TP, TEx>([CanBeNull] this Action<TP> action, TP param, Action<TEx> exceptionConsumer = null) where TEx : Exception
        {
            try { action?.Invoke(param); } catch (TEx e) { exceptionConsumer?.Invoke(e); }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static Action ContinueWith([CanBeNull] this Action action, [CanBeNull, ItemCanBeNull] params Action[] followingActions)
        {
            if (ArrayUtils.IsNullOrEmpty(followingActions)) return action;
            return () =>
            {
                action?.Invoke();
                foreach (var followingAction in followingActions)
                    followingAction?.Invoke();
            };
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static Action<T> ContinueWith<T>([CanBeNull] this Action<T> action, [CanBeNull, ItemCanBeNull] params Action<T>[] followingActions)
        {
            if (ArrayUtils.IsNullOrEmpty(followingActions)) return action;
            return input =>
            {
                action?.Invoke(input);
                foreach (var followingAction in followingActions)
                    followingAction?.Invoke(input);
            };
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static Action<T1, T2> ContinueWith<T1, T2>([CanBeNull] this Action<T1, T2> action, [CanBeNull, ItemCanBeNull] params Action<T1, T2>[] followingActions)
        {
            if (ArrayUtils.IsNullOrEmpty(followingActions)) return action;
            return (p1, p2) =>
            {
                action?.Invoke(p1, p2);
                foreach (var followingAction in followingActions)
                    followingAction?.Invoke(p1, p2);
            };
        }

    }

}
