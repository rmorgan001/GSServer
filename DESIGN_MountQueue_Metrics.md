# MountQueue Long-Term Metrics Design

## Overview
This design adds lightweight, thread-safe metrics tracking to the MountQueue class to monitor command processing statistics over the lifetime of a queue session.

## Requirements
1. Track total number of commands processed
2. Track successful command executions
3. Track failed commands
4. Track timed-out commands
5. Track exceptions handled
6. Lightweight structure with clear names
7. Reset to zero on queue start
8. Accessible from other threads for session logging

## Design

### 1. Metrics Structure

```csharp
/// <summary>
/// Thread-safe statistics for mount command queue processing
/// </summary>
public class MountQueueStatistics
{
    private long _totalCommandsProcessed;
    private long _commandsSuccessful;
    private long _commandsFailed;
    private long _commandsTimedOut;
    private long _exceptionsHandled;

    /// <summary>
    /// Total number of commands that entered the processing queue
    /// </summary>
    public long TotalCommandsProcessed => Interlocked.Read(ref _totalCommandsProcessed);

    /// <summary>
    /// Number of commands that completed successfully
    /// </summary>
    public long CommandsSuccessful => Interlocked.Read(ref _commandsSuccessful);

    /// <summary>
    /// Number of commands that failed (excluding timeouts)
    /// </summary>
    public long CommandsFailed => Interlocked.Read(ref _commandsFailed);

    /// <summary>
    /// Number of commands that timed out waiting for completion
    /// </summary>
    public long CommandsTimedOut => Interlocked.Read(ref _commandsTimedOut);

    /// <summary>
    /// Number of exceptions caught during command processing
    /// </summary>
    public long ExceptionsHandled => Interlocked.Read(ref _exceptionsHandled);

    /// <summary>
    /// Resets all statistics to zero
    /// </summary>
    internal void Reset()
    {
        Interlocked.Exchange(ref _totalCommandsProcessed, 0);
        Interlocked.Exchange(ref _commandsSuccessful, 0);
        Interlocked.Exchange(ref _commandsFailed, 0);
        Interlocked.Exchange(ref _commandsTimedOut, 0);
        Interlocked.Exchange(ref _exceptionsHandled, 0);
    }

    /// <summary>
    /// Increments the total commands processed counter
    /// </summary>
    internal void IncrementTotalProcessed()
    {
        Interlocked.Increment(ref _totalCommandsProcessed);
    }

    /// <summary>
    /// Increments the successful commands counter
    /// </summary>
    internal void IncrementSuccessful()
    {
        Interlocked.Increment(ref _commandsSuccessful);
    }

    /// <summary>
    /// Increments the failed commands counter
    /// </summary>
    internal void IncrementFailed()
    {
        Interlocked.Increment(ref _commandsFailed);
    }

    /// <summary>
    /// Increments the timed out commands counter
    /// </summary>
    internal void IncrementTimedOut()
    {
        Interlocked.Increment(ref _commandsTimedOut);
    }

    /// <summary>
    /// Increments the exceptions handled counter
    /// </summary>
    internal void IncrementExceptions()
    {
        Interlocked.Increment(ref _exceptionsHandled);
    }

    /// <summary>
    /// Returns a formatted string with all statistics
    /// </summary>
    /// <returns>Human-readable statistics summary</returns>
    public override string ToString()
    {
        return $"Total:{TotalCommandsProcessed}|Success:{CommandsSuccessful}|Failed:{CommandsFailed}|TimedOut:{CommandsTimedOut}|Exceptions:{ExceptionsHandled}";
    }
}
```

### 2. Integration into MountQueue

#### Add Field
```csharp
private static MountQueueStatistics _statistics;
```

#### Add Public Property
```csharp
/// <summary>
/// Gets the current session statistics for the mount command queue
/// </summary>
public static MountQueueStatistics Statistics => _statistics;
```

#### Initialize in Start() Method
```csharp
public static void Start()
{
    try
    {
        Stop();
        if (_cts == null) _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _actions = new Actions();
        _actions.InitializeAxes();
        _commandBlockingCollection = new BlockingCollection<IMountCommand>();
        
        // Initialize and reset statistics for new session
        if (_statistics == null)
        {
            _statistics = new MountQueueStatistics();
        }
        _statistics.Reset();
        
        IsRunning = true;

        _processingTask = Task.Factory.StartNew(() =>
        {
            // ... existing code
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    catch (Exception ex)
    {
        throw;
    }
}
```

