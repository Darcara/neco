namespace Neco.Benchmark;

using System.Collections.Generic;
using System.Threading.Channels;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
public class PriorityQueueBenchmark {
	[Params("WriteAll-ReadAll", "GraphSearch", "TaskQueue")]
	public String Pattern;

	public Int32 N = 1000;

	private Random _random;
	private List<Item> _tempList;
	private List<(Item, Double)> _tempList2;
	private Queue<Item> _simpleQueue;
	private PriorityQueue<Item, Double> _priorityQueue;
	private Channel<Item> _priorityChannel;

	[GlobalSetup]
	public void Setup() {
		_random = new Random(12345);
		_tempList = new List<Item>(N);
		_tempList2 = new List<(Item, Double)>(N);

		_simpleQueue = new Queue<Item>(N);
		_priorityQueue = new PriorityQueue<Item, Double>(N);
		UnboundedPrioritizedChannelOptions<Item> channelOptions = new() { Comparer = Comparer<Item>.Default, SingleReader = true, SingleWriter = true };
		_priorityChannel = Channel.CreateUnboundedPrioritized(channelOptions);
	}

	private Double ExcutePattern<TQueue, TData>(TQueue queue, TData data, Int32 n, Random random, Action<TQueue, TData, Item> enqueue, Action<TQueue, TData> afterEnqueue, Func<TQueue, TData, Int32, Double> dequeue, Action<TQueue, TData> cleanup) {
		return Pattern switch {
			"WriteAll-ReadAll" => WriteAllReadAllPattern(queue, data, n, random, enqueue, afterEnqueue, dequeue, cleanup),
			"GraphSearch" => GraphSearchPattern(queue, data, n, random, enqueue, afterEnqueue, dequeue, cleanup),
			"TaskQueue" => TaskQueuePattern(queue, data, n, random, enqueue, afterEnqueue, dequeue, cleanup),
			_ => throw new InvalidOperationException($"Invalid patter: {Pattern}"),
		};
	}

	private static Double WriteAllReadAllPattern<TQueue, TData>(TQueue queue, TData data, Int32 n, Random random, Action<TQueue, TData, Item> enqueue, Action<TQueue, TData> afterEnqueue, Func<TQueue, TData, Int32, Double> dequeue, Action<TQueue, TData> cleanup) {
		for (int i = 0; i < n; i++) {
			Item element = new(random.NextDouble(), 42);
			enqueue(queue, data, element);
		}

		afterEnqueue(queue, data);

		Double total = 0;
		for (int i = 0; i < n; i++) {
			total += dequeue(queue, data, i);
		}

		cleanup(queue, data);

		return total;
	}

	private static Double GraphSearchPattern<TQueue, TData>(TQueue queue, TData data, Int32 n, Random random, Action<TQueue, TData, Item> enqueue, Action<TQueue, TData> afterEnqueue, Func<TQueue, TData, Int32, Double> dequeue, Action<TQueue, TData> cleanup) {
		Double total = 0;

		Item initialNode = new(random.NextDouble(), 42);
		enqueue(queue, data, initialNode);
		afterEnqueue(queue, data);

		for (int i = 0; i < n / 10; i++) {
			total += dequeue(queue, data, i);

			for (int j = 0; j < 10; j++) {
				Item element = new(random.NextDouble(), 42);
				enqueue(queue, data, element);
			}

			afterEnqueue(queue, data);
		}

		cleanup(queue, data);
		return total;
	}

	private static Double TaskQueuePattern<TQueue, TData>(TQueue queue, TData data, Int32 n, Random random, Action<TQueue, TData, Item> enqueue, Action<TQueue, TData> afterEnqueue, Func<TQueue, TData, Int32, Double> dequeue, Action<TQueue, TData> cleanup) {
		Double total = 0;

		Item initialNode = new(random.NextDouble(), 42);
		enqueue(queue, data, initialNode);
		afterEnqueue(queue, data);

		for (int i = 0; i < n / 10; i++) {
			total += dequeue(queue, data, i);

			for (int j = 0; j < 10; j++) {
				Item element = new(random.NextDouble(), 42);
				enqueue(queue, data, element);
				afterEnqueue(queue, data);
			}
		}

		Int32 stillEnqueued = 1 + n - n/10;
		for (int i = 0; i < stillEnqueued; i++) {
			total += dequeue(queue, data, i);
		}

		cleanup(queue, data);
		return total;
	}

