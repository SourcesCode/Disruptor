using Disruptor.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.Dsl
{
    /// <summary>
    /// BasicExecutor
    /// </summary>
    public class BasicExecutor : IExecutor
    {
        private readonly ThreadFactory _factory;
        //private final Queue<Thread> threads = new ConcurrentLinkedQueue<>();

        private readonly TaskScheduler _scheduler;
        private readonly ConcurrentQueue<Thread> threads = new ConcurrentQueue<Thread>();

        /// <summary>
        /// BasicExecutor
        /// </summary>
        /// <param name="factory"></param>
        public BasicExecutor(ThreadFactory factory)
        {
            _factory = factory;
            _scheduler = TaskScheduler.Default;
        }

        /// <summary>
        /// BasicExecutor
        /// </summary>
        /// <param name="factory"></param>
        public BasicExecutor(TaskScheduler scheduler)
        {
            _factory = new ThreadFactory();
            _scheduler = scheduler ?? TaskScheduler.Default;
        }

        //@Override
        //public void execute(Runnable command)
        //{
        //    final Thread thread = factory.newThread(command);
        //    if (null == thread)
        //    {
        //        throw new RuntimeException("Failed to create thread to run: " + command);
        //    }
        //    thread.start();
        //    threads.add(thread);
        //}

        public void Execute(IRunnable command)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var workerThread = Thread.CurrentThread;
                    command.Run();
                    threads.Enqueue(workerThread);
                }
                catch (Exception)
                {
                }
            },
            CancellationToken.None, TaskCreationOptions.LongRunning, _scheduler);

        }


        public override string ToString()
        {
            return "BasicExecutor{" +
                "threads=" + DumpThreadInfo() +
                '}';
        }

        private string DumpThreadInfo()
        {
            List<Thread> threadstemp;
            lock (threads)
            {
                threadstemp = threads.ToList();
            }
            StringBuilder sb = new StringBuilder();
            foreach (Thread t in threadstemp)
            {
                sb.Append("{");
                sb.Append("name=").Append(t.Name).Append(",");
                sb.Append("id=").Append(t.ManagedThreadId).Append(",");
                sb.Append("state=").Append(t.ThreadState);
                //sb.append("lockInfo=").append(threadInfo.getLockInfo());
                sb.Append("},");
            }

            var output = sb.ToString();

            return $"[{output.Substring(0, output.Length - 1)}]";

        }

    }
}
