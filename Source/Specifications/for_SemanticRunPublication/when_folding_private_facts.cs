// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunPublication;

public class when_folding_private_facts : Specification
{
    EventStoreForTesting _store = null!;
    SemanticRunContext _context = null!;
    SemanticSpecification _specification = null!;
    SemanticRunPublication _publication = null!;
    SemanticSpecificationEvent _input = null!;
    SemanticValue _key = null!;
    SemanticRunReactionFailure? _failure;
    SemanticSpecificationEvent[] _published = [];

    void Establish()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan())).Plan!;
        _specification = plan.Specifications.Values.Single();
        var types = new SemanticRuntimeTypes(plan);
        _store = new(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        _context = new(typeof(DynamicCommand), null!, _specification, new(), types, _store.EventLog, plan);
        _publication = new(plan, _context, 0);
        var appended = _specification.WhenAppended!;
        _input = new(appended.EventContract, appended.Values) { EventSource = appended.EventSource };
        _key = appended.EventSource?.Value ?? SemanticValue.Null;
    }

    async Task Because()
    {
        await _context.Append(_input, _key, CancellationToken.None);
        _failure = await _publication.PublishPending(CancellationToken.None);
        await _context.Append(_input, _key, CancellationToken.None);
        _failure ??= await _publication.PublishPending(CancellationToken.None);
        var changed = _input with { Values = [_input.Values.Single() with { Value = SemanticValue.Text("UPS") }] };
        await _context.Append(changed, _key, CancellationToken.None);
        _failure ??= await _publication.PublishPending(CancellationToken.None);
        _published = [.. _context.Facts.Where(fact => fact.EventContract == _specification.ThenEvents.Single().EventContract)];
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_settle_without_failure() => _failure.ShouldBeNull();
    [Fact] void should_publish_only_initial_and_changed_content() => _published.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_canonical_first_publication() => _specification.ThenEvents.Single().Values.All(expected => _published[0].Values.Any(actual => actual.TargetProperty == expected.TargetProperty && SemanticExpectationComparer.AreEqual(actual.Value, expected.Value))).ShouldBeTrue();
    [Fact] void should_publish_the_changed_carrier() => _published[1].Values.ShouldContain(value => value.Value == SemanticValue.Text("UPS"));
    [Fact] void should_keep_the_event_source() => _published.All(fact => fact.EventSource == _input.EventSource).ShouldBeTrue();
}
