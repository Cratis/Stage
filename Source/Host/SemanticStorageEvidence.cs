// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Host;

/// <summary>
/// What a restart knows about how exactly Chronicle kept the decimals of the events it reads back.
/// </summary>
/// <param name="ExactDecimals">The stored event names whose stored schema carries the exact decimal format on every number; empty, a decimal is refused.</param>
/// <param name="Seeds">The seeds the model declares, so a stored seed decimal that equals the model's value exactly can be accepted when its stored schema is not exact.</param>
/// <param name="Ordinal">The position of the stored event among the seed events stored for the same event source and event type.</param>
/// <remarks>
/// A decimal that Chronicle stored without its exact format may have been rounded on the way in. The model still holds the value it was seeded
/// with, so the stored seed is accepted only when it is uniquely matched and every value equals the model's exactly. A missing or ambiguous
/// match proves nothing, so it does not match.
/// </remarks>
internal sealed record SemanticStorageEvidence(IReadOnlySet<string> ExactDecimals, SemanticSeedPlan? Seeds = null, int Ordinal = 0)
{
    /// <summary>Gets the evidence that nothing is known to be stored exactly.</summary>
    internal static SemanticStorageEvidence None { get; } = new(new HashSet<string>());

    /// <summary>
    /// Gets whether every decimal of an event type is stored exactly.
    /// </summary>
    /// <param name="storedEventName">The stored event name.</param>
    /// <returns><see langword="true" /> when its stored schema carries the exact decimal format on every number.</returns>
    internal bool HasExactDecimals(string storedEventName) => ExactDecimals.Contains(storedEventName);

    /// <summary>
    /// Gets whether the values read back for a stored seed event equal the model's seed event exactly.
    /// </summary>
    /// <param name="eventContract">The event contract of the stored event.</param>
    /// <param name="eventSourceId">The event source id of the stored event.</param>
    /// <param name="stored">The values read back from storage.</param>
    /// <returns><see langword="true" /> when exactly one model seed event corresponds and all values are equal.</returns>
    internal bool Matches(SemanticId eventContract, string eventSourceId, ImmutableArray<SemanticPropertyValue> stored)
    {
        if (Seeds is null) return false;

        var groups = Seeds.Groups.Where(group => group.EventSourceId == eventSourceId).ToArray();
        if (groups.Length != 1) return false;

        var facts = groups[0].Facts.Where(fact => fact.EventContract == eventContract).ToArray();
        return Ordinal >= 0 && Ordinal < facts.Length && Same(stored, facts[Ordinal].Values);
    }

    static bool Same(IEnumerable<SemanticPropertyValue> stored, IEnumerable<SemanticPropertyValue> seeded)
    {
        var seededByProperty = seeded.ToDictionary(value => value.TargetProperty, value => value.Value);
        var storedProperties = stored.Select(value => value.TargetProperty).ToHashSet();
        return seededByProperty.Keys.All(storedProperties.Contains) &&
            stored.All(value => Same(value.Value, seededByProperty.GetValueOrDefault(value.TargetProperty, SemanticValue.Null)));
    }

    // Decimals compare by value, so 0.3 equals 0.30; everything else is compared as the model holds it.
    static bool Same(SemanticValue stored, SemanticValue seeded) => (stored, seeded) switch
    {
        (SemanticNumberValue left, SemanticNumberValue right) => left.Value == right.Value,
        (SemanticArrayValue left, SemanticArrayValue right) => left.Values.Length == right.Values.Length && left.Values.Zip(right.Values).All(pair => Same(pair.First, pair.Second)),
        (SemanticCompositeValue left, SemanticCompositeValue right) => Same(left.Properties, right.Properties),
        _ => stored.Equals(seeded)
    };
}
