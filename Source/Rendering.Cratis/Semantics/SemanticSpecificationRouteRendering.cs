// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Preserves explicit fixture routes in generated seeds and event assertions.
/// </summary>
internal static class SemanticSpecificationRouteRendering
{
    internal static string Predicate(SemanticSpecificationEvent expected, SemanticApplicationContext context, string entry)
    {
        if (expected.Unrouted)
        {
            return $" && {entry}.Event.Context.EventSourceType == global::Cratis.Chronicle.Events.EventSourceType.Default" +
                $" && {entry}.Event.Context.EventStreamType == global::Cratis.Chronicle.Events.EventStreamType.All" +
                $" && {entry}.Event.Context.EventStreamId == global::Cratis.Chronicle.Events.EventStreamId.Default";
        }
        if (expected.Route is not { } route) return string.Empty;
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);

        return $" && {entry}.Event.Context.EventSourceType == {CSharpCodeBuilder.StringLiteral(source.SourceKind)}" +
            $" && {entry}.Event.Context.EventStreamType == {CSharpCodeBuilder.StringLiteral(stream.StreamKind)}" +
            $" && {entry}.Event.Context.EventStreamId == {StreamId(route, context)}";
    }

    internal static string Arguments(SemanticFixtureRoute route, SemanticApplicationContext context)
    {
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);

        return $"eventSourceType: {CSharpCodeBuilder.StringLiteral(source.SourceKind)}, eventStreamType: {CSharpCodeBuilder.StringLiteral(stream.StreamKind)}, eventStreamId: {StreamId(route, context)}";
    }

    internal static bool CanFormat(SemanticFixtureRoute route, SemanticApplicationContext context)
    {
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);

        return stream.StreamIdType is { } type
            ? Scalar(type, route.StreamId!, context) is not null
            : stream.StreamIdParts.All(part => Scalar(part.Type, route.StreamIdParts.Single(value => value.Part == part.Name).Value, context) is not null);
    }

    static string StreamId(SemanticFixtureRoute route, SemanticApplicationContext context)
    {
        var source = context.Application.EventSources.Single(source => source.Id == route.Source);
        var stream = source.Streams.Single(stream => stream.Id == route.Stream);
        if (stream.StreamIdType is { } type) return Scalar(type, route.StreamId!, context)!;
        if (stream.StreamIdParts.IsDefaultOrEmpty) return "global::Cratis.Chronicle.Events.EventStreamId.Default";
        var parts = stream.StreamIdParts.Select(part => Scalar(part.Type, route.StreamIdParts.Single(value => value.Part == part.Name).Value, context));

        return $"global::{context.RootNamespace}.GeneratedEventSources.StreamIds.Composite({string.Join(", ", parts)})";
    }

    static string? Scalar(SemanticTypeReference type, SemanticValue value, SemanticApplicationContext context)
    {
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;
        var helper = $"global::{context.RootNamespace}.GeneratedEventSources.StreamIds";
        switch (primitive, value)
        {
            case (SemanticPrimitiveType.Text, SemanticTextValue text) when SemanticStreamIdFormatter.TryFormatText(text.Value, out var formatted, out _):
                // Like literal command routes, text is validated without normalization during planning.
                return CSharpCodeBuilder.StringLiteral(formatted!);
            case (SemanticPrimitiveType.Uuid, SemanticTextValue uuid) when SemanticStreamIdFormatter.TryFormatUuidText(uuid.Value, out var formatted, out _):
                return $"{helper}.Uuid(global::System.Guid.Parse({CSharpCodeBuilder.StringLiteral(formatted!)}))";
            case (SemanticPrimitiveType.WholeNumber, SemanticNumberValue number) when decimal.Truncate(number.Value) == number.Value &&
                SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out var formatted, out _):
                // Portable literals can exceed the generated Int property's CLR range. Like literal command
                // routes, format those during planning rather than narrowing them to an int.
                return number.Value is >= int.MinValue and <= int.MaxValue
                    ? $"{helper}.Integer({new SemanticTypeSystem(context).Value(value, SemanticTypeReference.ForPrimitive(primitive))})"
                    : CSharpCodeBuilder.StringLiteral(formatted!);
            default:
                return null;
        }
    }
}
