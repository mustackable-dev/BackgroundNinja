using BackgroundNinja;
using Microsoft.Extensions.Caching.Memory;
using Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer()
    .AddLogging(x=>x.AddConsole())
    .AddSwaggerGen()
    .AddMemoryCache()
    .AddBackgroundWorker([
        new("*/2 * * * *", x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Sequential - Every 2 Minutes", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("seq2min");
            return Task.CompletedTask;
        }),
        new(TimeSpan.FromSeconds(7), x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Sequential - Every 7 Seconds", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("seq7sec");
            return Task.CompletedTask;
        }),
        new(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Parallel - Every 5 Seconds Instance 1", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("par5sec1");
            return Task.CompletedTask;
        }, RunMode.Parallel),
        new(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Parallel - Every 5 Seconds Instance 2", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("par5sec2");
            return Task.CompletedTask;
        }, RunMode.Parallel),
    ])
    
    //Adding a keyed instance of a worker that you can start and stop on demand via an endpoint
    .AddKeyedBackgroundWorker(1,[
        new BackgroundOperation("*/30 * * * * *", true, x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Thread - Every 30th Second", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("thr30sec");
            return Task.CompletedTask;
        }, RunMode.Thread),
        new BackgroundOperation("28 13 * * *", TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia"), x =>
        {
            x.GetRequiredService<ILogger<BackgroundOperation>>().LogInformation("[{Timestamp}]: Sequential - 13:28 With Timezone", DateTime.UtcNow.ToString("s"));
            x.GetRequiredService<IMemoryCache>().IncrementEntry("seq13:28tz");
            return Task.CompletedTask;
        })
    ]);

builder.Host.ConfigureHostOptions(x =>
    x.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

var app = builder.Build();
app.UseSwagger()
   .UseSwaggerUI()
   .UseHttpsRedirection();

app.MapDelete("/Workers/{id}", 
        async (int id, IServiceProvider serviceProvider) =>
        {
            BackgroundWorkerService? worker = serviceProvider.GetKeyedService<BackgroundWorkerService>(id);
            if(worker is null) return Results.NotFound();
            await worker.StopAsync(CancellationToken.None);
            return Results.NoContent();
        })
    .WithDescription("Stops a keyed worker registered with the specified id.")
    .WithOpenApi();

app.MapPost("/Workers/{id}", 
        async (int id, IServiceProvider serviceProvider) =>
        {
            BackgroundWorkerService? worker = serviceProvider.GetKeyedService<BackgroundWorkerService>(id);
            if(worker is null) return Results.NotFound();
            await worker.StartAsync(CancellationToken.None);
            return Results.Ok();
        })
    .WithDescription("Starts a keyed worker registered with the specified id.")
    .WithOpenApi();

app.MapGet("/Counters/{id}",
        (string id, IMemoryCache cache) =>
        {
            int result = cache.GetIncrementedEntry(id);
            if(result == - 1) return Results.NotFound();
            return Results.Ok(result);
        }) 
    .WithDescription("Retrieves the value of a counter with the specified id from the memory cache.")
    .WithOpenApi();

await app.RunAsync();


namespace Extensions
{
    public static class ExtensionMethods
    {
        public static void IncrementEntry(this IMemoryCache cache, string key)
        {
            cache.TryGetValue(key, out int count);
            count++;
            cache.Set(key, count);
        }

        public static int GetIncrementedEntry(this IMemoryCache cache, string key) =>
            cache.TryGetValue(key, out int count) ? count : -1;
    }
}