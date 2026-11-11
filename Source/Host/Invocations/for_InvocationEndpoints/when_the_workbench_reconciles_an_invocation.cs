// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.Invocations.given;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_InvocationEndpoints;

public class when_the_workbench_reconciles_an_invocation : an_invocation_silo
{
    HttpClient _client = null!;
    string _identity = null!;
    JsonElement _stopped;
    JsonElement _all;
    JsonElement _afterwards;
    HttpStatusCode _badStatus;
    HttpStatusCode _unknown;
    HttpStatusCode _badOutcome;
    HttpStatusCode _reconciled;
    HttpStatusCode _again;

    protected override bool MapEndpoints => true;

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", EffectClass.Once, "Declined")];

    async Task Because()
    {
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new() { BaseAddress = new(address) };
        _backend.Behavior = _ => throw new TimeoutException("no answer");
        _identity = (await Invocations.Invoke(new("Slack", "Pay", Origin("event 71")))).Record!.Identity;
        await Eventually(async () => (await Invocations.List(InvocationStatus.Stopped)).Count == 1, "the invocation to stop");

        _stopped = await _client.GetFromJsonAsync<JsonElement>("/stage/invocations?status=stopped");
        _all = await _client.GetFromJsonAsync<JsonElement>("/stage/invocations");
        _badStatus = (await _client.GetAsync("/stage/invocations?status=sleeping")).StatusCode;
        _unknown = (await _client.PostAsJsonAsync("/stage/invocations/inv-nothing/reconciliation", new { outcome = "returned" })).StatusCode;
        _badOutcome = (await _client.PostAsJsonAsync($"/stage/invocations/{_identity}/reconciliation", new { outcome = "Exploded" })).StatusCode;
        _reconciled = (await _client.PostAsJsonAsync($"/stage/invocations/{_identity}/reconciliation", new { outcome = "Declined", actor = "alice" })).StatusCode;
        await Eventually(() => !_reactions.Settlements.IsEmpty, "the origin to continue");
        _again = (await _client.PostAsJsonAsync($"/stage/invocations/{_identity}/reconciliation", new { outcome = "Declined", actor = "alice" })).StatusCode;
        _afterwards = await _client.GetFromJsonAsync<JsonElement>("/stage/invocations?status=stopped");
    }

    [Fact] void should_list_the_stopped_invocation_with_what_the_operator_needs() => (_stopped.GetProperty("invocations")[0].GetProperty("identity").GetString() == _identity && _stopped.GetProperty("invocations")[0].GetProperty("status").GetString() == "Stopped" && _stopped.GetProperty("invocations")[0].GetProperty("effect").GetString() == "Once" && _stopped.GetProperty("invocations")[0].GetProperty("capability").GetString() == "ability:Slack.Pay@1").ShouldBeTrue();
    [Fact] void should_count_the_stopped_invocations() => _stopped.GetProperty("stopped").GetInt32().ShouldEqual(1);
    [Fact] void should_list_invocations_of_every_status() => _all.GetProperty("invocations").GetArrayLength().ShouldEqual(1);
    [Fact] void should_refuse_a_status_that_does_not_exist() => _badStatus.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_not_find_an_invocation_that_was_never_recorded() => _unknown.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_refuse_an_outcome_the_ability_does_not_declare() => _badOutcome.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_accept_the_outcome_that_happened() => _reconciled.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_not_reconcile_what_is_no_longer_stopped() => _again.ShouldEqual(HttpStatusCode.Conflict);
    [Fact] void should_list_nothing_stopped_afterwards() => _afterwards.GetProperty("stopped").GetInt32().ShouldEqual(0);
}
