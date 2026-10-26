// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_policy_bindings_conflict : given.a_policy_application
{
    ConflictingPolicyBindings _exception = null!;

    void Establish() => _model = CompileModel();

    void Because() => _exception = Assert.Throws<ConflictingPolicyBindings>(() => ClassOf("IssueInvoice"));

    [Fact] void should_report_the_authorization_conflict_code() => ConflictingPolicyBindings.Code.ShouldEqual("STAGE-AUTH-002");
    [Fact] void should_identify_both_conflicting_operations() => _exception.Operations.ShouldContainOnly([CommandId("IssueInvoice"), CommandId("Block4")]);
    [Fact] void should_preserve_the_conflict_message() => _exception.Message.ShouldEqual($"Operations {string.Join(", ", _exception.Operations)} bind the policy 'OwnerForBillingInvoicingIssueInvoiceIssueInvoiceCommand' differently and cannot be given distinct policy class names.");

    internal static ExecutableSemanticModel CompileModel()
    {
        string[] candidates =
        [
            "OwnerForIssueInvoice",
            "OwnerForInvoicingIssueInvoice",
            "OwnerForBillingInvoicingIssueInvoice",
            "OwnerForBillingInvoicingIssueInvoiceIssueInvoice",
            "OwnerForBillingInvoicingIssueInvoiceIssueInvoiceCommand"
        ];
        var policies = string.Join('\n', candidates.Select(name => $"policy {name}\n  require role \"Staff\""));
        var blockers = candidates.Select((name, index) => Slice($"Block{index}", name)).ToArray();

        return invoice_model.Compile(Policies + "\n" + policies + "\n" + Module("Billing",
            Feature("Invoicing", Slice("IssueInvoice", "Owner"), Slice("VoidInvoice", "Owner", "orderId")),
            Feature("Blocking", blockers)));
    }
}
#endif
