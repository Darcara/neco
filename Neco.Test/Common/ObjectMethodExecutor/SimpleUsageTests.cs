namespace Neco.Test.Common.ObjectMethodExecutor;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Neco.Common.ObjectMethodExecutor;
using NUnit.Framework;

[TestFixture]
public class SimpleUsageTests {
	private static Int64 _testNumber;

	[SetUp]
	public void Setup() {
		Interlocked.Exchange(ref _testNumber, 0);
	}

	[Test]
	public void CanCallSyncVoidMethod() {
		MethodInfo methodInfo = GetType().GetMethod(nameof(SyncVoidMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(methodInfo, GetType().GetTypeInfo());
		
		Assert.That(ome.MethodInfo, Is.EqualTo(methodInfo));
		Assert.That(ome.MethodParameters, Is.EqualTo(methodInfo.GetParameters()));
		Assert.That(ome.TargetTypeInfo, Is.EqualTo(GetType().GetTypeInfo()));
		Assert.That(ome.AsyncResultType, Is.Null);
		Assert.That(ome.IsMethodAsync, Is.False);
		Object? returnValue = ome.Execute(this, null);
		Assert.That(returnValue, Is.Null);
		Assert.That(_testNumber, Is.EqualTo(1));
	}

	[Test]
	[Ignore("Static methods not supported yet")]
	public void CanCallSyncVoidStaticMethod() {
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(GetType().GetMethod(nameof(SyncVoidStaticMethod), BindingFlags.NonPublic | BindingFlags.Static)!, GetType().GetTypeInfo());
		Assert.That(ome.IsMethodAsync, Is.False);
		Object? returnValue = ome.Execute(null, null);
		Assert.That(returnValue, Is.Null);
		Assert.That(_testNumber, Is.EqualTo(1));
	}

	[Test]
	public void CanCallSyncMethodWithParams() {
		MethodInfo methodInfo = GetType().GetMethod(nameof(SyncMethodWithParam), BindingFlags.NonPublic | BindingFlags.Instance)!;
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(methodInfo, GetType().GetTypeInfo(), new Object[] { 4 });
		Assert.That(ome.GetDefaultValueForParameter(0), Is.EqualTo(4));
		Assert.That(ome.IsMethodAsync, Is.False);
		Object? returnValue = ome.Execute(this, new[] { ome.GetDefaultValueForParameter(0)! });
		Assert.That(returnValue, Is.EqualTo(4));
		Assert.That(_testNumber, Is.EqualTo(4));
	}

	[Test]
	public async Task CanCallAsyncMethodWithParams() {
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(GetType().GetMethod(nameof(AsyncMethodWithParam), BindingFlags.NonPublic | BindingFlags.Instance)!, GetType().GetTypeInfo());
		Assert.That(ome.AsyncResultType, Is.EqualTo(typeof(Int64)));
		Assert.That(ome.IsMethodAsync, Is.True);
		Object returnValue = await ome.ExecuteAsync(this, new Object[] { 4 });
		Assert.That(returnValue, Is.EqualTo(4));
		Assert.That(_testNumber, Is.EqualTo(4));
	}
	
	[Test]
	public async Task CanCallAsyncTaskMethodWithParams() {
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(GetType().GetMethod(nameof(AsyncTaskMethodWithParam), BindingFlags.NonPublic | BindingFlags.Instance)!, GetType().GetTypeInfo());
		Assert.That(ome.IsMethodAsync, Is.True);
		Object returnValue = await ome.ExecuteAsync(this, new Object[] { 4 });
		Assert.That(returnValue, Is.Null);
		Assert.That(_testNumber, Is.EqualTo(4));
	}
	
	[Test]
	public async Task CanCallAsyncVoidMethodWithParams() {
		ObjectMethodExecutor ome = ObjectMethodExecutor.Create(GetType().GetMethod(nameof(AsyncVoidMethodWithParam), BindingFlags.NonPublic | BindingFlags.Instance)!, GetType().GetTypeInfo());
		Assert.That(ome.IsMethodAsync, Is.False);
		Object? returnValue = ome.Execute(this, new Object[] { 4 });
		Assert.That(returnValue, Is.Null);
		Assert.That(_testNumber, Is.EqualTo(0));
		await Task.Delay(100);
		Assert.That(_testNumber, Is.EqualTo(4));
	}

	private void SyncVoidMethod() {
		Interlocked.Increment(ref _testNumber);
	}

	private static void SyncVoidStaticMethod() {
		Interlocked.Increment(ref _testNumber);
	}

	private Int64 SyncMethodWithParam(Int32 numToAdd) {
		return Interlocked.Add(ref _testNumber, numToAdd);
	}

	private async Task<Int64> AsyncMethodWithParam(Int32 numToAdd) {
		await Task.Delay(50);
		return Interlocked.Add(ref _testNumber, numToAdd);
	}

	private async Task AsyncTaskMethodWithParam(Int32 numToAdd) {
		await Task.Delay(50);
		Interlocked.Add(ref _testNumber, numToAdd);
	}

	private async void AsyncVoidMethodWithParam(Int32 numToAdd) {
		await Task.Delay(50);
		Interlocked.Add(ref _testNumber, numToAdd);
	}
}