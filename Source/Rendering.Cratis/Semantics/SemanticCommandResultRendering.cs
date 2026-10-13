// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the Handle contract for generated values and command responses.
/// </summary>
/// <param name="command">The command whose result is rendered.</param>
/// <param name="context">The indexed semantic application.</param>
internal sealed partial class SemanticCommandResultRendering(SemanticCommand command, SemanticApplicationContext context)
{
    readonly SemanticTypeSystem _types = new(context);
    readonly SemanticCommandCompliance _compliance = new(context);

    IEnumerable<SemanticProperty> Inputs => command.Properties.Where(property => !property.IsGenerated);

    SemanticProperty? DestinationProperty
    {
        get
        {
            var destination = command.Produces.IsEmpty ? null : SemanticDestinations.Of(command, command.Produces[0]) as SemanticResolvedExpression;

            return command.Properties.SingleOrDefault(property => property.Id == destination?.Target);
        }
    }

    bool HasOccurrence => command.Produces.Any(produced => produced.Mappings.Any(mapping =>
        mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }));

    internal static string PropertyValue(SemanticProperty property) => property.IsGenerated
        ? Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))
        : Identifiers.ToPascalCase(property.Name);

    internal void RenderDeclaration(CSharpCodeBuilder builder)
    {
        var destination = DestinationProperty;
        var providesSource = destination is { IsGenerated: false };
        var parameters = string.Join(", ", Inputs.Select(property => $"{_compliance.InputAttribute(property.Type)}{_types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));
        var name = Identifiers.ToPascalCase(command.Name);
        builder.Attribute(SemanticAuthorizationAttributes.For(command, context))
            .OpenBlock($"public record {name}({parameters}){(providesSource ? " : global::Cratis.Chronicle.Events.ICanProvideEventSourceId" : string.Empty)}");
        if (providesSource)
        {
            builder.Line("/// <inheritdoc/>")
                .ExpressionMember("public global::Cratis.Chronicle.Events.EventSourceId GetEventSourceId()", _types.EventSourceExpression(PropertyValue(destination!), destination!.Type))
                .BlankLine();
        }
    }

    internal void RenderHandle(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent, Action renderEventOnly)
    {
        if (command.Produces.Any(produced => produced.When is not null))
        {
            RenderConditionalHandle(builder, wrappedEvent);
            return;
        }
        if (command.Response is null && command.Properties.All(property => !property.IsGenerated))
        {
            renderEventOnly();
            return;
        }
        var result = HandleResult(wrappedEvent);
        builder.OpenBlock($"public {result.Type} Handle({Parameters()})");
        if (HasOccurrence)
        {
            builder.Line($"var occurred = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt(operation);");
        }
        builder.Line($"return {result.Value};").EndBlock();
    }

    internal void RenderResponseType(CSharpCodeBuilder builder)
    {
        if (command.Response is not SemanticRecordCommandResponse response) return;
        var name = Identifiers.ToPascalCase(command.Name);
        var fields = response.Fields.Select(field => $"{_types.Type(field.Type)} {Identifiers.ToPascalCase(field.Name)}");
        builder.Summary($"The response from {name}.")
            .Line($"public record {name}Response({string.Join(", ", fields)});")
            .BlankLine();
    }

    string Parameters()
    {
        var generated = command.Properties.Where(property => property.IsGenerated).Select(property =>
            $"[global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute({CSharpCodeBuilder.StringLiteral(property.Name)})] {_types.Type(property.Type)} {PropertyValue(property)}");

        return string.Join(", ", generated.Concat(HasOccurrence ? ["global::Cratis.Arc.IOperationContextAccessor operation"] : []));
    }

    ResultExpression HandleResult(Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var events = Events(wrappedEvent);
        var response = Response();
        if (response is null) return events;
        if (command.Produces.IsEmpty) return response;

        return new($"({response.Type}, {events.Type})", $"({response.Value}, {events.Value})");
    }

    ResultExpression Events(Func<SemanticProducedEvent, string> wrappedEvent)
    {
        if (SemanticGeneratedFreshness.Destinations(command, context).Any())
        {
            return new(SemanticGeneratedFreshness.ResultType, SemanticGeneratedFreshness.Events(command, context, wrappedEvent));
        }

        return command.Produces.Length == 1
            ? new("global::Cratis.Chronicle.EventSequences.EventForEventSourceId", wrappedEvent(command.Produces[0]))
            : new(
                "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>",
                $"new global::Cratis.Chronicle.EventSequences.EventForEventSourceId[] {{ {string.Join(", ", command.Produces.Select(wrappedEvent))} }}");
    }

    ResultExpression? Response() => command.Response switch
    {
        SemanticScalarCommandResponse scalar => new(_types.Type(scalar.Type), SourceValue(scalar.Source)),
        SemanticRecordCommandResponse record => new(
            $"{Identifiers.ToPascalCase(command.Name)}Response",
            $"new {Identifiers.ToPascalCase(command.Name)}Response({string.Join(", ", record.Fields.Select(field => SourceValue(field.Source)))})"),
        _ => null
    };

    string SourceValue(SemanticId source) => PropertyValue(command.Properties.Single(property => property.Id == source));

    sealed record ResultExpression(string Type, string Value);
}
