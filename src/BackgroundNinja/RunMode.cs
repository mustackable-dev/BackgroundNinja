namespace BackgroundNinja;

/// <summary>
/// Specifies the execution pattern of a <see cref="BackgroundOperation"/>.
/// </summary>
/// <remarks>
/// Operations are grouped in dedicated execution pools, based on their <see cref="RunMode"/>.
/// For every mode except <see cref="RunMode.Thread"/>, if an operation
/// runs for long enough to cause other operations to miss their schedule, the
/// <see cref="BackgroundWorkerService"/> will attempt to catch up immediately
/// on the stragglers' execution.
/// </remarks>
/// 
public enum RunMode
{
    /// <summary>
    /// Operations with identical schedules are executed one at a time in a sequence.
    /// The execution order depends on the definition order during the
    /// registration of the <see cref="BackgroundWorkerService"/>.
    /// </summary>
    Sequential,

    /// <summary>
    /// Operations with identical schedules are executed concurrently.
    /// </summary>
    Parallel,

    /// <summary>
    /// All operations are executed independently of one another.
    ///
    /// While this mode ensures strict compliance with schedule for each operation,
    /// you run the risk of ThreadPool starvation, depending on the intensity of
    /// your app and the executing hardware.
    /// </summary>
    Thread,
}