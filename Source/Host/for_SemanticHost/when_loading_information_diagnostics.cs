// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_information_diagnostics : given.a_model_path
{
    readonly List<StageUnsupportedIssue> _issues = [];
    readonly List<SemanticModelLoadDiagnostic> _notes = [];
    LoadedSemanticModel? _loaded;
    SceneApplication? _scene;
    SemanticExecutionResult _execution = null!;

    Task Establish() => File.WriteAllTextAsync(_path, """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Register a project"
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
                description "A project was registered"
        """);

    async Task Because()
    {
        (_loaded, _scene) = await SemanticHost.LoadModel(_path, _issues, _notes);
        var command = _loaded!.Plan.Commands.Values.Single();
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, new SemanticTextValue("f439c9cb-4207-48a5-8445-94572c2d21b9"))], []);
        _execution = new SemanticEvaluator().Execute(_loaded.Plan, SemanticWorld.Empty, request);
    }

    [Fact] void should_load_the_semantic_model() => _loaded.ShouldNotBeNull();
    [Fact] void should_serve_a_scene() => _scene.ShouldNotBeNull();
    [Fact] void should_admit_the_command() => _loaded!.Plan.Commands.Count.ShouldEqual(1);
    [Fact] void should_execute_the_command() => _execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_registered_event() => ((SemanticAccepted)_execution).Facts.Length.ShouldEqual(1);
    [Fact] void should_report_no_unsupported_issues() => _issues.ShouldBeEmpty();
    [Fact] void should_keep_both_notes() => _notes.Count.ShouldEqual(2);
    [Fact] void should_preserve_information_severity() => _notes.TrueForAll(note => note.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
}
