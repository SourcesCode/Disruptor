using Disruptor.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Disruptor.Tests.Internal
{
    [TestClass]
    public class StructProxyTests
    {
        [TestMethod]
        public void ShouldGenerateProxyForType()
        {
            var foo = new Foo();
            var fooProxy = StructProxy.CreateProxyInstance<IFoo>(foo);

            Assert.IsNotNull(fooProxy);
            Assert.IsTrue(fooProxy.GetType().IsValueType);
            //Assert.IsInstanceOf<IFoo>(fooProxy);
            Assert.IsInstanceOfType(fooProxy, typeof(IFoo));
            //Assert.IsInstanceOf<IOtherInterface>(fooProxy);
            Assert.IsInstanceOfType(fooProxy, typeof(IOtherInterface));
            fooProxy.Value = 888;

            Assert.AreEqual(foo.Value, 888);
            Assert.AreEqual(fooProxy.Value, 888);

            fooProxy.Compute(400, 44);

            Assert.AreEqual(foo.Value, 444);
            Assert.AreEqual(fooProxy.Value, 444);
        }

        [TestMethod]
        public void ShouldNotFailForInternalType()
        {
            var foo = new InternalFoo();
            IFoo fooProxy = null;

            try
            {
                fooProxy = StructProxy.CreateProxyInstance<IFoo>(foo);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            Assert.IsNotNull(fooProxy);
            Assert.AreEqual(fooProxy, foo);
        }

        [TestMethod]
        public void ShouldNotFailForPublicTypeWithInternalGenericArgument()
        {
            var bar = new Bar<InternalBarArg>();
            IBar<InternalBarArg> barProxy = null;
            try
            {
                barProxy = StructProxy.CreateProxyInstance<IBar<InternalBarArg>>(bar);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            Assert.IsNotNull(barProxy);
            Assert.AreEqual(barProxy, bar);
        }

        public interface IFoo
        {
            int Value { get; set; }

            void Compute(int a, long b);
        }

        public interface IOtherInterface
        {
        }

        public class Foo : IFoo, IOtherInterface
        {
            public int Value { get; set; }

            public void Compute(int a, long b)
            {
                Value = (int)(a + b);
            }
        }

        internal class InternalFoo : IFoo
        {
            public int Value { get; set; }

            public void Compute(int a, long b)
            {
            }
        }

        public interface IBar<T>
        {
        }

        public class Bar<T> : IBar<T>
        {

        }

        internal class InternalBarArg
        {
        }
    }
}
