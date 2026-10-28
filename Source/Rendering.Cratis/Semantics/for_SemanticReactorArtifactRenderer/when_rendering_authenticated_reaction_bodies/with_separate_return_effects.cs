// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_separate_return_effects : Specification
{
    (string Body, LoadedSemanticModel Inline, LoadedSemanticModel File)[] _models = [];
    (string Body, ArtifactRenderPlan Inline, ArtifactRenderPlan File)[] _plans = [];

    void Establish()
    {
        string[] bodies =
        [
            "return new object[] { new TicketOpened(Title) };",
            "return new object[] { new Notify(\"id\", Title) };",
            "yield return new TicketOpened(Title);",
            "TicketOpened opened = new TicketOpened(Title); yield return opened;",
            "TicketOpened? opened = new TicketOpened(Title); yield return opened;",
            "Notify command = new Notify(\"id\", Title); yield return command;",
            "TicketOpened[] opened = new[] { new TicketOpened(Title) }; return opened;",
            "IEnumerable<Notify> commands = new[] { new Notify(\"id\", Title) }; return commands;",
            "List<TicketOpened> opened = new List<TicketOpened> { new TicketOpened(Title) }; return opened;",
            "System.Collections.Generic.IReadOnlyList<Notify> commands = new[] { new Notify(\"id\", Title) }; return commands;",
            "yield return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, new TicketOpened(Title));",
            "yield return new Notify(\"id\", Title);",
            "return Title.Length > 0 ? new object[] { new TicketOpened(Title) } : null;",
            "var effects = new object[] { new Notify(\"id\", Title) }; return effects;",
            "var effects = new object[] { new TicketOpened(Title) }; return Title.Length > 0 ? effects : null;",
            "var effects = new object[] { new TicketOpened(Title) }; return [.. effects];",
            "return [new TicketOpened(Title)];",
            "return new List<object> { new Notify(\"id\", Title) };",
            "return null;",
            "return [];",
            "var effects = Title.Length > 0 ? new object[] { new TicketOpened(Title) } : null; return effects;",
            "return new object[] {};",
            "var effects = new object[] {}; return [.. effects];",
            "TicketOpened[] effects = new[] { new TicketOpened(Title) }; return [.. effects];",
            "return [.. new List<object> { new TicketOpened(Title) }];",
            "return (null);",
            "yield break;"
        ];
        _models = [.. bodies.Select(body => (body, reaction_return_model.Load(body), reaction_return_model.Load(body, file: true)))];
    }

    void Because() => _plans = [.. _models.Select(model => (model.Body, opaque_policy_model.Plan(model.Inline), opaque_policy_model.Plan(model.File)))];

    [Fact]
    void should_continue_to_render_and_compile_both_sources() => Assert.All(_plans, pair =>
    {
        Assert.True(pair.Inline.CanPublishArtifacts, opaque_policy_model.Errors(pair.Inline));
        Assert.True(pair.File.CanPublishArtifacts, opaque_policy_model.Errors(pair.File));
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(pair.Inline)));
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(pair.File)));
    });
    [Fact]
    void should_preserve_the_authenticated_body_bytes() => Assert.All(_plans, pair =>
    {
        var inline = Handler(pair.Inline);
        Assert.Equal(inline, Handler(pair.File));
        Assert.Contains(pair.Body, inline, StringComparison.Ordinal);
    });

    static string Handler(ArtifactRenderPlan plan) => Encoding.UTF8.GetString(Assert.Single(plan.Artifacts.Where(artifact =>
        artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))).Bytes.AsSpan());
}
#endif
