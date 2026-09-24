// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs.given;

public class an_invoice_model : Specification
{
    protected const string FirstExpectation = "then InvoiceIssued\n          description = \"First payload\"";

    protected ExecutableSemanticModel _model = null!;
    protected ArtifactRenderPlan _plan = null!;

    protected static string Invoices => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);

    protected void Plan(string source)
    {
        _model = invoice_model.Compile(source);
        _plan = invoice_model.Plan(_model);
    }

    protected string Artifact(string fileName) =>
        Encoding.UTF8.GetString(_plan.Artifacts.Single(_ => _.RelativePath.EndsWith(fileName, StringComparison.Ordinal)).Bytes.AsSpan());

    protected IEnumerable<string> ErrorCodes =>
        _plan.Diagnostics.Where(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(_ => _.Code);
}
