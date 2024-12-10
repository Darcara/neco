namespace Neco.Common.Data.Web;

using System;
using System.Net.Http;

/// <summary>
/// This is a marker used to check if the underlying handler should be disposed.
/// HttpClients share a reference to an instance of this class, and when it goes out of scope the inner handler is eligible to be disposed.
/// </summary>
internal sealed class LifetimeTrackingHttpMessageHandlerDecorator(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler) {
	protected override void Dispose(Boolean disposing) {
		// The lifetime of this is tracked separately by ActiveHandlerTracker
	}
}