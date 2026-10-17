// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_comparing_renderer_refusal_artifacts : Specification
{
    const string Vector = "GenerationFixturesCorpus.V21";
    const string OtherArtifact = "sem1:0000000000000000000000000000000000000000000000000000000000000002";
    expected_conformance_cell _cell = null!;
    renderer_refusal _invalid = null!;
    renderer_refusal _other = null!;
    string _actual = null!;
    string _extra = null!;
    IReadOnlyList<string> _valid = null!;
    IReadOnlyList<string> _untracked = null!;
    IReadOnlyList<string> _forgedExemption = null!;
    IReadOnlyList<string> _forgedIssue = null!;
    IReadOnlyList<string> _missingExemption = null!;
    IReadOnlyList<string> _staleExemption = null!;
    bool _matches = true;

    void Establish()
    {
        _invalid = renderer_refusals.AllowedByDesign.Single();
        _other = new(_invalid.Code, OtherArtifact);
        var abort = new renderer_refusal("STAGE-ESM-011", OtherArtifact);
        _actual = $"refused:{abort}+{_invalid}";
        _extra = $"{_actual}+{_other}";
        _cell = new(_actual, 500, null, [new(_invalid.Code, "Intentionally invalid generation fixture", _invalid.Artifact)])
        {
            IssueRefusals = [abort]
        };
    }

    void Because()
    {
        _matches = renderer_refusals.Matches(_actual, _extra);
        _valid = renderer_refusals.CoverageProblems(Vector, _cell, _actual);
        _untracked = renderer_refusals.CoverageProblems(Vector, _cell, _extra);
        _forgedExemption = renderer_refusals.CoverageProblems(Vector, _cell with
        {
            ByDesign = [.. _cell.ByDesign, new(_other.Code, "Not an authorized invalid fixture", _other.Artifact)]
        }, _extra);
        _forgedIssue = renderer_refusals.CoverageProblems(Vector, _cell with
        {
            IssueRefusals = [.. _cell.IssueRefusals, _other]
        }, _extra);
        _missingExemption = renderer_refusals.CoverageProblems(Vector, _cell with { ByDesign = [] }, _actual);
        _staleExemption = renderer_refusals.CoverageProblems(Vector, _cell, $"refused:{_cell.IssueRefusals.Single()}+{_other}");
    }

    [Fact] void should_detect_a_same_code_refusal_on_a_different_artifact() => _matches.ShouldBeFalse();
    [Fact] void should_preserve_both_artifacts_sharing_one_code() => renderer_refusals.Parse(_extra).Count.ShouldEqual(3);
    [Fact] void should_accept_only_the_deliberately_invalid_artifact() => _valid.ShouldBeEmpty();
    [Fact] void should_leave_the_other_artifact_untracked() => _untracked.ShouldContain($"untracked-refusal({_other})");
    [Fact] void should_reject_an_exemption_for_the_other_artifact() => _forgedExemption.ShouldContain($"invalid-by-design-entry({_other})");
    [Fact] void should_not_let_issue_500_cover_the_other_artifact() => _forgedIssue.ShouldContain($"issue-does-not-cover-refusal({_other})");
    [Fact] void should_require_the_invalid_artifact_exemption_even_with_issue_500() => _missingExemption.ShouldContain($"untracked-refusal({_invalid})");
    [Fact] void should_reject_a_stale_exemption_even_when_its_code_still_occurs() => _staleExemption.ShouldContain($"unnecessary by-design entry({_invalid})");
}
#endif
