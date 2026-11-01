// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_degrading_reaction_specifications : given.a_notify_speaker_render_request
{
    void Because() => Plan(Source + """

              specification DenyingGuestMail
                given caller
                when SendAcceptanceMail
                  talkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then denied
        """);

    [Fact] void should_render_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_application() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath == "Program.cs");
    [Fact] void should_preserve_the_reaction() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("AcceptanceNotifier.cs", StringComparison.Ordinal));
    [Fact] void should_preserve_the_role_policy() => _plan.Artifacts.ShouldContain(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("context.Principal.IsInRole(\"ProgramCommittee\")", StringComparison.Ordinal));
    [Fact] void should_preserve_the_renderable_specification() => _plan.Artifacts.ShouldContain(artifact => artifact.Sources.Contains(_slice.Specifications.Single(specification => specification.Name == "DenyingGuestMail").Id));
    [Fact] void should_omit_the_unrenderable_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_slice.Specifications.Single(specification => specification.Name == "AcceptingATalkTellsTheSpeaker").Id);
    [Fact] void should_warn_with_the_name_code_and_reason() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Message.Contains("AcceptingATalkTellsTheSpeaker", StringComparison.Ordinal) && diagnostic.Message.Contains("STAGE-ESM-024", StringComparison.Ordinal) && diagnostic.Message.Contains("clock", StringComparison.Ordinal));
}
