// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Sensitivity.for_SensitiveValues;

public class when_resolving_secret_rule_paths : Specification
{
    const string Marked = "{\"type\":\"string\",\"x-conceptAttributes\":{\"sensitive\":\"\"}}";

    static readonly string Schema = $"{{\"type\":\"object\",\"properties\":{{\"password\":{Marked},\"note\":{{\"type\":\"string\"}},\"credentials\":{{\"type\":\"object\",\"properties\":{{\"password\":{Marked},\"note\":{{\"type\":\"string\"}}}}}},\"history\":{{\"type\":\"array\",\"items\":{{\"type\":\"object\",\"properties\":{{\"password\":{Marked},\"note\":{{\"type\":\"string\"}}}}}}}},\"node\":{{\"type\":\"object\",\"properties\":{{\"child\":{{\"type\":\"object\"}}}}}}}}}}";

    [Fact] void should_treat_a_top_level_secret_as_secret() => SensitiveValues.IsSecretPath(Schema, "password").ShouldBeTrue();
    [Fact] void should_ignore_the_casing_of_the_path() => SensitiveValues.IsSecretPath(Schema, "PASSWORD").ShouldBeTrue();
    [Fact] void should_treat_a_nested_secret_as_secret() => SensitiveValues.IsSecretPath(Schema, "credentials.password").ShouldBeTrue();
    [Fact] void should_treat_a_secret_inside_a_list_of_composites_as_secret() => SensitiveValues.IsSecretPath(Schema, "history.password").ShouldBeTrue();
    [Fact] void should_treat_an_indexed_secret_inside_a_list_as_secret() => SensitiveValues.IsSecretPath(Schema, "history.0.password").ShouldBeTrue();
    [Fact] void should_treat_a_position_that_cannot_be_seen_into_as_secret() => SensitiveValues.IsSecretPath(Schema, "node.child.anything").ShouldBeTrue();
    [Fact] void should_not_treat_an_ordinary_property_as_secret() => SensitiveValues.IsSecretPath(Schema, "note").ShouldBeFalse();
    [Fact] void should_not_treat_an_ordinary_nested_property_as_secret() => SensitiveValues.IsSecretPath(Schema, "credentials.note").ShouldBeFalse();
    [Fact] void should_treat_an_undeclared_path_as_secret() => SensitiveValues.IsSecretPath(Schema, "unknown").ShouldBeTrue();
    [Fact] void should_treat_an_undeclared_nested_path_as_secret() => SensitiveValues.IsSecretPath(Schema, "credentials.unknown").ShouldBeTrue();
    [Fact] void should_treat_the_command_itself_as_secret_when_it_carries_a_secret() => SensitiveValues.IsSecretPath(Schema, string.Empty).ShouldBeTrue();
    [Fact] void should_treat_everything_as_secret_when_the_schema_cannot_be_read() => SensitiveValues.IsSecretPath("{not json", "note").ShouldBeTrue();
}
