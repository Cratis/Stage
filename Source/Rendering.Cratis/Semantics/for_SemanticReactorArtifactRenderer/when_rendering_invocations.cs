// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer;

public class when_rendering_invocations : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;

    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive"), _context).Content;

    [Fact] void should_guard_the_invocation_by_the_modeled_condition() => _source.ShouldContain("if ((@event.Amount > 0m))");
    [Fact] void should_construct_only_request_inputs_and_fill_optional_absence() => _source.ShouldContain("new CompleteWork(@event.Id, @event.Amount, null)");
    [Fact] void should_return_commands_instead_of_calling_the_pipeline() => _source.ShouldNotContain("ICommandPipeline");

    [Fact]
    public void should_leave_generated_values_to_command_execution()
    {
        SetContext(Compile("concept ReceiptId : Uuid\n" + Source.Replace("        note String optional", "        note String optional\n        receipt ReceiptId generated", StringComparison.Ordinal)));
        var source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive"), _context).Content;
        source.ShouldContain("new CompleteWork(@event.Id, @event.Amount, null)");
        source.ShouldNotContain("ReceiptId");
    }
}
