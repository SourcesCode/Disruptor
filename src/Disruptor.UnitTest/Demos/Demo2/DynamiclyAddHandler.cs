using System.Threading;
using System.Threading.Tasks;
using Disruptor.Tests.Support;
using Disruptor.Dsl;
using Disruptor.UnitTest.Support;
using Disruptor.UnitTest.Core;

namespace Disruptor.Tests.Example
{
    public class DynamiclyAddHandler
    {
        public void TestMain()
        {
            var executor = new BasicExecutor(TaskScheduler.Current);
            var disruptor = new Disruptor<StubEvent>(StubEvent.EventFactory, 1024, TaskScheduler.Current);
            var ringBuffer = disruptor.Start();

            // Construct 2 batch event processors.
            var handler1 = new DynamicHandler();
            var processor1 = BatchEventProcessorFactory.Create(ringBuffer, ringBuffer.NewBarrier(), handler1);

            var handler2 = new DynamicHandler();
            var processor2 = BatchEventProcessorFactory.Create(ringBuffer, ringBuffer.NewBarrier(processor1.GetSequence()), handler2);

            // Dynamically add both sequences to the ring buffer
            ringBuffer.AddGatingSequences(processor1.GetSequence(), processor2.GetSequence());

            // Start the new batch processors.
            executor.Execute(processor1);
            executor.Execute(processor2);

            // Remove a processor.

            // Stop the processor
            processor2.Halt();
            // Wait for shutdown the complete
            handler2.WaitShutdown();
            // Remove the gating sequence from the ring buffer
            ringBuffer.RemoveGatingSequence(processor2.GetSequence());
        }

        private class DynamicHandler : IEventHandler<StubEvent>, ILifecycleAware
        {
            private readonly ManualResetEvent _shutdownSignal = new ManualResetEvent(false);

            public void OnEvent(StubEvent data, long sequence, bool endOfBatch)
            {
            }

            public void OnStart()
            {
            }

            public void OnShutdown()
            {
                _shutdownSignal.Set();
            }

            public void WaitShutdown()
            {
                _shutdownSignal.WaitOne();
            }
        }
    }
}
