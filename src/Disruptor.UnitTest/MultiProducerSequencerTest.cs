using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class MultiProducerSequencerTest
    {
        private readonly MultiProducerSequencer _publisher = new MultiProducerSequencer(1024, new BlockingWaitStrategy());

        /// <summary>
        /// 已发布的消息应该是可用的
        /// </summary>
        [TestMethod]
        public void ShouldOnlyAllowMessagesToBeAvailableIfSpecificallyPublished()
        {
            _publisher.Publish(3);
            _publisher.Publish(5);

            Assert.AreEqual(_publisher.IsAvailable(0), (false));
            Assert.AreEqual(_publisher.IsAvailable(1), (false));
            Assert.AreEqual(_publisher.IsAvailable(2), (false));
            Assert.AreEqual(_publisher.IsAvailable(3), (true));
            Assert.AreEqual(_publisher.IsAvailable(4), (false));
            Assert.AreEqual(_publisher.IsAvailable(5), (true));
            Assert.AreEqual(_publisher.IsAvailable(6), (false));

        }
    }
}
