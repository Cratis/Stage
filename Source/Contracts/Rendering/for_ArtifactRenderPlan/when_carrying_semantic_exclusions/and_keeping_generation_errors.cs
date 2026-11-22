// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_keeping_generation_errors : given.a_request_with_exclusions
{
    void Because() => _result = Create([new("TARGET001", ArtifactRenderDiagnosticSeverity.Error, "Unsafe target.", _request.Model.Application.Id)]).WithAcceptance([], strict: true);

    [Fact] void should_refuse_publication() => _result.CanPublishArtifacts.ShouldBeFalse();
    [Fact] void should_remain_refused() => _result.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_preserve_original_generation_errors() => _result.WithAcceptance([]).Diagnostics.Single(diagnostic => diagnostic.Code == "TARGET001").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
}
