using Disruptor.Tests.Support;
using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Disruptor.Tests.DisruptorStressTest;

namespace Disruptor.Tests
{
    [TestClass]
    public class SequenceGroupTests
    {
        /// <summary>
        /// 空SequenceGroup应该返回最大的序号
        /// </summary>
        [TestMethod]
        public void ShouldReturnMaxSequenceWhenEmptyGroup()
        {
            var sequenceGroup = new SequenceGroup();
            Assert.AreEqual(long.MaxValue, sequenceGroup.Get());
        }

        [TestMethod]
        public void ShouldAddOneSequenceToGroup()
        {
            var sequence = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();

            sequenceGroup.Add(sequence);

            Assert.AreEqual(sequence.Get(), sequenceGroup.Get());
        }

        /// <summary>
        /// 应该能够成功移除不存在的序号
        /// </summary>
        [TestMethod]
        public void ShouldNotFailIfTryingToRemoveNotExistingSequence()
        {
            var group = new SequenceGroup();
            group.Add(new Sequence());
            group.Add(new Sequence());
            group.Remove(new Sequence());
        }

        /// <summary>
        /// 应该返回最小的序号
        /// </summary>
        [TestMethod]
        public void ShouldReportTheMinimumSequenceForGroupOfTwo()
        {
            var sequenceThree = new Sequence(3L);
            var sequenceSeven = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();

            sequenceGroup.Add(sequenceSeven);
            sequenceGroup.Add(sequenceThree);

            Assert.AreEqual(sequenceThree.Get(), sequenceGroup.Get());
        }

        /// <summary>
        /// 获取SequenceGroup的Size
        /// </summary>
        [TestMethod]
        public void ShouldReportSizeOfGroup()
        {
            var sequenceGroup = new SequenceGroup();
            sequenceGroup.Add(new Sequence());
            sequenceGroup.Add(new Sequence());
            sequenceGroup.Add(new Sequence());

            Assert.AreEqual(3, sequenceGroup.Size());
        }

        /// <summary>
        /// 应该能够成功移除存在的序号
        /// </summary>
        [TestMethod]
        public void ShouldRemoveSequenceFromGroup()
        {
            var sequenceThree = new Sequence(3L);
            var sequenceSeven = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();

            sequenceGroup.Add(sequenceSeven);
            sequenceGroup.Add(sequenceThree);

            //Assert.Equal(2, sequenceGroup.Size());
            Assert.AreEqual(sequenceThree.Get(), sequenceGroup.Get());

            Assert.IsTrue(sequenceGroup.Remove(sequenceThree));
            Assert.AreEqual(sequenceSeven.Get(), sequenceGroup.Get());
            Assert.AreEqual(1, sequenceGroup.Size());
        }

        /// <summary>
        /// 删除序号时，应该删除值相同的所有序号
        /// </summary>
        [TestMethod]
        public void ShouldRemoveSequenceFromGroupWhereItBeenAddedMultipleTimes()
        {
            var sequenceThree = new Sequence(3L);
            var sequenceSeven = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();

            sequenceGroup.Add(sequenceThree);
            sequenceGroup.Add(sequenceSeven);
            sequenceGroup.Add(sequenceThree);

            Assert.AreEqual(sequenceThree.Get(), sequenceGroup.Get());

            Assert.IsTrue(sequenceGroup.Remove(sequenceThree));
            Assert.AreEqual(sequenceSeven.Get(), sequenceGroup.Get());
            Assert.AreEqual(1, sequenceGroup.Size());
        }

        /// <summary>
        /// 给序号组赋值，该组下所有序号的值都应该相同
        /// </summary>
        [TestMethod]
        public void ShouldSetGroupSequenceToSameValue()
        {
            var sequenceThree = new Sequence(3L);
            var sequenceSeven = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();

            sequenceGroup.Add(sequenceSeven);
            sequenceGroup.Add(sequenceThree);

            const long expectedSequence = 11L;
            sequenceGroup.Set(expectedSequence);

            Assert.AreEqual(expectedSequence, sequenceThree.Get());
            Assert.AreEqual(expectedSequence, sequenceSeven.Get());
        }

        /// <summary>
        /// 在线程开始发布到Disruptor后，添加序列到序列组
        /// </summary>
        [TestMethod]
        public void ShouldAddWhileRunning()
        {
            var ringBuffer = RingBuffer<TestEvent>.CreateSingleProducer(TestEvent.EventFactory, 32);
            var sequenceThree = new Sequence(3L);
            var sequenceSeven = new Sequence(7L);
            var sequenceGroup = new SequenceGroup();
            sequenceGroup.Add(sequenceSeven);

            for (var i = 0; i < 11; i++)
            {
                ringBuffer.Publish(ringBuffer.Next());
            }

            sequenceGroup.AddWhileRunning(ringBuffer, sequenceThree);
            Assert.AreEqual(sequenceThree.Get(), (10L));
        }

    }
}