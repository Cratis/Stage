// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Exercises generated inbox and outbox reactor handlers directly with isolated sequence dependencies.
/// </summary>
internal static partial class SemanticTranslationSpecificationRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var located = context.DeclaringSlice(specification.Id);
        var capture = SemanticTranslationSpecificationAdmission.Capture(located.Slice, specification.WhenAppended!);
        var publication = SemanticTranslationSpecificationAdmission.Publication(located.Slice, specification.WhenAppended!);
        var reactor = $"global::{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{Identifiers.ToPascalCase(capture?.Name ?? publication!.Name)}";
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}";
        var builder = new CSharpCodeBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Specifications").Using("NSubstitute")
            .Using("Cratis.Chronicle.Events").Using("System.Linq");
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification")
            .Line("readonly global::Cratis.Chronicle.IEventStore _store = Substitute.For<global::Cratis.Chronicle.IEventStore>();")
            .Line("readonly global::Cratis.Chronicle.EventSequences.IEventLog _log = Substitute.For<global::Cratis.Chronicle.EventSequences.IEventLog>();")
            .Line("readonly global::Cratis.Chronicle.EventSequences.IEventSequence _outbox = Substitute.For<global::Cratis.Chronicle.EventSequences.IEventSequence>();")
            .Line("readonly global::System.Collections.Generic.List<global::Cratis.Chronicle.Events.AppendedEvent> _history = [];")
            .Line("readonly global::System.Collections.Generic.List<(global::Cratis.Chronicle.Events.EventSourceId EventSourceId, object Event)> _published = [];")
            .Line($"{reactor} _reactor = null!;")
            .Line("readonly global::System.Collections.Generic.List<global::Cratis.Chronicle.EventSequences.EventForEventSourceId> _produced = [];")
            .Line(SemanticSpecificationValuesRenderer.Declarations).BlankLine();
        RenderSetup(builder, specification, context, types, reactor);
        RenderAct(builder, specification, context, types, capture, publication);
        RenderAssertions(builder, specification, context, types, publication is not null);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), types.SpecificationContent(builder.ToString()));
    }

    static void RenderSetup(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types, string reactor)
    {
        builder.OpenBlock("void Establish()")
            .Line("_store.EventLog.Returns(_log);")
            .Line("_store.GetEventSequence(global::Cratis.Chronicle.EventSequences.EventSequenceId.Outbox).Returns(_outbox);")
            .Line("_log.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<global::System.Collections.Generic.IEnumerable<EventType>>()).Returns(call => global::System.Threading.Tasks.Task.FromResult<global::System.Collections.Immutable.IImmutableList<AppendedEvent>>(global::System.Collections.Immutable.ImmutableList.CreateRange(_history.Where(fact => fact.Context.EventSourceId == call.Arg<EventSourceId>() && call.Arg<global::System.Collections.Generic.IEnumerable<EventType>>().Contains(fact.Context.EventType)))));")
            .Line("_outbox.GetForEventSourceIdAndEventTypes(Arg.Any<EventSourceId>(), Arg.Any<global::System.Collections.Generic.IEnumerable<EventType>>()).Returns(global::System.Threading.Tasks.Task.FromResult<global::System.Collections.Immutable.IImmutableList<AppendedEvent>>(global::System.Collections.Immutable.ImmutableList<AppendedEvent>.Empty));")
            .OpenBlock("_outbox.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(call =>")
            .Line("_published.Add((call.Arg<EventSourceId>(), call.Arg<object>()));")
            .Line("return global::System.Threading.Tasks.Task.FromResult(new global::Cratis.Chronicle.EventSequences.AppendResult { SequenceNumber = global::Cratis.Chronicle.Events.EventSequenceNumber.First });")
            .EndBlock().Line(");");
        foreach (var contract in context.Events.Values)
        {
            builder.Line($"_store.EventTypes.GetEventTypeFor(typeof({types.EventType(contract)})).Returns(typeof({types.EventType(contract)}).GetEventType());");
        }
        foreach (var given in specification.GivenEvents)
        {
            var contract = context.Events[given.EventContract];
            builder.Line($"_history.Add(new(EventContext.Empty with {{ EventSourceId = {Source(given.EventSource, types)}, EventType = typeof({types.EventType(contract)}).GetEventType() }}, {Fact(contract, given.Values, types)}));");
        }
        var slice = context.DeclaringSlice(specification.Id).Slice;
        var capture = SemanticTranslationSpecificationAdmission.Capture(slice, specification.WhenAppended!);
        var needsStore = capture?.Appends.Any(append => append.When is not null) != false;
        builder.Line($"_reactor = new {reactor}({(needsStore ? "_store" : string.Empty)});").EndBlock().BlankLine();
    }

    static void RenderAct(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types, SemanticCapture? capture, SemanticProjection? publication)
    {
        var append = specification.WhenAppended!;
        var contract = context.Events[append.EventContract];
        var invocation = $"_reactor.On{Identifiers.ToPascalCase(contract.Name)}({Fact(contract, append.Values, types)}";
        builder.OpenBlock("async global::System.Threading.Tasks.Task Because()");
        if (publication is not null)
        {
            builder.Line($"await {invocation}, EventContext.Empty with {{ EventSourceId = {Source(append.EventSource, types)} }});");
        }
        else
        {
            var awaitResult = capture!.Appends.Any(append => append.When is not null) ? "await " : string.Empty;
            builder.Line($"_produced.AddRange(({awaitResult}{invocation})).Cast<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>());");
            if (awaitResult.Length == 0) builder.Line("await global::System.Threading.Tasks.Task.CompletedTask;");
        }
        builder.EndBlock().BlankLine();
    }

    static string Source(SemanticEventSourceIdentity? source, SemanticTypeSystem types) => source is null
        ? "global::Cratis.Chronicle.Events.EventSourceId.Unspecified"
        : types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type);

    static string Fact(SemanticEventContract contract, System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values, SemanticTypeSystem types) =>
        $"new {types.EventType(contract)}({string.Join(", ", contract.Properties.Select(property => types.Value(values.Single(value => value.TargetProperty == property.Id).Value, property.Type)))})";
}
