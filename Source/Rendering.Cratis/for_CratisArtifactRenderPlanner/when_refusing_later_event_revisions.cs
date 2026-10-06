// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_later_event_revisions : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var declarations = string.Join('\n', Enumerable.Range(1, 3).Select(revision =>
            $"      event InvoiceIssued generation {revision}\n        description String"));
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("      event InvoiceIssued\n        description String", declarations, StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_refuse_every_non_initial_revision() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
    [Fact] void should_name_revision_three() => _plan.Diagnostics.Single().Message.ShouldEqual("Event 'InvoiceIssued' has evolved to revision 3; Stage does not render event migrations yet.");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
