// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_expected_conformance_table.when_rendering;

public class with_an_unchanged_fingerprint : Specification
{
    expected_conformance_table _previous = null!;
    IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> _actual = null!;
    string _rendered = null!;

    void Establish()
    {
        _previous = new() { Vectors = new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>
        {
            ["V1"] = new Dictionary<conformance_surface, expected_conformance_cell>
            {
                [conformance_surface.Renderer] = new("refused", [new(new("STAGE-ESM-010", null, "sem1:one"), 501, null)])
            }
        } };
        _actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>>
        {
            ["V1"] = new Dictionary<conformance_surface, conformance_observation>
            {
                [conformance_surface.Renderer] = new("refused", [new("STAGE-ESM-010", null, "sem1:one")], new Dictionary<refusal_fingerprint, string>())
            }
        };
    }

    void Because() => _rendered = expected_conformance_table.Render(_previous, "1.0.0", _actual);

    [Fact] void should_keep_its_disposition() => expected_conformance_table.Load(_rendered).Vectors["V1"][conformance_surface.Renderer].Refusals.Single().Issue.ShouldEqual(501);
}
#endif
