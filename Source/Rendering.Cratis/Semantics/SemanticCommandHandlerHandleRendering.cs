// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Renders the unchecked Handle path around a typed handler payload batch.</summary>
/// <param name="command">Command owning the payload handler.</param>
/// <param name="context">Semantic application and generated type context.</param>
/// <param name="locals">Allocated Handle-local names.</param>
/// <param name="hasOccurrence">Whether the event envelopes use the command receipt time.</param>
internal sealed class SemanticCommandHandlerHandleRendering(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals, bool hasOccurrence)
{
    internal void Render(CSharpCodeBuilder builder, bool bare, Func<SemanticProducedEvent, string> eventValue, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var single = command.Produces.Length == 1;
        var resultType = single ? "global::Cratis.Chronicle.EventSequences.EventForEventSourceId" : "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";
        if (bare) resultType = new SemanticTypeSystem(context).EventType(context.Events[command.Produces[0].EventContract]);
        var parameters = hasOccurrence ? $"global::Cratis.Arc.IOperationContextAccessor {locals.Name("operation")}" : string.Empty;
        builder.OpenBlock($"public {resultType} Handle({parameters})");
        if (hasOccurrence)
        {
            builder.Line($"var {locals.Name("occurred")} = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt({locals.Name("operation")});");
        }
        SemanticCommandHandlerRendering.Prepare(builder, command, context, locals);
        var payload = single ? wrappedEvent(command.Produces[0]) : $"[{string.Join(", ", command.Produces.Select(wrappedEvent))}]";
        if (bare) payload = eventValue(command.Produces[0]);
        builder.Line($"return {payload};").EndBlock();
    }
}
