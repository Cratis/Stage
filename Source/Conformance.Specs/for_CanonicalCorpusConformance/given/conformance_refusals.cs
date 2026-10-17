// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

// Record string equality is ordinal: neither codes nor full artifact identities have aliases or wildcard matches.
public sealed record refusal_fingerprint(string Code, string? Kind, string Artifact)
{
    public override string ToString() => Kind is null ? $"{Code}@{Artifact}" : $"{Code}[{Kind}]@{Artifact}";
}

public sealed record refusal_record(refusal_fingerprint Fingerprint, int? Issue, string? ByDesign);

public sealed record expected_conformance_cell(string Outcome, IReadOnlyList<refusal_record> Refusals);

public sealed record conformance_observation(
    string Outcome,
    IReadOnlyList<refusal_fingerprint> Refusals,
    IReadOnlyDictionary<refusal_fingerprint, string> Details)
{
    public static conformance_observation WithoutRefusals(string outcome) => new(outcome, [], new Dictionary<refusal_fingerprint, string>());
}
#endif
