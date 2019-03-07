using System;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Disruptor.Tests.RingBufferEqualsConstraint;
using Disruptor.UnitTest.Support;
using System.Collections.Generic;
using Disruptor.UnitTest.Support.EventFactorys;

#pragma warning disable 618, 612

namespace Disruptor.Tests
{
    [TestClass]
    public class RingBufferTests
    {
        private readonly RingBuffer<StubEvent> _ringBuffer;
        private readonly ISequenceBarrier _sequenceBarrier;


        public RingBufferTests()
        {
            _ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 32);
            _sequenceBarrier = _ringBuffer.NewBarrier();
            _ringBuffer.AddGatingSequences(new NoOpEventProcessor<StubEvent>(_ringBuffer).GetSequence());
        }

        /// <summary>
        /// 应该能发布和获取事件
        /// </summary>
        [TestMethod]
        public void Should_Publish_And_Get()
        {
            Assert.AreEqual(Sequence.INITIAL_VALUE, _ringBuffer.GetCursor());

            var expectedEvent = new StubEvent(2701);
            _ringBuffer.PublishEvent(StubEvent.Translator, expectedEvent.Value, expectedEvent.TestString);

            //var claimSequence = _ringBuffer.Next();
            //var oldEvent = _ringBuffer[claimSequence];
            //oldEvent.Copy(expectedEvent);
            //_ringBuffer.Publish(claimSequence);

            var sequence = _sequenceBarrier.WaitFor(0L);
            Assert.AreEqual(0L, sequence);

            var evt = _ringBuffer.Get(sequence);
            Assert.AreEqual(expectedEvent, evt);

            Assert.AreEqual(0L, _ringBuffer.GetCursor());
        }

        /// <summary>
        /// 在一个单独的线程里，应该能发布和获取事件
        /// </summary>
        [TestMethod]
        public void ShouldClaimAndGetInSeparateThread()
        {
            var events = GetMessages(0, 0);

            var expectedEvent = new StubEvent(2701);

            //var sequence = _ringBuffer.Next();
            //var oldEvent = _ringBuffer[sequence];
            //oldEvent.Copy(expectedEvent);
            _ringBuffer.PublishEvent(StubEvent.Translator, expectedEvent.Value, expectedEvent.TestString);

            Assert.AreEqual(expectedEvent, events.Result[0]);
        }

