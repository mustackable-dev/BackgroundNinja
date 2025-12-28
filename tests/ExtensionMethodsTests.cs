using BackgroundNinja;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackgroundNinjaTests;

public class ExtensionMethodsTests
{
    [Fact]
    public void AddBackgroundWorker_ShouldRegisterBackgroundWorkerService_ServiceIsRegistered()
    {
        // Arrange
        ServiceCollection serviceCollection = new();
        serviceCollection.AddBackgroundWorker(
            new BackgroundOperation(TimeSpan.FromMinutes(10), x => Task.CompletedTask));
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Assert
        IHostedService? worker = serviceProvider.GetService<IHostedService>();
            
        Assert.NotNull(worker);
        Assert.IsType<BackgroundWorkerService>(worker);
    }
    
    [Fact]
    public void AddKeyedBackgroundWorker_ShouldRegisterKeyedBackgroundWorkerService_ServiceIsRegistered()
    {
        // Arrange
        ServiceCollection serviceCollection = new();
        serviceCollection.AddKeyedBackgroundWorker(
            "test",
            new BackgroundOperation(TimeSpan.FromMinutes(10), x => Task.CompletedTask));
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Assert
        BackgroundWorkerService? worker = serviceProvider.GetKeyedService<BackgroundWorkerService>("test");
            
        Assert.NotNull(worker);
    }
}
