using Xunit.Sdk;

namespace Xunit.v3;

/// <summary>
/// Base class for all execution pipeline context classes.
/// </summary>
/// <param name="explicitOption">The user's choice on how to treat explicit tests</param>
/// <param name="messageBus">The message bus to send execution messages to</param>
/// <param name="aggregator">The exception aggregator</param>
/// <param name="cancellationTokenSource">The cancellation token source</param>
/// <param name="parallelizationSemaphore">The semaphore used to limit test case parallelization.</param>
public class ContextBase(
	ExplicitOption explicitOption,
	IMessageBus messageBus,
	ExceptionAggregator aggregator,
	CancellationTokenSource cancellationTokenSource,
	SemaphoreSlim? parallelizationSemaphore = null) :
		IAsyncLifetime
{
	/// <summary>
	/// Gets the aggregator used for reporting exceptions.
	/// </summary>
	public ExceptionAggregator Aggregator { get; } = aggregator;

	/// <summary>
	/// Gets a value indicating whether test case parallelization is enabled.
	/// </summary>
	public bool TestCaseParallelizationEnabled { get; }

	/// <summary>
	/// Gets the semaphore used to limit parallelization within the execution pipeline.
	/// </summary>
	public SemaphoreSlim? ParallelizationSemaphore { get; } = parallelizationSemaphore;

	/// <summary>
	/// Gets the cancellation token source used for cancelling test execution.
	/// </summary>
	public CancellationTokenSource CancellationTokenSource { get; } = Guard.ArgumentNotNull(cancellationTokenSource);

	/// <summary>
	/// Gets a flag which indicates how explicit tests should be handled.
	/// </summary>
	public ExplicitOption ExplicitOption { get; } = explicitOption;

	/// <summary>
	/// Gets the message bus to send execution engine messages to.
	/// </summary>
	public IMessageBus MessageBus { get; } = Guard.ArgumentNotNull(messageBus);

	/// <inheritdoc/>
	public virtual ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		return default;
	}

	/// <inheritdoc/>
	public virtual ValueTask InitializeAsync() =>
		default;
}
