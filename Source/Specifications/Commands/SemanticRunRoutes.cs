// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticRunRoutes
{
    // Screenplay v4.114.0 SemanticEventRouting.TryFormat: stored names, declaration order and the shared codec.
    internal static bool TryFormat(SemanticApplication application, SemanticFixtureRoute? fixture, out SemanticEventRoute? route, out StreamIdFormatFailure failure)
    {
        route = null;
        failure = StreamIdFormatFailure.None;
        if (fixture is null) return true;
        var source = application.EventSources.Single(source => source.Id == fixture.Source);
        var stream = source.Streams.Single(stream => stream.Id == fixture.Stream);
        string? key = null;
        if (stream.StreamIdType is { } type)
        {
            if (!TryScalar(type, fixture.StreamId!, out key, out failure)) return false;
        }
        else if (!stream.StreamIdParts.IsDefaultOrEmpty)
        {
            var parts = new List<string>();
            foreach (var declaration in stream.StreamIdParts)
            {
                var value = fixture.StreamIdParts.Single(part => part.Part == declaration.Name).Value;
                if (!TryScalar(declaration.Type, value, out var part, out failure)) return false;
                parts.Add(part!);
            }
            key = SemanticStreamIdFormatter.EncodeComposite(parts);
        }
        route = new(source.SourceKind, stream.StreamKind, key);

        return true;

        bool TryScalar(SemanticTypeReference type, SemanticValue value, out string? formatted, out StreamIdFormatFailure error)
        {
            formatted = null;
            error = StreamIdFormatFailure.Noncanonical;
            var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive;
            if (value is SemanticTextValue text)
            {
                return primitive switch
                {
                    SemanticPrimitiveType.Text => SemanticStreamIdFormatter.TryFormatText(text.Value, out formatted, out error),
                    SemanticPrimitiveType.Uuid => SemanticStreamIdFormatter.TryFormatUuidText(text.Value, out formatted, out error),
                    _ => false
                };
            }
            if (primitive == SemanticPrimitiveType.WholeNumber && value is SemanticNumberValue number)
            {
                error = StreamIdFormatFailure.NotIntegral;
                if (decimal.Truncate(number.Value) != number.Value) return false;

                return SemanticStreamIdFormatter.TryFormatInteger(new BigInteger(number.Value), true, out formatted, out error);
            }

            return false;
        }
    }

    internal static SemanticEventRoute? Format(SemanticExecutionPlan plan, SemanticFixtureRoute? fixture)
    {
        if (!TryFormat(plan.Model.Application, fixture, out var route, out var failure))
        {
            throw new InvalidSemanticContract(SemanticStreamIdFormatter.FailureMessage(failure));
        }

        return route;
    }
}
