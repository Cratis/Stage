// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_naming_from_a_scoped_selection : given.a_policy_application
{
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _scoped = null!;
    SemanticId _issue = default!;

    void Because()
    {
        Compile(Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Owner"), Slice("VoidInvoice", "Owner", "orderId"))));
        _issue = CommandId("IssueInvoice");
        _application = invoice_model.Plan(_model);
        _scoped = Plan(ArtifactRenderScopeKind.Slice, _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Commands.Any(command => command.Id == _issue)).Id);
    }

    [Fact] void should_match_the_application_scope_names() => Policy(_scoped).ShouldEqual(Policy(_application).Where(path => path.Contains("IssueInvoice", StringComparison.Ordinal)));
    [Fact] void should_render_identical_bytes() => Text(_scoped, "GeneratedPolicies/OwnerForIssueInvoicePolicy.cs").ShouldEqual(Text(_application, "GeneratedPolicies/OwnerForIssueInvoicePolicy.cs"));

    static IEnumerable<string> Policy(ArtifactRenderPlan plan) => plan.Artifacts.Select(artifact => artifact.RelativePath).Where(path => path.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) && path.EndsWith("Policy.cs", StringComparison.Ordinal));
}
#endif
