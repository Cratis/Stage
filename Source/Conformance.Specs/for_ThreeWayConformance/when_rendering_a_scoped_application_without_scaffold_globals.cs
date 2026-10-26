// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_scoped_application_without_scaffold_globals
{
    const string Source = """
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                key String
                name ProjectName
              query ProjectById => ProjectSummary?
                by key String
              query AllProjects => observable ProjectSummary[]
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  name = name
              specification ListingProjects
                given ProjectRegistered
                  for "project-1"
                  name = "hello"
                then query AllProjects
                  result
                    key = "project-1"
                    name = "hello"
        """;

    [Theory]
    [InlineData(ArtifactRenderScopeKind.Module)]
    [InlineData(ArtifactRenderScopeKind.Feature)]
    public void should_compile_in_an_existing_application(ArtifactRenderScopeKind kind)
    {
        var model = invoice_model.Compile(Source);
        var module = model.Application.Modules.Single();
        var scope = new ArtifactRenderScope(kind, kind == ArtifactRenderScopeKind.Module ? module.Id : module.Features.Single().Id);
        var rendered = invoice_model.Plan(model, scope);
        ArtifactRenderPlan qualified;
        using (QualifiedSpelling.Begin()) qualified = invoice_model.Plan(model, scope);

        Assert.DoesNotContain(rendered.Artifacts, artifact => artifact.RelativePath.EndsWith("GlobalUsings.cs", StringComparison.Ordinal));
        AssertCompiles(Files(qualified));
        AssertCompiles(Files(rendered));
        var proof = QualifiedOutputProof.VerifyRendered(Files(qualified), Files(rendered), legacyImports: true);
        Assert.True(proof.Failures.Count == 0, string.Join('\n', proof.Failures));
        Assert.True(proof.ShortSpellings > 0, "The module spells no type by a short name.");
    }

    [Fact]
    public void should_preserve_full_render_output()
    {
        var full = invoice_model.Plan(invoice_model.Compile(Source));
        var files = Files(full).Where(file => file.RelativePath.EndsWith("ProjectLookup.cs", StringComparison.Ordinal) ||
            file.RelativePath.EndsWith("when_listing_projects_is_queried.cs", StringComparison.Ordinal) ||
            file.RelativePath.EndsWith("GlobalUsings.cs", StringComparison.Ordinal)).ToArray();
        Assert.Equal(FullOutput(), files);
    }

    static RenderedFile[] FullOutput() =>
    [
        new("GlobalUsings.cs", """
            global using Cratis.Chronicle.Keys;
            global using System.Collections.Immutable;
            global using System.Reactive.Subjects;
            """ + Environment.NewLine),
        new("Projects/Registration/ProjectLookup/ProjectLookup.cs", """
            using Invoices.Common;
            using Invoices.Projects.Registration.RegisterProject;

            namespace Invoices.Projects.Registration.ProjectLookup;

            [ReadModel]
            public record ProjectSummary([Key] string Key, ProjectName Name)
            {

                [AllowAnonymous]
                public static async Task<ProjectSummary?> ProjectById(IReadModels readModels, EventSourceId key) => await readModels.GetInstanceById<ProjectSummary>(key);

                [AllowAnonymous]
                public static ISubject<IEnumerable<ProjectSummary>> AllProjects(IReadModels readModels) => readModels.Observe<ProjectSummary>();
            }

            #pragma warning disable CHR0029, CHR0042
            /// <summary>
            /// Projects events onto ProjectSummary.
            /// </summary>
            public class ProjectSummaryProjection : IProjectionFor<ProjectSummary>
            {
                /// <summary>
                /// Defines the event transitions for this projection.
                /// </summary>
                /// <param name="builder">The projection builder.</param>
                public void Define(IProjectionBuilderFor<ProjectSummary> builder)
                {
                    builder.NoAutoMap();
                    builder.From<ProjectRegistered>(from =>
                    {
                        // Explicit mappings preserve the bound projection; no event-to-model AutoMap is inferred.
                        from.Set(model => model.Key).ToEventSourceId();
                        from.Set(model => model.Name).To(evt => evt.Name);
                    }
                    );
                }
            }
            #pragma warning restore CHR0029, CHR0042
            """ + Environment.NewLine),
        new("Projects/Registration/ProjectLookup/when_listing_projects_is_queried.cs", """
            using Cratis.Chronicle.Testing.Events;
            using Cratis.Specifications;
            using Invoices.Common;
            using Invoices.Projects.Registration.ProjectLookup;
            using Invoices.Projects.Registration.RegisterProject;
            using Xunit;

            namespace Invoices.Projects.Registration.ProjectLookup.when_listing_projects_is_queried;

            public class when_listing_projects_is_queried : Specification, IDisposable
            {
                IReadModels _readModels => _scenario.ReadModels;
                readonly EventStoreForTesting _scenario = new();
                IEnumerable<ProjectSummary> _results = [];
                static readonly string _key = "project-1";
                static readonly ProjectName _name = new ProjectName("hello");

                async Task Establish()
                {
                    (await _scenario.EventLog.Append(_key, new ProjectRegistered(_name))).IsSuccess.ShouldBeTrue();
                }

                async Task Because() => _results = await global::System.Reactive.Threading.Tasks.TaskObservableExtensions.ToTask(global::System.Reactive.Linq.Observable.FirstAsync(ProjectSummary.AllProjects(_readModels)));

                [Fact] void should_return_result_1() => (Enumerable.Single(_results, result => result.Key == _key) is not null).ShouldBeTrue();
                [Fact] void should_return_result_1_with_key() => Enumerable.Single(_results, result => result.Key == _key).Key.ShouldEqual(_key);
                [Fact] void should_return_result_1_with_name() => Enumerable.Single(_results, result => result.Key == _key).Name.ShouldEqual(_name);
                public void Dispose() => _scenario.Dispose();
            }
            """ + Environment.NewLine)
    ];

    static void AssertCompiles(RenderedFile[] files)
    {
        var errors = QualifiedOutputProof.CompilationErrors(files, legacyImports: true);
        Assert.True(errors.Count == 0, string.Join('\n', errors));
    }

    // Existing applications still have the pre-migration scaffold: no generated GlobalUsings.cs.
    static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
