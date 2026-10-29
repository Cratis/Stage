// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_unauthenticated_caller_for_an_unauthorized_command : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("      specification IssuingFirstInvoice\n", "      specification IssuingFirstInvoice\n        given caller\n          claim \"department\" = \"Sales\"\n", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_refuse_the_specification() => _plan.Diagnostics.ShouldContain(diagnostic =>
        diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning &&
        diagnostic.Message.Contains("no authorize clause", StringComparison.Ordinal));
    [Fact] void should_explain_how_to_keep_the_endpoint_open() => _plan.Diagnostics.ShouldContain(diagnostic =>
        diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("policy Anyone with require anonymous and authorize Anyone (ESM v31)", StringComparison.Ordinal));
    [Fact] void should_not_emit_the_refused_specification() => _plan.Artifacts.ShouldNotContain(artifact =>
        artifact.RelativePath.EndsWith("/when_issuing_first_invoice.cs", StringComparison.Ordinal));
    [Fact] void should_still_render_the_supported_command() => _plan.Artifacts.ShouldContain(artifact =>
        artifact.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal));
}
