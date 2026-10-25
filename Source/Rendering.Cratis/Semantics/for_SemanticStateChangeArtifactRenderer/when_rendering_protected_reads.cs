// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer;

public class when_rendering_protected_reads : Specification
{
    // OneOf.OneOf is named like its namespace; with type arguments the name cannot bind to the namespace, so it is short.
    const string Union = "OneOf<EventForEventSourceId, ValidationResult>";
    string _code = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ProtectedReads", "ProtectedReads"));
        _code = string.Join('\n', plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_protect_the_command_that_reads() => Declaration("Consume").ShouldContain("[ReadProtection(ReadProtectionMode.On)]");
    [Fact] void should_not_protect_a_command_without_reads() => Declaration("Store").ShouldNotContain("ReadProtection");
    [Fact] void should_look_the_view_up_by_its_key() => _code.ShouldContain("[LookUpBy(nameof(Consume.SourceId))] Current? current");
    [Fact] void should_guard_absence_with_the_reference_message() => _code.ShouldContain("if (current is null) return " + Union + ".FromT1(ValidationResult.Error(\"Current '\" + SourceId + \"' does not exist.\"));");
    [Fact] void should_guard_absence_before_the_requirement() => _code.IndexOf("current is null", StringComparison.Ordinal).ShouldBeLessThan(_code.IndexOf("The stored value must not be empty.", StringComparison.Ordinal));
    [Fact] void should_guard_the_requirement_inside_handle() => _code.ShouldContain("if (!(!object.Equals(current.Value, \"\"))) return " + Union + ".FromT1(ValidationResult.Error(\"The stored value must not be empty.\"));");
    [Fact] void should_map_the_read_path_into_the_event() => _code.ShouldContain("new Taken(current.Value)");
    [Fact] void should_move_the_requirement_out_of_the_validator() => _code.ShouldNotContain("RuleFor(_ => _).Must(command => ");
    [Fact] void should_commit_nothing_for_a_decision_without_an_outcome() => _code.ShouldContain(".FromT0([])");
    [Fact] void should_import_the_union_namespace() => _code.ShouldContain("using OneOf;");

    string Declaration(string command)
    {
        var start = _code.IndexOf($"public record {command}(", StringComparison.Ordinal);

        return _code[Math.Max(0, start - 400)..start];
    }
}
