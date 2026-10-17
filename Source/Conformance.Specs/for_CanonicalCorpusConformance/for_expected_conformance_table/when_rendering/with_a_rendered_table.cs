// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_expected_conformance_table.when_rendering;

public class with_a_rendered_table : Specification
{
    expected_conformance_table _previous = null!;
    IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> _actual = null!;
    expected_conformance_table _loaded = null!;
    refusal_record[] _records = null!;

    void Establish()
    {
        _records = [new(new("STAGE-PLAN-004", "UnsupportedQuery", "Orders"), 501, null),
            new(refusal_dispositions.InvalidGeneration, null, "Deliberately invalid fixture")];
        _previous = new() { Vectors = new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>
        {
            ["V1"] = new Dictionary<conformance_surface, expected_conformance_cell>
            {
                [conformance_surface.Renderer] = new("refused", _records)
            }
        } };
        _actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>>
        {
            ["V1"] = new Dictionary<conformance_surface, conformance_observation>
            {
                [conformance_surface.Renderer] = new("refused", [.. _records.Select(record => record.Fingerprint)], new Dictionary<refusal_fingerprint, string>())
            }
        };
    }

    void Because() => _loaded = expected_conformance_table.Load(expected_conformance_table.Render(_previous, "1.0.0", _actual));

    [Fact] void should_load_back_identical_records() => _loaded.Vectors["V1"][conformance_surface.Renderer].Refusals.SequenceEqual(_records).ShouldBeTrue();
    [Fact] void should_use_the_accepted_schema() => _loaded.SchemaProblems.ShouldBeEmpty();
}
#endif
