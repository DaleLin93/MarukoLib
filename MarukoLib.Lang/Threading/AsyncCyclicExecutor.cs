﻿using System;
using System.Threading;
using MarukoLib.Lang.Concurrent;
using MarukoLib.Lang.Events;

namespace MarukoLib.Lang.Threading
{

    public class TaskExceptionEventArgs : ExceptionEventArgs
    {

        public TaskExceptionEventArgs(object exception, bool terminate) : base(exception) => Terminate = terminate;

        /// <summary>
        /// Terminate the repeating task, default 'true'.
        /// </summary>
        public bool Terminate { get; set; }

    }

    /// <summary>
    /// Stopping action to do while the AsyncRepeatingRunner is stopped.
    /// </summary>
    public enum StoppingAction
    {
        None, Interrupt, Abort
    }

    /// <summary>
    /// Run specific task repeatedly, asynchronously.
    /// </summary>
    public class AsyncCyclicExecutor
    {

        internal static readonly AtomicLong InstanceId = Atomics.Long();

        public event EventHandler<TaskExceptionEventArgs> UnhandledException;

        public event EventHandler Starting;

        public event EventHandler Started;

        public event EventHandler Stopping;

        public event EventHandler Stopped;

        private readonly Action _task;

        private readonly bool _stopOnUnhandledException;

        private readonly ThreadPriority _priority;

        private readonly ApartmentState _apartmentState;

        private readonly StoppingAction _stoppingAction;

        private readonly Semaphore _mutex = new Semaphore(1, 1);

        private readonly AtomicBool _stopped = Atomics.Bool(true);

        private Thread _thread;

        public AsyncCyclicExecutor(Action task, bool stopOnUnhandledException = false, ThreadPriority priority = ThreadPriority.Normal, 
            StoppingAction stoppingAction = StoppingAction.None, ApartmentState apartmentState = ApartmentState.Unknown)
            : this($"{nameof(AsyncCyclicExecutor)} #{InstanceId.GetAndIncrement()}", task, stopOnUnhandledException, priority, stoppingAction, apartmentState) { }

        public AsyncCyclicExecutor(string name, Action task, bool stopOnUnhandledException = false, ThreadPriority priority = ThreadPriority.Normal, 
            StoppingAction stoppingAction = StoppingAction.None, ApartmentState apartmentState = ApartmentState.Unknown)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _task = task ?? throw new ArgumentNullException(nameof(task));
            _stopOnUnhandledException = stopOnUnhandledException;
            _priority = priority;
            _stoppingAction = stoppingAction;
            _apartmentState = apartmentState;
        }

        public string Name { get; }

        public bool IsStarted => !_stopped.Value;

        public bool Start()
        {
            if (!_mutex.WaitOne(1, false))
                return false;
            _stopped.Set(false);
            var thread = _thread = new Thread(Run) { Name = Name, IsBackground = true, Priority = _priority };
            if (_apartmentState != ApartmentState.Unknown) thread.SetApartmentState(_apartmentState);
            Starting?.Invoke(this, EventArgs.Empty);
            thread.Start();
            return true;
        }

        public bool Stop(bool waitForStop = false)
        {
            if (_stopped.Set(true)) return false;
            Stopping?.Invoke(this, EventArgs.Empty);
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (_stoppingAction)
            {
                case StoppingAction.Interrupt:
                    _thread.Interrupt();
                    break;
                case StoppingAction.Abort:
                    _thread.Abort();
                    break;
            }
            if (!waitForStop) return true;
            while (!_mutex.WaitOne(1, true)) { }
            _mutex.Release();
            return true;
        }

        private void Run()
        {
            try
            {
                Started?.Invoke(this, EventArgs.Empty);
                while (!_stopped.Value)
                    try
                    {
                        _task();
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        var eventArgs = new TaskExceptionEventArgs(e, _stopOnUnhandledException);
                        UnhandledException?.Invoke(this, eventArgs);
                        if (eventArgs.Terminate) break;
#if DEBUG
                        throw new Exception("rethrow task exception", e);
#endif
                    }
            }
            finally
            {
                _mutex.Release();
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

    }
}
