using System;
using System.Threading;

namespace Disruptor.UnitTest.Support
{
    public class SequenceUpdater
    {
        public readonly ISequence Sequence = new Sequence();
        private readonly CountdownEvent _barrier = new CountdownEvent(2);
        private readonly int _sleepTime;
        private readonly IWaitStrategy _waitStrategy;

        public SequenceUpdater(int sleepTime, IWaitStrategy waitStrategy)
        {
            _sleepTime = sleepTime;
            _waitStrategy = waitStrategy;
        }

        public void Run()
        {
            try
            {
                _barrier.Signal();
                _barrier.Wait();
                if (0 != _sleepTime)
                {
                    Thread.Sleep(_sleepTime);
                }
                Sequence.IncrementAndGet();
                _waitStrategy.SignalAllWhenBlocking();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void WaitForStartup()
        {
            _barrier.Signal();
            _barrier.Wait();
        }
    }
}