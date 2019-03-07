using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Concurrent sequence class used for tracking the progress of
    /// the ring buffer and event processors.  Support a number
    /// of concurrent operations including CAS and order writes.
    /// 
    /// Also attempts to be more efficient with regards to false
    /// sharing by adding padding around the volatile field.
    /// 
    /// 表示ring buffer的序列号。
    /// 
    /// Sequence是Disruptor最核心的组件。生产者对RingBuffer的互斥访问，
    /// 生产者与消费者之间的协调以及消费者之间的协调，都是通过Sequence实现。
    /// 几乎每一个重要的组件都包含Sequence。那么Sequence是什么呢？
    /// 首先Sequence是一个递增的序号，说白了就是计数器；
    /// 其次，由于需要在线程间共享，所以Sequence是引用传递，并且是线程安全的；
    /// 再次，Sequence支持CAS操作；最后，为了提高效率，Sequence通过padding来避免伪共享
    /// 
    /// 我们看到在Sequence的父类中有两个类，LhsPadding、RhsPadding，
    /// 这两个类都只是各有7个long型的属性，其他都没有了，
    /// 而Value类中只有一个volatile long型的属性，
    /// 其实真正用来存放位置标识的就是这个volatile long型的value，
    /// LhsPadding、RhsPadding中的一共14个long型是围绕在value前后，
    /// 用来做CPU缓存行填充用的，至于什么是缓存行，
    /// 可以参考 http://ifeve.com/disruptor-cacheline-padding/ ，
    /// 而这里为什么要用缓存行填充，则可以参考 http://ifeve.com/falsesharing/ 。
    /// Sequence有一个Unsafe对象（Unsafe对象的基本知识可以
    /// 参考 http://blog.csdn.net/aesop_wubo/article/details/7537278 ），
    /// 可以简单的理解，Unsafe就是用来做原子操作的。
    /// Sequence类的其他set、get等方法都是通过UNSAFE对象实现对value值的原子操作。
    /// 
    /// 总结：
    /// 1、存放生产者、消费者位置的是Sequence类；
    /// 2、对于Sequence的管理是Sequencer，分Single/Multi两种模式；
    /// 3、所有消费者的Sequence都被放在一个gatingSequences数组中，被生产者追踪；
    /// 4、消费者通过Sequencer可以获取一个SequenceBarrier，可以实现对生产者，以及该消费者依赖的序列组进行追踪；
    /// 5、SequenceBarrier中依赖的序列组是其他消费者的序列组，主要用在链式消费上，比如有两个消费者A、B，其中B需要在A之后进行消费，这A的序列就是B需要依赖的序列，因为B的消费速度不能超过A。
    /// 
    /// </summary>
    /// <remarks>通过增加补全来确保ring buffer的序列号不会和其他东西同时存在于一个缓存行中。没有伪共享，就没有和其它任何变量的意外冲突，没有不必要的缓存未命中。</remarks>
    public class Sequence : RhsPadding, ISequence
    {
        /// <summary>
        /// Set to -1 as sequence starting point.
        /// </summary>
        public static readonly long INITIAL_VALUE = -1L;

        /// <summary>
        /// Create a sequence initialised to -1.
        /// </summary>
        public Sequence() : this(INITIAL_VALUE) { }

        /// <summary>
        /// Create a sequence with a specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial value for this sequence.</param>
        public Sequence(long initialValue)
        {
            Value = initialValue;
        }

        /// <summary>
        /// Perform a volatile read of this sequence's value.
        /// 
        /// 读取，写入值都将使用内存栅栏
        /// </summary>
        /// <returns>The current value of the sequence.</returns>
        public virtual long Get()
        {
            return Volatile.Read(ref Value);
        }

        /// <summary>
        /// Perform an ordered write of this sequence. The intent is
        /// a Store/Store barrier between this write and any previous store.
        /// </summary>
        /// <param name="value">The new value for the sequence.</param>
        public virtual void Set(long value)
        {
            Value = value;
        }

        /// <summary>
        /// Performs a volatile write of this sequence. The intent is
        /// a Store/Store barrier between this write and any previous
        /// write and a Store/Load barrier between this write and any
        /// subsequent volatile read.
        /// 
        /// 读取，写入值都将使用内存栅栏
        /// </summary>
        /// <param name="value">The new value for the sequence.</param>
        public virtual void SetVolatile(long value)
        {
            Volatile.Write(ref Value, value);
        }

        /// <summary>
        /// Perform a compare and set operation on the sequence.
        /// 
        /// Atomically set the value to the given updated value if the current value == the expected value.
        /// </summary>
        /// <param name="expectedValue">The expected current value for the sequence</param>
        /// <param name="newValue">The value to update for the sequence</param>
        /// <returns>true if the operation succeeds, false otherwise.</returns>
        public virtual Boolean CompareAndSet(long expectedValue, long newValue)
        {
            return Interlocked.CompareExchange(ref Value, newValue, expectedValue) == expectedValue;
        }

        /// <summary>
        /// Atomically increment the sequence by one.
        /// 
        /// Increments the sequence and stores the result, as an atomic operation.
        /// </summary>
        /// <returns>The value after the increment.</returns>
        public virtual long IncrementAndGet()
        {
            return Interlocked.Increment(ref Value);
        }

        /// <summary>
        /// Atomically add the supplied value.
        /// 
        /// Increments the sequence and stores the result, as an atomic operation.
        /// </summary>
        /// <param name="increment">The value to add to the sequence.</param>
        /// <returns>The value after the increment.</returns>
        public virtual long AddAndGet(long increment)
        {
            //:2.0使用
            return Interlocked.Add(ref Value, increment);
            //:3.3.0使用
            //long currentValue;
            //long newValue;
            //do
            //{
            //    currentValue = Get();
            //    newValue = currentValue + increment;
            //}
            //while (!CompareAndSet(currentValue, newValue));
            //return newValue;
        }

        /// <summary>
        /// Value of the <see cref="Sequence"/> as a String.
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            //return Long.toString(get());
            return Value.ToString();
        }

    }
}
