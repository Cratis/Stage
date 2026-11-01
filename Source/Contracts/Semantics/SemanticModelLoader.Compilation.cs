// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Semantics;

public static partial class SemanticModelLoader
{
    internal static SemanticModelLoadResult FinishCompilation(CompilationResult<SemanticCompilation> compiled, ImmutableArray<Diagnostic> attachmentDiagnostics, ReleaseBaselineAssessment baseline, CancellationToken cancellationToken, string? root = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = compiled.Diagnostics.Select(diagnostic => new SemanticModelLoadDiagnostic(diagnostic.Code, diagnostic.Message, $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column})") { Severity = diagnostic.Severity }).ToImmutableArray();
        if (!compiled.Success)
        {
            return new(null, [new("STAGE-PLAN-003", "Screenplay compilation failed."), .. diagnostics]);
        }
        var model = compiled.Value!.Model;

        // Slice-local issues remain attached to the plan so each execution surface can refuse
        // the affected artifact without hiding supported siblings. Structural issues still block.
        var plan = SemanticExecutionPlan.Compile(model, []);
        if (!plan.Success)
        {
            return new(null, [.. diagnostics, .. plan.Issues.Select(issue => new SemanticModelLoadDiagnostic("STAGE-PLAN-004", $"{issue.Artifact}: {issue.Details}"))]);
        }

        diagnostics = [.. diagnostics, .. LocalPlanDiagnostics(plan)];
        var loaded = new LoadedSemanticModel(model, plan.Plan!)
        {
            SpecificationOrigins = compiled.Value!.SpecificationOrigins,
            SemanticIdentities = compiled.Value.Documents.IdentityCatalog.Semantics,
            IdentityCatalog = compiled.Value.Documents.IdentityCatalog,
            ImplementationRequirements = compiled.ImplementationRequirements,
            TypedContextDescriptors = compiled.TypedContextDescriptors,
            ImplementationContents = SemanticImplementationBodies.Resolve(compiled.Value!.Documents, compiled.ImplementationRequirements),
            AttachmentDiagnostics = attachmentDiagnostics,
            ReleaseBaseline = baseline,
            Realization = ApplicationRealization.From(compiled.Value!, root),
            Diagnostics = diagnostics
        };

        return new(loaded, diagnostics);
    }

    static IEnumerable<SemanticModelLoadDiagnostic> LocalPlanDiagnostics(SemanticExecutionPlanCompilation compilation) =>
        compilation.UnselectedIssues.Select(issue => new SemanticModelLoadDiagnostic("STAGE-PLAN-004", $"{issue.Artifact}: {issue.Details}") { Severity = DiagnosticSeverity.Warning });
}
