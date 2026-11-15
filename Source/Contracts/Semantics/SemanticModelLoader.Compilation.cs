// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Ui;

namespace Cratis.Stage.Contracts.Semantics;

public static partial class SemanticModelLoader
{
    /// <summary>
    /// The diagnostic code of a free screen a slice adopts that the executable model cannot serve.
    /// </summary>
    internal const string FreeScreenRefusalCode = "STAGE-SCENE-FREE-001";

    internal static SemanticModelLoadResult FinishCompilation(CompilationResult<SemanticCompilation> compiled, ImmutableArray<Diagnostic> attachmentDiagnostics, ReleaseBaselineAssessment baseline, CancellationToken cancellationToken, string? root = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = compiled.Diagnostics.Select(diagnostic => new SemanticModelLoadDiagnostic(diagnostic.Code, diagnostic.Message, $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column})") { Severity = diagnostic.Severity }).ToImmutableArray();
        if (!compiled.Success)
        {
            return new(null, [new("STAGE-PLAN-003", "Screenplay compilation failed."), .. diagnostics]);
        }
        var model = compiled.Value!.Model;

        // A free screen a slice adopts is rendered from the source, so it has to be one the executable model can serve (Cratis/Stage#1150).
        var screenRefusals = FreeScreenRefusals(compiled.Value!);
        if (screenRefusals.Length > 0)
        {
            return new(null, [.. diagnostics, .. screenRefusals.Select(refusal => new SemanticModelLoadDiagnostic(FreeScreenRefusalCode, refusal))]);
        }

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
            PassiveReadModels = compiled.Value!.PassiveReadModels,
            Diagnostics = diagnostics
        };

        return new(loaded, diagnostics);
    }

    /// <summary>
    /// Asks Screenplay's executable screen admission about the free screens the sources adopt, so that a need nobody provides is refused
    /// with the reason admission gives rather than rendered as a screen without data.
    /// </summary>
    /// <param name="compilation">The compiled model and the sources it came from.</param>
    /// <returns>Every reason admission refuses a screen, or none when no slice adopts a free screen or all of them are served.</returns>
    static ImmutableArray<string> FreeScreenRefusals(SemanticCompilation compilation)
    {
        // Only a model whose slices adopt a screen pays for the second parse.
        if (!compilation.Documents.Documents.Any(document => document.Text.Contains("uses screen", StringComparison.Ordinal)))
        {
            return [];
        }

        var texts = compilation.Documents.Documents.ToDictionary(document => document.DisplayPath, document => document.Text, StringComparer.Ordinal);
        var syntax = new PlayFileCompiler(new SourceTextFiles("free-screen-admission", texts), new ScreenplayCompiler()).CompileFolder("free-screen-admission").Result;
        if (!syntax.Success || FreeScreenConverter.Adoptions(syntax.Value!).Count == 0)
        {
            return [];
        }

        return ExecutableScreens.Admit(syntax.Value!, compilation).Refusals;
    }

    static IEnumerable<SemanticModelLoadDiagnostic> LocalPlanDiagnostics(SemanticExecutionPlanCompilation compilation) =>
        compilation.UnselectedIssues.Select(issue => new SemanticModelLoadDiagnostic("STAGE-PLAN-004", $"{issue.Artifact}: {issue.Details}") { Severity = DiagnosticSeverity.Warning });
}
