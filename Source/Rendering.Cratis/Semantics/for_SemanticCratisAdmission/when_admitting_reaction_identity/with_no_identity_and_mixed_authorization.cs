// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity;

/// <summary>
/// A reaction with no <c language="play">runs as</c> that invokes both a command without an <c language="play">authorize</c>
/// clause and an authorized command is refused: the inferred system identity would otherwise satisfy the authorized command's gate.
/// </summary>
public class with_no_identity_and_mixed_authorization : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    void Establish() => SetContext(Compile("policy CanApprove\n  require role \"Approver\"\n" + Source
        .Replace(
            "      event Acknowledged",
            "      command ApproveWork\n        id String identifier\n        amount Decimal\n        authorize CanApprove\n" +
            "        produces event Approved\n          for id\n          amount Decimal = amount\n      event Acknowledged",
            StringComparison.Ordinal)
        .Replace(
            "            amount = amount\n      reaction Acknowledge",
            "            amount = amount\n          invokes ApproveWork\n            id = id\n            amount = amount\n      reaction Acknowledge",
            StringComparison.Ordinal)));

    void Because() => Plan();

    [Fact]
    void should_refuse_the_reaction() => _plan.Diagnostics.ShouldContain(diagnostic =>
        diagnostic.Code == "STAGE-ESM-034" && diagnostic.Severity == Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains("'ApproveWork'", StringComparison.Ordinal));
    [Fact] void should_not_report_the_default_system_identity() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-055");
}
