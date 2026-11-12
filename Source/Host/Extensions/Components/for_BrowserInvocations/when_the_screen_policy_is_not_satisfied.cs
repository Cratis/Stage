// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_BrowserInvocations;

public class when_the_screen_policy_is_not_satisfied : given.a_browser_invocation_gateway
{
    BrowserInvocationResult _result = null!;

    protected override string Policy => "Approvers";

    void Establish() =>
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "Approvers").Returns(Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed());

    async Task Because() => _result = await _gateway.Invoke(Request(), _person);

    [Fact] void should_refuse_it_as_forbidden() => _result.Status.ShouldEqual(BrowserInvocationStatus.Forbidden);
    [Fact] void should_not_reach_the_ability_path() => _invocations.ReceivedCalls().ShouldBeEmpty();
}
