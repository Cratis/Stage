// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

// Separate from mapped property identity: admitting this profile never adds or marks a record property.
// Subscriptions are the renderer's actual first-event winners, not another walk of the projection syntax.
internal sealed partial class RootStringKeyProfile(ImmutableDictionary<string, string> values)
{
    // Nonempty, anchored counterpart of Chronicle 313f181 ValueExpressionResolver's payload capture.
    // .NET Unicode \w is intentional; spaces survive unchanged. No trimming or expression evaluation.
    [GeneratedRegex(@"\A[\w ._/:\*\+\-]+\z", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PayloadGrammar { get; }

    internal static RootStringKeyProfile? Admit(
        ProjectionSyntax projection,
        IReadOnlyList<(FromSyntax From, EventSpecSyntax Spec)> subscriptions,
        IReadOnlyList<MappedProperty> properties,
        IEnumerable<QuerySyntax> queries,
        string slicePath)
    {
        if (!subscriptions.Any(subscription => EffectiveKey(subscription.From, subscription.Spec) is LiteralExpressionSyntax { Value: string }))
        {
            return null;
        }

        var first = subscriptions.First(subscription => EffectiveKey(subscription.From, subscription.Spec) is LiteralExpressionSyntax { Value: string });
        UnsupportedRootStringKey Reject(UnsupportedRootStringKeyReason reason, SourceLocation location, string? eventName = null) =>
            new(slicePath, projection.Name, projection.ReadModel ?? projection.Name, eventName ?? first.Spec.Event, reason, location);

        var admitted = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var (from, spec) in subscriptions)
        {
            var key = EffectiveKey(from, spec);
            if (key is not LiteralExpressionSyntax { Value: string value })
            {
                throw Reject(UnsupportedRootStringKeyReason.MixedKeys, key?.Location ?? from.Key?.Location ?? spec.Location, spec.Event);
            }

            if (value.Length == 0)
            {
                throw Reject(UnsupportedRootStringKeyReason.EmptyLiteral, key.Location, spec.Event);
            }

            if (!PayloadGrammar.IsMatch(value))
            {
                throw Reject(UnsupportedRootStringKeyReason.UnencodableLiteral, key.Location, spec.Event);
            }

            if (from.ParentKey is not null)
            {
                throw Reject(UnsupportedRootStringKeyReason.RootParent, from.ParentKey.Location, spec.Event);
            }

            admitted.Add(spec.Event, value);
        }

        if (projection.Key is not null)
        {
            throw Reject(UnsupportedRootStringKeyReason.ExplicitIdentity, projection.Key.Location);
        }

        // These are the actual inferred C# properties, including nested/children/join/global mappings.
        // The generator emits no serialization renames. Conservatively block any casing of Id, regardless
        // of property type. The projection location identifies the inferred record, not an invented source node.
        if (properties.Any(property => property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)))
        {
            throw Reject(UnsupportedRootStringKeyReason.ConventionalId, projection.Location);
        }

        var typeName = Identifiers.ToPascalCase(projection.ReadModel ?? projection.Name);
        foreach (var by in queries.Where(query => QueryRenderer.Reads(query, typeName)).Select(query => query.By).OfType<QueryParameterSyntax>())
        {
            if (by.Type is not { Name: "String", IsCollection: false, IsOptional: false })
            {
                throw Reject(UnsupportedRootStringKeyReason.IncompatibleByType, by.Location);
            }
        }

        // Chronicle 16.38.2 applies class-level ConstantKey while processing constructor parameters/properties.
        // With neither, registration keeps the default event-source key. Do not synthesize record properties.
        if (properties.Count == 0)
        {
            throw Reject(UnsupportedRootStringKeyReason.PropertylessRecord, projection.Location);
        }

        return new(admitted.ToImmutable());
    }

    internal string ValueFor(string eventName) => values[eventName];

    static ExpressionSyntax? EffectiveKey(FromSyntax from, EventSpecSyntax spec) =>
        spec.Key ?? (from.Key as ExpressionKeySyntax)?.Expression;
}
