namespace Neco.Test.Common.Processing;

using System;
using System.Collections.Generic;
using Neco.Common.Helper;
using Neco.Common.Processing;
using NUnit.Framework;

[TestFixture]
public class BaseContextTests {
	[Test]
	public void HandlesFeaturesCorrectly() {
		BaseContext ctx = new();
		Dictionary<Object, Object>? features = ReflectionHelper.GetFieldOrPropertyValue<Dictionary<Object, Object>>(ctx, "_features");
		Assert.NotNull(features);
		
		ctx.SetData("Key", "Data");
		Assert.That(ctx.GetData<String, String>("Key"), Is.EqualTo("Data"));
		Assert.That(ctx.GetDataOrDefault<String, String>("Key"), Is.EqualTo("Data"));
		Assert.That(features, Has.Count.EqualTo(1));
		Assert.That(features?["Key"], Is.EqualTo("Data"));
		Assert.That(ctx.TryGetData("Key", out String? availableData), Is.EqualTo(true));
		Assert.That(availableData, Is.EqualTo("Data"));
		
		ctx.SetData("KeyAndData");
		Assert.That(ctx.GetData<Type, String>(typeof(String)), Is.EqualTo("KeyAndData"));
		Assert.That(ctx.GetDataOrDefault<Type, String>(typeof(String)), Is.EqualTo("KeyAndData"));
		Assert.That(features, Has.Count.EqualTo(2));
		Assert.That(features[typeof(String)], Is.EqualTo("KeyAndData"));
		Assert.That(ctx.TryGetData(typeof(String), out String? availableKeyAndData), Is.EqualTo(true));
		Assert.That(availableKeyAndData, Is.EqualTo("KeyAndData"));
		
		ctx.ClearData("KEY");
		Assert.That(ctx.GetData<String, String>("Key"), Is.EqualTo("Data"));
		
		ctx.ClearData("Key");
		Assert.Throws<KeyNotFoundException>(() => ctx.GetData<String, String>("Key"));
		Assert.That(ctx.GetDataOrDefault<String, String>("Key"), Is.Null);
		Assert.That(ctx.TryGetData("Key", out String? unavailableData), Is.EqualTo(false));
		Assert.That(unavailableData, Is.Null);
	}
}