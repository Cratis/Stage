// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_model_with_slice_local_plan_issues : Specification
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                description String
                produces ProjectRegistered
                  for projectId
                  description = description
              event ProjectRegistered
                description String
              specification Registering
                when RegisterProject
                  projectId = "first"
                  description = "North"
                then ProjectRegistered
                  description = "North"
            slice StateView Lookup
              readmodel ProjectView
                projectId String
                description String
              query ProjectById => ProjectView optional
                by projectId String
              projection ProjectViews => ProjectView
                from ProjectRegistered
                  projectId = $eventSourceId
                  description = description
              specification LookingUp
                then query ProjectById
                  arguments
                    projectId = "first"
                  result
                    projectId = "first"
                    description = "North"
        """;

    SemanticModelLoadResult _result = null!;

    void Because() => _result = Create();

    // Source compilation now requires optional lookups; older ESM still contains required queries.
    public static SemanticModelLoadResult Create()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "source.play", Source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        var original = compiled.Value!;
        var module = original.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with { Queries = [.. slice.Queries.Select(query => query with { Cardinality = SemanticQueryCardinality.One })] });
        var application = original.Model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        var model = ExecutableSemanticModel.Create(original.Model.LanguageVersion, original.Model.SemanticVersion, application);
        var compilation = SemanticCompilation.Create(model, original.Documents, original.SourceMap);
        return SemanticModelLoader.FinishCompilation(new(compilation, compiled.Diagnostics), [], ReleaseBaselineAssessment.NotSupplied, default);
    }

    [Fact] void should_load_the_model() => Assert.True(_result.Success, string.Join("; ", _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_keep_the_plan_issue_as_a_warning() => Assert.Contains(_result.Notes, note => note.Code == "STAGE-PLAN-004" && note.Severity == DiagnosticSeverity.Warning);
    [Fact] void should_have_no_blocking_diagnostics() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_attach_the_warning_to_the_loaded_model() => Assert.Equal(_result.Notes.AsEnumerable(), _result.Loaded!.Notes.AsEnumerable());
    [Fact] void should_run_the_supported_command() => new SemanticSpecificationRunner().Run(_result.Loaded!.Plan, _result.Loaded.Plan.Specifications.Values.Single(specification => specification.Name == "Registering").Id).Passed.ShouldBeTrue();
    [Fact] void should_refuse_only_the_single_query() => Assert.IsType<SemanticUnsupported>(new SemanticEvaluator().Execute(_result.Loaded!.Plan, SemanticWorld.Empty, SemanticExecutionRequest.ForQueries([new(_result.Loaded.Plan.Queries.Keys.Single(), SemanticValue.Text("first"))]))).Capability.ShouldEqual(SemanticExecutionCapability.Query);
}
#endif
