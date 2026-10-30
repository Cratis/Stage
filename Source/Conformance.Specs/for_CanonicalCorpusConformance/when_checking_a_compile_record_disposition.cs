// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_checking_a_compile_record_disposition : Specification
{
    readonly refusal_fingerprint _fingerprint = new("CS0246", "compile", "Features/Bad.cs");
    IReadOnlyList<string> _tracked = null!;
    IReadOnlyList<string> _untracked = null!;
    IReadOnlyList<string> _byDesign = null!;

    void Because()
    {
        _tracked = Problems(824, null);
        _untracked = Problems(null, null);
        _byDesign = Problems(824, "not-supported");
    }

    [Fact] void should_accept_a_tracked_compile_record() => _tracked.ShouldBeEmpty();
    [Fact] void should_reject_an_untracked_compile_record() => _untracked.Any(problem => problem.Contains("compile-record-needs-issue", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_by_design_compile_record() => _byDesign.Any(problem => problem.Contains("compile-record-needs-issue", StringComparison.Ordinal)).ShouldBeTrue();

    IReadOnlyList<string> Problems(int? issue, string? byDesign) =>
        refusal_dispositions.Problems("Vector", conformance_surface.Renderer, new("refused", [new(_fingerprint, issue, byDesign)]), new("refused", [_fingerprint], new Dictionary<refusal_fingerprint, string>()));
}
#endif
