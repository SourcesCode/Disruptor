using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class SingleProducerSequencerTests
    {
        [TestMethod]
        public void ShouldNotUpdateCursorDuringHasAvailableCapacity()
        {
            var sequencer = new SingleProducerSequencer(16, new BusySpinWaitStrategy());

            for (int i = 0; i < 32; i++)
            {
                var next = sequencer.Next();
                Assert.AreNotEqual(sequencer.GetCursor(), (next));

                sequencer.HasAvailableCapacity(13);
                Assert.AreNotEqual(sequencer.GetCursor(), (next));

                sequencer.Publish(next);
            }
        }
    }
}
