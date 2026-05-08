using Xunit.Sdk;

namespace Xunit.v3;

/// <summary>
/// Context class for <see cref="XunitTestMethodRunner"/>.
/// </summary>
/// <param name="testMethod">The test method</param>
/// <param name="testCases">The test cases from the test method</param>
/// <param name="explicitOption">The user's choice on how to treat explicit tests</param>
/// <param name="messageBus">The message bus to send execution messages to</param>
/// <param name="aggregator">The exception aggregator</param>
/// <param name="cancellationTokenSource">The cancellation token source</param>
/// <param name="constructorArguments">The constructor arguments for the test class</param>
/// <param name="parallelizationOptions">A value indicating whether test cases can be run in parallel.</param>
/// <param name="parallelizationSemaphore">The semaphore used to limit parallelization within the execution pipeline.</param>
/// <remarks>
/// This class is used for reflection-based tests.
/// </remarks>
public class XunitTestMethodRunnerContext(
	IXunitTestMethod testMethod,
	IReadOnlyCollection<IXunitTestCase> testCases,
	ExplicitOption explicitOption,
	IMessageBus messageBus,
	ExceptionAggregator aggregator,
	CancellationTokenSource cancellationTokenSource,
	object?[] constructorArguments,
	ParallelizationOptions parallelizationOptions,
	SemaphoreSlim? parallelizationSemaphore) :
	XunitTestMethodRunnerBaseContext<IXunitTestMethod, IXunitTestCase>(testMethod, testCases, explicitOption,
		messageBus, aggregator, cancellationTokenSource, constructorArguments, parallelizationOptions,
		parallelizationSemaphore)
{ }
