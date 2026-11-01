// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_implementation_requirements_are_supplied : given.a_provenance_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(With([], [
        Requirement("r2", SemanticImplementationRole.QueryPerformer, "Other", null, "perform.cs", "bb"),
        Requirement("r1", hash: "aa"),
        Requirement("r3", file: "missing.cs", hash: string.Empty),
        Requirement("r1", hash: "aa")]));

    [Fact] void should_record_one_distinct_entry_per_requirement() => _plan.InputProvenance.Implementations.Length.ShouldEqual(3);
    [Fact] void should_sort_by_requirement_id() => _plan.InputProvenance.Implementations.Select(_ => _.RequirementId).ShouldEqual(["r1", "r2", "r3"]);
    [Fact] void should_record_the_requirement_fields() => _plan.InputProvenance.Implementations[1].ShouldEqual(
        new("r2", SemanticImplementationRole.QueryPerformer, SemanticAddress.ForApplication(ApplicationIdentity.Create("Other")), null, "perform.cs", "bb"));
    [Fact] void should_record_an_unresolved_file_with_an_empty_hash() =>
        _plan.InputProvenance.Implementations[2].ShouldEqual(
            new("r3", SemanticImplementationRole.CommandHandler, SemanticAddress.ForApplication(ApplicationIdentity.Create("Projects")), "csharp", "missing.cs", string.Empty));
}