        /// <summary>
        /// 应该能发布和获取多个事件
        /// </summary>
        [TestMethod]
        public void ShouldClaimAndGetMultipleMessages()
        {
            var numEvents = _ringBuffer.GetBufferSize();
            for (var i = 0; i < numEvents; i++)
            {
                _ringBuffer.PublishEvent(StubEvent.Translator, i, "");
            }

            var expectedSequence = numEvents - 1;
            var available = _sequenceBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = 0; i < numEvents; i++)
            {
                Assert.AreEqual(i, _ringBuffer.Get(i).Value);
            }
        }

        /// <summary>
        /// 门控序号跟上消费序号，应该能够无限持续发布事件
        /// </summary>
        [TestMethod]
        public void ShouldWrap()
        {
            var numEvents = _ringBuffer.GetBufferSize();
            const int offset = 1000;
            for (var i = 0; i < numEvents + offset; i++)
            {
                _ringBuffer.PublishEvent(StubEvent.Translator, i, "");
            }

            var expectedSequence = numEvents + offset - 1;
            var available = _sequenceBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);

            for (var i = offset; i < numEvents + offset; i++)
            {
                Assert.AreEqual(i, _ringBuffer.Get(i).Value);
            }
        }

        /// <summary>
        /// 门控序号未跟上消费序号，只能发布ringBuffer容量（包装点=消费序号-门控序号）长度的事件。
        /// </summary>
        [TestMethod]
        public void ShouldPreventWrapping()
        {
            var sequence = new Sequence();
            var ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 4);
            ringBuffer.AddGatingSequences(sequence);

            ringBuffer.PublishEvent(StubEvent.Translator, 0, "0");
            ringBuffer.PublishEvent(StubEvent.Translator, 1, "1");
            ringBuffer.PublishEvent(StubEvent.Translator, 2, "2");
            ringBuffer.PublishEvent(StubEvent.Translator, 3, "3");

            Assert.IsFalse(ringBuffer.TryPublishEvent(StubEvent.Translator, 3, "3"));
        }

        /// <summary>
        /// 当buffer装满时，应该无法再次申请序号，并抛出异常
        /// </summary>
        [TestMethod]
        public void ShouldReturnFalseIfBufferIsFull()
        {
            _ringBuffer.AddGatingSequences(new Sequence(_ringBuffer.GetBufferSize()));

            for (var i = 0; i < _ringBuffer.GetBufferSize(); i++)
            {
                //var succeeded = _ringBuffer.TryNext(out var n);
                //Assert.IsTrue(succeeded);

                var n = _ringBuffer.TryNext();
                _ringBuffer.Publish(n);
            }

            Assert.ThrowsException<InsufficientCapacityException>(() =>
            {
                var sequence = _ringBuffer.TryNext();
            });

        }

        [TestMethod]
        public void ShouldPreventProducersOvertakingEventProcessorsWrapPoint()
        {
            const int ringBufferSize = 4;//16
            var mre = new ManualResetEvent(false);
            var producerComplete = false;
            var ringBuffer = new RingBuffer<StubEvent>(StubEvent.EventFactory, ringBufferSize);
            var processor = new TestEventProcessor(ringBuffer.NewBarrier());
            ringBuffer.AddGatingSequences(processor.GetSequence());

            var thread = new Thread(() =>
            {
                for (var i = 0; i <= ringBufferSize; i++) // produce 5 events
                {
                    var sequence = ringBuffer.Next();
                    var evt = ringBuffer.Get(sequence);
                    evt.Value = i;
                    ringBuffer.Publish(sequence);

                    if (i == 3) // unblock main thread after 4th eventData published
                    {
                        mre.Set();
                    }
                }

                producerComplete = true;
            });

            thread.Start();

            mre.WaitOne();
            Assert.AreEqual(ringBuffer.GetCursor(), (ringBufferSize - 1));
            Assert.IsFalse(producerComplete);

            processor.Run();
            thread.Join();

            Assert.IsTrue(producerComplete);
        }

        /// <summary>
        /// 应该能够发布无参事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEvent()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            ringBuffer.PublishEvent(translator);
            ringBuffer.TryPublishEvent(translator);

            //var matcher = new RingBufferEventMatcher(ringBuffer);
            //Assert.IsTrue(matcher.RingBufferWithEvents(new object[1] { 0L }, new object[1] { 1L }));
            Assert.IsTrue(RingBufferWithEvents(ringBuffer, 0L, 1L));
        }

        /// <summary>
        /// 应该能够发布带一个参数的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventOneArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            ringBuffer.PublishEvent(translator, "Foo");
            ringBuffer.TryPublishEvent(translator, "Foo");

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo-0", "Foo-1"));
        }

        /// <summary>
        /// 应该能够发布带两个参数的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventTwoArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvent(translator, "Foo", "Bar");
            ringBuffer.TryPublishEvent(translator, "Foo", "Bar");

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBar-0", "FooBar-1"));
        }

        /// <summary>
        /// 应该能够发布带三个参数的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventThreeArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            ringBuffer.PublishEvent(translator, "Foo", "Bar", "Baz");
            ringBuffer.TryPublishEvent(translator, "Foo", "Bar", "Baz");

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBarBaz-0", "FooBarBaz-1"));
        }

        /// <summary>
        /// 应该能够发布带可变参数的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventVarArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorVararg<object[]> translator = new VarArgEventTranslator();

            //ringBuffer.PublishEvent(translator, "Foo", "Bar", "Baz", "Bam");
            //ringBuffer.TryPublishEvent(translator, "Foo", "Bar", "Baz", "Bam");

            //Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBarBazBam-0", "FooBarBazBam-1"));

            ringBuffer.PublishEvent(translator, "Arg0");
            ringBuffer.TryPublishEvent(translator, "Arg0", "Arg1");
            ringBuffer.PublishEvent(translator, "Arg0", "Arg1", "Arg2");
            ringBuffer.TryPublishEvent(translator, "Arg0", "Arg1", "Arg2", "Arg3");

            var matcher = new RingBufferEventMatcher(ringBuffer);
            Assert.IsTrue(RingBufferWithEvents(ringBuffer,
                new object[1] { "Arg0-0" },
                new object[1] { "Arg0Arg1-1" },
                new object[1] { "Arg0Arg1Arg2-2" },
                new object[1] { "Arg0Arg1Arg2Arg3-3" }));

        }

        /// <summary>
        /// 应该能够批量发布事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEvents()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> eventTranslator = new NoArgEventTranslator();
            var translators = new[] { eventTranslator, eventTranslator };

            ringBuffer.PublishEvents(translators);
            Assert.IsTrue(ringBuffer.TryPublishEvents(translators));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, 0L, 1L, 2L, 3L));
        }

        /// <summary>
        /// 如果批量大于RingBuffer的容量，不应该发布事件。
        /// </summary>
        [TestMethod]
        public void ShouldNotPublishEventsIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new NoArgEventTranslator();
            var translators = new[] { translator, translator, translator, translator, translator };

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translators));
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translators));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        /// <summary>
        /// 应该能够发布具有批量大小的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventsWithBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> eventTranslator = new NoArgEventTranslator();
            var translators =
                new[] { eventTranslator, eventTranslator, eventTranslator };

            ringBuffer.PublishEvents(translators, 0, 1);
            Assert.IsTrue(ringBuffer.TryPublishEvents(translators, 0, 1));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer,
                new object[1] { 0L },
                new object[1] { 1L }));
            //Assert.IsTrue(RingBufferWithEvents(ringBuffer, 0L, 1L,null,null));
        }

        /// <summary>
        /// 应该能够发布批次范围内的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventsWithinBatch()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> eventTranslator = new NoArgEventTranslator();
            var translators = new[] { eventTranslator, eventTranslator, eventTranslator };

            ringBuffer.PublishEvents(translators, 1, 2);
            Assert.IsTrue(ringBuffer.TryPublishEvents(translators, 1, 2));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, 0L, 1L, 2L, 3L));
        }

        /// <summary>
        /// 应该能够批量发布带一个参数的事件
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventsOneArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            ringBuffer.PublishEvents(translator, new[] { "Foo", "Foo" });
            Assert.IsTrue(ringBuffer.TryPublishEvents(translator, new[] { "Foo", "Foo" }));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo-0", "Foo-1", "Foo-2", "Foo-3"));
        }

        [TestMethod]
        public void ShouldNotPublishEventsOneArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, new[] { "Foo", "Foo", "Foo", "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldPublishEventsOneArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            ringBuffer.PublishEvents(translator, 0, 1, new[] { "Foo", "Foo" });
            Assert.IsTrue(ringBuffer.TryPublishEvents(translator, 0, 1, new[] { "Foo", "Foo" }));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo-0", "Foo-1", null, null));
        }

        [TestMethod]
        public void ShouldPublishEventsOneArgWithinBatch()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            ringBuffer.PublishEvents(translator, 1, 2, new[] { "Foo", "Foo", "Foo" });
            Assert.IsTrue(ringBuffer.TryPublishEvents(translator, 1, 2, new[] { "Foo", "Foo", "Foo" }));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo-0", "Foo-1", "Foo-2", "Foo-3"));
        }

        [TestMethod]
        public void ShouldPublishEventsTwoArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvents(translator, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" });
            ringBuffer.TryPublishEvents(translator, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" });

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBar-0", "FooBar-1", "FooBar-2", "FooBar-3"));
        }

        [TestMethod]
        public void ShouldNotPublishEventsITwoArgIfBatchSizeIsBiggerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                    translator,
                    new[] { "Foo", "Foo", "Foo", "Foo", "Foo" },
                    new[] { "Bar", "Bar", "Bar", "Bar", "Bar" }));

            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldPublishEventsTwoArgWithBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvents(translator, 0, 1, new[] { "Foo0", "Foo1" }, new[] { "Bar0", "Bar1" });
            ringBuffer.TryPublishEvents(translator, 0, 1, new[] { "Foo2", "Foo3" }, new[] { "Bar2", "Bar3" });

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo0Bar0-0", "Foo2Bar2-1", null, null));
        }

        [TestMethod]
        public void ShouldPublishEventsTwoArgWithinBatch()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, 1, 2, new[] { "Foo0", "Foo1", "Foo2" }, new[] { "Bar0", "Bar1", "Bar2" });
            ringBuffer.TryPublishEvents(
                translator, 1, 2, new[] { "Foo3", "Foo4", "Foo5" }, new[] { "Bar3", "Bar4", "Bar5" });

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo1Bar1-0", "Foo2Bar2-1", "Foo4Bar4-2", "Foo5Bar5-3"));
        }

        /// <summary>
        /// 看到这个地方
        /// </summary>
        [TestMethod]
        public void ShouldPublishEventsThreeArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" });
            ringBuffer.TryPublishEvents(
                translator, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" });

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBarBaz-0", "FooBarBaz-1", "FooBarBaz-2", "FooBarBaz-3"));
        }

        [TestMethod]
        public void ShouldNotPublishEventsThreeArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                    translator,
                    new[] { "Foo", "Foo", "Foo", "Foo", "Foo" },
                    new[] { "Bar", "Bar", "Bar", "Bar", "Bar" },
                    new[] { "Baz", "Baz", "Baz", "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldPublishEventsThreeArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, 0, 1, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" });
            ringBuffer.TryPublishEvents(
                translator, 0, 1, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" });

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBarBaz-0", "FooBarBaz-1", null, null));
        }

        [TestMethod]
        public void ShouldPublishEventsThreeArgWithinBatch()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, 1, 2, new[] { "Foo0", "Foo1", "Foo2" }, new[] { "Bar0", "Bar1", "Bar2" },
                new[] { "Baz0", "Baz1", "Baz2" }
                );
            Assert.IsTrue(
                ringBuffer.TryPublishEvents(
                    translator, 1, 2, new[] { "Foo3", "Foo4", "Foo5" }, new[] { "Bar3", "Bar4", "Bar5" },
                    new[] { "Baz3", "Baz4", "Baz5" }));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "Foo1Bar1Baz1-0", "Foo2Bar2Baz2-1", "Foo4Bar4Baz4-2", "Foo5Bar5Baz5-3"));
        }

        [TestMethod]
        public void ShouldPublishEventsVarArg()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorVararg<object[]> translator = new VarArgEventTranslator();

            ringBuffer.PublishEvents(translator, new[] { "Foo", "Bar", "Baz", "Bam" }, new[] { "Foo", "Bar", "Baz", "Bam" });
            Assert.IsTrue(ringBuffer.TryPublishEvents(translator, new[] { "Foo", "Bar", "Baz", "Bam" }, new[] { "Foo", "Bar", "Baz", "Bam" }));

            Assert.IsTrue(RingBufferWithEvents(ringBuffer, "FooBarBazBam-0", "FooBarBazBam-1", "FooBarBazBam-2", "FooBarBazBam-3"));
        }

        [TestMethod]
        public void ShouldNotPublishEventsVarArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorVararg<object[]> translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                    translator,
                    new[] { "Foo", "Bar", "Baz", "Bam" },
                    new[] { "Foo", "Bar", "Baz", "Bam" },
                    new[] { "Foo", "Bar", "Baz", "Bam" },
                    new[] { "Foo", "Bar", "Baz", "Bam" },
                    new[] { "Foo", "Bar", "Baz", "Bam" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldPublishEventsVarArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorVararg<object[]> translator = new VarArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, 0, 1, new object[] { "Foo", "Bar", "Baz", "Bam" }, new object[] { "Foo", "Bar", "Baz", "Bam" });
            Assert.IsTrue(
                ringBuffer.TryPublishEvents(
                    translator, 0, 1, new object[] { "Foo", "Bar", "Baz", "Bam" }, new object[] { "Foo", "Bar", "Baz", "Bam" }));

            Assert.AreEqual(
                ringBuffer, RingBufferWithEvents("FooBarBazBam-0", "FooBarBazBam-1", null, null));
        }

        [TestMethod]
        public void ShouldPublishEventsVarArgWithinBatch()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorVararg<object[]> translator = new VarArgEventTranslator();

            ringBuffer.PublishEvents(
                translator, 1, 2, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                new object[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                new object[] { "Foo2", "Bar2", "Baz2", "Bam2" });
            Assert.IsTrue(
                ringBuffer.TryPublishEvents(
                    translator, 1, 2, new object[] { "Foo3", "Bar3", "Baz3", "Bam3" },
                    new object[] { "Foo4", "Bar4", "Baz4", "Bam4" },
                    new object[] { "Foo5", "Bar5", "Baz5", "Bam5" }));

            Assert.AreEqual(
                ringBuffer, RingBufferWithEvents(
                    "Foo1Bar1Baz1Bam1-0", "Foo2Bar2Baz2Bam2-1", "Foo4Bar4Baz4Bam4-2", "Foo5Bar5Baz5Bam5-3"));
        }

        [TestMethod]
        public void ShouldNotPublishEventsWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(new[] { translator, translator, translator, translator }, 1, 0));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(new[] { translator, translator, translator, translator }, 1, 0));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(new[] { translator, translator, translator }, 1, 3));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(new[] { translator, translator, translator }, 1, 3));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(new[] { translator, translator, translator, translator }, 1, -1));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(new[] { translator, translator, translator, translator }, 1, -1));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();
            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(new[] { translator, translator, translator, translator }, -1, 2));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslator<object[]> translator = new NoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(new[] { translator, translator, translator, translator }, -1, 2));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsOneArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, 1, 0, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsOneArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, 1, 0, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsOneArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, 1, 3, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }
        [TestMethod]
        public void ShouldNotTryPublishEventsOneArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, 1, 3, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }


        [TestMethod]
        public void ShouldNotPublishEventsOneArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, 1, -1, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsOneArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, 1, -1, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        /// <summary>
        /// -----------
        /// </summary>
        [TestMethod]
        public void ShouldNotPublishEventsOneArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();
            try
            {
                //-1, 1,
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, -1, 2, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }
        [TestMethod]
        public void ShouldNotTryPublishEventsOneArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorOneArg<object[], string> translator = new OneArgEventTranslator();

            try
            {
                //-1, 1,
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, -1, 2, new[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsTwoArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, 0, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, 0, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsTwoArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, 1, 3, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }
        [TestMethod]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, 1, 3, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsTwoArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, 1, -1, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, 1, -1, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsTwoArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();
            try
            {
                //-1 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(translator, -1, 2, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorTwoArg<object[], string, string> translator = new TwoArgEventTranslator();

            try
            {
                //-1 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(translator, -1, 2, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsThreeArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, 0, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, 0, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsThreeArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, 3, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" },
                                                     new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, 3, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" },
                                                     new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsThreeArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, -1, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" },
                                                     new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, -1, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsThreeArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                //-1 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, -1, 2, new[] { "Foo", "Foo" }, new[] { "Bar", "Bar" },
                                                     new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            IEventTranslatorThreeArg<object[], string, string, string> translator = new ThreeArgEventTranslator();

            try
            {
                //-1 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, -1, 2, new[] { "Foo", "Foo" },
                                                     new[] { "Bar", "Bar" }, new[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsVarArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, 0, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                                                     new object[] { "Foo2", "Bar2", "Baz2", "Bam2" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsVarArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, 0, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                                                     new object[] { "Foo2", "Bar2", "Baz2", "Bam2" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsVarArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, 3, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new object[]
                                                     {
                                                         "Foo2", "Bar2",
                                                         "Baz2", "Bam2"
                                                     }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsVarArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, 3, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new object[]
                                                     {
                                                         "Foo2", "Bar2",
                                                         "Baz2", "Bam2"
                                                     }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsVarArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                                                     translator, 1, -1, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new object[]
                                                     {
                                                         "Foo2", "Bar2",
                                                         "Baz2", "Bam2"
                                                     }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsVarArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                                                     translator, 1, -1, new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                     new object[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new object[]
                                                     {
                                                         "Foo2", "Bar2",
                                                         "Baz2", "Bam2"
                                                     }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotPublishEventsVarArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                // -1, 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.PublishEvents(
                    translator, -1, 2,
                    new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                    new object[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                    new object[] { "Foo2", "Bar2", "Baz2", "Bam2" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldNotTryPublishEventsVarArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 4);
            var translator = new VarArgEventTranslator();

            try
            {
                // -1, 1
                Assert.ThrowsException<ArgumentException>(() => ringBuffer.TryPublishEvents(
                    translator, -1, 2,
                    new object[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                    new object[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                    new object[] { "Foo2", "Bar2", "Baz2", "Bam2" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [TestMethod]
        public void ShouldAddAndRemoveSequences()
        {
            var ringBuffer = RingBuffer<object[]>.CreateSingleProducer(new ArrayFactory(1), 16);

            var sequenceThree = new Sequence(-1);
            var sequenceSeven = new Sequence(-1);
            ringBuffer.AddGatingSequences(sequenceThree, sequenceSeven);

            for (var i = 0; i < 10; i++)
            {
                ringBuffer.Publish(ringBuffer.Next());
            }

            sequenceThree.Set(3);
            sequenceSeven.Set(7);

            Assert.AreEqual(ringBuffer.GetMinimumGatingSequence(), (3L));
            Assert.IsTrue(ringBuffer.RemoveGatingSequence(sequenceThree));
            Assert.AreEqual(ringBuffer.GetMinimumGatingSequence(), (7L));
        }

        [TestMethod]
        public void ShouldHandleResetToAndNotWrapUnnecessarilySingleProducer()
        {
            AssertHandleResetAndNotWrap(RingBuffer<StubEvent>.CreateSingleProducer(StubEvent.EventFactory, 4));
        }

        [TestMethod]
        public void ShouldHandleResetToAndNotWrapUnnecessarilyMultiProducer()
        {
            AssertHandleResetAndNotWrap(RingBuffer<StubEvent>.CreateMultiProducer(StubEvent.EventFactory, 4));
        }

        private static void AssertHandleResetAndNotWrap(RingBuffer<StubEvent> rb)
        {
            var sequence = new Sequence();
            rb.AddGatingSequences(sequence);

            for (var i = 0; i < 128; i++)
            {
                rb.Publish(rb.Next());
                sequence.IncrementAndGet();
            }

            Assert.AreEqual(rb.GetCursor(), (127L));

            rb.ResetTo(31);
            sequence.Set(31);

            for (var i = 0; i < 4; i++)
            {
                rb.Publish(rb.Next());
            }

            Assert.AreEqual(rb.HasAvailableCapacity(1), (false));
        }

        private Task<List<StubEvent>> GetMessages(long initial, long toWaitFor)
        {
            var barrier = new Barrier(2);
            var dependencyBarrier = _ringBuffer.NewBarrier();

            var testWaiter = new TestWaiter(barrier, dependencyBarrier, _ringBuffer, initial, toWaitFor);
            var task = Task.Factory.StartNew(() => testWaiter.Call());

            barrier.SignalAndWait();

            return task;
        }

        private void AssertEmptyRingBuffer(RingBuffer<object[]> ringBuffer)
        {
            for (var i = 0; i < ringBuffer.GetBufferSize(); i++)
            {
                Assert.IsNull(ringBuffer.Get(i)[0]);
            }
        }

        private class NoArgEventTranslator : IEventTranslator<object[]>
        {
            public void TranslateTo(object[] eventData, long sequence)
            {
                eventData[0] = sequence;
            }
        }

        private class VarArgEventTranslator : IEventTranslatorVararg<object[]>
        {
            public void TranslateTo(object[] eventData, long sequence, params object[] args)
            {
                eventData[0] = (string)args[0] + args[1] + args[2] + args[3] + "-" + sequence;
            }
        }

        private class ThreeArgEventTranslator : IEventTranslatorThreeArg<object[], string, string, string>
        {
            public void TranslateTo(object[] eventData, long sequence, string arg0, string arg1, string arg2)
            {
                eventData[0] = arg0 + arg1 + arg2 + "-" + sequence;
            }
        }

        private class TwoArgEventTranslator : IEventTranslatorTwoArg<object[], string, string>
        {
            public void TranslateTo(object[] eventData, long sequence, string arg0, string arg1)
            {
                eventData[0] = arg0 + arg1 + "-" + sequence;
            }
        }

        private class OneArgEventTranslator : IEventTranslatorOneArg<object[], string>
        {
            public void TranslateTo(object[] eventData, long sequence, string arg0)
            {
                eventData[0] = arg0 + "-" + sequence;
            }
        }

        private class TestEventProcessor : IEventProcessor
        {
            private readonly ISequenceBarrier _barrier;
            private readonly ISequence _sequence = new Sequence();

            private int _running;

            public TestEventProcessor(ISequenceBarrier barrier)
            {
                _barrier = barrier;
            }

            public void Run()
            {
                if (Interlocked.Exchange(ref _running, 1) == 1)
                {
                    throw new IllegalStateException("Thread is already running");
                }

                try
                {
                    _barrier.WaitFor(0L);
                }
                catch (Exception ex)
                {
                    throw new RuntimeException(ex);
                }

                _sequence.Set(0L);
            }

            public ISequence GetSequence()
            {
                return _sequence;
            }

            public void Halt()
            {
                Interlocked.Exchange(ref _running, 0);
            }

            public bool IsRunning()
            {
                return _running == 1;
            }
        }

    }
}
