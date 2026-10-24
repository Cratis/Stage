// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_delivering_typed_keys : Specification
{
    [Theory]
    [InlineData(SemanticPrimitiveType.Uuid, true)]
    [InlineData(SemanticPrimitiveType.Uuid, false)]
    [InlineData(SemanticPrimitiveType.Text, false)]
    [InlineData(SemanticPrimitiveType.WholeNumber, false)]
    public async Task should_preserve_the_declared_identity_in_captured_facts(SemanticPrimitiveType primitive, bool concept)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        var keyType = concept ? SemanticTypeReference.ForConcept(model.Application.Concepts.Single().Id) : SemanticTypeReference.ForPrimitive(primitive);
        SemanticValue key = primitive == SemanticPrimitiveType.WholeNumber ? SemanticValue.Number(42) : new SemanticTextValue("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var plan = WithKey(model, keyType, key);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");

        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        var captures = new SemanticRunCaptures(plan, context);
        captures.Seed(0).ShouldBeNull();
        var appended = specification.WhenAppended!;
        await context.Append(new(appended.EventContract, appended.Values), SemanticValue.Null, CancellationToken.None);
        (await captures.Deliver(0, _ => Task.FromResult<SemanticRunReactionFailure?>(null), CancellationToken.None)).ShouldBeNull();
        var fact = context.Facts.Last();
        fact.EventSource!.Type.ShouldEqual(keyType);
        fact.EventSource.Value.ShouldEqual(key);
        fact.EventSource.ShouldEqual(reference.Execution.World.Facts.Last().Context!.EventSource);
        context.Facts.Count.ShouldEqual(reference.Execution.World.Facts.Length);
    }

    [Fact]
    public async Task should_execute_the_frozen_v32_typed_given_and_projection()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan())).Plan!;
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single()).Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_refuse_a_capture_key_that_cannot_be_an_identity()
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        var plan = WithKey(model, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean), new SemanticBooleanValue(true));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        Assert.StartsWith("STAGE-ESM-052:", result.Unsupported!.Details, StringComparison.Ordinal);
    }

    static SemanticExecutionPlan WithKey(ExecutableSemanticModel model, SemanticTypeReference type, SemanticValue key)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Where(slice => slice.Kind == SemanticSliceKind.Translate).Select(slice => slice with
        {
            Events = [.. slice.Events.Select(contract => contract with { Properties = [.. contract.Properties.Select(property => property with { Type = type })] })],
            ReadModels = [],
            Projections = [],
            Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { EventSourceType = type })] })],
            Specifications = [.. slice.Specifications.Select(specification => Delivery(slice, specification, type, key))]
        });
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        var text = type == SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(text ? LanguageVersion.V9 : LanguageVersion.V32, text ? SemanticVersion.V9 : SemanticVersion.V32, application)).Plan!;
    }

    static SemanticSpecification Delivery(SemanticSlice slice, SemanticSpecification specification, SemanticTypeReference type, SemanticValue key)
    {
        var source = slice.Events.Single(contract => contract.Origin is not null);
        var target = slice.Events.Single(contract => contract.Origin is null);
        return specification with
        {
            GivenEvents = [],
            WhenAppended = new(source.Id, [new(source.Properties.Single().Id, key)]),
            ThenEvents = [new(target.Id, [new(target.Properties.Single().Id, key)]) { EventSource = new(type, key) }],
            ThenReadModels = []
        };
    }
}
