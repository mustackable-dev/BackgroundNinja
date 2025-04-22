<img src="https://avatars.githubusercontent.com/u/200509271?s=400&u=52ffb95ce0884ad2c717719266626c6e1548c041&v=4" alt="Mustackable" width="100"/>

## Intro

BackgroundNinja is the easiest way to add a background worker to an ASP.NET application.

It is a lightweight and performant package with a simple and flexible API.

## Quick Start

Use the IServiceProvider extension to register the background worker to your service collection.

Here is how you can add a worker that logs a message every 60 seconds:

```csharp
using BackgroundNinja;

builder.Services
    .AddLogging(x=>x.AddConsole())
    .AddBackgroundWorker([
        new BackgroundOperation(
            TimeSpan.FromMinutes(1),
            x => 
            {
                var logger = x.GetRequiredService<ILogger<BackgroundOperation>>();
                logger.LogInformation("Hello!");
                return Task.CompletedTask;
            }
        )
    ]);
```

Here is how you can add a worker that logs a message every Tuesday at 13:00 Seoul time:

```csharp
using BackgroundNinja;

builder.Services
    .AddLogging(x=>x.AddConsole())
    .AddBackgroundWorker([
        new BackgroundOperation(
            "0 13 * * TUE",
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"),
            x => 
            {
                var logger = x.GetRequiredService<ILogger<BackgroundOperation>>();
                logger.LogInformation("Annyeong!");
                return Task.CompletedTask;
            }
        )
    ]);
```

... and that is all! Piece of cake :) 🎂

