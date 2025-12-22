namespace Neco.Test.Common.Extensions;

using System.Collections.Concurrent;
using System.Globalization;
using Neco.Common.Extensions;

[TestFixture]
public class DictionaryExtensionTests {
	[Test]
	public void GetOrAdd() {
		Dictionary<String, Int32> dictionary = new();

		Int32 newkey = dictionary.GetOrAdd("newkey", 1);
		Assert.That(newkey, Is.EqualTo(1));
		newkey = dictionary.GetOrAdd("newkey", 2);
		Assert.That(newkey, Is.EqualTo(1));
	}

	[Test]
	public void GetOrAddFunc() {
		Dictionary<Int32, Int32> dictionary = new();

		Int32 newkey = dictionary.GetOrAdd(42, key => key * 2);
		Assert.That(newkey, Is.EqualTo(84));
		newkey = dictionary.GetOrAdd(42, key => 123);
		Assert.That(newkey, Is.EqualTo(84));
	}

	[Test]
	public void GetOrAddFuncWithArgument() {
		Dictionary<Int32, Int32> dictionary = new();

		Int32 newkey = dictionary.GetOrAdd(42, (key, arg) => key * arg, 2);
		Assert.That(newkey, Is.EqualTo(84));
		newkey = dictionary.GetOrAdd(42, (_, _) => 123, 10);
		Assert.That(newkey, Is.EqualTo(84));
	}

	[Test]
	public void GetOrAddConcurrent() {
		// This is not a proper test and is only included to see that ConcurrentDictionary does not conflict
		ConcurrentDictionary<String, Int32> dictionary = new();
		Int32 newkey = dictionary.GetOrAdd("newkey", 1);
		Assert.That(newkey, Is.EqualTo(1));
	}

	[Test]
	public void AppendValue() {
		Dictionary<Int32, List<Int32>>  dictionary = new();
		
		dictionary.AppendValue(42, 1);
		Assert.That(dictionary.Count, Is.EqualTo(1));
		Assert.That(dictionary[42].Count, Is.EqualTo(1));
		Assert.That(dictionary[42][0], Is.EqualTo(1));
		
		dictionary.AppendValue(42, 5);
		Assert.That(dictionary.Count, Is.EqualTo(1));
		Assert.That(dictionary[42].Count, Is.EqualTo(2));
		Assert.That(dictionary[42][1], Is.EqualTo(5));
	}

	[Test]
	public void GetAs() {
		Dictionary<String, String> dictionary = new();
		dictionary.Add("int", "+0,042.0 ");
		Assert.That(dictionary.GetAsInt32("int"), Is.EqualTo(42));
		Assert.That(dictionary.GetAsInt64("int"), Is.EqualTo(42L));
		Assert.That(dictionary.GetAsUInt32("int"), Is.EqualTo(42U));
		Assert.That(dictionary.GetAsUInt64("int"), Is.EqualTo(42UL));
		Assert.That(dictionary.GetAsDouble("int"), Is.EqualTo(42.0f));
		Assert.That(dictionary.GetAsSingle("int"), Is.EqualTo(42.0));

		Boolean tryGetValue = dictionary.TryGetAs("int", out Int32 value, NumberStyles.Number);
		Assert.That(tryGetValue, Is.EqualTo(true));
		Assert.That(value, Is.EqualTo(42));
		
		Boolean tryGetMissingValue = dictionary.TryGetAs("non-existing", out Int32 _);
		Assert.That(tryGetMissingValue, Is.EqualTo(false));
		
		Assert.That(dictionary.GetAsOrDefault("int", 5), Is.EqualTo(42));
		Assert.That(dictionary.GetAsOrDefault("non-existing", 5), Is.EqualTo(5));
	}
}