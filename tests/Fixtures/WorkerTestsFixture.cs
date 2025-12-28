using BackgroundNinjaTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(WorkerTestsFixture))]
namespace BackgroundNinjaTests.Fixtures;
public class WorkerTestsFixture
{
    public IServiceCollection ServiceCollection { get; }= (new ServiceCollection()).AddMemoryCache();
}