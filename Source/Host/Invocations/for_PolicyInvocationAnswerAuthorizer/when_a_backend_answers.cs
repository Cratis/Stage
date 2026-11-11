// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_PolicyInvocationAnswerAuthorizer;

public class when_a_backend_answers : Specification
{
    IAuthorizationService _authorization = null!;
    PolicyInvocationAnswerAuthorizer _authorizer = null!;
    bool _ownSystem;
    bool _anotherSystem;
    bool _claimOfTheName;
    bool _ownSystemWithAPolicy;

    void Establish()
    {
        _authorization = Substitute.For<IAuthorizationService>();
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>()).Returns(Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed());
        _authorizer = new PolicyInvocationAnswerAuthorizer(_authorization);
    }

    async Task Because()
    {
        _ownSystem = await _authorizer.Authorize(Record("ability:Slack.Review@1"), Backend("Slack"));
        _anotherSystem = await _authorizer.Authorize(Record("ability:Mail.Send@1"), Backend("Slack"));

        // A person who carries a claim that looks like the backend's is still a person, held to the policy.
        var person = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "extension:Slack")], "test"));
        _claimOfTheName = await _authorizer.Authorize(Record("ability:Slack.Review@1", "Approvers"), person);
        _ownSystemWithAPolicy = await _authorizer.Authorize(Record("ability:Slack.Review@1", "Approvers"), Backend("Slack"));
    }

    static ClaimsPrincipal Backend(string system) => new(new ExtensionIdentity(system));

    static InvocationRecord Record(string capability, string policy = "") =>
        new("invocation-1", capability, EffectClass.Once, "sha256:a", new(InvocationOriginKind.ReactionBranch, "A/B", "1"), InvocationStatus.Suspended, DateTimeOffset.UtcNow) { AnswerPolicy = policy };

    [Fact] void should_let_it_answer_an_invocation_of_its_own_system() => _ownSystem.ShouldBeTrue();
    [Fact] void should_not_let_it_answer_an_invocation_of_another_system() => _anotherSystem.ShouldBeFalse();
    [Fact] void should_let_it_answer_whatever_policy_is_meant_for_people() => _ownSystemWithAPolicy.ShouldBeTrue();
    [Fact] void should_not_be_fooled_by_a_claim() => _claimOfTheName.ShouldBeFalse();
}
