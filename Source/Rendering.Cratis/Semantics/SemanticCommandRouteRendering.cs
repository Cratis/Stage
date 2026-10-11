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
internal sealed class SemanticCommandRouteRendering(SemanticCommand command, SemanticApplicationContext context)
{
    readonly List<(string Expression, string Variable, string Member)> _textParts = [];

    internal bool HasText => _textParts.Count > 0;

    string Helper => $"global::{context.RootNamespace}.GeneratedEventSources.StreamIds";

    internal string? StreamId()
    {
        if (command.Route is not { } route) return null;
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);
        if (stream.StreamIdType is { } type) return Scalar(type, route.StreamId!, 0);
        if (stream.StreamIdParts.IsDefaultOrEmpty) return null;
        var parts = stream.StreamIdParts.Select((part, index) => Scalar(part.Type, route.StreamIdParts.Single(mapping => mapping.Part == part.Name).Value, index)).ToArray();
        return $"{Helper}.Composite({string.Join(", ", parts)})";
    }

    internal void RenderChecks(CSharpCodeBuilder builder, string result)
    {
        foreach (var (expression, variable, member) in _textParts)
        {
            builder.OpenBlock($"if (!{Helper}.TryText({expression}, out var {variable}))")
                .Line($"return {result}.FromT1(global::Cratis.Arc.Validation.ValidationResult.Error({CSharpCodeBuilder.StringLiteral($"Stream id property '{member}' must be nonempty, well-formed Unicode NFC text.")}, [{CSharpCodeBuilder.StringLiteral(member)}]));")
                .EndBlock();
        }
    }

    string Scalar(SemanticTypeReference type, SemanticExpression value, int index)
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
        var member = Identifiers.ToPascalCase(property.Name);
        var expression = type.Kind == SemanticTypeReferenceKind.Concept ? $"{member}.Value" : member;
        if (primitive == SemanticPrimitiveType.Text)
        {
            // Property names cannot be a lower-case local name after generated PascalCase admission.
            var variable = $"streamIdPart{index}";
            _textParts.Add((expression, variable, member));
            return variable;
        }
        return primitive switch
        {
            SemanticPrimitiveType.Uuid => $"{Helper}.Uuid({expression})",
            SemanticPrimitiveType.WholeNumber => $"{Helper}.Integer({expression})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPrimitiveType), primitive)
        };
    }
}
