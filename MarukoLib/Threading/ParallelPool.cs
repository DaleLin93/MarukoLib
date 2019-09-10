using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarukoLib.Lang;
using MarukoLib.Logging;

namespace MarukoLib.Threading
{

    public class ParallelPool
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ParallelPool));

        public delegate void Consumer<in T>(T input);

        public sealed class Task
        {

            public readonly Guid Guid;

            public readonly uint TaskIndex;

            public readonly uint TotalTask;

            public Task(Guid guid, uint taskIndex, uint totalTask)
            {
                Guid = guid;
                TaskIndex = taskIndex;
                TotalTask = totalTask;
            }

            public override string ToString() => $"{nameof(Guid)}: {Guid}, {nameof(TaskIndex)}: {TaskIndex}, {nameof(TotalTask)}: {TotalTask}";

        }

        public readonly uint ParallelLevel;

        private readonly object _lock = new object();

        public ParallelPool(uint parallelLevel)
        {
            if (parallelLevel == 0) throw new ArgumentException("parallel level must be positive");
            ParallelLevel = parallelLevel;
        }

        public void Batch<T>(Consumer<T> consumer, params T[] @params) => Batch(@params.ToList(), consumer);

        public void Batch<T>(IList<T> @params, Consumer<T> consumer) => Batch((uint)@params.Count, task => consumer(@params[(int)task.TaskIndex]));

        public void Batch(Consumer<Task> consumer) => Batch(ParallelLevel, consumer);

        public void Batch(uint taskNum, Consumer<Task> consumer)
        {
            var guid = Guid.NewGuid();

            var tasks = new LinkedList<Task>();
            for (var i = 0; i < taskNum; i++)
                tasks.AddLast(new Task(guid, (uint)i, taskNum));

            var exceptions = new LinkedList<Exception>();
            lock (_lock)
            {
                var runningTasks = new System.Threading.Tasks.Task[ParallelLevel];
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;

                for (var i = 0; i < runningTasks.Length; i++)
                    runningTasks[i] = System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            Task task;
                            lock (tasks)
                            {
                                if (tasks.IsEmpty()) return;
                                task = tasks.First.Value;
                                tasks.RemoveFirst();
                            }

                            try
                            {
                                consumer(task);
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
                    System.Threading.Tasks.Task.WaitAll(runningTasks);
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
