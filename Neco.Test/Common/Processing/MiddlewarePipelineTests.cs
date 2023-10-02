namespace Neco.Test.Common.Processing;

using System;
using System.Diagnostics.CodeAnalysis;
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
	public async Task PipelineCallsFactoryMethodsOnlyOnce() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		Int32 firstFactory = 0;
		Int32 secondFactory = 0;
		Int32 thirdFactory = 0;
		pipeline.AppendMiddleware(next => {
			++firstFactory;
			return s => next($"{s}->[Chain{firstFactory}]");
		});
		pipeline.AppendMiddleware(next => {
			++secondFactory;
			return s => next($"{s}->[Chain{secondFactory}]");
		});
		pipeline.AppendMiddleware(next => {
			++thirdFactory;
			return s => next($"{s}->[Chain{secondFactory}]");
		});
		pipeline.Build();
		await pipeline.CallAsync("CALL");

		Assert.That(firstFactory, Is.EqualTo(1));
		Assert.That(secondFactory, Is.EqualTo(1));
		Assert.That(thirdFactory, Is.EqualTo(1));

		await pipeline.CallAsync("CALL");
		Assert.That(firstFactory, Is.EqualTo(1));
		Assert.That(secondFactory, Is.EqualTo(1));
		Assert.That(thirdFactory, Is.EqualTo(1));
	}

	[Test]
	[SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
	public void PipelineConstructsCorrectlyOnFirstCall() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		pipeline.AppendMiddlewareSuboptimal((next, s) => next($"{s}->[Func]"));
		pipeline.AppendMiddleware(next => s => next($"{s}->[Chain]"));
		pipeline.AppendMiddlewareSuboptimal(new ClassMiddleware());
		pipeline.Call("CALL");
	}
	
	[Test]
	public async Task PipelineConstructsCorrectlyOnFirstAsyncCall() {
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
		var middleware = new ClassMiddleware();
		pipeline.AppendMiddleware(next => s => middleware.Handle(next, s));
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

		await Console.Out.WriteLineAsync();
		await Console.Out.WriteLineAsync("********************");
		await Console.Out.WriteLineAsync("ExtFn -> ExtFn -> ExtFn");
		pipeline = new(_ => throw new InvalidOperationException());
		pipeline.AppendExtensionMiddleware();
		pipeline.AppendOneLevelOfIndirection();
		pipeline.ProperlyNamedExtensionFunction();
		pipeline.Build();
		ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("CALL"));
		await Console.Out.WriteLineAsync(ex?.ToString());
	}

	[Test]
	public async Task PipelineMapsWhenCorrectly() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		pipeline.BeforeMap();
		pipeline.MapWhen(s => s.Length >= 16, branch => {
			branch.AppendMiddleware(next => s => next($"{s}->[Branch1]"));
			branch.AppendMiddleware(next => s => next($"{s}->[Branch2]"));
			branch.AppendMiddleware(next => s => {
				Console.Out.WriteLine($"{s} -> Branch-EOP");
				return next(s);
			});
		});
		pipeline.AfterMap();
		pipeline.AfterMapThrow();
		pipeline.Build();
		await pipeline.CallAsync("CALL");
		var ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CallAsync("!"));
		await Console.Out.WriteLineAsync(ex?.ToString());
	}
	
	[Test]
	public async Task PipelineUsesWhenCorrectly() {
		MiddlewarePipeline<String> pipeline = new(s => Console.Out.WriteLineAsync($"{s} -> EOP"));
		Int32 branchedPipelineFinished = 0;
		pipeline.BeforeUse();
		pipeline.UseWhen(s => s.Length >= 16, branch => {
			branch.AppendMiddleware(next => s => next($"{s}->[Branch1]"));
			branch.AppendMiddleware(next => s => next($"{s}->[Branch2]"));
			branch.AppendMiddleware(next => s => {
				++branchedPipelineFinished;
				Console.Out.WriteLine($"{s} -> Branch-EOP");
				return next(s);
			});
		});
		pipeline.AppendMiddleware(next => s => next($"{s}->[AfterUse]"));
		Int32 originalPipelineAfterCalled = 0;
		pipeline.AppendMiddleware(next => ctx => {
			++originalPipelineAfterCalled;
			return next(ctx);
		});
		pipeline.Build();
		
		await pipeline.CallAsync("CALL");
		Assert.That(branchedPipelineFinished, Is.EqualTo(1));
		Assert.That(originalPipelineAfterCalled, Is.EqualTo(1));
		
		await pipeline.CallAsync("!");
		Assert.That(branchedPipelineFinished, Is.EqualTo(1));
		Assert.That(originalPipelineAfterCalled, Is.EqualTo(2));
	}

	private class ClassMiddleware : IMiddleware<String> {
		#region Implementation of IMiddleware<string>

		/// <inheritdoc />
		public Task Handle(MiddlewareDelegate<String> next, String context) => next($"{context}->[Class]");

		#endregion
	}
}

internal static class MiddlewareExtension {
	public static void AppendOneLevelOfIndirection(this MiddlewarePipeline<String> pipeline) => AppendExtensionMiddleware(pipeline);

	public static void AppendExtensionMiddleware(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(next => s => next($"{s}->[ExtFn]"));
	}

	public static void ProperlyNamedExtensionFunction(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(next => s => next($"{s}->[ExtFn]"));
	}

	public static void BeforeUse(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(next => s => next($"{s}->[BeforeUse]"));
	}
	
	public static void BeforeMap(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(next => s => next($"{s}->[BeforeMap]"));
	}
	public static void AfterMap(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(next => s => next($"{s}->[AfterMap]"));
	}
	public static void AfterMapThrow(this MiddlewarePipeline<String> pipeline) {
		pipeline.AppendMiddleware(_ =>  s => throw new InvalidOperationException($"AFTER MAP: {s}"));
	}
}