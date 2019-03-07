namespace Disruptor.EventProcessor
{
    /// <summary>
    /// BatchStartAware
    /// </summary>
    public struct BatchStartAware : IBatchStartAware
    {
        private readonly IBatchStartAware _eventHandler;

        public BatchStartAware(object eventHandler)
        {
            _eventHandler = eventHandler as IBatchStartAware;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchSize"></param>
        public void OnBatchStart(long batchSize)
        {
            _eventHandler?.OnBatchStart(batchSize);
        }

    }
}
