// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_unique_events : given.a_modeled_rule
{
    void Establish() => _source = Source.Replace("    slice StateView List", "      constraint OneRegistration\n        unique event RecordRegistered\n    slice StateView List", StringComparison.Ordinal);

    void Because() => Admit();

    [Fact] void should_name_the_unique_event_rule() => Refusal.Message.ShouldContain("constraint 'OneRegistration'");
    [Fact] void should_report_unsupported() => Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
}
