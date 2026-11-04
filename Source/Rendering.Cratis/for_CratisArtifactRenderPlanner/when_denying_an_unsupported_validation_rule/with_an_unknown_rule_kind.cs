// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule;

public class with_an_unknown_rule_kind : given.an_invoice_command_with_an_unsupported_validation_rule
{
    void Because() => _plan = Plan();

    string CommandText => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    ArtifactRenderConstruct Command => _plan.Inventory.Constructs.Single(construct => construct.Identity == _command.Id);

    [Fact] void should_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_be_degraded() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_have_no_errors() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_refuse_with_esm_005() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-005").ShouldBeFalse();
    [Fact] void should_warn_with_degrade_002_on_the_command() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-DEGRADE-002" && diagnostic.Artifact == _command.Id).Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
    [Fact] void should_record_degradation_evidence() => _plan.Degradations.Single(gap => gap.Diagnostic.Artifact == _command.Id).Diagnostic.ShouldEqual(_plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-DEGRADE-002"));
    [Fact] void should_mark_the_command_degraded_in_the_inventory() => Command.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_deny_the_enclosing_operation_in_the_inventory() => Command.Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.DenyEnclosingOperation);
    [Fact] void should_render_an_always_failing_validator_rule() => CommandText.ShouldContain("RuleFor(_ => _).Must(_ => false).WithMessage(\"STAGE-DEGRADE-002:");
    [Fact] void should_block_on_error_severity() => CommandText.ShouldContain("BlockOnValidationSeverity(ValidationResultSeverity.Error)");
    [Fact] void should_keep_the_command_handler() => CommandText.ShouldContain("Handle(");
}