#### Update ProcessCommandQueue Method
```csharp
private static void ProcessCommandQueue(IMountCommand command)
{
    // Increment total processed counter as soon as command is dequeued
    _statistics?.IncrementTotalProcessed();

    // ... existing variable declarations ...

    try
    {
        // ... existing diagnostic code ...

        if (!IsRunning || _cts.IsCancellationRequested || !Actions.IsConnected)
        {
            command.Exception = new MountException(ErrorCode.ErrQueueFailed, "Queue stopped or not connected");
            command.Successful = false;
            _statistics?.IncrementFailed();
        }
        else
        {
            executionStart = HiResDateTime.UtcNow;

            command.Execute(_actions);

            // Track success/failure based on command result
            if (command.Successful)
            {
                _statistics?.IncrementSuccessful();
            }
            else
            {
                _statistics?.IncrementFailed();
            }

            // ... existing diagnostic and warning code ...
        }
    }
    catch (Exception e)
    {
        command.Exception = e;
        command.Successful = false;
        _statistics?.IncrementFailed();
        _statistics?.IncrementExceptions();

        // ... existing diagnostic logging ...
    }
    finally
    {
        // Always signal completion - success or failure
        command.CompletionEvent.Set();
    }
}
```

#### Update GetCommandResult Method
```csharp
public static IMountCommand GetCommandResult(IMountCommand command)
{
    try
    {
        if (!IsRunning || _cts?.IsCancellationRequested != false)
        {
            var a = "Queue | IsRunning:" + IsRunning + "| IsCancel:" + _cts?.IsCancellationRequested;
            if (command.Exception != null) { a += "| Ex:" + command.Exception.Message; }
            var e = new MountException(ErrorCode.ErrQueueFailed, a);
            command.Exception = e;
            command.Successful = false;
            // Don't increment statistics here - command never entered queue
            return command;
        }

        // Wait for command to complete with 22 second timeout
        if (command.CompletionEvent.Wait(22000, _cts.Token))
        {
            // Command completed - return it
            // Statistics already updated in ProcessCommandQueue
            return command;
        }

        // Timeout occurred
        _statistics?.IncrementTimedOut();
        var ex = new MountException(ErrorCode.ErrQueueFailed, $"Queue Read Timeout {command.Id}, {command}");
        command.Exception = ex;
        command.Successful = false;
        return command;
    }
    catch (OperationCanceledException)
    {
        _statistics?.IncrementExceptions();
        command.Exception = new MountException(ErrorCode.ErrQueueFailed, "Operation cancelled");
        command.Successful = false;
        return command;
    }
    catch (Exception e)
    {
        _statistics?.IncrementExceptions();
        command.Exception = e;
        command.Successful = false;
        return command;
    }
}
```

### 3. Usage Example from Session Log

```csharp
// In another thread (e.g., session log writer)
var stats = MountQueue.Statistics;
if (stats != null)
{
    sessionLog.WriteLine($"Mount Queue Statistics: {stats}");
    // Or access individual properties
    sessionLog.WriteLine($"Total Commands: {stats.TotalCommandsProcessed}");
    sessionLog.WriteLine($"Success Rate: {(stats.CommandsSuccessful * 100.0 / stats.TotalCommandsProcessed):F2}%");
}
```

## Design Rationale

### Thread Safety
- Uses `Interlocked` operations for all counter updates (thread-safe without locks)
- Read-only properties use `Interlocked.Read()` for atomic 64-bit reads on 32-bit systems
- `internal` increment methods prevent external code from corrupting statistics

### Performance
- Zero allocation after initialization
- No locks or synchronization overhead
- Minimal CPU overhead (just atomic increments)
- No string allocations except when `ToString()` is explicitly called

### Accuracy
- Counters incremented at precise points:
  - `TotalProcessed`: When command is dequeued (entered processing)
  - `Successful/Failed`: Based on `command.Successful` after execution
  - `TimedOut`: When `GetCommandResult` times out
  - `Exceptions`: When exceptions are caught
- Clear separation between failed and timed-out commands

### Maintainability
- Self-documenting property and method names
- XML documentation on all public members
- Encapsulated in separate class (single responsibility)
- Easy to extend with additional metrics if needed

## Statistics Semantics

### Counter Relationships
- `TotalCommandsProcessed` = Commands that entered `ProcessCommandQueue()`
- `CommandsSuccessful` + `CommandsFailed` ≤ `TotalCommandsProcessed`
  - (Commands that completed processing)
- `CommandsTimedOut` = Commands that never completed (waited 22 seconds)
  - These are NOT counted in TotalProcessed (never dequeued)
- `ExceptionsHandled` = Any exception caught in either method
  - Subset of failed/timed out commands

### Edge Cases
- Commands rejected before queueing (queue full, not running): Not counted
- Commands that fail during execution: Counted in Failed + Exceptions (if exception thrown)
- Commands timing out: Counted in TimedOut + possibly in TotalProcessed (if dequeued)
- Operation cancelled: Counted in Exceptions

## Benefits
1. **Operational visibility**: Track queue health over time
2. **Debugging aid**: Identify patterns in failures/timeouts
3. **Performance monitoring**: Calculate success rates, error rates
4. **Session logging**: Provide summary statistics for troubleshooting
5. **Zero performance impact**: Only atomic increments, no allocations

## Future Enhancements (Optional)
- Add timing statistics (min/max/avg execution time)
- Add per-command-type breakdown
- Add rolling window statistics (last hour, last session)
- Expose via monitoring interface for real-time dashboards
