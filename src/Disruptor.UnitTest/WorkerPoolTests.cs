using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class WorkerPoolTests
    {
        [TestMethod]
        public void ShouldProcessEachMessageByOnlyOneWorker()
        {
            var pool = new WorkerPool<AtomicLong>(new AtomicLongEventFactory(),
                new FatalExceptionHandler(),
                new AtomicLongWorkHandler(),
                new AtomicLongWorkHandler());

            var ringBuffer = pool.Start(new BasicExecutor(TaskScheduler.Current));

            ringBuffer.Next();
            ringBuffer.Next();
            ringBuffer.Publish(0);
            ringBuffer.Publish(1);

            Thread.Sleep(500);

            Assert.AreEqual(ringBuffer.Get(0).Value, (1L));
            Assert.AreEqual(ringBuffer.Get(1).Value, (1L));
        }

        [TestMethod]
        public void ShouldProcessOnlyOnceItHasBeenPublished()
        {
            var pool = new WorkerPool<AtomicLong>(new AtomicLongEventFactory(),
                new FatalExceptionHandler(),
                new AtomicLongWorkHandler(),
                new AtomicLongWorkHandler());

            var ringBuffer = pool.Start(new BasicExecutor(TaskScheduler.Current));

            ringBuffer.Next();
            ringBuffer.Next();

            Thread.Sleep(1000);

            Assert.AreEqual(ringBuffer.Get(0).Value, (0L));
            Assert.AreEqual(ringBuffer.Get(1).Value, (0L));
        }

        public class AtomicLong
        {
            private long _value;

            public long Value => _value;

            public void Increment()
            {
                Interlocked.Increment(ref _value);
            }
        }
        public class AtomicLongEventFactory : IEventFactory<AtomicLong>
        {
            public AtomicLong NewInstance()
            {
                return new AtomicLong();
            }
        }

        public class AtomicLongWorkHandler : IWorkHandler<AtomicLong>
        {
            public void OnEvent(AtomicLong evt)
            {
                evt.Increment();
            }
        }
    }
}