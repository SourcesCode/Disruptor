using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest.Demos.Demo1
{
    public class Translator : IEventTranslatorOneArg<LongEvent, long>
    {
        public void TranslateTo(LongEvent @event, long sequence, long arg0)
        {
            @event.set(arg0);
        }
    }
}
