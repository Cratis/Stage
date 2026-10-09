// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;

public class a_source_plan : a_register_project_render_request
{
    protected string _root = null!;
    protected CratisPlanOptions _planOptions = null!;
    protected LoadedSemanticModel _loaded = null!;
    protected PlanSelection _featureSelection = new([PlanSelectionEntry.Feature("Projects", "Registration")]);
    protected PlanSelection _sliceSelection = new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]);
    protected CratisPlanResult _result = null!;

    async Task Establish()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent!;
        _root = Path.Combine(directory.FullName, ".ai-work", "210", "source-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(Path.Combine(_root, "projects.play"), Source);
        await File.WriteAllTextAsync(Path.Combine(_root, "tasks.play"), """
            module Tasks
              feature Backlog
                feature Intake
                  slice StateChange CaptureTask
                    command CaptureTask
                      taskId Uuid identifier
                      title String
                      produces TaskCaptured
                        for taskId
                        title = title
                    event TaskCaptured
                      title String
            """);
        await File.WriteAllTextAsync(Path.Combine(_root, "context.play"), "concept Unused : String\n");
        _planOptions = new(_model.Application.Name, _options.ProjectName, _options.RootNamespace);
        var loaded = await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join("; ", loaded.Diagnostics));
        _loaded = loaded.Loaded!;
    }

    protected Task<CratisPlanResult> From(params string[] paths) =>
        CratisRendering.PlanFrom(new PlaySources(_root, [.. paths]), _featureSelection, _planOptions);

    protected void ShouldSucceed() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    protected void ShouldRefuse(string code) => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    protected static IEnumerable<string> Paths(CratisPlanResult result) => result.Artifacts.Select(artifact => artifact.RelativePath);

    void Destroy() => Directory.Delete(_root, recursive: true);
}
