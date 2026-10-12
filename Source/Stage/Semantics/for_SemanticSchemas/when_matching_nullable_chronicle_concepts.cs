// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Concepts;
using Cratis.Serialization;
using Cratis.Types;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_matching_nullable_chronicle_concepts : given.a_schema
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_match_the_pinned_chronicle_generator(bool secret)
    {
        var pii = new PIIMetadataProvider();
        var encryption = new EncryptedMetadataProvider();
        var generator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(new KnownInstancesOf<ICanProvideComplianceMetadataForType>([pii]), new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([pii])),
            new SecurityMetadataResolver(new KnownInstancesOf<ICanProvideSecurityMetadataForType>([encryption]), new KnownInstancesOf<ICanProvideSecurityMetadataForProperty>([encryption])),
            new CamelCaseNamingPolicy());
        var expected = JsonNode.Parse(generator.Generate(secret ? typeof(secret_contract) : typeof(personal_contract)).ToJson())!["properties"]!["value"]!.ToJsonString();
        var actual = Value(Schema($"concept Protected : String {(secret ? "secret" : "pii")}", "Protected?")).ToJsonString();
        Assert.Equal(expected, actual);
    }

    [PII]
    record personal_text(string Value) : ConceptAs<string>(Value);

    [Encrypted]
    record secret_text(string Value) : ConceptAs<string>(Value);

    record personal_contract(personal_text? Value);
    record secret_contract(secret_text? Value);
}
