using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.TaskSchedulers
{
    public class MyTaskScheduler : TaskScheduler
    {
        public static new TaskScheduler Current { get; } = new MyTaskScheduler();
        public static new TaskScheduler Default { get; } = Current;

        private readonly BlockingCollection<Task> m_queue = new BlockingCollection<Task>();

        private MyTaskScheduler()
        {
            Thread thread = new Thread(Run);
            thread.IsBackground = true;//设为为后台线程，当主线程结束时线程自动结束
            thread.Start();
        }

        private void Run()
        {
            Console.WriteLine($"MyTaskScheduler, ThreadID: {Thread.CurrentThread.ManagedThreadId}");
            Task t;
            while (m_queue.TryTake(out t, Timeout.Infinite))
            {
                TryExecuteTask(t);//在当前线程执行Task
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return m_queue;
        }

        protected override void QueueTask(Task task)
        {
            m_queue.Add(task);//t.Start(MyTaskScheduler.Current)时，将Task加入到队列中
        }

        //当执行该函数时，程序正在尝试以同步的方式执行Task代码
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Main, ThreadID: {Thread.CurrentThread.ManagedThreadId}");

            for (int i = 0; i < 10; i++)
            {
                var t = new Task(() =>
                {
                    Console.WriteLine($"Task, ThreadID: {Thread.CurrentThread.ManagedThreadId}");
                });

                t.Start(MyTaskScheduler.Current);
            }
            Console.ReadKey();
        }
    }

}
