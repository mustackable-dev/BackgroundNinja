using Cronos;

namespace BackgroundNinja;

class ScheduledOperation : BackgroundOperation
{
    internal bool ShouldRun { get; set; }
    internal DateTime NextRun { get; set; }
    internal DateTime LastRun { get; set; } = DateTime.UtcNow;

    internal ScheduledOperation(BackgroundOperation operation) : base(operation)
    {
        CalculateNextRun();
    }
    internal void CalculateNextRun()
    {
        if (CycleSpan is not null)
        {
            NextRun = LastRun + (CycleSpan ?? TimeSpan.Zero);
        }
                
        if(Cron is not null)
        {
            CronExpression cron = CronExpression.Parse(Cron, CronHasSeconds ? CronFormat.IncludeSeconds: CronFormat.Standard);
            NextRun = cron.GetNextOccurrence(DateTime.UtcNow, CronTimeZone) ?? DateTime.MaxValue;
            
            //Occasionally, we might run into a millisecond difference issue that causes
            //the Cronos to return the occurence which is one behind the one we are
            //interested in. Since the minimum level of precision is seconds, not
            //milliseconds, we do the check below to catch these cases and use the
            //right occurence.
            
            if ((NextRun - LastRun).TotalSeconds <= 1)
            {
                NextRun = cron.GetNextOccurrence(NextRun, CronTimeZone) ?? DateTime.MaxValue;;
            }
        }
    }
}