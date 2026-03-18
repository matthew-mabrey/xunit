using Xunit.Sdk;
using Xunit.v3.Utility;

namespace Xunit.v3;

/// <summary>
/// A base class that provides default behavior when running test cases which are assumed
/// to result in one or more tests (that implement <see cref="ITest"/>).
/// </summary>
/// <typeparam name="TContext">The context type used by the runner</typeparam>
/// <typeparam name="TTestCase">The type of the test case used by the test framework. Must
/// derive from <see cref="ITestCase"/>.</typeparam>
/// <typeparam name="TTest">The type of the test that is generated from the test case. Must
/// derive from <see cref="ITest"/>.</typeparam>
public abstract class TestCaseRunner<TContext, TTestCase, TTest> :
	TestCaseRunnerBase<TContext, TTestCase>
		where TContext : TestCaseRunnerContext<TTestCase, TTest>
		where TTestCase : class, ITestCase
		where TTest : class, ITest
{
	/// <summary>
	/// Override this method to fail an individual test.
	/// </summary>
	/// <remarks>
	/// By default, uses <see cref="XunitRunnerHelper"/> to fail the test cases.
	/// </remarks>
	/// <param name="ctxt">The context that describes the current test case</param>
	/// <param name="test">The test to be failed.</param>
	/// <param name="exception">The exception that was caused during startup.</param>
	/// <returns>Returns summary information about the test case run.</returns>
	protected virtual ValueTask<RunSummary> FailTest(
		TContext ctxt,
		TTest test,
		Exception exception) =>
			new(XunitRunnerHelper.FailTest(
				Guard.ArgumentNotNull(ctxt).MessageBus,
				ctxt.CancellationTokenSource,
				test,
				exception
			));

	/// <summary>
	/// Override this method to run an individual test.
	/// </summary>
	/// <param name="ctxt">The context that describes the current test method</param>
	/// <param name="test">The test to be run.</param>
	/// <returns>Returns summary information about the test run.</returns>
	protected abstract ValueTask<RunSummary> RunTest(
		TContext ctxt,
		TTest test);

	/// <inheritdoc/>
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
		Justification = "We guarantee that parallel ValueTasks are only awaited once.")]
	protected override async ValueTask<RunSummary> RunTestCase(
		TContext ctxt,
		Exception? exception)
	{
		Guard.ArgumentNotNull(ctxt);

		var summary = new RunSummary();
		var taskRunner = TestPipelineTaskRunner.Create(ctxt.CancellationTokenSource.Token);
		List<ValueTask<RunSummary>>? parallel = null;

		foreach (var test in ctxt.Tests)
		{
			ValueTask<RunSummary> task() => exception is null
				? RunTest(ctxt, test)
				: FailTest(ctxt, test, exception);

			if (ctxt.TestCaseParallelizationEnabled)
				(parallel ??= []).Add(taskRunner(task));
			else
				summary.Aggregate(await task());

			if (ctxt.CancellationTokenSource.IsCancellationRequested)
				break;
		}

		if (parallel?.Count > 0)
			foreach (var task in parallel)
				try
				{
					summary.Aggregate(await task);
				}
				catch (TaskCanceledException) { }

		return summary;
	}
}
