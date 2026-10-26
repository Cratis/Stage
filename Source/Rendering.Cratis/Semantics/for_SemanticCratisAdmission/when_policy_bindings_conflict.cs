// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_policy_bindings_conflict : Specification
{
    SemanticApplicationContext _context = null!;
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;
    SemanticId[] _operations = [];

    void Establish()
    {
        var model = Policies.for_SemanticPolicyNames.when_policy_bindings_conflict.CompileModel();
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        _context = new(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
        _operations = [.. _context.AllSlices.SelectMany(located => located.Slice.Commands).Where(command => command.Name == "IssueInvoice" || command.Name == "Block4").Select(command => command.Id)];
    }

    void Because() => _diagnostics = SemanticCratisAdmission.Evaluate(_context, _context.AllSlices);

    [Fact] void should_report_the_conflict_for_each_operation() => _diagnostics.Where(diagnostic => diagnostic.Code == ConflictingPolicyBindings.Code).Select(diagnostic => diagnostic.Artifact).ShouldContainOnly(_operations);
    [Fact] void should_refuse_the_conflict_as_errors() => _diagnostics.Where(diagnostic => diagnostic.Code == ConflictingPolicyBindings.Code).All(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_preserve_the_policy_name_in_the_diagnostic() => _diagnostics.First(diagnostic => diagnostic.Code == ConflictingPolicyBindings.Code).Message.ShouldContain("bind the policy 'OwnerForBillingInvoicingIssueInvoiceIssueInvoiceCommand' differently");
}
#endif
