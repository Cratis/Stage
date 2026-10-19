// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_shared_slice_response_collision : Specification
{
    ArtifactRenderPlan _plan = null!;
    string Slice => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Billing/Invoicing/Issue/Issue.cs").Bytes.AsSpan());

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_several_commands.Source.Replace("ReviseInvoice", "IssueInvoiceResponse", StringComparison.Ordinal)));

    [Fact] void should_render_the_collision() => Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
    [Fact] void should_keep_the_command_name() => Slice.ShouldContain("record IssueInvoiceResponse(");
    [Fact] void should_escape_the_colliding_response_record() => Slice.ShouldContain("record IssueInvoiceResponse_(");
    [Fact] void should_return_the_escaped_response_record() => Slice.ShouldContain("new IssueInvoiceResponse_(");
    [Fact] void should_keep_the_response_of_the_colliding_command() => Slice.ShouldContain("record IssueInvoiceResponseResponse(");
    [Fact] void should_report_the_escaped_name() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-050" && diagnostic.Message.Contains("'IssueInvoiceResponse_'", StringComparison.Ordinal));
}
