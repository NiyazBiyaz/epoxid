using Epoxid.Runtime.Objects;

namespace Epoxid.Tests;

public abstract class TestWithRuntimeUsage : IDisposable
{
    protected TestWithRuntimeUsage()
    {
        RuntimeInitializer.InitializeObjectType();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
