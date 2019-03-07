namespace Disruptor
{
    /// <summary>
    /// IEventReleaseAware
    /// </summary>
    public interface IEventReleaseAware
    {
        /// <summary>
        /// SetEventReleaser
        /// </summary>
        /// <param name="eventReleaser"></param>
        void SetEventReleaser(IEventReleaser eventReleaser);

    }
}
