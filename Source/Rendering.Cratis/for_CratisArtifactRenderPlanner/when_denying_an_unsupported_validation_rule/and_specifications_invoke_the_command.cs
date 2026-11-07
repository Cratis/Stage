// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule;

public class and_specifications_invoke_the_command : given.an_invoice_command_with_an_unsupported_validation_rule
{
    void Because() => _plan = Plan();

    [Fact] void should_refuse_both_specifications() => _plan.Refusals.Where(refusal => refusal.Codes.Contains("STAGE-ESM-011")).Select(refusal => refusal.Artifact).ShouldContainOnly(_model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Select(specification => specification.Id));
    [Fact] void should_warn_that_both_specifications_are_skipped() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Message.StartsWith("Skipped specification", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_name_the_denied_command() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011").All(diagnostic => diagnostic.Message.Contains("IssueInvoice", StringComparison.Ordinal) && diagnostic.Message.Contains("STAGE-DEGRADE-002", StringComparison.Ordinal)).ShouldBeTrue();
}
