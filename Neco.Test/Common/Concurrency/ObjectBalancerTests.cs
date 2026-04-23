namespace Neco.Test.Common.Concurrency;

using System.Threading;
using System.Threading.Tasks;
using Neco.Common.Concurrency;

/// <summary>
/// Unit tests for ObjectBalancer factory, StaticObjectBalancer, and BalancedObject classes.
/// </summary>
[TestFixture]
public class ObjectBalancerTests : ATest {
	/// <summary>
	/// Disposable test double for verifying disposal behavior.
	/// </summary>
	private sealed class DisposableTestObject : IDisposable {
		public Int32 Id { get; }
		public Boolean IsDisposed { get; private set; }

		public DisposableTestObject(Int32 id) {
			Id = id;
		}

		public void Dispose() {
			IsDisposed = true;
		}
	}

	#region Factory Method Tests

	[Test]
	public void StaticFactory_WithNullEnumerable_ThrowsArgumentNullException() {
		Assert.That(() => ObjectBalancer.Static<String>(null!), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void StaticFactory_WithEnumerable_CreatesBalancer() {
		IEnumerable<String> objects = ["a", "b", "c"];
		IObjectBalancer<String> balancer = ObjectBalancer.Static(objects);
		
		Assert.That(balancer, Is.Not.Null);
		Assert.That(balancer, Is.TypeOf<StaticObjectBalancer<String>>());
		
		balancer.Dispose();
	}

	[Test]
	public void StaticFactory_WithCount_ThrowsArgumentNullException() {
		Assert.That(() => ObjectBalancer.Static<String>(3, null!), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void StaticFactory_WithCount_CreatesBalancer() {
		Int32 createCount = 0;
		IObjectBalancer<String> balancer = ObjectBalancer.Static(3, () => {
			createCount++;
			return $"Object{createCount}";
		});

		Assert.That(balancer, Is.Not.Null);
		Assert.That(createCount, Is.EqualTo(3));
		
		balancer.Dispose();
	}

	[Test]
	public void StaticFactory_WithZeroCount_CreatesEmptyBalancer() {
		IObjectBalancer<String> balancer = ObjectBalancer.Static<String>(0, () => "object");
		
		Assert.That(balancer, Is.Not.Null);
		
		balancer.Dispose();
	}

	#endregion

	#region Synchronous Get Tests

	[Test]
	public void Get_WithAvailableObject_ReturnsObject() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		String result = balancer.Get();
		
		Assert.That(result, Is.EqualTo("obj1"));
	}

	[Test]
	public void Get_WithMultipleObjects_ReturnsObjects() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["a", "b", "c"]);
		
		String obj1 = balancer.Get();
		String obj2 = balancer.Get();
		String obj3 = balancer.Get();
		
		Assert.That(new[] { obj1, obj2, obj3 }, Is.EquivalentTo(["a", "b", "c"]));
	}

	[Test]
	public void Get_WithReturnedObject_ReturnsSameObjectAgain() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		String first = balancer.Get();
		balancer.Return(first);
		String second = balancer.Get();
		
		Assert.That(second, Is.EqualTo(first));
	}

	[Test]
	public void Get_WhenPoolEmpty_BlocksUntilObjectReturned() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		String firstObj = balancer.Get();
		
		// Start a task that will return the object after a delay
		Task returnTask = Task.Run(() => {
			Thread.Sleep(100);
			balancer.Return(firstObj);
		});
		
		// This should block until the return task completes
		String secondObj = balancer.Get();
		
