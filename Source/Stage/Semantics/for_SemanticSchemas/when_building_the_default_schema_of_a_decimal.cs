// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticSchemas;

public class when_building_the_default_schema_of_a_decimal : given.a_schema
{
    string _schema = null!;

    void Because() => _schema = Value(Schema("", "Decimal")).ToJsonString();

    [Fact] void should_not_carry_a_format() => _schema.ShouldEqual("{\"type\":\"number\"}");
}
