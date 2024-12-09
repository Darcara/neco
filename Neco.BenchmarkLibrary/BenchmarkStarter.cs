namespace Neco.BenchmarkLibrary;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Neco.BenchmarkLibrary.Config;
using Neco.Common.Extensions;
using Neco.Common.Helper;

// BenchmarkRunner/switcher and configuration through command line, attributes and configs are kinda inconsistent
public static class BenchmarkStarter {
	/// <summary>
	/// 
	/// </summary>
	/// <param name="benchmarkType">Type that contains the <see cref="BenchmarkDotNet.Attributes.BenchmarkAttribute"/> and <see cref="BenchmarkCategoryAttribute"/></param>
	/// <param name="config">The configuration to use. Usually <see cref="NetConfig"/></param>
	/// <param name="resultsSuffix">Suffix of the results folder</param>
	/// <returns>The benchmark-<see cref="Summary"/></returns>
	public static Summary Run(Type benchmarkType, IConfig config, String? resultsSuffix = null) {
		Summary summary = BenchmarkRunner.Run(benchmarkType, config);

		if (!String.IsNullOrWhiteSpace(resultsSuffix)) {
			String destDirName = Path.ChangeExtension(summary.ResultsDirectoryPath, resultsSuffix);

			if (Directory.Exists(destDirName))
				Directory.Delete(destDirName, true);
			Directory.Move(summary.ResultsDirectoryPath, destDirName);

			String logSource = Path.Combine(config.ArtifactsPath, $"{summary.Title}.log");
			// if (File.Exists(logSource))
			File.Move(logSource, Path.Combine(destDirName, $"{summary.Title}.log"));
		}

		return summary;
	}

	public static Summary Run<TBench, TConfig>(params String[] category) where TConfig : IConfig, new() {
		TConfig config = new();
		if (category.Length > 0)
			config.AddFilter(new AnyCategoriesFilter(category));

		return Run(typeof(TBench), config, String.Join("+", category));
	}

	public static Summary Run<T>() {
		return Run<T, NetConfig>();
	}

	public static Summary[] Run<T>(Assembly assembly, Boolean noOverwrite = false) where T : IConfig, new() {
		return BenchmarkRunner.Run(assembly, new T(), noOverwrite ? ["--noOverwrite"] : null);
	}

	public static Summary[] Run(Assembly assembly, Boolean noOverwrite = false) {
		return BenchmarkRunner.Run(assembly, new NetConfig(), noOverwrite ? ["--noOverwrite"] : null);
	}

	public static void QuickBench(Assembly assembly) {
		foreach (Type benchmarkType in assembly
			         .GetTypes()
			         .Where(type => !type.IsAbstract && !type.IsGenericType)
			         .Where(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any()))
			         .OrderBy(t => t.Namespace)
			         .ThenBy(t => t.Name)) {
			QuickBench(benchmarkType);
		}
	}

	public static void QuickBench<T>() where T : new() => QuickBench(typeof(T));

	public static void QuickBench(Type type) {
		if (type.IsAbstract || type.IsGenericType) return;

		// NonPublic is more permissive than BenchmarkDotNet
		IEnumerable<MethodInfo> benchmarkMethods = type
			.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any())
			.ToArray();
		IEnumerable<MethodInfo> setupMethods = type
			.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(method => method.GetCustomAttributes(true).OfType<GlobalSetupAttribute>().Any())
			.ToArray();
		IEnumerable<MethodInfo> cleanupMethods = type
			.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(method => method.GetCustomAttributes(true).OfType<GlobalCleanupAttribute>().Any())
			.ToArray();

		(String Name, Object?[] Values)[] parameters = type
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(field => field.GetCustomAttributes(true).OfType<ParamsAttribute>().Any())
			.Select(field => (field.Name, field.GetCustomAttributes(true).OfType<ParamsAttribute>().Single().Values))
			.Concat(type
				.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(prop => prop.GetCustomAttributes(true).OfType<ParamsAttribute>().Any())
				.Select(prop => (prop.Name, prop.GetCustomAttributes(true).OfType<ParamsAttribute>().Single().Values))
			)
			.ToArray();

		Int32[] parameterIndices = new Int32[parameters.Length];

		do {
			foreach (MethodInfo benchmarkMethod in benchmarkMethods) {
				Object? benchmark = Activator.CreateInstance(type);
				if (benchmark == null) return;

				List<String> currentParamValues = new(parameters.Length);
				for (Int32 index = 0; index < parameters.Length; index++) {
					(String name, Object?[] values) = parameters[index];
					Object? actualValue = values[parameterIndices[index]];
					ReflectionHelper.SetFieldOrPropertyValue(benchmark, name, true, () => actualValue);
					currentParamValues.Add($"{name}={actualValue}");
				}

				setupMethods.ForEach(m => m.Invoke(benchmark, null));
				String param = currentParamValues.Count == 0 ? String.Empty : $"({String.Join(", ", currentParamValues)})";
				PerformanceHelper.GetPerformanceRough($"{type.GetName()}.{benchmarkMethod.Name}{param}", b => benchmarkMethod.Invoke(b, null), benchmark);
				cleanupMethods.ForEach(m => m.Invoke(benchmark, null));
			}

			for (Int32 index = parameters.Length - 1; index >= 0; index--) {
				(_, Object?[] values) = parameters[index];
				parameterIndices[index]++;

				if (parameterIndices[index] >= values.Length) {
					parameterIndices[index] = 0;
				} else {
					break;
				}
			}
		} while (parameterIndices.Any(idx => idx != 0));
	}
}