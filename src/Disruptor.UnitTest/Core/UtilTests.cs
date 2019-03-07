using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests.Utils
{
    [TestClass]
    public class UtilTests
    {
        [TestMethod]
        public void ShouldReturnNextPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1000);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [TestMethod]
        public void ShouldReturnExactPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1024);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [TestMethod]
        public void ShouldReturnMinimumSequence()
        {
            var sequences = new[] { new Sequence(11), new Sequence(4), new Sequence(13) };

            Assert.AreEqual(4L, Util.GetMinimumSequence(sequences));
        }

        [TestMethod]
        public void ShouldReturnLongMaxWhenNoEventProcessors()
        {
            var sequences = new Sequence[0];

            Assert.AreEqual(long.MaxValue, Util.GetMinimumSequence(sequences));
        }
    }
}