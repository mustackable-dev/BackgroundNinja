namespace BackgroundNinja;

internal class WorkerPool(RunMode mode, ScheduledOperation[] operations)
{
    internal RunMode Mode { get; } = mode;
    internal ScheduledOperation[] Operations { get; } = operations;
        
    //Unlike operations scheduled with a Cron expression, operations scheduled with
    //a TimeSpan do not have inherently deterministic execution times. Sequential
    //execution can result in lags which will accumulate over time. To prevent this,
    //we sync operations with identical TimeSpan interval, so that they always run
    //in the same cycle. For each worker pool, we calculate the mapping below to
    //help us quickly sync the operations.
    internal int[][] CycleSpanOperationsSyncIndexMaps { get; } =
        operations.Select((x, i) => (key: x.CycleSpan, value: i))
                  .Where(x => x.key is not null)
                  .GroupBy(x => x.key)
                  .Select(x => x.Select(y => y.value).ToArray())
                  .ToArray();
}