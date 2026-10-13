// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_unpinned_generated_destinations_and_unordered_expectations : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    const string Source = """
        concept WorkId : Uuid
        module Work
          feature Processing
            slice Automation FollowUp
              command Complete
                id WorkId generated identifier
                produces Completed
                  for id
                produces Acknowledged
                  for id
              event Started
              event Completed
              event Acknowledged
              reaction Worker
                when Started
                  invokes Complete
              specification Appending
                when append Started
                then Acknowledged
                then Completed
              specification Commanding
                when Complete
                then Acknowledged
                then Completed
        """;

    void Establish()
    {
        SetContext(Compile(Source));
        ReplaceAutomation(_located.Slice with
        {
            Specifications = [.. _located.Slice.Specifications.Select(specification => specification with { ThenEventsInAnyOrder = true })]
        });
    }

    void Because() => SemanticSpecificationAdmission.Validate(_context, _located.Slice, _diagnostics);

    [Fact] void should_refuse_both_the_append_and_equivalent_command_with_the_generation_diagnostic() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-038", "STAGE-ESM-038"]);
    [Fact] void should_locate_each_refusal_on_its_original_specification() => _diagnostics.Select(diagnostic => diagnostic.Artifact).ShouldContainOnly(_located.Slice.Specifications.Select(specification => specification.Id));
}
