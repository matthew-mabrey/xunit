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
			var testTcs1 = new TaskCompletionSource<bool>(TaskCreationOptions.None);
			var testTcs2 = new TaskCompletionSource<bool>(TaskCreationOptions.None);
			var testCase = Mocks.CoreTestCase(testCaseDisplayName: "TestCase1");
			var test1 = Mocks.CoreTest(testCase: testCase, testDisplayName: "Test1");
			var test2 = Mocks.CoreTest(testCase: testCase, testDisplayName: "Test2");
			
			var finishedTask = Task.WhenAll(testTcs1.Task, testTcs2.Task);
			var runner = new TestableCoreTestCaseRunner([test1, test2], runTestCase);

			await runner.RunAsync();
			
			async ValueTask<RunSummary> runTestCase(ICoreTest test)
			{
				if (test == test1)
				{
					testTcs1.TrySetResult(true);
				}
				else
				{
					testTcs2.TrySetResult(true);
				}

				await finishedTask;
				return new RunSummary();
			};
		}

		[Fact]
		public async ValueTask SyncTestCases()
		{
			var messages = new List<string>();
			var testCase = Mocks.CoreTestCase(testCaseDisplayName: "TestCase1");
			var test1 = Mocks.CoreTest(testCase: testCase, testDisplayName: "Test1");
			var test2 = Mocks.CoreTest(testCase: testCase, testDisplayName: "Test2");
			
			var runner = new TestableCoreTestCaseRunner([test1, test2], runTestCase);

			await runner.RunAsync();
			
			// let each test finish before the next one runs, despite sleeping. However, we don't know which one
			// gets to go first, so we look at the first one to see which one it is, and make sure the post-sleep happens
			// directly after the pre-sleep
			var firstMessage = messages[0];
			Assert.Contains("pre-sleep", firstMessage);
			Assert.Equal(firstMessage.Replace("pre-sleep", "post-sleep"), messages[1]);

			var thirdMessage = messages[2];
			Assert.NotEqual(firstMessage, thirdMessage);
			Assert.Contains("pre-sleep", thirdMessage);
			Assert.Equal(thirdMessage.Replace("pre-sleep", "post-sleep"), messages[3]);
			
			async ValueTask<RunSummary> runTestCase(ICoreTest test)
			{
				messages.Add($"{test.TestDisplayName} pre-sleep");
				
				await Task.Delay(30, TestContext.Current.CancellationToken);
				
				messages.Add($"{test.TestDisplayName} post-sleep");

				return new RunSummary { Total = 1 };
			};
		}
	}

	class TestableCoreTestCaseRunner:
		CoreTestCaseRunner<TestableCoreTestCaseRunner.TestableContext, ICoreTestCase, ICoreTest>
	{
		public readonly ExceptionAggregator Aggregator = new();
		public readonly CancellationTokenSource CancellationTokenSource = new();
		public readonly SpyMessageBus MessageBus = new();
		private readonly ICoreTest[] _tests;
		private readonly Func<ICoreTest, ValueTask<RunSummary>>? _runTestLamda;

		public TestableCoreTestCaseRunner(ICoreTestCase testCase)
		{
			_tests = [Mocks.CoreTest(testCase: testCase)];
		}
		
		public TestableCoreTestCaseRunner(ICoreTest[] tests, Func<ICoreTest, ValueTask<RunSummary>>? runTestLamda)
		{
			_tests = tests;
			_runTestLamda = runTestLamda;
		}
		
		public async ValueTask<RunSummary> RunAsync()
		{
			Guard.ArgumentNotNullOrEmpty(_tests);
			var testCase = _tests[0].TestCase;
			
			await using var ctxt = new TestableContext(
				testCase,
				_tests,
				ExplicitOption.Off,
				MessageBus,
				Aggregator,
				testCase.TestCaseDisplayName,
				testCase.SkipReason,
				CancellationTokenSource,
				runTest: _runTestLamda ?? (_ => new ValueTask<RunSummary>(new RunSummary { Total = 1 }))
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
			CancellationTokenSource cancellationTokenSource,
			Func<ICoreTest, ValueTask<RunSummary>> runTest) :
				CoreTestCaseRunnerContext<ICoreTestCase, ICoreTest>(testCase, tests, explicitOption, messageBus, aggregator, displayName, skipReason, cancellationTokenSource, parallelizationSemaphore: null)
		{
			public override ValueTask<RunSummary> RunTest(ICoreTest test) => runTest(test);
		}
	}
}
