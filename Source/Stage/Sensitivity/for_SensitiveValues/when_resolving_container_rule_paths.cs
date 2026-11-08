// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_resolving_container_rule_paths : Specification
{
    const string Marked = "{\"type\":\"string\",\"x-conceptAttributes\":{\"sensitive\":\"\"}}";

    static readonly string Schema = $"{{\"type\":\"object\",\"properties\":{{\"credentials\":{{\"type\":\"object\",\"properties\":{{\"password\":{Marked}}}}},\"history\":{{\"type\":\"array\",\"items\":{{\"type\":\"object\",\"properties\":{{\"password\":{Marked}}}}}}},\"address\":{{\"type\":\"object\",\"properties\":{{\"city\":{{\"type\":\"string\"}}}}}},\"tags\":{{\"type\":\"array\",\"items\":{{\"type\":\"string\"}}}},\"node\":{{\"type\":\"object\"}}}}}}";

    [Fact] void should_treat_a_container_carrying_a_secret_as_secret() => SensitiveValues.IsSecretPath(Schema, "credentials").ShouldBeTrue();
    [Fact] void should_treat_a_list_of_composites_carrying_a_secret_as_secret() => SensitiveValues.IsSecretPath(Schema, "history").ShouldBeTrue();
    [Fact] void should_not_treat_a_container_without_secrets_as_secret() => SensitiveValues.IsSecretPath(Schema, "address").ShouldBeFalse();
    [Fact] void should_not_treat_a_list_of_ordinary_values_as_secret() => SensitiveValues.IsSecretPath(Schema, "tags").ShouldBeFalse();
    [Fact] void should_treat_an_opaque_position_as_secret() => SensitiveValues.IsSecretPath(Schema, "node").ShouldBeTrue();
}
