using System.Collections.Generic;
using System.Threading;
using Xunit.Sdk;

namespace Xunit.v3;

/// <summary>
/// Context class for <see cref="XunitTestClassRunner"/>.
/// </summary>
/// <param name="testClass">The test class</param>
/// <param name="testCases">The test from the test class</param>
/// <param name="explicitOption">The user's choice on how to treat explicit tests</param>
/// <param name="messageBus">The message bus to send execution messages to</param>
/// <param name="testCaseOrderer">The orderer used to sort the test cases for executiong</param>
/// <param name="aggregator">The exception aggregator</param>
/// <param name="cancellationTokenSource">The cancellation token source</param>
/// <param name="collectionFixtureMappings">The fixtures attached to the test collection</param>
/// <param name="parallelizationSemaphore">The semaphore used to limit parallelization within the execution pipeline.</param>
public class XunitTestClassRunnerContext(
	IXunitTestClass testClass,
	IReadOnlyCollection<IXunitTestCase> testCases,
	ExplicitOption explicitOption,
	IMessageBus messageBus,
	ITestCaseOrderer testCaseOrderer,
	ExceptionAggregator aggregator,
	CancellationTokenSource cancellationTokenSource,
	FixtureMappingManager collectionFixtureMappings,
	SemaphoreSlim? parallelizationSemaphore = null) :
	XunitTestClassRunnerBaseContext<IXunitTestClass, IXunitTestCase>(testClass, testCases, explicitOption, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings, parallelizationSemaphore)
{ }
