// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_preserving_composite_shape : given.a_schema
{
    JsonObject _schema = null!;

    void Because() => _schema = Value(Schema("concept Email : String pii\ntype Contact\n  email Email\n  visible String", "Contact"));

    [Fact] void should_classify_the_personal_leaf() => _schema["properties"]!["email"]!["compliance"]!.ToJsonString().ShouldEqual("[{\"metadataType\":\"PII\",\"details\":\"\"}]");
    [Fact] void should_leave_the_container_unclassified() => _schema.ContainsKey("compliance").ShouldBeFalse();
    [Fact] void should_leave_other_leaves_unclassified() => _schema["properties"]!["visible"]!.ToJsonString().ShouldEqual("{\"type\":\"string\"}");
}
