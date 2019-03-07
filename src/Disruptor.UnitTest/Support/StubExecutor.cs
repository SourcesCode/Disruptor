using Disruptor.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.UnitTest.Support
{
    public class StubExecutor : IExecutor
    {
        private static readonly Task _completedTask = Task.FromResult(0);
        private readonly ConcurrentQueue<Thread> _threads = new ConcurrentQueue<Thread>();
        //private readonly BlockingCollection<Thread> _threads = new BlockingCollection<Thread>();
        private bool _ignoreExecutions;
        private int _executionCount;

        public void Execute(IRunnable command)
        {
            Interlocked.Increment(ref _executionCount);

            if (Volatile.Read(ref _ignoreExecutions))
            {
                return;
            }
            var t = new Thread(() =>
            {
                try
                {
                    command.Run();
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            _threads.Enqueue(t);
            t.Start();

        }

        public void JoinAllThreads()
        {
            while (_threads.TryDequeue(out var thread))
            {
                if (!thread.Join(5000))
                {
                    thread.Interrupt();
                    Assert.IsTrue(thread.Join(5000), "Failed to stop thread: " + thread);
                }
            }
            //foreach (var thread in _threads.GetConsumingEnumerable())
            //{
            //    if (!thread.IsAlive) continue;
            //    // if the thread has not terminated after the amount of time specified.
            //    if (thread.Join(5000)) continue;
            //    // double join
            //    thread.Interrupt();
            //    Assert.False(thread.Join(5000), $"Failed to stop thread: {thread}");
            //}
        }

        public void IgnoreExecutions()
        {
            //_ignoreExecutions = true;
            Volatile.Write(ref _ignoreExecutions, true);
        }

        public int GetExecutionCount()
        {
            return Volatile.Read(ref _executionCount);
        }


    }
}
