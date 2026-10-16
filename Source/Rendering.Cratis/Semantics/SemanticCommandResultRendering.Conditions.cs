// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed partial class SemanticCommandResultRendering
{
    const string ConditionalEventsType = "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";

    void RenderConditionalHandle(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var events = ConditionalEvents(wrappedEvent);
        var response = Response();
        var result = response is null ? events : new ResultExpression($"({response.Type}, {events.Type})", $"({response.Value}, {events.Value})");
        RenderCheckedHandle(builder, result, () => RenderSelectedEvents(builder, wrappedEvent));
    }

    void RenderSelectedEvents(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        foreach (var (produced, index) in command.Produces.Select((produced, index) => (produced, index)))
        {
            var condition = produced.When is null ? "true" : SemanticConditionRendering.Render(produced.When, new(command.Properties, string.Empty, context, PropertyValue));
            builder.Line($"var {locals.Name($"stageProduces{index}")} = {condition};");
        }
        builder.Line($"var {locals.Name("stageEvents")} = new global::System.Collections.Generic.List<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>();");
        foreach (var (produced, index) in command.Produces.Select((produced, index) => (produced, index)))
        {
            builder.OpenBlock($"if ({locals.Name($"stageProduces{index}")})").Line($"{locals.Name("stageEvents")}.Add({wrappedEvent(produced)});").EndBlock();
        }
    }

    ResultExpression ConditionalEvents(Func<SemanticProducedEvent, string> wrappedEvent) =>
        SemanticGeneratedFreshness.Destinations(command, context).Any()
            ? new(SemanticGeneratedFreshness.ResultType, SemanticGeneratedFreshness.Events(new(command, context, wrappedEvent, locals, locals.Name("stageEvents"), SourceGuard)))
            : new(ConditionalEventsType, locals.Name("stageEvents"));

    string SourceGuard(SemanticProperty property)
    {
        var targets = command.Produces.Select((produced, index) => (produced, index))
            .Where(item => SemanticDestinations.Of(command, item.produced) is SemanticResolvedExpression destination && destination.Target == property.Id).ToArray();
        return targets.Any(item => item.produced.When is null) ? "true" : string.Join(" || ", targets.Select(item => locals.Name($"stageProduces{item.index}")));
    }
}
