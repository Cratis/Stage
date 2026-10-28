// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_model_type_dependencies
{
    [Fact]
    public void should_compile_a_returned_event_declared_in_another_slice()
    {
        const string body = "return new object[] { new global::Invoices.Support.Tickets.FollowUp.Notified(Title) };";
        var source = reaction_body_model.Source(body) + "\n" + """
                slice Automation FollowUp
                  event Notified
                    title String
            """;
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        AssertRendered(plan, body);
        var compilation = RenderedOutput.CreateCompilation(opaque_policy_model.Files(plan));
        Assert.NotNull(compilation.GetTypeByMetadataName("Invoices.Support.Tickets.FollowUp.Notified"));
    }

    [Fact]
    public void should_compile_a_concept_comparison_using_the_real_generated_conversions()
    {
        const string body = "return Count > 0 ? new object[] { new TicketOpened(Title, Count) } : null;";
        var source = "concept UsageCount : Int\n" + reaction_body_model.Source(body, extraProperty: "        count UsageCount");
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        AssertRendered(plan, body);
    }

    static void AssertRendered(ArtifactRenderPlan plan, string body)
    {
        Assert.True(plan.CanPublishArtifacts, opaque_policy_model.Errors(plan));
        var handler = Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal)));
        Assert.Contains(body, Encoding.UTF8.GetString(handler.Bytes.AsSpan()), StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(plan)));
        Assert.Empty(RenderedOutput.Warnings(opaque_policy_model.Files(plan)));
    }
}
#endif
