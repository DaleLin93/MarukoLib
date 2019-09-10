using System;
using System.Threading;
using System.Windows.Threading;
using MarukoLib.Logging;

namespace MarukoLib.UI
{
    public static class DispatchingUtils
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(DispatchingUtils));

        private delegate void DispatcherInvokeNoParamActionDelegate(DispatcherObject dispatcherObj, Action action);

        private delegate void DispatcherInvokeSelfParamActionDelegate<T>(T control, Action<T> action) where T : DispatcherObject;

        private delegate void DispatcherInvokeOneParamActionDelegate<T>(DispatcherObject control, Action<T> action, T param);

        private delegate TR DispatcherInvokeNoParamFuncDelegate<TR>(DispatcherObject dispatcherObj, Func<TR> action);

        private delegate TR DispatcherInvokeDelegate<T, TR>(T control, Func<T, TR> action) where T : DispatcherObject;

        private delegate void ControlInvokeDelegate<T>(T control, Action<T> action) where T : System.Windows.Forms.Control;

        private delegate TR ControlInvokeDelegate<T, TR>(T control, Func<T, TR> action) where T : System.Windows.Forms.Control;

        public static bool Sleep(int millis)
        {
            try
            {
                Thread.Sleep(millis);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void DispatchBy(this Action action, DispatcherObject dispatcherObj) => dispatcherObj.DispatcherInvoke(obj => action());

        public static TR DispatchBy<TR>(this Func<TR> func, DispatcherObject dispatcherObj) => dispatcherObj.DispatcherInvoke(obj => func());

        public static void InvokeBy(this Action action, System.Windows.Forms.Control control) => control.ControlInvoke(obj => action());

        public static TR InvokeBy<TR>(this Func<TR> func, System.Windows.Forms.Control control) => control.ControlInvoke(obj => func());

        public static void DispatcherInvoke(this DispatcherObject dispatcherObj, Action action)
        {
            if (!dispatcherObj.Dispatcher.CheckAccess())
                dispatcherObj.Dispatcher.Invoke(new DispatcherInvokeNoParamActionDelegate(DispatcherInvoke), dispatcherObj, action);
            else
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Logger.Error("DispatcherInvoke - error while action execution", e);
                }
        }

        public static void DispatcherInvoke<T>(this DispatcherObject dispatcherObj, Action<T> action, T param)
        {
            if (!dispatcherObj.Dispatcher.CheckAccess())
                dispatcherObj.Dispatcher.Invoke(new DispatcherInvokeOneParamActionDelegate<T>(DispatcherInvoke), dispatcherObj, action, param);
            else
                try
                {
                    action(param);
                }
                catch (Exception e)
                {
                    Logger.Error("DispatcherInvoke - error while action execution", e);
                }
        }

        public static void DispatcherInvoke<T>(this T dispatcherObj, Action<T> action) where T : DispatcherObject
        {
            if (!dispatcherObj.Dispatcher.CheckAccess())
                dispatcherObj.Dispatcher.Invoke(new DispatcherInvokeSelfParamActionDelegate<T>(DispatcherInvoke), dispatcherObj, action);
            else
                try
                {
                    action(dispatcherObj);
                }
                catch (Exception e)
                {
                    Logger.Error("DispatcherInvoke - error while action execution", e);
                }
        }

        public static TR DispatcherInvoke<TR>(this DispatcherObject dispatcherObj, Func<TR> func)
        {
            if (!dispatcherObj.Dispatcher.CheckAccess())
                return (TR)dispatcherObj.Dispatcher.Invoke(new DispatcherInvokeNoParamFuncDelegate<TR>(DispatcherInvoke), dispatcherObj, func);

            try
            {
                return func();
            }
            catch (Exception e)
            {
                Logger.Error("DispatcherInvoke - error while function execution", e);
                return default(TR);
            }
        }

        public static TR DispatcherInvoke<T, TR>(this T dispatcherObj, Func<T, TR> func) where T : DispatcherObject
        {
            if (!dispatcherObj.Dispatcher.CheckAccess())
                return (TR)dispatcherObj.Dispatcher.Invoke(new DispatcherInvokeDelegate<T, TR>(DispatcherInvoke), dispatcherObj, func);

            try
            {
                return func(dispatcherObj);
            }
            catch (Exception e)
            {
                Logger.Error("DispatcherInvoke - error while function execution", e);
                return default;
            }
        }

        public static void ControlInvoke<T>(this T control, Action<T> action) where T : System.Windows.Forms.Control
        {
            if (control.InvokeRequired)
                control.Invoke(new ControlInvokeDelegate<T>(ControlInvoke), control, action);
            else
            {
                try
                {
                    action(control);
                }
                catch (Exception e)
                {
                    Logger.Error("ControlInvoke - error while action execution", e);
                }
            }
        }

        public static TR ControlInvoke<T, TR>(this T control, Func<T, TR> func) where T : System.Windows.Forms.Control
        {
            if (control.InvokeRequired)
                return (TR)control.Invoke(new ControlInvokeDelegate<T, TR>(ControlInvoke), new object[] { control, func });

            try
            {
                return func(control);
            }
            catch (Exception e)
            {
                Logger.Error("ControlInvoke - error while function execution", e);
                return default;
            }
        }
    }
}
