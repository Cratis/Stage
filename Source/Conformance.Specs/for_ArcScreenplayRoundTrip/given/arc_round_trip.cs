// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Text;
using Cratis.Arc.Screenplay;
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

// The Arc round trip: render the fixture, compile the rendered application, let the Arc adapter recover a Screenplay source from it,
// recompile that source, and compare the recovered model with the original on the canonical semantic model.
internal static class arc_round_trip
{
    public static RoundTripObservation Run(round_trip_fixture fixture)
    {
        var plan = fixture.Plan;
        Assert.True(plan.Success, $"{fixture.Name} did not plan:{Environment.NewLine}{string.Join(Environment.NewLine, plan.Diagnostics)}");
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var compilation = ClientOnly(RenderedOutput.CreateCompilation(files));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, $"{fixture.Name} rendered a compilation with errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Take(5))}");

        var application = fixture.Model.Application;
        var module = application.Modules.FirstOrDefault()?.Name ?? application.Name;
        var generated = new ScreenplayGenerator().Generate(compilation, new() { Domain = module, Module = module, AuthoringOnlyConstructs = true, SegmentsToSkip = 1 });
        foreach (var diagnostic in generated.Diagnostics)
        {
            Console.WriteLine($"{fixture.Name}: {diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}");
        }

        var recovered = Recover(application.Name, generated.Source);

        return new(
            fixture.Name,
            recovered.Model is null ? null : SemanticModelEquivalence.Compare(fixture.Model, recovered.Model),
            [.. plan.Diagnostics.Select(Render), .. generated.Diagnostics.Select(Adapter)],
            recovered.Errors);
    }

    // The test host also loads the kernel, which declares client SDK types such as IEventSequence in the same namespaces as the client;
    // aliasing the kernel, as RenderedOutput.Load does for clientOnly, leaves only the client SDK visible to the rendered application.
    static CSharpCompilation ClientOnly(CSharpCompilation compilation) => compilation.WithReferences(compilation.References.Select(reference => Path.GetFileName(reference.Display) == "Cratis.Chronicle.Core.dll"
        ? reference.WithAliases(["ChronicleKernel"])
        : reference));

    static (ExecutableSemanticModel? Model, ImmutableArray<string> Errors) Recover(string applicationName, string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(applicationName));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("roundtrip"), "roundtrip", "RoundTrip.play", source);
        var compilation = new SemanticModelCompiler().Compile(applicationName, SemanticDocumentSet.Create([document], catalog));

        return compilation.Success ? (compilation.Value!.Model, []) : (null, [.. compilation.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal)]);
    }

    static RoundTripDiagnostic Render(ArtifactRenderDiagnostic diagnostic) => new(RoundTripDirection.Render, diagnostic.Code, diagnostic.Severity switch
    {
        ArtifactRenderDiagnosticSeverity.Information => RoundTripDiagnosticSeverity.Information,
        ArtifactRenderDiagnosticSeverity.Warning => RoundTripDiagnosticSeverity.Warning,
        _ => RoundTripDiagnosticSeverity.Error
    });

    static RoundTripDiagnostic Adapter(ScreenplayDiagnostic diagnostic) => new(RoundTripDirection.Adapter, diagnostic.Code, diagnostic.Severity switch
    {
        ScreenplayDiagnosticSeverity.Information => RoundTripDiagnosticSeverity.Information,
        ScreenplayDiagnosticSeverity.Warning => RoundTripDiagnosticSeverity.Warning,
        _ => RoundTripDiagnosticSeverity.Error
    });
}
#endif
