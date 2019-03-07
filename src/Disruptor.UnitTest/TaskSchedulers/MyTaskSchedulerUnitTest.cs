using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.TaskSchedulers;

namespace Disruptor.UnitTest.TaskSchedulers
{
    [TestClass]
    public class MyTaskSchedulerUnitTest
    {
        [TestMethod]
        public void TestMethod1()
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
