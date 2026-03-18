using Xunit.Sdk;

namespace Xunit.v3;

/// <summary>
/// Used to declare the default test collection behavior for the assembly. This is only valid at the assembly level,
/// and there can be only one.
/// </summary>
public interface ICollectionBehaviorAttribute
{
	/// <summary>
	/// Gets the collection factory type specified by this collection behavior attribute.
	/// </summary>
	Type? CollectionFactoryType { get; }

	/// <summary>
	/// Gets a value indicating whether all test parallelization is disabled.
	/// </summary>
	bool DisableTestParallelization { get; }

	/// <summary>
	/// Gets options which determine the amount of parallelization to allow for tests in this assembly by default.
	/// </summary>
	/// <remarks>
	/// Overridden by <see cref="CollectionDefinitionAttribute.ParallelismOptions"/> within collections, unless
	/// set to <see cref="ParallelismOptions.None"/>. In which case, all tests in the assembly will be run serially.
	/// </remarks>
	ParallelismOptions ParallelismOptions { get; }

	/// <summary>
	/// Determines how many tests can run in parallel with each other. If set to 0, the system will
	/// use <see cref="Environment.ProcessorCount"/>. If set to a negative number, then there will
	/// be no limit to the number of threads.
	/// </summary>
	int MaxParallelThreads { get; }

	/// <summary>
	/// Determines the parallel algorithm used when running tests in parallel.
	/// </summary>
	ParallelAlgorithm ParallelAlgorithm { get; }
}