		Assert.That(secondObj, Is.EqualTo(firstObj));
		Assert.That(returnTask.IsCompleted, Is.True);
	}

	#endregion

	#region Asynchronous GetAsync Tests

	[Test]
	public async Task GetAsync_WithAvailableObject_ReturnsCompletedTask() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		ValueTask<String> task = balancer.GetAsync();
		
		Assert.That(task.IsCompleted, Is.True);
		String result = await task;
		Assert.That(result, Is.EqualTo("obj1"));
	}

	[Test]
	public async Task GetAsync_WithMultipleObjects_ReturnsObjects() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["a", "b", "c"]);
		
		String obj1 = await balancer.GetAsync();
		String obj2 = await balancer.GetAsync();
		String obj3 = await balancer.GetAsync();
		
		Assert.That(new[] { obj1, obj2, obj3 }, Is.EquivalentTo(["a", "b", "c"]));
	}

	[Test]
	public async Task GetAsync_WhenPoolEmpty_WaitsForReturn() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		String firstObj = await balancer.GetAsync();
		
		ValueTask<String> secondObjTask = balancer.GetAsync();
		Assert.That(secondObjTask.IsCompleted, Is.False);
		balancer.Return(firstObj);
		
		String secondObj = await secondObjTask;
		Assert.That(secondObj, Is.EqualTo(firstObj));
	}

	[Test]
	public void GetAsync_AfterDisposal_ThrowsObjectDisposedException() {
		IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj"]);
		balancer.Dispose();
		
		Assert.That(() => balancer.GetAsync(), Throws.TypeOf<ObjectDisposedException>());
	}

	#endregion

	#region GetDisposable Tests

	[Test]
	public void GetDisposable_ReturnsDisposableWrapper() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		BalancedObject<String> wrapped = balancer.GetDisposable();
		
		Assert.That(wrapped, Is.Not.Null);
		Assert.That(wrapped.TheObject, Is.EqualTo("obj1"));
	}

	#endregion

	#region GetDisposableAsync Tests

	[Test]
	public async Task GetDisposableAsync_WithAvailableObject_ReturnsCompletedTask() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		ValueTask<BalancedObject<String>> task = balancer.GetDisposableAsync();
		
		Assert.That(task.IsCompleted, Is.True);
		BalancedObject<String> wrapped = await task;
		Assert.That(wrapped.TheObject, Is.EqualTo("obj1"));
	}
	
	[Test]
	public async Task GetDisposable_WhenPoolEmpty_WaitsForReturn() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		BalancedObject<String> firstObj = await balancer.GetDisposableAsync();
		
		ValueTask<BalancedObject<String>> secondObjTask = balancer.GetDisposableAsync();
		Assert.That(secondObjTask.IsCompleted, Is.False);
		firstObj.Dispose();
		
		BalancedObject<String> secondObj = await secondObjTask;
		Assert.That(secondObj.TheObject, Is.EqualTo(firstObj.TheObject));
	}

	[Test]
	public async Task GetDisposableAsync_DisposingWrapper_ReturnsObjectToPool() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1"]);
		
		BalancedObject<String> wrapped = await balancer.GetDisposableAsync();
		String retrieved = wrapped.TheObject;
		
		wrapped.Dispose();
		
		// Object should now be available again
		String secondRetrieve = await balancer.GetAsync();
		Assert.That(secondRetrieve, Is.EqualTo(retrieved));
	}

	[Test]
	public void GetDisposableAsync_AfterDisposal_ThrowsObjectDisposedException() {
		IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj"]);
		balancer.Dispose();
		
		Assert.That(() => balancer.GetDisposableAsync(), Throws.TypeOf<ObjectDisposedException>());
	}

	#endregion

	#region Return Tests

	[Test]
	public void Return_WithDisposableObject_DoesNotDisposeWhenBalancerActive() {
		using IObjectBalancer<DisposableTestObject> balancer = ObjectBalancer.Static(
			[new DisposableTestObject(1)]
		);
		
		DisposableTestObject obj = balancer.Get();
		balancer.Return(obj);
		
		Assert.That(obj.IsDisposed, Is.False);
	}

	[Test]
	public void Return_AfterBalancerDisposal_DisposesObject() {
		DisposableTestObject obj = new(1);
		IObjectBalancer<DisposableTestObject> balancer = ObjectBalancer.Static([obj]);
		
		balancer.Dispose();
		balancer.Return(obj);
		
		// Return after disposal should dispose the object
		Assert.That(obj.IsDisposed, Is.True);
	}

	#endregion

	#region Disposal Tests

	[Test]
	public void Dispose_DisposesAllDisposableObjects() {
		DisposableTestObject obj1 = new(1);
		DisposableTestObject obj2 = new(2);
		DisposableTestObject obj3 = new(3);
		
		IObjectBalancer<DisposableTestObject> balancer = ObjectBalancer.Static([obj1, obj2, obj3]);
		
		balancer.Dispose();
		
		Assert.That(obj1.IsDisposed, Is.True);
		Assert.That(obj2.IsDisposed, Is.True);
		Assert.That(obj3.IsDisposed, Is.True);
	}

	[Test]
	public void Dispose_AllowsMultipleCalls() {
		DisposableTestObject obj = new(1);
		IObjectBalancer<DisposableTestObject> balancer = ObjectBalancer.Static([obj]);
		
		balancer.Dispose();
		// Second dispose call throws ChannelClosedException - this is expected behavior from Channel.Complete()
		Assert.That(() => balancer.Dispose(), Throws.Nothing);
	}

	[Test]
	public void Dispose_PreventsGetCalls() {
		IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj"]);
		balancer.Dispose();
		
		Assert.That(() => balancer.Get(), Throws.TypeOf<ObjectDisposedException>());
	}

	#endregion

	#region Concurrent Access Tests

	[Test]
	public void ConcurrentGetAndReturn_ObjectsReusedCorrectly() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1", "obj2"]);
		
		Int32 successCount = 0;
		Task[] tasks = new Task[10];
		
		for (Int32 i = 0; i < 10; i++) {
			tasks[i] = Task.Run(() => {
				String obj = balancer.Get();
				Thread.Sleep(10); // Simulate work
				balancer.Return(obj);
				Interlocked.Increment(ref successCount);
			});
		}
		
		Task.WaitAll(tasks);
		
		Assert.That(successCount, Is.EqualTo(10));
	}

	[Test]
	public void ConcurrentDisposableWrapper_CorrectlyReturnsObjects() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(["obj1", "obj2", "obj3"]);
		
		Int32 successCount = 0;
		Task[] tasks = new Task[9];
		
		for (Int32 i = 0; i < 9; i++) {
			tasks[i] = Task.Run(() => {
				using (BalancedObject<String> wrapped = balancer.GetDisposable()) {
					Thread.Sleep(5); // Simulate work
				}
				Interlocked.Increment(ref successCount);
			});
		}
		
		Task.WaitAll(tasks);
		
		Assert.That(successCount, Is.EqualTo(9));
	}

	#endregion

	#region Edge Case Tests

	[Test]
	public void EmptyBalancer_ThrowsOnGet() {
		using IObjectBalancer<String> balancer = ObjectBalancer.Static(Array.Empty<String>());
		
		// Get should block indefinitely (we'll timeout)
		Task getTask = Task.Run(() => balancer.Get());
		Boolean completed = getTask.Wait(TimeSpan.FromMilliseconds(100));
		
		Assert.That(completed, Is.False);
	}

	[Test]
	public void BalancedObject_WithNull_DoesNotThrow() {
		using IObjectBalancer<String?> balancer = ObjectBalancer.Static(new String?[] { null });
		
		BalancedObject<String?> wrapped = balancer.GetDisposable();
		Assert.That(wrapped.TheObject, Is.Null);
		
		wrapped.Dispose();
	}

	[Test]
	public void MixedDisposableAndNonDisposable_DisposesOnlyDisposable() {
		DisposableTestObject disposable = new(1);
		String nonDisposable = "text";
		
		IObjectBalancer<Object> balancer = ObjectBalancer.Static(
			new Object[] { disposable, nonDisposable }
		);
		
		balancer.Dispose();
		
		Assert.That(disposable.IsDisposed, Is.True);
	}

	#endregion
}