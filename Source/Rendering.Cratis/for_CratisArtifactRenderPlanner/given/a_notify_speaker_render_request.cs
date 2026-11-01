// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_notify_speaker_render_request : Specification
{
    // The issue-repro harness's 828-reaction/main.play model, copied without changing its contracts.
    protected const string Source = """
        domain Conference.Common
        concept TalkId : Uuid
        policy IsProgramCommittee
          require role "ProgramCommittee"

        module CallForPapers
          feature Review
            slice StateChange Accepting
              command AcceptTalk
                talkId TalkId identifier
                acceptedAt DateTime
                produces TalkAccepted
                  for talkId
                  acceptedAt = acceptedAt
              event TalkAccepted
                acceptedAt DateTime
              constraint OneAcceptancePerTalk
                unique event TalkAccepted

            slice Automation NotifySpeaker
              command SendAcceptanceMail
                talkId    TalkId identifier
                authorize IsProgramCommittee
                produces AcceptanceMailSent
                  for talkId
                  sentAt = $context.occurred

              event AcceptanceMailSent
                sentAt DateTime

              reaction AcceptanceNotifier
                runs as system role "ProgramCommittee"
                when TalkAccepted
                  invokes SendAcceptanceMail
                    talkId = $eventSourceId

              specification AcceptingATalkTellsTheSpeaker
                given clock "2026-11-06T09:00:00Z"
                when append TalkAccepted
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  acceptedAt = "2026-11-06T09:00:00Z"
                then AcceptanceMailSent
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  sentAt = "2026-11-06T09:00:00Z"
        """;

    protected ArtifactRenderPlan _plan = null!;
    protected SemanticSlice _slice = null!;

    protected void Plan(string source)
    {
        var model = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);
        _slice = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "NotifySpeaker");
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var options = new CratisRenderingOptions("Conference", "Conference");
        _plan = new CratisArtifactRenderPlanner().Plan(new(
            model,
            execution.Plan!,
            CratisRendering.CreateProfile(model.Application.Name, options),
            new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }
}
