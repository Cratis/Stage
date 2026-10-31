// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_retaining_reducer_compilations
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_the_renderer_symbols_and_context_reads(bool self)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source));
        var options = new CratisRenderingOptions("Projects", "Projects");
        var profile = self ? CratisRendering.CreateNewApplicationProfile("Projects", options) : CratisRendering.CreateProfile("Projects", options);
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, options);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var reducer = Assert.Single(context.Reducers);
        var (_, body, refusal) = BodyUnitAdmission.AdmitReducer(context, requirement);
        Assert.Null(refusal);
        var transition = Assert.Single(reducer.Transitions);
        var original = PureTransitionAdmission.Analyze(body, context, context.ReadModels[reducer.ReadModel], context.Events[transition.EventContract], descriptor, requirement);
        Assert.True(original.Accepted, original.Reason);
        var unit = BodyUnitSources.RenderReducer(context, descriptor, body);
        var trees = unit.Sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), index == 0 ? unit.Path : $"worker/declarations/{index}.cs"));
        var compilation = CSharpCompilation.Create("WorkerParity", trees, [.. PureTransitionAdmission.AnalysisReferences(), SemanticGenerationBodyAdmission.FundamentalsReference, SemanticGenerationBodyAdmission.ChronicleReference], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        var final = PureTransitionAdmission.AnalyzeRendered(compilation, unit.Path, descriptor, context);
        Assert.True(final.Accepted, final.Reason);
        Assert.Equal(original.BoundSymbols.ToArray(), final.BoundSymbols.ToArray());
        Assert.Equal(original.ContextReads, final.ContextReads);
        await using var pe = new MemoryStream();
        var emitted = compilation.Emit(pe);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
    }
}
#endif
