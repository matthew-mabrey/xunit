using Xunit.v3.Utility;

namespace Xunit.v3;

/// <summary>
/// Base test assembly runner for xUnit.net v3 tests.
/// </summary>
/// <remarks>
/// This class is shared between reflection-based and code generation-based tests.
/// </remarks>
public class CoreTestCaseRunner<TContext, TTestCase, TTest> : TestCaseRunner<TContext, TTestCase, TTest>
	where TContext : CoreTestCaseRunnerContext<TTestCase, TTest>
	where TTestCase : class, ICoreTestCase
	where TTest : class, ICoreTest
{
	/// <inheritdoc/>
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly",
		Justification = "We guarantee that parallel ValueTasks are only awaited once.")]
	protected override async ValueTask<RunSummary> RunTestCase(
		TContext ctxt,
		Exception? exception)
	{
		Guard.ArgumentNotNull(ctxt);
		
		var preInvokeFailed = true;

		if (exception is null)
		{
			try
			{
				ctxt.TestCase.PreInvoke();
				preInvokeFailed = false;
			}
			catch (Exception ex)
			{
				exception = ex;
			}
		}

		var summary = new RunSummary();
		if (ctxt.TestCase.TestCollection.EnableTestCaseParallelization)
		{
			var taskRunner = TestPipelineTaskRunner.Create(ctxt.CancellationTokenSource.Token);
			List<ValueTask<RunSummary>> parallel = [];

			foreach (var test in ctxt.Tests)
			{
				if (ctxt.CancellationTokenSource.IsCancellationRequested)
					break;

				parallel.Add(taskRunner(task));
				
				ValueTask<RunSummary> task() => exception is null
					? RunTest(ctxt, test)
					: FailTest(ctxt, test, exception);
			}

			foreach (var task in parallel)
			{
				try
				{
					summary.Aggregate(await task);
				}
				catch (TaskCanceledException)
				{
				}
			}
		}
		else
		{
			summary = await base.RunTestCase(ctxt, exception);
		}

		if (!preInvokeFailed)
			ctxt.Aggregator.Run(ctxt.TestCase.PostInvoke);

		return summary;
	}

	/// <summary>
	/// Runs the test via the context.
	/// </summary>
	/// <inheritdoc/>
	protected override async ValueTask<RunSummary> RunTest(
		TContext ctxt,
		TTest test)
	{
		Guard.ArgumentNotNull(ctxt);

		// only acquire the semaphore here if the collection has enabled test case parallelization, otherwise
		// it is acquired when the test collection is started
		var parallelizationSemaphore = ctxt.EnableTestCaseParallelization
			? ctxt.ParallelizationSemaphore
			: null;
		
		if (parallelizationSemaphore != null)
		{
			await parallelizationSemaphore.WaitAsync(ctxt.CancellationTokenSource.Token);
		}

		try
		{
			return await ctxt.RunTest(test);
		}
		finally
		{
			parallelizationSemaphore?.Release();
		}
	}
}
