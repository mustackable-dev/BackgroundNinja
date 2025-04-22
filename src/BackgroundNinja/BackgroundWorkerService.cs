using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackgroundNinja
{
    public class BackgroundWorkerService : BackgroundService
    {
        private readonly WorkerPool[] _pools;
        private readonly IServiceScopeFactory _scopeFactory;

        public BackgroundWorkerService(IEnumerable<BackgroundOperation> operations, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _pools = operations.Select(x=>new ScheduledOperation(x))
                               .GroupBy(x => x.Mode)
                               .Select(x => new WorkerPool(x.Key, x.ToArray()))
                               .ToArray();
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
            => await Task.WhenAll(_pools.Select(x=> RunPool(x, stoppingToken)).ToArray());

        private async Task RunPool(WorkerPool pool, CancellationToken cancellationToken)
        {
            if (pool.Operations.Length == 0)
            {
                return;
            }
            do
            {
                switch (pool.Mode)
                {
                    case RunMode.Sequential:
                        {
                            using IServiceScope scope = _scopeFactory.CreateScope();
                            foreach (ScheduledOperation operation in pool.Operations.Where(x=>x.ShouldRun))
                            {
                                operation.LastRun = DateTime.UtcNow;
                                await operation.OperationFactory(scope.ServiceProvider);
                            }
                        }
                        break;
                    
                    case RunMode.Parallel:
                        {
                            using IServiceScope scope = _scopeFactory.CreateScope();
                            await Task.WhenAll(pool.Operations.Where(x=>x.ShouldRun)
                                .Select(x =>
                                {
                                    x.LastRun = DateTime.UtcNow;
                                    return x.OperationFactory(scope.ServiceProvider);
                                })
                                .ToArray());
                        }
                        break;
                    
                    case RunMode.Thread:
                        foreach (ScheduledOperation operation in pool.Operations.Where(x=>x.ShouldRun))
                        {
                            operation.LastRun = DateTime.UtcNow;
                            ThreadPool.QueueUserWorkItem(_=>
                            {
                                using IServiceScope scope = _scopeFactory.CreateScope();
                                operation.OperationFactory(scope.ServiceProvider).Wait();
                            });
                        }
                        break;
                }
                TimeSpan delay = CalculateNextDelay(pool);
                await Task.Delay(delay, cancellationToken);
                
            } while (!cancellationToken.IsCancellationRequested);
        }
        

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach(ScheduledOperation operation in _pools.SelectMany(x => x.Operations).ToArray())
                operation.ShouldRun = false;
            await base.StopAsync(cancellationToken);
        }
        private static TimeSpan CalculateNextDelay(WorkerPool pool)
        {
            DateTime calculationStart = DateTime.UtcNow;
            DateTime earliestRun = DateTime.MaxValue;
            bool stragglersFound = false;
            foreach (ScheduledOperation operation in pool.Operations)
            {
                //Here we catch straggler operations which were not planned
                //to run in the last round, and missed their next planned
                //run due to long executions in the last round.
                
                if (!operation.ShouldRun && operation.NextRun <= calculationStart)
                {
                    operation.ShouldRun = true;
                    stragglersFound = true;
                }
                else
                {
                    operation.ShouldRun = false;
                }
                
                operation.CalculateNextRun();
                if(earliestRun>operation.NextRun) earliestRun = operation.NextRun;
            }

            SyncOperationsWithIdenticalCycleSpan(pool);
            
            foreach (ScheduledOperation operation in pool.Operations)
            {
                //We do another straggler check to catch up on operations
                //scheduled with a TimeSpan, which ran in the last round,
                //but should already run again due to long executions in
                //the last round.
                
                if (operation.NextRun <= calculationStart)
                {
                    operation.ShouldRun = true;
                    stragglersFound = true;
                }
                else
                {
                    if (operation.NextRun == earliestRun)
                    {
                        operation.ShouldRun = true;
                    }
                }
            }
            
            return stragglersFound ? TimeSpan.Zero : earliestRun - DateTime.UtcNow;
        }
        
        //Unlike operations scheduled with a Cron expression, operations scheduled with
        //a TimeSpan do not have inherently deterministic execution times. Sequential
        //execution can result in lags which will accumulate over time. To prevent this,
        //we sync operations with identical TimeSpan interval, so that they always run
        //in the same cycle.
        private static void SyncOperationsWithIdenticalCycleSpan(WorkerPool pool)
        {
            foreach (int[] group in pool.CycleSpanOperationsSyncIndexMaps)
            {
                DateTime earliestRun = pool.Operations.Where((_, i)=>Array.Exists(group, x=> x==i))
                                           .Select(x=>x.NextRun)
                                           .Min();
                
                foreach (var index in group)
                {
                    pool.Operations[index].NextRun = earliestRun;
                }
            }
        }
    }

}