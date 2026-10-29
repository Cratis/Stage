// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class native_reaction_model
{
    internal const string Body = "if (@event.Score >= 9) { return [new TalkHighlighted(@event.Score)]; } return [];";

    internal static ArtifactRenderPlan Plan(string body = Body, bool file = false) =>
        opaque_policy_model.Plan(reaction_body_model.LoadSource(Source(body, file), file ? body : null));

    internal static ArtifactRenderPlan ExecutablePlan()
    {
        var file = Slice(Source(file: true)).Replace("Highlight", "FileHighlight", StringComparison.Ordinal);
        var mutant = Slice(Source(Body.Replace("new TalkHighlighted(@event.Score)", "new TalkHighlighted(@event.Score + 1)", StringComparison.Ordinal)))
            .Replace("Highlight", "MutatedHighlight", StringComparison.Ordinal);
        return opaque_policy_model.Plan(reaction_body_model.LoadSource(Source() + "\n" + file + "\n" + mutant, Body.Replace("Highlight", "FileHighlight", StringComparison.Ordinal)));
    }

    static string Slice(string source) => source[source.IndexOf("    slice Automation", StringComparison.Ordinal)..];

    internal static string Source(string body = Body, bool file = false) => $$"""
        domain Conference.Common
        concept TalkId : Uuid
        module CallForPapers
          feature Review
            slice StateChange Accepting
              command AcceptTalk
                talkId TalkId identifier
                score Int
                produces TalkAccepted
                  for talkId
                  score = score
              event TalkAccepted
                score Int
            slice Automation Highlighting
              event TalkHighlighted
                score Int
              reaction HighlightTopTalk
                when TalkAccepted
        {{(file ? "          file Body.cs" : "          ```csharp\n          " + body + "\n          ```")}}
              specification HighScoreHighlights
                when append TalkAccepted
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 10
                then TalkHighlighted
                  score = 10
              specification LowScoreDoesNotHighlight
                when append TalkAccepted
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa7"
                  score = 5
                then no events
        """;
}
#endif
