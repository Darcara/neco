namespace Neco.Test.Mocks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

public class AlwaysLease : RateLimitLease {
	public static readonly AlwaysLease Instance = new(); 
	#region Overrides of RateLimitLease

	/// <inheritdoc />
	public override Boolean TryGetMetadata(String metadataName, out Object? metadata) {
		metadata = null;
		return false;
	}

	/// <inheritdoc />
	public override Boolean IsAcquired => true;

	/// <inheritdoc />
	public override IEnumerable<String> MetadataNames => Enumerable.Empty<String>();

	#endregion
}

public class RateLimiterMock : RateLimiter {
	internal Int32 DisposeCount = 0;
	
	#region Overrides of RateLimiter

	/// <inheritdoc />
	protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken) => ValueTask.FromResult<RateLimitLease>(AlwaysLease.Instance);

	/// <inheritdoc />
	protected override RateLimitLease AttemptAcquireCore(Int32 permitCount) => AlwaysLease.Instance;

	/// <inheritdoc />
	public override RateLimiterStatistics? GetStatistics() => null;

	/// <inheritdoc />
	public override TimeSpan? IdleDuration => null;

	#endregion

	#region Overrides of RateLimiter

	/// <inheritdoc />
	protected override void Dispose(Boolean disposing) {
		base.Dispose(disposing);
		++DisposeCount;
	}

	#endregion
}