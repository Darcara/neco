namespace Neco.Common.Processing.Threading;

using System.Threading;

public static class ThreadFactory {
	public static void Create(String name, Action a, Boolean isBackground = true, ThreadPriority priority = ThreadPriority.Normal) {
		Thread t = new Thread(() => a()) {
			IsBackground = isBackground,
			Priority = priority,
		};
		if (!String.IsNullOrWhiteSpace(name)) t.Name = name;
		t.Start();
	}

	public static void Create<T>(String name, Action<T> a, T param, Boolean isBackground = true, ThreadPriority priority = ThreadPriority.Normal) {
		Thread t = new Thread(() => a(param)) {
			IsBackground = isBackground,
			Priority = priority,
		};
		if (!String.IsNullOrWhiteSpace(name)) t.Name = name;
		t.Start();
	}
	
	public static void Create<T1, T2>(String name, Action<T1, T2> a, T1 param1, T2 param2, Boolean isBackground = true, ThreadPriority priority = ThreadPriority.Normal) {
		Thread t = new Thread(() => a(param1, param2)) {
			IsBackground = isBackground,
			Priority = priority,
		};
		if (!String.IsNullOrWhiteSpace(name)) t.Name = name;
		t.Start();
	}
	
	public static void Create<T1, T2, T3>(String name, Action<T1, T2, T3> a, T1 param1, T2 param2, T3 param3, Boolean isBackground = true, ThreadPriority priority = ThreadPriority.Normal) {
		Thread t = new Thread(() => a(param1, param2, param3)) {
			IsBackground = isBackground,
			Priority = priority,
		};
		if (!String.IsNullOrWhiteSpace(name)) t.Name = name;
		t.Start();
	}
}