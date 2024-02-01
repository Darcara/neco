namespace Neco.Test.Common;

using System;
using System.Threading;
using Neco.Common.Data;
using Neco.Common.Extensions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class SequentialGuidGeneratorTests {
	[SetUp]
	public void BeforeTest() {
		// WarmUp JIT
		for (Int32 i = 0; i < 100; i++) SequentialGuidGenerator.CreateSequentialGuid();

		// Wait for clock to advance after current id tick
		Thread.Sleep(10);
	}

	[Test]
	public void GeneratesSequentialGuids() {
		Guid g1 = SequentialGuidGenerator.CreateSequentialGuid();
		Thread.Sleep(1);
		Guid g2 = SequentialGuidGenerator.CreateSequentialGuid();
		
		Console.WriteLine($"{g1} -- {g1.ToByteArray().ToStringHexSingleLine(0,4)}-{g1.ToByteArray().ToStringHexSingleLine(4,2)}-{g1.ToByteArray().ToStringHexSingleLine(6)}");
		Console.WriteLine($"{g2} -- {g2.ToByteArray().ToStringHexSingleLine(0,4)}-{g2.ToByteArray().ToStringHexSingleLine(4,2)}-{g2.ToByteArray().ToStringHexSingleLine(6)}");
		Assert.That(g2, Is.GreaterThan(g1));
	}
	
	[Test]
	public void CanEstimateTimeFromId() {
		DateTime now = DateTime.UtcNow;
		Guid g1 = SequentialGuidGenerator.CreateSequentialGuid();
		DateTime idTime = SequentialGuidGenerator.FromSequentialGuid(g1);
		Console.WriteLine(now.ToString("O"));
		Console.WriteLine(idTime.ToString("O"));
		Assert.That(idTime.Kind, Is.EqualTo(DateTimeKind.Utc));
		Assert.That(idTime - now, Is.GreaterThan(TimeSpan.FromMicroseconds(-20)));
		Assert.That(idTime - now, Is.InRange(TimeSpan.FromMicroseconds(-20), TimeSpan.FromMicroseconds(20)));
	}
	
	// AMD Ryzen 9 3900X 12-Core Processor
	// CreateSequentialBinary 43,056,827 ops in 5,000.001ms = clean per operation: 0.084µs or 11,888,914.112op/s with GC 0/0/0
	// CreateSequentialBinary TotalCPUTime per operation: 5,000.000ms or clean 11,888,915.753op/s for a factor of 1.000
	[Test]
	[Category("Benchmark")]
	public void RoughGenerationBenchmark() {
		PerformanceHelper.GetPerformanceRough(nameof(SequentialGuidGenerator.CreateSequentialGuid), () => SequentialGuidGenerator.CreateSequentialGuid());
	}
	
	[Test]
	public void ConvertsToMySqlUuidCorrectly() {
		Guid newGuid = Guid.NewGuid();
		Guid convertedGuid = SequentialGuidGenerator.FromMySqlUuid(SequentialGuidGenerator.ToMySqlUuid(newGuid));
		Assert.That(convertedGuid, Is.EqualTo(newGuid));

		Byte[] array = newGuid.ToByteArray();
		Byte[] convertedArray = SequentialGuidGenerator.ToMySqlUuid(SequentialGuidGenerator.FromMySqlUuid(array));
		Assert.That(convertedArray, Is.EqualTo(array));
	}
}