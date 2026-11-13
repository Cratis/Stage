// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Chronicle.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host;

internal static partial class SemanticWorldRebuilder
{
    static SemanticEventRoute? Route(SemanticExecutionPlan plan, Chronicle.Contracts.Sequences.EventContext context)
    {
        if (IsDefaultRoute(context))
        {
            return null;
        }

        var source = (plan.Model.Application.EventSources.IsDefault ? [] : plan.Model.Application.EventSources)
            .SingleOrDefault(candidate => candidate.SourceKind == context.EventSourceType);
        var stream = source?.Streams.SingleOrDefault(candidate => candidate.StreamKind == context.EventStreamType)
            ?? throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} has an unknown stored event-source route.");
        var keyed = stream.StreamIdType is not null || !stream.StreamIdParts.IsDefaultOrEmpty;

        return new(
            context.EventSourceType,
            context.EventStreamType,
            !keyed && context.EventStreamId == EventStreamId.Default ? null : context.EventStreamId);
    }

    static bool IsDefaultRoute(Chronicle.Contracts.Sequences.EventContext context) =>
        context.EventSourceType == EventSourceType.Default.Value &&
        context.EventStreamType == EventStreamType.All.Value &&
        context.EventStreamId == EventStreamId.Default;

    static bool RouteMatches(SemanticExecutionPlan plan, SemanticCommandRoute? productionRoute, SemanticEventRoute? route)
    {
        if (route is null)
        {
            return productionRoute is null;
        }

        if (productionRoute is not { } declaration)
        {
            return false;
        }

        var source = plan.Model.Application.EventSources.Single(candidate => candidate.Id == declaration.Source);
        var stream = source.Streams.Single(candidate => candidate.Id == declaration.Stream);
        if (source.SourceKind != route.SourceKind || stream.StreamKind != route.StreamKind)
        {
            return false;
        }

        return StreamIdMatches(plan.Model.Application, declaration, stream, route);
    }

    static bool StreamIdMatches(SemanticApplication application, SemanticCommandRoute declaration, SemanticEventStream stream, SemanticEventRoute route)
    {
        if (declaration.StreamId is SemanticValueExpression literal)
        {
            return TryFormatLiteral(ScalarKind(stream.StreamIdType!, application), literal.Value, out var formatted) && formatted == route.StreamId;
        }

        if (!declaration.StreamIdParts.IsDefaultOrEmpty && route.StreamId is { } encoded)
        {
            return CompositeStreamIdMatches(application, declaration, stream, encoded);
        }

        return true;
    }

    static bool CompositeStreamIdMatches(SemanticApplication application, SemanticCommandRoute declaration, SemanticEventStream stream, string encoded)
    {
        var kinds = stream.StreamIdParts.Select(part => ScalarKind(part.Type, application)).ToArray();
        if (!SemanticStreamIdFormatter.TryDecodeComposite(encoded, kinds, out var decoded, out _))
        {
            return false;
        }

        return stream.StreamIdParts.Select((part, index) => (declaration.StreamIdParts.Single(mapping => mapping.Part == part.Name).Value, Index: index))
            .Where(part => part.Value is SemanticValueExpression)
            .All(part => TryFormatLiteral(kinds[part.Index], ((SemanticValueExpression)part.Value).Value, out var formatted) && formatted == decoded![part.Index]);
    }

    static StreamIdScalarKind ScalarKind(SemanticTypeReference type, SemanticApplication application) =>
        (type.Kind == SemanticTypeReferenceKind.Concept ? application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive) switch
        {
            SemanticPrimitiveType.Uuid => StreamIdScalarKind.Uuid,
            SemanticPrimitiveType.WholeNumber => StreamIdScalarKind.WholeNumber,
            SemanticPrimitiveType.Text => StreamIdScalarKind.Text,
            _ => throw new SemanticWorldRebuildRefused("A stored route has an unsupported stream identity type.")
        };

    static bool TryFormatLiteral(StreamIdScalarKind kind, SemanticValue value, out string? formatted)
    {
        formatted = null;
        return (kind, value) switch
        {
            (StreamIdScalarKind.Text, SemanticTextValue text) => SemanticStreamIdFormatter.TryFormatText(text.Value, out formatted, out _),
            (StreamIdScalarKind.Uuid, SemanticTextValue text) => SemanticStreamIdFormatter.TryFormatUuidText(text.Value, out formatted, out _),
            (StreamIdScalarKind.WholeNumber, SemanticNumberValue number) when decimal.Truncate(number.Value) == number.Value =>
                SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out formatted, out _),
            _ => false
        };
    }
}
