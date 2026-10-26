// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_generated_native_body_specifications
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_a_no_events_specification_driving_a_native_body(bool file)
    {
        var source = reaction_body_model.Source(reaction_body_model.Body, file) + "\n" + """
                  specification Notifying
                    when append TicketOpened
                      title = "Broken"
                    then no events
            """;
        Refused(source, file ? reaction_body_model.Body : null);
    }

    [Fact]
    public void should_refuse_a_given_event_driving_a_native_body()
    {
        var source = reaction_body_model.Source(reaction_body_model.Body)
            .Replace("      reaction Notifier", "      event Heartbeat\n      reaction Notifier", StringComparison.Ordinal) + "\n" + """
                  specification Notifying
                    given TicketOpened
                      title = "Broken"
                    when append Heartbeat
                    then no events
            """;
        Refused(source);
    }

    [Fact]
    public void should_still_render_a_native_body_without_a_specification()
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.Load());
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal));
    }

    static void Refused(string source, string? attachment = null)
    {
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source, attachment));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" &&
            diagnostic.Message.Contains("Stage cannot execute native reaction bodies in generated specs", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("reaction 'Notifier'", StringComparison.Ordinal));
    }
}
#endif
