// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceHandlerArtifactRenderer;

public class when_rendering_application_trigger_invocations : Specification
{
    string _handler = string.Empty;
    string _effect = string.Empty;
    IReadOnlyList<string> _errors = [];
    IReadOnlyList<string> _warnings = [];

    void Because()
    {
        var source = for_SemanticOccurrenceAdmission.given.an_application_trigger_reaction.InvocationSource
            .Replace("      reaction ProcessBatch", "      reaction ProcessBatch\n        runs as system role \"Worker\"", StringComparison.Ordinal);
        var model = for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
        var reaction = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        _effect = $"stage:reaction:{reaction.Id}:invocation:0:{reaction.Triggers[0].Invokes[0].Command}";
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        plan.Diagnostics.ShouldBeEmpty();
        plan.Success.ShouldBeTrue();
        _handler = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => Path.GetFileName(artifact.RelativePath) == "ProcessBatch.cs").Bytes.AsSpan());
        var files = when_rendering_application_trigger_productions.Files(plan);
        _errors = for_CratisRenderer.RenderedOutput.Errors(files);
        _warnings = for_CratisRenderer.RenderedOutput.Warnings(files);
    }

    [Fact] void should_inject_the_occurrence_command_executor() => _handler.ShouldContain("IOccurrenceCommandExecutor commands");
    [Fact] void should_not_inject_unused_production_dependencies() => _handler.ShouldNotContain("IChronicleClient");
    [Fact] void should_use_a_stable_invocation_effect_name() => _handler.ShouldContain(_effect);
    [Fact] void should_pass_system_roles() => _handler.ShouldContain("[\"Worker\"], cancellationToken");
    [Fact] void should_construct_the_command_from_trigger_properties() => _handler.ShouldContain("new FinishBatch(occurrence.BatchId)");
    [Fact] void should_not_wrap_the_executor_in_a_unit_of_work() => _handler.ShouldNotContain("UnitOfWorkManager");
    [Fact] void should_throw_when_not_settled() => _handler.ShouldContain("if (!invocation0.IsSettled) throw new InvalidOperationException");
    [Fact] void should_throw_on_an_unhandled_refusal() => _handler.ShouldContain("if (invocation0.Outcome is Refused) throw new InvalidOperationException(\"Reaction 'ProcessBatch' command 'FinishBatch' from application trigger 'BatchRequested' was refused.\")");
    [Fact] void should_use_short_type_names() => _handler.ShouldNotContain("global::");
    [Fact] void should_compile_against_the_pinned_client() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();
}
