namespace Disruptor
{
    /// <summary>
    /// Implementors of this interface must provide a single long value
    /// that represents their current cursor value.  Used during dynamic
    /// add/remove of Sequences from a SequenceGroups.AddSequences.
    /// </summary>
    public interface ICursored
    {
        /// <summary>
        /// Get the current cursor value.
        /// 用来获取当前游标的位置，也就是用来获取当前生产者的实时位置。
        /// </summary>
        /// <returns>current cursor value.</returns>
        long GetCursor();

    }
}
