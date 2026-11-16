// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_omitted_command_validation : given.a_modeled_rule
{
    void Establish() => _source = Source.Replace("          name not empty", "          name == \"allowed\"", StringComparison.Ordinal);

    void Because() => Admit();

    [Fact] void should_name_the_command_rule() => Refusal.Message.ShouldContain("command 'RegisterRecord' validation");
    [Fact] void should_report_unsupported() => Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
}
