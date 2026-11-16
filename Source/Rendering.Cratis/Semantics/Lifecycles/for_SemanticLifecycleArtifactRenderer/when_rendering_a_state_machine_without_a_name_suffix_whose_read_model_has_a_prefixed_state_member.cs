// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles.for_SemanticLifecycleArtifactRenderer;

public class when_rendering_a_state_machine_without_a_name_suffix_whose_read_model_has_a_prefixed_state_member : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_lifecycle_source.RenderableSource
        .Replace("InvoiceLifecycle", "InvoiceFlow", StringComparison.Ordinal)
        .Replace("status", "invoiceFlowStatus", StringComparison.Ordinal)));

    string Text(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    [Fact] void should_plan_the_application() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}")));
    [Fact] void should_resolve_the_state_from_the_read_model() => Text("Lifecycles/InvoiceFlow.cs").ShouldContain("typeof(Paid)");
    [Fact] void should_not_report_the_restart_state() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == LifecycleCodes.RestartState).ShouldBeFalse();
}
