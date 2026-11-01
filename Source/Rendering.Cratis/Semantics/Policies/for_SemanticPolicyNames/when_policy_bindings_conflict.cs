// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_policy_bindings_conflict : given.a_policy_application
{
    SemanticApplicationContext _context = null!;
    IReadOnlyDictionary<SemanticId, PolicyName> _names = null!;
    ImmutableArray<ConflictingPolicyBindings> _conflicts;

    void Establish()
    {
        _model = CompileModel();
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        _context = new(new(_model, SemanticExecutionPlan.Compile(_model).Plan, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id)), options);
    }

    void Because()
    {
        _names = SemanticPolicyNames.For(_context);
        _conflicts = SemanticPolicyNames.Conflicts(_context);
    }

    [Fact] void should_name_every_protected_operation_without_throwing() => _names.Keys.ShouldContainOnly(SemanticPolicyNames.Operations(_context.AllSlices, _context.Application.Policies).Select(operation => operation.Id));
    [Fact] void should_keep_the_deterministic_qualified_name() => _names[CommandId("IssueInvoice")].ClassName.ShouldEqual("OwnerForBillingInvoicingIssueInvoiceIssueInvoiceCommandPolicy");
    [Fact] void should_keep_the_same_name_in_a_fresh_context() => ClassOf("IssueInvoice").ShouldEqual(_names[CommandId("IssueInvoice")].ClassName);
    [Fact] void should_identify_both_conflicting_operations() => _conflicts.Single().Operations.ShouldContainOnly([CommandId("IssueInvoice"), CommandId("Block4")]);
    [Fact] void should_preserve_the_conflict_message() => _conflicts.Single().Message.ShouldEqual($"Operations {string.Join(", ", _conflicts.Single().Operations)} bind the policy 'OwnerForBillingInvoicingIssueInvoiceIssueInvoiceCommand' differently and cannot be given distinct policy class names.");

    internal static ExecutableSemanticModel CompileModel() => invoice_model.Compile(Source());

    internal static string Source()
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

        const string unprotected = """
            slice StateChange RecordInvoice
              command RecordInvoice
                invoiceId InvoiceId identifier
                produces InvoiceRecorded
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceRecorded
                invoiceId InvoiceId
        """;
        return Policies + "\n" + policies + "\n" + Module("Billing",
            Feature("Invoicing", Slice("IssueInvoice", "Owner"), Slice("VoidInvoice", "Owner", "orderId")),
            Feature("Blocking", blockers),
            Feature("Unprotected", unprotected));
    }
}
#endif
