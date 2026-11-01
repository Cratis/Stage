// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_running_source_keyed_captures : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    public async Task should_match_the_pinned_reference_for_inferred_and_explicit_sources(string? source)
    {
        var plan = Plan(specification => specification with { WhenAppended = specification.WhenAppended! with { EventSource = Source(source) } });
        var expected = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        expected.Passed.ShouldBeTrue();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_a_mismatched_source_in_history_or_the_action(bool history)
    {
        var plan = Plan(specification => history
            ? specification with { GivenEvents = [new(specification.WhenAppended!.EventContract, specification.WhenAppended.Values) { EventSource = Source("wrong") }] }
            : specification with { WhenAppended = specification.WhenAppended! with { EventSource = Source("wrong") } });
        var expected = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        var rejection = Assert.IsType<SemanticRejected>(expected.Execution);
        rejection.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
        result.Trace!.Rejection.ShouldEqual(rejection.Details);
    }

    [Fact]
    public async Task should_infer_independent_history_sources_and_suppress_identical_redelivery()
    {
        var plan = Plan(specification => specification with
        {
            GivenEvents =
            [
                new(specification.WhenAppended!.EventContract, specification.WhenAppended.Values),
                new(specification.WhenAppended.EventContract, [new(specification.WhenAppended.Values.Single().TargetProperty, SemanticValue.Text("9fa85f64-5717-4562-b3fc-2c963f66afa9"))])
            ],
            ThenEvents = [],
            ThenNoEvents = true
        });
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single()).Passed.ShouldBeTrue();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_missing_and_null_key_contract_rejections(bool missing)
    {
        var plan = Plan(specification => specification);
        var appended = plan.Specifications.Values.Single().WhenAppended!;
        var fact = new SemanticSpecificationEvent(appended.EventContract, missing ? [] : [new(appended.Values.Single().TargetProperty, SemanticValue.Null)]);
        var foreign = new SemanticFact(fact.EventContract, SemanticValue.Text("source"), fact.Values);
        var expected = Assert.Throws<InvalidSemanticContract>(() => new SemanticReactionSettlement(new SemanticEvaluator()).Settle(plan, SemanticWorld.Create([foreign], []), [foreign], null));
        var actual = Assert.Throws<InvalidSemanticContract>(() => SemanticCaptureSources.Normalize(plan, fact));
        actual.Message.ShouldEqual(expected.Message);
    }

    [Fact]
    public async Task should_execute_the_unchanged_v33_vector()
    {
        var plan = SemanticExecutionPlan.Compile(Model()).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single()).Passed.ShouldBeTrue();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    internal static SemanticExecutionPlan Plan(Func<SemanticSpecification, SemanticSpecification> change, SemanticTypeReference? keyType = null) => Transform(Model(), slice => ChangeSlice(slice, change, keyType), keyType is not null);

    static SemanticSlice ChangeSlice(SemanticSlice slice, Func<SemanticSpecification, SemanticSpecification> change, SemanticTypeReference? keyType)
    {
        if (slice.Kind != SemanticSliceKind.Translate) return slice;
        if (keyType is not null)
        {
            slice = slice with
            {
                Events = [.. slice.Events.Select(contract => contract with { Properties = [.. contract.Properties.Select(property => property with { Type = keyType })] })],
                Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { EventSourceType = keyType })] })]
            };
        }
        var source = slice.Events.Single(contract => contract.Origin is not null);
        var target = slice.Events.Single(contract => contract.Origin is null);
        var key = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var specification = slice.Specifications.Single() with
        {
            GivenEvents = [],
            ThenReadModels = [],
            WhenAppended = new(source.Id, [new(source.Properties.Single().Id, key)]),
            ThenEvents = [new(target.Id, [new(target.Properties.Single().Id, key)]) { EventSource = new(source.Properties.Single().Type, key) }]
        };
        return slice with { Specifications = [change(specification)] };
    }

    static ExecutableSemanticModel Model() => SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());

    static SemanticEventSourceIdentity? Source(string? value) => value is null ? null : new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text(value));

    static SemanticExecutionPlan Transform(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> change, bool captureOnly = false)
    {
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Where(slice => !captureOnly || slice.Kind == SemanticSliceKind.Translate).Select(change)] })]
            })]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }
}
