// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_primitive_uuid_projection_identity : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = when_rendering_portable_authorization.Source.Replace("invoiceId InvoiceId", "invoiceId Uuid", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    // A key that is not the event source identity is rendered as an explicit key of the scope, so no concept-backed identity is needed.
    [Fact] void should_render_the_projection_keyed_by_its_payload_property() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-009");
    [Fact] void should_admit_primitive_uuid_authorization() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-015");
    [Fact] void should_plan_the_application() => _plan.Diagnostics.Select(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error ? diagnostic.Code : string.Empty).Where(code => code.Length > 0).ShouldBeEmpty();
}
