// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_generated_native_body_specifications
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_omit_a_no_events_specification_driving_a_native_body(bool file)
    {
        var source = reaction_body_model.Source(reaction_body_model.Body, file) + "\n" + """
                  specification Notifying
                    when append TicketOpened
                      title = "Broken"
                    then no events
            """;
        Omitted(source, file ? reaction_body_model.Body : null);
    }

    [Fact]
    public void should_omit_a_given_event_driving_a_native_body()
    {
        var source = reaction_body_model.Source(reaction_body_model.Body)
            .Replace("      reaction Notifier", "      event Heartbeat\n      reaction Notifier", StringComparison.Ordinal) + "\n" + """
                  specification Notifying
                    given TicketOpened
                      for "ticket-one"
                      title = "Broken"
                    when append Heartbeat
                    then no events
            """;
        Omitted(source);
    }

    [Fact]
    public void should_still_render_a_native_body_without_a_specification()
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.Load());
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal));
    }

    static void Omitted(string source, string? attachment = null)
    {
        var loaded = reaction_body_model.LoadSource(source, attachment);
        var specification = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Equal(ArtifactRenderOutcome.Complete, plan.Outcome);
        plan.Artifacts.ShouldNotBeEmpty();
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "Program.cs");
        var handler = Encoding.UTF8.GetString(Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))).Bytes.AsSpan());
        Assert.Contains("var Title = @event.Title;", handler, StringComparison.Ordinal);
        Assert.Contains(reaction_body_model.Body, handler, StringComparison.Ordinal);
        var refusal = Assert.Single(plan.Refusals);
        Assert.Equal(specification.Id, refusal.Artifact);
        Assert.Equal("specification", refusal.Kind);
        Assert.Contains("STAGE-ESM-011", refusal.Codes);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.Sources.Contains(specification.Id));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" &&
            diagnostic.Artifact == specification.Id && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning &&
            diagnostic.Message.Contains($"Skipped specification '{specification.Name}' (STAGE-ESM-011)", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Stage cannot execute native reaction bodies in generated specs", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("reaction 'Notifier'", StringComparison.Ordinal));
    }
}
#endif
