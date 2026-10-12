// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_preserving_collection_shape : given.a_schema
{
    string _schema = null!;

    void Because() => _schema = Value(Schema("concept Email : String pii", "Email[]")).ToJsonString();

    [Fact] void should_classify_each_element_not_the_collection() => _schema.ShouldEqual("{\"type\":\"array\",\"items\":{\"type\":\"string\",\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]}}");
}
