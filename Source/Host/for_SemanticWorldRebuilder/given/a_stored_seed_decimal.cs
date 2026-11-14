// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_stored_seed_decimal : a_stored_typed_event
{
    protected const string SourceId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    protected void PrepareSeed(string jsonValue, bool exactSchema)
    {
        Prepare("Decimal", jsonValue, exactSchema);
        _stored.Context.CausedBy = new() { Subject = SemanticChronicleRegistration.SeedSubject };
    }

    /// <summary>Gets the seed the model declares for the stored event: one event of the source, holding the decimal.</summary>
    /// <param name="value">The decimal the seed holds.</param>
    /// <param name="sourceId">The event source id of the seed.</param>
    /// <returns>The seed group.</returns>
    internal SemanticSeedGroup Group(decimal value, string sourceId = SourceId)
    {
        var contract = _typedPlan.Events.Values.Single();
        ImmutableArray<SemanticPropertyValue> values =
        [
            new(contract.Properties[0].Id, SemanticValue.Text(SourceId)),
            new(contract.Properties[1].Id, SemanticValue.Number(value))
        ];
        return new SemanticSeedGroup(sourceId, [new SemanticFact(contract.Id, SemanticValue.Text(sourceId), values)]);
    }

    internal SemanticStorageEvidence EvidenceFor(params SemanticSeedGroup[] groups) => new(_exactDecimals, new SemanticSeedPlan([.. groups], []));
}
