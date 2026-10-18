// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.for_refusal_dispositions.when_validating;

public class with_a_regenerated_by_design_record : Specification
{
    const string Reason = "Chronicle reserves the stored name in this frozen golden vector";
    readonly refusal_fingerprint _fingerprint = new("STAGE-ESM-030", null, "sem1:0000000000000000000000000000000000000000000000000000000000002333");
    expected_conformance_cell _regenerated = null!;
    conformance_observation _actual = null!;
    IReadOnlyList<string> _problems = null!;

    void Establish()
    {
        var previous = new expected_conformance_table
        {
            Vectors = new Dictionary<string, IReadOnlyDictionary<conformance_surface, expected_conformance_cell>>
            {
                ["EsmGoldenVectors.V24"] = new Dictionary<conformance_surface, expected_conformance_cell>
                {
                    [conformance_surface.Renderer] = new("refused", [new(_fingerprint, null, Reason)])
                }
            }
        };
        _actual = new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>());
        var actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>>
        {
            ["EsmGoldenVectors.V24"] = new Dictionary<conformance_surface, conformance_observation>
            {
                [conformance_surface.Renderer] = _actual
            }
        };
        _regenerated = expected_conformance_table.Load(expected_conformance_table.Render(previous, "1.0.0", actual)).Vectors["EsmGoldenVectors.V24"][conformance_surface.Renderer];
    }

    void Because() => _problems = refusal_dispositions.Problems("EsmGoldenVectors.V24", conformance_surface.Renderer, _regenerated, _actual);

    [Fact] void should_report_no_problems() => _problems.ShouldBeEmpty();
    [Fact] void should_keep_the_by_design_reason() => _regenerated.Refusals.Single().ByDesign.ShouldEqual(Reason);
    [Fact] void should_not_add_an_issue() => _regenerated.Refusals.Single().Issue.ShouldBeNull();
}
#endif
