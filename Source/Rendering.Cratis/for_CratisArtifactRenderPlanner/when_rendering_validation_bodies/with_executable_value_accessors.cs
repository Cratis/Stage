// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_executable_value_accessors
{
    [Theory]
    [InlineData("return context.ValueAs<int>() > 0;", 1L, true)]
    [InlineData("return context.ValueAs<int>() > 0;", 2147483648L, false)]
    [InlineData("return context.ValueAs<long>() > 0;", 1L, true)]
    [InlineData("return (int)context.Value > 0;", 2147483648L, false)]
    public void should_execute_checked_whole_numbers(string body, long value, bool valid)
    {
        var source = validation_body_model.Command(body, generated: true).Replace("amount Decimal", "amount Int", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Equal(valid, validation_body_model.Validate(opaque_policy_model.Compile(plan), "PlaceOrder", value).IsValid);
    }

    [Theory]
    [InlineData("int", new long[] { 1, 2 }, true)]
    [InlineData("int", new long[] { 2147483648L }, false)]
    [InlineData("int", new long[] { 2147483647L, 1 }, false)]
    [InlineData("long", new long[] { 1, 2 }, true)]
    [InlineData("long", new long[] { long.MaxValue, 1 }, false)]
    public void should_execute_the_lazy_collection(string element, long[] values, bool valid)
    {
        var source = validation_body_model.Command($"return context.ValueAs<IEnumerable<{element}>>().Sum() > 0;", generated: true).Replace("amount Decimal", "amount Int[]", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Equal(valid, validation_body_model.Validate(opaque_policy_model.Compile(plan), "PlaceOrder", values).IsValid);
    }

    [Theory]
    [InlineData("return (decimal)context.Value > 0;")]
    [InlineData("return context.ValueAs<decimal>() > 0;")]
    public void should_execute_the_primitive_concept_conversion(string body)
    {
        var source = validation_body_model.Command(body, generated: true)
            .Replace("concept OrderId : Uuid", "concept OrderId : Uuid\nconcept Money : Decimal", StringComparison.Ordinal)
            .Replace("amount Decimal", "amount Money", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var money = assembly.GetTypes().Single(type => type.Name == "Money");
        Assert.True(validation_body_model.Validate(assembly, "PlaceOrder", Activator.CreateInstance(money, 5m)!).IsValid);
    }

    [Fact]
    public void should_execute_the_artifact_accessor()
    {
        var source = validation_body_model.Command("return context.ArtifactAs<PlaceOrder>().Amount > 0 && !context.IsWholeArtifact;", generated: true);
        var plan = opaque_policy_model.Plan(validation_body_model.Load(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.True(validation_body_model.Validate(opaque_policy_model.Compile(plan), "PlaceOrder", 5m).IsValid);
    }

    [Fact]
    public void should_fail_an_iterator_rule_on_overflow()
    {
        var source = validation_body_model.IteratorConcept("if ((int)context.Value > 0) yield return \"Not allowed\";")
            .Replace("concept Amount : Decimal", "concept Amount : Int", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(validation_body_model.LoadWholeNumbers(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.False(validation_body_model.Validate(opaque_policy_model.Compile(plan), "Amount", 2147483648L).IsValid);
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("nb-NO")]
    public void should_execute_with_invariant_culture_and_restore_the_caller(string name)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command("return context.ValueAs<string>().StartsWith(\"I\");", generated: true).Replace("amount Decimal", "amount String", StringComparison.Ordinal)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var caller = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = caller;
            CultureInfo.CurrentUICulture = caller;
            Assert.True(validation_body_model.Validate(assembly, "PlaceOrder", "Invariant").IsValid);
            Assert.Same(caller, CultureInfo.CurrentCulture);
            Assert.Same(caller, CultureInfo.CurrentUICulture);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }
}
#endif
