// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_compliance : given.a_compliance_store
{
    JsonObject _properties = null!;

    async Task Because()
    {
        await SemanticChronicleRegistration.Register(_client, "Compliance", _plan);
        _properties = JsonNode.Parse(_registrations.Single().Types.Single().Schema)!["properties"]!.AsObject();
    }

    [Fact] void should_register_personal_data() => _properties["name"]!["compliance"]![0]!["metadataType"]!.GetValue<string>().ShouldEqual("PII");
    [Fact] void should_register_non_string_personal_data() => _properties["number"]!["compliance"]![0]!["metadataType"]!.GetValue<string>().ShouldEqual("PII");
    [Fact] void should_register_secrets() => _properties["token"]!["security"]![0]!["metadataType"]!.GetValue<string>().ShouldEqual("EncryptedSubject");
    [Fact] void should_use_the_same_schema_for_the_generation() => _registrations.Single().Types.Single().Generations.Single().Schema.ShouldEqual(_registrations.Single().Types.Single().Schema);
}
