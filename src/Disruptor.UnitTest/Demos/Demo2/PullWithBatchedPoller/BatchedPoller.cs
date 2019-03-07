using Disruptor.UnitTest.Demos.Demo2;
using System;

namespace Disruptor.Tests.Example.PullWithBatchedPoller
{
    public class BatchedPoller<T> where T : class
    {
        private readonly EventPoller<DataEvent<T>> _poller;
        private readonly int _maxBatchSize;
        private readonly BatchedData _polledData;

        public BatchedPoller(RingBuffer<DataEvent<T>> ringBuffer, int batchSize)
        {
            _poller = ringBuffer.NewPoller();
            ringBuffer.AddGatingSequences(_poller.GetSequence());

            if (batchSize < 1)
            {
                batchSize = 20;
            }
            _maxBatchSize = batchSize;
            _polledData = new BatchedData(_maxBatchSize);
        }

        public T Poll()
        {
            if (_polledData.GetMsgCount() > 0)
            {
                return _polledData.PollMessage(); // we just fetch from our local
            }

            LoadNextValues(_poller, _polledData); // we try to load from the ring
            return _polledData.GetMsgCount() > 0 ? _polledData.PollMessage() : null;
        }

        private EventPoller<DataEvent<T>>.PollState LoadNextValues(EventPoller<DataEvent<T>> poller, BatchedData batch)
        {
            return poller.Poll(new DataEventHandler(batch));
        }

        public class DataEventHandler : EventPoller<DataEvent<T>>.IHandler<DataEvent<T>>
        {
            private readonly BatchedData _batch;
            public DataEventHandler(BatchedData batch)
            {
                _batch = batch;
            }

            public bool OnEvent(DataEvent<T> @event, long sequence, bool endOfBatch)
            {
                var item = @event.CopyOfData();
                return item != null && _batch.AddDataItem(item);
            }
        }



        public class BatchedData
        {

            private int _msgHighBound;
            private readonly int _capacity;
            private readonly T[] _data;
            private int _cursor;

            public BatchedData(int size)
            {
                _capacity = size;
                _data = new T[_capacity];
            }

            private void ClearCount()
            {
                _msgHighBound = 0;
                _cursor = 0;
            }

            public int GetMsgCount()
            {
                return _msgHighBound - _cursor;
            }

            public bool AddDataItem(T item)
            {
                if (_msgHighBound >= _capacity)
                {
                    throw new ArgumentOutOfRangeException("Attempting to add item to full batch");
                }

                _data[_msgHighBound++] = item;
                return _msgHighBound < _capacity;
            }

            public T PollMessage()
            {
                T rtVal = default(T);
                if (_cursor < _msgHighBound)
                {
                    rtVal = _data[_cursor++];
                }
                if (_cursor > 0 && _cursor >= _msgHighBound)
                {
                    ClearCount();
                }
                return rtVal;
            }
        }
    }

}
