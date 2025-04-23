namespace BackgroundNinja;

/// <summary>
/// Represents an operation that runs in the background according to a specified schedule.
/// </summary>
/// <remarks>
/// The schedule can be configured either via a cron expression (<see cref="string"/>) or
/// via a cycle span (<see cref="TimeSpan"/>).</remarks>
public class BackgroundOperation
{
    internal string? Cron { get; }
    internal bool CronHasSeconds { get; }
    internal TimeSpan? CycleSpan { get; }
    internal Func<IServiceProvider, Task> OperationFactory { get; }
    internal TimeZoneInfo CronTimeZone { get; }
    internal RunMode Mode { get; }

    private BackgroundOperation(Func<IServiceProvider, Task> operationFactory, RunMode mode = RunMode.Sequential, string? cronExpression = null, bool cronHasSeconds = false, TimeZoneInfo? cronTimeZone = null, TimeSpan? cycleSpan = null)
    {
        Cron = cronExpression;
        CycleSpan = cycleSpan;
        CronHasSeconds = cronHasSeconds;
        OperationFactory = operationFactory;
        CronTimeZone = cronTimeZone ?? TimeZoneInfo.Utc;
        Mode = mode;
    }
    internal BackgroundOperation(BackgroundOperation operation): 
        this(operation.OperationFactory, operation.Mode, operation.Cron, operation.CronHasSeconds, operation.CronTimeZone,  operation.CycleSpan){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cycleSpan">
    /// Cycle span representing the interval between each operation run.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    public BackgroundOperation(TimeSpan cycleSpan, Func<IServiceProvider, Task> operationFactory) :
        this(operationFactory, cycleSpan: cycleSpan){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="T:BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cycleSpan">
    /// Cycle span representing the interval between each operation run.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    /// <param name="mode">
    /// The execution mode in which the operation will run. Default is <see cref="RunMode.Sequential"/>.
    /// For more information see <see cref="RunMode"/>.
    /// </param>
    public BackgroundOperation(TimeSpan cycleSpan, Func<IServiceProvider, Task> operationFactory, RunMode mode) :
        this(operationFactory, cycleSpan: cycleSpan, mode: mode){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    public BackgroundOperation(string cronExpression, Func<IServiceProvider, Task> operationFactory) :
        this(operationFactory, cronExpression: cronExpression){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    /// <param name="mode">
    /// The execution mode in which the operation will run. Default is <see cref="RunMode.Sequential"/>.
    /// For more information see <see cref="RunMode"/>.
    /// </param>
    public BackgroundOperation(string cronExpression, Func<IServiceProvider, Task> operationFactory, RunMode mode) :
        this(operationFactory, cronExpression: cronExpression, mode: mode){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronTimeZone">
    /// <summary>
    /// The time zone applicable for the Cron expression. Defaults is <see cref="TimeZoneInfo.Utc"/>.
    /// </summary>
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    public BackgroundOperation(string cronExpression, TimeZoneInfo cronTimeZone, Func<IServiceProvider, Task> operationFactory) :
        this(operationFactory, cronExpression: cronExpression, cronTimeZone: cronTimeZone){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronTimeZone">
    /// <summary>
    /// The time zone applicable for the Cron expression. Defaults is <see cref="TimeZoneInfo.Utc"/>.
    /// </summary>
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    /// <param name="mode">
    /// The execution mode in which the operation will run. Default is <see cref="RunMode.Sequential"/>.
    /// For more information see <see cref="RunMode"/>.
    /// </param>
    public BackgroundOperation(string cronExpression, TimeZoneInfo cronTimeZone, Func<IServiceProvider, Task> operationFactory, RunMode mode) :
        this(operationFactory, cronExpression: cronExpression, cronTimeZone: cronTimeZone, mode: mode){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronHasSeconds">
    /// Indicates whether the Cron expression includes seconds precision. More information
    /// <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    public BackgroundOperation(string cronExpression, bool cronHasSeconds, Func<IServiceProvider, Task> operationFactory) :
        this(operationFactory, cronExpression: cronExpression, cronHasSeconds: cronHasSeconds){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronHasSeconds">
    /// Indicates whether the Cron expression includes seconds precision. More information
    /// <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    /// <param name="mode">
    /// The execution mode in which the operation will run. Default is <see cref="RunMode.Sequential"/>.
    /// For more information see <see cref="RunMode"/>.
    /// </param>
    public BackgroundOperation(string cronExpression, bool cronHasSeconds, Func<IServiceProvider, Task> operationFactory, RunMode mode) :
        this(operationFactory, cronExpression: cronExpression, cronHasSeconds: cronHasSeconds, mode: mode){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronHasSeconds">
    /// Indicates whether the Cron expression includes seconds precision. More information
    /// <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronTimeZone">
    /// <summary>
    /// The time zone applicable for the Cron expression. Defaults is <see cref="TimeZoneInfo.Utc"/>.
    /// </summary>
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    public BackgroundOperation(string cronExpression, bool cronHasSeconds, TimeZoneInfo cronTimeZone, Func<IServiceProvider, Task> operationFactory) :
        this(operationFactory, cronExpression: cronExpression, cronHasSeconds: cronHasSeconds, cronTimeZone: cronTimeZone){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperation"/> class.
    /// </summary>
    /// <param name="cronExpression">
    /// Cron expression specifying the run schedule of the operation. Uses the Cronos syntax,
    /// read more <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronHasSeconds">
    /// Indicates whether the Cron expression includes seconds precision. More information
    /// <see href="https://github.com/HangfireIO/Cronos?tab=readme-ov-file#cron-format">here</see>.
    /// </param>
    /// <param name="cronTimeZone">
    /// <summary>
    /// The time zone applicable for the Cron expression. Defaults is <see cref="TimeZoneInfo.Utc"/>.
    /// </summary>
    /// </param>
    /// <param name="operationFactory">
    /// Factory method to create the task executed in the operation. For example:
    /// <para>
    /// <code>
    /// async x => {
    /// x.GetRequiredService&lt;IMemoryCache&gt;().Set("gnomeCount", 1);
    /// await Task.Delay(TimeSpan.FromSeconds(6));
    /// }
    /// </code>
    /// </para>
    /// </param>
    /// <param name="mode">
    /// The execution mode in which the operation will run. Default is <see cref="RunMode.Sequential"/>.
    /// For more information see <see cref="RunMode"/>.
    /// </param>
    public BackgroundOperation(string cronExpression, bool cronHasSeconds, TimeZoneInfo cronTimeZone, Func<IServiceProvider, Task> operationFactory, RunMode mode) :
        this(operationFactory, cronExpression: cronExpression, cronHasSeconds: cronHasSeconds, cronTimeZone: cronTimeZone, mode: mode){}
}