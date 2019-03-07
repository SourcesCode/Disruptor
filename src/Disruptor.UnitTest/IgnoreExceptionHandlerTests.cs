using System;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    [TestClass]
    public class IgnoreExceptionHandlerTests
    {
        /// <summary>
        /// �����쳣��ֻ��¼��־
        /// </summary>
        [TestMethod]
        public void ShouldIgnoreException()
        {
            var exception = new Exception();
            var stubEvent = new StubEvent(0);

            var exceptionHandler = new IgnoreExceptionHandler();
            exceptionHandler.HandleEventException(exception, 0L, stubEvent);

        }
    }
}