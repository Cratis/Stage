// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_verifying_an_envelope;

public class with_a_tampered_body : given.a_verified_envelope
{
    void Establish() => _request = _request with { ImplementationContents = _request.ImplementationContents.SetItem(_requirement.RequirementId, "return false;") };
    void Because() => _diagnostics = SemanticImplementationAdmission.Verify(_request);

    [Fact] void should_use_the_dedicated_hash_integrity_code() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-ESM-056");
    [Fact] void should_report_an_error() => _diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_report_an_envelope_failure() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-020");
    [Fact] void should_name_the_requirement() => _diagnostics.Single().Message.ShouldContain(_requirement.RequirementId);
}
#endif
