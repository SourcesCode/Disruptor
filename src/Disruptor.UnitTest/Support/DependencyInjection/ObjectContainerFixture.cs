using System;

namespace Disruptor.UnitTest.Support.DependencyInjection
{
    public class ObjectContainerFixture : IDisposable
    {
        public IServiceProvider Services { get; private set; }

        public ObjectContainerFixture()
        {
            //Services = new ServiceCollection()
            //    .AddLogging()
            //    .AddScoped<IExceptionHandler<StubEvent>, FatalExceptionHandler<StubEvent>>()
            //    .AddScoped<IExceptionHandler<TestEvent>, FatalExceptionHandler<TestEvent>>()
            //    .AddScoped<IExceptionHandler<object>, IgnoreExceptionHandler>()
            //    .BuildServiceProvider();
        }

        public void Dispose()
        {
            Services = null;
        }
    }
}
