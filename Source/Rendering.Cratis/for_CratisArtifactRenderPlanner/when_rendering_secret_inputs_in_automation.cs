// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_secret_inputs_in_automation : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _slice = string.Empty;
    string _invocation = string.Empty;
    string _production = string.Empty;
    string _concept = string.Empty;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept AccessToken : String secret
            module Work
              feature Processing
                slice StateChange Start
                  command StartWork
                    id String identifier
                    token AccessToken
                    produces event Started
                      for id
                      id String = id
                      token AccessToken = token
                slice Automation FollowUp
                  command CompleteWork
                    id String identifier
                    token AccessToken
                    produces event Completed
                      for id
                      token AccessToken = token
                  event Copied
                    token AccessToken
                  reaction Complete
                    when Started
                      invokes CompleteWork
                        id = id
                        token = token
                  reaction Copy
                    when Started
                      produces Copied
                        token = token
            """);
        _plan = invoice_model.Plan(model);
        _slice = Content("FollowUp.cs");
        _invocation = Content("Complete.cs");
        _production = Content("Copy.cs");
        _concept = Content("AccessToken.cs");
    }

    string Content(string fileName) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith($"/{fileName}", StringComparison.Ordinal)).Bytes.AsSpan());

    [Fact] void should_admit_the_complete_application() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_exclude_the_automation_command_secret_from_causation_capture() => _slice.ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] global::Invoices.Common.AccessToken Token");
    [Fact] void should_invoke_the_protected_command_record() => _invocation.ShouldContain("new global::Invoices.Work.Processing.FollowUp.CompleteWork(@event.Id, @event.Token)");
    [Fact] void should_protect_the_produced_event_secret() => _concept.ShouldContain("global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute");
    [Fact] void should_produce_the_protected_event_record() => _production.ShouldContain("new global::Invoices.Work.Processing.FollowUp.Copied(@event.Token)");
}
