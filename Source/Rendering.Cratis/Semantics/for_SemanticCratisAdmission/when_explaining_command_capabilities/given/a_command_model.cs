// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities.given;

public class a_command_model : Specification
{
    protected const string Source = """
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                streamReference String identifier
                description String
                produces InvoiceIssued
                  for streamReference
                  description = description
              event InvoiceIssued
                description String
        """;

    protected ExecutableSemanticModel _model = null!;
    protected SemanticSlice _slice = null!;
    protected SemanticCommand _command = null!;
    protected ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Establish() => SetModel(Source);

    protected void SetModel(string source)
    {
        _model = invoice_model.Compile(source);
        _slice = _model.Application.Modules.Single().Features.Single().Slices.Single();
        _command = _slice.Commands.Single();
    }

    protected void Evaluate()
    {
        // Test the capability predicates directly: malformed references cannot enter an executable model.
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        var request = new ArtifactRenderRequest(_model, SemanticExecutionPlan.Compile(_model).Plan!, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticCratisAdmission.ValidateCommandCapabilities(context, _slice, _command, diagnostics);
        _diagnostics = [.. diagnostics];
    }

    protected IEnumerable<string> Messages => _diagnostics.Select(diagnostic => diagnostic.Message);
}
