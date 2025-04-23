using BackgroundNinja;
using Cronos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace BackgroundNinjaTests;

public class BackgroundWorkerServiceFacts
{
    [Theory]
    [InlineData(RunMode.Thread)]
    [InlineData(RunMode.Parallel)]
    [InlineData(RunMode.Sequential)]
    public async Task ExecuteAsync_SingleRunModeOperations_ShouldRunToSchedule(RunMode mode)
    {
        // Arrange
        
        ServiceCollection serviceCollection = new();
        
        DateTime startTime = DateTime.UtcNow.Truncate(TimeSpan.TicksPerSecond);
        DateTime endTime = startTime.AddSeconds(61);
        
        string cron40 = "*/40 * * * * *";
        string cronExactMinute = $"{endTime.Minute} {endTime.Hour} * * *";
        
        serviceCollection
            .AddMemoryCache()
            .AddBackgroundWorker([
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
                new(cronExactMinute, x =>
                {
                    x.GetRequiredService<IMemoryCache>().AddOccurence("Minute");
                    return Task.CompletedTask;
                }, mode)]);
        
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IHostedService? worker = serviceProvider.GetService<IHostedService>();
        if (worker is not null)
        {
            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(endTime - startTime);
            endTime = DateTime.UtcNow;
            await worker.StopAsync(CancellationToken.None);
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
        DateTime[] expectedOccurrences40 = CronExpression.Parse(cron40, CronFormat.IncludeSeconds).GetLastOccurence(startTime, endTime).ToArray();
        
        Assert.True(occurrences40.Count == expectedOccurrences40.Length);
        
        for(int i = 0; i<expectedOccurrences40.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrences40[i] - occurrences40[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesExactMinute = memoryCache.GetOccurrences("Minute");
        DateTime[] expectedOccurrencesExactMinute = CronExpression.Parse(cronExactMinute).GetLastOccurence(startTime, endTime).ToArray();
        
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
        
        ServiceCollection serviceCollection = new();
        
        DateTime startTime = DateTime.UtcNow.Truncate(TimeSpan.TicksPerSecond);
        DateTime endTime = startTime.AddSeconds(90);
        
        string cronEveryMinute = "*/1 * * * *";
        string cronExactMinute = $"{endTime.Minute} {endTime.Hour} * * *";
        string cronEach10Seconds = "*/10 * * * * *";
        
        serviceCollection
            .AddMemoryCache()
            .AddBackgroundWorker([
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
                }, RunMode.Sequential)]);
        
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        IHostedService? worker = serviceProvider.GetService<IHostedService>();
        if (worker is not null)
        {
            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(endTime - startTime);
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
        DateTime[] expectedOccurrencesEveryMinute = CronExpression.Parse(cronEveryMinute).GetLastOccurence(startTime, endTime).ToArray();
        
        Assert.True(occurrencesEveryMinute.Count == expectedOccurrencesEveryMinute.Length);
        
        for(int i = 0; i<expectedOccurrencesEveryMinute.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesEveryMinute[i] - occurrencesEveryMinute[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesExactMinute = memoryCache.GetOccurrences("Minute");
        DateTime[] expectedOccurrencesExactMinute = CronExpression.Parse(cronExactMinute).GetLastOccurence(startTime, endTime).ToArray();
        
        Assert.True(occurrencesExactMinute.Count == expectedOccurrencesExactMinute.Length);
        for(int i = 0; i<expectedOccurrencesExactMinute.Length; i++)
        {
            Assert.True(Math.Abs((expectedOccurrencesExactMinute[i] - occurrencesExactMinute[i]).TotalSeconds) < 1);
        }
        
        List<DateTime> occurrencesEach10Seconds = memoryCache.GetOccurrences("Each 10 Seconds");
        DateTime[] expectedOccurrencesEach10Seconds = CronExpression.Parse(cronEach10Seconds, CronFormat.IncludeSeconds).GetLastOccurence(startTime, endTime).ToArray();

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

    public static DateTime[] GetLastOccurence(this CronExpression cronExpression, DateTime startTime, DateTime endTime)
    {
        DateTime[] occurrences = cronExpression.GetOccurrences(startTime, endTime).ToArray();
        
        //Occasionally, Cronos would calculate an instance that is a few milliseconds earlier than the correct one
        //in the sequence, and we should omit this instance
        
        if (occurrences.Length > 1 && ((occurrences[1] - occurrences[0]).TotalSeconds <= 1 || (occurrences[0] - startTime).TotalSeconds < 1))
        {
            return occurrences.Skip(1).ToArray();
        }
        
        return occurrences;
    }
}