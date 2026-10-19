// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves command route identities inside the handler, after authorization and validation.
/// </summary>
/// <param name="command">The routed command.</param>
/// <param name="context">The indexed semantic application.</param>
/// <param name="locals">The command's shared Handle name allocator.</param>
internal sealed class SemanticCommandRouteRendering(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals locals)
{
    readonly List<(string Expression, string Variable, string Member, SemanticPrimitiveType Primitive)> _checkedParts = [];

    internal bool HasChecks => _checkedParts.Count > 0;

    internal bool HasOverrides => command.Produces.Any(produced => produced.Route is not null);

    string Helper => $"global::{context.RootNamespace}.GeneratedEventSources.StreamIds";

    internal string? StreamId()
    {
        if (command.Route is not { } route) return null;

        return StreamId(route, string.Empty);
    }

    /// <summary>
    /// Renders the wrapper routing metadata of a production that overrides the command route.
    /// </summary>
    /// <param name="produced">The production.</param>
    /// <param name="index">The production's position, which keeps its checked stream id locals apart from the command's.</param>
    /// <returns>The wrapper initializers, or none when the production follows the command route.</returns>
    /// <remarks>
    /// Arc 22.112.1 CommandTransactionAppender.ResolveDefinition takes the wrapper's EventSource and
    /// EventStream together and inherits nothing of the command's definition. ResolveRouting replaces the
    /// stream id only when the wrapper's differs from EventStreamId.Default, so a keyed override sets it from its own
    /// wrapper. An unkeyed override under a keyed command route, and a literal "Default" stream id, are not proven
    /// equivalent and are refused by STAGE-ESM-036 before rendering.
    /// </remarks>
    internal IEnumerable<string> Override(SemanticProducedEvent produced, int index)
    {
        if (produced.Route is not { } route) yield break;
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);
        yield return $"EventSource = typeof(global::{SemanticEventSourceArtifactRenderer.Namespace(context)}.{SemanticEventSourceArtifactRenderer.ClassName(source)})";
        yield return $"EventStream = {SemanticEventSourceArtifactRenderer.StreamReference(source, stream, context)}";
        if (StreamId(route, $"production{index}") is { } streamId)
        {
            yield return $"EventStreamId = {streamId}";
        }
    }

    /// <summary>
    /// Collects the checked stream id parts of every override and then the command route.
    /// </summary>
    /// <remarks>
    /// The executor resolves every production override before the command route (SemanticRunContext.TryResolveRoute), so
    /// the first refused part reports the same property in the generated Handle().
    /// </remarks>
    internal void Collect()
    {
        foreach (var (produced, index) in command.Produces.Select((produced, index) => (produced, index)))
        {
            _ = Override(produced, index).ToArray();
        }

        StreamId();
    }

    internal void RenderChecks(CSharpCodeBuilder builder, string result)
    {
        foreach (var (expression, variable, member, primitive) in _checkedParts)
        {
            var method = primitive == SemanticPrimitiveType.Text ? "TryText" : "TryInteger";
            var message = primitive == SemanticPrimitiveType.Text
                ? $"Stream id property '{member}' must be nonempty, well-formed Unicode NFC text."
                : $"Stream id property '{member}' must be between -9007199254740991 and 9007199254740991.";
            builder.OpenBlock($"if (!{Helper}.{method}({expression}, out var {variable}))")
                .Line($"return {result}.FromT1(global::Cratis.Arc.Validation.ValidationResult.Error({CSharpCodeBuilder.StringLiteral(message)}, [{CSharpCodeBuilder.StringLiteral(member)}]));")
                .EndBlock();
        }
    }

    string? StreamId(SemanticCommandRoute route, string scope)
    {
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);
        if (stream.StreamIdType is { } type) return Scalar(type, route.StreamId!, 0, scope);
        if (stream.StreamIdParts.IsDefaultOrEmpty) return null;
        var parts = stream.StreamIdParts.Select((part, index) => Scalar(part.Type, route.StreamIdParts.Single(mapping => mapping.Part == part.Name).Value, index, scope)).ToArray();
        return $"{Helper}.Composite({string.Join(", ", parts)})";
    }

    string Scalar(SemanticTypeReference type, SemanticExpression value, int index, string scope)
    {
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;
        if (value is SemanticValueExpression literal)
        {
            string? formatted = null;
            var valid = (primitive, literal.Value) switch
            {
                (SemanticPrimitiveType.Text, SemanticTextValue text) => SemanticStreamIdFormatter.TryFormatText(text.Value, out formatted, out _),
                (SemanticPrimitiveType.Uuid, SemanticTextValue uuid) => SemanticStreamIdFormatter.TryFormatUuidText(uuid.Value, out formatted, out _),
                (SemanticPrimitiveType.WholeNumber, SemanticNumberValue number) => decimal.Truncate(number.Value) == number.Value && SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out formatted, out _),
                _ => false
            };
            if (!valid) throw UnsupportedSemanticRendering.For("literal stream identity", primitive);
            return CSharpCodeBuilder.StringLiteral(formatted!);
        }

        var property = command.Properties.Single(property => property.Id == ((SemanticResolvedExpression)value).Target);
        var member = context.MemberNames.Property(property);
        var expression = type.Kind == SemanticTypeReferenceKind.Concept ? $"{member}.Value" : member;
        if (primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber)
        {
            var variable = locals.Name(scope.Length == 0 ? $"streamIdPart{index}" : $"{scope}StreamIdPart{index}");
            if (_checkedParts.TrueForAll(part => part.Variable != variable))
            {
                _checkedParts.Add((expression, variable, member, primitive));
            }

            return variable;
        }
        return primitive switch
        {
            SemanticPrimitiveType.Uuid => $"{Helper}.Uuid({expression})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPrimitiveType), primitive)
        };
    }
}
