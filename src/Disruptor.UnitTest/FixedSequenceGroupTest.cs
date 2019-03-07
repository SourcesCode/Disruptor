using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Disruptor.Tests
{
    [TestClass]
    public class FixedSequenceGroupTest
    {
        /// <summary>
        /// 返回两个序列组之间最小的序号
        /// </summary>
        [TestMethod]
        public void ShouldReturnMinimumOf2Sequences()
        {
            var sequence1 = new Sequence(34);
            var sequnece2 = new Sequence(47);
            var group = new FixedSequenceGroup(new[] { sequence1, sequnece2 });

            Assert.AreEqual(group.Get(), (34L));
            sequence1.Set(35);
            Assert.AreEqual(group.Get(), (35L));
            sequence1.Set(48);
            Assert.AreEqual(group.Get(), (47L));
        }
    }
}