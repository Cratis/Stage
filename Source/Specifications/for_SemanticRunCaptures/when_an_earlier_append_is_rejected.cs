// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_an_earlier_append_is_rejected : Specification
{
    SemanticRunReactionFailure? _result;
    int _settlements;

    async Task Because()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var capture = slice.Captures.Single();
        var later = capture.Appends[1] with { When = null };
        capture = capture with { Appends = [capture.Appends[0], later, capture.Appends[2]] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Captures = [capture] }] }] }] };
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
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
        var malformed = fact with
        {
            Values = [.. fact.Values.Select(property => property.TargetProperty == status ? property with { Value = SemanticValue.Number(42) } : property)]
        };
        await context.Append(prepared with { Fact = malformed }, CancellationToken.None);
        _result = await captures.Deliver(
            0,
            _ =>
            {
                _settlements++;
                return Task.FromResult<SemanticRunReactionFailure?>(SemanticRunReactionFailure.Reject("earlier rejection", "guard"));
            },
            CancellationToken.None);
    }

    [Fact] void should_preserve_the_first_disposition() => _result.ShouldEqual(SemanticRunReactionFailure.Reject("earlier rejection", "guard"));
    [Fact] void should_not_evaluate_the_later_invalid_mapping() => _settlements.ShouldEqual(1);
}
