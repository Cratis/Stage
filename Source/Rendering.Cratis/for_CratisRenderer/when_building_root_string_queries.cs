// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisRenderer.when_building_root_string_queries.context;
using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

// SDK reference assemblies, unchanged package scaffold and unchanged legacy output: no TPA warning suppression.
public class when_building_root_string_queries(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_the_fixed_and_declared_queries_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    /// <summary>
    /// A raw key is declared as the event source id, not as the string it is stored as.
    /// </summary>
    /// <remarks>
    /// Arc coerces a query argument to the declared parameter type before validation runs, so a parameter
    /// declared as a raw string reaches the lookup unvalidated while a conversion in the body produces the type
    /// the query wanted anyway. Arc's own analyser rejects the old shape (ARC0015), and a generated file cannot
    /// be corrected by the person who has to build it.
    /// </remarks>
    [Fact] void should_declare_the_fixed_lookup_key_as_an_event_source_id() => fixture.Files.Single(file => file.RelativePath.Contains("Fixed", StringComparison.Ordinal)).Content.ShouldContain("FixedOrderById(IReadModels readModels, EventSourceId id)");
    [Fact] void should_preserve_collection_authorization() => fixture.Files.Single(file => file.RelativePath.Contains("Declared", StringComparison.Ordinal)).Content.ShouldContain("[Authorize]\n    public static IQueryable<DeclaredOrder> AllOrders");
    [Fact] void should_preserve_the_live_string_lookup() => fixture.Files.Single(file => file.RelativePath.Contains("Declared", StringComparison.Ordinal)).Content.ShouldContain("LiveOrder(IMongoCollection<DeclaredOrder> collection, string lookup)");

    public class context : a_generated_application
    {
        public IReadOnlyList<RenderedFile> Files { get; private set; } = [];
        public string DebugWarnings { get; private set; } = null!;

        protected override ArtifactRenderPlan CreatePlan()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RootStringControl"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scaffold"), "scaffold", "Scaffold.play", "module Sales");
            var compilation = new SemanticModelCompiler().Compile("RootStringControl", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            compilation.Diagnostics.ShouldBeEmpty();
            var model = compilation.Value!.Model;
            model.Application.Concepts.ShouldBeEmpty();
            model.Application.Types.ShouldBeEmpty();
            model.Application.Modules.Single().Features.ShouldBeEmpty();
            var execution = SemanticExecutionPlan.Compile(model);
            execution.Success.ShouldBeTrue();
            var profile = CratisRendering.CreateProfile(model.Application.Name, new("RootStringControl", "RootStringApp"));
            var request = new ArtifactRenderRequest(model, execution.Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
            var scaffold = new CratisArtifactRenderPlanner().Plan(request);
            scaffold.Success.ShouldBeTrue();
            scaffold.Diagnostics.ShouldBeEmpty();
            Assert.Equal(profile.Inputs.Select(input => input.Sha256).Order(), scaffold.Artifacts.Select(artifact => artifact.Sha256).Order());
            Assert.Equal(["Program.cs"], scaffold.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath));

            var syntax = new NativeScreenplayCompiler().Compile("""
                policy Readers
                  require authenticated
                module Sales
                  feature Orders
                    slice StateView Fixed
                      event OrderCreated
                        total Decimal
                      projection FixedProjection => FixedOrder
                        from OrderCreated
                          key literal " ._/:*+- blåbær_日本語 "
                          total = total
                    slice StateView Declared
                      event OrderUpdated
                        total Decimal
                      projection DeclaredProjection => DeclaredOrder
                        from OrderUpdated
                          key literal "   "
                          total = total
                      query AllOrders => DeclaredOrder[]
                        authorize Readers
                      query OrderById => DeclaredOrder
                        by lookup String
                      query LiveOrder => observable DeclaredOrder
                        by lookup String
                """);
            Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
            syntax.Diagnostics.ShouldBeEmpty();
            Files = RenderLegacy(syntax.Value!).GetAwaiter().GetResult();
            return ArtifactRenderPlan.Create(request, [.. scaffold.Artifacts, .. Files.Select(file => PlannedArtifact.CreateText(file.RelativePath, file.Content))], scaffold.Diagnostics);
        }

        static async Task<IReadOnlyList<RenderedFile>> RenderLegacy(ApplicationSyntax application)
        {
            var codeOutput = new InMemoryCodeOutput();
            IRenderer renderer = new CratisRenderer(new a_stub_scaffolder(), new Dictionary<SliceType, ISliceRenderer> { [SliceType.StateView] = new StateViewSliceRenderer() }, codeOutput);
            await using var output = new StringWriter();
            await using var error = new StringWriter();
            await renderer.Render([application], new DirectoryInfo(Path.Combine(Path.GetTempPath(), "root-string-control")), output, error);
            codeOutput.Files.Count.ShouldEqual(2);
            codeOutput.Files.SelectMany(file => file.Diagnostics).ShouldBeEmpty();
            error.ToString().ShouldBeEmpty();
            codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
            return [.. codeOutput.Files];
        }

        async Task Because()
        {
            try
            {
                await Run("root-string-restore.log", "restore", "RootStringControl.csproj", "-p:Configuration=Debug", "--nologo", "-warnaserror", "-p:TreatWarningsAsErrors=true", "-p:CodeAnalysisTreatWarningsAsErrors=true", "-p:MSBuildTreatWarningsAsErrors=true", "-p:CratisProxiesOutputPath=");
                using var items = JsonDocument.Parse(await Run("root-string-compile-items.log", "msbuild", "RootStringControl.csproj", "-getItem:Compile", "--nologo", "-p:Configuration=Debug", "-p:CratisProxiesOutputPath="));
                var compiled = items.RootElement.GetProperty("Items").GetProperty("Compile").EnumerateArray().Select(item => item.GetProperty("Identity").GetString()).ToArray();
                foreach (var file in Files)
                {
                    compiled.ShouldContain(file.RelativePath);
                }

                DebugWarnings = BuildWarnings(await Run("root-string-debug-build.log", "build", "RootStringControl.csproj", "-c", "Debug", "--no-restore", "--nologo", "-warnaserror", "-p:TreatWarningsAsErrors=true", "-p:CodeAnalysisTreatWarningsAsErrors=true", "-p:MSBuildTreatWarningsAsErrors=true", "-p:CratisProxiesOutputPath="));
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
