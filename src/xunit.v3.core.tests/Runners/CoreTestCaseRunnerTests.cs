using Xunit;
using Xunit.Sdk;
using Xunit.v3;

public class CoreTestCaseRunnerTests
{
	public class InvokeHandlers
	{
		[Fact]
		public async ValueTask RunsPreAndPostInvokeByDefault()
		{
			var operations = new List<string>();
			var testCase = Mocks.CoreTestCase(
				preInvoke: () => operations.Add("PreInvoke()"),
				postInvoke: () => operations.Add("PostInvoke()")
			);
			var runner = new TestableCoreTestCaseRunner(testCase);

			var result = await runner.RunAsync();

			Assert.Equal(1, result.Total);
			Assert.Equal(0, result.Failed);
			Assert.Equal(0, result.Skipped);
			Assert.Equal(0, result.NotRun);
			Assert.Collection(
				operations,
				op => Assert.Equal("PreInvoke()", op),
				op => Assert.Equal("PostInvoke()", op)
			);
		}

		[Fact]
		public async ValueTask PreInvokeFails_SkipsPostInvoke()
		{
			var operations = new List<string>();
			var testCase = Mocks.CoreTestCase(
				preInvoke: () => { operations.Add("PreInvoke()"); throw new DivideByZeroException(); },
				postInvoke: () => operations.Add("PostInvoke()")
			);
			var runner = new TestableCoreTestCaseRunner(testCase);

			var result = await runner.RunAsync();

			Assert.Equal(1, result.Total);
			Assert.Equal(1, result.Failed);
			Assert.Equal(0, result.Skipped);
			Assert.Equal(0, result.NotRun);
			Assert.Equal("PreInvoke()", Assert.Single(operations));
		}

		[Fact]
		public async ValueTask AggregatorContainsException_SkipsPreAndPostInvoke()
		{
			var operations = new List<string>();
			var testCase = Mocks.CoreTestCase(
				preInvoke: () => operations.Add("PreInvoke()"),
				postInvoke: () => operations.Add("PostInvoke()")
			);
			var runner = new TestableCoreTestCaseRunner(testCase);
			runner.Aggregator.Add(new DivideByZeroException());

			var result = await runner.RunAsync();

			Assert.Equal(1, result.Total);
			Assert.Equal(1, result.Failed);
			Assert.Equal(0, result.Skipped);
			Assert.Equal(0, result.NotRun);
			Assert.Empty(operations);
		}

		// Test Cases:
		// 1. Test Case Collection (DisableParallelization = false, EnableTestCaseParallelization = true) has two test cases run in parallel within collection
		// 2. Test Case Collection (DisableParallelization = true, EnableTestCaseParallelization = true) has two cases run in parallel, and sync with another outside collection
		// 3. Test Case Collection (DisableParallelization = false, EnableTestCaseParallelization = false) test collections run parallel, and cases run synchronously 
		// 4. Test Case Collection (DisableParallelization = true, EnableTestCaseParallelization = false) test collections run synchronously, and cases run synchronously 
		// 5. TestAssembly setting overrides test collection setting
		
		[Fact]
		public async ValueTask ParallelTestCases()
		{
			var testCollection1 = Mocks.CoreTestCollection(uniqueID: "1");
			var testCase1 = Mocks.CoreTestCase(testCaseDisplayName: "TestCase1", testMethod: Mocks.CoreTestMethod(testClass: Mocks.CoreTestClass(testCollection: testCollection1)));
			var testCollection2 = Mocks.CoreTestCollection(uniqueID: "2");
			var testCase2 = Mocks.CoreTestCase(testCaseDisplayName: "TestCase2", testMethod: Mocks.CoreTestMethod(testClass: Mocks.CoreTestClass(testCollection: testCollection2)));
			var options = TestData.TestFrameworkExecutionOptions(enableTestCaseParallelization: true);
			var runner = new TestableCoreTestAssemblyRunner([testCase1, testCase2], options);

			await runner.RunAsync();

			// When it's parallel, we should always get pre, pre, post, post
			var messages = DiagnosticMessageSink.Messages.OfType<IDiagnosticMessage>().Select(m => m.Message).ToArray();
			Assert.Equal(4, messages.Length);
			var firstPreSleep = messages[0];
			Assert.EndsWith("pre-sleep", firstPreSleep);
			Assert.Equal(firstPreSleep.Replace("pre-", "post-"), messages[1]);
			var secondPreSleep = messages[2];
			Assert.EndsWith("pre-sleep", secondPreSleep);
			Assert.Equal(secondPreSleep.Replace("pre-", "post-"), messages[3]);
		}
	}

	class TestableCoreTestCaseRunner(ICoreTestCase testCase) :
		CoreTestCaseRunner<TestableCoreTestCaseRunner.TestableContext, ICoreTestCase, ICoreTest>
	{
		public readonly ExceptionAggregator Aggregator = new();
		public readonly CancellationTokenSource CancellationTokenSource = new();
		public readonly SpyMessageBus MessageBus = new();

		public async ValueTask<RunSummary> RunAsync()
		{
			await using var ctxt = new TestableContext(
				testCase,
				[Mocks.CoreTest(testCase: testCase)],
				ExplicitOption.Off,
				MessageBus,
				Aggregator,
				testCase.TestCaseDisplayName,
				testCase.SkipReason,
				CancellationTokenSource
			);
			await ctxt.InitializeAsync();

			return await Run(ctxt);
		}

		public class TestableContext(
			ICoreTestCase testCase,
			IReadOnlyCollection<ICoreTest> tests,
			ExplicitOption explicitOption,
			IMessageBus messageBus,
			ExceptionAggregator aggregator,
			string displayName,
			string? skipReason,
			CancellationTokenSource cancellationTokenSource) :
				CoreTestCaseRunnerContext<ICoreTestCase, ICoreTest>(testCase, tests, explicitOption, messageBus, aggregator, displayName, skipReason, cancellationTokenSource, parallelizationSemaphore: null)
		{
			public override ValueTask<RunSummary> RunTest(ICoreTest test) =>
				new(new RunSummary { Total = 1 });
		}
	}
}
