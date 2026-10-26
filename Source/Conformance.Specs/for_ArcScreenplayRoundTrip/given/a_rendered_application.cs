// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Arc.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

public abstract class a_rendered_application : Specification
{
    protected const string RoutedSource = round_trip_fixtures.RoutedSource;

    protected ExecutableSemanticModel _model = null!;
    protected Compilation _compilation = null!;
    protected ScreenplayGenerationResult _result = null!;
    protected ExecutableSemanticModel _recovered = null!;
    protected abstract string Source { get; }

    void Establish()
    {
        _model = Compile(Source);
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Banking", "Banking"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        _compilation = RenderedOutput.CreateCompilation(files);
        Assert.Empty(_compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    protected void Generate()
    {
        _result = new ScreenplayGenerator().Generate(_compilation, new() { Domain = "Banking", Module = "Banking", AuthoringOnlyConstructs = true });
        _recovered = Compile(_result.Source);
    }

    protected void AssertSourceDiagnosticsAbsent()
    {
        string[] codes =
        [
            ScreenplayDiagnosticCodes.EventSourceNotRepresentable,
            ScreenplayDiagnosticCodes.EventStreamNotDeclared,
            ScreenplayDiagnosticCodes.EventStreamIdConcurrencyNotRepresentable,
            ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable,
            ScreenplayDiagnosticCodes.ObserverEventStreamNotDeclared,
            ScreenplayDiagnosticCodes.UnreadableCommandRoute,
            ScreenplayDiagnosticCodes.SourceDidNotCompile,
            ScreenplayDiagnosticCodes.DocumentDidNotCompile,
            ScreenplayDiagnosticCodes.DocumentDidNotBind
        ];
        _result.Diagnostics.Where(diagnostic => codes.Contains(diagnostic.Code, StringComparer.Ordinal)).ShouldBeEmpty();
    }

    protected static void AssertRoundTripEquivalent(string fixture)
    {
        var verdict = arc_round_trip_ledger.Load().Reconcile([arc_round_trip.Run(round_trip_fixtures.Inline(fixture))], requireCompleteRun: false);
        Assert.True(verdict.IsClean, arc_round_trip_ledger.Describe(verdict.Findings));
    }

    protected static string StoredNames(string sourceId, string streamId) => round_trip_fixtures.StoredNames(sourceId, streamId);

    static ExecutableSemanticModel Compile(string source) => round_trip_fixtures.Compile(source);
}
#endif
