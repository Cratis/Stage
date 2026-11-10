// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_frozen_capture_numbers : given.a_translation_application
{
    MethodInfo _text = null!;

    void Establish() => _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan()), slice => slice with { Specifications = [] });

    void Because()
    {
        Render();
        _text = _assembly.GetType("Contracts.Shipping.Tracking.Receive.Feed", throwOnError: true)!.GetMethod("Text", BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    [Theory]
    [InlineData(1.23d, "1.23")]
    [InlineData(9007199254740991d, "9007199254740990")]
    [InlineData(-1.23d, "-1.23")]
    [InlineData(1e-28d, "0.0000000000000000000000000001")]
    [InlineData(1e28d, "10000000000000000000000000000")]
    [InlineData(1e-100d, "0")]
    [InlineData(1.234567890123445d, "1.23456789012344")]
    public void should_execute_the_frozen_double_conversion(double value, string expected) => Text(value).ShouldEqual(expected);

    [Theory]
    [InlineData(1.23f, "1.23")]
    [InlineData(12345678f, "12345680")]
    [InlineData(1e28f, "9999999000000000000000000000")]
    public void should_execute_the_frozen_single_conversion(float value, string expected) => Text(value).ShouldEqual(expected);

    [Fact] void should_keep_exact_decimals() => Text(1.2345678901234567890123456789m).ShouldEqual("1.2345678901234567890123456789");
    [Fact] void should_keep_exact_integers() => Text(9007199254740991L).ShouldEqual("9007199254740991");
    [Fact] void should_keep_text() => Text("1.23").ShouldEqual("1.23");
    [Fact] void should_keep_booleans() => Text(true).ShouldEqual("true");
    [Fact] void should_keep_null() => Text(null).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => RenderedOutput.Warnings(_sources).ShouldBeEmpty();

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(1e29d)]
    public void should_keep_overflow_rejection(double value)
    {
        var exception = Catch.Exception(() => Text(value));
        exception.ShouldBeOfExactType<TargetInvocationException>();
        exception!.InnerException.ShouldBeOfExactType<OverflowException>();
    }

    string Text(object? value) => (string)_text.Invoke(null, [value])!;
}
#endif
