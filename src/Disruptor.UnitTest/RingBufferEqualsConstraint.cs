using System.Linq;

namespace Disruptor.Tests
{
    public class RingBufferEqualsConstraint //: Constraint
    {
        private readonly object[] _values;

        public RingBufferEqualsConstraint(params object[] values)
        {
            _values = values;
        }

        //public override ConstraintResult ApplyTo<TActual>(TActual actual)
        //{
        //    var valid = true;
        //    var ringBuffer = (RingBuffer<object[]>)(object)actual;
        //    for (var i = 0; i < _values.Length; i++)
        //    {
        //        var value = _values[i];
        //        valid &= value == null || ringBuffer[i][0].Equals(value);
        //    }

        //    return new ConstraintResult(this, actual, valid);
        //}

        public static RingBufferEqualsConstraint RingBufferWithEvents(params object[] values)
        {
            return new RingBufferEqualsConstraint(values);
        }

        public static bool RingBufferWithEvents(RingBuffer<object[]> _ringBuffer, params dynamic[] events)
        {
            var len = events.Length;
            var actualValues = new dynamic[len];
            var expectedValues = new dynamic[len];

            for (var i = 0; i < len; i++)
            {
                actualValues[i] = events[i][0];
                expectedValues[i] = _ringBuffer.Get(i)[0];
            }

            return expectedValues.SequenceEqual(actualValues);
        }

    }
}
