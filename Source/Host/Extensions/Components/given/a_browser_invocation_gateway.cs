// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Cratis.Stage.Host.Extensions.Components.given;

public class a_browser_invocation_gateway : Specification
{
    protected IAuthorizationService _authorization = null!;
    protected IAbilityInvocations _invocations = null!;
    protected IInvocationLedger _ledger = null!;
    protected BrowserInvocations _gateway = null!;
    protected AbilityInvocation? _invoked;
    protected ClaimsPrincipal _person = null!;

    protected virtual string Policy => string.Empty;

    protected static ClaimsPrincipal Person(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    protected static BrowserInvocation Request(string ability = "Send", string interaction = "click-1") =>
        new("Orders", "Slack", "ChannelPicker", ability, interaction, """{"channel":"ops"}""");

    protected static InvocationRecord Record(InvocationStatus status, string outcome = "", string output = "") =>
        new("invocation-1", "ability:Slack.Send@1", EffectClass.Once, "sha256:a", new(InvocationOriginKind.ScreenInteraction, "Orders/Slack.ChannelPicker", "1"), status, DateTimeOffset.UnixEpoch)
        {
            Outcome = outcome,
            Output = output
        };

    void Establish()
    {
        var catalog = new ExtensionComponentCatalog([
            new("Orders", "Slack", "ChannelPicker", ExtensionComponentKind.React, ["Send"], ["picked"], Policy)
        ]);
        _person = Person("ada");
        _authorization = Substitute.For<IAuthorizationService>();
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>()).Returns(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success());
        _invocations = Substitute.For<IAbilityInvocations>();
        _invocations.Invoke(Arg.Do<AbilityInvocation>(request => _invoked = request))
            .Returns(_ => new InvocationAdmission(true, Record(InvocationStatus.Recorded), string.Empty));
        _ledger = Substitute.For<IInvocationLedger>();
        _ledger.Get("invocation-1").Returns(Record(InvocationStatus.Settled, "returned", """{"ts":"1"}"""));
        _gateway = new BrowserInvocations(
            catalog,
            _authorization,
            _invocations,
            _ledger,
            new BrowserInvocationOptions { Wait = TimeSpan.FromMilliseconds(200), Poll = TimeSpan.FromMilliseconds(5) });
    }
}
