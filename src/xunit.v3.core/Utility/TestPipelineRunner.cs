using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xunit.v3.Utility;

internal static class TestPipelineTaskRunner
{
	public static Func<Func<ValueTask<RunSummary>>, ValueTask<RunSummary>> Create(CancellationToken cancellationToken)
	{
		if (SynchronizationContext.Current is not null)
		{
			var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			return code => new(Task.Factory.StartNew(() => code().AsTask(), cancellationToken,
				TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler, scheduler).Unwrap());
		}

		return code => new(Task.Run(() => code().AsTask(), cancellationToken));
	}
}
