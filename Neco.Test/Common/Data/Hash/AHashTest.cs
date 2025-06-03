namespace Neco.Test.Common.Data.Hash;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;
using NUnit.Framework;

public abstract class AHashTest {
	protected static void TestAllAtOnce(String expectedResult, Byte[] input, Func<IIncrementalHash> hashCreator) {
		using (IIncrementalHash hasher = hashCreator()) {
			hasher.Reset();
			hasher.AppendData(input);
			Byte[] hash = hasher.GetCurrentHash();
			String hashReadable = hash.ToStringHexSingleLine();

			Assert.That(hashReadable, Is.EqualTo(expectedResult), $"Default hashing for {input.ToStringHexSingleLine()} - ({input.Length} bytes) {Encoding.ASCII.GetString(input)}");
		}

		using (IIncrementalHash hasher = hashCreator()) {
			Byte[] paddedInput = new Byte[input.Length + 1];
			Array.Copy(input, 0, paddedInput, 1, input.Length);
			paddedInput[0] = 0xA0;

			hasher.Reset();
			hasher.AppendData(paddedInput, 1, input.Length);
			Byte[] hash = hasher.GetCurrentHash();
			String hashReadable = hash.ToStringHexSingleLine();

			Assert.That(hashReadable, Is.EqualTo(expectedResult), $"Input padded front 1 byte for {input.ToStringHexSingleLine()} - ({input.Length} bytes) {Encoding.ASCII.GetString(input)}");
		}

		using (IIncrementalHash hasher = hashCreator()) {
			Byte[] paddedInput = new Byte[input.Length + 1];
			Array.Copy(input, 0, paddedInput, 0, input.Length);
			paddedInput[input.Length] = 0xA0;

			hasher.Reset();
			hasher.AppendData(paddedInput, 0, input.Length);
			Byte[] hash = hasher.GetCurrentHash();
			String hashReadable = hash.ToStringHexSingleLine();

			Assert.That(hashReadable, Is.EqualTo(expectedResult), $"Input padded back 1 byte for {input.ToStringHexSingleLine()} - ({input.Length} bytes) {Encoding.ASCII.GetString(input)}");
		}

		using (IIncrementalHash hasher = hashCreator()) {
			Byte[] paddedInput = new Byte[input.Length + 2];
			Array.Copy(input, 0, paddedInput, 1, input.Length);
			paddedInput[0] = 0xA0;
			paddedInput[paddedInput.Length - 1] = 0xA0;

			hasher.Reset();
			hasher.AppendData(paddedInput, 1, input.Length);
			Byte[] hash = hasher.GetCurrentHash();
			String hashReadable = hash.ToStringHexSingleLine();

			Assert.That(hashReadable, Is.EqualTo(expectedResult), $"Input padded front and back 1 byte for {input.ToStringHexSingleLine()} - ({input.Length} bytes) {Encoding.ASCII.GetString(input)}");
		}
	}

	protected static void TestHashFunc(Dictionary<Byte[], String> testCases, Func<IIncrementalHash> hashCreator) {
		foreach (Byte[]? input in testCases.Keys) {
			String expectedResult = testCases[input];
			TestAllAtOnce(expectedResult, input, hashCreator);
		}
	}
}