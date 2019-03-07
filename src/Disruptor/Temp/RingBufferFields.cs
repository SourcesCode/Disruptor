using Disruptor.Core;
using System;

namespace Disruptor
{
    /// <summary>
    /// RingBufferPad
    /// </summary>
    public abstract class RingBufferPad
    {
        protected long P1, P2, P3, P4, P5, P6, P7;
    }

    /// <summary>
    /// RingBufferFields
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RingBufferFields<T> : RingBufferPad
    {
        /// <summary>
        /// Buffer数组填充
        /// </summary>
        private static readonly int BUFFER_PAD;
        /// <summary>
        /// Buffer数组起始基址
        /// </summary>
        private static readonly long REF_ARRAY_BASE;
        /// <summary>
        /// 2^n=每个数组对象引用所占空间，这个n就是REF_ELEMENT_SHIFT
        /// </summary>
        private static readonly int REF_ELEMENT_SHIFT;
        //private static readonly Unsafe UNSAFE = Util.getUnsafe();

        /// <summary>
        /// RingBufferFields
        /// </summary>
        static RingBufferFields()
        {
            //Object数组引用长度，32位为4字节，64位为8字节
            int scale = IntPtr.Size;//UNSAFE.arrayIndexScale(Object[].class);
            if (4 == scale)
            {
                REF_ELEMENT_SHIFT = 2;
            }
            else if (8 == scale)
            {
                REF_ELEMENT_SHIFT = 3;
            }
            else
            {
                throw new IllegalStateException("Unknown pointer size");
            }
            //需要填充128字节，缓存行长度一般是128字节
            BUFFER_PAD = 128 / scale;
            //这里需要说明下，REF_ARRAY_BASE是整个数组的起始地址+用于缓存行填充的那些空位的偏移量
            //BUFFER_PAD << REF_ELEMENT_SHIFT表示BUFFER_PAD个引用的占用字节数
            //比如一个引用占用字节数是4，那么REF_ELEMENT_SHIFT是2，
            //BUFFER_PAD << REF_ELEMENT_SHIFT就相当于BUFFER_PAD*4
            // Including the buffer pad in the array base offset
            //TODO:
            REF_ARRAY_BASE = 140;   //UNSAFE.arrayBaseOffset(Object[].class) + 128;
        }

        /// <summary>
        /// 数组的下表掩码
        /// </summary>
        private readonly long indexMask;
        /// <summary>
        /// 数组的大小
        /// </summary>
        protected readonly int bufferSize;
        /// <summary>
        /// 生产者序列管理者
        /// </summary>
        protected readonly ISequencer sequencer;
        /// <summary>
        /// 用来真正存放数据的数组
        /// </summary>
        private readonly T[] entries;

        /// <summary>
        /// RingBufferFields
        /// </summary>
        /// <param name="eventFactory"></param>
        /// <param name="sequencer"></param>
        protected RingBufferFields(ISequencer sequencer, IEventFactory<T> eventFactory)
        {
            this.sequencer = sequencer;
            this.bufferSize = sequencer.GetBufferSize();
            //保证buffer大小不小于1
            if (bufferSize < 1)
            {
                throw new IllegalArgumentException("bufferSize must not be less than 1");
            }
            //保证buffer大小为2的n次方
            if (IntExtension.BitCount(bufferSize) != 1)
            {
                throw new IllegalArgumentException("bufferSize must be a power of 2");
            }
            //m % 2^n  <=>  m & (2^n - 1)
            this.indexMask = bufferSize - 1;
            //对于entries数组的缓存行填充，申请的数组大小为实际需要大小加上2 * BUFFER_PAD，所占空间就是2 *128字节。
            //由于数组中的元素经常访问，所以将数组中的实际元素两边各加上128字节的padding防止false sharing。 
            this.entries = new T[sequencer.GetBufferSize() + 2 * BUFFER_PAD];
            //利用eventFactory初始化RingBuffer的每个槽
            Fill(eventFactory);
        }

        /// <summary>
        /// Fill
        /// </summary>
        /// <param name="eventFactory"></param>
        private void Fill(IEventFactory<T> eventFactory)
        {
            for (int i = 0; i < bufferSize; i++)
            {
                //看到没有，是从数组的第BUFFER_PAD+1个元素开始为有效位置，最后一个有效位置为BUFFER_PAD+bufferSize
                //在数组的前后都各有BUFFER_PAD个空位，是用来做缓存行填充用的
                entries[BUFFER_PAD + i] = eventFactory.NewInstance();
            }
        }

        /// <summary>
        /// ElementAt
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected T ElementAt(long sequence)
        {
            //TODO;Test
            return entries[BUFFER_PAD + (int)(sequence & indexMask)];
            //return (E)UNSAFE.getObject(entries, REF_ARRAY_BASE + ((sequence & indexMask) << REF_ELEMENT_SHIFT));
        }

    }

}
