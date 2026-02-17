namespace Neco.AspNet;

using System;

public static class NotModifiedResult {
	public const Int32 HeaderNotPresent = 0;
	public const Int32 NotModified = 1;
	public const Int32 Modified = 2;
}