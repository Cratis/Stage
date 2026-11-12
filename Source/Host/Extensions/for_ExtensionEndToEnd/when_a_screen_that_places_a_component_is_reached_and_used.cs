// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Components;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AnExtensionHost = Cratis.Stage.Host.Extensions.given.an_extension_host;

namespace Cratis.Stage.Host.Extensions.for_ExtensionEndToEnd;

public class when_a_screen_that_places_a_component_is_reached_and_used : AnExtensionHost
{
    const string Input = """{"message":"hello"}""";

    int _startsBeforeReach;
    IReadOnlyList<ScreenBackend> _reached = [];
    int _startsAfterReach;
    BrowserInvocationResult _first = null!;
    BrowserInvocationResult _again = null!;
    InvocationRecord? _recorded;

    async Task Because()
    {
        await StartHost();
        _startsBeforeReach = _runtime.Starts.Count;
        _reached = Services.GetRequiredService<ScreenActivation>().Reach("/NoticeBoard");
        await Eventually(() => Task.FromResult(_runtime.Starts.Count > 0), "the backend to start");
        _startsAfterReach = _runtime.Starts.Count;

        var gateway = Services.GetRequiredService<BrowserInvocations>();
        _first = await gateway.Invoke(new("NoticeBoard", "Notifier", "NoticeBoard", "Notify", "click-1", Input), Person("ada"));
        _again = await gateway.Invoke(new("NoticeBoard", "Notifier", "NoticeBoard", "Notify", "click-1", Input), Person("ada"));
        _recorded = await Services.GetRequiredService<IInvocationLedger>().Get(_first.Identity);
    }

    [Fact] void should_not_start_the_backend_before_the_screen_is_reached() => _startsBeforeReach.ShouldEqual(0);
    [Fact] void should_start_the_backend_of_the_system_when_the_screen_is_reached() => _reached.Select(backend => backend.System).ShouldContainOnly("Notifier");
    [Fact] void should_start_it_once() => _startsAfterReach.ShouldEqual(1);
    [Fact] void should_settle_the_invocation_with_the_answer_of_the_backend() => (_first.Status, _first.Outcome).ShouldEqual((BrowserInvocationStatus.Settled, "returned"));
    [Fact] void should_return_what_the_ability_returned() => JsonElement.Parse(_first.Output).GetProperty("noticeId").GetString().ShouldEqual("hello");
    [Fact] void should_have_called_the_ability_the_contract_declares() => _backend.Invoked.Select(request => request.Ability).ShouldContainOnly("Notify");
    [Fact] void should_pass_the_inputs_the_component_gave() => _backend.Invoked.Single().InputJson.ShouldEqual(Input);
    [Fact] void should_keep_the_answer_in_the_ledger() => _recorded!.Status.ShouldEqual(InvocationStatus.Settled);
    [Fact] void should_record_the_screen_interaction_as_the_cause() => _recorded!.Origin.Kind.ShouldEqual(InvocationOriginKind.ScreenInteraction);
    [Fact] void should_pin_the_invocation_to_the_locked_package() => _recorded!.PackageDigest.ShouldEqual(_entry.Digest);
    [Fact] void should_not_do_the_effect_twice_when_the_interaction_is_asked_again() => (_again.Identity, _backend.Invoked.Count).ShouldEqual((_first.Identity, 1));
}
