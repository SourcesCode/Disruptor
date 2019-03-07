using Disruptor.Dsl;
using Disruptor.UnitTest.Support.EventFactorys;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Disruptor.Tests
{
    [TestClass]
    public class ShutdownOnFatalExceptionTest
    {
        private readonly Random _random = new Random();
        private readonly FailingEventHandler _failingEventHandler = new FailingEventHandler();
        private Disruptor<byte[]> _disruptor;


        public ShutdownOnFatalExceptionTest()
        {
            _disruptor = new Disruptor<byte[]>(new ByteArrayEventFactory(256), 1024, TaskScheduler.Current, ProducerType.SINGLE, new BlockingWaitStrategy());
            _disruptor.HandleEventsWith(_failingEventHandler);
            _disruptor.SetDefaultExceptionHandler(new FatalExceptionHandler());
        }

        [TestMethod]
        public void ShouldShutdownGracefulEvenWithFatalExceptionHandler()
        {
            var task = Task.Run(() =>
            {
                _disruptor.Start();

                for (var i = 1; i < 10; i++)
                {
                    var bytes = new byte[32];
                    _random.NextBytes(bytes);
                    _disruptor.PublishEvent(new ByteArrayTranslator(bytes));
                }
            });

            Assert.IsTrue(task.Wait(1000));
        }

        public class ByteArrayTranslator : IEventTranslator<byte[]>
        {
            private readonly byte[] _bytes;

            public ByteArrayTranslator(byte[] bytes)
            {
                _bytes = bytes;
            }

            public void TranslateTo(byte[] eventData, long sequence)
            {
                _bytes.CopyTo(eventData, 0);
            }
        }

        [TestMethod]
        public void Teardown()
        {
            _disruptor.Shutdown();
        }


        private class FailingEventHandler : IEventHandler<byte[]>
        {
            private int _count = 0;

            public void OnEvent(byte[] data, long sequence, bool endOfBatch)
            {
                _count++;
                if (_count == 3)
                {
                    throw new InvalidOperationException();
                }
            }
        }


    }
}
