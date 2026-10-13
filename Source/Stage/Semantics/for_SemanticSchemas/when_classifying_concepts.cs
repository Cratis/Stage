// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_classifying_concepts : given.a_schema
{
    [Theory]
    [InlineData("pii", "compliance", "PII")]
    [InlineData("secret", "security", "EncryptedSubject")]
    [InlineData("secret\n  secret scope subject", "security", "EncryptedSubject")]
    [InlineData("secret\n  secret scope namespace", "security", "EncryptedNamespace")]
    [InlineData("secret\n  secret scope global", "security", "EncryptedGlobal")]
    [InlineData("pii secret", "compliance", "PII")]
    public void should_emit_only_the_effective_classification(string classification, string category, string metadataType)
    {
        var schema = Value(Schema($"concept Protected : String {classification}", "Protected"));
        Assert.Equal($"{{\"type\":\"string\",\"{category}\":[{{\"metadataType\":\"{metadataType}\",\"details\":\"\"}}]}}", schema.ToJsonString());
    }
}
