// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_classifying_optional_concepts : given.a_schema
{
    string _schema = null!;

    void Because() => _schema = Value(Schema("concept Email : String pii", "Email?")).ToJsonString();

    [Fact] void should_classify_the_nullable_primitive_once_without_a_union_wrapper() => _schema.ShouldEqual("{\"type\":[\"string\",\"null\"],\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]}");
}
