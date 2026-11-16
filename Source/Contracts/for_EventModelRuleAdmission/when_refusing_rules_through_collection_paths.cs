// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_rules_through_collection_paths : given.a_modeled_rule
{
    [Theory]
    [InlineData("Detail[]", "details.name")]
    [InlineData("Envelope", "details.items.name")]
    public void should_refuse_a_converted_rule_that_reaches_a_collection_element(string type, string path)
    {
        _source = "type Detail\n  name Name\ntype Envelope\n  items Detail[]\n" + Source
            .Replace("        validate", $"        details {type}\n        validate", StringComparison.Ordinal)
            .Replace("          name not empty", $"          {path} not empty", StringComparison.Ordinal);

        Admit();

        Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
        Refusal.Message.ShouldContain("command 'RegisterRecord' validation");
    }
}
