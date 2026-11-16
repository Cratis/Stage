// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_nested_enum_membership : given.a_modeled_rule
{
    [Theory]
    [InlineData("Detail")]
    [InlineData("Detail[]")]
    [InlineData("Envelope")]
    public void should_find_enum_membership_inside_a_command_input(string type)
    {
        _source = "concept Status : Enum\n  allowed\ntype Detail\n  next Detail optional\n  status Status\ntype Envelope\n  items Detail[]\n" + Source
            .Replace("        validate", $"        details {type}\n        validate", StringComparison.Ordinal);

        Admit();

        Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
        Refusal.Message.ShouldContain("enum concept 'Status' membership on command 'RegisterRecord.details");
    }
}
