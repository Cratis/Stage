// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer;

public class when_rendering_commands_and_events : given.an_automation
{
    string _source = string.Empty;

    void Because() => _source = SemanticAutomationArtifactRenderer.Render(_located, _context).Content;

    [Fact] void should_include_the_slice_command() => _source.ShouldContain("public record CompleteWork(string Id, decimal Amount, string? Note)");
    [Fact] void should_include_its_event() => _source.ShouldContain("public record Completed(decimal Amount);");
    [Fact] void should_include_the_reaction_production_event() => _source.ShouldContain("public record Acknowledged(global::System.DateTimeOffset ScheduledAt);");
    [Fact] void should_not_paste_reactors_into_the_slice_file() => _source.ShouldNotContain("IReactor");
}
