using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Disruptor.UnitTest.Support
{
    public class WaitStrategyTestUtil
    {
        public static void AssertWaitForWithDelayOf(int sleepTimeMillis, IWaitStrategy waitStrategy)
        {
            var sequenceUpdater = new SequenceUpdater(sleepTimeMillis, waitStrategy);
            Task.Run(() => sequenceUpdater.Run());
            sequenceUpdater.WaitForStartup();
            var cursor = new Sequence(0);
            var sequence = waitStrategy.WaitFor(0, cursor, sequenceUpdater.Sequence, new DummySequenceBarrier());

            Assert.AreEqual(0L, sequence);
        }
    }
}
