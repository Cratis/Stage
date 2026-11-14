// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_enum_membership : given.a_modeled_rule
{
    void Establish() => _source = Source.Replace("concept Name : String", "concept Name : Enum\n  allowed", StringComparison.Ordinal);

    [Fact] void should_name_the_enum_rule() => Refusal.Message.ShouldContain("enum concept 'Name' membership");
    [Fact] void should_report_unsupported() => Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
}
