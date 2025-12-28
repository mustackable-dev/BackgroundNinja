using BackgroundNinja;
using BackgroundNinjaTests.Fixtures;
using Cronos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace BackgroundNinjaTests;

public class BackgroundWorkerServiceTests(WorkerTestsFixture fixture)
{
    [Theory]
    [InlineData(RunMode.Thread)]
    [InlineData(RunMode.Parallel)]
    [InlineData(RunMode.Sequential)]
    public async Task ExecuteAsync_SingleRunModeOperations_ShouldRunToSchedule(RunMode mode)
    {
        // Arrange
        
        string cron40 = "*/40 * * * * *";
        int exactTimestampDelay = 60030;
        DateTime exactTimestamp = DateTime.UtcNow
            .Truncate(TimeSpan.TicksPerSecond)
            .AddMilliseconds(exactTimestampDelay);
        string cronExactTimestamp = $"{exactTimestamp.Minute} {exactTimestamp.Hour} * * *";
        
        fixture.ServiceCollection
            .AddBackgroundWorker(
                new(TimeSpan.FromSeconds(10), x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("10");
                    return Task.CompletedTask;
                }, mode),
                new(cron40, true, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("40");
                    return Task.CompletedTask;
                }, mode),
                new(cronExactTimestamp, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("Exact");
                    return Task.CompletedTask;
                }, mode));
        
        ServiceProvider serviceProvider = fixture.ServiceCollection.BuildServiceProvider();
        IHostedService? worker = serviceProvider.GetService<IHostedService>();
        
        DateTime startTime = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow;
        
        if (worker is not null)
        {
            await worker.StartAsync(CancellationToken.None);
            startTime = DateTime.UtcNow;
            await Task.Delay(TimeSpan.FromMilliseconds(exactTimestampDelay), CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);
            endTime = DateTime.UtcNow;
        }
        
        //Assert
        
        Assert.NotNull(worker);
        IMemoryCache? memoryCache = serviceProvider.GetService<IMemoryCache>();
        Assert.NotNull(memoryCache);
        
        List<DateTime> occurrences10 = memoryCache.GetOccurrences("10");

        for(int i = 0; i<occurrences10.Count; i++)
        {
            if (i < occurrences10.Count - 1)
            {
                Assert.True(Math.Abs((occurrences10[i+1] - occurrences10[i]).TotalSeconds - 10) < 1);
            }
        }
        
        List<DateTime> occurrences40 = memoryCache.GetOccurrences("40");
        DateTime[] expectedOccurrences40 = CronExpression
            .Parse(cron40, CronFormat.IncludeSeconds)
            .GetOccurrences(startTime, endTime)
            .ToArray();
        
        Assert.True(occurrences40.Count == expectedOccurrences40.Length);
        
        for(int i = 0; i<expectedOccurrences40.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrences40[i] - occurrences40[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesExactMinute = memoryCache.GetOccurrences("Exact");
        DateTime[] expectedOccurrencesExactMinute = CronExpression
            .Parse(cronExactTimestamp)
            .GetOccurrences(startTime, endTime)
            .ToArray();
        
        Assert.True(occurrencesExactMinute.Count == expectedOccurrencesExactMinute.Length);
        
        for(int i = 0; i<expectedOccurrencesExactMinute.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesExactMinute[i] - occurrencesExactMinute[i]).TotalSeconds) < 1);
        }

    }
    
    [Fact]
    public async Task ExecuteAsync_MixedRunModeOperations_ShouldRunToSchedule()
    {
        // Arrange
        
        string cronEveryMinute = "*/1 * * * *";
        string cronEach10Seconds = "*/10 * * * * *";
        int exactTimestampDelay = 90030;
        DateTime exactTimestamp = DateTime.UtcNow
            .Truncate(TimeSpan.TicksPerSecond)
            .AddMilliseconds(exactTimestampDelay);
        string cronExactMinute = $"{exactTimestamp.Minute} {exactTimestamp.Hour} * * *";
        
        fixture.ServiceCollection
            .AddBackgroundWorker(
                new(TimeSpan.FromSeconds(15), x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("15");
                    return Task.CompletedTask;
                }, RunMode.Thread),
                new(cronEveryMinute, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("Every Minute");
                    return Task.CompletedTask;
                }, RunMode.Parallel),
                new(cronExactMinute, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("Minute");
                    return Task.CompletedTask;
                }, RunMode.Sequential),
                new(cronEach10Seconds, true, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("Each 10 Seconds");
                    return Task.CompletedTask;
                }, RunMode.Sequential));
        
        ServiceProvider serviceProvider = fixture.ServiceCollection.BuildServiceProvider();
        IHostedService? worker = serviceProvider.GetService<IHostedService>();
        
        DateTime startTime = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow;
        
        if (worker is not null)
        {
            await worker.StartAsync(CancellationToken.None);
            startTime = DateTime.UtcNow;
            await Task.Delay(TimeSpan.FromMilliseconds(exactTimestampDelay), CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);
            endTime = DateTime.UtcNow;
        }
        
        //Assert
        
        Assert.NotNull(worker);
        IMemoryCache? memoryCache = serviceProvider.GetService<IMemoryCache>();
        Assert.NotNull(memoryCache);
        
        List<DateTime> occurrences15 = memoryCache.GetOccurrences("15");
        
        for(int i = 0; i<occurrences15.Count; i++)
        {
            if (i < occurrences15.Count - 1)
            {
                Assert.True(Math.Abs((occurrences15[i+1] - occurrences15[i]).TotalSeconds - 15) < 1);
            }
        }
        
        List<DateTime> occurrencesEveryMinute = memoryCache.GetOccurrences("Every Minute");
        DateTime[] expectedOccurrencesEveryMinute = CronExpression
            .Parse(cronEveryMinute)
            .GetOccurrences(startTime, endTime)
            .ToArray();
        
        Assert.True(occurrencesEveryMinute.Count == expectedOccurrencesEveryMinute.Length);
        
        for(int i = 0; i<expectedOccurrencesEveryMinute.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesEveryMinute[i] - occurrencesEveryMinute[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesExactMinute = memoryCache.GetOccurrences("Minute");
        DateTime[] expectedOccurrencesExactMinute = CronExpression
            .Parse(cronExactMinute)
            .GetOccurrences(startTime, endTime)
            .ToArray();
        
        Assert.True(occurrencesExactMinute.Count == expectedOccurrencesExactMinute.Length);
        for(int i = 0; i<expectedOccurrencesExactMinute.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesExactMinute[i] - occurrencesExactMinute[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesEach10Seconds = memoryCache.GetOccurrences("Each 10 Seconds");
        DateTime[] expectedOccurrencesEach10Seconds = CronExpression
            .Parse(cronEach10Seconds, CronFormat.IncludeSeconds)
            .GetOccurrences(startTime, endTime)
            .ToArray();

        Assert.True(occurrencesEach10Seconds.Count == expectedOccurrencesEach10Seconds.Length);
        for(int i = 0; i<expectedOccurrencesEach10Seconds.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesEach10Seconds[i] - occurrencesEach10Seconds[i]).TotalSeconds) < 1);
        }

    }
}

public static class TestUtilities
{
    public static void AddOccurence(this IMemoryCache cache, string key)
    {
        cache.TryGetValue(key, out List<DateTime>? occurrences);
        occurrences ??= new List<DateTime>();
        occurrences.Add(DateTime.UtcNow);
        cache.Set(key, occurrences);
    }

    public static List<DateTime> GetOccurrences(this IMemoryCache cache, string key) =>
        cache.TryGetValue(key, out List<DateTime>? occurrences) ? occurrences ?? new() : new ();
    
    public static DateTime Truncate(this DateTime date, long resolution)
    {
        return new DateTime(date.Ticks - (date.Ticks % resolution), date.Kind);
    }
}