namespace Neco.Test.Common.Processing.Threading;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Neco.Common.Processing.Threading;

[TestFixture]
public class SyncOrAsyncActionTest {
	private readonly MethodInfo _someMethodInfo = typeof(SyncOrAsyncActionTest).GetMethod(nameof(SomeMethod), BindingFlags.Instance| BindingFlags.NonPublic)!;
	private readonly MethodInfo _someStaticAsyncMethodInfo = typeof(SyncOrAsyncActionTest).GetMethod(nameof(SomeStaticAsyncMethod), BindingFlags.Static|BindingFlags.NonPublic)!;
	private static Int32 _lastCallParameter = -1;
	private void SomeMethod(Int32 i) {
		_lastCallParameter = i;
	}

	private static Task SomeStaticAsyncMethod(Int32 i) {
		_lastCallParameter = i;
		return Task.CompletedTask;
	}
	
	[Test]
	public void CorrectEquality() {
		SyncOrAsyncAction original = SyncOrAsyncAction.FromMethod(_someMethodInfo, this, 42);
		SyncOrAsyncAction equal = SyncOrAsyncAction.FromMethod(_someMethodInfo, this, 42);
		SyncOrAsyncAction differant = SyncOrAsyncAction.FromMethod(_someMethodInfo, this, 5);
		
		Assert.Multiple(() => {
			Assert.That(original, Is.EqualTo(equal));
			Assert.That(original.Equals(equal));
			Assert.That(equal == original, Is.True);
			Assert.That(equal != original, Is.False);
			Assert.That(Equals(original, equal), Is.True);
			Assert.That(Equals(equal, original), Is.True);
			
			Assert.That(original, Is.Not.EqualTo(differant));
			Assert.That(original.Equals(differant), Is.False);
			Assert.That(differant == original, Is.False);
			Assert.That(differant != original, Is.True);
			Assert.That(Equals(original, differant), Is.False);
			Assert.That(Equals(differant, original), Is.False);
			
			Assert.That(original.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
			Assert.That(original.GetHashCode(), Is.Not.EqualTo(differant.GetHashCode()));
		});
	}

	[Test]
	[SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
	public async Task CallsMethod() {
		Assert.That(_lastCallParameter, Is.Not.EqualTo(2));
		SyncOrAsyncAction syncWrap = SyncOrAsyncAction.FromMethod(_someMethodInfo, this, 2);
		Assert.That(syncWrap.IsAsync, Is.False);
		syncWrap.Invoke();
		Assert.That(_lastCallParameter, Is.EqualTo(2));
		
		var syncWrapAction = SyncOrAsyncAction.FromAction(() => _lastCallParameter = 3);
		Assert.That(syncWrapAction.IsAsync, Is.False);
		syncWrapAction.Invoke();
		Assert.That(_lastCallParameter, Is.EqualTo(3));
		
		syncWrapAction = SyncOrAsyncAction.FromAction(p => _lastCallParameter = p, 4);
		Assert.That(syncWrapAction.IsAsync, Is.False);
		syncWrapAction.Invoke();
		Assert.That(_lastCallParameter, Is.EqualTo(4));
		
		SyncOrAsyncAction asyncWrap = SyncOrAsyncAction.FromMethod(_someStaticAsyncMethodInfo, null, 5);
		Assert.That(asyncWrap.IsAsync, Is.True);
		await asyncWrap.InvokeAsync();
		Assert.That(_lastCallParameter, Is.EqualTo(5));
		
		SyncOrAsyncAction asyncWrapAction = SyncOrAsyncAction.FromFunc(async static () => {
			await Task.Yield();
			_lastCallParameter = 6;
		});
		Assert.That(asyncWrapAction.IsAsync, Is.True);
		await asyncWrapAction.InvokeAsync();
		Assert.That(_lastCallParameter, Is.EqualTo(6));
		
		asyncWrapAction = SyncOrAsyncAction.FromFunc(async p => {
			await Task.Yield();
			_lastCallParameter = p;
		}, 7);
		Assert.That(asyncWrapAction.IsAsync, Is.True);
		await asyncWrapAction.InvokeAsync();
		Assert.That(_lastCallParameter, Is.EqualTo(7));
	}
}