namespace Xunit.Sdk;

/// <summary>Options representing how much parallelization to enable within the test execution pipeline.</summary>
[Flags]
public enum ParallelizationOptions
{
	/// <summary>
	/// Parallelization in the test execution pipeline is fully disabled. Indicating only one test can be running at a time.
	/// </summary>
	Disabled = 0,

	/// <summary>Whether the test assemblies can run in parallel.</summary>
	Assemblies = 1 << 0,

	/// <summary>Whether the test collections of a test assembly can run in parallel.</summary>
	Collections = 1 << 1,

	/// <summary>Whether the test classes of a test collection can run in parallel.</summary>
	Classes = 1 << 2,

	/// <summary>Whether the test methods of a test class can run in parallel.</summary>
	Methods = 1 << 3,

	/// <summary>Whether the test cases of a test method can run in parallel.</summary>
	TestCases = 1 << 4,

	/// <summary>Whether the tests of a test case can be run in parallel.</summary>
	Tests = 1 << 5,

	/// <summary>The default parallelization options used for the test execution pipeline.</summary>
	Default = Collections,

	/// <summary>Whether the test execution pipeline is fully parallelized.</summary>
	All = Assemblies | Collections | Classes | Methods | TestCases | Tests,
}
