// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_admitting_unused_nested_enum_types : given.a_modeled_rule
{
    void Establish() => _source = "concept Status : Enum\n  allowed\ntype Detail\n  status Status\n" + Source;

    void Because() => Admit();

    [Fact] void should_not_refuse_a_type_without_command_inputs() => _refusal.ShouldBeNull();
}
