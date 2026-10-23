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

public class when_a_structured_field_is_mapped_to_a_scalar : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_the_reference_scalar_read_refusal(bool collection)
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var capture = slice.Captures.Single();
        capture = capture with { Appends = [.. capture.Appends.Select((append, index) => index == 1 ? append with { When = null } : append)] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Captures = [capture] }] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        var specification = plan.Specifications.Values.Single(value => value.Name == "FirstArrival");
        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        var captures = new SemanticRunCaptures(plan, context);
        captures.Seed(0).ShouldBeNull();
        var incoming = specification.WhenAppended!;
        var fact = new SemanticSpecificationEvent(incoming.EventContract, incoming.Values) { EventSource = incoming.EventSource, Route = incoming.Route };
        var prepared = context.Prepare(fact, incoming.EventSource?.Value ?? SemanticValue.Null, null);
        var status = plan.Events[incoming.EventContract].Properties.Single(property => property.Name == "status").Id;

        // Exercise a structured capture record independently of input materialization.
        SemanticValue structured = collection ? new SemanticArrayValue([]) : new SemanticCompositeValue([]);
        var record = fact with { Values = [.. fact.Values.Select(property => property.TargetProperty == status ? property with { Value = structured } : property)] };
        await context.Append(prepared with { Fact = record }, CancellationToken.None);
        var failure = await captures.Deliver(0, _ => Task.FromResult<SemanticRunReactionFailure?>(null), CancellationToken.None);
        failure!.ToRecord(slice, specification).Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        context.Facts.Count.ShouldEqual(2);
    }
}
