using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarukoLib.Lang;
using MarukoLib.Logging;

namespace MarukoLib.Threading
{

    /// <summary>
    /// Split a big task into N sub-tasks (N = parallel level).
    /// </summary>
    public class ParallelPool
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ParallelPool));

        public sealed class TaskDescriptor
        {

            public readonly Guid Guid;

            public readonly uint TaskIndex;

            public readonly uint TotalTask;

            public TaskDescriptor(Guid guid, uint taskIndex, uint totalTask)
            {
                Guid = guid;
                TaskIndex = taskIndex;
                TotalTask = totalTask;
            }

            public override string ToString() => $"{nameof(Guid)}: {Guid}, {nameof(TaskIndex)}: {TaskIndex}, {nameof(TotalTask)}: {TotalTask}";

        }

        public static readonly uint RecommendedMaxParallelLevel = (uint) Environment.ProcessorCount;

        public readonly uint ParallelLevel;

        private readonly object _lock = new object();

        public ParallelPool(uint parallelLevel)
        {
            if (parallelLevel == 0) throw new ArgumentException("parallel level must be positive");
            ParallelLevel = parallelLevel;
        }

        public void Batch<T>(Action<T> taskExecFunc, params T[] @params) 
            => Batch(@params.ToList(), taskExecFunc);

        public void Batch<T>(IList<T> @params, Action<T> taskExecFunc) 
            => Batch((uint)@params.Count, task => taskExecFunc(@params[(int)task.TaskIndex]));

        public void Batch(Action<TaskDescriptor> taskExecFunc)
            => Batch(ParallelLevel, taskExecFunc);

        public void Batch(uint taskNum, Action<TaskDescriptor> taskExecFunc)
        {
            var guid = Guid.NewGuid();

            var tasks = new LinkedList<TaskDescriptor>();
            for (var i = 0; i < taskNum; i++)
                tasks.AddLast(new TaskDescriptor(guid, (uint)i, taskNum));

            var exceptions = new LinkedList<Exception>();
            lock (_lock)
            {
                var runningTasks = new Task[ParallelLevel];
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                for (var i = 0; i < runningTasks.Length; i++)
                    runningTasks[i] = Task.Factory.StartNew(() =>
                    {
                        for (;;)
                        {
                            TaskDescriptor taskDescriptor;
                            lock (tasks)
                            {
                                if (tasks.IsEmpty()) return;
                                taskDescriptor = tasks.First.Value;
                                tasks.RemoveFirst();
                            }

                            try
                            {
                                taskExecFunc(taskDescriptor);
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Batch - Task", e);
                                exceptions.AddLast(e);
                            }
                        }
                    }, cancellationToken);

                try
                {
                    Task.WaitAll(runningTasks);
                }
                catch (Exception e)
                {
                    cancellationTokenSource.Cancel();
                    Logger.Error("Batch - Wait", e);
                    throw new Exception("error occurred while waiting", e);
                }
            }
            if (exceptions.Any()) throw new Exception("error occurred in parallel task", exceptions.First.Value);
        }

    }
}
