// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_reading_the_selection : given.a_started_host
{
    JsonElement _state;
    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, SandboxActingUser.Route);
        request.Headers.Add("Cookie", "stage-acting-user=alice");
        _response = await _client.SendAsync(request);
        _state = JsonSerializer.Deserialize<JsonElement>(await _response.Content.ReadAsStringAsync());
    }

    [Fact] void should_report_the_selection() => _state.GetProperty("current").GetString().ShouldEqual("alice");
    [Fact] void should_offer_the_display_name() => _state.GetProperty("users")[0].GetProperty("displayName").GetString().ShouldEqual("Alice Example");
    [Fact] void should_offer_modeled_personas() => _state.GetProperty("users")[0].GetProperty("personas")[0].GetString().ShouldEqual("Editor");
    [Fact] void should_not_offer_claims() => _state.GetProperty("users")[0].TryGetProperty("claims", out _).ShouldBeFalse();
    [Fact] void should_not_offer_a_principal() => _state.GetProperty("users")[0].TryGetProperty("principal", out _).ShouldBeFalse();
}
