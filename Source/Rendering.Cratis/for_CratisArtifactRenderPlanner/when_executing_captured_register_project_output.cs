// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_captured_register_project_output : a_register_project_render_request
{
    readonly List<EventSourceId> _destinations = [];
    readonly List<Guid> _eventProjectIds = [];
    readonly List<string> _eventNames = [];
    readonly List<Type> _eventClrTypes = [];
    readonly List<EventType> _eventTypes = [];
    readonly List<ValidationResult> _validations = [];
    ExecutableSemanticModel _capturedModel = null!;
    SemanticExecutionPlanCompilation _capturedExecution = null!;
    ArtifactRenderPlan _capturedPlan = null!;
    IReadOnlyList<string> _compilationErrors = null!;
    IReadOnlyList<string> _compilationWarnings = null!;
    ValidationResult _emptyNameValidation = null!;
    Type _expectedEventType = null!;
    byte[] _capture = null!;
    byte[] _reserializedCapture = null!;
    string _expectedRejectionMessage = null!;
    (Guid Id, string Name)[] _inputs = [];

    void Establish()
    {
        _inputs =
        [
            (Guid.Parse(Corpus.RuntimeStreamId), "Screenplay"),
            (Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa7"), "Stage")
        ];
        _expectedRejectionMessage = Corpus.SpecificationExpectations.Single(_ => _.Outcome == SemanticExecutionOutcomeKind.Rejected).RejectionMessage!;
    }

    void Because()
    {
        var compilation = Compile(Corpus.SourceForms.Single(_ => _.Name == "single"));
        _capture = SemanticModelSerializer.Serialize(compilation.Model);
        _capturedModel = SemanticModelSerializer.Deserialize(_capture);
        _reserializedCapture = SemanticModelSerializer.Serialize(_capturedModel);
        _capturedExecution = SemanticExecutionPlan.Compile(_capturedModel);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _capturedModel.Application.Id);
        _capturedPlan = CratisRendering.Plan(_capturedModel, _capturedExecution.Plan!, scope, _options);
        var sources = _capturedPlan.Artifacts
            .Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
            .Select(_ => new RenderedFile(_.RelativePath, Text(_)))
            .ToArray();
        _compilationErrors = RenderedOutput.Errors(sources);
        _compilationWarnings = RenderedOutput.Warnings(sources);

        // Load once for this fixture; the unchanged compiler includes every generated DEBUG specification.
        var assembly = RenderedOutput.Load(sources);
        var projectIdType = assembly.GetType("Projects.Common.ProjectId", throwOnError: true)!;
        var projectNameType = assembly.GetType("Projects.Common.ProjectName", throwOnError: true)!;
        var commandType = assembly.GetType("Projects.Projects.Registration.RegisterProject.RegisterProject", throwOnError: true)!;
        _expectedEventType = assembly.GetType("Projects.Projects.Registration.RegisterProject.ProjectRegistered", throwOnError: true)!;
        var validatorType = assembly.GetType("Projects.Projects.Registration.RegisterProject.RegisterProjectValidator", throwOnError: true)!;
        var validator = (IValidator)Activator.CreateInstance(validatorType)!;
        var handle = commandType.GetMethod("Handle", Type.EmptyTypes)!;
        foreach (var input in _inputs)
        {
            var projectId = Activator.CreateInstance(projectIdType, input.Id)!;
            var projectName = Activator.CreateInstance(projectNameType, input.Name)!;
            var command = Activator.CreateInstance(commandType, projectId, projectName)!;
            _destinations.Add(((ICanProvideEventSourceId)command).GetEventSourceId());
            _validations.Add(Validate(validator, commandType, command));
            var @event = handle.Invoke(command, [])!;
            var eventType = @event.GetType();
            _eventClrTypes.Add(eventType);
            _eventTypes.Add(eventType.GetEventType());
            _eventProjectIds.Add(((EventSourceId<Guid>)eventType.GetProperty("ProjectId")!.GetValue(@event)!).Value);
            _eventNames.Add(((ConceptAs<string>)eventType.GetProperty("Name")!.GetValue(@event)!).Value);
        }

        var emptyNameCommand = Activator.CreateInstance(
            commandType,
            Activator.CreateInstance(projectIdType, _inputs[0].Id),
            Activator.CreateInstance(projectNameType, string.Empty))!;
        _emptyNameValidation = Validate(validator, commandType, emptyNameCommand);
    }

    [Fact] void should_preserve_the_captured_bytes() => _reserializedCapture.SequenceEqual(_capture).ShouldBeTrue();
    [Fact] void should_load_the_expected_corpus_revision() => _capturedModel.Revision.ShouldEqual(Corpus.SemanticRevision);
    [Fact] void should_admit_the_captured_execution_plan() => _capturedExecution.Success.ShouldBeTrue();
    [Fact] void should_execute_only_the_deserialized_model() => ReferenceEquals(_capturedExecution.Plan!.Model, _capturedModel).ShouldBeTrue();
    [Fact] void should_admit_the_captured_application_render_plan() => _capturedPlan.Success.ShouldBeTrue();
    [Fact] void should_compile_all_generated_backend_and_debug_specification_sources() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, _compilationWarnings).ShouldEqual(string.Empty);
    [Fact] void should_provide_the_canonical_destination() => _destinations[0].ShouldEqual(new EventSourceId(Corpus.RuntimeStreamId));
    [Fact] void should_provide_the_second_commands_distinct_destination() => _destinations[1].ShouldEqual(new EventSourceId(_inputs[1].Id.ToString()));
    [Fact] void should_return_the_exact_generated_project_registered_type() => _eventClrTypes.TrueForAll(_ => _ == _expectedEventType).ShouldBeTrue();
    [Fact] void should_return_the_fully_qualified_project_registered_event() => _eventClrTypes.TrueForAll(_ => _.FullName == "Projects.Projects.Registration.RegisterProject.ProjectRegistered").ShouldBeTrue();
    [Fact] void should_identify_the_native_event_type_as_project_registered() => _eventTypes.TrueForAll(_ => _.Id.Value == "ProjectRegistered").ShouldBeTrue();
    [Fact] void should_use_the_first_native_event_generation() => _eventTypes.TrueForAll(_ => _.Generation == EventTypeGeneration.First).ShouldBeTrue();
    [Fact] void should_carry_the_canonical_project_identity() => _eventProjectIds[0].ShouldEqual(_inputs[0].Id);
    [Fact] void should_carry_the_canonical_project_name() => _eventNames[0].ShouldEqual("Screenplay");
    [Fact] void should_carry_the_second_commands_distinct_identity() => _eventProjectIds[1].ShouldEqual(_inputs[1].Id);
    [Fact] void should_carry_the_second_commands_distinct_name() => _eventNames[1].ShouldEqual(_inputs[1].Name);
    [Fact] void should_accept_both_valid_names() => _validations.TrueForAll(_ => _.IsValid).ShouldBeTrue();
    [Fact] void should_have_no_failures_for_valid_names() => _validations.SelectMany(_ => _.Errors).ShouldBeEmpty();
    [Fact] void should_reject_an_empty_name() => _emptyNameValidation.IsValid.ShouldBeFalse();
    [Fact] void should_report_exactly_one_empty_name_failure() => _emptyNameValidation.Errors.Count.ShouldEqual(1);
    [Fact] void should_report_the_name_property() => Assert.True(
        _emptyNameValidation.Errors.Select(_ => _.PropertyName).SequenceEqual(["name"]),
        $"Expected exactly [name]; actual ({_emptyNameValidation.Errors.Count}): [{string.Join(", ", _emptyNameValidation.Errors.Select(_ => _.PropertyName))}]");
    [Fact] void should_report_the_exact_corpus_rejection_message() => _emptyNameValidation.Errors.Select(_ => _.ErrorMessage).ShouldEqual(_expectedRejectionMessage);

    static ValidationResult Validate(IValidator validator, Type commandType, object command)
    {
        var contextType = typeof(ValidationContext<>).MakeGenericType(commandType);
        var context = (IValidationContext)Activator.CreateInstance(contextType, command)!;
        return validator.Validate(context);
    }
}
