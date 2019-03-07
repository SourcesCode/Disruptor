using System;
using Disruptor.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor.UnitTest.Support;

namespace Disruptor.Tests
{
    [TestClass]
    public class FatalExceptionHandlerTests
    {
        /// <summary>
        /// ≤‚ ‘HandleEventException∫Û≈◊≥ˆRuntimeException“Ï≥£
        /// </summary>
        [TestMethod]
        public void ShouldHandleFatalException()
        {
            var causeException = new Exception();
            var evt = new StubEvent(0);

            var exceptionHandler = new FatalExceptionHandler();

            try
            {
                exceptionHandler.HandleEventException(causeException, 0L, evt);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(causeException, ex.InnerException);
            }

            Assert.ThrowsException<RuntimeException>(() => exceptionHandler.HandleEventException(causeException, 0L, evt));

        }
    }
}