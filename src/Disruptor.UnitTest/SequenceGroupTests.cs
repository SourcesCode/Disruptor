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
        /// ��SequenceGroupӦ�÷����������
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
        /// Ӧ���ܹ��ɹ��Ƴ������ڵ����
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
        /// Ӧ�÷�����С�����
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
        /// ��ȡSequenceGroup��Size
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
        /// Ӧ���ܹ��ɹ��Ƴ����ڵ����
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
        /// ɾ�����ʱ��Ӧ��ɾ��ֵ��ͬ���������
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
        /// ������鸳ֵ��������������ŵ�ֵ��Ӧ����ͬ
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
        /// ���߳̿�ʼ������Disruptor��������е�������
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