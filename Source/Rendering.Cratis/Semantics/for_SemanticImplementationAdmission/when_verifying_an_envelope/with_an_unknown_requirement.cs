// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_verifying_an_envelope;

public class with_an_unknown_requirement : given.a_verified_envelope
{
    void Establish() => _request = _request with { ImplementationRequirements = [], ImplementationContents = [] };
    void Because() => _diagnostics = SemanticImplementationAdmission.Verify(_request);

    [Fact] void should_preserve_the_envelope_failure_code() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-ESM-020");
    [Fact] void should_report_an_error() => _diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_report_hash_integrity() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-056");
}
#endif
