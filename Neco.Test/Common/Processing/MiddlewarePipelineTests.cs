namespace Neco.Test.Common.Processing;

using System;
using System.Threading.Tasks;
using Neco.Common.Processing;
using NUnit.Framework;

[TestFixture]
public class MiddlewarePipelineTests {
	[Test]
	public async Task PipelineConstructsCorrectly() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.Build();
		await pipeline.CallAsync("CALL");
	}

	[Test]
	public async Task PipelineConstructsCorrectlyOnFirstCall() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		await pipeline.CallAsync("CALL");
	}

	[Test]
	public Task PipelineCanOnlyBeBuiltOnce() {
		MiddlewarePipeline<String> pipeline = new();
		pipeline.Build();
		Assert.Throws<InvalidOperationException>(() => pipeline.Build());
		return Task.CompletedTask;
	}

	[Test]
	public async Task PipelineHasProperStackTrace() {
		await Console.Out.WriteLineAsync("**********************");
		await Console.Out.WriteLineAsync("FUNC -> CHAIN -> CLASS");
		MiddlewarePipeline<String> pipeline = new(_ => throw new InvalidOperationException());
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.Build();
		InvalidOperationException? ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("CALL"));
		await Console.Out.WriteLineAsync(ex?.ToString());

		await Console.Out.WriteLineAsync();
		await Console.Out.WriteLineAsync("********************");
		await Console.Out.WriteLineAsync("FUNC -> FUNC -> FUNC");
		pipeline = new(_ => throw new InvalidOperationException());
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.Build();
		ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("CALL"));
		await Console.Out.WriteLineAsync(ex?.ToString());

		await Console.Out.WriteLineAsync();
		await Console.Out.WriteLineAsync("********************");
		await Console.Out.WriteLineAsync("CHAIN -> CHAIN -> CHAIN");
		pipeline = new(_ => throw new InvalidOperationException());
		MiddlewareDelegate<String> Chainer(MiddlewareDelegate<String> next) => s => next($"{s}->[Chain]");
		pipeline.AppendMiddleware(Chainer);
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.Build();
		ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("CALL"));
		await Console.Out.WriteLineAsync(ex?.ToString());

		await Console.Out.WriteLineAsync();
		await Console.Out.WriteLineAsync("********************");
		await Console.Out.WriteLineAsync("CLASS -> CLASS -> CLASS");
		pipeline = new(_ => throw new InvalidOperationException());
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.Build();
		ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("CALL"));
		await Console.Out.WriteLineAsync(ex?.ToString());
	}

	private class ClassMiddleware : IMiddleware<String> {
		#region Implementation of IMiddleware<string>

		/// <inheritdoc />
		public Task Handle(MiddlewareDelegate<String> next, String context) => next($"{context}->[Class]");

		#endregion
	}
}