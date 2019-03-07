using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.TaskSchedulers;

namespace Disruptor.UnitTest.TaskSchedulers
{
    [TestClass]
    public class LimitedConcurrencyLevelTaskSchedulerUnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Create a scheduler that uses two threads. 
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(2);
            List<Task> tasks = new List<Task>();

            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(lcts);
            CancellationTokenSource cts = new CancellationTokenSource();

            // Use our factory to run a set of tasks. 
            Object lockObj = new Object();
            int outputItem = 0;

            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t = factory.StartNew(() =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        lock (lockObj)
                        {
                            Console.Write("{0} in task t-{1} on thread {2}   ",
                                          i, iteration, Thread.CurrentThread.ManagedThreadId);
                            outputItem++;
                            if (outputItem % 3 == 0)
                                Console.WriteLine();
                        }
                    }
                }, cts.Token);
                tasks.Add(t);
            }
            // Use it to run a second set of tasks.                       
            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t1 = factory.StartNew(() =>
                {
                    for (int outer = 0; outer <= 10; outer++)
                    {
                        for (int i = 0x21; i <= 0x7E; i++)
                        {
                            lock (lockObj)
                            {
                                Console.Write("'{0}' in task t1-{1} on thread {2}   ",
                                              Convert.ToChar(i), iteration, Thread.CurrentThread.ManagedThreadId);
                                outputItem++;
                                if (outputItem % 3 == 0)
                                    Console.WriteLine();
                            }
                        }
                    }
                }, cts.Token);
                tasks.Add(t1);
            }

            // Wait for the tasks to complete before displaying a completion message.
            Task.WaitAll(tasks.ToArray());
            cts.Dispose();
            Console.WriteLine("\n\nSuccessful completion.");
        }
    }
    // The following is a portion of the output from a single run of the example:
    //    'T' in task t1-4 on thread 3   'U' in task t1-4 on thread 3   'V' in task t1-4 on thread 3   
    //    'W' in task t1-4 on thread 3   'X' in task t1-4 on thread 3   'Y' in task t1-4 on thread 3   
    //    'Z' in task t1-4 on thread 3   '[' in task t1-4 on thread 3   '\' in task t1-4 on thread 3   
    //    ']' in task t1-4 on thread 3   '^' in task t1-4 on thread 3   '_' in task t1-4 on thread 3   
    //    '`' in task t1-4 on thread 3   'a' in task t1-4 on thread 3   'b' in task t1-4 on thread 3   
    //    'c' in task t1-4 on thread 3   'd' in task t1-4 on thread 3   'e' in task t1-4 on thread 3   
    //    'f' in task t1-4 on thread 3   'g' in task t1-4 on thread 3   'h' in task t1-4 on thread 3   
    //    'i' in task t1-4 on thread 3   'j' in task t1-4 on thread 3   'k' in task t1-4 on thread 3   
    //    'l' in task t1-4 on thread 3   'm' in task t1-4 on thread 3   'n' in task t1-4 on thread 3   
    //    'o' in task t1-4 on thread 3   'p' in task t1-4 on thread 3   ']' in task t1-2 on thread 4   
    //    '^' in task t1-2 on thread 4   '_' in task t1-2 on thread 4   '`' in task t1-2 on thread 4   
    //    'a' in task t1-2 on thread 4   'b' in task t1-2 on thread 4   'c' in task t1-2 on thread 4   
    //    'd' in task t1-2 on thread 4   'e' in task t1-2 on thread 4   'f' in task t1-2 on thread 4   
    //    'g' in task t1-2 on thread 4   'h' in task t1-2 on thread 4   'i' in task t1-2 on thread 4   
    //    'j' in task t1-2 on thread 4   'k' in task t1-2 on thread 4   'l' in task t1-2 on thread 4   
    //    'm' in task t1-2 on thread 4   'n' in task t1-2 on thread 4   'o' in task t1-2 on thread 4   
    //    'p' in task t1-2 on thread 4   'q' in task t1-2 on thread 4   'r' in task t1-2 on thread 4   
    //    's' in task t1-2 on thread 4   't' in task t1-2 on thread 4   'u' in task t1-2 on thread 4   
    //    'v' in task t1-2 on thread 4   'w' in task t1-2 on thread 4   'x' in task t1-2 on thread 4   
    //    'y' in task t1-2 on thread 4   'z' in task t1-2 on thread 4   '{' in task t1-2 on thread 4   
    //    '|' in task t1-2 on thread 4   '}' in task t1-2 on thread 4   '~' in task t1-2 on thread 4   
    //    'q' in task t1-4 on thread 3   'r' in task t1-4 on thread 3   's' in task t1-4 on thread 3   
    //    't' in task t1-4 on thread 3   'u' in task t1-4 on thread 3   'v' in task t1-4 on thread 3   
    //    'w' in task t1-4 on thread 3   'x' in task t1-4 on thread 3   'y' in task t1-4 on thread 3   
    //    'z' in task t1-4 on thread 3   '{' in task t1-4 on thread 3   '|' in task t1-4 on thread 3  
}
