﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class WorkerStressTest
    {
        [TestMethod]
        public void ShouldHandleLotsOfThreads()
        {
            var disruptor = new Disruptor<TestEvent>(TestEvent.Factory, 1 << 16, TaskScheduler.Current, ProducerType.MULTI, new SleepingWaitStrategy());
            var ringBuffer = disruptor.GetRingBuffer();
            disruptor.SetDefaultExceptionHandler(new FatalExceptionHandler());

            var threads = Math.Max(1, Environment.ProcessorCount / 2);

            const int iterations = 20000000;
            var publisherCount = threads;
            var handlerCount = threads;

            var end = new CountdownEvent(publisherCount);
            var start = new CountdownEvent(publisherCount);

            var handlers = Initialise(new TestWorkHandler[handlerCount]);
            var publishers = Initialise(new Publisher[publisherCount], ringBuffer, iterations, start, end);

            disruptor.HandleEventsWithWorkerPool(handlers);

            disruptor.Start();

            foreach (var publisher in publishers)
            {
                Task.Factory.StartNew(publisher.Run);
            }

            end.Wait();
            while (ringBuffer.GetCursor() < (iterations - 1))
            {
                Thread.Sleep(0); // LockSupport.parkNanos(1);
            }

            disruptor.Shutdown();

            foreach (var publisher in publishers)
            {
                Assert.AreEqual(publisher.Failed, (false));
            }

            foreach (var handler in handlers)
            {
                Assert.AreNotEqual(handler.MessagesSeen, (0));
            }
        }

        private Publisher[] Initialise(Publisher[] publishers, RingBuffer<TestEvent> buffer, int messageCount, CountdownEvent start, CountdownEvent end)
        {
            for (var i = 0; i < publishers.Length; i++)
            {
                publishers[i] = new Publisher(buffer, messageCount, start, end);
            }

            return publishers;
        }

        private TestWorkHandler[] Initialise(TestWorkHandler[] testWorkHandlers)
        {
            for (var i = 0; i < testWorkHandlers.Length; i++)
            {
                var handler = new TestWorkHandler();
                testWorkHandlers[i] = handler;
            }

            return testWorkHandlers;
        }

        private class TestWorkHandler : IWorkHandler<TestEvent>
        {
            public int MessagesSeen;

            public void OnEvent(TestEvent @event)
            {
                MessagesSeen++;
            }
        }

        private class Publisher
        {
            private readonly RingBuffer<TestEvent> _ringBuffer;
            private readonly CountdownEvent _end;
            private readonly CountdownEvent _start;
            private readonly int _iterations;

            public bool Failed;

            public Publisher(RingBuffer<TestEvent> ringBuffer, int iterations, CountdownEvent start, CountdownEvent end)
            {
                _ringBuffer = ringBuffer;
                _end = end;
                _start = start;
                _iterations = iterations;
            }

            public void Run()
            {
                try
                {
                    _start.Signal();
                    _start.Wait();

                    var i = _iterations;
                    while (--i != -1)
                    {
                        var next = _ringBuffer.Next();
                        var testEvent = _ringBuffer.Get(next);
                        testEvent.Sequence = next;
                        testEvent.A = next + 13;
                        testEvent.B = next - 7;
                        testEvent.S = "wibble-";
                        _ringBuffer.Publish(next);
                    }
                }
                catch (Exception)
                {
                    Failed = true;
                }
                finally
                {
                    _end.Signal();
                }
            }
        }

        private class TestEvent
        {
            public static readonly TestEventFactory Factory = new TestEventFactory();

            public long Sequence;
            public long A;
            public long B;
            public string S;
        }

        private class TestEventFactory : IEventFactory<TestEvent>
        {
            public TestEvent NewInstance()
            {
                return new TestEvent();
            }
        }

    }
}
