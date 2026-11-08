// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RuntimeConfigurationFiles;

public class when_rendering_the_environment_with_signed_forwarded_identity : Specification
{
    JsonArray _keys = null!;

    void Because()
    {
        var realization = ApplicationRealization.None with
        {
            Environment = [new("authentication.providers.EntraId.clientId", false)]
        };
        _keys = JsonNode.Parse(RuntimeConfigurationFiles.Environment(realization, "Lending", signedForwardedIdentity: true))!["keys"]!.AsArray();
    }

    [Fact] void should_keep_the_key_and_secret_of_every_entry() => _keys.All(entry => entry!["key"] is not null && entry["secret"] is not null).ShouldBeTrue();
    [Fact] void should_target_the_application_with_the_arc_secret() => Entry("forwardedIdentity.arc.secretBase64")["target"]!.GetValue<string>().ShouldEqual("application");
    [Fact] void should_name_the_arc_configuration() => Entry("forwardedIdentity.arc.secretBase64")["configuration"]!.GetValue<string>().ShouldEqual("Cratis:Arc:ForwardedIdentity:Keys:0:SecretBase64");
    [Fact] void should_target_authproxy_with_the_authproxy_secret() => Entry("forwardedIdentity.authProxy.secretBase64")["target"]!.GetValue<string>().ShouldEqual("authProxy");
    [Fact] void should_name_the_authproxy_configuration() => Entry("forwardedIdentity.authProxy.secretBase64")["configuration"]!.GetValue<string>().ShouldEqual("Cratis:AuthProxy:Services:<name>:ForwardedIdentity:SecretBase64");
    [Fact] void should_not_write_a_target_for_a_key_whose_configuration_is_not_known() => Entry("authentication.providers.EntraId.clientId").ContainsKey("target").ShouldBeFalse();
    [Fact] void should_not_write_a_configuration_for_a_key_whose_configuration_is_not_known() => Entry("authentication.providers.EntraId.clientId").ContainsKey("configuration").ShouldBeFalse();

    JsonObject Entry(string key) => _keys.Single(entry => entry!["key"]!.GetValue<string>() == key)!.AsObject();
}
