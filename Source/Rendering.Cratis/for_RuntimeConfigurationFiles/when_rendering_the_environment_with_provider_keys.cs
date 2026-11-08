// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RuntimeConfigurationFiles;

public class when_rendering_the_environment_with_provider_keys : Specification
{
    JsonArray _keys = null!;

    void Because()
    {
        var realization = ApplicationRealization.None with
        {
            Environment =
            [
                new("authentication.providers.EntraId.clientSecret", true),
                new("authentication.providers.Partner Portal.authority", false),
                new("authentication.providers.Acme-Corp.clientId", false),
                new("onboarding.email.password", true)
            ]
        };
        _keys = JsonNode.Parse(RuntimeConfigurationFiles.Environment(realization, "Lending"))!["keys"]!.AsArray();
    }

    [Fact] void should_keep_the_key_and_secret_of_every_entry() => _keys.All(entry => entry!["key"] is not null && entry["secret"] is not null).ShouldBeTrue();
    [Fact] void should_target_authproxy() => Entry("authentication.providers.EntraId.clientSecret")["target"]!.GetValue<string>().ShouldEqual("authProxy");
    [Fact] void should_name_the_authproxy_environment_configuration() => Configuration("authentication.providers.EntraId.clientSecret").ShouldEqual("Cratis:AuthProxy:Application:Environment:authentication.providers.EntraId.clientSecret");
    [Fact] void should_keep_a_space_in_the_identity() => Configuration("authentication.providers.Partner Portal.authority").ShouldEqual("Cratis:AuthProxy:Application:Environment:authentication.providers.Partner Portal.authority");
    [Fact] void should_keep_a_hyphen_in_the_identity() => Configuration("authentication.providers.Acme-Corp.clientId").ShouldEqual("Cratis:AuthProxy:Application:Environment:authentication.providers.Acme-Corp.clientId");
    [Fact] void should_not_name_a_target_for_an_onboarding_key() => Entry("onboarding.email.password").ContainsKey("target").ShouldBeFalse();
    [Fact] void should_not_name_a_configuration_for_an_onboarding_key() => Entry("onboarding.email.password").ContainsKey("configuration").ShouldBeFalse();

    JsonObject Entry(string key) => _keys.Single(entry => entry!["key"]!.GetValue<string>() == key)!.AsObject();

    string Configuration(string key) => Entry(key)["configuration"]!.GetValue<string>();
}
