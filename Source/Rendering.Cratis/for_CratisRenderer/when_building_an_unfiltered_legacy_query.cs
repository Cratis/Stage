// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
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

using context = Cratis.Stage.Rendering.Cratis.for_CratisRenderer.when_building_an_unfiltered_legacy_query.context;
using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

// Retains the original positive DSL and legacy output; the standard SDK resolves reference assemblies instead
// of the in-memory TPA compiler's MongoDB System.Linq.Expressions 6 -> 10 metadata unification warning.
public class when_building_an_unfiltered_legacy_query(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_the_unchanged_legacy_output_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_declared_method_and_its_authorization() => fixture.LegacyFile.Content.ShouldContain("[Authorize]\n    public static IQueryable<InvoiceSummary> ListedInvoices(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");
    [Fact] void should_not_invent_an_all_method() => fixture.LegacyFile.Content.ShouldNotContain("AllInvoiceSummaries");
    [Fact] void should_not_invent_a_by_id_method() => fixture.LegacyFile.Content.ShouldNotContain("InvoiceSummaryById");

    public class context : a_generated_application
    {
        const string LegacySource = """
            policy Readers
              require authenticated
            module Billing
              feature Invoices
                slice StateView Summary
                  event InvoiceRegistered
                    status String
                  readmodel InvoiceSummary
                    status String
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceRegistered
                      status = status
                  query ListedInvoices => InvoiceSummary[]
                    authorize Readers
            """;

        public RenderedFile LegacyFile { get; private set; } = null!;
        public string DebugWarnings { get; private set; } = null!;

        protected override ArtifactRenderPlan CreatePlan()
        {
            // A genuinely empty semantic application supplies only the package-owned scaffold. Do not render
            // the inherited canonical corpus: its semantic query would not exercise this legacy control.
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("LegacyQueryControl"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scaffold"), "scaffold", "Scaffold.play", "module Billing");
            var compilation = new SemanticModelCompiler().Compile("LegacyQueryControl", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            compilation.Diagnostics.ShouldBeEmpty();
            var model = compilation.Value!.Model;
            model.Application.Concepts.ShouldBeEmpty();
            model.Application.Types.ShouldBeEmpty();
            model.Application.Modules.Single().Features.ShouldBeEmpty();
            var execution = SemanticExecutionPlan.Compile(model);
            execution.Success.ShouldBeTrue();
            var profile = CratisRendering.CreateProfile(model.Application.Name, new("LegacyQueryControl", "AppQueryIntentAdmission"));
            var request = new ArtifactRenderRequest(model, execution.Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
            var scaffold = new CratisArtifactRenderPlanner().Plan(request);
            scaffold.Success.ShouldBeTrue();
            scaffold.Diagnostics.ShouldBeEmpty();

            // Equality to every package input hash proves the plan adds no canonical or other semantic artifact.
            Assert.Equal(profile.Inputs.Select(input => input.Sha256).Order(), scaffold.Artifacts.Select(artifact => artifact.Sha256).Order());
            Assert.Equal(["Program.cs"], scaffold.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath));
            scaffold.Artifacts.ShouldContain(artifact => artifact.RelativePath == "LegacyQueryControl.csproj");

            var syntax = new NativeScreenplayCompiler().Compile(LegacySource);
            Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
            syntax.Diagnostics.ShouldBeEmpty();
            var application = syntax.Value!;
            var slice = application.Modules.Single().Features.Single().Slices.Single();
            var query = Assert.Single(slice.Queries);
            query.Filters.ShouldBeEmpty();
            query.Performer.ShouldBeNull();
            query.Name.ShouldEqual("ListedInvoices");
            query.ReturnType.Name.ShouldEqual("InvoiceSummary");
            query.ReturnType.IsCollection.ShouldBeTrue();
            QueryRenderer.IsFullyRendered(query).ShouldBeTrue();

            LegacyFile = RenderLegacy(application).GetAwaiter().GetResult();
            var legacyArtifact = PlannedArtifact.CreateText(LegacyFile.RelativePath, LegacyFile.Content);
            Assert.Equal(Encoding.UTF8.GetBytes(LegacyFile.Content), legacyArtifact.Bytes.ToArray());
            return ArtifactRenderPlan.Create(request, [.. scaffold.Artifacts, legacyArtifact], scaffold.Diagnostics);
        }

        static async Task<RenderedFile> RenderLegacy(ApplicationSyntax application)
        {
            var codeOutput = new InMemoryCodeOutput();
            IRenderer renderer = new CratisRenderer(
                new a_stub_scaffolder(),
                new Dictionary<SliceType, ISliceRenderer> { [SliceType.StateView] = new StateViewSliceRenderer() },
                codeOutput);
            await using var output = new StringWriter();
            await using var error = new StringWriter();
            await renderer.Render([application], new DirectoryInfo(Path.Combine(Path.GetTempPath(), "app-query-intent-admission")), output, error);
            var file = Assert.Single(codeOutput.Files);
            file.RelativePath.ShouldEqual(Path.Combine("Billing", "Invoices", "Summary", "Summary.cs"));
            file.Diagnostics.ShouldBeEmpty();
            error.ToString().ShouldBeEmpty();
            output.ToString().ShouldContain("Rendering complete.");
            codeOutput.FailureMarkerWasWritten.ShouldBeFalse();

            return file;
        }

        async Task Because()
        {
            try
            {
                await Run("legacy-query-restore.log", "restore", "LegacyQueryControl.csproj", "-p:Configuration=Debug", "--nologo", "-warnaserror", "-p:TreatWarningsAsErrors=true", "-p:CodeAnalysisTreatWarningsAsErrors=true", "-p:MSBuildTreatWarningsAsErrors=true", "-p:CratisProxiesOutputPath=");
                using var compileItems = JsonDocument.Parse(await Run("legacy-query-compile-items.log", "msbuild", "LegacyQueryControl.csproj", "-getItem:Compile", "--nologo", "-p:Configuration=Debug", "-p:CratisProxiesOutputPath="));
                var compiledPaths = compileItems.RootElement.GetProperty("Items").GetProperty("Compile").EnumerateArray().Select(item => item.GetProperty("Identity").GetString()).ToArray();
                compiledPaths.ShouldContain(LegacyFile.RelativePath);
                DebugWarnings = BuildWarnings(await Run("legacy-query-debug-build.log", "build", "LegacyQueryControl.csproj", "-c", "Debug", "--no-restore", "--nologo", "-warnaserror", "-p:TreatWarningsAsErrors=true", "-p:CodeAnalysisTreatWarningsAsErrors=true", "-p:MSBuildTreatWarningsAsErrors=true", "-p:CratisProxiesOutputPath="));
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
