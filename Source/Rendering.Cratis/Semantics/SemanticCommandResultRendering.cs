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
/// <param name="locals">The command's shared Handle name allocator.</param>
internal sealed partial class SemanticCommandResultRendering(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals)
{
    // Arc refuses a compared DateTime input without an offset or Z after authorization, with the reference evaluator's message.
    const string ExplicitOffset = "[property: global::Cratis.Arc.Commands.ExplicitOffset] ";

    readonly SemanticTypeSystem _types = new(context);
    readonly SemanticCommandCompliance _compliance = new(context);

    /// <summary>
    /// Gets the command's protected decision reads.
    /// </summary>
    internal SemanticDecisionReadRendering Reads { get; } = new(command, context, locals);

    IEnumerable<SemanticProperty> Inputs => command.Properties.Where(property => !property.IsGenerated);

    // A command that only decides still names the stream it decides for, so Arc's analyzers see a keyed command.
    SemanticProperty? DestinationProperty
    {
        get
        {
            var destination = command.Produces.IsEmpty ? null : SemanticDestinations.Of(command, command.Produces[0]) as SemanticResolvedExpression;

            return command.Properties.SingleOrDefault(property => property.Id == destination?.Target) ??
                (Reads.HasReads && command.Produces.IsEmpty && command.Response is null ? command.Properties.FirstOrDefault(property => property.IsIdentifier && !property.IsGenerated) : null);
        }
    }

    bool HasOccurrence => command.Produces.Any(produced => produced.Mappings.Any(mapping =>
        mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }));

    internal static string PropertyValue(SemanticProperty property, SemanticApplicationContext? application = null) => property.IsGenerated
        ? Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))
        : application?.MemberNames.Property(property) ?? Identifiers.ToPascalCase(property.Name);

    // Command declarations and validation analysis must agree on raw-bound DateTime concept inputs.
    internal static string InputType(SemanticProperty property, IReadOnlySet<SemanticId> raw, SemanticTypeSystem types) => raw.Contains(property.Id)
        ? types.Primitive(SemanticPrimitiveType.DateTime) + (property.Type.IsOptional ? "?" : string.Empty)
        : types.Type(property.Type);

    internal void RenderDeclaration(CSharpCodeBuilder builder)
    {
        var destination = DestinationProperty;
        var providesSource = destination is { IsGenerated: false };
        var marked = SemanticDateComparisonAdmission.Marked(context.Application, command);
        var raw = SemanticDateComparisonAdmission.Raw(context.Application, command);
        var parameters = string.Join(", ", Inputs.Select(property => $"{(marked.Contains(property.Id) ? ExplicitOffset : string.Empty)}{_compliance.InputAttribute(property.Type)}{InputType(property, raw, _types)} {context.MemberNames.Property(property)}"));
        var name = Identifiers.ToPascalCase(command.Name);
        builder.Attribute(SemanticAuthorizationAttributes.For(command, context))
            .OpenBlock($"public record {name}({parameters}){(providesSource ? " : global::Cratis.Chronicle.Events.ICanProvideEventSourceId" : string.Empty)}");
        if (providesSource)
        {
            builder.Line("/// <inheritdoc/>")
                .ExpressionMember("public global::Cratis.Chronicle.Events.EventSourceId GetEventSourceId()", _types.EventSourceExpression(Value(destination!), destination!.Type))
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
        if (!Reads.HasReads && command.Response is null && command.Properties.All(property => !property.IsGenerated))
        {
            renderEventOnly();
            return;
        }
        RenderCheckedHandle(builder, HandleResult(wrappedEvent));
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

    string Value(SemanticProperty property) => PropertyValue(property, context);

    void RenderCheckedHandle(CSharpCodeBuilder builder, ResultExpression result, Action? renderEvents = null)
    {
        var routing = new SemanticCommandRouteRendering(command, context, locals);
        routing.Collect();
        var routeResult = $"global::OneOf.OneOf<{result.Type}, global::Cratis.Arc.Validation.ValidationResult>";
        var checkedResult = routing.HasChecks || Reads.HasReads;
        builder.OpenBlock($"public {(checkedResult ? routeResult : result.Type)} Handle({Parameters()})");
        if (Reads.HasReads)
        {
            // The decision is read first, so a missing view wins over every requirement; requirements then run in model order.
            Reads.RenderAbsenceGuards(builder, routeResult);
            foreach (var requirement in command.Requirements)
            {
                SemanticRequirementRendering.RenderGuard(builder, requirement, command, context, Reads.Scope, routeResult);
            }
        }
        if (routing.HasChecks) routing.RenderChecks(builder, routeResult);
        if (HasOccurrence)
        {
            builder.Line($"var {locals.Name("occurred")} = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt({locals.Name("operation")});");
        }
        renderEvents?.Invoke();
        builder.Line($"return {(checkedResult ? $"{routeResult}.FromT0({result.Value})" : result.Value)};").EndBlock();
    }

    // Stage renders generated-value freshness itself (explicit concurrency scopes from ESM v15, unguarded below), so
    // Arc's own guard, Guarded by default, must not apply as well until the scopes move to it (Arc#3129).
    string Parameters()
    {
        var generated = command.Properties.Where(property => property.IsGenerated).Select(property =>
            $"[global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute({CSharpCodeBuilder.StringLiteral(property.Name)}, Freshness = global::Cratis.Arc.Commands.GeneratedValueFreshness.Unguarded)] {_types.Type(property.Type)} {Value(property)}");

        return string.Join(", ", Reads.Parameters().Concat(generated).Concat(HasOccurrence ? [$"global::Cratis.Arc.IOperationContextAccessor {locals.Name("operation")}"] : []));
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
        // A decision-only command commits nothing, yet Arc still validates the reads it declared.
        if (command.Produces.IsEmpty) return new(ConditionalEventsType, "[]");

        if (SemanticGeneratedFreshness.Destinations(command, context).Any())
        {
            return new(SemanticGeneratedFreshness.ResultType, SemanticGeneratedFreshness.Events(new(command, context, wrappedEvent, locals)));
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

    // A raw-bound DateTime concept converts back explicitly, so a checked result never needs two user-defined conversions.
    string SourceValue(SemanticId source)
    {
        var property = command.Properties.Single(property => property.Id == source);

        return SemanticDateComparisonAdmission.Raw(context.Application, command).Contains(property.Id)
            ? $"(({_types.Type(property.Type)}){Value(property)})"
            : Value(property);
    }

    sealed record ResultExpression(string Type, string Value);
}