	private static readonly Object _none = new();

	[Benchmark]
	public Double QueueOverhead() {
		return ExcutePattern(_simpleQueue, _none, N, _random,
			static (q, _, e) => q.Enqueue(e),
			static (_, _) => { },
			static (q, _, _) => q.Dequeue().Priority,
			static (q, _) => { q.Clear(); });
	}

	[Benchmark]
	public Double ListSort() {
		return ExcutePattern(_tempList, _none, N, _random,
			static (q, _, e) => q.Add(e),
			static (q, _) => {
				if (q.Count > 1) q.Sort();
			},
			static (q, _, _) => {
				Int32 lastElementIndex = q.Count - 1;
				Double priority = q[lastElementIndex].Priority;
				q.RemoveAt(lastElementIndex);
				return priority;
			},
			static (q, _) => { q.Clear(); });
	}

	[Benchmark(Baseline = true)]
	public Double PriorityQueueSingle() {
		return ExcutePattern(_priorityQueue, _none, N, _random,
			static (q, _, e) => q.Enqueue(e, e.Priority),
			static (_, _) => { },
			static (q, _, _) => q.Dequeue().Priority,
			static (q, _) => { q.Clear(); });
	}

	[Benchmark]
	public Double PriorityQueueRange() {
		return ExcutePattern(_priorityQueue, _tempList2, N, _random,
			static (_, list, e) => list.Add((e, e.Priority)),
			static (q, list) => {
				q.EnqueueRange(list);
				list.Clear();
			},
			static (q, _, _) => q.Dequeue().Priority,
			static (q, l) => {
				q.Clear();
				l.Clear();
			});
	}

	// This is basically a lock around PriorityQueue
	[Benchmark]
	public Double ChannelPrioritized() {
		return ExcutePattern(_priorityChannel, _none, N, _random,
			static (q, _, e) => q.Writer.TryWrite(e),
			static (_, _) => { },
			static (q, _, _) => q.Reader.TryRead(out Item item) ? item.Priority : 0,
			static (q, _) => {
				while (q.Reader.TryRead(out Item _)) {
					// discard					
				}
			});
	}

	private sealed class Item : IComparable<Item>, IComparable, IEquatable<Item> {
		public readonly Double Priority;
		public readonly Int64 UselessPayload;

		public Item(Double priority, Int64 uselessPayload) {
			Priority = priority;
			UselessPayload = uselessPayload;
		}

		#region Relational members

		/// <inheritdoc />
		public int CompareTo(Item other) {
			if (other is null) return 1;
			if (ReferenceEquals(this, other)) return 0;
			return Priority.CompareTo(other.Priority);
		}

		/// <inheritdoc />
		public int CompareTo(Object obj) {
			if (obj is null) return 1;
			if (ReferenceEquals(this, obj)) return 0;
			return obj is Item other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Item)}");
		}

		public static bool operator <(Item left, Item right) => Comparer<Item>.Default.Compare(left, right) < 0;

		public static bool operator >(Item left, Item right) => Comparer<Item>.Default.Compare(left, right) > 0;

		public static bool operator <=(Item left, Item right) => Comparer<Item>.Default.Compare(left, right) <= 0;

		public static bool operator >=(Item left, Item right) => Comparer<Item>.Default.Compare(left, right) >= 0;

		#endregion

		#region Equality members

		/// <inheritdoc />
		public bool Equals(Item other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Priority.Equals(other.Priority) && UselessPayload == other.UselessPayload;
		}

		/// <inheritdoc />
		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Item other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(Priority, UselessPayload);

		public static bool operator ==(Item left, Item right) => Equals(left, right);

		public static bool operator !=(Item left, Item right) => !Equals(left, right);

		#endregion
	}
}