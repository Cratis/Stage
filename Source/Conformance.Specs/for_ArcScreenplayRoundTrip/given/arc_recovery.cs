// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Arc.Screenplay;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Xunit;
using ScreenplayDiagnostic = Cratis.Screenplay.Diagnostics.Diagnostic;
using ScreenplaySeverity = Cratis.Screenplay.Diagnostics.DiagnosticSeverity;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal sealed class arc_recovery
{
    internal Compilation Compilation { get; }
    internal ScreenplayGenerationResult Result { get; }
    internal CompilationResult<ApplicationSyntax> Syntax { get; }
    internal CompilationResult<SemanticCompilation> Bound { get; }
    internal ExecutableSemanticModel Model => Bound.Value.Model;
    internal CommandModel Command => Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
    internal SemanticCommand BoundCommand => Commands(Model).Single();

    internal arc_recovery(params RenderedFile[] files)
    {
        Compilation = RenderedOutput.CreateCompilation(files);
        Result = new ScreenplayGenerator().Generate(Compilation, new() { Domain = "Banking", Module = "Banking", AuthoringOnlyConstructs = true });
        Syntax = new ScreenplayCompiler().Compile(Result.Source);
        Bound = Bind(Result.Source);
    }

    internal static arc_recovery Native(string source) => new(
        new("Banking/Program.cs", "namespace Banking;"),
        new("Banking/Authors/Registration/RegisterAuthor.cs", source));

    internal static arc_recovery Render(string source)
    {
        var bound = Bind(source);
        Assert.True(bound.Success, string.Join(Environment.NewLine, bound.Diagnostics));
        Assert.Empty(bound.Diagnostics);
        var model = bound.Value.Model;
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Banking", "Banking"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        Assert.NotEmpty(files);
        return new([.. files, new("Program.cs", "namespace Banking;")]);
    }

    internal static IEnumerable<SemanticCommand> Commands(ExecutableSemanticModel model) =>
        model.Application.Modules.SelectMany(module => FeatureCommands(module.Features));

    // Rendered root/module namespace segments can become nested features during recovery.
    static IEnumerable<SemanticCommand> FeatureCommands(IEnumerable<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Commands).Concat(FeatureCommands(feature.Features)));

    internal static CompilationResult<SemanticCompilation> Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Banking"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("roundtrip"), "roundtrip", "RoundTrip.play", source);
        return new SemanticModelCompiler().Compile("Banking", SemanticDocumentSet.Create([document], catalog));
    }

    internal void AssertHealthy(bool redundantRoute = false, params string[] diagnostics)
    {
        var errors = Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        if (diagnostics is [ScreenplayDiagnosticCodes.UnrepresentableProductionDestination])
        {
            Assert.Single(Result.Diagnostics, diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination);
        }
        else
        {
            Assert.True(diagnostics.Order(StringComparer.Ordinal).SequenceEqual(Result.Diagnostics.Select(diagnostic => diagnostic.Code).Order(StringComparer.Ordinal)), string.Join(Environment.NewLine, Result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        }
        Assert.True(Syntax.Success, string.Join(Environment.NewLine, Syntax.Diagnostics));
        Assert.DoesNotContain(Syntax.Diagnostics, diagnostic => !AllowedDiagnostic(diagnostic, redundantRoute));
        Assert.True(Bound.Success, string.Join(Environment.NewLine, Bound.Diagnostics));
        Assert.DoesNotContain(Bound.Diagnostics, diagnostic => !AllowedDiagnostic(diagnostic, redundantRoute));
    }

    // The generator always emits domain authoring metadata; it has no ESM behavior.
    static bool AllowedDiagnostic(ScreenplayDiagnostic diagnostic, bool redundantRoute) =>
        (diagnostic.Code == "PLAY0270" && diagnostic.Severity == ScreenplaySeverity.Information) ||
        (redundantRoute && diagnostic.Code == "PLAY0664" && diagnostic.Severity == ScreenplaySeverity.Warning);

    internal void AssertPropertyDestination(int index, string name)
    {
        Assert.Equal(new PropertyPathSource(name), Command.Produces.ElementAt(index).Destination);
        var destination = Assert.IsType<SemanticResolvedExpression>(BoundCommand.Produces[index].Destination);
        Assert.Equal(BoundCommand.Properties.Single(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Id, destination.Target);
    }

    internal void AssertNameMapping(int index)
    {
        var mapping = Assert.Single(Command.Produces.ElementAt(index).Mappings);
        Assert.Equal("Name", mapping.Property);
        Assert.Equal(new PropertyPathSource("Name"), mapping.Source);
    }
}
#endif
