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
    protected const string RoutedSource = """
        concept AccountId : Uuid
        eventsource Account
          identifier AccountId
          stream Transactions
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                amount Decimal
                stream Account.Transactions
                produces event Deposited
                  amount Decimal = amount
        """;

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

    protected void AssertSourceAndStreamsRecovered()
    {
        var expected = _model.Application.EventSources.Single();
        var actual = _recovered.Application.EventSources.Single();
        actual.Name.ShouldEqual(expected.Name);
        actual.SourceKind.ShouldEqual(expected.SourceKind);
        actual.Streams.Select(stream => (stream.Name, stream.StreamKind)).ShouldEqual(expected.Streams.Select(stream => (stream.Name, stream.StreamKind)));
    }

    protected static string StoredNames(string sourceId, string streamId) => RoutedSource
        .Replace("eventsource Account\n", $"eventsource Account\n  id \"{sourceId}\"\n", StringComparison.Ordinal)
        .Replace("  stream Transactions\n", $"  stream Transactions\n    id \"{streamId}\"\n", StringComparison.Ordinal);

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Banking"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("roundtrip"), "roundtrip", "RoundTrip.play", source);
        var compilation = new SemanticModelCompiler().Compile("Banking", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return compilation.Value!.Model;
    }
}
#endif
