using System;
using System.Threading;
using Disruptor.UnitTest.Support;

namespace Disruptor.UnitTest.Support
{
    public class DelayedEventHandler : IEventHandler<TestEvent>, ILifecycleAware
    {
        private int _readyToProcessEvent;
        private volatile bool _stopped;
        private readonly Barrier _barrier;

        private DelayedEventHandler(Barrier barrier)
        {
            _barrier = barrier;
        }

        public DelayedEventHandler() : this(new Barrier(2))
        {
        }

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            WaitForAndSetFlag(0);
        }

        public void ProcessEvent()
        {
            WaitForAndSetFlag(1);
        }

        public void StopWaiting()
        {
            _stopped = true;
        }

        private void WaitForAndSetFlag(int newValue)
        {
            while (!_stopped && Thread.CurrentThread.IsAlive && Interlocked.Exchange(ref _readyToProcessEvent, newValue) == newValue)
            {
                Thread.Yield();
            }
        }

        public void OnStart()
        {
            try
            {
                _barrier.SignalAndWait();
            }
            catch (ThreadInterruptedException e)
            {
                throw new ApplicationException("", e);
            }
            catch (BarrierPostPhaseException ex)
            {
                throw new ApplicationException("", ex);
            }
            catch (Exception e)
            {
                throw new RuntimeException(e);
            }
        }

        public void OnShutdown()
        {
        }

        public void AwaitStart()
        {
            _barrier.SignalAndWait();
        }
    }
}