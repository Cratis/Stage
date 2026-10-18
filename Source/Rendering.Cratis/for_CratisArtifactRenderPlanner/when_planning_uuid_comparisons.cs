// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_uuid_comparisons : Specification
{
    const string Source = """
        concept MemberId : Uuid
        module Access
          feature Approvals
            slice StateChange Approve
              command ApproveRequest
                id String identifier
                requestedBy MemberId
                approvedBy MemberId
                raw Uuid
                validate
                  require approvedBy != requestedBy
                    message "Approver must differ from requester."
                  require raw == approvedBy
                    message "The values must match."
                  require requestedBy != "a1111111-1111-4111-8111-111111111111"
                    message "The watched member may not request."
                produces when approvedBy == "b2222222-2222-4222-8222-222222222222"
                  RequestApproved
                    for id
              event RequestApproved
        """;

    ArtifactRenderPlan _plan = null!;
    string _code = string.Empty;

    void Because()
    {
        var model = invoice_model.Compile(Source);
        _plan = invoice_model.Plan(model);
        _code = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Approve.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_render_a_parsed_guid_literal() => _code.ShouldContain("!global::System.Object.Equals(command.RequestedBy.Value, global::System.Guid.Parse(\"a1111111-1111-4111-8111-111111111111\"))");
    [Fact] void should_render_a_parsed_guid_literal_in_a_production_guard() => _code.ShouldContain("global::System.Object.Equals(ApprovedBy.Value, global::System.Guid.Parse(\"b2222222-2222-4222-8222-222222222222\"))");
    [Fact] void should_compare_concept_values() => _code.ShouldContain("!global::System.Object.Equals(command.ApprovedBy.Value, command.RequestedBy.Value)");
    [Fact] void should_compare_a_bare_uuid_with_a_concept_value() => _code.ShouldContain("global::System.Object.Equals(command.Raw, command.ApprovedBy.Value)");
    [Fact] void should_not_compare_a_guid_with_text() => _code.ShouldNotContain("command.RequestedBy.Value, \"");
    [Fact] void should_report_no_diagnostics() => _plan.Diagnostics.ShouldBeEmpty();
}
