// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_concept_validation : given.a_modeled_rule
{
    void Establish() => _source = Source.Replace("concept Name : String", "concept Name : String\n  validate\n    not empty", StringComparison.Ordinal);

    void Because() => Admit();

    [Fact] void should_name_the_concept_rule() => Refusal.Message.ShouldContain("concept 'Name' validation");
    [Fact] void should_report_unsupported() => Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
    [Fact] void should_identify_the_source() => Refusal.Message.ShouldContain("registry.play");
}
