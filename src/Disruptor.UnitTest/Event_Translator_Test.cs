using Disruptor.UnitTest.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disruptor.UnitTest
{
    [TestClass]
    public class Event_Translator_Test
    {
        private static readonly string TestValue = "Wibble";

        /// <summary>
        /// 翻译事件
        /// </summary>
        [TestMethod]
        public void Should_Translate_Other_Data_Into_An_Event()
        {
            var @event = StubEvent.EventFactory.NewInstance();
            var translator = new ExampleEventTranslator(TestValue);
            translator.TranslateTo(@event, 0);

            Assert.AreEqual(@event.TestString, TestValue);
        }

        private class ExampleEventTranslator : IEventTranslator<StubEvent>
        {
            private readonly string _testValue;
            public ExampleEventTranslator(string testValue)
            {
                _testValue = testValue;
            }

            public void TranslateTo(StubEvent @event, long sequence)
            {
                @event.TestString = _testValue;
            }
        }
    }
}
