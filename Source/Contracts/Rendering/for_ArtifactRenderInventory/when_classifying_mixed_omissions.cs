// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_classifying_mixed_omissions : given.an_inventory
{
    [Fact]
    void should_preserve_partial_publication_and_original_omission_warnings_through_acceptance()
    {
        var (owner, warning, omission) = Omitted();
        var inventory = ArtifactRenderInventory.Create([_rendered, owner]);
        var plan = ArtifactRenderPlan.Create(_request, [], [warning], [], [omission], []).WithInventory(inventory);
        Assert.Equal(ArtifactRenderOutcome.Degraded, inventory.Outcome);
        Assert.Equal(1, inventory.Score.Counts.Refused);
        Assert.Equal(1, inventory.Score.Counts.Rendered);
        Assert.True(plan.CanPublishArtifacts);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.WithAcceptance([], strict: true).Outcome);
        Assert.Equal(plan.Digest, ArtifactRenderPlan.Create(_request, [], [warning], [], [omission], []).WithInventory(ArtifactRenderInventory.Create([owner, _rendered])).Digest);
        Assert.Equal(ArtifactRenderOutcome.Refused, ArtifactRenderInventory.Create([owner]).Outcome);
    }

    [Fact]
    void should_not_accept_warning_evidence_without_an_actual_plan_omission()
    {
        var (owner, warning, _) = Omitted();
        var inventory = ArtifactRenderInventory.Create([owner, _rendered]);
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request, [], [warning]).WithInventory(inventory));
    }

    [Fact]
    void should_reject_wrong_omission_codes_and_fabricated_original_warning_text()
    {
        var (owner, warning, omission) = Omitted();
        var inventory = ArtifactRenderInventory.Create([owner, _rendered]);
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request, [], [warning], [], [omission with { Codes = ["OTHER"] }], []).WithInventory(inventory));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request, [], [warning with { Message = "Different evidence." }], [], [omission], []).WithInventory(inventory));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([owner with { Kind = ArtifactRenderConstructKind.Command }]));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([owner with { Omission = null }]));
    }

    [Fact]
    void should_keep_genuine_generation_errors_refused_and_nonpublishable()
    {
        var (owner, warning, omission) = Omitted();
        var error = new ArtifactRenderDiagnostic("SAFETY", ArtifactRenderDiagnosticSeverity.Error, "Unsafe generation.", _rendered.Identity);
        var unsafeOwner = _rendered with { Outcome = ArtifactRenderOutcome.Refused, RefusalReasons = [error] };
        var inventory = ArtifactRenderInventory.Create([owner, unsafeOwner]);
        var plan = ArtifactRenderPlan.Create(_request, [], [warning, error], [], [omission], []).WithInventory(inventory);
        Assert.Equal(ArtifactRenderOutcome.Refused, inventory.Outcome);
        Assert.False(plan.CanPublishArtifacts);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
    }

    [Fact]
    void should_reject_each_invalid_omission_owner_and_witness_relationship()
    {
        var (owner, _, omission) = Omitted();
        ArtifactRenderConstruct[] invalid =
        [
            owner with { Outcome = ArtifactRenderOutcome.Rendered },
            owner with { Kind = ArtifactRenderConstructKind.Command },
            owner with { Omission = omission with { Artifact = Id(6) } },
            owner with { Omission = omission with { Kind = "command" } },
            owner with { Omission = omission with { Codes = default } },
            owner with { Omission = omission with { Codes = [] } }
        ];
        Assert.All(invalid, candidate => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([candidate])));
    }

    [Fact]
    void should_reject_wrong_warning_shape_without_dereferencing_null_reasons()
    {
        var (owner, warning, _) = Omitted();
        ArtifactRenderDiagnostic[] invalid =
        [
            warning with { Artifact = Id(6) },
            warning with { Code = "OTHER" },
            warning with { Severity = ArtifactRenderDiagnosticSeverity.Information },
            warning with { Severity = ArtifactRenderDiagnosticSeverity.Error },
            warning with { Message = string.Empty },
            null!
        ];
        Assert.All(invalid, reason => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([owner with { RefusalReasons = [reason] }])));
    }

    [Fact]
    void should_require_every_original_diagnostic_field_when_attaching_omission_evidence()
    {
        var (owner, warning, omission) = Omitted();
        var inventory = ArtifactRenderInventory.Create([owner]);
        ArtifactRenderDiagnostic[] changed =
        [
            warning with { Artifact = Id(6) },
            warning with { Code = "OTHER" },
            warning with { Severity = ArtifactRenderDiagnosticSeverity.Information },
            warning with { Message = "Changed warning." }
        ];
        Assert.All(changed, diagnostic => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderPlan.Create(_request, [], [diagnostic], [], [omission], []).WithInventory(inventory)));
    }

    [Fact]
    void should_require_complete_code_coverage_and_the_exact_recorded_code_sequence()
    {
        var (owner, warning, omission) = Omitted();
        var second = warning with { Code = "SECOND", Message = "Second original warning." };
        var witness = omission with { Codes = [warning.Code, second.Code] };
        var complete = owner with { Omission = witness, RefusalReasons = [warning, second] };
        var inventory = ArtifactRenderInventory.Create([complete]);
        var plan = ArtifactRenderPlan.Create(_request, [], [warning, second], [], [witness], []);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.WithInventory(inventory).Outcome);
        Assert.Throws<InvalidArtifactRenderContract>(() => plan.WithInventory(ArtifactRenderInventory.Create([complete with { Omission = witness with { Codes = [second.Code, warning.Code] } }])));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([complete with { RefusalReasons = [warning] }]));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([complete with { Omission = witness with { Codes = [warning.Code] } }]));
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([complete with { Omission = witness with { Codes = [warning.Code, second.Code, "EXTRA"] } }]));
    }

    static (ArtifactRenderConstruct Owner, ArtifactRenderDiagnostic Warning, ArtifactRenderRefusal Omission) Omitted()
    {
        var warning = new ArtifactRenderDiagnostic("OMITTED-SPEC", ArtifactRenderDiagnosticSeverity.Warning, "Original omission warning.", Id(5));
        var omission = new ArtifactRenderRefusal(Id(5), "specification", [warning.Code]);
        var owner = new ArtifactRenderConstruct(Id(5), ArtifactRenderConstructKind.Specification, "model/orders.play", ArtifactRenderOutcome.Refused) { SemanticLocator = Locator(5), Omission = omission, RefusalReasons = [warning] };
        return (owner, warning, omission);
    }
}
