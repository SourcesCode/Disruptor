﻿namespace Disruptor.UnitTest.Support
{
    public class DummySequenceBarrier : ISequenceBarrier
    {
        //public bool IsAlerted => false;

        public void Alert()
        {
        }

        public void CheckAlert()
        {
        }

        public void ClearAlert()
        {
        }

        public long GetCursor()
        {
            return 0;
        }

        public long WaitFor(long sequence)
        {
            return 0;
        }

        public bool IsAlerted()
        {
            return false;
        }

    }
}
