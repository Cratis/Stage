// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_preserving_unclassified_concepts : given.a_schema
{
    string _schema = null!;

    void Because() => _schema = Schema("concept Label : String", "Label");

    [Fact] void should_keep_the_schema_byte_identical() => _schema.ShouldEqual("{\"type\":\"object\",\"properties\":{\"value\":{\"type\":\"string\"}},\"required\":[\"value\"]}");
}
