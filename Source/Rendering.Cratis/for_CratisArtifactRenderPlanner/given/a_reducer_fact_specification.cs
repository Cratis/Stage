// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class a_reducer_fact_specification
{
    internal const string Body = "return new TalkScore(Guid.Parse(context.Key), (context.State?.Score ?? 0) + context.Event.Score);";

    internal const string Source = """
        concept TalkId : Uuid
        module CallForPapers
          feature Review
            slice StateChange Scoring
              command ScoreTalk
                talkId TalkId identifier
                score Int
                produces TalkScored
                  for talkId
                  score = score
              event TalkScored
                score Int
            slice StateView TalkScores
              readmodel TalkScore
                talkId TalkId key
                score Int
              reducer TalkScoreReducer => TalkScore
                on TalkScored
                  ```csharp
                  return new TalkScore(Guid.Parse(context.Key), (context.State?.Score ?? 0) + context.Event.Score);
                  ```
              query ScoreOfTalk => TalkScore optional
                by talkId TalkId
              specification ScoreShows
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
                then readmodel TalkScore
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
              specification ScoreAccumulates
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
                given TalkScored
                  for "4fa85f64-5717-4562-b3fc-2c963f66afa7"
                  score = 40
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 5
                then readmodel TalkScore
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 12
              specification ScoreAfterAppend
                given TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 7
                when append TalkScored
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 5
                then readmodel TalkScore
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  score = 12
        """;

    internal static async Task<ArtifactRenderPlan> Plan(string source = Source, bool self = false) =>
        when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source), self: self);
}
#endif
