using Xunit.Sdk;

namespace Xunit;

/// <summary>
/// Used to declare a test collection container class. The container class gives
/// developers a place to attach interfaces like <see cref="IClassFixture{T}"/> and
/// <see cref="ICollectionFixture{T}"/> that will be applied to all tests classes
/// that are members of the test collection.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CollectionDefinitionAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionDefinitionAttribute" /> class.
	/// Use this constructor when collection references by test classes use the generic
	/// <see cref="CollectionAttribute{TCollectionDefinition}"/> attribute or refer to the
	/// fixture class using <see cref="CollectionAttribute(Type)"/>.
	/// </summary>
	public CollectionDefinitionAttribute()
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionDefinitionAttribute" /> class.
	/// Use this constructor when collection references by test classes use
	/// <see cref="CollectionAttribute(string)"/>.
	/// </summary>
	/// <param name="name">The test collection name.</param>
	public CollectionDefinitionAttribute(string name) =>
		Name = Guard.ArgumentNotNull(name);

	/// <summary>
	/// Gets or sets a flag which indicates whether this collection should not run in parallel with other collections in the assembly.
	/// </summary>
	public bool DisableParallelization
	{
		get => OptionalParallelismOptions == ParallelismOptions.None;
		set
		{
			if (value)
			{
				OptionalParallelismOptions = ParallelismOptions.None;
			}
		}
	}

	/// <summary>
	/// Gets or sets the parallelism options to use for this test collection. If not set, <see cref="ParallelismOptionsAliases.Default"/> is used.
	/// </summary>
	public ParallelismOptions ParallelismOptions
	{
		get => OptionalParallelismOptions ?? ParallelismOptionsAliases.Default;
		set
		{
			OptionalParallelismOptions = value;
		}
	}

	/// <summary>
	/// Gets the collection definition name, if one was provided.
	/// </summary>
	public string? Name { get; }

	/// <summary>
	/// Gets or sets the parallelism options to use for this test collection, or null if none have been specified.
	/// </summary>
	/// <remarks>
	/// Required since attribute properties cannot be nullable and the assembly options should be used when undefined.
	/// </remarks>
	public ParallelismOptions? OptionalParallelismOptions { get; set; }
}
