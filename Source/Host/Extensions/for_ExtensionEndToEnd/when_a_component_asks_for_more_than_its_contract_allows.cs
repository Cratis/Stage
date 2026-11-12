// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AnExtensionHost = Cratis.Stage.Host.Extensions.given.an_extension_host;

namespace Cratis.Stage.Host.Extensions.for_ExtensionEndToEnd;

public class when_a_component_asks_for_more_than_its_contract_allows : AnExtensionHost
{
    BrowserInvocationResult _undeclared = null!;
    BrowserInvocationResult _otherScreen = null!;
    BrowserInvocationResult _otherComponent = null!;
    BrowserInvocationResult _anonymous = null!;

    async Task Because()
    {
        await StartHost();
        var gateway = Services.GetRequiredService<BrowserInvocations>();
        _undeclared = await gateway.Invoke(new("NoticeBoard", "Notifier", "NoticeBoard", "Delete", "click-1", "{}"), Person("ada"));
        _otherScreen = await gateway.Invoke(new("Elsewhere", "Notifier", "NoticeBoard", "Notify", "click-1", "{}"), Person("ada"));
        _otherComponent = await gateway.Invoke(new("NoticeBoard", "Notifier", "Hidden", "Notify", "click-1", "{}"), Person("ada"));
        _anonymous = await gateway.Invoke(new("NoticeBoard", "Notifier", "NoticeBoard", "Notify", "click-1", "{}"), new ClaimsPrincipal(new ClaimsIdentity()));
    }

    [Fact] void should_refuse_an_ability_the_contract_does_not_let_the_component_invoke() => _undeclared.Status.ShouldEqual(BrowserInvocationStatus.NotAllowed);
    [Fact] void should_refuse_a_screen_that_does_not_place_the_component() => _otherScreen.Status.ShouldEqual(BrowserInvocationStatus.NotPlaced);
    [Fact] void should_refuse_a_component_the_screen_does_not_place() => _otherComponent.Status.ShouldEqual(BrowserInvocationStatus.NotPlaced);
    [Fact] void should_refuse_someone_who_is_not_signed_in() => _anonymous.Status.ShouldEqual(BrowserInvocationStatus.Forbidden);
    [Fact] void should_not_call_the_backend() => _backend.Invoked.ShouldBeEmpty();
}
