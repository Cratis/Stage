// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule;

public class in_strict_mode : given.an_invoice_command_with_an_unsupported_validation_rule
{
    void Because() => _plan = Plan(strict: true);

    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
    [Fact] void should_promote_degrade_002_to_an_error() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-DEGRADE-002").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
}
