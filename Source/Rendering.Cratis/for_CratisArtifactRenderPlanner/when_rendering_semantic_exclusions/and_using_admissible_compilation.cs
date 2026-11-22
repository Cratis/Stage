// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_using_admissible_compilation : given.an_excluded_feature
{
    AdmissibleCompilationResult _admissible = null!;
    IReadOnlyList<string> _errors = [];

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source.Replace("command RegisterProject\n", "command RegisterProject\n        description \"Registers a surviving project\"\n", StringComparison.Ordinal).TrimEnd() +
            "\n    slice StateView Removed\n      readmodel RemovedView\n        id String\n      query Scoped => RemovedView[]\n        scoped to identity\n";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        _admissible = new SemanticModelCompiler().CompileAdmissible("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(_admissible.Compilation.Success, string.Join(Environment.NewLine, _admissible.Compilation.Diagnostics));
        var compiled = _admissible.Compilation.Value!;
        _request = _request with
        {
            Model = compiled.Model,
            Profile = CratisRendering.WithAuthoringMetadata(_request.Profile, compiled),
            ExecutionPlan = SemanticExecutionPlan.Compile(compiled.Model).Plan,
            Scope = new(ArtifactRenderScopeKind.Application, compiled.Model.Application.Id),
            IdentityCatalog = compiled.Documents.IdentityCatalog,
            Exclusions = _admissible.Exclusions
        };
    }

    void Because()
    {
        _plan = _planner.Plan(_request);
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath is not ("Program.cs" or "GlobalUsings.cs"))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)));
        _errors = RenderedOutput.Errors(files);
    }

    [Fact] void should_use_actual_compiler_exclusions() => _admissible.Exclusions.IsEmpty.ShouldBeFalse();
    [Fact] void should_render_the_surviving_commands() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_surviving_authoring_metadata() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal))).ShouldContain("/// Registers a surviving project");
    [Fact] void should_publish_both_reports_with_real_identity_metadata() => _plan.Artifacts.Any(artifact => artifact.RelativePath == ".stage/completeness.json").ShouldBeTrue();
    [Fact] void should_report_the_original_binding_diagnostics() => _admissible.Exclusions.SelectMany(exclusion => exclusion.Diagnostics).All(diagnostic => Text(Report).Contains(diagnostic.Code, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_the_actual_generated_sources() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
}
#endif
