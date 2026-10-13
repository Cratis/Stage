// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_appending_a_secret_input : given.a_compliance_store
{
    async Task Because()
    {
        var values = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("record-1"),
            ["name"] = JsonSerializer.SerializeToElement("Synthetic Name"),
            ["number"] = JsonSerializer.SerializeToElement(37),
            ["token"] = JsonSerializer.SerializeToElement("synthetic-secret")
        };
        await _runtime.Execute(_plan.Commands.Values.Single(), values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "caller", "Caller", "user"), false);
    }

    [Fact] void should_append_the_secret_as_event_content() => _appends.Single().Events.Single().Content.ShouldContain("synthetic-secret");
    [Fact] void should_include_only_identity_in_causation() => JsonSerializer.Serialize(_appends.Single().CausedBy).ShouldEqual("{\"Subject\":\"caller\",\"Name\":\"Caller\",\"UserName\":\"user\",\"OnBehalfOf\":null}");
    [Fact] void should_not_include_the_secret_in_causation() => JsonSerializer.Serialize(_appends.Single().CausedBy).ShouldNotContain("synthetic-secret");
}
