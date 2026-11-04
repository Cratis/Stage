// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule;

public class with_an_undefined_severity : given.an_invoice_command_with_an_unsupported_validation_rule
{
    Exception? _error;

    protected override SemanticValidationRule Unsupported(SemanticValidationRule rule) => rule with { Severity = (SemanticValidationSeverity)99 };

    void Because() => _error = Catch.Exception(() => _plan = Plan());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_be_degraded() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_warn_with_degrade_002_on_the_command() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-DEGRADE-002" && diagnostic.Artifact == _command.Id).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_not_refuse_with_esm_005() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-005").ShouldBeFalse();
}
