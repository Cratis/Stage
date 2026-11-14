// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal static partial class SemanticWorldRebuilder
{
    static void EnsureSeedMatches(bool seedMustMatch, SemanticStorageEvidence evidence, SemanticEventContract contract, Chronicle.Contracts.Sequences.EventContext context, ImmutableArray<SemanticPropertyValue> values)
    {
        if (seedMustMatch && !evidence.Matches(contract.Id, context.EventSourceId, values))
        {
            throw new SemanticWorldRebuildRefused($"Event {context.SequenceNumber} is a seed whose decimal values Chronicle storage did not keep exactly (its stored schema has no decimal format) and that does not equal the model's seed.");
        }
    }

    // A date and time is never read back exactly, but a stored seed is exempt from that, as it was before decimals were checked. A decimal is, when the event type's stored schema carries the exact decimal format on every number;
    // a stored seed in a type that does not is deferred to the seed match, because the model still holds the value it was seeded with.
    // Returns whether such a seed decimal must be matched against the model.
    static bool EnsureExactStorageTypes(SemanticEventContract contract, SemanticApplication application, bool decimalsExact, bool isSeed)
    {
        var seedMustMatch = false;
        foreach (var (path, primitive) in contract.Properties.SelectMany(property => StoragePrimitives(property.Name, property.Type, application)))
        {
            var lossyDecimal = primitive == SemanticPrimitiveType.DecimalNumber && !decimalsExact;
            if (lossyDecimal && isSeed)
            {
                seedMustMatch = true;
            }
            else if (IsLossy(primitive, lossyDecimal, isSeed))
            {
                throw new SemanticWorldRebuildRefused($"Event property '{path}' uses {primitive}, which Chronicle storage cannot round-trip exactly.");
            }
        }

        return seedMustMatch;
    }

    static bool IsLossy(SemanticPrimitiveType primitive, bool lossyDecimal, bool isSeed) =>
        lossyDecimal || (primitive == SemanticPrimitiveType.DateTime && !isSeed);

    static IEnumerable<(string Path, SemanticPrimitiveType Primitive)> StoragePrimitives(string path, SemanticTypeReference type, SemanticApplication application)
    {
        if (type.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            return application.Types.Single(candidate => candidate.Id == type.Target).Properties
                .SelectMany(property => StoragePrimitives($"{path}.{property.Name}", property.Type, application));
        }

        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? application.Concepts.Single(candidate => candidate.Id == type.Target).Primitive
            : type.Primitive;
        return primitive is SemanticPrimitiveType.DateTime or SemanticPrimitiveType.DecimalNumber ? [(path, primitive)] : [];
    }

    // The position of each stored seed event among the seed events stored for the same event source and event type.
    static Dictionary<ulong, int> SeedOrdinals(AppendedEventResponse[] history) =>
        history.Where(@event => @event.Context.CausedBy?.Subject == SemanticChronicleRegistration.SeedSubject)
            .GroupBy(@event => (@event.Context.EventSourceId, @event.Context.EventType.Id))
            .SelectMany(group => group.Select((@event, ordinal) => (@event.Context.SequenceNumber, ordinal)))
            .ToDictionary(entry => entry.SequenceNumber, entry => entry.ordinal);
}