More examples can be seen in the minimal API example [here](https://github.com/mustackable-dev/BackgroundNinja/blob/master/example/Program.cs).

## Scheduling

There are two ways to define execution schedule of a BackgroundOperation:

- ### Using a TimeSpan

Using a TimeSpan is simple and effective, it will cover most of your needs. The cycle begins with the moment the background worker is initialized.

All background tasks with the same TimeSpan and within the same [Run Mode](##Run Modes) pool will run in the same batch.

- ### Using a Cron expression

Using a Cron expression is an advanced, but extremely flexible and powerful option for scheduling.

It is the recommended way to schedule a background operation, if precise execution times are important for your project. Cron schedules are deterministic, unlike the current TimeSpan schedules.

You can read more about Cron [here](https://en.wikipedia.org/wiki/Cron). Some examples:

```*/15 * * * *``` - will run every 15th minute

```5 0 * 8 *``` - will run at 00:05 in August

```15 14 1 * *``` - will run at 14:15 on the first day of the month

```0 22 * * 1-5``` - will run at 22:00 on every day-of-week from Monday to Friday

#### Time Zone Support

When running Cron jobs at a specific time of the day (e.g. 14:15), it is important to specify a time zone for the Cron expression.

BackgroundOperation offers constructors which take in a TimeZoneInfo as an additional parameter. For example:

```csharp
new BackgroundOperation(
    cronExpression: "28 13 * * *", 
    cronTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia"), x =>
        {
            x.GetRequiredService<IMemoryCache>().IncrementEntry("seq13:28tz");
            return Task.CompletedTask;
        })
```

More information on the TimeZoneInfo.FindSystemTimeZoneById, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid).

Daylight savings time is automatically taken into account, no need to do anything extra.

#### Seconds support

BackgroundNinja uses [Cronos](https://github.com/HangfireIO/Cronos) for cron expression parsing, which optionally allows seconds precision, instead of the standard minutes precision.

More information about the syntax can be found [here](https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format).

You can instantiate a BackgroundOperation that will run every 30th second via a cron schedule with seconds precision like this:

```csharp
new BackgroundOperation(
    cronExpression: "*/30 * * * * *",
    cronHasSeconds: true,
    x =>
        {
            x.GetRequiredService<GnomeLauncher>().Launch();
            return Task.CompletedTask;
        })
```

## Multiple Operations

You have probably already noticed that the AddBackgroundWorker method takes in an array of BackgroundOperation, instead of just one.

While it is perfectly valid to do this:

```csharp
new BackgroundOperation(
        TimeSpan.FromMinutes(5),
        x =>
        {
            x.GetRequiredService<InventoryService>().RefreshStockLevels();
            x.GetRequiredService<InvoiceService>().SendInvoices();
            x.GetRequiredService<ReportingService>().SendDelayNotifications();
            return Task.CompletedTask;
        })
```

 ... what if, without adding custom logic to your service, you want to refresh stock levels every 5 minutes, send invoices every 30 minutes and send delay notifications only at exactly 09:15 UTC every day?
 
This is easily achievable with BackgroundNinja:

```csharp
[
    new BackgroundOperation(
            TimeSpan.FromMinutes(5),
            x =>
            {
                x.GetRequiredService<InventoryService>().RefreshStockLevels();
                return Task.CompletedTask;
            }),
    new BackgroundOperation(
            TimeSpan.FromMinutes(30),
            x =>
            {
                x.GetRequiredService<InvoiceService>().SendInvoices();
                return Task.CompletedTask;
            }),
    new BackgroundOperation(
            "15 9 * * *",
            TimeZoneInfo.Utc, 
            x =>
            {
                x.GetRequiredService<ReportingService>().SendDelayNotifications();
                return Task.CompletedTask;
            })
]
```

This approach provides you with a lot more flexibility for scheduling, control as well as for error handling.

With no performance penalty!

## Multiple workers

If you application design demands it (or if you just want better separation of concerns), you can easily add multiple background workers to your application.

This is possible by calling the extension method ```AddBackgroundWorker``` multiple times.

Each BackgroundWorkerService is registered as a singleton instance of an IHostedService to get around the current limitation of registering multiple instances of the same implementation of IHostedService (more information [here](https://github.com/dotnet/runtime/issues/38751)).

If the provided extensions methods for registering the background workers do not fit your needs, you can register them in any manner your prefer - the BackgroundWorkerService class is public 👍

### Keyed workers

BackgroundNinja also provides an extension method for adding a keyed instance of a background worker.

Using a keyed background worker is useful, if your application design requires a way to stop and start workers on demand.

Here is how you can do this:

```csharp
    .AddKeyedBackgroundWorker(1,[
        new BackgroundOperation("*/30 * * * * *", true, x =>
        {
            x.GetRequiredService<IGardeningService>().PlantSeeds();
            return Task.CompletedTask;
        }, RunMode.Thread)
    ])
```

This allows you to retrieve the background worker later on and stop it or start it. For example, here is a method that stops a worker:

```csharp
static async Task StopWorker(int workerId, IServiceProvider serviceProvider)
{
    BackgroundWorkerService? worker = serviceProvider.GetKeyedService<BackgroundWorkerService>(workerId);
    
    if(worker is not null) await worker.StopAsync(CancellationToken.None);
}
```

More examples can be seen in the minimal API example [here](https://github.com/mustackable-dev/BackgroundNinja/blob/master/example/Program.cs).

## Run Modes

The RunMode parameter in the BackgroundOperation constructors give you more fine-grained control over how exactly each operation is executed in the worker.

In a BackgroundWorkerService instance, operations are grouped in dedicated execution pools, based on their Run mode. Here are the execution pools:

- ### Sequential (Default)

    In the Sequential pool, operations with identical schedules are executed one at a time in a sequence. The execution order depends on the definition order during the registration of the BackgroundWorkerService. For example:

    ```csharp
        ///Operation A
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverCoconuts();
            return Task.CompletedTask;
        }),
                
        ///Operation B
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverOranges();
            return Task.CompletedTask;
        })
    ```
    Operation A will be run first and awaited, **then** Operation B will be run and awaited.

- ### Parallel

    In the parallel pool, operations with identical schedules are executed concurrently. For example:

    ```csharp
        ///Operation A
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverCoconuts();
            return Task.CompletedTask;
        }, RunMode.Parallel),
                
        ///Operation B
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverOranges();
            return Task.CompletedTask;
        }, RunMode.Parallel)
    ```
  Operation A and Operation B **will start at the same time**. The worker will await the completion of both before proceeding with other operations.

- ### Thread

  In the Thread pool, all operations are executed independently of one another and are not awaited. This mode uses ThreadPool.QueueUserWorkItem to run operations and is the only one that potentially may never have [stragglers](##stragglers). For example:

    ```csharp
        ///Operation A
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverCoconuts();
            return Task.CompletedTask;
        }, RunMode.Thread),
                
        ///Operation B
        new BackgroundOperation(TimeSpan.FromSeconds(5), x =>
        {
            x.GetRequiredService<IFruitService>().DeliverOranges();
            return Task.CompletedTask;
        }, RunMode.Thread)
    ```
  
    The background worker will add the tasks to the ThreadPool, which will handle their execution (ideally, they would start immediately). While this method theoretically ensures the strictest possible adherence to the BackgroundOperation's schedule, **it could potentially be very resource-intensive and lead to thread exhaustion**. Use with caution!

#### Run Modes FAQ:

***Are `TimeSpam.FromMinutes(15)` and `*/15 * * * *` basically the same schedule?***

If your worker started at exactly 00:00:00, they will be.

However, as far as BackgroundNinja is concerned, equality on schedule is established based on equality in the scheduling parameter.

To put it another way, if TimeSpan scheduling is apples, Cron scheduling is oranges. You can compare an apple only to another apple, but not to an orange (even if the orange is also round and green 😃 🐷).

***Do operations with the same schedule but different Run Mode affect each other?***

No, tasks are separated into different execution pools when the BackgroundWorkerService is instantiated. They do not interfere with each other.

## Stragglers

If one of your operations takes too long to complete, it may derail the scheduled execution of other operations in the same pool (except if you are using the [Thread](###thread) pool).

Operations that missed their schedule due to prolonged execution (of other operations or themselves) will be referred as ***stragglers*** in this section.

Stragglers are essentially a scheduling problem that the developer can easily resolve by adjusting the operations' schedules. However, you will only notice this problem when it has already happened, and the damages is done.

BackgroundNinja will scan for stragglers at the end of every round and will immediately run them once. While this is not a perfect fix, it will at least make sure the operations, which missed their schedule, will run at least once.

Using a logger in your ```operationFactory``` method can help you debug straggler issues. 👍

## License

MIT



